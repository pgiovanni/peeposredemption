using Microsoft.EntityFrameworkCore;
using peeposredemption.Domain.Entities;
using peeposredemption.Domain.Interfaces.Repositories;
using peeposredemption.Infrastructure.Persistence;

namespace peeposredemption.Infrastructure.Repositories;

public class MessageAttachmentRepository : IMessageAttachmentRepository
{
    private readonly AppDbContext _db;

    public MessageAttachmentRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task AddAsync(MessageAttachment attachment)
    {
        await _db.MessageAttachments.AddAsync(attachment);
    }

    public Task<MessageAttachment?> GetByIdAsync(Guid id)
    {
        return _db.MessageAttachments.FirstOrDefaultAsync(a => a.Id == id);
    }

    public Task<List<MessageAttachment>> GetByMessageIdAsync(Guid messageId)
    {
        return _db.MessageAttachments.Where(a => a.MessageId == messageId).ToListAsync();
    }
}
