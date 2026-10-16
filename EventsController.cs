
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.API.Data;
using RaceDay.API.Models;

namespace RaceDay.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class EventsController : ControllerBase
    {
        private readonly RaceDayContext _context;

        public EventsController(RaceDayContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Gets all events. Public access.
        /// </summary>
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetEvents()
        {
            var events = await _context.Events
                .AsNoTracking()
                .Select(e => new
                {
                    e.EventID,
                    e.OrganiserID,
                    e.EventName,
                    e.EventDate,
                    e.Location,
                    e.Description,
                    e.EventType,
                    e.DistanceKM,
                    e.Status
                })
                .ToListAsync();

            return Ok(events);
        }


        /// <summary>
        /// Retrieves a single event using its ID.
        /// This endpoint is publicly accessible.
        /// </summary>
        /// <param name="id">The ID of the event to retrieve.</param>
        /// <returns>The event details if found.</returns>
        /// <response code="200">The event was found and returned.</response>
        /// <response code="404">No event exists with the supplied ID.</response>
        [HttpGet("{id:int}")]
        [AllowAnonymous]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetEventById(int id)
        {
            
            var eventItem = await _context.Events
                .AsNoTracking()
                .Where(e => e.EventID == id)
                .Select(e => new
                {
                    e.EventID,
                    e.OrganiserID,
                    e.EventName,
                    e.EventDate,
                    e.Location,
                    e.Description,
                    e.EventType,
                    e.DistanceKM,
                    e.Status
                })
                .FirstOrDefaultAsync();

            if (eventItem == null)
                return NotFound(new
                {
                    message = "Event not found."
                });

            return Ok(eventItem);
        }

        /// <summary>
        /// Creates an event for the logged-in Organiser.
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Organiser")]
        public async Task<IActionResult> CreateEvent(Event request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // Never trust an organiser ID supplied in the request body.
            var userIdValue = User.FindFirst(
                System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            if (!int.TryParse(userIdValue, out int organiserId))
                return Unauthorized();

            request.EventID = 0;
            request.OrganiserID = organiserId;
            request.Organiser = null;

            _context.Events.Add(request);
            await _context.SaveChangesAsync();

            return CreatedAtAction(
                nameof(GetEvents),
                new { id = request.EventID },
                new
                {
                    request.EventID,
                    request.OrganiserID,
                    request.EventName,
                    request.EventDate,
                    request.Location,
                    request.Description,
                    request.EventType,
                    request.DistanceKM,
                    request.Status
                });
        }

        /// <summary>
        /// Updates an event owned by the logged-in Organiser.
        /// </summary>
        [HttpPut("{id:int}")]
        [Authorize(Roles = "Organiser")]
        public async Task<IActionResult> UpdateEvent(int id, Event request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userIdValue = User.FindFirst(
                System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            if (!int.TryParse(userIdValue, out int organiserId))
                return Unauthorized();

            var existingEvent = await _context.Events
                .FirstOrDefaultAsync(e =>
                    e.EventID == id && e.OrganiserID == organiserId);

            if (existingEvent == null)
                return NotFound(new
                {
                    message = "Event not found or you do not have permission to update it."
                });

            existingEvent.EventName = request.EventName;
            existingEvent.EventDate = request.EventDate;
            existingEvent.Location = request.Location;
            existingEvent.Description = request.Description;
            existingEvent.EventType = request.EventType;
            existingEvent.DistanceKM = request.DistanceKM;
            existingEvent.Status = request.Status;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Event updated successfully.",
                existingEvent.EventID,
                existingEvent.OrganiserID,
                existingEvent.EventName,
                existingEvent.EventDate,
                existingEvent.Location,
                existingEvent.Description,
                existingEvent.EventType,
                existingEvent.DistanceKM,
                existingEvent.Status
            });
        }

        /// <summary>
        /// Deletes an event owned by the logged-in Organiser.
        /// </summary>
        [HttpDelete("{id:int}")]
        [Authorize(Roles = "Organiser")]
        public async Task<IActionResult> DeleteEvent(int id)
        {
            var userIdValue = User.FindFirst(
                System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            if (!int.TryParse(userIdValue, out int organiserId))
                return Unauthorized();

            var existingEvent = await _context.Events
                .FirstOrDefaultAsync(e =>
                    e.EventID == id && e.OrganiserID == organiserId);

            if (existingEvent == null)
                return NotFound(new
                {
                    message = "Event not found or you do not have permission to delete it."
                });

            _context.Events.Remove(existingEvent);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Event deleted successfully." });
        }
    }
}