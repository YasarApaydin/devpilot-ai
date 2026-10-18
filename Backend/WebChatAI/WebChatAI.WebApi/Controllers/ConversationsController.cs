using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebChatAI.Application.DTOs;
using WebChatAI.Application.Features.Conversations;
using WebChatAI.WebApi.Abstractions;

namespace WebChatAI.WebApi.Controllers;

[Authorize]
public class ConversationsController : ApiController
{
    public ConversationsController(IMediator mediator) : base(mediator)
    {
    }

    [HttpGet]
    public async Task<IActionResult> GetConversations([FromQuery] int page = 1, [FromQuery] int pageSize = 50, CancellationToken cancellationToken = default)
    {
        var result = await Mediator.Send(new GetConversationsQuery(page, pageSize), cancellationToken);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetConversation(Guid id, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new GetConversationByIdQuery(id), cancellationToken);
        return result.Success ? Ok(result) : NotFound(result);
    }

    [HttpPost]
    public async Task<IActionResult> CreateConversation([FromBody] CreateConversationRequestDto? dto, CancellationToken cancellationToken)
    {
        var command = new CreateConversationCommand(dto?.Title);
        var result = await Mediator.Send(command, cancellationToken);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateConversation(Guid id, [FromBody] UpdateConversationRequestDto dto, CancellationToken cancellationToken)
    {
        var command = new UpdateConversationCommand(id, dto.Title, dto.IsPinned);
        var result = await Mediator.Send(command, cancellationToken);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPatch("{id:guid}/pin")]
    public async Task<IActionResult> TogglePin(Guid id, CancellationToken cancellationToken)
    {
        var command = new TogglePinConversationCommand(id);
        var result = await Mediator.Send(command, cancellationToken);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteConversation(Guid id, CancellationToken cancellationToken)
    {
        var command = new DeleteConversationCommand(id);
        var result = await Mediator.Send(command, cancellationToken);
        return result.Success ? Ok(result) : NotFound(result);
    }

    [HttpGet("{id:guid}/messages")]
    public async Task<IActionResult> GetMessages(Guid id, [FromQuery] int page = 1, [FromQuery] int pageSize = 500, CancellationToken cancellationToken = default)
    {
        var result = await Mediator.Send(new GetConversationMessagesQuery(id, page, pageSize), cancellationToken);
        return result.Success ? Ok(result) : NotFound(result);
    }
}
