# Gmail Refresh Token Tool

One-time helper for getting a Gmail OAuth2 refresh token for Mushtio OTP email.

1. In Google Cloud Console, keep any production redirect URI that already
   exists, and add this local loopback URI to the OAuth Web Client:

   ```text
   http://localhost:5001/oauth2callback
   ```

2. Run the tool with temporary environment variables. The client secret is read
   from the environment and is never written to source files.

   ```powershell
   $env:GOOGLE_CLIENT_ID="<client-id>"
   $env:GOOGLE_CLIENT_SECRET="<client-secret>"
   $env:GOOGLE_SENDER_EMAIL="<gmail-sender>"
   dotnet run --project tools/GmailRefreshTokenTool/GmailRefreshTokenTool.csproj
   ```

3. The tool prints the exact redirect URI, opens a browser, and waits locally
   for Google's callback. Sign in with the Gmail account that will send OTP
   email and consent to:

   ```text
   https://www.googleapis.com/auth/gmail.send
   ```

4. When successful, the tool prints only:

   ```text
   Refresh token retrieved successfully
   ```

   It stores `Google:ClientId`, `Google:ClientSecret`, `Google:RefreshToken`,
   and `Google:SenderEmail` directly in user-secrets for the API project.
