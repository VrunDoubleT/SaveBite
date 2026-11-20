using Microsoft.EntityFrameworkCore;
using SaveBite.Backend.Data;
using SaveBite.Backend.Models.Entities;

internal static class EnglishScreenshotData
{
    public static async Task ApplyAsync(AppDbContext db)
    {
        await using var transaction = await db.Database.BeginTransactionAsync();
        var now = DateTime.UtcNow;
        var names = new[] {
            "Rice Bowls", "Noodles", "Bakery", "Sandwiches", "Salads", "Soups",
            "Grilled Dishes", "Vegetarian", "Vegan", "Seafood", "Chicken", "Beef",
            "Pork", "Pizza", "Pasta", "Breakfast", "Lunch Specials", "Dinner Specials",
            "Family Meals", "Snacks", "Desserts", "Fresh Fruit", "Coffee", "Tea",
            "Milk Tea", "Juices", "Smoothies", "Healthy Meals", "Gluten-Free",
            "Seasonal Favorites", "Meal Boxes", "Side Dishes"
        };
        var categories = await db.Set<Category>().OrderBy(c => c.CreatedAt).ThenBy(c => c.Id).ToListAsync();
        for (var i = 0; i < Math.Max(names.Length, categories.Count); i++)
        {
            var category = i < categories.Count ? categories[i] : new Category { Id = Guid.NewGuid(), CreatedAt = now.AddDays(-i - 1) };
            category.Name = i < names.Length ? names[i] : $"Chef's Collection {i + 1}";
            category.Description = $"Discover freshly prepared {category.Name.ToLowerInvariant()} at reduced prices and help prevent food waste.";
            category.IsActive = i % 7 != 6;
            category.UpdatedAt = now;
            if (i >= categories.Count) db.Add(category);
        }
        var users = await db.Users.OrderBy(x => x.Id).ToListAsync();
        var people = new[] { "Alex Morgan", "Olivia Bennett", "James Carter", "Emma Wilson", "Daniel Brooks", "Sophie Turner", "Noah Parker", "Grace Miller", "Ethan Collins", "Chloe Adams", "Lucas Reed" };
        for (var i = 0; i < users.Count; i++) users[i].FullName = people[i % people.Length];
        users.Single(u => u.Email == "admin@gmail.com").FullName = "SaveBite Administrator";
        var customer = users.Single(u => u.Email == "customer@gmail.com");
        customer.FullName = "Alex Morgan";
        customer.Phone = "0912345678";
        var shopNames = new[] { "Green Kitchen", "Sunset Meal House", "Golden Crust Bakery", "Riverside Rice Kitchen", "Bubble Tea Corner", "Spice Garden", "Fresh Leaf Cafe", "Crispy Chicken House" };
        var shops = await db.Shops.OrderBy(x => x.Id).ToListAsync();
        for (var i = 0; i < shops.Count; i++)
        {
            shops[i].Name = shopNames[i % shopNames.Length];
            shops[i].Description = "Freshly prepared meals, friendly service, and great end-of-day savings.";
            shops[i].AddressLine = $"{600 + i * 12} Nguyen Van Cu Extension";
            shops[i].Ward = "An Binh Ward"; shops[i].District = "Ninh Kieu District"; shops[i].City = "Can Tho";
        }
        var products = await db.Products.OrderBy(x => x.Id).ToListAsync();
        var dishNames = new[] { "Honey Grilled Chicken Rice", "Grilled Pork Baguette", "Vegetable Rice Bowl", "Beef Noodle Soup", "Roast Pork Rice", "Garlic Butter Croissant", "Fresh Garden Salad", "Crispy Chicken Box", "Mushroom Noodles", "Seafood Pizza", "Vegetarian Lunch Box", "Grilled Beef Rice", "Evening Meal Combo", "Iced Milk Coffee", "Peach Iced Tea", "Chocolate Brownie", "Caramel Milk Tea", "Fresh Orange Juice", "Tofu Rice Bowl", "Chicken Sandwich", "Vegetable Soup", "Roasted Sweet Potato", "Tuna Sandwich", "Cheese Pasta", "Mango Smoothie", "Butter Pastry", "Lemongrass Chicken", "Mixed Fruit Cup", "Egg Fried Rice", "Steamed Dumplings", "Beef Stir-Fry", "Peanut Noodle Salad" };
        for (var i = 0; i < products.Count; i++)
        {
            products[i].Name = dishNames[i % dishNames.Length] + (i >= dishNames.Length ? $" - House Special {i / dishNames.Length + 1}" : "");
            products[i].Description = "Prepared fresh today using quality ingredients. Reserve at a reduced price and collect before closing.";
        }
        var productNames = products.ToDictionary(p => p.Id, p => p.Name);
        var items = await db.Set<OrderItem>().Include(x => x.FlashDealVariant).ThenInclude(x => x.Variant).ToListAsync();
        foreach (var item in items)
        {
            item.ProductNameSnapshot = productNames[item.FlashDealVariant.Variant.ProductId];
            item.VariantNameSnapshot = "Regular portion";
        }
        foreach (var order in await db.Orders.ToListAsync())
        {
            var shop = shops.Single(s => s.Id == order.ShopId);
            order.ShopNameSnapshot = shop.Name;
            order.ShopAddressSnapshot = $"{shop.AddressLine}, {shop.Ward}, {shop.District}, {shop.City}";
        }
        var addresses = await db.Set<UserAddress>().OrderBy(x => x.Id).ToListAsync();
        foreach (var address in addresses)
        {
            address.Label = address.IsDefault ? "Home" : "Office";
            address.AddressLine = "600 Nguyen Van Cu Extension";
            address.Ward = "An Binh Ward"; address.District = "Ninh Kieu District"; address.City = "Can Tho";
        }
        foreach (var (label, line, offset) in new[] { ("Office", "24 Tran Hoang Na Street", 0.003), ("University", "600 Nguyen Van Cu Extension", 0.001) })
            if (!addresses.Any(a => a.UserId == customer.Id && a.Label == label))
                db.Add(new UserAddress { Id = Guid.NewGuid(), UserId = customer.Id, Label = label, AddressLine = line, Ward = "An Binh Ward", District = "Ninh Kieu District", City = "Can Tho", Latitude = 10.01245 + offset, Longitude = 105.73243 + offset, CreatedAt = now, UpdatedAt = now });
        await db.SaveChangesAsync();
        await transaction.CommitAsync();
        Console.WriteLine($"English screenshot data: {await db.Set<Category>().CountAsync()} categories, {products.Count} products, {await db.Orders.CountAsync()} orders, {await db.Set<UserAddress>().CountAsync(a => a.UserId == customer.Id)} customer addresses.");
    }
}
