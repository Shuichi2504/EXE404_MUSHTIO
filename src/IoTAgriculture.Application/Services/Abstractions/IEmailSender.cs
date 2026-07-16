namespace IoTAgriculture.Services.Interfaces
{
    public interface IEmailSender
    {
        Task SendVerificationCodeAsync(
            string email,
            string code,
            string purpose,
            CancellationToken cancellationToken = default);
    }
}
