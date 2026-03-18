using MediatR;
using peeposredemption.Domain.Interfaces;

namespace peeposredemption.Application.Features.Security.Queries;

public record GetUserSecurityInfoQuery(Guid UserId) : IRequest<UserSecurityInfoDto>;

public record UserSecurityInfoDto(
    Guid UserId,
    string Username,
    bool IsSuspicious,
    List<IpLogDto> IpLogs,
    List<DeviceDto> Devices,
    List<FingerprintDto> Fingerprints,
    List<LinkedAccountDto> LinkedAccounts);

public record IpLogDto(string IpAddress, bool IsVpn, bool IsTor, DateTime SeenAt);
public record DeviceDto(Guid DeviceId, DateTime FirstSeenAt, DateTime LastSeenAt, bool IsBanned);
public record FingerprintDto(string FingerprintHash, DateTime CreatedAt);
public record LinkedAccountDto(Guid UserId, string Username, string MatchType, string MatchValue);

public class GetUserSecurityInfoQueryHandler : IRequestHandler<GetUserSecurityInfoQuery, UserSecurityInfoDto>
{
    private readonly IUnitOfWork _uow;

    public GetUserSecurityInfoQueryHandler(IUnitOfWork uow) => _uow = uow;

    public async Task<UserSecurityInfoDto> Handle(GetUserSecurityInfoQuery query, CancellationToken ct)
    {
        var user = await _uow.Users.GetByIdAsync(query.UserId)
            ?? throw new InvalidOperationException("User not found.");

        var ipLogs = await _uow.UserIpLogs.GetByUserIdAsync(query.UserId);
        var devices = await _uow.UserDevices.GetByUserIdAsync(query.UserId);
        var fingerprints = await _uow.UserFingerprints.GetByUserIdAsync(query.UserId);

        // Find linked accounts (shared IPs, devices, fingerprints)
        var linked = new Dictionary<Guid, LinkedAccountDto>();

        // By IP
        foreach (var ip in ipLogs.Select(l => l.IpAddress).Distinct())
        {
            var matches = await _uow.UserIpLogs.GetByIpAddressAsync(ip);
            foreach (var m in matches.Where(m => m.UserId != query.UserId))
                linked.TryAdd(m.UserId, new LinkedAccountDto(m.UserId, m.User.Username, "IP", ip));
        }

        // By device
        foreach (var d in devices)
        {
            var matches = await _uow.UserDevices.GetByDeviceIdAsync(d.DeviceId);
            foreach (var m in matches.Where(m => m.UserId != query.UserId))
                linked.TryAdd(m.UserId, new LinkedAccountDto(m.UserId, m.User.Username, "Device", d.DeviceId.ToString()));
        }

        // By fingerprint
        foreach (var fp in fingerprints)
        {
            var matches = await _uow.UserFingerprints.GetByFingerprintHashAsync(fp.FingerprintHash);
            foreach (var m in matches.Where(m => m.UserId != query.UserId))
                linked.TryAdd(m.UserId, new LinkedAccountDto(m.UserId, m.User.Username, "Fingerprint", fp.FingerprintHash[..8]));
        }

        return new UserSecurityInfoDto(
            user.Id,
            user.Username,
            user.IsSuspicious,
            ipLogs.Select(l => new IpLogDto(l.IpAddress, l.IsVpn, l.IsTor, l.SeenAt)).ToList(),
            devices.Select(d => new DeviceDto(d.DeviceId, d.FirstSeenAt, d.LastSeenAt, d.IsBanned)).ToList(),
            fingerprints.Select(f => new FingerprintDto(f.FingerprintHash, f.CreatedAt)).ToList(),
            linked.Values.ToList());
    }
}
