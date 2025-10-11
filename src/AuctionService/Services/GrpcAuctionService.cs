using AuctionService.Data;
using Grpc.Core;

namespace AuctionService.Services;

// GrpcAuction.GrpcAuctionBase is the abstract base class automatically generated from the .proto definition.
public class GrpcAuctionService : GrpcAuction.GrpcAuctionBase
{
    // AuctionContext is your EF Core database context that manages the Auctions table.
    private readonly AuctionContext _dbContext;

    public GrpcAuctionService(AuctionContext dbContext)
    {
        _dbContext = dbContext;
    }

    // Overrides the virtual method defined in GrpcAuctionBase
    // request: the GetAuctionRequest message sent from the client (contains id string)
    // context: provides information about the gRPC call (metadata, deadline, cancellation, etc.)
    // Return type: Task<GrpcAuctionResponse> → asynchronous gRPC response message.
    public override async Task<GrpcAuctionResponse> GetAuction(GetAuctionRequest request,
        ServerCallContext context)
    {
        Console.WriteLine("==> Received Grpc request for auction");

        var auction = await _dbContext.Auctions.FindAsync(Guid.Parse(request.Id)) ?? throw new RpcException(new Status(StatusCode.NotFound, "Not found"));

        // Builds the gRPC response message expected by the client
        // GrpcAuctionResponse and GrpcAuctionModel are C# classes auto-generated from your .proto file.
        var response = new GrpcAuctionResponse
        {
            Auction = new GrpcAuctionModel
            {
                AuctionEnd = auction.AuctionEnd.ToString(), // Converted to string (because gRPC field type is string)
                Id = auction.Id.ToString(), // Converted to string (Guid → string)
                ReservePrice = auction.ReservePrice,
                Seller = auction.Seller
            }
        };

        return response;
    }
}
