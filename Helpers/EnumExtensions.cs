using System.Text;

namespace CoworkingSpace.Web.Helpers;

/// <summary>
/// Small presentation helpers for rendering enums / values in views.
/// </summary>
public static class EnumExtensions
{
    /// <summary>"MeetingRoom" -> "Meeting Room", "InProgress" -> "In Progress".</summary>
    public static string ToSpacedString(this Enum value)
    {
        var name = value.ToString();
        if (string.IsNullOrEmpty(name)) return name;

        var sb = new StringBuilder(name.Length + 4);
        foreach (var c in name)
        {
            if (char.IsUpper(c) && sb.Length > 0)
            {
                sb.Append(' ');
            }
            sb.Append(c);
        }
        return sb.ToString();
    }
}
