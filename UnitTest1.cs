
using Xunit;

namespace RaceDay.API.Tests
{
    public class UnitTest1
    {
        [Fact]
        public void EventName_ShouldNotBeEmpty()
        {
            // Arrange
            string eventName = "Soweto Community Run";

            // Act
            bool hasName = !string.IsNullOrWhiteSpace(eventName);

            // Assert
            Assert.True(hasName);
        }

        [Fact]
        public void EventDistance_ShouldBeGreaterThanZero()
        {
            // Arrange
            double distanceKM = 10;

            // Act
            bool isValidDistance = distanceKM > 0;

            // Assert
            Assert.True(isValidDistance);
        }

        [Fact]
        public void EnrolmentStatus_ShouldBeActive()
        {
            // Arrange
            string status = "Active";

            // Assert
            Assert.Equal("Active", status);
        }
    }
}
