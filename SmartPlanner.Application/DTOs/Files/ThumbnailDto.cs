namespace SmartPlanner.Application.Dtos.Files
{
    public enum ThumbnailSize
    {
        Small = 0,    // 200x200
        Medium = 1,   // 800x600
        Large = 2     // оригинал
    }

    public class ThumbnailRequestDto
    {
        public ThumbnailSize Size { get; set; } = ThumbnailSize.Small;
        public int? Width { get; set; }
        public int? Height { get; set; }
        public bool Crop { get; set; } = false;
    }
}
