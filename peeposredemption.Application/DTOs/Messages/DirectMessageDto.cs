using System;
using System.Collections.Generic;
using System.Text;

namespace peeposredemption.Application.DTOs.Messages
{
    public record DirectMessageDto(Guid Id, Guid SenderId, Guid RecipientId, string Content, DateTime SentAt);
}
