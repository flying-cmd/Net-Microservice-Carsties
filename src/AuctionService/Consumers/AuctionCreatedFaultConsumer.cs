using Contracts;
using MassTransit;

namespace AuctionService.Consumers;

//workflow:
// 1. AuctionService publishes AuctionCreated.
// 2. SearchService tries to process it → fails.
// 3. MassTransit publishes Fault<AuctionCreated>.
// 4. AuctionService(or any other service interested in recovery) listens for that fault and decides what to do (e.g., correct data, republish, or alert).

// IConsumer<Fault<AuctionCreated>> means it will receive messages of type Fault<AuctionCreated>
// In MassTransit, when a message (like AuctionCreated) is consumed but fails (e.g., throws an exception), MassTransit automatically publishes a Fault<T> message.
public class AuctionCreatedFaultConsumer : IConsumer<Fault<AuctionCreated>>
{
    // context gives:
    // context.Message → the actual fault message.
    // context.Message.Message → the original AuctionCreated that failed.
    // context.Message.Exceptions → a list of exception details.
    public async Task Consume(ConsumeContext<Fault<AuctionCreated>> context)
    {
        Console.WriteLine("--> Consuming faulty create.");

        // Gets the first exception that caused the fault
        var exception = context.Message.Exceptions.First();

        // If the failure was caused by an ArgumentException
        if (exception.ExceptionType == "System.ArgumentException")
        {
            // Take the original AuctionCreated message (context.Message.Message)
            // Modify its Model property to "FooBar"
            context.Message.Message.Model = "FooBar";

            // Republish the modified message
            await context.Publish(context.Message.Message);
        }
        else
        {
            Console.WriteLine("Unknown fault");
        }
    }
}