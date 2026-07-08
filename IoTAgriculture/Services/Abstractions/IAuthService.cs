using IoTAgriculture.DTOs.Auth;

namespace IoTAgriculture.Services.Interfaces
{
    public interface IAuthService
    {
        Task<AuthResponseDto> RegisterAsync(RegisterRequestDto dto);
        Task<AuthResponseDto?> LoginAsync(LoginRequestDto dto);
        Task<UserProfileDto?> GetProfileAsync(string token);
        Task<AccountSummaryDto?> GetAccountSummaryAsync(string token);
        Task<List<UserActivityDto>> GetActivitiesAsync(string token, int limit = 50);
        Task<UserProfileDto?> UpdateProfileAsync(string token, UpdateProfileRequestDto dto);
        Task<bool> ChangePasswordAsync(string token, ChangePasswordRequestDto dto);
        Task LogoutAsync(string token);
    }
}
