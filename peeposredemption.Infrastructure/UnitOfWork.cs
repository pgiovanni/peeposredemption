using peeposredemption.Domain.Interfaces;
using peeposredemption.Domain.Interfaces.Repositories;
using peeposredemption.Infrastructure.Persistence;
using System;
using System.Collections.Generic;
using System.Text;

namespace peeposredemption.Infrastructure
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly AppDbContext _db;

        public IUserRepository Users { get; }
        public IServerRepository Servers { get; }
        public IMessageRepository Messages { get; }
        public IDirectMessageRepository DirectMessages { get; }
        public IChannelRepository Channels { get; }
        public IServerInviteRepository ServerInvites { get; }
        public IFriendRequestRepository FriendRequests { get; }

        public UnitOfWork(AppDbContext db,
            IUserRepository users, IServerRepository servers,
            IMessageRepository messages, IDirectMessageRepository directMessages,
            IChannelRepository channels, IServerInviteRepository serverInvites,
            IFriendRequestRepository friendRequests)
        {
            _db = db;
            Users = users;
            Servers = servers;
            Messages = messages;
            DirectMessages = directMessages;
            Channels = channels;
            ServerInvites = serverInvites;
            FriendRequests = friendRequests;
        }

        public Task<int> SaveChangesAsync() => _db.SaveChangesAsync();
    }

}
