using Microsoft.AspNetCore.Identity;

namespace CoworkingSpace.Web.Models;

/// <summary>
/// Custom Identity user. We intentionally leave this empty so the
/// generated <c>AspNetUsers</c> table matches the provided schema 1:1.
/// Members link to their <c>Member</c> row through <see cref="Member.UserId"/>.
/// </summary>
public class ApplicationUser : IdentityUser
{
}
