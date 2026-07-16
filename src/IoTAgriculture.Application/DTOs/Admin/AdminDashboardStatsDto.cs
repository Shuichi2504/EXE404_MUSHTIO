namespace IoTAgriculture.DTOs.Admin
{
    public class AdminDashboardStatsDto
    {
        public int TotalUsers { get; set; }
        public int TotalDevices { get; set; }
        public int OnlineDevices { get; set; }
        public int OfflineDevices { get; set; }
    }
}
