#!/bin/bash
# ==============================================================================
# Script tự động Seed dữ liệu PostgreSQL + Redis cho SaveBite (Linux/macOS/Git Bash)
# Cách chạy: bash seed_test_data.sh
# ==============================================================================

echo "=========================================================="
echo ">> [1/2] Đang nạp dữ liệu vào PostgreSQL (savebite-postgres)..."
echo "=========================================================="

docker exec -i savebite-postgres psql -U savebite -d savebite < "$(dirname "$0")/seed_test_stores_deals.sql"

if [ $? -eq 0 ]; then
    echo "[OK] Đã insert dữ liệu vào PostgreSQL thành công!"
else
    echo "[LỖI] Có lỗi khi chạy SQL script trên PostgreSQL."
fi

echo ""
echo "=========================================================="
echo ">> [2/2] Đang nạp dữ liệu vào Redis Cache (savebite-redis)..."
echo "=========================================================="

node "$(dirname "$0")/seed_test_data_redis.js"

if [ $? -eq 0 ]; then
    echo "[OK] Đã cache dữ liệu vào Redis thành công!"
else
    echo "[LỖI] Có lỗi khi chạy Redis seed script."
fi

echo ""
echo "=========================================================="
echo ">> HOÀN TẤT SEED TEST DATA!"
echo "Xem trực tiếp tại:"
echo "1. Cửa hàng gần bạn: http://localhost:5173/stores-near"
echo "2. Flash Deal Bánh Mì Cô Ba: http://localhost:5173/deals/a1111111-2222-3333-4444-555555555555"
echo "3. Flash Deal Trà Sữa KOI Thé: http://localhost:5173/deals/a2222222-2222-3333-4444-555555555555"
echo "4. Flash Deal Cơm Tấm Ba Ghiền: http://localhost:5173/deals/a3333333-2222-3333-4444-555555555555"
echo "5. Flash Deal Gà Rán Jollibee: http://localhost:5173/deals/a4444444-2222-3333-4444-555555555555"
echo "=========================================================="
