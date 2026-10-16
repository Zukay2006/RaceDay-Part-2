using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using RaceDay.API.Data;
using RaceDay.API.DTOs;
using RaceDay.API.Models;

namespace RaceDay.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly RaceDayContext _context;
        private readonly IPasswordHasher<Organiser> _organiserHasher;
        private readonly IPasswordHasher<Participant> _participantHasher;

        public AuthController(RaceDayContext context)
        {
            _context = context;
            _organiserHasher = new PasswordHasher<Organiser>();
            _participantHasher = new PasswordHasher<Participant>();
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            string email = request.Email.Trim().ToLowerInvariant();
            string role = request.Role.Trim();

            bool organiserExists = await _context.Organisers
                .AnyAsync(o => o.Email.ToLower() == email);

            bool participantExists = await _context.Participants
                .AnyAsync(p => p.Email.ToLower() == email);

            if (organiserExists || participantExists)
                return Conflict(new { message = "This email address is already registered." });

            if (role == "Organiser")
            {
                var organiser = new Organiser
                {
                    FirstName = request.FirstName.Trim(),
                    LastName = request.LastName.Trim(),
                    Email = email,
                    Phone = request.Phone.Trim()
                };

                organiser.PasswordHash = _organiserHasher.HashPassword(
                    organiser, request.Password);

                _context.Organisers.Add(organiser);
            }
            else if (role == "Participant")
            {
                var participant = new Participant
                {
                    FirstName = request.FirstName.Trim(),
                    LastName = request.LastName.Trim(),
                    Email = email,
                    Phone = request.Phone.Trim()
                };

                participant.PasswordHash = _participantHasher.HashPassword(
                    participant, request.Password);

                _context.Participants.Add(participant);
            }
            else
            {
                return BadRequest(new { message = "Role must be Organiser or Participant." });
            }

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Registration successful. You can now log in.",
                email,
                role
            });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            string email = request.Email.Trim().ToLowerInvariant();
            string role = request.Role.Trim();

            int userId;
            string firstName;
            string lastName;
            string storedPassword;

            if (role == "Organiser")
            {
                var organiser = await _context.Organisers
                    .FirstOrDefaultAsync(o => o.Email.ToLower() == email);

                if (organiser == null)
                    return Unauthorized(new { message = "Invalid email, password, or role." });

                storedPassword = organiser.PasswordHash;

                if (!VerifyPassword(organiser, request.Password, storedPassword))
                    return Unauthorized(new { message = "Invalid email, password, or role." });

                // Upgrade an old plain-text password after successful login.
                if (!storedPassword.StartsWith("AQAAAA"))
                {
                    organiser.PasswordHash = _organiserHasher.HashPassword(
                        organiser, request.Password);

                    await _context.SaveChangesAsync();
                }

                userId = organiser.OrganiserID;
                firstName = organiser.FirstName;
                lastName = organiser.LastName;
            }
            else if (role == "Participant")
            {
                var participant = await _context.Participants
                    .FirstOrDefaultAsync(p => p.Email.ToLower() == email);

                if (participant == null)
                    return Unauthorized(new { message = "Invalid email, password, or role." });

                storedPassword = participant.PasswordHash;

                if (!VerifyPassword(participant, request.Password, storedPassword))
                    return Unauthorized(new { message = "Invalid email, password, or role." });

                // Upgrade an old plain-text password after successful login.
                if (!storedPassword.StartsWith("AQAAAA"))
                {
                    participant.PasswordHash = _participantHasher.HashPassword(
                        participant, request.Password);

                    await _context.SaveChangesAsync();
                }

                userId = participant.ParticipantID;
                firstName = participant.FirstName;
                lastName = participant.LastName;
            }
            else
            {
                return BadRequest(new { message = "Role must be Organiser or Participant." });
            }

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim(ClaimTypes.Name, email),
                new Claim(ClaimTypes.Role, role),
                new Claim("FirstName", firstName),
                new Claim("LastName", lastName)
            };

            var identity = new ClaimsIdentity(
                claims, CookieAuthenticationDefaults.AuthenticationScheme);

            var principal = new ClaimsPrincipal(identity);

            // Start a fresh server-side session for the logged-in user.
            HttpContext.Session.Clear();

            HttpContext.Session.SetInt32("UserId", userId);
            HttpContext.Session.SetString("Role", role);

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                principal);

            return Ok(new
            {
                message = "Login successful.",
                userId,
                firstName,
                lastName,
                email,
                role
            });
        }

        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            HttpContext.Session.Clear();
            await HttpContext.SignOutAsync(
                CookieAuthenticationDefaults.AuthenticationScheme);

            return Ok(new { message = "Logout successful." });
        }

        private bool VerifyPassword(
    Organiser organiser, string password, string storedPassword)
        {
            if (!storedPassword.StartsWith("AQAAAA"))
                return storedPassword == password;

            var result = _organiserHasher.VerifyHashedPassword(
                organiser, storedPassword, password);

            return result != PasswordVerificationResult.Failed;
        }

        private bool VerifyPassword(
            Participant participant, string password, string storedPassword)
        {
            if (!storedPassword.StartsWith("AQAAAA"))
                return storedPassword == password;

            var result = _participantHasher.VerifyHashedPassword(
                participant, storedPassword, password);

            return result != PasswordVerificationResult.Failed;
        }
    }
}