namespace CoworkingSpace.Web.ViewModels;

public class ImageUploadInfo
{
    public string Id { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public int ImageId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public bool IsPrimary { get; set; }
    public int Order { get; set; }
}