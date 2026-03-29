using System;
using System.Collections.Generic;
using System.Text;

namespace peeposredemption.Application.DTOs.Servers
{    
    public record ServerDto(Guid Id, string Name, string? IconUrl, string? WelcomeInviteCode = null, bool IsPrivate = false, bool IsPersonal = false);

}
