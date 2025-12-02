using System.ComponentModel.DataAnnotations;

namespace DataPlatform.Api.DTOs;

///
/// DTO used when submitting login credentials.
///
public class SignInRequest
{
    ///
    /// Username provided by the user.
    ///
    [Required]
    public string Username { get; set; } = string.Empty;

    ///
    /// Plain-text password entered by the user.
    ///
    [Required]
    public string Password { get; set; } = string.Empty;

    ///
    /// URL to redirect to after successful login.
    ///
    public string? ReturnUrl { get; set; }
}