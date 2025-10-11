using AuctionService; // contains the auto-generated C# classes from your .proto file
using BiddingService.Models;
using Grpc.Net.Client; // create a GrpcChannel and connect to another gRPC server

namespace BiddingService.Services;

// the gRPC client used by your BiddingService microservice to call 
// the GrpcAuctionService hosted inside the AuctionService microservice
public class GrpcAuctionClient
{
    private readonly ILogger<GrpcAuctionClient> _logger;
    private readonly IConfiguration _config;

    public GrpcAuctionClient(ILogger<GrpcAuctionClient> logger, IConfiguration config)
    {
        _logger = logger;
        _config = config;
    }

    // 1. Calls the remote gRPC service GrpcAuction.GetAuction
    // 2. Retrieves the response
    // 3. Converts it into a local Auction model used inside BiddingService
    public Auction GetAuction(string id)
    {
        _logger.LogInformation("Calling GRPC Service");

        // create a gRPC channel
        // GrpcChannel.ForAddress() creates a connection (HTTP/2 channel) to the gRPC server.
        var channel = GrpcChannel.ForAddress(_config["GrpcAuction"]);

        // Create the client stub
        // GrpcAuction.GrpcAuctionClient is an auto-generated class (from the .proto file).
        // It represents the client stub that knows how to talk to the remote gRPC server.
        var client = new GrpcAuction.GrpcAuctionClient(channel);

        // Creates a new request object (defined in .proto) with the Id of the auction you want to fetch.
        var request = new GetAuctionRequest { Id = id };

        // send the gRPC request
        try
        {
            // Call the remote gRPC method defined in .proto
            // The result reply is a GrpcAuctionResponse object that contains (defined in .proto):
            // reply.Auction.Id
            // reply.Auction.Seller
            // reply.Auction.AuctionEnd
            // reply.Auction.ReservePrice
            var reply = client.GetAuction(request);

            // Convert the reply into a local Auction model
            var auction = new Auction
            {
                ID = reply.Auction.Id,
                AuctionEnd = DateTime.Parse(reply.Auction.AuctionEnd),
                Seller = reply.Auction.Seller,
                ReservePrice = reply.Auction.ReservePrice
            };

            return auction;
        }
        catch (Exception ex)
        {
            // error handling
            _logger.LogError(ex, "Could not call GRPC Server");
            return null;
        }
    }
}


/*
Workflow:
| Step | Component                           | Description                                               |
| ---- | ----------------------------------- | --------------------------------------------------------- |
| 1️⃣  | `BiddingService`                    | Calls `GrpcAuctionClient.GetAuction(id)`                  |
| 2️⃣  | gRPC client                         | Connects to `AuctionService` using HTTP/2                 |
| 3️⃣  | `AuctionService.GrpcAuctionService` | Handles request → queries SQL DB                          |
| 4️⃣  | gRPC response                       | Returns auction data via protobuf                         |
| 5️⃣  | Client                              | Converts it into local `Auction` object                   |
| 6️⃣  | BiddingService                      | Uses the data (e.g., to verify bids, compare times, etc.) |

*/