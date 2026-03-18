namespace peeposredemption.Application.DTOs.Channels;
public record ChannelDto(Guid Id, Guid ServerId, string Name, int Type = 0);
