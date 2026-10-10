# ==============================================================================
# Script tự động Seed dữ liệu PostgreSQL + Redis cho SaveBite (Windows PowerShell)
# Cách chạy: powershell ./seed_test_data.ps1
# ==============================================================================

Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host ">> [1/2] Đang nạp dữ liệu vào PostgreSQL (savebite-postgres)..." -ForegroundColor Yellow
Write-Host "==========================================================" -ForegroundColor Cyan

Get-Content (Join-Path $PSScriptRoot "seed_test_stores_deals.sql") -Raw -Encoding UTF8 | docker exec -i savebite-postgres psql -U savebite -d savebite

if ($LASTEXITCODE -eq 0) {
    Write-Host "[OK] Đã insert dữ liệu vào PostgreSQL thành công!" -ForegroundColor Green
} else {
    Write-Host "[LỖI] Có lỗi khi chạy SQL script trên PostgreSQL." -ForegroundColor Red
}

Write-Host ""
Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host ">> [2/2] Đang nạp dữ liệu vào Redis Cache (savebite-redis)..." -ForegroundColor Yellow
Write-Host "==========================================================" -ForegroundColor Cyan

node (Join-Path $PSScriptRoot "seed_test_data_redis.js")

if ($LASTEXITCODE -eq 0) {
    Write-Host "[OK] Đã cache dữ liệu vào Redis thành công!" -ForegroundColor Green
} else {
    Write-Host "[LỖI] Có lỗi khi chạy Redis seed script." -ForegroundColor Red
}

Write-Host ""
Write-Host "==========================================================" -ForegroundColor Green
Write-Host ">> HOÀN TẤT SEED TEST DATA!" -ForegroundColor Green
Write-Host "Bây giờ bạn và bạn của bạn có thể mở web để test:" -ForegroundColor White
Write-Host "1. Cửa hàng gần bạn: http://localhost:5173/stores-near" -ForegroundColor White
Write-Host "2. Flash Deal Bánh Mì Cô Ba: http://localhost:5173/deals/a1111111-2222-3333-4444-555555555555" -ForegroundColor White
Write-Host "3. Flash Deal Trà Sữa KOI Thé: http://localhost:5173/deals/a2222222-2222-3333-4444-555555555555" -ForegroundColor White
Write-Host "4. Flash Deal Cơm Tấm Ba Ghiền: http://localhost:5173/deals/a3333333-2222-3333-4444-555555555555" -ForegroundColor White
Write-Host "5. Flash Deal Gà Rán Jollibee: http://localhost:5173/deals/a4444444-2222-3333-4444-555555555555" -ForegroundColor White
Write-Host "==========================================================" -ForegroundColor Green
