using MediatR;
using peeposredemption.Domain.Entities;
using peeposredemption.Domain.Interfaces;

namespace peeposredemption.Application.Features.Security.Commands;

public record ReportMessageCommand(
    Guid ReporterId,
    Guid MessageId,
    string Reason,
    string? Note) : IRequest;

public class ReportMessageCommandHandler : IRequestHandler<ReportMessageCommand>
{
    private readonly IUnitOfWork _uow;
    public ReportMessageCommandHandler(IUnitOfWork uow) => _uow = uow;

    public async Task Handle(ReportMessageCommand cmd, CancellationToken ct)
    {
        var message = await _uow.Messages.GetByIdAsync(cmd.MessageId)
            ?? throw new InvalidOperationException("Message not found.");

        // Fetch recent context messages from the same channel
        var context = await _uow.Messages.GetChannelMessagesAsync(message.ChannelId, page: 1, pageSize: 5);
        var snapshot = context
            .Select(m => $"[{m.SentAt:u}] {m.AuthorId}: {m.Content}")
            .ToList();

        var body = $"Reported message (ID: {message.Id}):\n> {message.Content}\n\nReason: {cmd.Reason}\n";
        if (!string.IsNullOrEmpty(cmd.Note))
            body += $"Note: {cmd.Note}\n";
        body += $"\n--- Recent channel context ---\n{string.Join('\n', snapshot)}";

        var ticket = new SupportTicket
        {
            UserId = cmd.ReporterId,
            Category = SupportTicketCategory.TrustSafety,
            Subject = $"Message Report: {cmd.Reason}",
            Description = body,
            Status = SupportTicketStatus.Open,
            ReportedMessageId = cmd.MessageId,
            ReportedUserId = message.AuthorId
        };

        await _uow.SupportTickets.AddAsync(ticket);
        await _uow.SaveChangesAsync();
    }
}
