using MediatR;
using peeposredemption.Domain.Entities;
using peeposredemption.Domain.Interfaces;
using System.Security.Cryptography;

namespace peeposredemption.Application.Features.ParentalControls.Commands;

public record GenerateParentalLinkCodeCommand(Guid ChildUserId) : IRequest<string>;

public class GenerateParentalLinkCodeCommandHandler : IRequestHandler<GenerateParentalLinkCodeCommand, string>
{
    private readonly IUnitOfWork _uow;
    public GenerateParentalLinkCodeCommandHandler(IUnitOfWork uow) => _uow = uow;

    public async Task<string> Handle(GenerateParentalLinkCodeCommand cmd, CancellationToken ct)
    {
        var user = await _uow.Users.GetByIdAsync(cmd.ChildUserId)
            ?? throw new InvalidOperationException("User not found.");

        if (!user.IsMinor)
            throw new InvalidOperationException("Only minors (13-17) can generate a parent link code.");

        // Check for existing active link
        var existingActive = await _uow.ParentalLinks.GetActiveByChildIdAsync(cmd.ChildUserId);
        if (existingActive != null)
            throw new InvalidOperationException("You already have an active parental link.");

        // Check for existing pending (unexpired) link — reuse it
        var existingPending = await _uow.ParentalLinks.GetPendingByChildIdAsync(cmd.ChildUserId);
        if (existingPending != null)
            return existingPending.LinkCode;

        var code = GenerateCode();
        var link = new ParentalLink
        {
            ChildUserId = cmd.ChildUserId,
            LinkCode = code,
            Status = ParentalLinkStatus.Pending
        };

        await _uow.ParentalLinks.AddAsync(link);
        await _uow.SaveChangesAsync();

        return code;
    }

    private static string GenerateCode()
    {
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        var bytes = RandomNumberGenerator.GetBytes(8);
        var result = new char[8];
        for (int i = 0; i < 8; i++)
            result[i] = chars[bytes[i] % chars.Length];
        return new string(result);
    }
}
