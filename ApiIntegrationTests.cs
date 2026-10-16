using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RaceDay.API.Data;
using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace RaceDay.API.Tests
{
    public class ApiIntegrationTests
    {
        private static WebApplicationFactoryClientOptions ClientOptions() =>
            new WebApplicationFactoryClientOptions
            {
                BaseAddress = new Uri("https://localhost"),
                AllowAutoRedirect = false
            };

        private static object Registration(string email, string role = "Participant") =>
            new
            {
                firstName = "Test",
                lastName = "User",
                email,
                password = "Password123",
                phone = "0712345678",
                role
            };

        private static async Task RegisterAndLogin(
            HttpClient client, string email, string role)
        {
            var registerResponse = await client.PostAsJsonAsync(
                "/api/Auth/register", Registration(email, role));

            registerResponse.EnsureSuccessStatusCode();

            var loginResponse = await client.PostAsJsonAsync(
                "/api/Auth/login",
                new
                {
                    email,
                    password = "Password123",
                    role
                });

            loginResponse.EnsureSuccessStatusCode();
        }

        [Fact]
        public async Task RegisterParticipant_ReturnsOk()
        {
            using var factory = new RaceDayApiFactory();
            using var client = factory.CreateClient(ClientOptions());

            var response = await client.PostAsJsonAsync(
                "/api/Auth/register",
                Registration("participant1@example.com"));

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task RegisterSameEmailTwice_ReturnsConflict()
        {
            using var factory = new RaceDayApiFactory();
            using var client = factory.CreateClient(ClientOptions());

            await RegisterAndLogin(
                client, "duplicate@example.com", "Participant");

            var response = await client.PostAsJsonAsync(
                "/api/Auth/register",
                Registration("duplicate@example.com"));

            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        }

        [Fact]
        public async Task LoginWithWrongPassword_ReturnsUnauthorized()
        {
            using var factory = new RaceDayApiFactory();
            using var client = factory.CreateClient(ClientOptions());

            var registerResponse = await client.PostAsJsonAsync(
                "/api/Auth/register",
                Registration("wrongpassword@example.com"));

            registerResponse.EnsureSuccessStatusCode();

            var response = await client.PostAsJsonAsync(
                "/api/Auth/login",
                new
                {
                    email = "wrongpassword@example.com",
                    password = "WrongPassword123",
                    role = "Participant"
                });

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task UnauthenticatedUser_CannotCreateEvent()
        {
            using var factory = new RaceDayApiFactory();
            using var client = factory.CreateClient(ClientOptions());

            var response = await client.PostAsJsonAsync(
                "/api/Events", new { });

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task Participant_CannotCreateEvent()
        {
            using var factory = new RaceDayApiFactory();
            using var client = factory.CreateClient(ClientOptions());

            await RegisterAndLogin(
                client, "participant2@example.com", "Participant");

            var response = await client.PostAsJsonAsync(
                "/api/Events", new { });

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task UnauthenticatedUser_CannotViewMyEnrolments()
        {
            using var factory = new RaceDayApiFactory();
            using var client = factory.CreateClient(ClientOptions());

            var response = await client.GetAsync("/api/Enrolments/my");

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);


        }

        [Fact]
        public async Task Organiser_CanCreateEvent()
        {
            using var factory = new RaceDayApiFactory();
            using var client = factory.CreateClient(ClientOptions());

            await RegisterAndLogin(
                client, "organiser.event@example.com", "Organiser");

            var eventRequest = new
            {
                eventName = "Test Race",
                eventDate = DateTime.Today.AddDays(10),
                location = "Benoni",
                description = "Integration test event",
                eventType = "Running",
                distanceKM = 5.0m,
                status = "Upcoming"
            };

            var response = await client.PostAsJsonAsync(
                "/api/Events", eventRequest);

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        }

        [Fact]
        public async Task Participant_CanEnrol_AndEnrolmentIsRecorded()
        {
            using var factory = new RaceDayApiFactory();
            using var client = factory.CreateClient(ClientOptions());

            const string email = "participant.enrol@example.com";

            await RegisterAndLogin(client, email, "Participant");

            // Seed a test event and category in the in-memory database.
            using (var scope = factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider
                    .GetRequiredService<RaceDayContext>();

                context.Events.Add(new RaceDay.API.Models.Event
                {
                    EventID = 1,
                    OrganiserID = 1,
                    EventName = "Test Enrolment Race",
                    EventDate = DateTime.Today.AddDays(10),
                    Location = "Benoni",
                    Description = "Test event",
                    EventType = "Running",
                    DistanceKM = 5.0m,
                    Status = "Upcoming"
                });

                context.Categories.Add(new RaceDay.API.Models.Category
                {
                    CategoryID = 1,
                    EventID = 1,
                    CategoryName = "5K",
                    ActivityType = "Running",
                    DistanceKM = 5.0m,
                    EntryFee = 0m
                });

                await context.SaveChangesAsync();
            }

            var response = await client.PostAsJsonAsync(
                "/api/Enrolments",
                new
                {
                    eventID = 1,
                    categoryID = 1
                });

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            // Verify the enrolment was actually saved.
            using (var scope = factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider
                    .GetRequiredService<RaceDayContext>();

                var participant = await context.Participants
                    .SingleAsync(p => p.Email == email);

                var enrolment = await context.Enrolments
                    .SingleAsync(e => e.ParticipantID == participant.ParticipantID);

                Assert.Equal(1, enrolment.CategoryID);
                Assert.Equal("Active", enrolment.EnrolmentStatus);
            }


        }


        [Fact]
        public async Task Organiser_CanUpdateAndDeleteOwnEvent()
        {
            using var factory = new RaceDayApiFactory();
            using var client = factory.CreateClient(ClientOptions());

            await RegisterAndLogin(
                client, "organiser.manage@example.com", "Organiser");

            // Create an event first.
            var createResponse = await client.PostAsJsonAsync(
                "/api/Events",
                new
                {
                    eventName = "Original Race",
                    eventDate = DateTime.Today.AddDays(10),
                    location = "Benoni",
                    description = "Original description",
                    eventType = "Running",
                    distanceKM = 5.0m,
                    status = "Upcoming"
                });

            Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

            var createdEvent = await createResponse.Content
                .ReadFromJsonAsync<EventResponse>();

            Assert.NotNull(createdEvent);

            // Update the event.
            var updateResponse = await client.PutAsJsonAsync(
                $"/api/Events/{createdEvent.EventID}",
                new
                {
                    eventName = "Updated Race",
                    eventDate = DateTime.Today.AddDays(15),
                    location = "Johannesburg",
                    description = "Updated description",
                    eventType = "Running",
                    distanceKM = 10.0m,
                    status = "Upcoming"
                });

            Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

            // Delete the same event.
            var deleteResponse = await client.DeleteAsync(
                $"/api/Events/{createdEvent.EventID}");

            Assert.Equal(HttpStatusCode.OK, deleteResponse.StatusCode);

            // Confirm that the event is no longer available.
            var getResponse = await client.GetAsync(
                $"/api/Events/{createdEvent.EventID}");

            Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
        }

        private sealed class EventResponse
        {
            public int EventID { get; set; }
        }


        [Fact]
        public async Task Participant_CannotUpdateOrDeleteEvent()
        {
            using var factory = new RaceDayApiFactory();
            using var client = factory.CreateClient(ClientOptions());

            // Register and log in as a Participant.
            await RegisterAndLogin(
                client, "participant.manage@example.com", "Participant");

            // Create an event directly in the test database.
            int eventId;

            using (var scope = factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider
                    .GetRequiredService<RaceDay.API.Data.RaceDayContext>();

                var testEvent = new RaceDay.API.Models.Event
                {
                    EventName = "Protected Race",
                    EventDate = DateTime.Today.AddDays(10),
                    Location = "Benoni",
                    Description = "Role restriction test",
                    EventType = "Running",
                    DistanceKM = 5.0m,
                    Status = "Upcoming",
                    OrganiserID = 999
                };

                context.Events.Add(testEvent);
                await context.SaveChangesAsync();

                eventId = testEvent.EventID;
            }

            // A Participant must be forbidden from updating the event.
            var updateResponse = await client.PutAsJsonAsync(
                $"/api/Events/{eventId}",
                new
                {
                    eventName = "Unauthorised Update",
                    eventDate = DateTime.Today.AddDays(15),
                    location = "Johannesburg",
                    description = "Should not be saved",
                    eventType = "Running",
                    distanceKM = 10.0m,
                    status = "Upcoming"
                });

            Assert.Equal(
                HttpStatusCode.Forbidden,
                updateResponse.StatusCode);

            // A Participant must also be forbidden from deleting it.
            var deleteResponse = await client.DeleteAsync(
                $"/api/Events/{eventId}");

            Assert.Equal(
                HttpStatusCode.Forbidden,
                deleteResponse.StatusCode);
        }

    }


}
