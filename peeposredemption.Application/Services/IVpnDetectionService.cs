namespace peeposredemption.Application.Services;

public interface IVpnDetectionService
{
    Task<(bool IsVpn, bool IsTor)> CheckIpAsync(string ipAddress);
}
