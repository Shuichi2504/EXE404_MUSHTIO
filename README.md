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
web/
  index.html
  app.js
  styles.css
frontend/
  Flutter mobile app
```

## API

```powershell
dotnet build IoTAgriculture.sln
dotnet run --project src\IoTAgriculture.API\IoTAgriculture.API.csproj
```

## Web

Serve the `web/` folder with any static server or VS Code Live Server. The web app calls the backend API directly and lets you change the API base URL on the login screen.
