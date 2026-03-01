using System;
using System.Collections.Generic;
using System.Text;

namespace peeposredemption.Application.DTOs.Auth
{
    public record RegisterDto(string Username, string Email, string Password);
}
