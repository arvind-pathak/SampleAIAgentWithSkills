using System.Text.Json.Serialization;

namespace BackendAgent.Models;

/// <summary>
/// Represents an employee record loaded from employees.json mock data.
/// </summary>
public class EmployeeInfo
{
    [JsonPropertyName("department")]
    public string Department { get; set; } = string.Empty;

    [JsonPropertyName("leaveBalance")]
    public int LeaveBalance { get; set; }
}

/// <summary>
/// Incoming request from the Angular chat UI.
/// </summary>
public record ChatRequest(string Message);

/// <summary>
/// Outgoing response from the AI Agent to the Angular chat UI.
/// </summary>
public record ChatResponse(string Response);
