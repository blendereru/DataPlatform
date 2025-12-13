using System.Net;
using System.Net.Http.Json;
using DataPlatform.Api.DTOs;
using DataPlatform.Api.Models;
using DataPlatform.Api.Models.Messages;
using DataPlatform.Api.Tests.Common;
using MassTransit.Testing;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace DataPlatform.Api.Tests;

public class EventsControllerTests
    : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
{
    private readonly CustomWebApplicationFactory _factory;
    private HttpClient _client = default!;
    private ITestHarness _harness = default!;

    public EventsControllerTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    // ----------------------------------------------------
    // Test lifecycle
    // ----------------------------------------------------
    public async Task InitializeAsync()
    {
        _client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        _harness = _factory.Services.GetRequiredService<ITestHarness>();

        await using var db = _factory.CreateDbContext();
        await CustomWebApplicationFactory.ClearDatabaseAsync(db);
    }

    public Task DisposeAsync() => Task.CompletedTask;

    // ----------------------------------------------------
    // GET /events
    // ----------------------------------------------------
    [Fact]
    public async Task Index_Requires_Authentication()
    {
        var response = await _client.GetAsync("/events");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("/auth/signin", response.Headers.Location?.ToString());
    }

    [Fact]
    public async Task Index_Returns_View_With_Events_For_Authenticated_User()
    {
        var authClient = await CreateAuthenticatedClient();

        await SeedEvents(3);

        var response = await authClient.GetAsync("/events");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/html", response.Content.Headers.ContentType?.MediaType);
    }

    // ----------------------------------------------------
    // POST /api/events
    // ----------------------------------------------------
    [Fact]
    public async Task Create_Requires_Authentication()
    {
        var response = await _client.PostAsJsonAsync("/api/events",
            new CreateEventRequest { Payload = "test" });

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("/auth/signin", response.Headers.Location?.ToString());
    }

    [Fact]
    public async Task Create_Returns_BadRequest_When_Payload_Is_Empty()
    {
        var authClient = await CreateAuthenticatedClient();

        var response = await authClient.PostAsJsonAsync("/api/events",
            new CreateEventRequest { Payload = "" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_Returns_BadRequest_When_Request_Is_Null()
    {
        var authClient = await CreateAuthenticatedClient();

        var response = await authClient.PostAsync("/api/events", null);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_Persists_Event_In_Database()
    {
        var authClient = await CreateAuthenticatedClient();

        var payload = "event-payload";

        var response = await authClient.PostAsJsonAsync("/api/events",
            new CreateEventRequest { Payload = payload });

        response.EnsureSuccessStatusCode();

        await using var db = _factory.CreateDbContext();
        var entity = await db.Events.SingleAsync();

        Assert.Equal(payload, entity.Payload);
        Assert.NotEqual(default, entity.CreatedAt);
    }

    [Fact]
    public async Task Create_Publishes_EventMessage()
    {
        var authClient = await CreateAuthenticatedClient();

        var payload = "publish-test";

        var response = await authClient.PostAsJsonAsync("/api/events",
            new CreateEventRequest { Payload = payload });

        response.EnsureSuccessStatusCode();

        Assert.True(await _harness.Published.Any<EventMessage>());
    }

    [Fact]
    public async Task Create_Returns_Created_EventEntity()
    {
        var authClient = await CreateAuthenticatedClient();

        var payload = "response-test";

        var response = await authClient.PostAsJsonAsync("/api/events",
            new CreateEventRequest { Payload = payload });

        response.EnsureSuccessStatusCode();

        var entity = await response.Content.ReadFromJsonAsync<EventEntity>();

        Assert.NotNull(entity);
        Assert.Equal(payload, entity.Payload);
        Assert.True(entity.Id != Guid.Empty);
    }

    // ----------------------------------------------------
    // Helpers
    // ----------------------------------------------------
    private async Task<HttpClient> CreateAuthenticatedClient()
    {
        await SeedUser("events-user", "Password123!");

        var loginClient = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var loginResponse = await loginClient.PostAsync(
            "/auth/signin",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["Username"] = "events-user",
                ["Password"] = "Password123!"
            }));

        var cookie = loginResponse.Headers.GetValues("Set-Cookie").Single();

        var authClient = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        authClient.DefaultRequestHeaders.Add("Cookie", cookie);

        return authClient;
    }

    private async Task SeedUser(string username, string password)
    {
        await using var db = _factory.CreateDbContext();

        if (await db.Users.AnyAsync(u => u.Username == username))
            return;

        db.Users.Add(new User
        {
            Username = username,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password)
        });

        await db.SaveChangesAsync();
    }

    private async Task SeedEvents(int count)
    {
        await using var db = _factory.CreateDbContext();

        for (var i = 0; i < count; i++)
        {
            db.Events.Add(new EventEntity
            {
                Payload = $"event-{i}"
            });
        }

        await db.SaveChangesAsync();
    }
}