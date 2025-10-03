using Contracts;
using MassTransit;
using MongoDB.Entities;
using SearchService.Models;

namespace SearchService.Consumers;

public class BidPlacedConsumer : IConsumer<BidPlaced>
{
    public async Task Consume(ConsumeContext<BidPlaced> context)
    {
        Console.WriteLine("--> Consuming bid placed: " + context.Message.AuctionId);

        // DB.Find<Item>() → tells MongoDB we want to search for a document in the Item collection.
        // .OneAsync(context.Message.AuctionId) → fetches the document with the given AuctionId
        // Find<T>() returns a query builder object that you can add filters, sorting, projection, etc. to.
        var auction = await DB.Find<Item>().OneAsync(context.Message.AuctionId);

        if (auction.CurrentHighBid == null
            || context.Message.BidStatus.Contains("Accepted")
            && context.Message.Amount > auction.CurrentHighBid)
        {
            auction.CurrentHighBid = context.Message.Amount;
            await auction.SaveAsync();
        }
    }
}