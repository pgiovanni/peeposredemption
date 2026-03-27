using MediatR;
using peeposredemption.Domain.Interfaces;

namespace peeposredemption.Application.Features.Security.Queries;

public record GetAltSuspicionsQuery : IRequest<List<AltSuspicionDto>>;

public class AltSuspicionDto
{
    public Guid Id { get; set; }
    public Guid UserId1 { get; set; }
    public string Username1 { get; set; } = "";
    public Guid UserId2 { get; set; }
    public string Username2 { get; set; } = "";
    public int Score { get; set; }
    public List<string> Signals { get; set; } = new();
    public DateTime DetectedAt { get; set; }
    public bool? IsConfirmed { get; set; }
}

public class GetAltSuspicionsQueryHandler : IRequestHandler<GetAltSuspicionsQuery, List<AltSuspicionDto>>
{
    private readonly IUnitOfWork _uow;
    public GetAltSuspicionsQueryHandler(IUnitOfWork uow) => _uow = uow;

    public async Task<List<AltSuspicionDto>> Handle(GetAltSuspicionsQuery query, CancellationToken ct)
    {
        var pending = await _uow.AltSuspicions.GetPendingAsync();
        return pending.Select(s => new AltSuspicionDto
        {
            Id = s.Id,
            UserId1 = s.UserId1,
            Username1 = s.User1.Username,
            UserId2 = s.UserId2,
            Username2 = s.User2.Username,
            Score = s.Score,
            Signals = System.Text.Json.JsonSerializer.Deserialize<List<string>>(s.Signals) ?? new(),
            DetectedAt = s.DetectedAt,
            IsConfirmed = s.IsConfirmed
        }).ToList();
    }
}
