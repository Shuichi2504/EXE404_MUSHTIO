using IoTAgriculture.DTOs.Admin;

namespace IoTAgriculture.Services.Interfaces
{
    public interface IAdminService
    {
        Task<AdminDashboardStatsDto> GetDashboardStatsAsync(CancellationToken cancellationToken = default);
        Task<List<FirebaseDeviceDto>> ReadFirebaseDevicesAsync(CancellationToken cancellationToken = default);
    }
}
