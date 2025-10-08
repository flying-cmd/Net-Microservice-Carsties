using Microsoft.AspNetCore.Mvc;
using MongoDB.Entities;
using SearchService.Models;
using SearchService.RequestHelpers;

namespace SearchService.Controllers;

[ApiController]
[Route("api/search")]
public class SearchControllers : ControllerBase
{
    // [FromQuery] -> tells ASP.NET Core to bind query string parameters into the SearchParams object
    [HttpGet]
    public async Task<ActionResult<List<Item>>> SearchItems([FromQuery] SearchParams searchParams)
    {
        // Creates a paged search query for the Item collection
        // First Item = collection type, second Item = projection type (same here, but could be different if you only want to return part of the data).
        var query = DB.PagedSearch<Item, Item>();

        // // Sorts the results by Make property in ascending order
        // query.Sort(x => x.Ascending(a => a.Make));

        if (!string.IsNullOrEmpty(searchParams.SearchTerm))
        {
            // Performs a full text search across all fields with a text index (from 'DbInitializer' on Make, Model, and Color)
            // .SortByTextScore() -> sorts results by MongoDB's text relevance score (best matches first)
            query.Match(Search.Full, searchParams.SearchTerm).SortByTextScore();
        }

        // switch expression to sort results based on OrderBy parameter:
        // "make" → sort alphabetically by Make
        // "new" → sort by newest items first (CreatedAt descending)
        // Default (_) → sort by soonest AuctionEnd date (ascending)
        query = searchParams.OrderBy switch
        {
            "make" => query.Sort(x => x.Ascending(a => a.Make)).Sort(x => x.Ascending(a => a.Model)),
            "new" => query.Sort(x => x.Descending(a => a.CreatedAt)),
            _ => query.Sort(x => x.Ascending(a => a.AuctionEnd))
        };

        query = searchParams.FilterBy switch
        {
            "finished" => query.Match(x => x.AuctionEnd < DateTime.UtcNow),
            "endingSoon" => query.Match(x => x.AuctionEnd > DateTime.UtcNow && x.AuctionEnd < DateTime.UtcNow.AddHours(6)),
            _ => query.Match(x => x.AuctionEnd > DateTime.UtcNow)
        };

        if (!string.IsNullOrEmpty(searchParams.Seller))
        {
            query.Match(x => x.Seller == searchParams.Seller);
        }

        if (!string.IsNullOrEmpty(searchParams.Winner))
        {
            query.Match(x => x.Winner == searchParams.Winner);
        }

        // Sets pagination
        // Example: page 2, size 4 -> skips first 4 docs, returns next 4.
        query.PageNumber(searchParams.PageNumber);
        query.PageSize(searchParams.PageSize);

        var result = await query.ExecuteAsync();

        // Returns HTTP 200 with JSON response containing results, page count, and total count
        return Ok(new
        {
            results = result.Results,
            pageCount = result.PageCount,
            totalCount = result.TotalCount
        });
    }
}