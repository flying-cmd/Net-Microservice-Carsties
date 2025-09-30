using AuctionService.DTOs;
using AuctionService.Entities;
using AutoMapper;
using Contracts;

namespace AuctionService.RequestHelpers;

public class MappingProfiles : Profile
{
    public MappingProfiles()
    {
        // Merges both Auction and Item into a single DTO with all auction details
        // Auction entity has a navigation property Item, which doesn’t exist in the DTO. That’s why you need the IncludeMembers(x => x.Item)
        CreateMap<Auction, AuctionDto>().IncludeMembers(x => x.Item);
        CreateMap<Item, AuctionDto>();

        // Maps CreateAuctionDto to Auction
        // d => d.Item -> Destination member = Auction.Item (the navigation property that holds the item details)
        // o => o.MapFrom(...) -> Configures where AutoMapper should get the value from
        // s => s -> Source is the entire CreateAuctionDto object
        // When filling in the Item property of Auction, map the entire CreateAuctionDto into an Item object
        CreateMap<CreateAuctionDto, Auction>()
            .ForMember(d => d.Item, o => o.MapFrom(s => s));
        CreateMap<CreateAuctionDto, Item>();

        // Maps AuctionDto to AuctionCreated
        CreateMap<AuctionDto, AuctionCreated>();

        // IncludeMembers(x => x.Item): When mapping from Auction → AuctionUpdated, also look at the properties of Auction.Item as if they were properties of Auction itself.
        CreateMap<Auction, AuctionUpdated>().IncludeMembers(x => x.Item);
        // When use .IncludeMembers(), you also need to tell AutoMapper how to map from Item → AuctionUpdated
        CreateMap<Item, AuctionUpdated>();

        // CreateMap<UpdateAuctionDto, AuctionUpdated>();

        // CreateMap<Auction, AuctionDeleted>();
    }
}
