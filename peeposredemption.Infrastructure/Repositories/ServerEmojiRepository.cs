using Microsoft.EntityFrameworkCore;
using peeposredemption.Domain.Entities;
using peeposredemption.Domain.Interfaces.Repositories;
using peeposredemption.Infrastructure.Persistence;

namespace peeposredemption.Infrastructure.Repositories
{
    public class ServerEmojiRepository : IServerEmojiRepository
    {
        private readonly AppDbContext _db;

        public ServerEmojiRepository(AppDbContext db) => _db = db;

        public Task<List<ServerEmoji>> GetByServerIdAsync(Guid serverId) =>
            _db.ServerEmojis.Where(e => e.ServerId == serverId).OrderBy(e => e.Name).ToListAsync();

        public Task<ServerEmoji?> GetByNameAsync(Guid serverId, string name) =>
            _db.ServerEmojis.FirstOrDefaultAsync(e => e.ServerId == serverId && e.Name == name);

        public Task<ServerEmoji?> GetByIdAsync(Guid id) =>
            _db.ServerEmojis.FirstOrDefaultAsync(e => e.Id == id);

        public async Task AddAsync(ServerEmoji emoji) =>
            await _db.ServerEmojis.AddAsync(emoji);

        public Task<int> CountByServerIdAsync(Guid serverId) =>
            _db.ServerEmojis.CountAsync(e => e.ServerId == serverId);

        public void Delete(ServerEmoji emoji) =>
            _db.ServerEmojis.Remove(emoji);
    }
}
