using System.Collections.Concurrent;

namespace peeposredemption.API.Infrastructure;

public record VoiceParticipant(
    Guid UserId,
    string DisplayName,
    string? AvatarUrl,
    string ConnectionId,
    bool IsMuted = false,
    bool IsDeafened = false,
    bool IsCameraOn = false);

public class VoiceStateTracker
{
    private const int MaxParticipants = 6;

    // channelId -> (userId -> participant)
    private readonly ConcurrentDictionary<Guid, ConcurrentDictionary<Guid, VoiceParticipant>> _channels = new();

    public (bool Success, List<VoiceParticipant> Participants) TryJoin(
        Guid channelId, Guid userId, string displayName, string? avatarUrl, string connectionId)
    {
        var participants = _channels.GetOrAdd(channelId, _ => new ConcurrentDictionary<Guid, VoiceParticipant>());

        // Already in this channel — update connection
        if (participants.ContainsKey(userId))
        {
            participants[userId] = new VoiceParticipant(userId, displayName, avatarUrl, connectionId);
            return (true, participants.Values.ToList());
        }

        if (participants.Count >= MaxParticipants)
            return (false, participants.Values.ToList());

        var participant = new VoiceParticipant(userId, displayName, avatarUrl, connectionId);
        participants[userId] = participant;
        return (true, participants.Values.ToList());
    }

    public VoiceParticipant? Leave(Guid channelId, Guid userId)
    {
        if (!_channels.TryGetValue(channelId, out var participants))
            return null;

        participants.TryRemove(userId, out var removed);

        if (participants.IsEmpty)
            _channels.TryRemove(channelId, out _);

        return removed;
    }

    public List<(Guid ChannelId, VoiceParticipant Participant)> LeaveByConnectionId(string connectionId)
    {
        var removed = new List<(Guid, VoiceParticipant)>();

        foreach (var (channelId, participants) in _channels)
        {
            var match = participants.Values.FirstOrDefault(p => p.ConnectionId == connectionId);
            if (match != null)
            {
                participants.TryRemove(match.UserId, out _);
                removed.Add((channelId, match));

                if (participants.IsEmpty)
                    _channels.TryRemove(channelId, out _);
            }
        }

        return removed;
    }

    public void UpdateState(Guid channelId, Guid userId, bool? muted, bool? deafened, bool? cameraOn)
    {
        if (!_channels.TryGetValue(channelId, out var participants))
            return;

        if (!participants.TryGetValue(userId, out var current))
            return;

        participants[userId] = current with
        {
            IsMuted = muted ?? current.IsMuted,
            IsDeafened = deafened ?? current.IsDeafened,
            IsCameraOn = cameraOn ?? current.IsCameraOn
        };
    }

    public List<VoiceParticipant> GetParticipants(Guid channelId)
    {
        if (_channels.TryGetValue(channelId, out var participants))
            return participants.Values.ToList();
        return new List<VoiceParticipant>();
    }
}
