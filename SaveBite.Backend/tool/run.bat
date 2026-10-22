@echo off
echo Starting SaveBite Redis Flash Deal Sync...
dotnet run --project "%~dp0SaveBite.RedisSync.csproj"
pause
