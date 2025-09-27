using AuctionService.DTOs;
using AuctionService.Entities;
using AutoMapper;

namespace AuctionService.RequestHelpers;

public class MappingProfiles : Profile
{
    public MappingProfiles()
    {
        // Merges both Auction and Item into a single DTO with all auction details
        // Auction entity has a navigation property Item, which doesn’t exist in the DTO. That’s why you need the IncludeMembers(x => x.Item)
        CreateMap<Auction, AuctionDto>().IncludeMembers(x => x.Item);
        CreateMap<Item, AuctionDto>();

        // d => d.Item -> Destination member = Auction.Item (the navigation property that holds the item details)
        // o => o.MapFrom(...) -> Configures where AutoMapper should get the value from
        // s => s -> Source is the entire CreateAuctionDto object
        // When filling in the Item property of Auction, map the entire CreateAuctionDto into an Item object
        CreateMap<CreateAuctionDto, Auction>()
            .ForMember(d => d.Item, o => o.MapFrom(s => s));
        CreateMap<CreateAuctionDto, Item>();
    }
}
