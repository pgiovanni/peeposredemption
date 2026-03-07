using System;
using System.Collections.Generic;
using System.Text;

namespace peeposredemption.Application.DTOs.Messages
{
    public record MessageDto(Guid Id, Guid AuthorId, string AuthorUsername, string Content, DateTime SentAt);
}
