using System.Text.Json;
using MongoDB.Driver;
using MongoDB.Entities;
using SearchService.Models;
using SearchService.Services;

namespace SearchService.Data;

public class DbInitializer
{
    // The method accepts a WebApplication object, so it has access to configuration and environment info
    public static async Task InitDb(WebApplication app)
    {
        await DB.InitAsync("SearchDb", MongoClientSettings.FromConnectionString(app.Configuration.GetConnectionString("MongoDbConnection")));

        // creates a text index on the Item collection (the MongoDB collection for the Item class)
        // DB.Index<Item>() → tells MongoDB.Entities you want to define an index for the Item entity/collection
        // .Key(x => x.Make, KeyType.Text) → adds a text index on the Make property.
        // .Key(x => x.Model, KeyType.Text) → adds a text index on the Model property.
        // .Key(x => x.Color, KeyType.Text) → adds a text index on the Color property.
        // In MongoDB, text indexes are special indexes that allow you to perform text searches (similar to full-text search)
        // So later, you can do things like: var results = await DB.SearchText<Item>("red toyota").ExecuteAsync();
        // and it will look through the Make, Model, and Color fields for matches.
        await DB.Index<Item>()
            .Key(x => x.Make, KeyType.Text)
            .Key(x => x.Model, KeyType.Text)
            .Key(x => x.Color, KeyType.Text)
            .CreateAsync();

        var count = await DB.CountAsync<Item>();

        // Seeds data if collection is empty
        // if (count == 0)
        // {
        //     Console.WriteLine("No data in Item collection. Seeding data...");
        //     var itemData = await File.ReadAllTextAsync("Data/auctions.json");

        //     // Sets up JSON options so property name matching is case-insensitive (handles variations like "make" vs "Make")
        //     var options = new JsonSerializerOptions
        //     {
        //         PropertyNameCaseInsensitive = true
        //     };

        //     var items = JsonSerializer.Deserialize<List<Item>>(itemData, options)!;

        //     await DB.SaveAsync(items);
        // }

        // Get Items from Auction Service
        // Creates a scope so you can resolve scoped/transient services from DI (like AuctionSvcHttpClient)
        using var scope = app.Services.CreateScope();

        var httpClient = scope.ServiceProvider.GetRequiredService<AuctionSvcHttpClient>();

        var items = await httpClient.GetItemsForSearchDb();

        Console.WriteLine($"Loaded {items.Count} items from Auction Service.");

        if (items.Count > 0)
        {
            await DB.SaveAsync(items);
        }
    }
}