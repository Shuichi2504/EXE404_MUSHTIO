# IoTAgriculture

Clean workspace layout:

```text
IoTAgriculture.sln
src/
  IoTAgriculture.API/
    wwwroot/
      index.html
      app.js
      styles.css
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

## Web

The web app lives in `src/IoTAgriculture.API/wwwroot` and is served by the API project at `/`.

```text
GET /
```
