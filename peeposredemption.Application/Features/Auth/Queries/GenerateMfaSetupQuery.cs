using MediatR;
using OtpNet;
using QRCoder;
using peeposredemption.Domain.Interfaces;
using System.Drawing;

namespace peeposredemption.Application.Features.Auth.Queries;

public record MfaSetupDto(string Secret, string QrCodeBase64);

public record GenerateMfaSetupQuery(Guid UserId) : IRequest<MfaSetupDto>;

public class GenerateMfaSetupQueryHandler : IRequestHandler<GenerateMfaSetupQuery, MfaSetupDto>
{
    private readonly IUnitOfWork _uow;

    public GenerateMfaSetupQueryHandler(IUnitOfWork uow) => _uow = uow;

    public async Task<MfaSetupDto> Handle(GenerateMfaSetupQuery req, CancellationToken ct)
    {
        var user = await _uow.Users.GetByIdAsync(req.UserId)
            ?? throw new InvalidOperationException("User not found.");

        // Generate a random 20-byte secret
        var secretBytes = KeyGeneration.GenerateRandomKey(20);
        var secret = Base32Encoding.ToString(secretBytes);

        // Build otpauth URI
        var uri = $"otpauth://totp/Torvex:{Uri.EscapeDataString(user.Username)}?secret={secret}&issuer=Torvex";

        // Generate QR code as base64 PNG
        using var qrGenerator = new QRCodeGenerator();
        var qrCodeData = qrGenerator.CreateQrCode(uri, QRCodeGenerator.ECCLevel.Q);
        using var qrCode = new PngByteQRCode(qrCodeData);
        var pngBytes = qrCode.GetGraphic(5);
        var base64 = Convert.ToBase64String(pngBytes);

        return new MfaSetupDto(secret, base64);
    }
}
