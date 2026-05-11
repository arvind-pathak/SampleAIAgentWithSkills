namespace BackendAgent.Services;

/// <summary>
/// Represents a skill loaded from a SKILL.md file.
///
/// A SKILL.md file has two parts:
///
///   1. YAML front matter (between --- fences):
///      name:        the skill identifier (must match C# routing switch)
///      description: short phrase sent to the LLM router for intent classification
///      max_tokens:  (optional) how many tokens the LLM may use for the response
///      triggers:    list of example phrases that trigger this skill
///
///   2. Markdown body (everything after the closing ---):
///      The actual instructions sent to the LLM when this skill is invoked.
///      The LLM reads these instructions and generates the response dynamically.
///      Use {{DATA}} as a placeholder — AgentService substitutes real data here.
///
/// This means: to change how the agent responds, edit SKILL.md. No C# changes needed.
/// </summary>
public class SkillDefinition
{
    /// <summary>Skill identifier — from name: in YAML. Used to map to C# data logic.</summary>
    public string Name { get; set; } = "";

    /// <summary>
    /// Short LLM-friendly phrase from description: in YAML.
    /// Embedded in the router prompt so the LLM can classify user intent.
    /// </summary>
    public string Description { get; set; } = "";

    /// <summary>
    /// Max tokens for the LLM response — from max_tokens: in YAML (default: 150).
    /// Tune verbosity per skill without changing C# code.
    /// </summary>
    public int MaxTokens { get; set; } = 150;

    /// <summary>
    /// Example phrases from triggers: list in YAML.
    /// Sent to the router LLM alongside Description to improve classification accuracy.
    /// Add a new phrase here and restart — agent immediately recognises it.
    /// </summary>
    public List<string> Triggers { get; set; } = new();

    /// <summary>
    /// The markdown body of SKILL.md — everything after the closing --- fence.
    /// Sent to the LLM as a system prompt when this skill is invoked.
    /// The LLM reads these instructions and writes the response.
    /// May contain {{DATA}} which AgentService replaces with real C# data.
    /// </summary>
    public string Instructions { get; set; } = "";
}

/// <summary>
/// SkillDefinitionLoader — discovers and parses every SKILL.md under the skills/ folder.
///
/// Folder convention (mirrors the pattern from the video):
///   skills/
///     greeting/SKILL.md
///     leave/SKILL.md
///     employee/SKILL.md
///
/// Each SKILL.md has YAML front matter (---) followed by a markdown body.
/// The loader parses both and returns a list of SkillDefinition objects.
///
/// At startup AgentService calls LoadFromDirectory(), then:
///   a) Builds the router prompt from Description + Triggers  — drives intent classification
///   b) Stores Instructions per skill                         — drives response generation
///
/// This is the same pattern used by GitHub Copilot, Claude Code, and Semantic Kernel:
/// skills are self-describing .md files the agent reads — not hardcoded strings in code.
/// </summary>
public static class SkillDefinitionLoader
{
    /// <summary>
    /// Scans every subdirectory of skillsRoot for a SKILL.md file and parses it.
    /// Subdirectories are sorted alphabetically so category numbers are stable.
    /// </summary>
    public static List<SkillDefinition> LoadFromDirectory(string skillsRoot)
    {
        var definitions = new List<SkillDefinition>();

        if (!Directory.Exists(skillsRoot))
        {
            Console.WriteLine($"[SkillLoader] WARNING: skills/ directory not found: {skillsRoot}");
            return definitions;
        }

        foreach (var skillDir in Directory.GetDirectories(skillsRoot).OrderBy(d => d))
        {
            var skillMdPath = Path.Combine(skillDir, "SKILL.md");
            if (!File.Exists(skillMdPath))
            {
                Console.WriteLine($"[SkillLoader] Skipping '{skillDir}' — no SKILL.md found.");
                continue;
            }

            var def = ParseSkillMd(skillMdPath);
            if (def is not null)
            {
                definitions.Add(def);
                Console.WriteLine($"[SkillLoader] Loaded '{def.Name}' — {def.Triggers.Count} trigger(s), max_tokens={def.MaxTokens}");
                Console.WriteLine($"              description:  \"{def.Description}\"");
                Console.WriteLine($"              instructions: {def.Instructions.Length} chars");
            }
        }

        return definitions;
    }

    /// <summary>
    /// Parses a single SKILL.md file.
    ///
    /// Format:
    ///   ---
    ///   name: SkillName
    ///   description: short routing description
    ///   max_tokens: 120
    ///   triggers:
    ///     - Example phrase one
    ///     - Example phrase two
    ///   ---
    ///
    ///   Markdown body used as LLM system prompt.
    ///   May contain {{DATA}} placeholder.
    ///
    /// The parser is intentionally simple (no YAML library dependency).
    /// </summary>
    private static SkillDefinition? ParseSkillMd(string filePath)
    {
        var lines = File.ReadAllLines(filePath);
        var def = new SkillDefinition
        {
            Name = Path.GetFileName(Path.GetDirectoryName(filePath) ?? "")
        };

        int i = 0;

        if (lines.Length == 0 || lines[0].Trim() != "---")
        {
            Console.WriteLine($"[SkillLoader] Skipping '{filePath}' — missing YAML front matter (no opening ---).");
            return null;
        }
        i = 1;

        bool inTriggers = false;
        for (; i < lines.Length; i++)
        {
            var line = lines[i];
            if (line.Trim() == "---") { i++; break; }

            if (inTriggers && line.TrimStart().StartsWith("- "))
            {
                def.Triggers.Add(line.TrimStart()[2..].Trim());
                continue;
            }

            var colonIdx = line.IndexOf(':');
            if (colonIdx < 0) continue;

            var key   = line[..colonIdx].Trim().ToLowerInvariant();
            var value = line[(colonIdx + 1)..].Trim();

            inTriggers = false;

            switch (key)
            {
                case "name":        def.Name        = value;                                     break;
                case "description": def.Description = value;                                     break;
                case "max_tokens":  if (int.TryParse(value, out int mt)) def.MaxTokens = mt;     break;
                case "triggers":    inTriggers = true;                                           break;
            }
        }

        def.Instructions = string.Join("\n", lines.Skip(i)).Trim();

        if (string.IsNullOrWhiteSpace(def.Name))
        {
            Console.WriteLine($"[SkillLoader] Skipping '{filePath}' — name: is required in YAML front matter.");
            return null;
        }

        if (string.IsNullOrWhiteSpace(def.Instructions))
        {
            Console.WriteLine($"[SkillLoader] Skipping '{filePath}' — markdown body (LLM instructions) is empty.");
            return null;
        }

        return def;
    }
}