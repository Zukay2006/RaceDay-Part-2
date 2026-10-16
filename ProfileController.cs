
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.API.Data;
using RaceDay.API.Models;

namespace RaceDay.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Organiser,Participant")]
    public class ProfileController : ControllerBase
    {
        private readonly RaceDayContext _context;
        private readonly IPasswordHasher<Organiser> _organiserHasher;
        private readonly IPasswordHasher<Participant> _participantHasher;

        public ProfileController(RaceDayContext context)
        {
            _context = context;
            _organiserHasher = new PasswordHasher<Organiser>();
            _participantHasher = new PasswordHasher<Participant>();
        }

        // View the profile of the currently logged-in user.
        [HttpGet]
        public async Task<IActionResult> GetProfile()
        {
            var userIdValue = User.FindFirstValue(
                ClaimTypes.NameIdentifier);
            var role = User.FindFirstValue(ClaimTypes.Role);

            if (!int.TryParse(userIdValue, out int userId))
                return Unauthorized();

            if (role == "Organiser")
            {
                var organiser = await _context.Organisers
                    .AsNoTracking()
                    .FirstOrDefaultAsync(o => o.OrganiserID == userId);

                if (organiser == null)
                    return NotFound();

                return Ok(new
                {
                    organiser.OrganiserID,
                    organiser.FirstName,
                    organiser.LastName,
                    organiser.Email,
                    organiser.Phone,
                    Role = "Organiser"
                });
            }

            if (role == "Participant")
            {
                var participant = await _context.Participants
                    .AsNoTracking()
                    .FirstOrDefaultAsync(p => p.ParticipantID == userId);

                if (participant == null)
                    return NotFound();

                return Ok(new
                {
                    participant.ParticipantID,
                    participant.FirstName,
                    participant.LastName,
                    participant.Email,
                    participant.Phone,
                    Role = "Participant"
                });
            }

            return Forbid();
        }

        // Update the profile of the currently logged-in user.
        [HttpPut]
        public async Task<IActionResult> UpdateProfile(
            UpdateProfileRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (!string.IsNullOrEmpty(request.Password) &&
    request.Password.Length < 8)
            {
                return BadRequest(new
                {
                    message = "A new password must contain at least 8 characters."
                });
            }

            var userIdValue = User.FindFirstValue(
                ClaimTypes.NameIdentifier);
            var role = User.FindFirstValue(ClaimTypes.Role);

            if (!int.TryParse(userIdValue, out int userId))
                return Unauthorized();

            string email = request.Email.Trim().ToLowerInvariant();

            if (role == "Organiser")
            {
                var organiser = await _context.Organisers
                    .FirstOrDefaultAsync(o => o.OrganiserID == userId);

                if (organiser == null)
                    return NotFound();

                bool emailInUse = await _context.Organisers
                    .AnyAsync(o => o.OrganiserID != userId &&
                                   o.Email.ToLower() == email)
                    || await _context.Participants
                    .AnyAsync(p => p.Email.ToLower() == email);

                if (emailInUse)
                    return Conflict(new
                    {
                        message = "That email address is already in use."
                    });

                organiser.FirstName = request.FirstName.Trim();
                organiser.LastName = request.LastName.Trim();
                organiser.Email = email;
                organiser.Phone = request.Phone.Trim();

                if (!string.IsNullOrWhiteSpace(request.Password))
                {
                    organiser.PasswordHash =
                        _organiserHasher.HashPassword(
                            organiser, request.Password);
                }
            }
            else if (role == "Participant")
            {
                var participant = await _context.Participants
                    .FirstOrDefaultAsync(p => p.ParticipantID == userId);

                if (participant == null)
                    return NotFound();

                bool emailInUse = await _context.Participants
                    .AnyAsync(p => p.ParticipantID != userId &&
                                   p.Email.ToLower() == email)
                    || await _context.Organisers
                    .AnyAsync(o => o.Email.ToLower() == email);

                if (emailInUse)
                    return Conflict(new
                    {
                        message = "That email address is already in use."
                    });

                participant.FirstName = request.FirstName.Trim();
                participant.LastName = request.LastName.Trim();
                participant.Email = email;
                participant.Phone = request.Phone.Trim();

                if (!string.IsNullOrWhiteSpace(request.Password))
                {
                    participant.PasswordHash =
                        _participantHasher.HashPassword(
                            participant, request.Password);
                }
            }
            else
            {
                return Forbid();
            }

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Profile updated successfully. Log in again if your email address changed."
            });
        }
    }

    public class UpdateProfileRequest
    {
        [System.ComponentModel.DataAnnotations.Required]
        [System.ComponentModel.DataAnnotations.StringLength(50)]
        public string FirstName { get; set; } = string.Empty;

        [System.ComponentModel.DataAnnotations.Required]
        [System.ComponentModel.DataAnnotations.StringLength(50)]
        public string LastName { get; set; } = string.Empty;

        [System.ComponentModel.DataAnnotations.Required]
        [System.ComponentModel.DataAnnotations.EmailAddress]
        [System.ComponentModel.DataAnnotations.StringLength(100)]
        public string Email { get; set; } = string.Empty;

        [System.ComponentModel.DataAnnotations.Required]
        [System.ComponentModel.DataAnnotations.StringLength(20)]
        public string Phone { get; set; } = string.Empty;

        public string? Password { get; set; }
    }
}