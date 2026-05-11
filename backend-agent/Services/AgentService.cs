using System.Text;
using System.Text.Json;
using BackendAgent.Models;
using BackendAgent.Skills;
using Microsoft.AspNetCore.Hosting;

namespace BackendAgent.Services;

/// <summary>
/// AgentService — AI Agent orchestrator using SKILL.md-driven routing.
///
/// === WHY NOT SK AUTO TOOL CALLING? ===
/// llama3.2:3b (and phi3) are too small to reliably handle SK FunctionChoiceBehavior.Auto().
/// They hallucinate tool names, call tools for greetings, and leak raw JSON to the response.
/// Auto tool calling requires 8b+ parameter models (e.g. llama3.1:8b, mistral:7b).
///
/// === WHAT THIS DOES INSTEAD ===
/// A two-stage approach that is reliable on any local model:
///
///   Stage 1 — ROUTER (LLM call, ~0.3s)
///     The SKILL.md YAML front matter (name, description, triggers) is loaded at startup
///     and baked into a routing prompt. The LLM outputs ONE digit — nothing else.
///     This is maximally constrained so even small models get it right.
///
///   Stage 2 — EXECUTE
///     C# reads the matched skill's markdown body from SKILL.md.
///     For data skills (leave, employee): C# fetches the data, injects it into {{DATA}} in
///     the SKILL.md instructions, then the LLM generates a response — so SKILL.md drives
///     both routing AND the response format.
///     For the greeting skill: the LLM generates a response from the SKILL.md instructions.
///
/// === HOW SKILL.md FILES DRIVE EVERYTHING ===
///   • YAML triggers:    what phrases route to this skill  (edit → restart → works)
///   • YAML description: what the router LLM reads        (edit → restart → works)
///   • Markdown body:    what the responder LLM is told   (greeting only)
///   No C# changes needed to change routing or greeting behaviour.
/// </summary>
public class AgentService
{
    private readonly HttpClient _http;
    private readonly LeaveSkill _leaveSkill;
    private readonly EmployeeSkill _employeeSkill;
    private readonly List<string> _knownEmployees;

    private const string OllamaUrl = "http://localhost:11434/api/chat";
    private const string ModelId   = "llama3.2:3b";

    private readonly string _routerSystemPrompt;
    private readonly Dictionary<int, SkillDefinition> _categoryToSkill;

    public AgentService(Dictionary<string, EmployeeInfo> employees, IWebHostEnvironment env)
    {
        _leaveSkill     = new LeaveSkill(employees);
        _employeeSkill  = new EmployeeSkill(employees);
        _knownEmployees = new List<string>(employees.Keys);
        _http           = new HttpClient { Timeout = TimeSpan.FromMinutes(3) };

        // Load every skills/*/SKILL.md at startup
        var skillsRoot  = Path.Combine(env.ContentRootPath, "skills");
        var definitions = SkillDefinitionLoader.LoadFromDirectory(skillsRoot);

        _categoryToSkill = new Dictionary<int, SkillDefinition>();
        for (int i = 0; i < definitions.Count; i++)
            _categoryToSkill[i + 1] = definitions[i];

        // Router prompt built entirely from SKILL.md YAML (description + triggers)
        _routerSystemPrompt = BuildRouterPrompt(definitions);

        Console.WriteLine("\n[Agent] ── RouterSystemPrompt (from SKILL.md YAML) ───────────────────");
        Console.WriteLine(_routerSystemPrompt);
        Console.WriteLine($"[Agent] Skill map: {string.Join(", ", _categoryToSkill.Select(kv => $"{kv.Key}={kv.Value.Name}"))}");
        Console.WriteLine("[Agent] ─────────────────────────────────────────────────────────────────\n");
    }

    private static string BuildRouterPrompt(List<SkillDefinition> definitions)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Classify the user message. Reply with ONLY a single digit, no other text, no punctuation.");
        for (int i = 0; i < definitions.Count; i++)
        {
            var def      = definitions[i];
            var examples = string.Join(", ", def.Triggers.Take(5));
            sb.AppendLine($"{i + 1} = {def.Description}. Examples: {examples}");
        }
        return sb.ToString().TrimEnd();
    }

    public async Task<string> ChatAsync(string userMessage)
    {
        // Stage 1 — Router LLM: reads SKILL.md descriptions+triggers, outputs a digit
        var category = await ClassifyIntentAsync(userMessage);

        if (!int.TryParse(category, out int num) ||
            !_categoryToSkill.TryGetValue(num, out SkillDefinition? skill))
        {
            return "I'm not sure how to help with that. Try asking about leave balance, departments, or say Hello.";
        }

        Console.WriteLine($"[Agent] Routed to: {skill.Name} (category {num})");

        // Stage 2 — Execute: C# fetches data, LLM formats the response using SKILL.md instructions
        var employeeName = ExtractEmployeeName(userMessage);
        return skill.Name switch
        {
            // Data skills: C# fetches raw data, then LLM responds using SKILL.md instructions + {{DATA}}
            "leave"    => await GenerateDataSkillResponseAsync(skill.Instructions, _leaveSkill.GetLeaveBalance(employeeName), userMessage, skill.MaxTokens),
            "employee" => await GenerateDataSkillResponseAsync(skill.Instructions, _employeeSkill.GetEmployeeInfo(employeeName), userMessage, skill.MaxTokens),

            // Greeting: LLM generates response from greeting/SKILL.md instructions
            "greeting" => await GenerateGreetingAsync(skill.Instructions, userMessage, skill.MaxTokens),

            _ => $"Skill '{skill.Name}' is loaded from SKILL.md but has no C# handler yet."
        };
    }

    /// <summary>
    /// Router LLM call — single digit output, temperature 0, max 5 tokens.
    /// Maximally constrained so small models always get it right.
    /// </summary>
    private async Task<string> ClassifyIntentAsync(string userMessage)
    {
        var body = JsonSerializer.Serialize(new
        {
            model    = ModelId,
            stream   = false,
            messages = new object[]
            {
                new { role = "system", content = _routerSystemPrompt },
                new { role = "user",   content = userMessage }
            },
            options = new { num_predict = 5, temperature = 0.0 }
        });

        var resp = await _http.PostAsync(OllamaUrl, new StringContent(body, Encoding.UTF8, "application/json"));
        resp.EnsureSuccessStatusCode();

        using var doc    = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
        var       content = doc.RootElement.GetProperty("message").GetProperty("content").GetString() ?? "";

        Console.WriteLine($"[Agent] Router response: '{content.Trim()}'");

        foreach (var ch in content)
            if (char.IsDigit(ch)) return ch.ToString();

        return "0";
    }

    /// <summary>
    /// Data skill LLM call — replaces {{DATA}} in SKILL.md instructions with real C# data,
    /// then sends the full instructions to the LLM so its response is shaped by SKILL.md.
    /// </summary>
    private async Task<string> GenerateDataSkillResponseAsync(string skillInstructions, string data, string userMessage, int maxTokens)
    {
        var systemPrompt = skillInstructions.Replace("{{DATA}}", data);

        Console.WriteLine($"[Agent] DataSkill system prompt sent to LLM:\n{systemPrompt}");

        var body = JsonSerializer.Serialize(new
        {
            model    = ModelId,
            stream   = false,
            messages = new object[]
            {
                new { role = "system", content = systemPrompt },
                new { role = "user",   content = userMessage }
            },
            options = new { num_predict = maxTokens, temperature = 0.2 }
        });

        var resp = await _http.PostAsync(OllamaUrl, new StringContent(body, Encoding.UTF8, "application/json"));
        resp.EnsureSuccessStatusCode();

        using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
        return doc.RootElement.GetProperty("message").GetProperty("content").GetString()?.Trim()
               ?? data;
    }

    /// <summary>
    /// Greeting LLM call — reads greeting/SKILL.md markdown body as system prompt.
    /// </summary>
    private async Task<string> GenerateGreetingAsync(string skillInstructions, string userMessage, int maxTokens)
    {
        var body = JsonSerializer.Serialize(new
        {
            model    = ModelId,
            stream   = false,
            messages = new object[]
            {
                new { role = "system", content = skillInstructions },
                new { role = "user",   content = userMessage }
            },
            options = new { num_predict = maxTokens, temperature = 0.4 }
        });

        var resp = await _http.PostAsync(OllamaUrl, new StringContent(body, Encoding.UTF8, "application/json"));
        resp.EnsureSuccessStatusCode();

        using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
        return doc.RootElement.GetProperty("message").GetProperty("content").GetString()?.Trim()
               ?? "Hello! How can I help you today?";
    }

    private string ExtractEmployeeName(string userMessage)
    {
        foreach (var name in _knownEmployees)
            if (userMessage.Contains(name, StringComparison.OrdinalIgnoreCase))
                return name;
        return userMessage;
    }
}