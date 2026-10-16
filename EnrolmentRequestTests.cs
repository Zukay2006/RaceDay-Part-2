
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using RaceDay.API.DTOs;
using Xunit;

namespace RaceDay.API.Tests
{
    public class EnrolmentRequestTests
    {
        private static bool IsValid(EnrolmentRequest request)
        {
            var results = new List<ValidationResult>();
            var context = new ValidationContext(request);

            return Validator.TryValidateObject(
                request, context, results, true);
        }

        [Fact]
        public void ValidRequest_ShouldPassValidation()
        {
            var request = new EnrolmentRequest
            {
                EventID = 1,
                CategoryID = 1
            };

            Assert.True(IsValid(request));
        }

        [Theory]
        [InlineData(0, 1)]
        [InlineData(-1, 1)]
        [InlineData(1, 0)]
        [InlineData(1, -1)]
        [InlineData(0, 0)]
        [InlineData(-1, -1)]
        public void InvalidIDs_ShouldFailValidation(
            int eventID, int categoryID)
        {
            var request = new EnrolmentRequest
            {
                EventID = eventID,
                CategoryID = categoryID
            };

            Assert.False(IsValid(request));
        }

        [Fact]
        public void MaximumIntegerIDs_ShouldPassValidation()
        {
            var request = new EnrolmentRequest
            {
                EventID = int.MaxValue,
                CategoryID = int.MaxValue
            };

            Assert.True(IsValid(request));
        }
    }
}
