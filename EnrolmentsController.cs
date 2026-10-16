
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.API.Data;
using RaceDay.API.DTOs;
using RaceDay.API.Models;

namespace RaceDay.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class EnrolmentsController : ControllerBase
    {
        private readonly RaceDayContext _context;

        public EnrolmentsController(RaceDayContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Gets enrolments for events owned by the logged-in Organiser.
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "Organiser")]
        public async Task<IActionResult> GetEnrolments()
        {
            var userIdValue = User.FindFirstValue(
                ClaimTypes.NameIdentifier);

            if (!int.TryParse(userIdValue, out int organiserId))
                return Unauthorized();

            var enrolments = await _context.Enrolments
                .AsNoTracking()
                .Where(e => e.Category != null &&
                            e.Category.Event != null &&
                            e.Category.Event.OrganiserID == organiserId)
                .Select(e => new
                {
                    e.EnrolmentID,
                    e.ParticipantID,
                    EventID = e.CategoryID != 0
                        ? e.Category!.EventID : 0,
                    e.CategoryID,
                    e.EnrolmentDate,
                    e.EnrolmentStatus
                })
                .ToListAsync();

            return Ok(enrolments);
        }


        /// <summary>
        /// Gets enrolments belonging to the logged-in Participant.
        /// </summary>
        [HttpGet("my")]
        [Authorize(Roles = "Participant")]
        public async Task<IActionResult> GetMyEnrolments()
        {
            var userIdValue = User.FindFirstValue(
                ClaimTypes.NameIdentifier);

            if (!int.TryParse(userIdValue, out int participantId))
                return Unauthorized();

            var enrolments = await _context.Enrolments
                .AsNoTracking()
                .Where(e => e.ParticipantID == participantId)
                .Select(e => new
                {
                    e.EnrolmentID,
                    e.ParticipantID,
                    EventID = e.Category!.EventID,
                    EventName = e.Category!.Event!.EventName,
                    e.CategoryID,
                    CategoryName = e.Category!.CategoryName,
                    e.EnrolmentDate,
                    e.EnrolmentStatus
                })
                .ToListAsync();

            return Ok(enrolments);
        }


        /// <summary>
        /// Allows the logged-in Participant to enrol in a category
        /// belonging to the selected event.
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Participant")]
        public async Task<IActionResult> CreateEnrolment(
            EnrolmentRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userIdValue = User.FindFirstValue(
                ClaimTypes.NameIdentifier);

            if (!int.TryParse(userIdValue, out int participantId))
                return Unauthorized();

            // Confirm that the selected category belongs to the event.
            var categoryExists = await _context.Categories
                .AnyAsync(c =>
                    c.CategoryID == request.CategoryID &&
                    c.EventID == request.EventID);

            if (!categoryExists)
            {
                return BadRequest(new
                {
                    message = "The selected category does not belong to this event."
                });
            }

            // Prevent duplicate enrolment in the same category.
            var alreadyEnrolled = await _context.Enrolments
                .AnyAsync(e =>
                    e.ParticipantID == participantId &&
                    e.CategoryID == request.CategoryID);

            if (alreadyEnrolled)
            {
                return Conflict(new
                {
                    message = "You are already enrolled in this category."
                });
            }

            var enrolment = new Enrolment
            {
                ParticipantID = participantId,
                CategoryID = request.CategoryID,
                EnrolmentDate = DateTime.Today,
                EnrolmentStatus = "Active"
            };

            _context.Enrolments.Add(enrolment);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Enrolment successful.",
                enrolment.EnrolmentID,
                enrolment.ParticipantID,
                EventID = request.EventID,
                enrolment.CategoryID,
                enrolment.EnrolmentDate,
                enrolment.EnrolmentStatus
            });
        }
    }
}
