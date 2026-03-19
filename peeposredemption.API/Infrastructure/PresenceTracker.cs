using System.Collections.Concurrent;

namespace peeposredemption.API.Infrastructure;

public class PresenceTracker
{
    // userId → set of connectionIds
    private readonly ConcurrentDictionary<Guid, HashSet<string>> _connections = new();

    /// <summary>Returns true if this is the user's first connection (they just came online).</summary>
    public bool UserConnected(Guid userId, string connectionId)
    {
        var connections = _connections.GetOrAdd(userId, _ => new HashSet<string>());
        lock (connections)
        {
            connections.Add(connectionId);
            return connections.Count == 1;
        }
    }

    /// <summary>Returns true if this was the user's last connection (they just went offline).</summary>
    public bool UserDisconnected(Guid userId, string connectionId)
    {
        if (!_connections.TryGetValue(userId, out var connections))
            return false;

        lock (connections)
        {
            connections.Remove(connectionId);
            if (connections.Count == 0)
            {
                _connections.TryRemove(userId, out _);
                return true;
            }
            return false;
        }
    }

    public bool IsOnline(Guid userId) => _connections.ContainsKey(userId);

    public HashSet<Guid> GetOnlineUsers(IEnumerable<Guid> userIds)
    {
        var result = new HashSet<Guid>();
        foreach (var id in userIds)
        {
            if (_connections.ContainsKey(id))
                result.Add(id);
        }
        return result;
    }
}
