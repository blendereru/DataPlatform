using System.Net;
using DataPlatform.Api.Models;
using DataPlatform.Api.Tests.Common;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;

namespace DataPlatform.Api.Tests;

public class AccountControllerTests
    : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
{
    private readonly CustomWebApplicationFactory _factory;
    private HttpClient _client = default!;

    public AccountControllerTests(CustomWebApplicationFactory factory)
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
            AllowAutoRedirect = false // important for auth tests
        });

        await using var db = _factory.CreateDbContext();
        await CustomWebApplicationFactory.ClearDatabaseAsync(db);
    }

    public Task DisposeAsync() => Task.CompletedTask;

    // ----------------------------------------------------
    // GET /auth/signin
    // ----------------------------------------------------
    [Fact]
    public async Task Get_Login_Returns_Login_View()
    {
        var response = await _client.GetAsync("/auth/signin");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/html", response.Content.Headers.ContentType?.MediaType);
    }

    // ----------------------------------------------------
    // POST /auth/register
    // ----------------------------------------------------
    [Fact]
    public async Task Register_Successfully_Creates_User_And_Redirects()
    {
        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Username"] = "john",
            ["Password"] = "Password123!",
            ["ConfirmPassword"] = "Password123!"
        });

        var response = await _client.PostAsync("/auth/register", content);

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/auth/signin", response.Headers.Location?.ToString());

        await using var db = _factory.CreateDbContext();
        var user = await db.Users.SingleAsync(u => u.Username == "john");

        Assert.True(BCrypt.Net.BCrypt.Verify("Password123!", user.PasswordHash));
    }

    [Fact]
    public async Task Register_Fails_When_Passwords_Do_Not_Match()
    {
        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Username"] = "john",
            ["Password"] = "Password123!",
            ["ConfirmPassword"] = "Different!"
        });

        var response = await _client.PostAsync("/auth/register", content);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Register_Fails_When_User_Already_Exists()
    {
        await SeedUser("existing", "Password123!");

        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Username"] = "existing",
            ["Password"] = "Password123!",
            ["ConfirmPassword"] = "Password123!"
        });

        var response = await _client.PostAsync("/auth/register", content);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    // ----------------------------------------------------
    // POST /auth/signin
    // ----------------------------------------------------
    [Fact]
    public async Task Login_Succeeds_With_Valid_Credentials()
    {
        await SeedUser("loginuser", "Password123!");

        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Username"] = "loginuser",
            ["Password"] = "Password123!"
        });

        var response = await _client.PostAsync("/auth/signin", content);

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/dashboard", response.Headers.Location?.ToString());

        Assert.Contains(
            response.Headers,
            h => h.Key == "Set-Cookie" &&
                 h.Value.Any(v => v.Contains(".AspNetCore.Cookies")));
    }

    [Fact]
    public async Task Login_Fails_When_User_Not_Found()
    {
        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Username"] = "unknown",
            ["Password"] = "Password123!"
        });

        var response = await _client.PostAsync("/auth/signin", content);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Login_Fails_With_Wrong_Password()
    {
        await SeedUser("wrongpass", "Password123!");

        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Username"] = "wrongpass",
            ["Password"] = "Invalid!"
        });

        var response = await _client.PostAsync("/auth/signin", content);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    // ----------------------------------------------------
    // POST /auth/logout
    // ----------------------------------------------------
    [Fact]
    public async Task Logout_Requires_Authentication()
    {
        var response = await _client.PostAsync("/auth/logout", null);

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.StartsWith("/auth/signin", response.Headers.Location?.ToString());
    }

    [Fact]
    public async Task Logout_Succeeds_For_Authenticated_User()
    {
        var authClient = await CreateAuthenticatedClient("logoutuser");

        var response = await authClient.PostAsync("/auth/logout", null);

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/auth/signin", response.Headers.Location?.ToString());
    }

    // ----------------------------------------------------
    // Helpers
    // ----------------------------------------------------
    private async Task SeedUser(string username, string password)
    {
        await using var db = _factory.CreateDbContext();

        db.Users.Add(new User
        {
            Username = username,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password)
        });

        await db.SaveChangesAsync();
    }

    private async Task<HttpClient> CreateAuthenticatedClient(string username)
    {
        await SeedUser(username, "Password123!");

        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Username"] = username,
            ["Password"] = "Password123!"
        });

        var response = await _client.PostAsync("/auth/signin", content);

        var cookie = response.Headers.GetValues("Set-Cookie").Single();

        var authClient = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        authClient.DefaultRequestHeaders.Add("Cookie", cookie);

        return authClient;
    }
}
