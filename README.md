# IoTAgriculture

Clean workspace layout:

```text
IoTAgriculture.sln
src/
  IoTAgriculture.API/
  IoTAgriculture.Application/
  IoTAgriculture.Infrastructure/
  IoTAgriculture.Domain/
tests/
  IoTAgriculture.Tests/
frontend/
  Flutter mobile app
```

## API

```powershell
dotnet build IoTAgriculture.sln
dotnet run --project src\IoTAgriculture.API\IoTAgriculture.API.csproj
```

Health check:

```text
GET /api/health
```

Login is a POST endpoint, so opening it directly in a browser will return `405 Method Not Allowed`:

```text
POST /api/auth/login
```

## Flutter App Notes

Mushtio uses the .NET auth session stored in the database, not a standard JWT.
`POST /api/auth/login` and `POST /api/auth/register` return an auth JSON body
with `token`, `expiresAt`, and `user`. The Flutter app stores that token with
`flutter_secure_storage` and sends it as `Authorization: Bearer {token}`. There
is no refresh-token endpoint, so the client simply restores the saved token with
`GET /api/auth/me` and clears local auth state when the server rejects it.

Email OTP is handled by:

```text
POST /api/auth/request-email-code
POST /api/auth/verify-email-code
POST /api/auth/reset-password
```

Email OTP is sent with Gmail OAuth2. Configure the Google OAuth values with
user-secrets in Development or secure environment variables in Production.

Firebase in this repo is for realtime data and push notifications. It is not
used as Firebase Auth, because app authentication is the .NET session-token flow
above.

Logbook supports daily endpoints today:

```text
GET /api/logbooks/today
POST /api/logbooks/today/generate
GET /api/logbooks/today/csv
GET /api/logbooks/{yyyy-MM-dd}
```

The Flutter weekly bar chart currently composes one week from seven daily
requests. A backend `startDate/endDate` or `week` query would reduce network
round-trips later.

Design tokens are centralized in `frontend/lib/core/theme/app_theme.dart`.
Sensor colors are kept consistent across dashboard and logbook: warm coral for
temperature, blue for humidity, amber/brown for soil moisture, and violet for
CO2/air quality.

## Frontend

The current frontend is the Flutter app in `frontend/`. The old React/Vite
frontend has been removed from this solution.

## Cấu hình gửi email OTP qua Gmail OAuth2

Mushtio sends OTP email through the Gmail API using a Google OAuth2 Web Client
and the `https://www.googleapis.com/auth/gmail.send` scope. Do not put client
secrets or refresh tokens in `appsettings.json`.

Required configuration keys:

```text
Google:ClientId
Google:ClientSecret
Google:RefreshToken
Google:SenderEmail
```

Development setup:

```powershell
dotnet user-secrets set "Google:ClientId" "<CLIENT_ID>" --project src\IoTAgriculture.API\IoTAgriculture.API.csproj
dotnet user-secrets set "Google:ClientSecret" "<CLIENT_SECRET>" --project src\IoTAgriculture.API\IoTAgriculture.API.csproj
dotnet user-secrets set "Google:RefreshToken" "<REFRESH_TOKEN>" --project src\IoTAgriculture.API\IoTAgriculture.API.csproj
dotnet user-secrets set "Google:SenderEmail" "<GMAIL_SENDER>" --project src\IoTAgriculture.API\IoTAgriculture.API.csproj
```

Production setup uses environment variables or Azure Key Vault:

```text
Google__ClientId
Google__ClientSecret
Google__RefreshToken
Google__SenderEmail
```

To get or rotate the refresh token, add this redirect URI to the OAuth Web
Client in Google Cloud Console:

```text
http://localhost:5001/oauth2callback
```

Then run the one-time helper:

```powershell
$env:GOOGLE_CLIENT_ID="<CLIENT_ID>"
$env:GOOGLE_CLIENT_SECRET="<CLIENT_SECRET>"
$env:GOOGLE_SENDER_EMAIL="<GMAIL_SENDER>"
dotnet run --project tools\GmailRefreshTokenTool\GmailRefreshTokenTool.csproj
```

Sign in with the Gmail account that will send OTP email. The helper stores
`Google:ClientId`, `Google:ClientSecret`, `Google:RefreshToken`, and
`Google:SenderEmail` directly in user-secrets and does not print the refresh
token. If Gmail returns `invalid_grant`, the refresh token was revoked or
expired; run the helper again and update the stored secret.
