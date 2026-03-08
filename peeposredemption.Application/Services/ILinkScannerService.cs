namespace peeposredemption.Application.Services
{
    public interface ILinkScannerService
    {
        bool ContainsMaliciousLink(string content);
    }
}
