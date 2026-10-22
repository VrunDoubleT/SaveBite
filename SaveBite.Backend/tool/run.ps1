Write-Host "Starting SaveBite Redis Flash Deal Sync..." -ForegroundColor Cyan
dotnet run --project "$PSScriptRoot/SaveBite.RedisSync.csproj"
