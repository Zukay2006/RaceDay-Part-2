
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
    public class ResultsController : ControllerBase
    {
        private readonly RaceDayContext _context;

        public ResultsController(RaceDayContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Participants can view only their own results.
        /// Organisers can view results for their own events.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetResults()
        {
            var userIdValue = User.FindFirstValue(
                ClaimTypes.NameIdentifier);
            var role = User.FindFirstValue(ClaimTypes.Role);

            if (!int.TryParse(userIdValue, out int userId))
                return Unauthorized();

            var query = _context.Results.AsNoTracking();

            if (role == "Participant")
            {
                query = query.Where(r =>
                    r.Enrolment != null &&
                    r.Enrolment.ParticipantID == userId);
            }
            else if (role == "Organiser")
            {
                query = query.Where(r =>
                    r.Enrolment != null &&
                    r.Enrolment.Category != null &&
                    r.Enrolment.Category.Event != null &&
                    r.Enrolment.Category.Event.OrganiserID == userId);
            }
            else
            {
                return Forbid();
            }

            var results = await query
                .Select(r => new
                {
                    r.ResultID,
                    r.EnrolmentID,
                    r.FinishTime,
                    r.Position,
                    r.ResultStatus
                })
                .ToListAsync();

            return Ok(results);
        }

        /// <summary>
        /// Allows an Organiser to record or update a result
        /// for an enrolment belonging to one of their own events.
        /// </summary>
        [HttpPut("enrolment/{enrolmentId:int}")]
        [Authorize(Roles = "Organiser")]
        public async Task<IActionResult> SaveResult(
            int enrolmentId,
            ResultRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (request.FinishTime.HasValue &&
                request.FinishTime.Value < TimeSpan.Zero)
            {
                return BadRequest(new
                {
                    message = "Finish time cannot be negative."
                });
            }

            var userIdValue = User.FindFirstValue(
                ClaimTypes.NameIdentifier);

            if (!int.TryParse(userIdValue, out int organiserId))
                return Unauthorized();

            var enrolment = await _context.Enrolments
                .Include(e => e.Category)
                    .ThenInclude(c => c!.Event)
                .FirstOrDefaultAsync(e =>
                    e.EnrolmentID == enrolmentId);

            if (enrolment == null ||
                enrolment.Category?.Event == null ||
                enrolment.Category.Event.OrganiserID != organiserId)
            {
                return NotFound(new
                {
                    message = "Enrolment not found or you do not own its event."
                });
            }

            var result = await _context.Results
                .FirstOrDefaultAsync(r =>
                    r.EnrolmentID == enrolmentId);

            if (result == null)
            {
                result = new Result
                {
                    EnrolmentID = enrolmentId
                };

                _context.Results.Add(result);
            }

            result.FinishTime = request.FinishTime;
            result.Position = request.Position;
            result.ResultStatus = request.ResultStatus.Trim();

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Result saved successfully.",
                result.ResultID,
                result.EnrolmentID,
                result.FinishTime,
                result.Position,
                result.ResultStatus
            });
        }
    }
}
