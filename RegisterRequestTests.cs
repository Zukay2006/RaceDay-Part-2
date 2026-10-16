
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using RaceDay.API.DTOs;
using Xunit;

namespace RaceDay.API.Tests
{
    public class RegisterRequestTests
    {
        private static bool IsValid(RegisterRequest request)
        {
            var results = new List<ValidationResult>();
            var context = new ValidationContext(request);

            return Validator.TryValidateObject(
                request, context, results, true);
        }

        private static RegisterRequest ValidRequest()
        {
            return new RegisterRequest
            {
                FirstName = "Sipho",
                LastName = "Nkosi",
                Email = "sipho@example.com",
                Password = "Password123",
                Phone = "0712345678",
                Role = "Participant"
            };
        }

        [Fact]
        public void ValidRegistration_ShouldPassValidation()
        {
            Assert.True(IsValid(ValidRequest()));
        }

        [Theory]
        [InlineData("")]
        [InlineData("not-an-email")]
        [InlineData("sipho@")]
        public void InvalidEmail_ShouldFailValidation(string email)
        {
            var request = ValidRequest();
            request.Email = email;

            Assert.False(IsValid(request));
        }

        [Theory]
        [InlineData("")]
        [InlineData("short")]
        [InlineData("1234567")]
        public void InvalidPassword_ShouldFailValidation(string password)
        {
            var request = ValidRequest();
            request.Password = password;

            Assert.False(IsValid(request));
        }

        [Theory]
        [InlineData("")]
        [InlineData("Admin")]
        [InlineData("organiser")]
        [InlineData("Guest")]
        public void InvalidRole_ShouldFailValidation(string role)
        {
            var request = ValidRequest();
            request.Role = role;

            Assert.False(IsValid(request));
        }

        [Theory]
        [InlineData("Organiser")]
        [InlineData("Participant")]
        public void AllowedRole_ShouldPassValidation(string role)
        {
            var request = ValidRequest();
            request.Role = role;

            Assert.True(IsValid(request));
        }

        [Theory]
        [InlineData("", "Nkosi", "sipho@example.com", "Password123", "0712345678", "Participant")]
        [InlineData("Sipho", "", "sipho@example.com", "Password123", "0712345678", "Participant")]
        [InlineData("Sipho", "Nkosi", "", "Password123", "0712345678", "Participant")]
        [InlineData("Sipho", "Nkosi", "sipho@example.com", "", "0712345678", "Participant")]
        [InlineData("Sipho", "Nkosi", "sipho@example.com", "Password123", "", "Participant")]
        public void MissingRequiredField_ShouldFailValidation(
            string firstName, string lastName, string email,
            string password, string phone, string role)
        {
            var request = new RegisterRequest
            {
                FirstName = firstName,
                LastName = lastName,
                Email = email,
                Password = password,
                Phone = phone,
                Role = role
            };

            Assert.False(IsValid(request));
        }
    }
}
