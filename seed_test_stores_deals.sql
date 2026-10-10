-- ==============================================================================
-- SAVEBITE SEED TEST DATA SCRIPT (POSTGRESQL)
-- Mục đích: Tạo dữ liệu mẫu đầy đủ cho 4 Cửa hàng gần bạn và 4 Flash Deals
-- Toạ độ tập trung tại Ninh Kiều, Cần Thơ (cực gần vị trí mặc định 10.024, 105.766)
-- Đảm bảo an toàn, Idempotent (chạy nhiều lần không trùng lặp / không lỗi)
-- ==============================================================================

SET client_encoding = 'UTF8';

DO $$
DECLARE
    -- Owner User ID
    v_owner_id UUID := '00000000-0000-0000-0000-000000000002';
    
    -- Category IDs
    v_cat_anvat UUID := 'f9f6c152-746d-4266-b070-622126e8d67e';
    v_cat_douong UUID := '8a481dc9-b036-4cc1-a8b0-b63ec46eff1f';
    v_cat_com UUID := '44f6e893-88fa-4107-98ae-2fff441d43ef';
    v_cat_dochien UUID := '52d66fa3-7664-44f2-bb21-5d0fa36c41c9';

    -- Shop Application IDs
    v_app_1 UUID := 'a0000000-0000-0000-0000-000000000001';
    v_app_2 UUID := 'a0000000-0000-0000-0000-000000000002';
    v_app_3 UUID := 'a0000000-0000-0000-0000-000000000003';
    v_app_4 UUID := 'a0000000-0000-0000-0000-000000000004';

    -- Shop IDs
    v_shop_1 UUID := 'b0000000-0000-0000-0000-000000000001'; -- Bánh Mì Cô Ba
    v_shop_2 UUID := 'b0000000-0000-0000-0000-000000000002'; -- Trà Sữa KOI Thé
    v_shop_3 UUID := 'b0000000-0000-0000-0000-000000000003'; -- Cơm Tấm Ba Ghiền
    v_shop_4 UUID := 'b0000000-0000-0000-0000-000000000004'; -- Gà Rán Jollibee

    -- Product IDs (Dedicated Test IDs)
    v_prod_1 UUID := 'b1a2c3d4-e5f6-4a7b-8c9d-0e1f2a3b4c5d'; -- Bánh Mì Thịt Nướng
    v_prod_2 UUID := 'b2a2c3d4-e5f6-4a7b-8c9d-0e1f2a3b4c5d'; -- Trà Sữa Trân Châu Hoàng Kim
    v_prod_3 UUID := 'b3a2c3d4-e5f6-4a7b-8c9d-0e1f2a3b4c5d'; -- Cơm Tấm Sườn Bì Chả
    v_prod_4 UUID := 'b4a2c3d4-e5f6-4a7b-8c9d-0e1f2a3b4c5d'; -- Combo Gà Giòn Sốt Cay

    -- Deal IDs
    v_deal_1 UUID := 'a1111111-2222-3333-4444-555555555555';
    v_deal_2 UUID := 'a2222222-2222-3333-4444-555555555555';
    v_deal_3 UUID := 'a3333333-2222-3333-4444-555555555555';
    v_deal_4 UUID := 'a4444444-2222-3333-4444-555555555555';

BEGIN
    -- -------------------------------------------------------------------------
    -- 1. ĐẢM BẢO USER OWNER TỒN TẠI
    -- -------------------------------------------------------------------------
    IF NOT EXISTS (SELECT 1 FROM users WHERE id = v_owner_id) THEN
        INSERT INTO users (id, email, phone, password_hash, full_name, email_verified_at, status, customer_status, shop_status, role, created_at, updated_at)
        VALUES (
            v_owner_id,
            'owner@gmail.com',
            '0900000002',
            'AQAAAAIAAYagAAAAEEkI9sXbC3bX/bNlM72/49Vn0dF4dIuHn6WwKkZ6pYd8d3f1Q1f8L3X2k9q8z1==',
            'Nguyễn Minh Anh',
            NOW(),
            'Active',
            'Active',
            'Active',
            'StoreOwner',
            NOW(),
            NOW()
        );
    END IF;

    -- -------------------------------------------------------------------------
    -- 2. ĐẢM BẢO CÁC CATEGORY TỒN TẠI
    -- -------------------------------------------------------------------------
    INSERT INTO categories (id, name, description, is_active, created_at, updated_at)
    VALUES 
        (v_cat_anvat, 'Ăn vặt', 'Bánh mì, gỏi cuốn, bánh tráng, đồ ăn nhẹ', true, NOW(), NOW()),
        (v_cat_douong, 'Đồ uống', 'Trà sữa, cà phê, nước ép trái cây tươi mát', true, NOW(), NOW()),
        (v_cat_com, 'Cơm', 'Cơm tấm sườn nướng, cơm gà xối mỡ, cơm niêu', true, NOW(), NOW()),
        (v_cat_dochien, 'Đồ chiên', 'Gà rán giòn rụm, khoai tây chiên, burger', true, NOW(), NOW())
    ON CONFLICT (id) DO UPDATE SET 
        name = EXCLUDED.name, 
        is_active = true,
        updated_at = NOW();

    -- -------------------------------------------------------------------------
    -- 3. SHOP APPLICATIONS (Được duyệt Approved)
    -- -------------------------------------------------------------------------
    INSERT INTO shop_applications (id, applicant_user_id, name, description, business_license_no, address_line, ward, district, city, latitude, longitude, logo_url, cover_image_url, opening_time, closing_time, status, bank_name, bank_account_number, bank_account_holder, payos_client_id, payos_api_key, payos_checksum_key, revision_number, created_at, updated_at)
    VALUES
        (v_app_1, v_owner_id, 'Bánh Mì Cô Ba', 'Bánh mì thịt nướng & pate nóng giòn', 'GPKD-001', '72 Nguyễn Văn Cừ Nối Dài', 'An Khánh', 'Ninh Kiều', 'Cần Thơ', 10.0275, 105.7682, 'https://images.unsplash.com/photo-1509722747041-616f39b57569?w=300', 'https://images.unsplash.com/photo-1555396273-367ea4eb4db5?w=1200', '06:00:00', '23:00:00', 'Approved', 'MB Bank', '0900000001', 'NGUYEN MINH ANH', 'payos_client_1', 'payos_key_1', 'payos_check_1', 1, NOW(), NOW()),
        (v_app_2, v_owner_id, 'Trà Sữa KOI Thé', 'Trà sữa Đài Loan trân châu hoàng kim', 'GPKD-002', '30 Tháng 4', 'Xuân Khánh', 'Ninh Kiều', 'Cần Thơ', 10.0298, 105.7665, 'https://images.unsplash.com/photo-1556881286-fc6915169721?w=300', 'https://images.unsplash.com/photo-1517248135467-4c7edcad34c4?w=1200', '08:00:00', '23:30:00', 'Approved', 'MB Bank', '0900000002', 'NGUYEN MINH ANH', 'payos_client_2', 'payos_key_2', 'payos_check_2', 1, NOW(), NOW()),
        (v_app_3, v_owner_id, 'Cơm Tấm Ba Ghiền', 'Cơm tấm sườn bì chả nướng than hoa', 'GPKD-003', 'Đường 3/2', 'Xuân Khánh', 'Ninh Kiều', 'Cần Thơ', 10.0332, 105.7690, 'https://images.unsplash.com/photo-1544025162-d76694265947?w=300', 'https://images.unsplash.com/photo-1514933651103-005eec06c04b?w=1200', '07:00:00', '22:30:00', 'Approved', 'MB Bank', '0900000003', 'NGUYEN MINH ANH', 'payos_client_3', 'payos_key_3', 'payos_check_3', 1, NOW(), NOW()),
        (v_app_4, v_owner_id, 'Gà Rán Jollibee', 'Gà rán giòn cay & burger thơm ngon', 'GPKD-004', 'Mậu Thân', 'An Hòa', 'Ninh Kiều', 'Cần Thơ', 10.0215, 105.7630, 'https://images.unsplash.com/photo-1626082927389-6cd097cdc6ec?w=300', 'https://images.unsplash.com/photo-1466978913421-dad2ebd01d17?w=1200', '09:00:00', '22:30:00', 'Approved', 'MB Bank', '0900000004', 'NGUYEN MINH ANH', 'payos_client_4', 'payos_key_4', 'payos_check_4', 1, NOW(), NOW())
    ON CONFLICT (id) DO UPDATE SET
        name = EXCLUDED.name,
        status = 'Approved',
        updated_at = NOW();

    -- -------------------------------------------------------------------------
    -- 4. SHOPS (CỬA HÀNG GẦN BẠN - STATUS: Active, GIỜ MỞ CỬA CẢ NGÀY ĐỂ TEST)
    -- -------------------------------------------------------------------------
    INSERT INTO shops (id, owner_user_id, application_id, name, description, address_line, ward, district, city, latitude, longitude, logo_url, cover_image_url, opening_time, closing_time, status, created_at, updated_at)
    VALUES
        (v_shop_1, v_owner_id, v_app_1, 'Bánh Mì Cô Ba', 'Bánh mì nướng than hoa pate béo ngậy gia truyền hơn 15 năm', '72 Nguyễn Văn Cừ Nối Dài', 'An Khánh', 'Ninh Kiều', 'Cần Thơ', 10.0275, 105.7682, 'https://images.unsplash.com/photo-1509722747041-616f39b57569?w=300', 'https://images.unsplash.com/photo-1555396273-367ea4eb4db5?w=1200', '06:00:00', '23:00:00', 'Active', NOW(), NOW()),
        (v_shop_2, v_owner_id, v_app_2, 'Trà Sữa KOI Thé', 'Trà sữa tươi chuẩn vị Đài Loan, trân châu hoàng kim dai ngon ngất ngây', '30 Tháng 4', 'Xuân Khánh', 'Ninh Kiều', 'Cần Thơ', 10.0298, 105.7665, 'https://images.unsplash.com/photo-1556881286-fc6915169721?w=300', 'https://images.unsplash.com/photo-1517248135467-4c7edcad34c4?w=1200', '08:00:00', '23:30:00', 'Active', NOW(), NOW()),
        (v_shop_3, v_owner_id, v_app_3, 'Cơm Tấm Ba Ghiền', 'Cơm tấm sườn cọng dày nướng than thơm lừng, bì giòn chả bùi đậm đà', 'Đường 3/2', 'Xuân Khánh', 'Ninh Kiều', 'Cần Thơ', 10.0332, 105.7690, 'https://images.unsplash.com/photo-1544025162-d76694265947?w=300', 'https://images.unsplash.com/photo-1514933651103-005eec06c04b?w=1200', '07:00:00', '22:30:00', 'Active', NOW(), NOW()),
        (v_shop_4, v_owner_id, v_app_4, 'Gà Rán Jollibee', 'Gà rán giòn tan rụm da ngọt mềm thịt, burger bò và mì Ý sốt bò bằm', 'Mậu Thân', 'An Hòa', 'Ninh Kiều', 'Cần Thơ', 10.0215, 105.7630, 'https://images.unsplash.com/photo-1626082927389-6cd097cdc6ec?w=300', 'https://images.unsplash.com/photo-1466978913421-dad2ebd01d17?w=1200', '09:00:00', '22:30:00', 'Active', NOW(), NOW())
    ON CONFLICT (id) DO UPDATE SET
        name = EXCLUDED.name,
        description = EXCLUDED.description,
        address_line = EXCLUDED.address_line,
        latitude = EXCLUDED.latitude,
        longitude = EXCLUDED.longitude,
        logo_url = EXCLUDED.logo_url,
        cover_image_url = EXCLUDED.cover_image_url,
        opening_time = '06:00:00',
        closing_time = '23:30:00',
        status = 'Active',
        updated_at = NOW();

    -- -------------------------------------------------------------------------
    -- 5. LÀM SẠCH DỮ LIỆU CŨ CỦA 4 DEALS TEST ĐỂ TRÁNH CONFLICT
    -- -------------------------------------------------------------------------
    DELETE FROM flash_deal_variants WHERE flash_deal_id IN (v_deal_1, v_deal_2, v_deal_3, v_deal_4);
    DELETE FROM flash_deals WHERE id IN (v_deal_1, v_deal_2, v_deal_3, v_deal_4);

    DELETE FROM product_variant_values WHERE variant_id IN (
        SELECT id FROM product_variants WHERE product_id IN (v_prod_1, v_prod_2, v_prod_3, v_prod_4)
    );
    DELETE FROM product_variants WHERE product_id IN (v_prod_1, v_prod_2, v_prod_3, v_prod_4);
    DELETE FROM product_attribute_values WHERE attribute_id IN (
        SELECT id FROM product_attributes WHERE product_id IN (v_prod_1, v_prod_2, v_prod_3, v_prod_4)
    );
    DELETE FROM product_attributes WHERE product_id IN (v_prod_1, v_prod_2, v_prod_3, v_prod_4);
    DELETE FROM product_images WHERE product_id IN (v_prod_1, v_prod_2, v_prod_3, v_prod_4);
    DELETE FROM "Products" WHERE id IN (v_prod_1, v_prod_2, v_prod_3, v_prod_4);

    -- -------------------------------------------------------------------------
    -- 6. TẠO 4 SẢN PHẨM ("Products")
    -- -------------------------------------------------------------------------
    INSERT INTO "Products" (id, shop_id, category_id, name, description, status, created_at, updated_at)
    VALUES
        (v_prod_1, v_shop_1, v_cat_anvat, 'Bánh Mì Thịt Nướng Pate', 'Bánh mì giòn rụm nướng than hoa mỗi sáng, thịt nướng thơm lừng kết hợp pate nhà làm béo ngậy.', 'Active', NOW(), NOW()),
        (v_prod_2, v_shop_2, v_cat_douong, 'Trà Sữa Trân Châu Hoàng Kim', 'Trà đen Ceylon thơm nồng hòa quyện sữa tươi thanh béo cùng trân châu hoàng kim dẻo mềm óng ánh.', 'Active', NOW(), NOW()),
        (v_prod_3, v_shop_3, v_cat_com, 'Cơm Tấm Sườn Bì Chả Đặc Biệt', 'Sườn cốt lết ướp mật ong nướng than vàng ươm, bì heo trộn thính thơm giòn và chả trứng hấp mềm mịn.', 'Active', NOW(), NOW()),
        (v_prod_4, v_shop_4, v_cat_dochien, 'Combo Gà Giòn Sốt Cay Vui Vẻ', '2 miếng gà giòn rụm sốt cay ngọt Hàn Quốc, kèm khoai tây chiên lắc phô mai và nước giải khát.', 'Active', NOW(), NOW());

    -- -------------------------------------------------------------------------
    -- 7. PRODUCT IMAGES (GALLERY THUMBNAILS)
    -- -------------------------------------------------------------------------
    -- Deal 1: Bánh Mì (dùng ảnh local đã có sẵn trong fontend)
    INSERT INTO product_images (id, product_id, image_url)
    VALUES
        (gen_random_uuid(), v_prod_1, '/images/deals/banh-mi/bm-overview.jpg'),
        (gen_random_uuid(), v_prod_1, '/images/deals/banh-mi/bm-thuong-khong-cay.jpg'),
        (gen_random_uuid(), v_prod_1, '/images/deals/banh-mi/bm-thuong-cay-vua.jpg'),
        (gen_random_uuid(), v_prod_1, '/images/deals/banh-mi/bm-lon-khong-cay.jpg'),
        (gen_random_uuid(), v_prod_1, '/images/deals/banh-mi/bm-lon-cay-vua.jpg');

    -- Deal 2: Trà Sữa KOI Thé
    INSERT INTO product_images (id, product_id, image_url)
    VALUES
        (gen_random_uuid(), v_prod_2, 'https://images.unsplash.com/photo-1558857563-b37cf5a9143a?w=800'),
        (gen_random_uuid(), v_prod_2, 'https://images.unsplash.com/photo-1541696432-82c6da8ce7bf?w=800'),
        (gen_random_uuid(), v_prod_2, 'https://images.unsplash.com/photo-1556881286-fc6915169721?w=800');

    -- Deal 3: Cơm Tấm Ba Ghiền
    INSERT INTO product_images (id, product_id, image_url)
    VALUES
        (gen_random_uuid(), v_prod_3, 'https://images.unsplash.com/photo-1544025162-d76694265947?w=800'),
        (gen_random_uuid(), v_prod_3, 'https://images.unsplash.com/photo-1563245372-f21724e3856d?w=800');

    -- Deal 4: Gà Rán Jollibee
    INSERT INTO product_images (id, product_id, image_url)
    VALUES
        (gen_random_uuid(), v_prod_4, 'https://images.unsplash.com/photo-1626082927389-6cd097cdc6ec?w=800'),
        (gen_random_uuid(), v_prod_4, 'https://images.unsplash.com/photo-1562967914-608f82629710?w=800');

    -- -------------------------------------------------------------------------
    -- 8. PRODUCT ATTRIBUTES & VALUES (Đổi ảnh theo Size và Độ cay/Topping)
    -- -------------------------------------------------------------------------
    -- Bánh Mì Attributes: Size x Độ cay
    INSERT INTO product_attributes (id, product_id, name)
    VALUES 
        ('11111111-aaaa-bbbb-cccc-111111111111', v_prod_1, 'Size'),
        ('22222222-aaaa-bbbb-cccc-222222222222', v_prod_1, 'Độ cay');

    INSERT INTO product_attribute_values (id, attribute_id, value, image_url)
    VALUES
        ('11111111-1111-1111-1111-111111111111', '11111111-aaaa-bbbb-cccc-111111111111', 'Thường', '/images/deals/banh-mi/bm-thuong-khong-cay.jpg'),
        ('22222222-2222-2222-2222-222222222222', '11111111-aaaa-bbbb-cccc-111111111111', 'Lớn - Thêm Trứng', '/images/deals/banh-mi/bm-lon-khong-cay.jpg'),
        ('33333333-3333-3333-3333-333333333333', '22222222-aaaa-bbbb-cccc-222222222222', 'Không cay', '/images/deals/banh-mi/bm-thuong-khong-cay.jpg'),
        ('44444444-4444-4444-4444-444444444444', '22222222-aaaa-bbbb-cccc-222222222222', 'Cay vừa', '/images/deals/banh-mi/bm-thuong-cay-vua.jpg');

    -- Trà Sữa Attributes: Size x Lượng đường
    INSERT INTO product_attributes (id, product_id, name)
    VALUES 
        ('33333333-aaaa-bbbb-cccc-333333333333', v_prod_2, 'Size'),
        ('44444444-aaaa-bbbb-cccc-444444444444', v_prod_2, 'Độ ngọt');

    INSERT INTO product_attribute_values (id, attribute_id, value, image_url)
    VALUES
        ('55555555-1111-1111-1111-111111111111', '33333333-aaaa-bbbb-cccc-333333333333', 'Size M', 'https://images.unsplash.com/photo-1558857563-b37cf5a9143a?w=800'),
        ('55555555-2222-2222-2222-222222222222', '33333333-aaaa-bbbb-cccc-333333333333', 'Size L (Khổng lồ)', 'https://images.unsplash.com/photo-1541696432-82c6da8ce7bf?w=800'),
        ('55555555-3333-3333-3333-333333333333', '44444444-aaaa-bbbb-cccc-444444444444', '50% Đường', 'https://images.unsplash.com/photo-1558857563-b37cf5a9143a?w=800'),
        ('55555555-4444-4444-4444-444444444444', '44444444-aaaa-bbbb-cccc-444444444444', '100% Đường chuẩn', 'https://images.unsplash.com/photo-1558857563-b37cf5a9143a?w=800');

    -- -------------------------------------------------------------------------
    -- 9. PRODUCT VARIANTS & VARIANT VALUES
    -- -------------------------------------------------------------------------
    -- Bánh Mì Variants:
    -- Var 1: Thường + Không cay (20k -> 12k)
    -- Var 2: Thường + Cay vừa (20k -> 12k)
    -- Var 3: Lớn + Không cay (30k -> 18k)
    -- Var 4: Lớn + Cay vừa (30k -> 18k)
    INSERT INTO product_variants (id, product_id, sku, original_price, deal_price, is_active, created_at)
    VALUES
        ('aaaa1111-1111-1111-1111-aaaaaaaaaaaa', v_prod_1, 'BM-THUONG-KHONGCAY', 20000.00, 12000.00, true, NOW()),
        ('bbbb2222-2222-2222-2222-bbbbbbbbbbbb', v_prod_1, 'BM-THUONG-CAYVUA',    20000.00, 12000.00, true, NOW()),
        ('cccc3333-3333-3333-3333-cccccccccccc', v_prod_1, 'BM-LON-KHONGCAY',    30000.00, 18000.00, true, NOW()),
        ('dddd4444-4444-4444-4444-dddddddddddd', v_prod_1, 'BM-LON-CAYVUA',       30000.00, 18000.00, true, NOW());

    INSERT INTO product_variant_values (variant_id, attribute_value_id)
    VALUES
        ('aaaa1111-1111-1111-1111-aaaaaaaaaaaa', '11111111-1111-1111-1111-111111111111'),
        ('aaaa1111-1111-1111-1111-aaaaaaaaaaaa', '33333333-3333-3333-3333-333333333333'),
        ('bbbb2222-2222-2222-2222-bbbbbbbbbbbb', '11111111-1111-1111-1111-111111111111'),
        ('bbbb2222-2222-2222-2222-bbbbbbbbbbbb', '44444444-4444-4444-4444-444444444444'),
        ('cccc3333-3333-3333-3333-cccccccccccc', '22222222-2222-2222-2222-222222222222'),
        ('cccc3333-3333-3333-3333-cccccccccccc', '33333333-3333-3333-3333-333333333333'),
        ('dddd4444-4444-4444-4444-dddddddddddd', '22222222-2222-2222-2222-222222222222'),
        ('dddd4444-4444-4444-4444-dddddddddddd', '44444444-4444-4444-4444-444444444444');

    -- Trà Sữa Variants:
    -- Var 5: Size M + 50% Đường (45k -> 25k)
    -- Var 6: Size M + 100% Đường (45k -> 25k)
    -- Var 7: Size L + 50% Đường (60k -> 35k)
    -- Var 8: Size L + 100% Đường (60k -> 35k)
    INSERT INTO product_variants (id, product_id, sku, original_price, deal_price, is_active, created_at)
    VALUES
        ('aaaa5555-1111-1111-1111-aaaaaaaaaaaa', v_prod_2, 'KOI-M-50D',  45000.00, 25000.00, true, NOW()),
        ('aaaa5555-2222-2222-2222-aaaaaaaaaaaa', v_prod_2, 'KOI-M-100D', 45000.00, 25000.00, true, NOW()),
        ('aaaa5555-3333-3333-3333-aaaaaaaaaaaa', v_prod_2, 'KOI-L-50D',  60000.00, 35000.00, true, NOW()),
        ('aaaa5555-4444-4444-4444-aaaaaaaaaaaa', v_prod_2, 'KOI-L-100D', 60000.00, 35000.00, true, NOW());

    INSERT INTO product_variant_values (variant_id, attribute_value_id)
    VALUES
        ('aaaa5555-1111-1111-1111-aaaaaaaaaaaa', '55555555-1111-1111-1111-111111111111'),
        ('aaaa5555-1111-1111-1111-aaaaaaaaaaaa', '55555555-3333-3333-3333-333333333333'),
        ('aaaa5555-2222-2222-2222-aaaaaaaaaaaa', '55555555-1111-1111-1111-111111111111'),
        ('aaaa5555-2222-2222-2222-aaaaaaaaaaaa', '55555555-4444-4444-4444-444444444444'),
        ('aaaa5555-3333-3333-3333-aaaaaaaaaaaa', '55555555-2222-2222-2222-222222222222'),
        ('aaaa5555-3333-3333-3333-aaaaaaaaaaaa', '55555555-3333-3333-3333-333333333333'),
        ('aaaa5555-4444-4444-4444-aaaaaaaaaaaa', '55555555-2222-2222-2222-222222222222'),
        ('aaaa5555-4444-4444-4444-aaaaaaaaaaaa', '55555555-4444-4444-4444-444444444444');

    -- Cơm Tấm Single Variant:
    INSERT INTO product_variants (id, product_id, sku, original_price, deal_price, is_active, created_at)
    VALUES
        ('aaaa6666-1111-1111-1111-aaaaaaaaaaaa', v_prod_3, 'COM-TAM-DB', 65000.00, 39000.00, true, NOW());

    -- Gà Rán Single Variant:
    INSERT INTO product_variants (id, product_id, sku, original_price, deal_price, is_active, created_at)
    VALUES
        ('aaaa7777-1111-1111-1111-aaaaaaaaaaaa', v_prod_4, 'JOL-GA-COMBO', 79000.00, 49000.00, true, NOW());

    -- -------------------------------------------------------------------------
    -- 10. FLASH DEALS (STATUS: OnSale, HIỆU LỰC 30 NGÀY TỚI)
    -- -------------------------------------------------------------------------
    INSERT INTO flash_deals (id, shop_id, product_id, sale_start_time, order_end_time, shop_closing_time, status, created_at, updated_at)
    VALUES
        (v_deal_1, v_shop_1, v_prod_1, NOW() - INTERVAL '1 hour', NOW() + INTERVAL '30 days', NOW() + INTERVAL '30 days' + INTERVAL '2 hours', 'OnSale', NOW(), NOW()),
        (v_deal_2, v_shop_2, v_prod_2, NOW() - INTERVAL '1 hour', NOW() + INTERVAL '30 days', NOW() + INTERVAL '30 days' + INTERVAL '2 hours', 'OnSale', NOW() - INTERVAL '10 minutes', NOW()),
        (v_deal_3, v_shop_3, v_prod_3, NOW() - INTERVAL '1 hour', NOW() + INTERVAL '30 days', NOW() + INTERVAL '30 days' + INTERVAL '2 hours', 'OnSale', NOW() - INTERVAL '20 minutes', NOW()),
        (v_deal_4, v_shop_4, v_prod_4, NOW() - INTERVAL '1 hour', NOW() + INTERVAL '30 days', NOW() + INTERVAL '30 days' + INTERVAL '2 hours', 'OnSale', NOW() - INTERVAL '30 minutes', NOW());

    -- -------------------------------------------------------------------------
    -- 11. FLASH DEAL VARIANTS
    -- -------------------------------------------------------------------------
    -- Deal 1: Bánh Mì Cô Ba
    INSERT INTO flash_deal_variants (id, flash_deal_id, variant_id, original_price, deal_price, discount_percent, total_quantity, reserved_quantity, sold_quantity, status, created_at, updated_at)
    VALUES
        (gen_random_uuid(), v_deal_1, 'aaaa1111-1111-1111-1111-aaaaaaaaaaaa', 20000.00, 12000.00, 40.00, 100, 5, 85, 'Active', NOW(), NOW()),
        (gen_random_uuid(), v_deal_1, 'bbbb2222-2222-2222-2222-bbbbbbbbbbbb', 20000.00, 12000.00, 40.00, 100, 5, 85, 'Active', NOW(), NOW()),
        (gen_random_uuid(), v_deal_1, 'cccc3333-3333-3333-3333-cccccccccccc', 30000.00, 18000.00, 40.00, 100, 2, 70, 'Active', NOW(), NOW()),
        (gen_random_uuid(), v_deal_1, 'dddd4444-4444-4444-4444-dddddddddddd', 30000.00, 18000.00, 40.00, 100, 2, 70, 'Active', NOW(), NOW());

    -- Deal 2: Trà Sữa KOI Thé
    INSERT INTO flash_deal_variants (id, flash_deal_id, variant_id, original_price, deal_price, discount_percent, total_quantity, reserved_quantity, sold_quantity, status, created_at, updated_at)
    VALUES
        (gen_random_uuid(), v_deal_2, 'aaaa5555-1111-1111-1111-aaaaaaaaaaaa', 45000.00, 25000.00, 44.00, 50, 2, 38, 'Active', NOW(), NOW()),
        (gen_random_uuid(), v_deal_2, 'aaaa5555-2222-2222-2222-aaaaaaaaaaaa', 45000.00, 25000.00, 44.00, 50, 1, 40, 'Active', NOW(), NOW()),
        (gen_random_uuid(), v_deal_2, 'aaaa5555-3333-3333-3333-aaaaaaaaaaaa', 60000.00, 35000.00, 41.00, 60, 3, 45, 'Active', NOW(), NOW()),
        (gen_random_uuid(), v_deal_2, 'aaaa5555-4444-4444-4444-aaaaaaaaaaaa', 60000.00, 35000.00, 41.00, 60, 4, 48, 'Active', NOW(), NOW());

    -- Deal 3: Cơm Tấm Ba Ghiền
    INSERT INTO flash_deal_variants (id, flash_deal_id, variant_id, original_price, deal_price, discount_percent, total_quantity, reserved_quantity, sold_quantity, status, created_at, updated_at)
    VALUES
        (gen_random_uuid(), v_deal_3, 'aaaa6666-1111-1111-1111-aaaaaaaaaaaa', 65000.00, 39000.00, 40.00, 80, 5, 62, 'Active', NOW(), NOW());

    -- Deal 4: Gà Rán Jollibee
    INSERT INTO flash_deal_variants (id, flash_deal_id, variant_id, original_price, deal_price, discount_percent, total_quantity, reserved_quantity, sold_quantity, status, created_at, updated_at)
    VALUES
        (gen_random_uuid(), v_deal_4, 'aaaa7777-1111-1111-1111-aaaaaaaaaaaa', 79000.00, 49000.00, 38.00, 100, 8, 75, 'Active', NOW(), NOW());

    RAISE NOTICE '=======================================================';
    RAISE NOTICE 'THÀNH CÔNG: Đã insert dữ liệu mẫu cho 4 Quán và 4 Flash Deals!';
    RAISE NOTICE 'Quán 1: Bánh Mì Cô Ba (% - %)', v_shop_1, v_deal_1;
    RAISE NOTICE 'Quán 2: Trà Sữa KOI Thé (% - %)', v_shop_2, v_deal_2;
    RAISE NOTICE 'Quán 3: Cơm Tấm Ba Ghiền (% - %)', v_shop_3, v_deal_3;
    RAISE NOTICE 'Quán 4: Gà Rán Jollibee (% - %)', v_shop_4, v_deal_4;
    RAISE NOTICE '=======================================================';
END $$;
