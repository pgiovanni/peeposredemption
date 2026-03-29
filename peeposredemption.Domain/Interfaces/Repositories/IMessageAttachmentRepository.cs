using peeposredemption.Domain.Entities;

namespace peeposredemption.Domain.Interfaces.Repositories;

public interface IMessageAttachmentRepository
{
    Task AddAsync(MessageAttachment attachment);
    Task<MessageAttachment?> GetByIdAsync(Guid id);
    Task<List<MessageAttachment>> GetByMessageIdAsync(Guid messageId);
}
