using MediatR;
using peeposredemption.Domain.Entities;
using peeposredemption.Domain.Interfaces;

namespace peeposredemption.Application.Features.Moderation.Queries
{
    public record GetMemberRoleQuery(Guid ServerId, Guid UserId) : IRequest<ServerRole>;

    public class GetMemberRoleQueryHandler : IRequestHandler<GetMemberRoleQuery, ServerRole>
    {
        private readonly IUnitOfWork _uow;
        public GetMemberRoleQueryHandler(IUnitOfWork uow) => _uow = uow;

        public async Task<ServerRole> Handle(GetMemberRoleQuery q, CancellationToken ct)
        {
            var role = await _uow.Servers.GetMemberRoleAsync(q.ServerId, q.UserId);
            return role ?? ServerRole.Member;
        }
    }
}
