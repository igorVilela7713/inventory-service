using FluentAssertions;
using Inventory.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace Inventory.IntegrationTests;

public class StockApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public StockApiTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Override DbContext for testing
                services.AddDbContext<InventoryDbContext>(opts =>
                    opts.UseInMemoryDatabase("TestDb"));
            });
        }).CreateClient();
    }

    [Fact]
    public async Task GetStock_ReturnsEmpty_Initially()
    {
        var response = await _client.GetAsync("/api/v1/stock");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task CreateStock_Returns201()
    {
        var request = new { ProductId = "TEST-001", ProductName = "Test Product", Quantity = 50 };
        var response = await _client.PostAsJsonAsync("/api/v1/stock", request);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }
}
