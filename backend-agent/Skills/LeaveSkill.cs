using BackendAgent.Models;

namespace BackendAgent.Skills;

/// <summary>
/// LeaveSkill — returns leave balance data from employees.json.
/// Called by AgentService when the router LLM classifies a message as a leave query.
/// The result is returned directly to the user — no second LLM call needed.
/// </summary>
public class LeaveSkill
{
    private readonly Dictionary<string, EmployeeInfo> _employees;

    public LeaveSkill(Dictionary<string, EmployeeInfo> employees)
    {
        _employees = employees;
    }

    public string GetLeaveBalance(string employeeName)
    {
        // Normalize name to handle case variations
        var normalizedName = _employees.Keys
            .FirstOrDefault(k => k.Equals(employeeName, StringComparison.OrdinalIgnoreCase));

        if (normalizedName is not null)
        {
            var balance = _employees[normalizedName].LeaveBalance;
            return $"{normalizedName} has {balance} leave days remaining.";
        }

        return $"Employee '{employeeName}' was not found in the system. Available employees: {string.Join(", ", _employees.Keys)}.";
    }
}
