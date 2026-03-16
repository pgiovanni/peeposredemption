namespace peeposredemption.Application.DTOs.Users;

public record UserDto(Guid Id, string Username, string? AvatarUrl, string? DisplayName = null)
{
    public string DisplayOrUsername => DisplayName ?? Username;
}
