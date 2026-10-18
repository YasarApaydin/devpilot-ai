using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace WebChatAI.WebApi.Abstractions;

[ApiController]
[Route("api/[controller]")]
public abstract class ApiController : ControllerBase
{
    protected readonly IMediator Mediator;

    protected ApiController(IMediator mediator)
    {
        Mediator = mediator;
    }
}
