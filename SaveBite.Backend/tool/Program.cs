using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Npgsql;
using StackExchange.Redis;

namespace SaveBite.RedisSync;

internal class Program
{
    public static async Task Main(string[] args)
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;

        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("==========================================================");
        Console.WriteLine("        SaveBite - Redis Flash Deal Sync Tool             ");
        Console.WriteLine("==========================================================");
        Console.ResetColor();

        // 1. Resolve configuration
        var projectDir = Directory.GetCurrentDirectory();
        var apiSettingsPath = Path.Combine(projectDir, "..", "SaveBite.Api", "appsettings.json");
        if (!File.Exists(apiSettingsPath))
        {
            apiSettingsPath = Path.Combine(projectDir, "SaveBite.Api", "appsettings.json");
        }

        var configBuilder = new ConfigurationBuilder();
        if (File.Exists(apiSettingsPath))
        {
            configBuilder.AddJsonFile(Path.GetFullPath(apiSettingsPath), optional: true);
        }
        var config = configBuilder.Build();

        var postgresConn = Environment.GetEnvironmentVariable("ConnectionStrings__Postgres")
            ?? config.GetConnectionString("Postgres")
            ?? "Host=localhost;Port=5432;Database=savebite;Username=savebite;Password=SaveBiTePasswOrd@@14";
        var redisConn = Environment.GetEnvironmentVariable("ConnectionStrings__Redis")
            ?? config.GetConnectionString("Redis")
            ?? "localhost:6379,password=SaveBiTePasswOrd@@14";

        // Parse CLI overrides if provided
        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == "--postgres" && i + 1 < args.Length) postgresConn = args[++i];
            if (args[i] == "--redis" && i + 1 < args.Length) redisConn = args[++i];
        }

        Console.WriteLine($"[INFO] PostgreSQL: Host=localhost, Database=savebite");
        Console.WriteLine($"[INFO] Redis:      {redisConn.Split(',')[0]}");

        // 2. Connect to Redis
        IConnectionMultiplexer redis;
        try
        {
            var redisOptions = ConfigurationOptions.Parse(redisConn);
            redisOptions.AbortOnConnectFail = false;
            redisOptions.ConnectTimeout = 5000;
            redis = await ConnectionMultiplexer.ConnectAsync(redisOptions);
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("[OK] Connected to Redis successfully.");
            Console.ResetColor();
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"[ERROR] Failed to connect to Redis: {ex.Message}");
            Console.ResetColor();
            return;
        }

        var redisDb = redis.GetDatabase();

        // 3. Connect to PostgreSQL and load flash deals
        try
        {
            await using var conn = new NpgsqlConnection(postgresConn);
            await conn.OpenAsync();
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("[OK] Connected to PostgreSQL successfully.");
            Console.ResetColor();

            // Check if flash_deals table exists
            const string checkTableSql = @"
                SELECT EXISTS (
                    SELECT 1 
                    FROM information_schema.tables 
                    WHERE table_schema = 'public' 
                    AND table_name = 'flash_deals'
                );
            ";
            await using (var checkCmd = new NpgsqlCommand(checkTableSql, conn))
            {
                var exists = (bool)(await checkCmd.ExecuteScalarAsync() ?? false);
                if (!exists)
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("\n[CẢNH BÁO] Database PostgreSQL hiện tại chưa có bảng 'flash_deals'!");
                    Console.WriteLine("👉 NGUYÊN NHÂN: Database vừa được reset hoặc chưa được chạy Migration.");
                    Console.WriteLine("👉 CÁCH XỬ LÝ:");
                    Console.WriteLine("   1. Mở terminal và chạy API chính trước:");
                    Console.WriteLine("      cd \"../SaveBite.Api\"");
                    Console.WriteLine("      dotnet run");
                    Console.WriteLine("      (SaveBite.Api sẽ tự động chạy Migrate & Seed dữ liệu vào Postgres)");
                    Console.WriteLine("   2. Sau khi SaveBite.Api khởi động xong, chạy lại tool này để nạp vào Redis.");
                    Console.ResetColor();
                    return;
                }
            }

            // Query active deals
            const string dealsSql = @"
                SELECT 
                    fd.id AS deal_id,
                    fd.shop_id,
                    fd.product_id,
                    fd.sale_start_time,
                    fd.order_end_time,
                    fd.shop_closing_time,
                    fd.status AS deal_status,
                    fd.created_at AS deal_created_at,
                    s.name AS shop_name,
                    s.address_line AS shop_address,
                    s.logo_url AS shop_logo,
                    s.latitude,
                    s.longitude,
                    s.status AS shop_status,
                    p.name AS product_name,
                    p.description AS product_description,
                    p.status AS product_status,
                    c.name AS category_name
                FROM public.flash_deals fd
                JOIN public.shops s ON fd.shop_id = s.id
                JOIN public.""Products"" p ON fd.product_id = p.id
                LEFT JOIN public.categories c ON p.category_id = c.id
                WHERE fd.status = 'OnSale' AND s.status = 'Active' AND p.status = 'Active'
                ORDER BY s.name, p.name;
            ";

            var deals = new List<DealData>();
            await using (var cmd = new NpgsqlCommand(dealsSql, conn))
            await using (var reader = await cmd.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    deals.Add(new DealData
                    {
                        DealId = reader.GetGuid(reader.GetOrdinal("deal_id")),
                        ShopId = reader.GetGuid(reader.GetOrdinal("shop_id")),
                        ProductId = reader.GetGuid(reader.GetOrdinal("product_id")),
                        SaleStartTime = reader.GetDateTime(reader.GetOrdinal("sale_start_time")),
                        OrderEndTime = reader.GetDateTime(reader.GetOrdinal("order_end_time")),
                        ShopClosingTime = reader.GetDateTime(reader.GetOrdinal("shop_closing_time")),
                        DealStatus = reader.GetString(reader.GetOrdinal("deal_status")),
                        CreatedAt = reader.GetDateTime(reader.GetOrdinal("deal_created_at")),
                        ShopName = reader.GetString(reader.GetOrdinal("shop_name")),
                        ShopAddress = reader.GetString(reader.GetOrdinal("shop_address")),
                        ShopLogoUrl = reader.IsDBNull(reader.GetOrdinal("shop_logo")) ? null : reader.GetString(reader.GetOrdinal("shop_logo")),
                        Latitude = reader.GetDouble(reader.GetOrdinal("latitude")),
                        Longitude = reader.GetDouble(reader.GetOrdinal("longitude")),
                        ProductName = reader.GetString(reader.GetOrdinal("product_name")),
                        ProductDescription = reader.IsDBNull(reader.GetOrdinal("product_description")) ? null : reader.GetString(reader.GetOrdinal("product_description")),
                        CategoryName = reader.IsDBNull(reader.GetOrdinal("category_name")) ? null : reader.GetString(reader.GetOrdinal("category_name"))
                    });
                }
            }

            if (deals.Count == 0)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("[WARN] Không tìm thấy Flash Deal nào đang mở bán (OnSale) trong PostgreSQL.");
                Console.ResetColor();
                return;
            }

            Console.WriteLine($"[INFO] Tìm thấy {deals.Count} flash deals đang hoạt động trong PostgreSQL.");

            // Query product images
            var imagesByProduct = new Dictionary<Guid, List<string>>();
            const string imagesSql = @"
                SELECT product_id, image_url, sort_order
                FROM public.product_images
                ORDER BY product_id, sort_order;
            ";
            await using (var cmd = new NpgsqlCommand(imagesSql, conn))
            await using (var reader = await cmd.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    var pId = reader.GetGuid(reader.GetOrdinal("product_id"));
                    var url = reader.GetString(reader.GetOrdinal("image_url"));
                    if (!imagesByProduct.TryGetValue(pId, out var list))
                    {
                        list = new List<string>();
                        imagesByProduct[pId] = list;
                    }
                    if (!string.IsNullOrEmpty(url) && !list.Contains(url))
                    {
                        list.Add(url);
                    }
                }
            }

            // Query variant attributes
            var variantAttrs = new Dictionary<Guid, Dictionary<string, string>>();
            var variantImgs = new Dictionary<Guid, string>();
            const string variantAttrsSql = @"
                SELECT 
                    pvv.variant_id,
                    pa.name AS attr_name,
                    pav.value AS attr_val,
                    pav.image_url AS attr_img
                FROM public.product_variant_values pvv
                JOIN public.product_attribute_values pav ON pvv.attribute_value_id = pav.id
                JOIN public.product_attributes pa ON pav.attribute_id = pa.id;
            ";
            await using (var cmd = new NpgsqlCommand(variantAttrsSql, conn))
            await using (var reader = await cmd.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    var vId = reader.GetGuid(reader.GetOrdinal("variant_id"));
                    var aName = reader.GetString(reader.GetOrdinal("attr_name"));
                    var aVal = reader.GetString(reader.GetOrdinal("attr_val"));
                    var aImg = reader.IsDBNull(reader.GetOrdinal("attr_img")) ? null : reader.GetString(reader.GetOrdinal("attr_img"));

                    if (!variantAttrs.TryGetValue(vId, out var dict))
                    {
                        dict = new Dictionary<string, string>();
                        variantAttrs[vId] = dict;
                    }
                    dict[aName] = aVal;

                    if (!string.IsNullOrEmpty(aImg) && !variantImgs.ContainsKey(vId))
                    {
                        variantImgs[vId] = aImg;
                    }
                }
            }

            // Query flash deal variants
            var variantsByDeal = new Dictionary<Guid, List<VariantData>>();
            const string variantsSql = @"
                SELECT 
                    fdv.id AS deal_variant_id,
                    fdv.flash_deal_id,
                    fdv.variant_id,
                    fdv.original_price,
                    fdv.deal_price,
                    fdv.discount_percent,
                    fdv.total_quantity,
                    fdv.reserved_quantity,
                    fdv.sold_quantity,
                    fdv.status AS deal_variant_status,
                    pv.sku,
                    pv.is_active
                FROM public.flash_deal_variants fdv
                JOIN public.product_variants pv ON fdv.variant_id = pv.id
                WHERE fdv.status = 'Active';
            ";
            await using (var cmd = new NpgsqlCommand(variantsSql, conn))
            await using (var reader = await cmd.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    var dealId = reader.GetGuid(reader.GetOrdinal("flash_deal_id"));
                    var vId = reader.GetGuid(reader.GetOrdinal("variant_id"));
                    var sku = reader.IsDBNull(reader.GetOrdinal("sku")) ? null : reader.GetString(reader.GetOrdinal("sku"));
                    var origPrice = reader.GetDecimal(reader.GetOrdinal("original_price"));
                    var dealPrice = reader.GetDecimal(reader.GetOrdinal("deal_price"));
                    var disc = reader.GetDecimal(reader.GetOrdinal("discount_percent"));
                    var totalQty = reader.GetInt32(reader.GetOrdinal("total_quantity"));
                    var resQty = reader.GetInt32(reader.GetOrdinal("reserved_quantity"));
                    var soldQty = reader.GetInt32(reader.GetOrdinal("sold_quantity"));
                    var isActive = reader.GetBoolean(reader.GetOrdinal("is_active"));

                    if (!variantsByDeal.TryGetValue(dealId, out var list))
                    {
                        list = new List<VariantData>();
                        variantsByDeal[dealId] = list;
                    }

                    list.Add(new VariantData
                    {
                        Id = reader.GetGuid(reader.GetOrdinal("deal_variant_id")),
                        VariantId = vId,
                        Sku = sku,
                        OriginalPrice = origPrice,
                        DealPrice = dealPrice,
                        DiscountPercent = disc,
                        TotalQuantity = totalQty,
                        ReservedQuantity = resQty,
                        SoldQuantity = soldQty,
                        IsActive = isActive
                    });
                }
            }

            // 4. Populate Redis
            var uniqueShops = new HashSet<Guid>();
            var jsonOptions = new JsonSerializerOptions { PropertyNamingPolicy = null };

            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("--- Inserting Flash Deals into Redis ---");
            Console.ResetColor();

            foreach (var deal in deals)
            {
                var dealExpiry = deal.OrderEndTime > DateTime.UtcNow
                    ? (deal.OrderEndTime - DateTime.UtcNow + TimeSpan.FromHours(2))
                    : TimeSpan.FromDays(30);

                if (dealExpiry < TimeSpan.FromHours(12)) dealExpiry = TimeSpan.FromDays(30);

                // Get images
                imagesByProduct.TryGetValue(deal.ProductId, out var productImgs);
                productImgs ??= new List<string>();
                var primaryImg = productImgs.FirstOrDefault() ?? string.Empty;

                // Process variants
                variantsByDeal.TryGetValue(deal.DealId, out var dealVariants);
                dealVariants ??= new List<VariantData>();

                var variantResponses = new List<FlashDealVariantResponse>();
                var minDealPrice = dealVariants.Count > 0 ? dealVariants.Min(v => v.DealPrice) : 0m;
                var maxOriginalPrice = dealVariants.Count > 0 ? dealVariants.Max(v => v.OriginalPrice) : 0m;
                var maxDiscountPercent = dealVariants.Count > 0 ? dealVariants.Max(v => v.DiscountPercent) : 0m;

                foreach (var v in dealVariants)
                {
                    variantAttrs.TryGetValue(v.VariantId, out var attrs);
                    attrs ??= new Dictionary<string, string>();

                    variantImgs.TryGetValue(v.VariantId, out var vImg);

                    var vName = attrs.Count > 0
                        ? string.Join(" - ", attrs.Values)
                        : (v.Sku ?? "Default");

                    var availableQty = Math.Max(0, v.TotalQuantity - v.ReservedQuantity - v.SoldQuantity);
                    var isPurchasable = availableQty > 0 && v.IsActive;

                    variantResponses.Add(new FlashDealVariantResponse
                    {
                        Id = v.Id,
                        VariantId = v.VariantId,
                        Sku = v.Sku,
                        VariantName = vName,
                        ImageUrl = vImg,
                        Attributes = attrs,
                        OriginalPrice = v.OriginalPrice,
                        DealPrice = v.DealPrice,
                        DiscountPercent = v.DiscountPercent,
                        TotalQuantity = v.TotalQuantity,
                        SoldQuantity = v.SoldQuantity,
                        ReservedQuantity = v.ReservedQuantity,
                        Status = 0, // Active
                        IsPurchasable = isPurchasable
                    });
                }

                // Write deal hash: savebite:deal:{dealId}
                var dealKey = $"savebite:deal:{deal.DealId}";
                var dealEntries = new HashEntry[]
                {
                    new("id", deal.DealId.ToString()),
                    new("shopId", deal.ShopId.ToString()),
                    new("name", deal.ProductName),
                    new("productName", deal.ProductName),
                    new("shopName", deal.ShopName),
                    new("shopAddress", deal.ShopAddress),
                    new("shopLogoUrl", deal.ShopLogoUrl ?? string.Empty),
                    new("productId", deal.ProductId.ToString()),
                    new("productImageUrl", primaryImg),
                    new("productImageUrls", JsonSerializer.Serialize(productImgs)),
                    new("categoryName", deal.CategoryName ?? string.Empty),
                    new("description", deal.ProductDescription ?? string.Empty),
                    new("status", deal.DealStatus),
                    new("saleStart", deal.SaleStartTime.ToString("o")),
                    new("orderEnd", deal.OrderEndTime.ToString("o")),
                    new("shopClosingTime", deal.ShopClosingTime.ToString("o")),
                    new("createdAt", deal.CreatedAt.ToString("o")),
                    new("minDealPrice", minDealPrice.ToString()),
                    new("maxOriginalPrice", maxOriginalPrice.ToString()),
                    new("maxDiscountPercent", maxDiscountPercent.ToString())
                };

                await redisDb.HashSetAsync(dealKey, dealEntries);
                await redisDb.KeyExpireAsync(dealKey, dealExpiry);

                // Write variants hash: savebite:deal:{dealId}:variants
                var variantsKey = $"savebite:deal:{deal.DealId}:variants";
                if (variantResponses.Count > 0)
                {
                    var variantEntries = variantResponses.Select(vr => new HashEntry(
                        vr.VariantId.ToString(),
                        JsonSerializer.Serialize(vr, jsonOptions)
                    )).ToArray();
                    await redisDb.HashSetAsync(variantsKey, variantEntries);
                    await redisDb.KeyExpireAsync(variantsKey, dealExpiry);
                }

                // Write stock hash: savebite:deal:{dealId}:stock
                var stockKey = $"savebite:deal:{deal.DealId}:stock";
                if (variantResponses.Count > 0)
                {
                    var stockEntries = variantResponses.Select(vr => new HashEntry(
                        vr.VariantId.ToString(),
                        vr.AvailableQuantity
                    )).ToArray();
                    await redisDb.HashSetAsync(stockKey, stockEntries);
                    await redisDb.KeyExpireAsync(stockKey, dealExpiry);
                }

                // Write shop deals sorted set: savebite:shop:{shopId}:deals
                var shopDealsKey = $"savebite:shop:{deal.ShopId}:deals";
                var utcOrderEnd = deal.OrderEndTime.Kind == DateTimeKind.Utc
                    ? deal.OrderEndTime
                    : DateTime.SpecifyKind(deal.OrderEndTime, DateTimeKind.Utc);
                var orderEndScore = new DateTimeOffset(utcOrderEnd).ToUnixTimeSeconds();

                await redisDb.SortedSetAddAsync(shopDealsKey, deal.DealId.ToString(), orderEndScore);
                await redisDb.KeyExpireAsync(shopDealsKey, dealExpiry);

                // Add shop geo coordinate: savebite:geo:active-shops
                await redisDb.GeoAddAsync("savebite:geo:active-shops", deal.Longitude, deal.Latitude, deal.ShopId.ToString());

                uniqueShops.Add(deal.ShopId);
                Console.WriteLine($" -> Synced: [{deal.ShopName}] \"{deal.ProductName}\" ({minDealPrice:N0}đ) - OrderEnd: {deal.OrderEndTime:yyyy-MM-dd HH:mm}");
            }

            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("==========================================================");
            Console.WriteLine($" [SUCCESS] Synced {deals.Count} flash deals across {uniqueShops.Count} shops into Redis!");
            Console.WriteLine("==========================================================");
            Console.ResetColor();
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"[ERROR] {ex.Message}\n{ex.StackTrace}");
            Console.ResetColor();
        }
    }
}

public class DealData
{
    public Guid DealId { get; set; }
    public Guid ShopId { get; set; }
    public Guid ProductId { get; set; }
    public DateTime SaleStartTime { get; set; }
    public DateTime OrderEndTime { get; set; }
    public DateTime ShopClosingTime { get; set; }
    public string DealStatus { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string ShopName { get; set; } = string.Empty;
    public string ShopAddress { get; set; } = string.Empty;
    public string? ShopLogoUrl { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string? ProductDescription { get; set; }
    public string? CategoryName { get; set; }
}

public class VariantData
{
    public Guid Id { get; set; }
    public Guid VariantId { get; set; }
    public string? Sku { get; set; }
    public decimal OriginalPrice { get; set; }
    public decimal DealPrice { get; set; }
    public decimal DiscountPercent { get; set; }
    public int TotalQuantity { get; set; }
    public int ReservedQuantity { get; set; }
    public int SoldQuantity { get; set; }
    public bool IsActive { get; set; }
}

public class FlashDealVariantResponse
{
    public Guid Id { get; set; }
    public Guid VariantId { get; set; }
    public string? Sku { get; set; }
    public string? VariantName { get; set; }
    public string? ImageUrl { get; set; }
    public Dictionary<string, string> Attributes { get; set; } = new();
    public decimal OriginalPrice { get; set; }
    public decimal DealPrice { get; set; }
    public decimal DiscountPercent { get; set; }
    public int TotalQuantity { get; set; }
    public int SoldQuantity { get; set; }
    public int ReservedQuantity { get; set; }
    public int AvailableQuantity => Math.Max(0, TotalQuantity - ReservedQuantity - SoldQuantity);
    public int Status { get; set; }
    public bool IsPurchasable { get; set; }
}
