namespace IoTAgriculture.Services.Interfaces
{
    public interface IFirebaseRtdbService
    {
        Task<T?> GetAsync<T>(string path, CancellationToken cancellationToken = default);
        Task SetAsync<T>(string path, T value, CancellationToken cancellationToken = default);
        Task PatchAsync<T>(string path, T value, CancellationToken cancellationToken = default);
        Task<string> PushAsync<T>(string path, T value, CancellationToken cancellationToken = default);
    }
}
