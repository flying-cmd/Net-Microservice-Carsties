using AutoMapper;
using Contracts;
using MassTransit;
using MongoDB.Entities;
using SearchService.Models;

namespace SearchService.Consumers;

// Workflow:
// 1. AuctionService publishes an AuctionCreated event after a new auction is created.
// 2. SearchService receives the event through this consumer
// 3. The event data is transformed and stored locally in MongoDB so that the SearchService has its own copy optimized for searching

// Defines a consumer that listens for messages of type AuctionCreated
// AuctionCreated is a message contract (likely a DTO representing an event published by another microservice, e.g., AuctionService)
public class AuctionCreatedConsumer : IConsumer<AuctionCreated>
{
    private readonly IMapper _mapper;

    public AuctionCreatedConsumer(IMapper mapper)
    {
        _mapper = mapper;
    }

    // ConsumeContext<AuctionCreated> context contains the full message and metadata
    // context.Message gives the actual AuctionCreated event data (e.g., auction ID, title, price)
    public async Task Consume(ConsumeContext<AuctionCreated> context)
    {
        Console.WriteLine("--> Consuming auction created: " + context.Message.Id);

        // Convert AuctionCreated to MongoDB entity Item
        var item = _mapper.Map<Item>(context.Message);

        // mimic a exception
        if (item.Model == "foo")
        {
            throw new ArgumentException("Cannot sell cars with model Foo");
        }

        await item.SaveAsync();
    }
}
