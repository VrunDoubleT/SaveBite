using Microsoft.EntityFrameworkCore;
using SaveBite.Backend.Models.Entities;
using SaveBite.Backend.Models.Enums;

namespace SaveBite.Backend.Data.Seed;

public static class DbSeeder
{
    public const string DefaultPassword = "SaveBite@123";
    private const string MarkerEmail = "admin@gmail.com";

    public static async Task SeedAsync(AppDbContext context)
    {
        var hasBaseSeed = await context.Users.AnyAsync(x => x.Email == MarkerEmail);
        var hasExtendedSeed = await context.Set<AuditLog>()
            .AnyAsync(x => x.Action == "SEED_V2_COMPLETED");

        if (hasBaseSeed && hasExtendedSeed) return;

        var strategy = context.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await context.Database.BeginTransactionAsync();
            var now = DateTime.UtcNow;
            if (!hasBaseSeed)
            {
                var hash = BCrypt.Net.BCrypt.HashPassword(DefaultPassword);
                await SeedUsersAsync(context, now, hash);
                await SeedShopAsync(context, now);
                await SeedCatalogAsync(context, now);
                await SeedOrdersAsync(context, now);
                await SeedFinanceAsync(context, now);
                await SeedTrustAsync(context, now);
                await SeedFeedbackAsync(context, now);
            }

            if (!hasExtendedSeed)
            {
                await SeedRemainingDataAsync(context, now);
            }

            await transaction.CommitAsync();
        });
    }

    private static async Task SeedUsersAsync(AppDbContext db, DateTime now, string hash)
    {
        await db.AddRangeAsync(
            new User { Id = Id(1), Email = MarkerEmail, Phone = "0900000001", PasswordHash = hash, FullName = "SaveBite Admin", EmailVerifiedAt = now.AddMonths(-6), Status = UserStatus.Active, CustomerStatus = CustomerStatus.Active, ShopStatus = ShopAccessStatus.Active, Role = UserRole.Admin, CreatedAt = now.AddMonths(-6), UpdatedAt = now },
            new User { Id = Id(2), Email = "owner@gmail.com", Phone = "0900000002", PasswordHash = hash, FullName = "Nguyen Minh Anh", EmailVerifiedAt = now.AddMonths(-5), Status = UserStatus.Active, CustomerStatus = CustomerStatus.Active, ShopStatus = ShopAccessStatus.Active, Role = UserRole.StoreOwner, CreatedAt = now.AddMonths(-5), UpdatedAt = now },
            new User { Id = Id(3), Email = "customer.locked@gmail.com", Phone = "0900000003", PasswordHash = hash, FullName = "Tran Hoang Nam", EmailVerifiedAt = now.AddMonths(-4), Status = UserStatus.Active, CustomerStatus = CustomerStatus.Suspended, ShopStatus = ShopAccessStatus.Active, Role = UserRole.Staff, CreatedAt = now.AddMonths(-4), UpdatedAt = now },
            new User { Id = Id(4), Email = "shop.locked@gmail.com", Phone = "0900000004", PasswordHash = hash, FullName = "Le Thu Ha", EmailVerifiedAt = now.AddMonths(-3), Status = UserStatus.Active, CustomerStatus = CustomerStatus.Active, ShopStatus = ShopAccessStatus.Suspended, Role = UserRole.StoreOwner, CreatedAt = now.AddMonths(-3), UpdatedAt = now });
        await db.SaveChangesAsync();

        await db.AddRangeAsync(
            new UserAddress { Id = Id(5), UserId = Id(2), Label = "Nha rieng", AddressLine = "123 Nguyen Van Cu", Ward = "Phuong 4", District = "Quan 5", City = "Ho Chi Minh", Latitude = 10.762622, Longitude = 106.660172, IsDefault = true, CreatedAt = now.AddMonths(-5), UpdatedAt = now },
            new UserAddress { Id = Id(6), UserId = Id(3), Label = "Cong ty", AddressLine = "456 Le Van Sy", Ward = "Phuong 14", District = "Quan 3", City = "Ho Chi Minh", Latitude = 10.786730, Longitude = 106.674500, IsDefault = true, CreatedAt = now.AddMonths(-4), UpdatedAt = now },
            new UserAddress { Id = Id(7), UserId = Id(4), Label = "Nha rieng", AddressLine = "789 Dien Bien Phu", Ward = "Phuong 15", District = "Quan Binh Thanh", City = "Ho Chi Minh", Latitude = 10.801550, Longitude = 106.714700, IsDefault = true, CreatedAt = now.AddMonths(-3), UpdatedAt = now },
            new AccountStatusLog { Id = Id(8), UserId = Id(3), AdminId = Id(1), Action = UserStatus.Suspended, Reason = "Khoa quyen customer do nhieu lan khong den nhan mon.", CreatedAt = now.AddDays(-7) },
            new AccountStatusLog { Id = Id(9), UserId = Id(4), AdminId = Id(1), Action = UserStatus.Suspended, Reason = "Khoa quyen shop trong khi cho bo sung ho so.", CreatedAt = now.AddDays(-5) },
            new UserRoleChangeLog { Id = Id(10), UserId = Id(2), PreviousRole = UserRole.User, NewRole = UserRole.StoreOwner, Reason = "Don dang ky shop da duoc phe duyet.", CreatedAt = now.AddMonths(-2) },
            new RefreshToken { Id = Id(11), UserId = Id(2), TokenHash = "seed-only-refresh-token-hash", ExpiresAt = now.AddDays(30), CreatedAt = now },
            new AuditLog { Id = Id(12), ActorUserId = Id(1), ActorType = AuditActorType.User, Action = "SUSPEND_CUSTOMER_ACCESS", TargetType = "User", TargetId = Id(3), OldValuesJson = "{\"customerStatus\":\"Active\"}", NewValuesJson = "{\"customerStatus\":\"Suspended\"}", Reason = "Vi pham chinh sach nhan hang.", CorrelationId = Id(13), CreatedAt = now.AddDays(-7) });
        await db.SaveChangesAsync();
    }

    private static async Task SeedShopAsync(AppDbContext db, DateTime now)
    {
        await db.AddRangeAsync(
            new Category { Id = Id(20), Name = "Do an che bien san", Description = "Mon an trong ngay ban gia uu dai de giam lang phi.", ImageUrl = "https://picsum.photos/seed/savebite-category/600/400", IsActive = true, CreatedAt = now.AddMonths(-6), UpdatedAt = now },
            new ShopApplication { Id = Id(21), ApplicantUserId = Id(2), Name = "Bep Xanh Cuoi Ngay", Description = "Cua hang ban cac suat an con moi vao cuoi ngay.", BusinessLicenseNo = "SB-SEED-0001", AddressLine = "88 Nguyen Thi Minh Khai", Ward = "Phuong Vo Thi Sau", District = "Quan 3", City = "Ho Chi Minh", Latitude = 10.776889, Longitude = 106.691139, LogoUrl = "https://picsum.photos/seed/shop-logo/400", CoverImageUrl = "https://picsum.photos/seed/shop-cover/1200/400", OpeningTime = new TimeOnly(8, 0), ClosingTime = new TimeOnly(21, 30), Status = ShopApplicationStatus.Approved, BankName = "Vietcombank", BankAccountNumber = "0123456789", BankAccountHolder = "NGUYEN MINH ANH", PayosClientId = "seed-client", PayosApiKey = "seed-api-key", PayosChecksumKey = "seed-checksum-key", RevisionNumber = 1, CreatedAt = now.AddMonths(-3), UpdatedAt = now.AddMonths(-2) });
        await db.SaveChangesAsync();

        await db.AddRangeAsync(
            new ShopApplicationDocument { Id = Id(23), ApplicationId = Id(21), DocumentType = ShopDocumentType.BusinessLicense, FileUrl = "https://example.local/seed/business-license.pdf", OriginalFileName = "giay-phep-kinh-doanh.pdf", ContentType = "application/pdf", RevisionNumber = 1, UploadedBy = Id(2), UploadedAt = now.AddMonths(-3), IsCurrent = true },
            new ShopApplicationReviewLog { Id = Id(24), ApplicationId = Id(21), AdminId = Id(1), FromStatus = ShopApplicationStatus.Pending, ToStatus = ShopApplicationStatus.Approved, RevisionNumber = 1, Note = "Ho so hop le.", CreatedAt = now.AddMonths(-2) },
            new Shop { Id = Id(22), OwnerUserId = Id(2), ApplicationId = Id(21), Name = "Bep Xanh Cuoi Ngay", Description = "Mon ngon moi moi ngay, gia tot sau 18 gio.", AddressLine = "88 Nguyen Thi Minh Khai", Ward = "Phuong Vo Thi Sau", District = "Quan 3", City = "Ho Chi Minh", Latitude = 10.776889, Longitude = 106.691139, LogoUrl = "https://picsum.photos/seed/shop-logo/400", CoverImageUrl = "https://picsum.photos/seed/shop-cover/1200/400", OpeningTime = new TimeOnly(8, 0), ClosingTime = new TimeOnly(21, 30), Status = ShopStatus.Active, CreatedAt = now.AddMonths(-2), UpdatedAt = now });
        await db.SaveChangesAsync();

        await db.AddRangeAsync(
            new ShopPaymentConfig { Id = 1, ShopId = Id(22), BankName = "Vietcombank", BankAccountNumber = "0123456789", BankAccountHolder = "NGUYEN MINH ANH", PayosClientId = "seed-client", PayosApiKey = "seed-api-key", PayosChecksumKey = "seed-checksum-key", CreatedAt = now.AddMonths(-2), UpdatedAt = now },
            new SaveBite.Backend.Models.Entities.StaffInvitation { Id = Id(25), ShopId = Id(22), InvitedUserId = Id(3), InvitedBy = Id(2), Status = "Accepted", InvitedAt = now.AddMonths(-1).AddDays(-2), RespondedAt = now.AddMonths(-1) },
            new ShopStaff { Id = Id(26), ShopId = Id(22), UserId = Id(3), DisplayName = "Nam - Nhan vien ca toi", Nickname = "Nam", Status = ShopStaffStatus.Active, JoinedAt = now.AddMonths(-1) });
        await db.SaveChangesAsync();
    }

    private static async Task SeedCatalogAsync(AppDbContext db, DateTime now)
    {
        await db.AddRangeAsync(
            new Product { Id = Id(30), ShopId = Id(22), CategoryId = Id(20), Name = "Com ga nuong mat ong", Description = "Com ga trong ngay kem rau va nuoc sot.", Status = ProductStatus.Active, CreatedAt = now.AddDays(-45), UpdatedAt = now },
            new Product { Id = Id(31), ShopId = Id(22), CategoryId = Id(20), Name = "Banh mi thit nuong", Description = "Banh mi gion, thit nuong va rau dua tuoi.", Status = ProductStatus.Active, CreatedAt = now.AddDays(-30), UpdatedAt = now },
            new Product { Id = Id(35), ShopId = Id(22), CategoryId = Id(20), Name = "Mi xao bo", Description = "Mi xao bo voi rau cai va nuoc sot dam da.", Status = ProductStatus.Active, CreatedAt = now.AddDays(-40), UpdatedAt = now },
            new Product { Id = Id(36), ShopId = Id(22), CategoryId = Id(20), Name = "Com suon nuong", Description = "Com trang kem suon nuong mem va rau tuoi.", Status = ProductStatus.Active, CreatedAt = now.AddDays(-38), UpdatedAt = now },
            new Product { Id = Id(37), ShopId = Id(22), CategoryId = Id(20), Name = "Bun thit nuong", Description = "Bun tuoi, thit nuong, rau song va nuoc mam chua ngot.", Status = ProductStatus.Active, CreatedAt = now.AddDays(-36), UpdatedAt = now },
            new Product { Id = Id(38), ShopId = Id(22), CategoryId = Id(20), Name = "Com bo luc lac", Description = "Com trang voi bo luc lac va rau cu tuoi.", Status = ProductStatus.Active, CreatedAt = now.AddDays(-34), UpdatedAt = now },
            new Product { Id = Id(39), ShopId = Id(22), CategoryId = Id(20), Name = "Ga ran gion", Description = "Ga ran vang gion, phuc vu kem tuong va rau an kem.", Status = ProductStatus.Active, CreatedAt = now.AddDays(-32), UpdatedAt = now },
            new Product { Id = Id(40), ShopId = Id(22), CategoryId = Id(20), Name = "Banh mi ga xe", Description = "Banh mi gion kem ga xe, rau dua va sot dac biet.", Status = ProductStatus.Active, CreatedAt = now.AddDays(-30), UpdatedAt = now },
            new Product { Id = Id(41), ShopId = Id(22), CategoryId = Id(20), Name = "Com ca chien", Description = "Com trang kem ca chien gion va nuoc mam chua ngot.", Status = ProductStatus.Active, CreatedAt = now.AddDays(-28), UpdatedAt = now },
            new Product { Id = Id(42), ShopId = Id(22), CategoryId = Id(20), Name = "Bun bo Hue", Description = "Bun bo Hue voi thit bo, cha cua va rau song.", Status = ProductStatus.Active, CreatedAt = now.AddDays(-26), UpdatedAt = now },
            new Product { Id = Id(43), ShopId = Id(22), CategoryId = Id(20), Name = "Com xao hai san", Description = "Com xao voi tom, muc va rau cu tuoi.", Status = ProductStatus.Active, CreatedAt = now.AddDays(-24), UpdatedAt = now },
            new Product { Id = Id(44), ShopId = Id(22), CategoryId = Id(20), Name = "Pizza hai san", Description = "Pizza de mem, pho mai va hai san tuoi.", Status = ProductStatus.Active, CreatedAt = now.AddDays(-22), UpdatedAt = now },
            new Product { Id = Id(45), ShopId = Id(22), CategoryId = Id(20), Name = "Mi y sot bo bam", Description = "Mi y ket hop sot bo bam dam da va pho mai.", Status = ProductStatus.Active, CreatedAt = now.AddDays(-20), UpdatedAt = now },
            new Product { Id = Id(46), ShopId = Id(22), CategoryId = Id(20), Name = "Khoai tay chien", Description = "Khoai tay chien vang gion, dung kem tuong cham.", Status = ProductStatus.Active, CreatedAt = now.AddDays(-18), UpdatedAt = now },
            new Product { Id = Id(47), ShopId = Id(22), CategoryId = Id(20), Name = "Hamburger bo", Description = "Burger bo voi rau tuoi, pho mai va sot dac biet.", Status = ProductStatus.Active, CreatedAt = now.AddDays(-16), UpdatedAt = now },
            new Product { Id = Id(48), ShopId = Id(22), CategoryId = Id(20), Name = "Salad ga nuong", Description = "Salad rau tuoi ket hop ga nuong va sot me.", Status = ProductStatus.Active, CreatedAt = now.AddDays(-14), UpdatedAt = now },
            new Product { Id = Id(49), ShopId = Id(22), CategoryId = Id(20), Name = "Chao suon", Description = "Chao suon nong hoi, thom ngon va bo duong.", Status = ProductStatus.Active, CreatedAt = now.AddDays(-12), UpdatedAt = now },
            new Product { Id = Id(50), ShopId = Id(22), CategoryId = Id(20), Name = "Xoi ga", Description = "Xoi nep deo voi ga xe va hanh phi.", Status = ProductStatus.Active, CreatedAt = now.AddDays(-10), UpdatedAt = now },
            new Product { Id = Id(51), ShopId = Id(22), CategoryId = Id(20), Name = "Banh xeo", Description = "Banh xeo vang gion, nhan tom thit va gia.", Status = ProductStatus.Active, CreatedAt = now.AddDays(-8), UpdatedAt = now },
            new Product { Id = Id(52), ShopId = Id(22), CategoryId = Id(20), Name = "Com chien trung", Description = "Com chien voi trung, lap xuong va rau cu.", Status = ProductStatus.Active, CreatedAt = now.AddDays(-6), UpdatedAt = now },
            new Product { Id = Id(53), ShopId = Id(22), CategoryId = Id(20), Name = "Banh ngot tong hop", Description = "Banh ngot trong ngay voi nhieu loai huong vi.", Status = ProductStatus.Active, CreatedAt = now.AddDays(-4), UpdatedAt = now },
            new Product { Id = Id(54), ShopId = Id(22), CategoryId = Id(20), Name = "Combo an toi", Description = "Combo gom mon chinh, mon phu va nuoc uong.", Status = ProductStatus.Active, CreatedAt = now.AddDays(-2), UpdatedAt = now });
        await db.SaveChangesAsync();

        await db.AddRangeAsync(
            new ProductImage { Id = Id(32), ProductId = Id(30), ImageUrl = "https://encrypted-tbn0.gstatic.com/images?q=tbn:ANd9GcT_KATaLV1ifbfGSePWWwXwhh9kxe1XkJxeYMl0wkJ-Kw&s=10" },
            new ProductImage { Id = Id(33), ProductId = Id(31), ImageUrl = "https://encrypted-tbn0.gstatic.com/images?q=tbn:ANd9GcTI0ZACCyTF302jvVAipe8XcUQFrkn3o37ij5T6tt09kg&s=10" },
            new ProductImage { Id = Id(55), ProductId = Id(35), ImageUrl = "https://encrypted-tbn0.gstatic.com/images?q=tbn:ANd9GcQUOKxKzHu4f7zaJUcIP0pynClh_OA3SWkr_FUsscI2Vg&s=10" },
            new ProductImage { Id = Id(56), ProductId = Id(36), ImageUrl = "https://encrypted-tbn0.gstatic.com/images?q=tbn:ANd9GcRYnz99YAoQpbxq1Fe8RGq9KWMUes18US53VBzJCb_5iw&s=10" },
            new ProductImage { Id = Id(57), ProductId = Id(37), ImageUrl = "https://encrypted-tbn0.gstatic.com/images?q=tbn:ANd9GcQkrHwPjwhpRWzmy4ziL4Uje6f055MK40gwdxPrBj0Evw&s=10" },
            new ProductImage { Id = Id(58), ProductId = Id(38), ImageUrl = "https://encrypted-tbn0.gstatic.com/images?q=tbn:ANd9GcQ_UxUXyJmbof_EJdreu9wQyYJ8MPih_Z7h_Shsab6Vow&s=10" },
            new ProductImage { Id = Id(59), ProductId = Id(39), ImageUrl = "https://encrypted-tbn0.gstatic.com/images?q=tbn:ANd9GcTtR4OYkBZ3aB0qylIwhIDFzsqEq0UY_rSw6_c64WyGlA&s=10" },
            new ProductImage { Id = Id(60), ProductId = Id(40), ImageUrl = "https://encrypted-tbn0.gstatic.com/images?q=tbn:ANd9GcSyiw-8zxRk6x2iH9ofS8-LeV9WP2oMeeCmlCg339NuKQ&s=10" },
            new ProductImage { Id = Id(61), ProductId = Id(41), ImageUrl = "https://encrypted-tbn0.gstatic.com/images?q=tbn:ANd9GcQgtRv1eeIhQPQTDfHGbwjnYU1W6ZzPh-bspCWw5Q33_g&s=10" },
            new ProductImage { Id = Id(62), ProductId = Id(42), ImageUrl = "https://encrypted-tbn0.gstatic.com/images?q=tbn:ANd9GcSh1-jItLgQc_iSESNE9JB9D9Hz8ArJNAGnh4jDibIePg&s=10" },
            new ProductImage { Id = Id(63), ProductId = Id(43), ImageUrl = "https://encrypted-tbn0.gstatic.com/images?q=tbn:ANd9GcR2ggoZMq4UNwemRF3iqQ8sUTNNRgDwq0wDr1TZ1alPrw&s=10" },
            new ProductImage { Id = Id(64), ProductId = Id(44), ImageUrl = "https://encrypted-tbn0.gstatic.com/images?q=tbn:ANd9GcSw9Cmi2otCSaKOZlG1WoFnKe5PAdnTe6v_jAlKsjjBCg&s=10" },
            new ProductImage { Id = Id(65), ProductId = Id(45), ImageUrl = "https://encrypted-tbn0.gstatic.com/images?q=tbn:ANd9GcTGc17ZV3VsOf3WQR6xShznXC1t6XxEhFOz-CBJKo_rbA&s=10" },
            new ProductImage { Id = Id(66), ProductId = Id(46), ImageUrl = "https://encrypted-tbn0.gstatic.com/images?q=tbn:ANd9GcRQ_OFXsKYmSk3tscV868X1JReuMFEn_8uKXv_uWZXLbA&s=10" },
            new ProductImage { Id = Id(67), ProductId = Id(47), ImageUrl = "https://encrypted-tbn0.gstatic.com/images?q=tbn:ANd9GcRpEWmOp7x1LT2MZt0g_qnTFVggsQSOSEYnRvqqIOMOgg&s=10" },
            new ProductImage { Id = Id(68), ProductId = Id(48), ImageUrl = "https://encrypted-tbn0.gstatic.com/images?q=tbn:ANd9GcSK5aVQEOn-zw7NKDAow-FpPMqjWscXa5Nx_dlvNlA9KQ&s=10" },
            new ProductImage { Id = Id(69), ProductId = Id(49), ImageUrl = "https://encrypted-tbn0.gstatic.com/images?q=tbn:ANd9GcQr6rblLGzi-PhC0S6uvhR5l050SbFiaaBusZgO4Yo_4Q&s=10" },
            new ProductImage { Id = Id(70), ProductId = Id(50), ImageUrl = "https://encrypted-tbn0.gstatic.com/images?q=tbn:ANd9GcSCQS1w6CP7XFv1qw5pFt5uKd1VDqYNgkfHq88wmXVNyQ&s=10" },
            new ProductImage { Id = Id(71), ProductId = Id(51), ImageUrl = "https://encrypted-tbn0.gstatic.com/images?q=tbn:ANd9GcSGRvwNJilzn42nj-19NGC-gcUj1Ti7yVNgkZR2wlWYJA&s=10" },
            new ProductImage { Id = Id(72), ProductId = Id(52), ImageUrl = "https://encrypted-tbn0.gstatic.com/images?q=tbn:ANd9GcTUHNGPl8UrqQLvBaJYSBb3oGAjgpWhDapj-4_Zx2bD-g&s=10" },
            new ProductImage { Id = Id(73), ProductId = Id(53), ImageUrl = "https://encrypted-tbn0.gstatic.com/images?q=tbn:ANd9GcSBMBZqvOJvs_cHwFpO28-CA_6_egavbZAadhWHKROVFQ&s=10" },
            new ProductImage { Id = Id(74), ProductId = Id(54), ImageUrl = "https://encrypted-tbn0.gstatic.com/images?q=tbn:ANd9GcSfI4sAK7xVSs0A-Pr6w9_e-CbKezi2Pvyew3ETxdI6aA&s=10" },

            new ProductAttribute { Id = Id(34), ProductId = Id(30), Name = "Kich co" },
            new ProductContentRevision { Id = Id(41), ProductId = Id(30), RevisionNumber = 1, SubmittedByUserId = Id(2), Name = "Com ga nuong mat ong", Description = "Com ga trong ngay kem rau va nuoc sot.", CategoryId = Id(20), ImagesJson = "[\"https://picsum.photos/seed/com-ga/800/600\"]", Status = ProductReviewStatus.Approved, ReviewedByUserId = Id(1), ReviewNote = "Noi dung hop le.", SubmittedAt = now.AddDays(-45), ReviewedAt = now.AddDays(-44) });
        await db.SaveChangesAsync();

        await db.AddRangeAsync(
            new ProductAttributeValue { Id = Id(35), AttributeId = Id(34), Value = "Tieu chuan" },
            new ProductAttributeValue { Id = Id(36), AttributeId = Id(34), Value = "Lon" },
            new ProductVariant { Id = Id(38), ProductId = Id(30), Sku = "COM-GA-TC", OriginalPrice = 60000m, DealPrice = 35000m, IsActive = true, CreatedAt = now.AddDays(-45) },
            new ProductVariant { Id = Id(39), ProductId = Id(31), Sku = "BANH-MI-TN", OriginalPrice = 35000m, DealPrice = 20000m, IsActive = true, CreatedAt = now.AddDays(-30) });
        await db.SaveChangesAsync();

        await db.AddRangeAsync(
            new ProductVariantValue { VariantId = Id(38), AttributeValueId = Id(35) },
            new FlashDeal { Id = Id(42), ShopId = Id(22), ProductId = Id(30), SaleStartTime = now.AddDays(-20), OrderEndTime = now.AddDays(20), ShopClosingTime = now.AddDays(20).AddHours(2), Status = FlashDealStatus.OnSale, CreatedAt = now.AddDays(-21), UpdatedAt = now },
            new FlashDeal { Id = Id(43), ShopId = Id(22), ProductId = Id(31), SaleStartTime = now.AddDays(-20), OrderEndTime = now.AddDays(20), ShopClosingTime = now.AddDays(20).AddHours(2), Status = FlashDealStatus.OnSale, CreatedAt = now.AddDays(-21), UpdatedAt = now });
        await db.SaveChangesAsync();

        await db.AddRangeAsync(
            new FlashDealVariant { Id = Id(44), FlashDealId = Id(42), VariantId = Id(38), OriginalPrice = 60000m, DealPrice = 35000m, DiscountPercent = 41.67m, TotalQuantity = 100, ReservedQuantity = 2, SoldQuantity = 12, Status = FlashDealVariantStatus.Active, CreatedAt = now.AddDays(-21), UpdatedAt = now },
            new FlashDealVariant { Id = Id(45), FlashDealId = Id(43), VariantId = Id(39), OriginalPrice = 35000m, DealPrice = 20000m, DiscountPercent = 42.86m, TotalQuantity = 80, ReservedQuantity = 1, SoldQuantity = 9, Status = FlashDealVariantStatus.Active, CreatedAt = now.AddDays(-21), UpdatedAt = now });
        await db.SaveChangesAsync();
    }

    private static async Task SeedOrdersAsync(AppDbContext db, DateTime now)
    {
        await db.AddRangeAsync(
            new CartItem { Id = Id(46), UserId = Id(4), FlashDealVariantId = Id(45), Quantity = 1, AddedAt = now.AddHours(-2) },
            Order(Id(50), "SB-SEED-0001", OrderStatus.Completed, PaymentMethod.OnlinePayment, 70000m, now.AddDays(-10), false),
            Order(Id(51), "SB-SEED-0002", OrderStatus.NoShow, PaymentMethod.PayAtShop, 40000m, now.AddDays(-8), true),
            Order(Id(52), "SB-SEED-0003", OrderStatus.Completed, PaymentMethod.OnlinePayment, 35000m, now.AddDays(-3), false));
        await db.SaveChangesAsync();

        await db.AddRangeAsync(
            Item(Id(53), Id(50), Id(44), "Com ga nuong mat ong", 60000m, 35000m, 2),
            Item(Id(54), Id(51), Id(45), "Banh mi thit nuong", 35000m, 20000m, 2),
            Item(Id(55), Id(52), Id(44), "Com ga nuong mat ong", 60000m, 35000m, 1),
            new OrderStatusHistory { Id = Id(56), OrderId = Id(50), ToStatus = OrderStatus.Pending, ChangedBy = Id(4), Note = "Khach hang tao don.", CreatedAt = now.AddDays(-10) },
            new OrderStatusHistory { Id = Id(57), OrderId = Id(50), FromStatus = OrderStatus.ReadyForPickup, ToStatus = OrderStatus.Completed, ChangedBy = Id(3), Note = "Khach da nhan mon.", CreatedAt = now.AddDays(-10).AddMinutes(28) },
            new StaffActivityLog { Id = Id(58), ShopId = Id(22), ActionBy = Id(3), OrderId = Id(50), Action = OrderStatus.Completed, Note = "Xac nhan giao mon thanh cong.", CreatedAt = now.AddDays(-10).AddMinutes(28) });
        await db.SaveChangesAsync();
    }

    private static async Task SeedFinanceAsync(AppDbContext db, DateTime now)
    {
        await db.AddRangeAsync(
            Payment(Id(70), Id(50), 70000m, "PAYOS-SEED-0001", now.AddDays(-10)),
            Payment(Id(71), Id(52), 35000m, "PAYOS-SEED-0003", now.AddDays(-3)),
            new PlatformFeeConfig { Id = Id(74), FeeRate = 0.05m, EffectiveFrom = now.AddMonths(-6), CreatedBy = Id(1), CreatedAt = now.AddMonths(-6) },
            new PlatformFeeStatement { Id = Id(75), ShopId = Id(22), BillingYear = now.Year, BillingMonth = (short)now.Month, Status = PlatformFeeStatementStatus.Issued, TotalFeeAmount = 4750m, IssuedAt = now.AddDays(-1), DueAt = now.AddDays(14), CreatedAt = now.AddDays(-1), UpdatedAt = now.AddDays(-1) });
        await db.SaveChangesAsync();

        await db.AddRangeAsync(
            Fee(Id(76), Id(50), Id(75), 70000m, 3500m, now.AddDays(-10)),
            Fee(Id(77), Id(52), Id(75), 35000m, 1750m, now.AddDays(-3)),
            Fee(Id(78), Id(51), null, 0m, 0m, now.AddDays(-8)),
            new Refund { Id = Id(79), OrderId = Id(50), RefundType = RefundType.CustomerRequest, Status = RefundStatus.Completed, InitiatedBy = Id(4), Reason = "Mot suat com bi thieu phan rau kem.", RequestedAmount = 20000m, ReceiverMethod = RefundReceiverMethod.BankAccount, ReceiverBankName = "Techcombank", ReceiverAccountNumber = "190000000001", ReceiverAccountHolder = "LE THU HA", ReviewedBy = Id(1), ReviewedAt = now.AddDays(-9).AddHours(1), TransferReference = "REFUND-SEED-0001", TransferredAt = now.AddDays(-9).AddHours(2), CreatedAt = now.AddDays(-9), UpdatedAt = now.AddDays(-9).AddHours(2) });
        await db.SaveChangesAsync();

        await db.AddRangeAsync(
            new RefundEvidenceImage { Id = Id(80), RefundId = Id(79), ImageUrl = "https://picsum.photos/seed/refund/800/600", ImageType = RefundEvidenceType.RequestEvidence, UploadedBy = Id(4), CreatedAt = now.AddDays(-9) },
            new PlatformFeeAdjustment { Id = Id(81), ShopId = Id(22), OrderFeeId = Id(76), RefundId = Id(79), StatementId = Id(75), Amount = -500m, Reason = "Dieu chinh phi theo khoan hoan 20.000 VND.", CreatedAt = now.AddDays(-9).AddHours(2) });
        await db.SaveChangesAsync();
    }

    private static async Task SeedTrustAsync(AppDbContext db, DateTime now)
    {
        await db.AddRangeAsync(
            new TrustLevel { Id = Id(90), Name = "Bronze", Description = "Cap khoi dau.", Rank = 1, IsActive = true, CreatedAt = now.AddMonths(-6), UpdatedAt = now },
            new TrustLevel { Id = Id(91), Name = "Silver", Description = "Khach hang co lich su nhan mon tot.", Rank = 2, IsActive = true, CreatedAt = now.AddMonths(-6), UpdatedAt = now },
            new TrustScoreRule { Id = Id(94), ScoreDelta = 5, EffectiveFrom = now.AddMonths(-6), CreatedBy = Id(1), CreatedAt = now.AddMonths(-6) },
            new TrustScoreRule { Id = Id(95), ScoreDelta = -20, EffectiveFrom = now.AddMonths(-6), CreatedBy = Id(1), CreatedAt = now.AddMonths(-6) },
            new RecoveryRequirementRule { Id = Id(96), MinimumOccurrence = 1, ScoreDeduction = 20, RequiredOnlineOrders = 1, RequiredOnlineOrderValue = 30000m, PermanentOfflineLock = false, IsActive = true, EffectiveFrom = now.AddMonths(-6), CreatedByUserId = Id(1), CreatedAt = now.AddMonths(-6) });
        await db.SaveChangesAsync();

        await db.AddRangeAsync(
            new TrustLevelRequirement { Id = Id(92), TrustLevelId = Id(90), MinScore = 0, MinCompletedOrders = 0, MinTotalOrderValue = 0m },
            new TrustLevelRequirement { Id = Id(93), TrustLevelId = Id(91), MinScore = 80, MinCompletedOrders = 3, MinTotalOrderValue = 100000m },
            new UserTrustScore { Id = Id(98), UserId = Id(2), TrustLevelId = Id(91), CurrentScore = 100, CreatedAt = now.AddMonths(-5), UpdatedAt = now },
            new UserTrustScore { Id = Id(99), UserId = Id(3), TrustLevelId = Id(90), CurrentScore = 40, CreatedAt = now.AddMonths(-4), UpdatedAt = now },
            new UserTrustScore { Id = Id(100), UserId = Id(4), TrustLevelId = Id(91), CurrentScore = 85, CreatedAt = now.AddMonths(-3), UpdatedAt = now },
            new UserLevelProgress { Id = Id(101), UserId = Id(4), CompletedOrdersCount = 2, AccumulatedOrderValue = 105000m, CreatedAt = now.AddMonths(-3), UpdatedAt = now },
            new UserTrustLevelHistory { Id = Id(102), UserId = Id(4), NewTrustLevelId = Id(91), Reason = TrustLevelChangeReason.InitialAssignment, CreatedAt = now.AddMonths(-3) });
        await db.SaveChangesAsync();

        await db.AddRangeAsync(
            new NoShowPenalty { Id = Id(97), UserId = Id(4), OrderId = Id(51), RecoveryRuleId = Id(96), PenaltyYear = now.AddDays(-8).Year, PenaltyMonth = now.AddDays(-8).Month, OccurrenceNumber = 1, ScoreDeducted = 20, RequiredOnlineOrders = 1, RequiredOnlineOrderValue = 30000m, CompletedOnlineOrders = 1, CompletedOnlineOrderValue = 35000m, PermanentOfflineLock = false, LockEndsAt = now.AddDays(-3), Status = NoShowPenaltyStatus.Recovered, CreatedAt = now.AddDays(-8).AddHours(1), RecoveredAt = now.AddDays(-3).AddMinutes(25) },
            new TrustScoreHistory { Id = Id(103), UserId = Id(4), OrderId = Id(51), RuleId = Id(95), EventType = TrustScoreEventType.NoShow, ScoreBefore = 100, ScoreDelta = -20, ScoreAfter = 80, Reason = "Khong den nhan don hang.", CreatedAt = now.AddDays(-8).AddHours(1) },
            new TrustScoreHistory { Id = Id(104), UserId = Id(4), OrderId = Id(52), RuleId = Id(94), EventType = TrustScoreEventType.OrderCompleted, ScoreBefore = 80, ScoreDelta = 5, ScoreAfter = 85, Reason = "Hoan thanh don online.", CreatedAt = now.AddDays(-3).AddMinutes(25) });
        await db.SaveChangesAsync();

        await db.AddRangeAsync(
            new NoShowPenaltyOrderHistory { Id = Id(105), NoShowPenaltyId = Id(97), OrderId = Id(52), OrderValue = 35000m, CreatedAt = now.AddDays(-3).AddMinutes(25) },
            new UserTrustLevelHistory { Id = Id(106), UserId = Id(4), PreviousTrustLevelId = Id(91), NewTrustLevelId = Id(90), Reason = TrustLevelChangeReason.NoShowPenalty, OrderId = Id(51), NoShowPenaltyId = Id(97), CreatedAt = now.AddDays(-8).AddHours(1) });
        await db.SaveChangesAsync();
    }

    private static async Task SeedFeedbackAsync(AppDbContext db, DateTime now)
    {
        await db.AddRangeAsync(
            new ProductFeedback { Id = Id(110), OrderItemId = Id(53), ProductId = Id(30), UserId = Id(4), Rating = 4, Comment = "Mon ngon, dong goi sach se; shop xu ly phan thieu nhanh.", Status = FeedbackStatus.Visible, CreatedAt = now.AddDays(-9), UpdatedAt = now.AddDays(-8) },
            new Notification { Id = Id(113), UserId = Id(4), Type = NotificationType.OrderCompleted, Title = "Don hang da hoan thanh", Body = "Don SB-SEED-0001 da hoan thanh. Hay danh gia mon an.", ActionUrl = $"/orders/{Id(50)}", DeduplicationKey = "seed-order-completed-0001", ReadAt = now.AddDays(-9), CreatedAt = now.AddDays(-10).AddMinutes(28) },
            new Notification { Id = Id(114), UserId = Id(2), Type = NotificationType.RefundRequestCreated, Title = "Co yeu cau hoan tien moi", Body = "Khach hang tao yeu cau hoan tien cho don SB-SEED-0001.", ActionUrl = $"/shops/orders/{Id(50)}/refund", DeduplicationKey = "seed-refund-created-0001", CreatedAt = now.AddDays(-9) });
        await db.SaveChangesAsync();

        await db.AddRangeAsync(
            new ProductFeedbackReply { Id = Id(111), ProductFeedbackId = Id(110), RepliedByUserId = Id(2), Content = "Cam on ban da gop y. Shop da cap nhat quy trinh dong goi.", CreatedAt = now.AddDays(-8), UpdatedAt = now.AddDays(-8) },
            new ProductFeedbackModerationLog { Id = Id(112), ProductFeedbackId = Id(110), AdminUserId = Id(1), Action = FeedbackModerationAction.Restore, PreviousStatus = FeedbackStatus.HiddenByAdmin, NewStatus = FeedbackStatus.Visible, Reason = "Noi dung hop le sau khi kiem tra lai.", CreatedAt = now.AddDays(-8) });
        await db.SaveChangesAsync();
    }

    private static async Task SeedRemainingDataAsync(AppDbContext db, DateTime now)
    {
        await SeedRemainingUsersAndShopAsync(db, now);
        await SeedRemainingCatalogAsync(db, now);
        await SeedRemainingOrdersAsync(db, now);
        await SeedRemainingEngagementAsync(db, now);

        await db.AddAsync(new AuditLog
        {
            Id = Id(199),
            ActorType = AuditActorType.System,
            Action = "SEED_V2_COMPLETED",
            TargetType = "Database",
            TargetId = Id(199),
            NewValuesJson = "{\"version\":2,\"status\":\"completed\"}",
            Reason = "Danh dau bo du lieu seed mo rong da duoc insert day du.",
            CorrelationId = Id(198),
            CreatedAt = now
        });
        await db.SaveChangesAsync();
    }

    private static async Task SeedRemainingUsersAndShopAsync(AppDbContext db, DateTime now)
    {
        await db.AddRangeAsync(
            new UserAddress { Id = Id(120), UserId = Id(1), Label = "Van phong", AddressLine = "01 Le Duan", Ward = "Phuong Ben Nghe", District = "Quan 1", City = "Ho Chi Minh", Latitude = 10.781130, Longitude = 106.699490, IsDefault = true, CreatedAt = now.AddMonths(-6), UpdatedAt = now },
            new RefreshToken { Id = Id(121), UserId = Id(2), TokenHash = "seed-revoked-refresh-token-hash", ExpiresAt = now.AddDays(-10), CreatedAt = now.AddDays(-40), RevokedAt = now.AddDays(-35), RevokeReason = "Rotated", ReplacedByTokenId = Id(11) },
            new UserRoleChangeLog { Id = Id(122), UserId = Id(3), PreviousRole = UserRole.User, NewRole = UserRole.Staff, Reason = "Chap nhan loi moi lam nhan vien shop.", CreatedAt = now.AddMonths(-1) },
            new AuditLog { Id = Id(123), ActorType = AuditActorType.System, Action = "REFRESH_TOKEN_ROTATED", TargetType = "RefreshToken", TargetId = Id(121), NewValuesJson = "{\"replacedByTokenId\":\"00000000-0000-0000-0000-000000000011\"}", CreatedAt = now.AddDays(-35) },
            new ShopApplication { Id = Id(124), ApplicantUserId = Id(4), Name = "Quan Nho Thu Ha", Description = "Ho so shop cua tai khoan dang bi khoa quyen shop.", BusinessLicenseNo = "SB-SEED-0002", AddressLine = "15 Phan Xich Long", Ward = "Phuong 2", District = "Quan Phu Nhuan", City = "Ho Chi Minh", Latitude = 10.798780, Longitude = 106.684030, OpeningTime = new TimeOnly(7, 0), ClosingTime = new TimeOnly(20, 0), Status = ShopApplicationStatus.Approved, BankName = "Techcombank", BankAccountNumber = "190000000001", BankAccountHolder = "LE THU HA", PayosClientId = "seed-client-locked", PayosApiKey = "seed-api-locked", PayosChecksumKey = "seed-checksum-locked", RevisionNumber = 2, CreatedAt = now.AddMonths(-2), UpdatedAt = now.AddDays(-5) });
        await db.SaveChangesAsync();

        await db.AddRangeAsync(
            new ShopApplicationDocument { Id = Id(125), ApplicationId = Id(124), DocumentType = ShopDocumentType.FoodSafetyCertificate, FileUrl = "https://example.local/seed/food-safety-certificate.pdf", OriginalFileName = "chung-nhan-an-toan-thuc-pham.pdf", ContentType = "application/pdf", RevisionNumber = 2, UploadedBy = Id(4), UploadedAt = now.AddDays(-20), IsCurrent = true },
            new ShopApplicationReviewLog { Id = Id(126), ApplicationId = Id(124), AdminId = Id(1), FromStatus = ShopApplicationStatus.NeedsRevision, ToStatus = ShopApplicationStatus.Approved, RevisionNumber = 2, Note = "Ho so bo sung da hop le.", CreatedAt = now.AddDays(-15) },
            new Shop { Id = Id(127), OwnerUserId = Id(4), ApplicationId = Id(124), Name = "Quan Nho Thu Ha", Description = "Shop tam dung do tai khoan bi khoa quyen shop.", AddressLine = "15 Phan Xich Long", Ward = "Phuong 2", District = "Quan Phu Nhuan", City = "Ho Chi Minh", Latitude = 10.798780, Longitude = 106.684030, OpeningTime = new TimeOnly(7, 0), ClosingTime = new TimeOnly(20, 0), Status = ShopStatus.Suspended, CreatedAt = now.AddDays(-15), UpdatedAt = now.AddDays(-5) });
        await db.SaveChangesAsync();

        await db.AddRangeAsync(
            new ShopPaymentConfig { Id = 2, ShopId = Id(127), BankName = "Techcombank", BankAccountNumber = "190000000001", BankAccountHolder = "LE THU HA", PayosClientId = "seed-client-locked", PayosApiKey = "seed-api-locked", PayosChecksumKey = "seed-checksum-locked", CreatedAt = now.AddDays(-15), UpdatedAt = now.AddDays(-5) },
            new Notification { Id = Id(128), UserId = Id(4), Type = NotificationType.ShopApplicationApproved, Title = "Ho so shop da duoc duyet", Body = "Ho so Quan Nho Thu Ha da duoc phe duyet.", ActionUrl = $"/shops/{Id(127)}", DeduplicationKey = "seed-shop-approved-0002", ReadAt = now.AddDays(-14), DismissedAt = now.AddDays(-13), CreatedAt = now.AddDays(-15) },
            new Notification { Id = Id(129), UserId = Id(3), Type = NotificationType.StaffInvitationAccepted, Title = "Da tham gia shop", Body = "Ban da tro thanh nhan vien cua Bep Xanh Cuoi Ngay.", ActionUrl = $"/shops/{Id(22)}", DeduplicationKey = "seed-staff-accepted-0001", ReadAt = now.AddMonths(-1), CreatedAt = now.AddMonths(-1) });
        await db.SaveChangesAsync();
    }

    private static async Task SeedRemainingCatalogAsync(AppDbContext db, DateTime now)
    {
        await db.AddRangeAsync(
            new ProductAttribute { Id = Id(130), ProductId = Id(31), Name = "Do cay" },
            new ProductVariant { Id = Id(133), ProductId = Id(30), Sku = "COM-GA-LON", OriginalPrice = 75000m, DealPrice = 45000m, IsActive = true, CreatedAt = now.AddDays(-40) },
            new ProductContentRevision { Id = Id(134), ProductId = Id(31), RevisionNumber = 1, SubmittedByUserId = Id(2), Name = "Banh mi thit nuong dac biet", Description = "Can bo sung anh thanh phan va thong tin di ung.", CategoryId = Id(20), ImagesJson = "[\"https://picsum.photos/seed/banh-mi/800/600\"]", Status = ProductReviewStatus.NeedsRevision, ReviewedByUserId = Id(1), ReviewNote = "Can bo sung thong tin thanh phan.", SubmittedAt = now.AddDays(-7), ReviewedAt = now.AddDays(-6) });
        await db.SaveChangesAsync();

        await db.AddRangeAsync(
            new ProductAttributeValue { Id = Id(131), AttributeId = Id(130), Value = "Khong cay" },
            new ProductAttributeValue { Id = Id(132), AttributeId = Id(130), Value = "Cay vua" },
            new ProductVariantValue { VariantId = Id(133), AttributeValueId = Id(36) });
        await db.SaveChangesAsync();

        await db.AddRangeAsync(
            new ProductVariantValue { VariantId = Id(39), AttributeValueId = Id(131) },
            new FlashDealVariant { Id = Id(135), FlashDealId = Id(42), VariantId = Id(133), OriginalPrice = 75000m, DealPrice = 45000m, DiscountPercent = 40m, TotalQuantity = 40, ReservedQuantity = 0, SoldQuantity = 5, Status = FlashDealVariantStatus.Active, CreatedAt = now.AddDays(-20), UpdatedAt = now });
        await db.SaveChangesAsync();
    }

    private static async Task SeedRemainingOrdersAsync(AppDbContext db, DateTime now)
    {
        var orderAt = now.AddDays(-2);
        await db.AddRangeAsync(
            new CartItem { Id = Id(136), UserId = Id(2), FlashDealVariantId = Id(135), Quantity = 2, AddedAt = now.AddMinutes(-45) },
            new Order { Id = Id(140), OrderCode = "SB-SEED-0004", CustomerId = Id(4), ShopId = Id(22), PaymentMethod = PaymentMethod.PayAtShop, Status = OrderStatus.Completed, TotalAmount = 20000m, DepositAmount = 0m, RemainingAmount = 0m, DistanceKm = 4.2m, EtaMinutes = 20, SoftDeadlineAt = orderAt.AddMinutes(35), HardDeadlineAt = orderAt.AddHours(1), IsLateArrival = true, NoShow = false, ConfirmedAt = orderAt.AddMinutes(2), ReadyAt = orderAt.AddMinutes(18), CompletedAt = orderAt.AddMinutes(42), CreatedAt = orderAt, UpdatedAt = orderAt.AddMinutes(42) },
            new PlatformFeeStatement { Id = Id(147), ShopId = Id(127), BillingYear = now.Year, BillingMonth = (short)now.Month, Status = PlatformFeeStatementStatus.Draft, TotalFeeAmount = 0m, CreatedAt = now.AddDays(-5), UpdatedAt = now.AddDays(-5) });
        await db.SaveChangesAsync();

        await db.AddRangeAsync(
            Item(Id(141), Id(140), Id(45), "Banh mi thit nuong", 35000m, 20000m, 1),
            new Payment { Id = Id(142), OrderId = Id(140), PaymentType = PaymentType.Full, Method = PaymentMethod.PayAtShop, Amount = 20000m, Status = PaymentStatus.Succeeded, PaidAt = orderAt.AddMinutes(42), CreatedAt = orderAt, UpdatedAt = orderAt.AddMinutes(42) },
            new Payment { Id = Id(143), OrderId = Id(51), PaymentType = PaymentType.Remaining, Method = PaymentMethod.PayAtShop, Amount = 40000m, Status = PaymentStatus.Failed, CreatedAt = now.AddDays(-8), UpdatedAt = now.AddDays(-8).AddHours(1) },
            Fee(Id(144), Id(140), Id(75), 20000m, 1000m, orderAt));

        var statement = await db.PlatformFeeStatements.FindAsync(Id(75));
        if (statement is not null)
        {
            statement.TotalFeeAmount = 5750m;
            statement.UpdatedAt = now;
        }
        await db.SaveChangesAsync();

        await db.AddRangeAsync(
            History(145, 50, OrderStatus.Pending, OrderStatus.Confirmed, 3, now.AddDays(-10).AddMinutes(2), "Nhan vien xac nhan don."),
            History(146, 50, OrderStatus.Confirmed, OrderStatus.Preparing, 3, now.AddDays(-10).AddMinutes(5), "Bat dau chuan bi mon."),
            History(148, 50, OrderStatus.Preparing, OrderStatus.ReadyForPickup, 3, now.AddDays(-10).AddMinutes(20), "Mon da san sang."),
            History(149, 51, OrderStatus.Pending, OrderStatus.Confirmed, 3, now.AddDays(-8).AddMinutes(2), "Nhan vien xac nhan don."),
            History(150, 51, OrderStatus.Confirmed, OrderStatus.Preparing, 3, now.AddDays(-8).AddMinutes(5), "Bat dau chuan bi mon."),
            History(151, 51, OrderStatus.Preparing, OrderStatus.ReadyForPickup, 3, now.AddDays(-8).AddMinutes(20), "Mon da san sang."),
            History(152, 51, OrderStatus.ReadyForPickup, OrderStatus.NoShow, 3, now.AddDays(-8).AddHours(1), "Khach khong den nhan mon."),
            History(153, 52, OrderStatus.Pending, OrderStatus.Confirmed, 3, now.AddDays(-3).AddMinutes(2), "Nhan vien xac nhan don."),
            History(154, 52, OrderStatus.Confirmed, OrderStatus.Preparing, 3, now.AddDays(-3).AddMinutes(5), "Bat dau chuan bi mon."),
            History(155, 52, OrderStatus.Preparing, OrderStatus.ReadyForPickup, 3, now.AddDays(-3).AddMinutes(18), "Mon da san sang."),
            History(156, 52, OrderStatus.ReadyForPickup, OrderStatus.Completed, 3, now.AddDays(-3).AddMinutes(25), "Khach da nhan mon."),
            History(157, 140, OrderStatus.Pending, OrderStatus.Confirmed, 3, orderAt.AddMinutes(2), "Nhan vien xac nhan don."),
            History(158, 140, OrderStatus.Confirmed, OrderStatus.Preparing, 3, orderAt.AddMinutes(5), "Bat dau chuan bi mon."),
            History(159, 140, OrderStatus.Preparing, OrderStatus.ReadyForPickup, 3, orderAt.AddMinutes(18), "Mon da san sang."),
            History(160, 140, OrderStatus.ReadyForPickup, OrderStatus.Completed, 3, orderAt.AddMinutes(42), "Khach den tre nhung da nhan mon."),
            new StaffActivityLog { Id = Id(161), ShopId = Id(22), ActionBy = Id(3), OrderId = Id(51), Action = OrderStatus.NoShow, Note = "Danh dau khach khong den nhan mon.", CreatedAt = now.AddDays(-8).AddHours(1) },
            new StaffActivityLog { Id = Id(162), ShopId = Id(22), ActionBy = Id(3), OrderId = Id(52), Action = OrderStatus.Completed, Note = "Don phuc hoi da hoan thanh.", CreatedAt = now.AddDays(-3).AddMinutes(25) },
            new StaffActivityLog { Id = Id(163), ShopId = Id(22), ActionBy = Id(3), OrderId = Id(140), Action = OrderStatus.Completed, Note = "Khach den tre 7 phut.", CreatedAt = orderAt.AddMinutes(42) });
        await db.SaveChangesAsync();
    }

    private static async Task SeedRemainingEngagementAsync(AppDbContext db, DateTime now)
    {
        await db.AddRangeAsync(
            new ProductFeedback { Id = Id(170), OrderItemId = Id(141), ProductId = Id(31), UserId = Id(4), Rating = 5, Comment = "Banh mi gion va nhan vien than thien.", Status = FeedbackStatus.Visible, CreatedAt = now.AddDays(-1), UpdatedAt = now.AddDays(-1) },
            new ProductFeedback { Id = Id(171), OrderItemId = Id(55), ProductId = Id(30), UserId = Id(4), Rating = 2, Comment = "Danh gia nay dang duoc an de admin kiem tra.", Status = FeedbackStatus.HiddenByAdmin, CreatedAt = now.AddDays(-2), UpdatedAt = now.AddDays(-1), HiddenAt = now.AddDays(-1) },
            new UserLevelProgress { Id = Id(172), UserId = Id(2), CompletedOrdersCount = 0, AccumulatedOrderValue = 0m, CreatedAt = now.AddMonths(-5), UpdatedAt = now },
            new UserLevelProgress { Id = Id(173), UserId = Id(3), CompletedOrdersCount = 0, AccumulatedOrderValue = 0m, CreatedAt = now.AddMonths(-4), UpdatedAt = now },
            new UserTrustLevelHistory { Id = Id(174), UserId = Id(4), PreviousTrustLevelId = Id(90), NewTrustLevelId = Id(91), Reason = TrustLevelChangeReason.RequirementsMet, OrderId = Id(52), CreatedAt = now.AddDays(-3).AddMinutes(25) },
            new Refund { Id = Id(175), OrderId = Id(52), RefundType = RefundType.CustomerRequest, Status = RefundStatus.AwaitingCustomerDetails, InitiatedBy = Id(4), Reason = "Khach hang dang bo sung thong tin nhan tien.", RequestedAmount = 10000m, CreatedAt = now.AddDays(-1), UpdatedAt = now.AddDays(-1) },
            new RefundEvidenceImage { Id = Id(176), RefundId = Id(79), ImageUrl = "https://picsum.photos/seed/refund-transfer/800/600", ImageType = RefundEvidenceType.TransferProof, UploadedBy = Id(1), CreatedAt = now.AddDays(-9).AddHours(2) },
            new SaveBite.Backend.Models.Entities.StaffInvitation { Id = Id(177), ShopId = Id(127), InvitedUserId = Id(2), InvitedBy = Id(4), Status = "Declined", InvitedAt = now.AddDays(-12), RespondedAt = now.AddDays(-11) },
            new ProductImage { Id = Id(178), ProductId = Id(30), ImageUrl = "https://picsum.photos/seed/com-ga-detail/800/600" });
        await db.SaveChangesAsync();

        await db.AddRangeAsync(
            new ProductFeedbackReply { Id = Id(180), ProductFeedbackId = Id(170), RepliedByUserId = Id(2), Content = "Cam on ban da ung ho shop!", CreatedAt = now.AddHours(-20), UpdatedAt = now.AddHours(-20) },
            new ProductFeedbackModerationLog { Id = Id(181), ProductFeedbackId = Id(171), AdminUserId = Id(1), Action = FeedbackModerationAction.Hide, PreviousStatus = FeedbackStatus.Visible, NewStatus = FeedbackStatus.HiddenByAdmin, Reason = "Tam an de xac minh noi dung voi don hang.", CreatedAt = now.AddDays(-1) },
            new Notification { Id = Id(182), UserId = Id(4), Type = NotificationType.PaymentSucceeded, Title = "Thanh toan thanh cong", Body = "Thanh toan cho don SB-SEED-0003 da thanh cong.", ActionUrl = $"/orders/{Id(52)}", DeduplicationKey = "seed-payment-success-0003", ReadAt = now.AddDays(-2), CreatedAt = now.AddDays(-3) },
            new Notification { Id = Id(183), UserId = Id(4), Type = NotificationType.NoShowPenaltyCreated, Title = "Gioi han nhan tai shop", Body = "Tai khoan bi gioi han tam thoi do khong den nhan don SB-SEED-0002.", ActionUrl = "/account/trust-score", DeduplicationKey = "seed-no-show-penalty-0002", ReadAt = now.AddDays(-7), CreatedAt = now.AddDays(-8) },
            new Notification { Id = Id(184), UserId = Id(2), Type = NotificationType.ProductNeedsRevision, Title = "San pham can chinh sua", Body = "Banh mi thit nuong can bo sung thong tin thanh phan.", ActionUrl = $"/products/{Id(31)}/revisions", DeduplicationKey = "seed-product-revision-0002", CreatedAt = now.AddDays(-6) },
            new Notification { Id = Id(185), UserId = Id(4), Type = NotificationType.RefundApproved, Title = "Yeu cau hoan tien da duoc duyet", Body = "Yeu cau hoan tien cua don SB-SEED-0001 da duoc duyet.", ActionUrl = $"/refunds/{Id(79)}", DeduplicationKey = "seed-refund-approved-0001", ReadAt = now.AddDays(-8), CreatedAt = now.AddDays(-9).AddHours(1) });

        var noShowHistory = await db.TrustScoreHistories.FindAsync(Id(103));
        if (noShowHistory is not null) noShowHistory.NoShowPenaltyId = Id(97);

        var penaltyLevelHistory = await db.UserTrustLevelHistories.FindAsync(Id(106));
        if (penaltyLevelHistory is not null) penaltyLevelHistory.NewTrustLevelId = Id(90);
        await db.SaveChangesAsync();
    }

    private static Order Order(Guid id, string code, OrderStatus status, PaymentMethod method, decimal total, DateTime at, bool noShow) => new()
    {
        Id = id,
        OrderCode = code,
        CustomerId = Id(4),
        ShopId = Id(22),
        PaymentMethod = method,
        Status = status,
        TotalAmount = total,
        DepositAmount = 0m,
        RemainingAmount = method == PaymentMethod.PayAtShop ? total : 0m,
        DistanceKm = 2.5m,
        EtaMinutes = 12,
        SoftDeadlineAt = at.AddMinutes(30),
        HardDeadlineAt = at.AddHours(1),
        IsLateArrival = false,
        NoShow = noShow,
        ConfirmedAt = at.AddMinutes(2),
        ReadyAt = at.AddMinutes(20),
        CompletedAt = status == OrderStatus.Completed ? at.AddMinutes(28) : null,
        ExpiredAt = status == OrderStatus.NoShow ? at.AddHours(1) : null,
        CreatedAt = at,
        UpdatedAt = status == OrderStatus.NoShow ? at.AddHours(1) : at.AddMinutes(28)
    };

    private static OrderItem Item(Guid id, Guid orderId, Guid dealVariantId, string name, decimal original, decimal deal, int quantity) => new()
    {
        Id = id,
        OrderId = orderId,
        FlashDealVariantId = dealVariantId,
        ProductNameSnapshot = name,
        VariantNameSnapshot = "Tieu chuan",
        OriginalPriceSnapshot = original,
        DealPriceSnapshot = deal,
        UnitPrice = deal,
        Quantity = quantity,
        Subtotal = deal * quantity
    };

    private static OrderStatusHistory History(int id, int orderId, OrderStatus from, OrderStatus to, int changedBy, DateTime at, string note) => new()
    {
        Id = Id(id),
        OrderId = Id(orderId),
        FromStatus = from,
        ToStatus = to,
        ChangedBy = Id(changedBy),
        Note = note,
        CreatedAt = at
    };

    private static Payment Payment(Guid id, Guid orderId, decimal amount, string gatewayId, DateTime at) => new()
    {
        Id = id,
        OrderId = orderId,
        PaymentType = PaymentType.Full,
        Method = PaymentMethod.OnlinePayment,
        Amount = amount,
        GatewayTransactionId = gatewayId,
        Status = PaymentStatus.Succeeded,
        PaidAt = at.AddMinutes(1),
        CreatedAt = at,
        UpdatedAt = at.AddMinutes(1)
    };

    private static OrderPlatformFee Fee(Guid id, Guid orderId, Guid? statementId, decimal gross, decimal fee, DateTime at) => new()
    {
        Id = id,
        OrderId = orderId,
        ShopId = Id(22),
        FeeConfigId = Id(74),
        StatementId = statementId,
        GrossAmount = gross,
        FeeRate = 0.05m,
        FeeAmount = fee,
        CalculatedAt = at.AddMinutes(28),
        CreatedAt = at.AddMinutes(28),
        UpdatedAt = at.AddMinutes(28)
    };

    private static Guid Id(int value) => Guid.Parse($"00000000-0000-0000-0000-{value:000000000000}");
}
