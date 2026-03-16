using peeposredemption.Domain.Entities;

namespace peeposredemption.Domain.Interfaces.Repositories;

public interface IArtistSubmissionRepository
{
    Task AddAsync(ArtistSubmission submission);
    Task<ArtistSubmission?> GetByIdAsync(Guid id);
    Task<ArtistSubmission?> GetActiveByUserIdAsync(Guid userId);
    Task<List<ArtistSubmission>> GetAllAsync();
}
