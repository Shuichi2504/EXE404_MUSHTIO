using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using IoTAgriculture.Services.Interfaces;

namespace IoTAgriculture.Services
{
    public class EmailSender : IEmailSender
    {
        private static readonly SemaphoreSlim TokenLock = new(1, 1);
        private static string? _cachedAccessToken;
        private static DateTimeOffset _cachedAccessTokenExpiresAt;

        private readonly IConfiguration _configuration;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<EmailSender> _logger;

        public EmailSender(
            IConfiguration configuration,
            IHttpClientFactory httpClientFactory,
            ILogger<EmailSender> logger)
        {
            _configuration = configuration;
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        public async Task SendVerificationCodeAsync(
            string email,
            string code,
            string purpose,
            CancellationToken cancellationToken = default)
        {
            var senderEmail = RequiredConfig("Google:SenderEmail");
            var accessToken = await GetAccessTokenAsync(cancellationToken);
            var rawMessage = BuildRawMessage(senderEmail, email, code, purpose);

            using var request = new HttpRequestMessage(
                HttpMethod.Post,
                "https://gmail.googleapis.com/gmail/v1/users/me/messages/send");
            request.Headers.Authorization = new("Bearer", accessToken);
            request.Content = JsonContent.Create(new GmailSendRequest(rawMessage));

            var client = _httpClientFactory.CreateClient();
            using var response = await client.SendAsync(request, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation(
                    "Sent Mushtio OTP email to {Email} for {Purpose}",
                    email,
                    purpose);
                return;
            }

            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError(
                "Gmail send failed with status {StatusCode}: {Body}",
                response.StatusCode,
                body);
            throw new InvalidOperationException(
                $"Gmail send failed with status {(int)response.StatusCode}. Check Google OAuth credentials and Gmail API access.");
        }

        private async Task<string> GetAccessTokenAsync(CancellationToken cancellationToken)
        {
            if (!string.IsNullOrWhiteSpace(_cachedAccessToken) &&
                _cachedAccessTokenExpiresAt > DateTimeOffset.UtcNow.AddMinutes(5))
            {
                return _cachedAccessToken;
            }

            await TokenLock.WaitAsync(cancellationToken);
            try
            {
                if (!string.IsNullOrWhiteSpace(_cachedAccessToken) &&
                    _cachedAccessTokenExpiresAt > DateTimeOffset.UtcNow.AddMinutes(5))
                {
                    return _cachedAccessToken;
                }

                var clientId = RequiredConfig("Google:ClientId");
                var clientSecret = RequiredConfig("Google:ClientSecret");
                var refreshToken = RequiredConfig("Google:RefreshToken");

                using var request = new HttpRequestMessage(
                    HttpMethod.Post,
                    "https://oauth2.googleapis.com/token")
                {
                    Content = new FormUrlEncodedContent(new Dictionary<string, string>
                    {
                        ["client_id"] = clientId,
                        ["client_secret"] = clientSecret,
                        ["refresh_token"] = refreshToken,
                        ["grant_type"] = "refresh_token"
                    })
                };

                var client = _httpClientFactory.CreateClient();
                using var response = await client.SendAsync(request, cancellationToken);
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                if (!response.IsSuccessStatusCode)
                {
                    if (body.Contains("invalid_grant", StringComparison.OrdinalIgnoreCase))
                    {
                        _logger.LogError(
                            "Google OAuth refresh failed with invalid_grant. The refresh token is expired or revoked; run the Gmail refresh-token tool again.");
                    }
                    else
                    {
                        _logger.LogError(
                            "Google OAuth refresh failed with status {StatusCode}: {Body}",
                            response.StatusCode,
                            body);
                    }

                    throw new InvalidOperationException(
                        $"Google OAuth refresh failed with status {(int)response.StatusCode}.");
                }

                var token = JsonSerializer.Deserialize<GoogleTokenResponse>(
                    body,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (string.IsNullOrWhiteSpace(token?.AccessToken))
                {
                    throw new InvalidOperationException("Google OAuth token response did not contain an access token.");
                }

                _cachedAccessToken = token.AccessToken;
                _cachedAccessTokenExpiresAt = DateTimeOffset.UtcNow.AddSeconds(
                    Math.Max(60, token.ExpiresIn - 60));
                return _cachedAccessToken;
            }
            finally
            {
                TokenLock.Release();
            }
        }

        private string RequiredConfig(string key)
        {
            var value = _configuration[key];
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value;
            }

            throw new InvalidOperationException(
                $"Missing required configuration '{key}'. Set it with user-secrets for Development or environment variables in Production.");
        }

        private static string BuildRawMessage(
            string senderEmail,
            string recipientEmail,
            string code,
            string purpose)
        {
            var subject = EncodeHeader("Mã xác thực Mushtio");
            var purposeLabel = purpose.Equals("reset-password", StringComparison.OrdinalIgnoreCase)
                ? "đặt lại mật khẩu"
                : "đăng ký tài khoản";
            var html = $"""
                <!doctype html>
                <html>
                <body style="font-family: Arial, sans-serif; color: #1C2B22; line-height: 1.5;">
                  <h2 style="color: #2E7D5B;">Mã xác thực Mushtio</h2>
                  <p>Bạn đang yêu cầu mã OTP để {WebUtility.HtmlEncode(purposeLabel)}.</p>
                  <p style="font-size: 28px; font-weight: 700; letter-spacing: 6px; color: #2E7D5B;">{WebUtility.HtmlEncode(code)}</p>
                  <p>Mã có hiệu lực trong 10 phút.</p>
                  <p>Nếu bạn không thực hiện yêu cầu này, vui lòng bỏ qua email. Không chia sẻ mã này cho bất kỳ ai.</p>
                </body>
                </html>
                """;

            var builder = new StringBuilder()
                .Append("From: Mushtio <").Append(senderEmail).AppendLine(">")
                .Append("To: ").Append(recipientEmail).AppendLine()
                .Append("Subject: ").Append(subject).AppendLine()
                .AppendLine("MIME-Version: 1.0")
                .AppendLine("Content-Type: text/html; charset=UTF-8")
                .AppendLine("Content-Transfer-Encoding: 8bit")
                .AppendLine()
                .AppendLine(html);

            return Base64UrlEncode(Encoding.UTF8.GetBytes(builder.ToString()));
        }

        private static string EncodeHeader(string value)
        {
            return "=?UTF-8?B?" + Convert.ToBase64String(Encoding.UTF8.GetBytes(value)) + "?=";
        }

        private static string Base64UrlEncode(byte[] bytes)
        {
            return Convert.ToBase64String(bytes)
                .TrimEnd('=')
                .Replace('+', '-')
                .Replace('/', '_');
        }

        private sealed record GmailSendRequest([property: JsonPropertyName("raw")] string Raw);

        private sealed class GoogleTokenResponse
        {
            [JsonPropertyName("access_token")]
            public string AccessToken { get; set; } = string.Empty;

            [JsonPropertyName("expires_in")]
            public int ExpiresIn { get; set; }
        }
    }
}
