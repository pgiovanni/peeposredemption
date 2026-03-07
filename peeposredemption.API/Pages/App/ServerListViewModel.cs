using peeposredemption.Application.DTOs.Servers;

namespace peeposredemption.API.Pages.App;

public class ServerListViewModel
{
    public List<ServerDto> Servers { get; set; } = new();
    public Dictionary<Guid, Guid> ServerDefaultChannels { get; set; } = new();
    public Guid? ActiveServerId { get; set; }
}
