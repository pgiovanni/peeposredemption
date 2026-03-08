using MediatR;
using peeposredemption.Domain.Entities;
using peeposredemption.Domain.Interfaces;

namespace peeposredemption.Application.Features.Moderation.Queries
{
    public record ServerMemberDto(Guid UserId, string Username, string? Nickname, ServerRole Role);

    public record GetServerMembersQuery(Guid ServerId, Guid RequesterId) : IRequest<List<ServerMemberDto>>;

    public class GetServerMembersQueryHandler : IRequestHandler<GetServerMembersQuery, List<ServerMemberDto>>
    {
        private readonly IUnitOfWork _uow;
        public GetServerMembersQueryHandler(IUnitOfWork uow) => _uow = uow;

        public async Task<List<ServerMemberDto>> Handle(GetServerMembersQuery q, CancellationToken ct)
        {
            var requesterRole = await _uow.Servers.GetMemberRoleAsync(q.ServerId, q.RequesterId);
            if (requesterRole is null)
                throw new UnauthorizedAccessException("You are not a member of this server.");

            var members = await _uow.Servers.GetServerMembersAsync(q.ServerId);
            return members
                .Select(sm => new ServerMemberDto(sm.UserId, sm.User.Username, sm.Nickname, sm.Role))
                .ToList();
        }
    }
}
