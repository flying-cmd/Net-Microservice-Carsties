using AuctionService.Data;
using AuctionService.DTOs;
using AuctionService.Entities;
using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AuctionService.Controllers;

[ApiController]
[Route("api/auctions")]
public class AuctionsController : ControllerBase
{
    private readonly AuctionContext _context;
    private readonly IMapper _mapper;

    public AuctionsController(AuctionContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    // AuctionResult<T> is a wrapper type for controller action return values
    // A concrete object of type T (e.g., AuctionDto) -> returned as 200 OK with JSON.
    // A special response like NotFound(), BadRequest(), Ok(), etc.
    [HttpGet]
    public async Task<ActionResult<List<AuctionDto>>> GetAuctions()
    {
        var auctions = await _context.Auctions
            .Include(a => a.Item)
            .OrderBy(a => a.Item.Make)
            .ToListAsync();

        return _mapper.Map<List<AuctionDto>>(auctions);
    }

    [HttpGet("{id}")] // api/auctions/{id}
    public async Task<ActionResult<AuctionDto>> GetAuctionById(Guid id)
    {
        var auction = await _context.Auctions
            .Include(x => x.Item)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (auction == null)
        {
            return NotFound();
        }

        return _mapper.Map<AuctionDto>(auction);
    }

    // parameters: CreateAuctionDto auctionDto -> ASP.NET Core automatically binds the request body JSON to this DTO
    [HttpPost]
    public async Task<ActionResult<AuctionDto>> CreateAuction(CreateAuctionDto auctionDto)
    {
        // convert client’s CreateAuctionDto into an Auction entity
        var auction = _mapper.Map<Auction>(auctionDto);

        // TODO: add current user as seller
        auction.Seller = "test";

        _context.Auctions.Add(auction);

        // result is true if successful
        var result = await _context.SaveChangesAsync() > 0;

        if (!result)
        {
            // 400 Bad Request
            return BadRequest("Could not save changes to DB");
        }

        // 201 Created response
        // CreatedAtAction does 3 things:
        // 1. Sets status code = 201.
        // 2. Adds a Location header pointing to GET /api/auctions/{id}.
        //    - Uses nameof(GetAuctionById) to reference your GetAuctionById method.
        //    - Fills the {id} route parameter with auction.Id.
        // 3. Returns the created auction in the response body.
        // Example:
        /*
        HTTP/1.1 201 Created
        Content-Type: application/json; charset=utf-8
        Location: http://localhost:7001/api/auctions/7b6e3e73-1234-4af1-bcc9-abc123

        {
            "id": "7b6e3e73-1234-4af1-bcc9-abc123",
            "reservePrice": 30000,
            "seller": "test",
            "winner": null,
            "soldAmount": null,
            "currentHighBid": null,
            "createdAt": "2025-09-26T10:15:30Z",
            "updatedAt": "2025-09-26T10:15:30Z",
            "auctionEnd": "2025-12-31T23:59:59Z",
            "status": "Active",
            "make": "Tesla",
            "model": "Model 3",
            "year": 2022,
            "color": "White",
            "mileage": 10000,
            "imageUrl": "http://example.com/car.png"
        }
        */
        return CreatedAtAction(nameof(GetAuctionById), new { id = auction.Id }, _mapper.Map<AuctionDto>(auction));
    }

    [HttpPut("{id}")] // api/auctions/{id}
    public async Task<ActionResult> UpdateAuction(Guid id, UpdateAuctionDto updateAuctionDto)
    {
        var auction = await _context.Auctions.Include(x => x.Item)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (auction == null)
        {
            return NotFound();
        }

        // TODO: check seller == username

        auction.Item.Make = updateAuctionDto.Make ?? auction.Item.Make;
        auction.Item.Model = updateAuctionDto.Model ?? auction.Item.Model;
        auction.Item.Color = updateAuctionDto.Color ?? auction.Item.Color;
        auction.Item.Mileage = updateAuctionDto.Mileage ?? auction.Item.Mileage;
        auction.Item.Year = updateAuctionDto.Year ?? auction.Item.Year;

        var result = await _context.SaveChangesAsync() > 0;

        if (!result)
        {
            return BadRequest("Could not save changes to DB");
        }

        return Ok();
    }

    [HttpDelete("{id}")] // api/auctions/{id}
    public async Task<ActionResult> DeleteAuction(Guid id)
    {
        var auction = await _context.Auctions.FindAsync(id);

        if (auction == null)
        {
            return NotFound();
        }

        _context.Auctions.Remove(auction);

        var result = await _context.SaveChangesAsync() > 0;

        if (!result)
        {
            return BadRequest("Could not save changes to DB");
        }

        return Ok();
    }
}