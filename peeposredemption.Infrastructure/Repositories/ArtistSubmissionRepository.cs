using Microsoft.EntityFrameworkCore;
using peeposredemption.Domain.Entities;
using peeposredemption.Domain.Interfaces.Repositories;
using peeposredemption.Infrastructure.Persistence;

namespace peeposredemption.Infrastructure.Repositories;

public class ArtistSubmissionRepository : IArtistSubmissionRepository
{
    private readonly AppDbContext _db;
    public ArtistSubmissionRepository(AppDbContext db) => _db = db;

    public async Task AddAsync(ArtistSubmission submission) =>
        await _db.ArtistSubmissions.AddAsync(submission);

    public Task<ArtistSubmission?> GetByIdAsync(Guid id) =>
        _db.ArtistSubmissions.Include(s => s.User).FirstOrDefaultAsync(s => s.Id == id);

    public Task<ArtistSubmission?> GetActiveByUserIdAsync(Guid userId) =>
        _db.ArtistSubmissions.FirstOrDefaultAsync(s =>
            s.UserId == userId && (s.Status == SubmissionStatus.Pending || s.Status == SubmissionStatus.Approved));

    public Task<List<ArtistSubmission>> GetAllAsync() =>
        _db.ArtistSubmissions.Include(s => s.User).OrderByDescending(s => s.SubmittedAt).ToListAsync();
}
