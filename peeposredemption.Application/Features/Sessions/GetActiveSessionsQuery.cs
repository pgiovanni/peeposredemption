using MediatR;
using peeposredemption.Application.Services;
using peeposredemption.Domain.Interfaces;

namespace peeposredemption.Application.Features.Sessions;

public record SessionDto(
    Guid Id,
    string? IpAddress,
    string? Browser,
    string? Os,
    DateTime CreatedAt,
    bool IsCurrent);

public record GetActiveSessionsQuery(Guid UserId, string? CurrentTokenHash = null) : IRequest<List<SessionDto>>;

public class GetActiveSessionsQueryHandler : IRequestHandler<GetActiveSessionsQuery, List<SessionDto>>
{
    private readonly IUnitOfWork _uow;

    public GetActiveSessionsQueryHandler(IUnitOfWork uow) => _uow = uow;

    public async Task<List<SessionDto>> Handle(GetActiveSessionsQuery query, CancellationToken ct)
    {
        var sessions = await _uow.RefreshTokens.GetActiveSessionsAsync(query.UserId);

        return sessions.Select(s => new SessionDto(
            s.Id,
            s.IpAddress,
            ParseBrowser(s.UserAgent),
            ParseOs(s.UserAgent),
            s.CreatedAt,
            IsCurrent: s.Token == query.CurrentTokenHash
        )).ToList();
    }

    private static string? ParseBrowser(string? ua)
    {
        if (string.IsNullOrEmpty(ua)) return null;
        if (ua.Contains("Edg/")) return "Edge";
        if (ua.Contains("OPR/") || ua.Contains("Opera")) return "Opera";
        if (ua.Contains("Chrome/") && !ua.Contains("Edg/")) return "Chrome";
        if (ua.Contains("Firefox/")) return "Firefox";
        if (ua.Contains("Safari/") && !ua.Contains("Chrome/")) return "Safari";
        return "Unknown";
    }

    private static string? ParseOs(string? ua)
    {
        if (string.IsNullOrEmpty(ua)) return null;
        if (ua.Contains("Windows")) return "Windows";
        if (ua.Contains("Mac OS")) return "macOS";
        if (ua.Contains("Android")) return "Android";
        if (ua.Contains("iPhone") || ua.Contains("iPad")) return "iOS";
        if (ua.Contains("Linux")) return "Linux";
        return "Unknown";
    }
}
