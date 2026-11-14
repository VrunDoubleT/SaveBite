using System.Reflection;
using Microsoft.Extensions.Logging.Abstractions;
using SaveBite.Backend.Models.Entities;
using SaveBite.Backend.Repositories.Interfaces;
using SaveBite.Backend.Services.Implementations;
using Xunit;

namespace SaveBite.Tests;
public class ShopOrderSearchTests
{
    [Theory]
    [InlineData("123", 1, 1)]
    [InlineData(null, 3, 10)]
    public async Task SearchUsesSingleResultWhileListKeepsPagination(string? search, int expectedPage, int expectedSize)
    {
        var repository = DispatchProxy.Create<IShopOrderRepository, Repository>();
        var proxy = (Repository)(object)repository;
        var service = new ShopOrderService(repository, null!, null!, null!, null!, null!, TimeProvider.System,
            NullLogger<ShopOrderService>.Instance, null!, null!, null!, null!, null!, null!, null!, null!);
        var result = await service.PageAsync(Guid.NewGuid(), Guid.NewGuid(), "All", search, 3, 10);
        Assert.Equal(expectedPage, proxy.Page);
        Assert.Equal(expectedSize, proxy.Size);
        Assert.Equal(expectedPage, result.Page.Page);
        Assert.Equal(expectedSize, result.Page.PageSize);
        if (search != null) Assert.Equal("000123", proxy.Search);
    }
    public class Repository : DispatchProxy
    {
        public int Page, Size;
        public string? Search;
        protected override object? Invoke(MethodInfo? method, object?[]? args)
        {
            if (method!.Name == "RequireViewAccessAsync") return Task.FromResult(true);
            if (method.Name == "PageAsync")
            {
                Search = (string?)args![2]; Page = (int)args[3]!; Size = (int)args[4]!;
                return Task.FromResult(((IReadOnlyList<Order>)Array.Empty<Order>(), 0, (IReadOnlyDictionary<string,int>)new Dictionary<string,int>()));
            }
            throw new NotSupportedException(method.Name);
        }
    }
}
