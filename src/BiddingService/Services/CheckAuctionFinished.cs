// a background service in BiddingService microservice that periodically checks for auctions that have ended, 
// updates their status in MongoDB, and publishes an event message (via MassTransit + RabbitMQ) so other microservices
// can react (e.g., AuctionService, SearchService, etc.)

using BiddingService.Models;
using Contracts;
using MassTransit;
using MongoDB.Entities;

namespace BiddingService.Services;


// BackgroundService lets you run continuous background tasks that start 
// when the app starts and stop gracefully when the app stops.
public class CheckAuctionFinished : BackgroundService
{
    private readonly ILogger<CheckAuctionFinished> _logger;
    private readonly IServiceProvider _services;

    // IServiceProvider → allows you to create a new DI scope later to resolve scoped services
    // Why use IServiceProvider?
    // Because BackgroundService itself is a singleton — and if you inject scoped services directly 
    // (like DbContext or IPublishEndpoint), you’ll get lifetime conflicts. So instead, you use 
    // IServiceProvider.CreateScope() each loop to get fresh scoped instances safely.
    public CheckAuctionFinished(ILogger<CheckAuctionFinished> logger, IServiceProvider services)
    {
        _logger = logger;
        _services = services;
    }

    // ASP.NET Core calls ExecuteAsync() automatically when the app starts.
    // The CancellationToken allows graceful shutdown
    // You pass stoppingToken to any async method that supports cancellation — so that 
    // the operation can abort early if the application is stopping.
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Starting check for finished auctions");

        stoppingToken.Register(() => _logger.LogInformation("==> Auction check is stopping"));

        // When the app is shutting down, the cancellation token is triggered, the loop exits, and the task completes gracefully.
        while (!stoppingToken.IsCancellationRequested)
        {
            await CheckAuctions(stoppingToken);

            // wait 5 seconds between each check
            await Task.Delay(5000, stoppingToken);
        }
    }

    private async Task CheckAuctions(CancellationToken stoppingToken)
    {
        var finishedAuctions = await DB.Find<Auction>()
            .Match(x => x.AuctionEnd <= DateTime.UtcNow)
            .Match(x => !x.Finished)
            .ExecuteAsync(stoppingToken);

        if (finishedAuctions.Count == 0) return;

        _logger.LogInformation("==> Found {count} auctions that have completed", finishedAuctions.Count);

        // CreateScope() creates a new scoped dependency lifetime
        using var scope = _services.CreateScope();
        // From that scope, it resolves IPublishEndpoint (MassTransit service used to publish messages).
        var endpoint = scope.ServiceProvider.GetRequiredService<IPublishEndpoint>();

        foreach (var auction in finishedAuctions)
        {
            auction.Finished = true;
            await auction.SaveAsync(null, stoppingToken);

            var winningBid = await DB.Find<Bid>()
                .Match(a => a.AuctionId == auction.ID)
                .Match(b => b.BidStatus == BidStatus.Accepted)
                .Sort(x => x.Descending(s => s.Amount))
                .ExecuteFirstAsync(stoppingToken);
            // By passing stoppingToken, you’re telling MongoDB driver:
            // “If the service is shutting down, cancel this query immediately.”
            // Prevents hanging database calls during shutdown and allows your background task to stop quickly and cleanly.


            await endpoint.Publish(new AuctionFinished
            {
                ItemSold = winningBid != null,
                AuctionId = auction.ID,
                Winner = winningBid?.Bidder,
                Amount = winningBid?.Amount,
                Seller = auction.Seller
            }, stoppingToken);
        }
    }
}
