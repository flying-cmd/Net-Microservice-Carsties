using AuctionService.Data;
using AuctionService.DTOs;
using AuctionService.Entities;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Contracts;
using MassTransit;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AuctionService.Controllers;

[ApiController]
[Route("api/auctions")]
public class AuctionsController : ControllerBase
{
    private readonly AuctionContext _context;
    private readonly IMapper _mapper;
    // An IPublishEndpoint lets you publish events (messages) into the message bus, so other services can listen and react to them
    private readonly IPublishEndpoint _publishEndpoint;

    public AuctionsController(AuctionContext context, IMapper mapper, IPublishEndpoint publishEndpoint)
    {
        _context = context;
        _mapper = mapper;
        _publishEndpoint = publishEndpoint;
    }

    // AuctionResult<T> is a wrapper type for controller action return values
    // A concrete object of type T (e.g., AuctionDto) -> returned as 200 OK with JSON.
    // A special response like NotFound(), BadRequest(), Ok(), etc.
    [HttpGet]
    public async Task<ActionResult<List<AuctionDto>>> GetAllAuctions(string date)
    {
        var query = _context.Auctions.OrderBy(x => x.Item.Make).AsQueryable();

        if (!string.IsNullOrEmpty(date))
        {
            query = query.Where(x => x.UpdatedAt.CompareTo(DateTime.Parse(date).ToUniversalTime()) > 0);
        }

        // var auctions = await _context.Auctions
        //     .Include(a => a.Item)
        //     .OrderBy(a => a.Item.Make)
        //     .ToListAsync();

        // return _mapper.Map<List<AuctionDto>>(auctions);

        // AutoMapper generates SQL that directly selects only the fields needed for AuctionDto

        return await query.ProjectTo<AuctionDto>(_mapper.ConfigurationProvider).ToListAsync();
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

        // convert Auction entity into AuctionDto for publishing to message bus
        var newAuction = _mapper.Map<AuctionDto>(auction);

        // publish AuctionCreated event. Looks like you're publishing to RabbitMQ now, but because of UseBusOutbox():
        // MassTransit intercepts the publish call.
        // Instead of sending the message to RabbitMQ immediately, it stores the message in the Outbox (via EF).
        // The publish is tied to the current EF transaction.
        // When you call _publishEndpoint.Publish(...) inside a DbContext transaction:
        // 1. The publish action does not immediately send to RabbitMQ.
        // 2. Instead, the message is recorded in the Outbox table of your database.
        //     - It's part of the same EF transaction as your entity (Auction)
        //     - If the transaction rolls back, the outbox message is rolled back too.
        // 3. When SaveChangesAsync() succeeds:
        //     - The entity is saved to the Auctions table.
        //     - The outbox message is saved to the Outbox table.
        // 4. Later, the Outbox processor (polling every QueryDelay, e.g., 10 seconds) reads the message from the Outbox table and publishes it to RabbitMQ.
        // 5. If SaveChangesAsync() fails:
        //     - Neither the entity nor the outbox message is saved.
        //     - So nothing is published → consistency is preserved.
        await _publishEndpoint.Publish(_mapper.Map<AuctionCreated>(newAuction));

        // result is true if successful
        // Inserts Auction into Auctions table, and Inserts the pending AuctionCreated event into Outbox table
        // Or nothing saved if SaveChangesAsync() fails
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
        return CreatedAtAction(nameof(GetAuctionById), new { id = auction.Id }, newAuction);
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