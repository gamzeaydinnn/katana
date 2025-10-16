@echo off
echo Starting Katana Backend...
cd /d "%~dp0"
start "Katana Backend" cmd /k "dotnet run --project src\Katana.API\Katana.API.csproj"
timeout /t 8 /nobreak
echo.
echo Starting Katana Frontend...
cd frontend\katana-web
start "Katana Frontend" cmd /k "npm start"
echo.
echo Both services are starting...
echo Backend: http://localhost:5000
echo Frontend: http://localhost:3000
echo.
pause
