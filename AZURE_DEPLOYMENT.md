# Azure Deployment

This project is split into:

- `backend/IoTAgriculture`: ASP.NET Core API
- `frontend`: Flutter app

## 1. Backend

Deploy the backend to Azure App Service.

Set these App Service settings:

- `ConnectionStrings__DefaultConnection`: Azure SQL connection string
- `Jwt__Key`: a strong secret key
- `Gemini__ApiKey`: your Gemini API key
- `Firebase__AuthToken`: Firebase RTDB auth token if you use one
- `Cors__AllowedOrigins`: optional browser client origins, separated by comma or semicolon

If you want local development to continue working, keep your local values in `backend/IoTAgriculture/appsettings.Development.json`.

## 2. Database

Use Azure SQL Database.

- Create an Azure SQL server and database
- Update the backend app setting `ConnectionStrings__DefaultConnection`
- Run migrations against the Azure SQL database before or after deployment

## 3. Local defaults

- Flutter local default API URL: `http://10.0.2.2:5000/api`
- Backend local settings: `backend/IoTAgriculture/appsettings.Development.json`

## 4. Notes

- The backend no longer forces a fixed port in code.
- CORS is configured from `Cors:AllowedOrigins` instead of always allowing every origin.
