using System;
using System.Collections.Generic;
using System.Text;

namespace peeposredemption.Application.DTOs.Messages
{
    public record SendMessageDto(Guid ChannelId, string Content);
}
