using Contracts;
using MassTransit;
using Microsoft.AspNetCore.SignalR;
using NotificationService.Hubs;

namespace NotificationService.Consumers;

// IConsumer<AuctionCreated>: Defines a class that reacts to messages of type AuctionCreated
// IHubContext<NotificationHub>: Enables server-to-client real-time messaging using SignalR.
public class AuctionCreatedConsumer : IConsumer<AuctionCreated>
{
    private readonly IHubContext<NotificationHub> _hubContext;

    public AuctionCreatedConsumer(IHubContext<NotificationHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public async Task Consume(ConsumeContext<AuctionCreated> context)
    {
        Console.WriteLine("--> auction created message received");

        // sends the message to all connected SignalR clients (every browser connected to your NotificationHub)
        // "AuctionCreated" is the event name clients listen for on the front end (e.g., in React, connection.on("AuctionCreated", handler))
        // context.Message is the actual AuctionCreated event payload (e.g., auction ID, item info, seller).
        await _hubContext.Clients.All.SendAsync("AuctionCreated", context.Message);
    }
}


/*
Workflow:
| Step | Service                 | Action                                                                          |
| ---- | ----------------------- | ------------------------------------------------------------------------------- |
| 1    | **AuctionService**      | User creates a new auction.                                                     |
| 2    |                         | AuctionService publishes an `AuctionCreated` event to RabbitMQ via MassTransit. |
| 3    | **NotificationService** | `AuctionCreatedConsumer` receives the message.                                  |
| 4    |                         | Logs the message and sends a real-time SignalR broadcast.                       |
| 5    | **Frontend (Next.js)**  | Listens on the `"AuctionCreated"` event and updates the auction list instantly. |

*/
