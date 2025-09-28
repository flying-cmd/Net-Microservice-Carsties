using MongoDB.Entities;
using SearchService.Models;

namespace SearchService.Services;

public class AuctionSvcHttpClient
{
    private readonly HttpClient _httpClient;

    // instance of IConfiguration
    // Gives access to settings in appsettings.json or environment variables
    // In this case, it will retrieve the Auction Service base URL (AuctionSvcUrl)
    private readonly IConfiguration _config;

    public AuctionSvcHttpClient(HttpClient httpClient, IConfiguration config)
    {
        _httpClient = httpClient;
        _config = config;
    }

    // fetch the latest auction items from the Auction Service so they can be inserted/updated in the Search DB (MongoDB)
    public async Task<List<Item>> GetItemsForSearchDb()
    {
        // Find last updated timestamp from Search DB
        // DB.Find<Item, string>(): Queries the MongoDB collection of Item objects. The second generic parameter <string> means the final result type after projection will be a string.
        // .Project(x => x.UpdatedAt.ToString()): Instead of returning the whole Item, it only selects the UpdatedAt field, converted to a string
        var lastUpdated = await DB.Find<Item, string>()
            .Sort(x => x.Descending(x => x.UpdatedAt))
            .Project(x => x.UpdatedAt.ToString())
            .ExecuteFirstAsync();

        // Call Auction Service API
        return await _httpClient.GetFromJsonAsync<List<Item>>($"{_config["AuctionServiceUrl"]}/api/auctions?date={lastUpdated}");
    }
}
