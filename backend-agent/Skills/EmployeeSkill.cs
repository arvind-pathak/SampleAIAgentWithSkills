using BackendAgent.Models;

namespace BackendAgent.Skills;

/// <summary>
/// EmployeeSkill — returns department info from employees.json.
/// Called by AgentService when the router LLM classifies a message as an employee query.
/// The result is returned directly to the user — no second LLM call needed.
/// </summary>
public class EmployeeSkill
{
    private readonly Dictionary<string, EmployeeInfo> _employees;

    public EmployeeSkill(Dictionary<string, EmployeeInfo> employees)
    {
        _employees = employees;
    }

    public string GetEmployeeInfo(string employeeName)
    {
        var normalizedName = _employees.Keys
            .FirstOrDefault(k => k.Equals(employeeName, StringComparison.OrdinalIgnoreCase));

        if (normalizedName is not null)
        {
            var employee = _employees[normalizedName];
            return $"{normalizedName} works in the {employee.Department} department and has {employee.LeaveBalance} leave days remaining.";
        }

        return $"Employee '{employeeName}' was not found. Available employees: {string.Join(", ", _employees.Keys)}.";
    }
}
