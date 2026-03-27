namespace peeposredemption.Application.Services;

public interface IAltDetectionService
{
    /// <summary>Runs full alt scoring pass and persists AltSuspicion records for score >= 50.</summary>
    Task<int> RunScanAsync();
}
