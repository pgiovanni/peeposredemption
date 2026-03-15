using MediatR;
using peeposredemption.Domain.Entities;
using peeposredemption.Domain.Interfaces;

namespace peeposredemption.Application.Features.ParentalControls.Commands;

public record ClaimParentalLinkCommand(Guid ParentUserId, string LinkCode) : IRequest<Unit>;

public class ClaimParentalLinkCommandHandler : IRequestHandler<ClaimParentalLinkCommand, Unit>
{
    private readonly IUnitOfWork _uow;
    public ClaimParentalLinkCommandHandler(IUnitOfWork uow) => _uow = uow;

    public async Task<Unit> Handle(ClaimParentalLinkCommand cmd, CancellationToken ct)
    {
        var parent = await _uow.Users.GetByIdAsync(cmd.ParentUserId)
            ?? throw new InvalidOperationException("User not found.");

        if (parent.DateOfBirth.HasValue && parent.DateOfBirth.Value.AddYears(18) > DateTime.UtcNow)
            throw new InvalidOperationException("You must be 18 or older to claim a parental link.");

        var link = await _uow.ParentalLinks.GetByCodeAsync(cmd.LinkCode.Trim().ToUpperInvariant())
            ?? throw new InvalidOperationException("Invalid or expired link code.");

        if (link.Status != ParentalLinkStatus.Pending)
            throw new InvalidOperationException("This link code has already been used.");

        if (link.CreatedAt < DateTime.UtcNow.AddHours(-24))
            throw new InvalidOperationException("This link code has expired. Ask the minor to generate a new one.");

        if (link.ChildUserId == cmd.ParentUserId)
            throw new InvalidOperationException("You cannot claim your own link code.");

        link.ParentUserId = cmd.ParentUserId;
        link.Status = ParentalLinkStatus.Active;

        await _uow.SaveChangesAsync();

        return Unit.Value;
    }
}
