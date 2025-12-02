using System.ComponentModel.DataAnnotations;

namespace DataPlatform.Api.DTOs;

///
/// DTO used when creating a new user account.
///
public class RegisterRequest
{
    ///
    /// Desired username for the new account.
    ///
    [Required]
    public string Username { get; set; } = "";
    
    ///
    /// Password chosen by the user.
    ///
    [Required]
    public string Password { get; set; } = "";
    
    ///
    /// Confirmation of the password to ensure accuracy.
    ///
    [Required]
    [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
    public string ConfirmPassword { get; set; } = "";
}