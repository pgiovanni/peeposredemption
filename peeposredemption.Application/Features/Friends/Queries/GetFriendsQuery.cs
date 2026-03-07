using MediatR;
using peeposredemption.Application.DTOs.Users;
using peeposredemption.Domain.Interfaces;

namespace peeposredemption.Application.Features.Friends.Queries;

public record GetFriendsQuery(Guid UserId) : IRequest<List<UserDto>>;

public class GetFriendsQueryHandler : IRequestHandler<GetFriendsQuery, List<UserDto>>
{
    private readonly IUnitOfWork _uow;
    public GetFriendsQueryHandler(IUnitOfWork uow) => _uow = uow;

    public async Task<List<UserDto>> Handle(GetFriendsQuery query, CancellationToken ct)
    {
        var accepted = await _uow.FriendRequests.GetAcceptedAsync(query.UserId);
        return accepted.Select(r =>
        {
            var friend = r.SenderId == query.UserId ? r.Receiver : r.Sender;
            return new UserDto(friend.Id, friend.Username, friend.AvatarUrl);
        }).ToList();
    }
}
