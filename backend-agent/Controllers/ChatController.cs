using BackendAgent.Models;
using BackendAgent.Services;
using Microsoft.AspNetCore.Mvc;

namespace BackendAgent.Controllers;

/// <summary>
/// ChatController - exposes a single POST /api/chat endpoint.
/// Receives user messages from the Angular UI and delegates to the AI Agent.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class ChatController : ControllerBase
{
    private readonly AgentService _agentService;
    private readonly ILogger<ChatController> _logger;

    public ChatController(AgentService agentService, ILogger<ChatController> logger)
    {
        _agentService = agentService;
        _logger = logger;
    }

    /// <summary>
    /// POST /api/chat
    /// Accepts a user message and returns the AI Agent's response.
    /// The agent internally selects and invokes the appropriate skill.
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<ChatResponse>> Chat([FromBody] ChatRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Message))
        {
            return BadRequest(new { error = "Message cannot be empty." });
        }

        _logger.LogInformation("[Agent] User says: {Message}", request.Message);

        // The agent reads the message, picks a skill, and returns a response
        var response = await _agentService.ChatAsync(request.Message);

        _logger.LogInformation("[Agent] Responds: {Response}", response);

        return Ok(new ChatResponse(response));
    }
}
