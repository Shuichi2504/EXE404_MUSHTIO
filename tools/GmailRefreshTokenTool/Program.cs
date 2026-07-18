using System.Diagnostics;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

const string GmailSendScope = "https://www.googleapis.com/auth/gmail.send";
const string DefaultRedirectUri = "http://localhost:5001/oauth2callback";

var clientId = ReadOption(args, "--client-id") ??
    Environment.GetEnvironmentVariable("GOOGLE_CLIENT_ID");
var clientSecret = ReadOption(args, "--client-secret") ??
    Environment.GetEnvironmentVariable("GOOGLE_CLIENT_SECRET");
var senderEmail = ReadOption(args, "--sender-email") ??
    Environment.GetEnvironmentVariable("GOOGLE_SENDER_EMAIL");
var redirectUri = ReadOption(args, "--redirect-uri") ??
    Environment.GetEnvironmentVariable("GOOGLE_REDIRECT_URI") ??
    DefaultRedirectUri;
var apiProject = Path.GetFullPath(
    ReadOption(args, "--api-project") ??
    Environment.GetEnvironmentVariable("IOT_API_PROJECT") ??
    FindApiProjectPath());

if (string.IsNullOrWhiteSpace(clientId) ||
    string.IsNullOrWhiteSpace(clientSecret) ||
    string.IsNullOrWhiteSpace(senderEmail))
{
    Console.Error.WriteLine("Missing Google OAuth settings.");
    Console.Error.WriteLine("Set GOOGLE_CLIENT_ID, GOOGLE_CLIENT_SECRET, and GOOGLE_SENDER_EMAIL, or pass --client-id, --client-secret, and --sender-email.");
    return 1;
}

if (!Uri.TryCreate(redirectUri, UriKind.Absolute, out var redirect) ||
    !redirect.IsLoopback ||
    redirect.Scheme != Uri.UriSchemeHttp)
{
    Console.Error.WriteLine("Redirect URI must be an HTTP loopback URL, for example http://localhost:5001/oauth2callback.");
    return 1;
}

if (!File.Exists(apiProject))
{
    Console.Error.WriteLine($"API project was not found: {apiProject}");
    return 1;
}

var listenerPrefix = $"{redirect.Scheme}://{redirect.Authority}/";
using var listener = new HttpListener();
listener.Prefixes.Add(listenerPrefix);
listener.Start();

var state = Guid.NewGuid().ToString("N");
var authUrl = BuildAuthUrl(clientId, redirectUri, state);

Console.WriteLine("Mushtio Gmail OAuth2 refresh-token helper");
Console.WriteLine();
Console.WriteLine("Required Google Cloud Authorized redirect URI:");
Console.WriteLine(redirectUri);
Console.WriteLine();
Console.WriteLine($"Listening locally on {listenerPrefix}");
Console.WriteLine($"Callback path: {redirect.AbsolutePath}");
Console.WriteLine();
Console.WriteLine("Open this URL and sign in with the Gmail account that will send OTP email:");
Console.WriteLine(authUrl);
Console.WriteLine();
Console.WriteLine("The refresh token will be written directly to dotnet user-secrets and will not be printed.");

TryOpenBrowser(authUrl);

var context = await listener.GetContextAsync();
var request = context.Request;
var response = context.Response;

try
{
    if (request.Url == null ||
        !request.Url.AbsolutePath.Equals(redirect.AbsolutePath, StringComparison.OrdinalIgnoreCase))
    {
        await WriteBrowserResponseAsync(response, "Unexpected callback path. You can close this tab.", 400);
        return 1;
    }

    var error = request.QueryString["error"];
    if (!string.IsNullOrWhiteSpace(error))
    {
        await WriteBrowserResponseAsync(response, $"Google returned error: {WebUtility.HtmlEncode(error)}", 400);
        Console.Error.WriteLine($"Google returned error: {error}");
        return 1;
    }

    if (!string.Equals(request.QueryString["state"], state, StringComparison.Ordinal))
    {
        await WriteBrowserResponseAsync(response, "Invalid state. You can close this tab.", 400);
        Console.Error.WriteLine("Invalid OAuth state.");
        return 1;
    }

    var code = request.QueryString["code"];
    if (string.IsNullOrWhiteSpace(code))
    {
        await WriteBrowserResponseAsync(response, "Missing authorization code. You can close this tab.", 400);
        Console.Error.WriteLine("Missing authorization code.");
        return 1;
    }

    using var http = new HttpClient();
    using var tokenResponse = await http.PostAsync(
        "https://oauth2.googleapis.com/token",
        new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["client_id"] = clientId,
            ["client_secret"] = clientSecret,
            ["code"] = code,
            ["grant_type"] = "authorization_code",
            ["redirect_uri"] = redirectUri
        }));
    var body = await tokenResponse.Content.ReadAsStringAsync();
    if (!tokenResponse.IsSuccessStatusCode)
    {
        await WriteBrowserResponseAsync(response, "Token exchange failed. Check the console.", 500);
        Console.Error.WriteLine("Token exchange failed. Check that the redirect URI exactly matches the Google Cloud OAuth client.");
        return 1;
    }

    var token = JsonSerializer.Deserialize<TokenResponse>(body);
    if (string.IsNullOrWhiteSpace(token?.RefreshToken))
    {
        await WriteBrowserResponseAsync(response, "No refresh token returned. Revoke app access or force consent and try again.", 500);
        Console.Error.WriteLine("No refresh token returned. The auth URL already uses access_type=offline and prompt=consent; revoke prior app access and try again.");
        return 1;
    }

    await SetUserSecretAsync(apiProject, "Google:ClientId", clientId);
    await SetUserSecretAsync(apiProject, "Google:ClientSecret", clientSecret);
    await SetUserSecretAsync(apiProject, "Google:RefreshToken", token.RefreshToken);
    await SetUserSecretAsync(apiProject, "Google:SenderEmail", senderEmail);

    await WriteBrowserResponseAsync(response, "Refresh token retrieved successfully. You can close this tab.", 200);
    Console.WriteLine();
    Console.WriteLine("Refresh token retrieved successfully");
    Console.WriteLine($"Saved Google OAuth settings to user-secrets for: {apiProject}");
    return 0;
}
finally
{
    listener.Stop();
}

static string? ReadOption(string[] args, string name)
{
    for (var i = 0; i < args.Length - 1; i++)
    {
        if (string.Equals(args[i], name, StringComparison.OrdinalIgnoreCase))
        {
            return args[i + 1];
        }
    }

    return null;
}

static string FindApiProjectPath()
{
    var directory = new DirectoryInfo(Environment.CurrentDirectory);
    while (directory != null)
    {
        var candidate = Path.Combine(
            directory.FullName,
            "src",
            "IoTAgriculture.API",
            "IoTAgriculture.API.csproj");
        if (File.Exists(candidate))
        {
            return candidate;
        }

        directory = directory.Parent;
    }

    return Path.Combine(
        Environment.CurrentDirectory,
        "src",
        "IoTAgriculture.API",
        "IoTAgriculture.API.csproj");
}

static string BuildAuthUrl(string clientId, string redirectUri, string state)
{
    var query = new Dictionary<string, string>
    {
        ["client_id"] = clientId,
        ["redirect_uri"] = redirectUri,
        ["response_type"] = "code",
        ["scope"] = GmailSendScope,
        ["access_type"] = "offline",
        ["prompt"] = "consent",
        ["state"] = state
    };

    return "https://accounts.google.com/o/oauth2/v2/auth?" +
        string.Join("&", query.Select(x =>
            $"{WebUtility.UrlEncode(x.Key)}={WebUtility.UrlEncode(x.Value)}"));
}

static void TryOpenBrowser(string authUrl)
{
    try
    {
        Process.Start(new ProcessStartInfo(authUrl) { UseShellExecute = true });
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"Could not open browser automatically: {ex.Message}");
    }
}

static async Task SetUserSecretAsync(string apiProject, string key, string value)
{
    using var process = new Process();
    process.StartInfo = new ProcessStartInfo("dotnet")
    {
        UseShellExecute = false,
        RedirectStandardOutput = true,
        RedirectStandardError = true
    };
    process.StartInfo.ArgumentList.Add("user-secrets");
    process.StartInfo.ArgumentList.Add("set");
    process.StartInfo.ArgumentList.Add(key);
    process.StartInfo.ArgumentList.Add(value);
    process.StartInfo.ArgumentList.Add("--project");
    process.StartInfo.ArgumentList.Add(apiProject);

    process.Start();
    await process.StandardOutput.ReadToEndAsync();
    await process.StandardError.ReadToEndAsync();
    await process.WaitForExitAsync();

    if (process.ExitCode != 0)
    {
        throw new InvalidOperationException($"Failed to save user-secret '{key}'.");
    }
}

static async Task WriteBrowserResponseAsync(HttpListenerResponse response, string message, int statusCode)
{
    response.StatusCode = statusCode;
    response.ContentType = "text/html; charset=utf-8";
    var bytes = Encoding.UTF8.GetBytes($"<!doctype html><title>Mushtio Gmail OAuth</title><p>{message}</p>");
    response.ContentLength64 = bytes.Length;
    await response.OutputStream.WriteAsync(bytes);
    response.Close();
}

sealed class TokenResponse
{
    [JsonPropertyName("refresh_token")]
    public string RefreshToken { get; set; } = string.Empty;
}
