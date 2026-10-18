using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebChatAI.Application.DTOs;
using WebChatAI.Application.Features.Profile;
using WebChatAI.WebApi.Abstractions;

namespace WebChatAI.WebApi.Controllers;

[Authorize]
public class ProfileController : ApiController
{
    public ProfileController(IMediator mediator) : base(mediator)
    {
    }

    [HttpGet]
    public async Task<IActionResult> GetProfile(CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new GetProfileQuery(), cancellationToken);
        return result.Success ? Ok(result) : NotFound(result);
    }

    [HttpPut]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequestDto dto, CancellationToken cancellationToken)
    {
        var command = new UpdateProfileCommand(dto.FirstName, dto.LastName);
        var result = await Mediator.Send(command, cancellationToken);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequestDto dto, CancellationToken cancellationToken)
    {
        var command = new ChangePasswordCommand(dto.CurrentPassword, dto.NewPassword);
        var result = await Mediator.Send(command, cancellationToken);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpDelete("account")]
    public async Task<IActionResult> DeleteAccount(CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new DeleteAccountCommand(), cancellationToken);
        if (!result.Success)
        {
            return BadRequest(result);
        }

        // Clean authentication cookies
        Response.Cookies.Delete("accessToken");
        Response.Cookies.Delete("refreshToken");

        return Ok(result);
    }
}
