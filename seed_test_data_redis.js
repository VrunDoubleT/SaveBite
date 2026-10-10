/**
 * SAVEBITE REDIS SEED SCRIPT
 * Tự động tạo dữ liệu Redis cache cho 4 Cửa hàng gần bạn và 4 Flash Deals
 * Không cần npm install (chạy thuần Node.js core 'net')
 * Chạy lệnh: node seed_test_data_redis.js
 */

const net = require("net");

const REDIS_HOST = process.env.REDIS_HOST || "localhost";
const REDIS_PORT = parseInt(process.env.REDIS_PORT || "6379", 10);
const REDIS_PASS = process.env.REDIS_PASSWORD || "SaveBiTePasswOrd@@14";

// 1. DỮ LIỆU CỬA HÀNG (GEO)
const SHOPS = [
  {
    id: "b0000000-0000-0000-0000-000000000001",
    name: "Bánh Mì Cô Ba",
    lat: 10.0275,
    lon: 105.7682,
    address: "72 Nguyễn Văn Cừ Nối Dài, An Khánh, Ninh Kiều, Cần Thơ",
    logo: "https://images.unsplash.com/photo-1509722747041-616f39b57569?w=300",
  },
  {
    id: "b0000000-0000-0000-0000-000000000002",
    name: "Trà Sữa KOI Thé",
    lat: 10.0298,
    lon: 105.7665,
    address: "30 Tháng 4, Xuân Khánh, Ninh Kiều, Cần Thơ",
    logo: "https://images.unsplash.com/photo-1556881286-fc6915169721?w=300",
  },
  {
    id: "b0000000-0000-0000-0000-000000000003",
    name: "Cơm Tấm Ba Ghiền",
    lat: 10.0332,
    lon: 105.769,
    address: "Đường 3/2, Xuân Khánh, Ninh Kiều, Cần Thơ",
    logo: "https://images.unsplash.com/photo-1544025162-d76694265947?w=300",
  },
  {
    id: "b0000000-0000-0000-0000-000000000004",
    name: "Gà Rán Jollibee",
    lat: 10.0215,
    lon: 105.763,
    address: "Mậu Thân, An Hòa, Ninh Kiều, Cần Thơ",
    logo: "https://images.unsplash.com/photo-1626082927389-6cd097cdc6ec?w=300",
  },
];

// Thời gian hiệu lực: từ hiện tại đến 30 ngày tới
const now = new Date();
const orderEnd = new Date(now.getTime() + 30 * 24 * 60 * 60 * 1000);
const closingTime = new Date(orderEnd.getTime() + 2 * 60 * 60 * 1000);
const orderEndScore = Math.floor(orderEnd.getTime() / 1000);

// 2. DỮ LIỆU FLASH DEALS
const DEALS = [
  // Deal 1: Bánh Mì Cô Ba
  {
    id: "a1111111-2222-3333-4444-555555555555",
    shopId: "b0000000-0000-0000-0000-000000000001",
    shopName: "Bánh Mì Cô Ba",
    shopAddress: "72 Nguyễn Văn Cừ Nối Dài, An Khánh, Ninh Kiều, Cần Thơ",
    shopLogoUrl: "https://images.unsplash.com/photo-1509722747041-616f39b57569?w=300",
    productId: "b1a2c3d4-e5f6-4a7b-8c9d-0e1f2a3b4c5d",
    productName: "Bánh Mì Thịt Nướng Pate",
    productImageUrl: "/images/deals/banh-mi/bm-overview.jpg",
    productImageUrls: [
      "/images/deals/banh-mi/bm-overview.jpg",
      "/images/deals/banh-mi/bm-thuong-khong-cay.jpg",
      "/images/deals/banh-mi/bm-thuong-cay-vua.jpg",
      "/images/deals/banh-mi/bm-lon-khong-cay.jpg",
      "/images/deals/banh-mi/bm-lon-cay-vua.jpg",
    ],
    categoryName: "Ăn vặt",
    description: "Bánh mì giòn rụm nướng than hoa mỗi sáng, thịt nướng thơm lừng kết hợp pate nhà làm béo ngậy ăn kèm đồ chua giòn rụm.",
    minDealPrice: 12000,
    maxOriginalPrice: 30000,
    maxDiscountPercent: 40,
    variants: [
      {
        Id: "11111111-1111-1111-1111-111111111111",
        VariantId: "aaaa1111-1111-1111-1111-aaaaaaaaaaaa",
        Sku: "BM-THUONG-KHONGCAY",
        VariantName: "Thường - Không cay",
        ImageUrl: "/images/deals/banh-mi/bm-thuong-khong-cay.jpg",
        Attributes: { Size: "Thường", "Độ cay": "Không cay" },
        OriginalPrice: 20000,
        DealPrice: 12000,
        DiscountPercent: 40,
        TotalQuantity: 100,
        SoldQuantity: 85,
        AvailableQuantity: 15,
        Status: 0,
      },
      {
        Id: "22222222-2222-2222-2222-222222222222",
        VariantId: "bbbb2222-2222-2222-2222-bbbbbbbbbbbb",
        Sku: "BM-THUONG-CAYVUA",
        VariantName: "Thường - Cay vừa",
        ImageUrl: "/images/deals/banh-mi/bm-thuong-cay-vua.jpg",
        Attributes: { Size: "Thường", "Độ cay": "Cay vừa" },
        OriginalPrice: 20000,
        DealPrice: 12000,
        DiscountPercent: 40,
        TotalQuantity: 100,
        SoldQuantity: 85,
        AvailableQuantity: 15,
        Status: 0,
      },
      {
        Id: "33333333-3333-3333-3333-333333333333",
        VariantId: "cccc3333-3333-3333-3333-cccccccccccc",
        Sku: "BM-LON-KHONGCAY",
        VariantName: "Lớn - Thêm Trứng - Không cay",
        ImageUrl: "/images/deals/banh-mi/bm-lon-khong-cay.jpg",
        Attributes: { Size: "Lớn - Thêm Trứng", "Độ cay": "Không cay" },
        OriginalPrice: 30000,
        DealPrice: 18000,
        DiscountPercent: 40,
        TotalQuantity: 100,
        SoldQuantity: 70,
        AvailableQuantity: 30,
        Status: 0,
      },
      {
        Id: "44444444-4444-4444-4444-444444444444",
        VariantId: "dddd4444-4444-4444-4444-dddddddddddd",
        Sku: "BM-LON-CAYVUA",
        VariantName: "Lớn - Thêm Trứng - Cay vừa",
        ImageUrl: "/images/deals/banh-mi/bm-lon-cay-vua.jpg",
        Attributes: { Size: "Lớn - Thêm Trứng", "Độ cay": "Cay vừa" },
        OriginalPrice: 30000,
        DealPrice: 18000,
        DiscountPercent: 40,
        TotalQuantity: 100,
        SoldQuantity: 70,
        AvailableQuantity: 30,
        Status: 0,
      },
    ],
  },

  // Deal 2: Trà Sữa KOI Thé
  {
    id: "a2222222-2222-3333-4444-555555555555",
    shopId: "b0000000-0000-0000-0000-000000000002",
    shopName: "Trà Sữa KOI Thé",
    shopAddress: "30 Tháng 4, Xuân Khánh, Ninh Kiều, Cần Thơ",
    shopLogoUrl: "https://images.unsplash.com/photo-1556881286-fc6915169721?w=300",
    productId: "b2a2c3d4-e5f6-4a7b-8c9d-0e1f2a3b4c5d",
    productName: "Trà Sữa Trân Châu Hoàng Kim",
    productImageUrl: "https://images.unsplash.com/photo-1558857563-b37cf5a9143a?w=800",
    productImageUrls: [
      "https://images.unsplash.com/photo-1558857563-b37cf5a9143a?w=800",
      "https://images.unsplash.com/photo-1541696432-82c6da8ce7bf?w=800",
      "https://images.unsplash.com/photo-1556881286-fc6915169721?w=800",
    ],
    categoryName: "Đồ uống",
    description: "Trà đen Ceylon thơm đậm kết hợp sữa tươi nguyên kem thanh mát, trân châu hoàng kim dẻo mềm óng ánh.",
    minDealPrice: 25000,
    maxOriginalPrice: 60000,
    maxDiscountPercent: 44,
    variants: [
      {
        Id: "55555555-1111-1111-1111-111111111111",
        VariantId: "aaaa5555-1111-1111-1111-aaaaaaaaaaaa",
        Sku: "KOI-M-50D",
        VariantName: "Size M - 50% Đường",
        ImageUrl: "https://images.unsplash.com/photo-1558857563-b37cf5a9143a?w=800",
        Attributes: { Size: "Size M", "Độ ngọt": "50% Đường" },
        OriginalPrice: 45000,
        DealPrice: 25000,
        DiscountPercent: 44,
        TotalQuantity: 50,
        SoldQuantity: 38,
        AvailableQuantity: 12,
        Status: 0,
      },
      {
        Id: "55555555-2222-2222-2222-222222222222",
        VariantId: "aaaa5555-2222-2222-2222-aaaaaaaaaaaa",
        Sku: "KOI-M-100D",
        VariantName: "Size M - 100% Đường chuẩn",
        ImageUrl: "https://images.unsplash.com/photo-1558857563-b37cf5a9143a?w=800",
        Attributes: { Size: "Size M", "Độ ngọt": "100% Đường chuẩn" },
        OriginalPrice: 45000,
        DealPrice: 25000,
        DiscountPercent: 44,
        TotalQuantity: 50,
        SoldQuantity: 40,
        AvailableQuantity: 10,
        Status: 0,
      },
      {
        Id: "55555555-3333-3333-3333-333333333333",
        VariantId: "aaaa5555-3333-3333-3333-aaaaaaaaaaaa",
        Sku: "KOI-L-50D",
        VariantName: "Size L (Khổng lồ) - 50% Đường",
        ImageUrl: "https://images.unsplash.com/photo-1541696432-82c6da8ce7bf?w=800",
        Attributes: { Size: "Size L (Khổng lồ)", "Độ ngọt": "50% Đường" },
        OriginalPrice: 60000,
        DealPrice: 35000,
        DiscountPercent: 41,
        TotalQuantity: 60,
        SoldQuantity: 45,
        AvailableQuantity: 15,
        Status: 0,
      },
      {
        Id: "55555555-4444-4444-4444-444444444444",
        VariantId: "aaaa5555-4444-4444-4444-aaaaaaaaaaaa",
        Sku: "KOI-L-100D",
        VariantName: "Size L (Khổng lồ) - 100% Đường chuẩn",
        ImageUrl: "https://images.unsplash.com/photo-1541696432-82c6da8ce7bf?w=800",
        Attributes: { Size: "Size L (Khổng lồ)", "Độ ngọt": "100% Đường chuẩn" },
        OriginalPrice: 60000,
        DealPrice: 35000,
        DiscountPercent: 41,
        TotalQuantity: 60,
        SoldQuantity: 48,
        AvailableQuantity: 12,
        Status: 0,
      },
    ],
  },

  // Deal 3: Cơm Tấm Ba Ghiền
  {
    id: "a3333333-2222-3333-4444-555555555555",
    shopId: "b0000000-0000-0000-0000-000000000003",
    shopName: "Cơm Tấm Ba Ghiền",
    shopAddress: "Đường 3/2, Xuân Khánh, Ninh Kiều, Cần Thơ",
    shopLogoUrl: "https://images.unsplash.com/photo-1544025162-d76694265947?w=300",
    productId: "b3a2c3d4-e5f6-4a7b-8c9d-0e1f2a3b4c5d",
    productName: "Cơm Tấm Sườn Bì Chả Đặc Biệt",
    productImageUrl: "https://images.unsplash.com/photo-1544025162-d76694265947?w=800",
    productImageUrls: [
      "https://images.unsplash.com/photo-1544025162-d76694265947?w=800",
      "https://images.unsplash.com/photo-1563245372-f21724e3856d?w=800",
    ],
    categoryName: "Cơm",
    description: "Sườn cốt lết dày nướng than thơm lừng mọng nước, kèm bì giòn dai và chả trứng đúc mềm béo.",
    minDealPrice: 39000,
    maxOriginalPrice: 65000,
    maxDiscountPercent: 40,
    variants: [
      {
        Id: "66666666-1111-1111-1111-111111111111",
        VariantId: "aaaa6666-1111-1111-1111-aaaaaaaaaaaa",
        Sku: "COM-TAM-DB",
        VariantName: "Suất Sườn Bì Chả Đặc Biệt",
        ImageUrl: "https://images.unsplash.com/photo-1544025162-d76694265947?w=800",
        Attributes: { "Phần ăn": "Đặc biệt" },
        OriginalPrice: 65000,
        DealPrice: 39000,
        DiscountPercent: 40,
        TotalQuantity: 80,
        SoldQuantity: 62,
        AvailableQuantity: 18,
        Status: 0,
      },
    ],
  },

  // Deal 4: Gà Rán Jollibee
  {
    id: "a4444444-2222-3333-4444-555555555555",
    shopId: "b0000000-0000-0000-0000-000000000004",
    shopName: "Gà Rán Jollibee",
    shopAddress: "Mậu Thân, An Hòa, Ninh Kiều, Cần Thơ",
    shopLogoUrl: "https://images.unsplash.com/photo-1626082927389-6cd097cdc6ec?w=300",
    productId: "b4a2c3d4-e5f6-4a7b-8c9d-0e1f2a3b4c5d",
    productName: "Combo Gà Giòn Sốt Cay Vui Vẻ",
    productImageUrl: "https://images.unsplash.com/photo-1626082927389-6cd097cdc6ec?w=800",
    productImageUrls: [
      "https://images.unsplash.com/photo-1626082927389-6cd097cdc6ec?w=800",
      "https://images.unsplash.com/photo-1562967914-608f82629710?w=800",
    ],
    categoryName: "Đồ chiên",
    description: "2 miếng gà giòn rụm sốt cay ngọt đậm đà, khoai tây chiên vàng giòn và 1 ly nước ngọt tươi mát.",
    minDealPrice: 49000,
    maxOriginalPrice: 79000,
    maxDiscountPercent: 38,
    variants: [
      {
        Id: "77777777-1111-1111-1111-111111111111",
        VariantId: "aaaa7777-1111-1111-1111-aaaaaaaaaaaa",
        Sku: "JOL-GA-COMBO",
        VariantName: "Combo 2 Miếng Gà Giòn Sốt Cay",
        ImageUrl: "https://images.unsplash.com/photo-1626082927389-6cd097cdc6ec?w=800",
        Attributes: { "Combo": "2 Miếng Gà + Khoai + Nước" },
        OriginalPrice: 79000,
        DealPrice: 49000,
        DiscountPercent: 38,
        TotalQuantity: 100,
        SoldQuantity: 75,
        AvailableQuantity: 25,
        Status: 0,
      },
    ],
  },
];

function formatRESP(args) {
  let str = `*${args.length}\r\n`;
  for (const arg of args) {
    const s = String(arg);
    const byteLen = Buffer.byteLength(s, "utf8");
    str += `$${byteLen}\r\n${s}\r\n`;
  }
  return str;
}

const client = net.createConnection(REDIS_PORT, REDIS_HOST, () => {
  console.log(`Đang kết nối đến Redis tại ${REDIS_HOST}:${REDIS_PORT}...`);

  let buffer = "";
  // 1. AUTH
  buffer += formatRESP(["AUTH", REDIS_PASS]);

  // 2. GEOADD
  for (const shop of SHOPS) {
    buffer += formatRESP(["GEOADD", "savebite:geo:active-shops", String(shop.lon), String(shop.lat), shop.id]);
  }

  // 3. DEALS
  for (const deal of DEALS) {
    // 3.1 ZADD ShopDeals
    buffer += formatRESP(["ZADD", `savebite:shop:${deal.shopId}:deals`, String(orderEndScore), deal.id]);

    // 3.2 HSET Deal Detail
    const dealEntries = [
      "HSET",
      `savebite:deal:${deal.id}`,
      "id", deal.id,
      "shopId", deal.shopId,
      "name", deal.productName,
      "productName", deal.productName,
      "shopName", deal.shopName,
      "shopAddress", deal.shopAddress,
      "shopLogoUrl", deal.shopLogoUrl,
      "productId", deal.productId,
      "productImageUrl", deal.productImageUrl,
      "productImageUrls", JSON.stringify(deal.productImageUrls),
      "categoryName", deal.categoryName,
      "description", deal.description,
      "status", "OnSale",
      "saleStart", now.toISOString(),
      "orderEnd", orderEnd.toISOString(),
      "shopClosingTime", closingTime.toISOString(),
      "createdAt", now.toISOString(),
      "minDealPrice", String(deal.minDealPrice),
      "maxOriginalPrice", String(deal.maxOriginalPrice),
      "maxDiscountPercent", String(deal.maxDiscountPercent),
    ];
    buffer += formatRESP(dealEntries);
    buffer += formatRESP(["EXPIRE", `savebite:deal:${deal.id}`, String(30 * 86400)]);

    // 3.3 HSET Deal Variants
    const varEntries = ["HSET", `savebite:deal:${deal.id}:variants`];
    for (const v of deal.variants) {
      varEntries.push(v.VariantId, JSON.stringify(v));
    }
    buffer += formatRESP(varEntries);
    buffer += formatRESP(["EXPIRE", `savebite:deal:${deal.id}:variants`, String(30 * 86400)]);

    // 3.4 HSET Deal Stock
    const stockEntries = ["HSET", `savebite:deal:${deal.id}:stock`];
    for (const v of deal.variants) {
      stockEntries.push(v.VariantId, String(v.AvailableQuantity));
    }
    buffer += formatRESP(stockEntries);
    buffer += formatRESP(["EXPIRE", `savebite:deal:${deal.id}:stock`, String(30 * 86400)]);
  }

  client.write(buffer, () => {
    console.log("Đã gửi toàn bộ lệnh Seed vào Redis thành công!");
    setTimeout(() => {
      client.end();
      console.log("=======================================================");
      console.log("THÀNH CÔNG: Dữ liệu Redis đã được đồng bộ hoàn tất!");
      console.log("=======================================================");
      process.exit(0);
    }, 500);
  });
});

client.on("data", (data) => {
  // Nhận phản hồi từ Redis
});

client.on("error", (err) => {
  console.error("Lỗi kết nối Redis:", err.message);
  process.exit(1);
});
