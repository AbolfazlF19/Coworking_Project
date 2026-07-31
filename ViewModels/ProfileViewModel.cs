using CoworkingSpace.Web.Models;

namespace CoworkingSpace.Web.ViewModels;

public class ProfileViewModel
{
    public ApplicationUser User { get; set; } = null!;
    public Member Member { get; set; } = null!;
}