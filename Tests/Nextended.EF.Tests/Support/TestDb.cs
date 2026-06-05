using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Nextended.EF.Tests.Domain;

namespace Nextended.EF.Tests.Support;

/// <summary>Fresh InMemory database per test, optionally seeded with a small fixture.</summary>
internal static class TestDb
{
    public static ShopContext CreateEmpty(string? databaseName = null)
    {
        var options = new DbContextOptionsBuilder<ShopContext>()
            .UseInMemoryDatabase(databaseName ?? Guid.NewGuid().ToString())
            .EnableSensitiveDataLogging()
            .Options;
        return new ShopContext(options);
    }

    public static async Task<ShopContext> SeedAsync()
    {
        var ctx = CreateEmpty();

        var widget = new Product { Id = 1, Name = "Widget",   Sku = "W-1", Price = 9.50m };
        var gizmo  = new Product { Id = 2, Name = "Gizmo",    Sku = "G-2", Price = 19.99m };
        var sprocket = new Product { Id = 3, Name = "Sprocket", Sku = "S-3", Price = 5.00m };

        var alice = new Customer
        {
            Id = 1, Name = "Alice", Email = "alice@example.com", CreditLimit = 500m,
            Address = new Address { Id = 1, Street = "Main 1", City = "Berlin", ZipCode = "10115" },
        };
        var bob = new Customer
        {
            Id = 2, Name = "Bob", Email = "bob@example.com", CreditLimit = 1500m,
            Address = new Address { Id = 2, Street = "Park 4", City = "Hamburg", ZipCode = "20095" },
        };
        var carol = new Customer
        {
            Id = 3, Name = "Carol", Email = "carol@cargo.com", CreditLimit = 100m,
            // no address on purpose
        };

        var order1 = new Order
        {
            Id = 1, OrderNumber = "ORD-100", CustomerId = 1,
            CreatedAt = new DateTime(2025, 1, 5, 9, 0, 0, DateTimeKind.Utc), TotalCost = 38.99m,
            Lines = new List<OrderLine>
            {
                new() { Id = 10, OrderId = 1, ProductId = 1, Quantity = 2, UnitPrice = 9.50m },
                new() { Id = 11, OrderId = 1, ProductId = 2, Quantity = 1, UnitPrice = 19.99m },
            },
        };
        var order2 = new Order
        {
            Id = 2, OrderNumber = "ORD-101", CustomerId = 2,
            CreatedAt = new DateTime(2025, 2, 12, 9, 0, 0, DateTimeKind.Utc), TotalCost = 25.00m,
            Lines = new List<OrderLine>
            {
                new() { Id = 12, OrderId = 2, ProductId = 3, Quantity = 5, UnitPrice = 5.00m },
            },
        };
        var order3 = new Order
        {
            Id = 3, OrderNumber = "ORD-102", CustomerId = 2,
            CreatedAt = new DateTime(2025, 3, 1, 9, 0, 0, DateTimeKind.Utc), TotalCost = 9.50m,
            Lines = new List<OrderLine>
            {
                new() { Id = 13, OrderId = 3, ProductId = 1, Quantity = 1, UnitPrice = 9.50m },
            },
        };

        await ctx.Products.AddRangeAsync(widget, gizmo, sprocket);
        await ctx.Customers.AddRangeAsync(alice, bob, carol);
        await ctx.Orders.AddRangeAsync(order1, order2, order3);
        await ctx.SaveChangesAsync();

        ctx.DetachAll();
        return ctx;
    }
}
