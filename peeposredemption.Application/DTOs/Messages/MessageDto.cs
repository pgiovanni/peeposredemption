using System;
using System.Collections.Generic;
using System.Text;

namespace peeposredemption.Application.DTOs.Messages
{
    public record MessageDto(
        Guid Id,
        Guid AuthorId,
        string AuthorUsername,
        string Content,
        DateTime SentAt,
        bool IsDeleted = false,
        string? AuthorAvatarUrl = null,
        Guid? ReplyToMessageId = null,
        string? ReplyToAuthorUsername = null,
        string? ReplyToContentPreview = null,
        string? AttachmentUrl = null,
        string? AttachmentFileName = null,
        string? AttachmentContentType = null);
}
