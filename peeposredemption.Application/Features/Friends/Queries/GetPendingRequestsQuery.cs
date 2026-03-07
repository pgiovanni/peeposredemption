using MediatR;
using peeposredemption.Application.DTOs.Friends;
using peeposredemption.Domain.Interfaces;

namespace peeposredemption.Application.Features.Friends.Queries;

public record GetPendingRequestsQuery(Guid UserId) : IRequest<List<FriendRequestDto>>;

public class GetPendingRequestsQueryHandler : IRequestHandler<GetPendingRequestsQuery, List<FriendRequestDto>>
{
    private readonly IUnitOfWork _uow;
    public GetPendingRequestsQueryHandler(IUnitOfWork uow) => _uow = uow;

    public async Task<List<FriendRequestDto>> Handle(GetPendingRequestsQuery query, CancellationToken ct)
    {
        var pending = await _uow.FriendRequests.GetPendingReceivedAsync(query.UserId);
        return pending.Select(r => new FriendRequestDto(r.Id, r.Sender.Username, r.CreatedAt)).ToList();
    }
}
