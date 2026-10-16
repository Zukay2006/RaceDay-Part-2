
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.API.Data;
using RaceDay.API.Models;

namespace RaceDay.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CategoriesController : ControllerBase
    {
        private readonly RaceDayContext _context;

        public CategoriesController(RaceDayContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Gets all categories with their associated event.
        /// </summary>
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetCategories()
        {
            var categories = await _context.Categories
                .AsNoTracking()
                .Select(c => new
                {
                    c.CategoryID,
                    c.EventID,
                    c.CategoryName,
                    c.ActivityType,
                    c.DistanceKM,
                    c.EntryFee,
                    EventName = c.Event != null ? c.Event.EventName : null
                })
                .ToListAsync();

            return Ok(categories);
        }

        /// <summary>
        /// Creates a category for an event owned by the logged-in Organiser.
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Organiser")]
        public async Task<IActionResult> CreateCategory(Category request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!int.TryParse(userIdValue, out int organiserId))
                return Unauthorized();

            var eventExists = await _context.Events.AnyAsync(e =>
                e.EventID == request.EventID &&
                e.OrganiserID == organiserId);

            if (!eventExists)
                return NotFound(new
                {
                    message = "Event not found or you do not own this event."
                });

            var category = new Category
            {
                EventID = request.EventID,
                CategoryName = request.CategoryName,
                ActivityType = request.ActivityType,
                DistanceKM = request.DistanceKM,
                EntryFee = request.EntryFee
            };

            _context.Categories.Add(category);
            await _context.SaveChangesAsync();

            return CreatedAtAction(
                nameof(GetCategories),
                new { id = category.CategoryID },
                new
                {
                    category.CategoryID,
                    category.EventID,
                    category.CategoryName,
                    category.ActivityType,
                    category.DistanceKM,
                    category.EntryFee
                });
        }

        /// <summary>
        /// Updates a category belonging to an event owned by the logged-in Organiser.
        /// </summary>
        [HttpPut("{id:int}")]
        [Authorize(Roles = "Organiser")]
        public async Task<IActionResult> UpdateCategory(int id, Category request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!int.TryParse(userIdValue, out int organiserId))
                return Unauthorized();

            var category = await _context.Categories
                .Include(c => c.Event)
                .FirstOrDefaultAsync(c => c.CategoryID == id);

            if (category == null ||
                category.Event == null ||
                category.Event.OrganiserID != organiserId)
            {
                return NotFound(new
                {
                    message = "Category not found or you do not own its event."
                });
            }

            // Do not allow moving a category to another event through this request.
            category.CategoryName = request.CategoryName;
            category.ActivityType = request.ActivityType;
            category.DistanceKM = request.DistanceKM;
            category.EntryFee = request.EntryFee;

            await _context.SaveChangesAsync();

            return Ok(new { message = "Category updated successfully." });
        }

        /// <summary>
        /// Deletes a category belonging to an event owned by the logged-in Organiser.
        /// </summary>
        [HttpDelete("{id:int}")]
        [Authorize(Roles = "Organiser")]
        public async Task<IActionResult> DeleteCategory(int id)
        {
            var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!int.TryParse(userIdValue, out int organiserId))
                return Unauthorized();

            var category = await _context.Categories
                .Include(c => c.Event)
                .FirstOrDefaultAsync(c => c.CategoryID == id);

            if (category == null ||
                category.Event == null ||
                category.Event.OrganiserID != organiserId)
            {
                return NotFound(new
                {
                    message = "Category not found or you do not own its event."
                });
            }

            _context.Categories.Remove(category);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Category deleted successfully." });
        }
    }
}