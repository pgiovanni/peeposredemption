using MediatR;
using Microsoft.AspNetCore.Mvc;
using peeposredemption.Application.Features.Auth.Commands;

namespace peeposredemption.API.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly IMediator _mediator;
        public AuthController(IMediator mediator) => _mediator = mediator;

        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterCommand cmd)
            => Ok(await _mediator.Send(cmd));

        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginCommand cmd)
            => Ok(await _mediator.Send(cmd));
    }

}
