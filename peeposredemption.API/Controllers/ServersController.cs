using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using peeposredemption.Application.Features.Servers.Commands;

[ApiController]
[Route("api/servers")]
[Authorize]
public class ServersController : ControllerBase
{
    private readonly IMediator _mediator;
    public ServersController(IMediator mediator) => _mediator = mediator;

    [HttpPost("create")]
    public async Task<IActionResult> Create([FromBody] CreateServerCommand cmd)
        => Ok(await _mediator.Send(cmd));
}