namespace IoTAgriculture.Services.Interfaces
{
    public interface IAlertService
    {
        Task ProcessAlertsAsync(CancellationToken cancellationToken = default);
    }
}
