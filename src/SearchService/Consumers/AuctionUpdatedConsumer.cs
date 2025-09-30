using AutoMapper;
using Contracts;
using MassTransit;
using MongoDB.Entities;
using SearchService.Models;

namespace SearchService.Consumers;

public class AuctionUpdatedConsumer : IConsumer<AuctionUpdated>
{
    private readonly IMapper _mapper;

    public AuctionUpdatedConsumer(IMapper mapper)
    {
        _mapper = mapper;
    }

    public async Task Consume(ConsumeContext<AuctionUpdated> context)
    {
        Console.WriteLine("--> Consuming auction updated: " + context.Message.Id);

        var item = _mapper.Map<Item>(context.Message);

        // await DB.Update<Item>()
        //     .Match(a => a.ID == updatedAuction.Id)
        //     .Modify(a => a.Make, updatedAuction.Make)
        //     .Modify(a => a.Model, updatedAuction.Model)
        //     .Modify(a => a.Color, updatedAuction.Color)
        //     .Modify(a => a.Mileage, updatedAuction.Mileage)
        //     .Modify(a => a.Year, updatedAuction.Year)
        //     .ExecuteAsync();

        var result = await DB.Update<Item>()
            .Match(a => a.ID == context.Message.Id)
            .ModifyOnly(x => new
            {
                x.Make,
                x.Model,
                x.Color,
                x.Mileage,
                x.Year
            }, item)
            .ExecuteAsync();

        if (!result.IsAcknowledged)
        {
            throw new MessageException(typeof(AuctionUpdated), "Problem updating auction");
        }
    }
}