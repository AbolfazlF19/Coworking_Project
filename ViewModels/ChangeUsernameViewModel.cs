using System.ComponentModel.DataAnnotations;

namespace CoworkingSpace.Web.ViewModels;

public class ChangeUsernameViewModel
{
    [Required(ErrorMessage = "Current password is required.")]
    [DataType(DataType.Password)]
    [Display(Name = "Current Password")]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "New username is required.")]
    [StringLength(100, MinimumLength = 3, ErrorMessage = "Username must be between 3 and 100 characters.")]
    [Display(Name = "New Username")]
    public string NewUsername { get; set; } = string.Empty;
}