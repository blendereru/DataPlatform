using DataPlatform.Api.Data;
using DataPlatform.Api.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using DataPlatform.Api.DTOs;
using Microsoft.EntityFrameworkCore;

public class AccountController : Controller
{
    private readonly ApplicationContext _db;
    private readonly ILogger<AccountController> _logger;

    public AccountController(ApplicationContext db, ILogger<AccountController> logger)
    {
        _db = db;
        _logger = logger;
    }

    /// <summary>
    /// Returns the sign-in page for user authentication.
    /// </summary>
    /// <param name="returnUrl">Optional return URL after successful login.</param>
    /// <returns>Sign-in Razor view.</returns>
    [HttpGet("/auth/signin")]
    [AllowAnonymous]
    public IActionResult Login(string? returnUrl = null)
    {
        _logger.LogInformation("Rendering Login page {@ReturnUrl}", returnUrl);
        return View(new SignInRequest { ReturnUrl = returnUrl });
    }


    /// <summary>
    /// Authenticates an existing user and creates an auth cookie session.
    /// </summary>
    /// <remarks>
    /// Authentication uses BCrypt hashing.  
    /// Returns 400 if credentials are invalid or model is incorrect.
    /// </remarks>
    /// <param name="signInRequest">Login DTO containing username and password.</param>
    /// <response code="200">Redirect to Dashboard or ReturnUrl</response>
    /// <response code="400">Invalid credentials or validation errors.</response>
    [HttpPost("/auth/signin")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Login(SignInRequest signInRequest)
    {
        _logger.LogInformation("Login attempt {@SignInRequest}", signInRequest);

        if (!ModelState.IsValid)
        {
            _logger.LogWarning("Login failed - invalid model {@ModelState}", ModelState);
            return View(signInRequest);
        }

        var user = await _db.Users.FirstOrDefaultAsync(u =>
            u.Username == signInRequest.Username);

        if (user == null)
        {
            _logger.LogWarning("Login failed - user not found: {Username}", signInRequest.Username);
            ModelState.AddModelError("", "Invalid username or password.");
            return View(signInRequest);
        }

        if (!BCrypt.Net.BCrypt.Verify(signInRequest.Password, user.PasswordHash))
        {
            _logger.LogWarning("Login failed - wrong password: {Username}", signInRequest.Username);
            ModelState.AddModelError("", "Invalid username or password.");
            return View(signInRequest);
        }

        try
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.Username),
                new Claim("UserId", user.Id.ToString())
            };

            if (user.Username == "Sanzhar")
                claims.Add(new Claim(ClaimTypes.Role, "Admin"));

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(identity));

            _logger.LogInformation("User login successful {@User}", user);

            return Redirect(signInRequest.ReturnUrl ?? "/dashboard");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during login {@SignInRequest}", signInRequest);
            return StatusCode(500, "An unexpected error occurred.");
        }
    }
    
    /// <summary>
    /// Displays the registration page.
    /// </summary>
    /// <returns>Registration Razor view.</returns>
    [HttpGet("/auth/register")]
    [AllowAnonymous]
    public IActionResult Register()
    {
        _logger.LogInformation("Rendering Register page");
        return View(new RegisterRequest());
    }


    /// <summary>
    /// Registers a new user account with a hashed password.
    /// </summary>
    /// <remarks>
    /// - Passwords are hashed using BCrypt  
    /// - Username must be unique  
    /// </remarks>
    /// <param name="model">Registration data</param>
    /// <response code="200">Redirect to sign-in page</response>
    /// <response code="400">Validation failed or username already exists</response>
    [HttpPost("/auth/register")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Register(RegisterRequest model)
    {
        _logger.LogInformation("Registration attempt {@RegisterRequest}", model);

        if (!ModelState.IsValid)
        {
            _logger.LogWarning("Registration failed - invalid model {@ModelState}", ModelState);
            return View(model);
        }

        if (model.Password != model.ConfirmPassword)
        {
            _logger.LogWarning("Registration failed - passwords mismatch {@RegisterRequest}", model);
            ModelState.AddModelError("", "Passwords do not match.");
            return View(model);
        }

        if (await _db.Users.AnyAsync(u => u.Username == model.Username))
        {
            _logger.LogWarning("Registration failed - username exists: {Username}", model.Username);
            ModelState.AddModelError("", "User already exists.");
            return View(model);
        }

        try
        {
            var user = new User
            {
                Username = model.Username,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.Password)
            };

            _db.Users.Add(user);
            await _db.SaveChangesAsync();

            _logger.LogInformation("User successfully registered {@User}", user);

            return Redirect("/auth/signin");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during registration {@RegisterRequest}", model);
            return StatusCode(500, "An unexpected error occurred.");
        }
    }
    
    /// <summary>
    /// Logs out the currently authenticated user.
    /// </summary>
    /// <remarks>
    /// Removes the authentication cookie and returns user to the login page.
    /// </remarks>
    /// <response code="200">Redirect to login page</response>
    [Authorize]
    [HttpPost("/auth/logout")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> Logout()
    {
        _logger.LogInformation("User logout requested {Username}", User.Identity?.Name);

        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

        _logger.LogInformation("User successfully logged out {Username}", User.Identity?.Name);

        return Redirect("/auth/signin");
    }
}
