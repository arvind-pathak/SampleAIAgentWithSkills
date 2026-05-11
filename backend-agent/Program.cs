using System.Text.Json;
using BackendAgent.Models;
using BackendAgent.Services;

var builder = WebApplication.CreateBuilder(args);

// =====================================================
// CORS — Allow Angular frontend (different port) to call the API
// =====================================================
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy
            .AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader();
    });
});

// =====================================================
// LOAD MOCK EMPLOYEE DATA
// employees.json simulates a database for this demo.
// =====================================================
var dataPath = Path.Combine(AppContext.BaseDirectory, "Data", "employees.json");
var jsonContent = File.ReadAllText(dataPath);

var jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
var employees = JsonSerializer.Deserialize<Dictionary<string, EmployeeInfo>>(jsonContent, jsonOptions)
    ?? new Dictionary<string, EmployeeInfo>();

// Make employee data available for dependency injection into AgentService
builder.Services.AddSingleton(employees);

// =====================================================
// REGISTER AI AGENT SERVICE
// AgentService wires up Semantic Kernel + Ollama + all Skills.
// Singleton because building the Kernel is expensive.
// =====================================================
builder.Services.AddSingleton<AgentService>();

builder.Services.AddControllers();

var app = builder.Build();

// Pre-warm the AgentService so the first chat request is fast
app.Services.GetRequiredService<AgentService>();

// Enable CORS before routing (order matters!)
app.UseCors();

app.MapControllers();

app.Run();
