using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SmartPlanner.Application.Dtos.Files;

namespace SmartPlanner.Application.Interfaces.Services
{
    public interface IImageService
    {
        Task<(int Width, int Height)> GetImageDimensionsAsync(string imagePath);
        Task<string> GenerateThumbnailAsync(string sourceImagePath, string outputDirectory,
            ThumbnailSize size, bool crop = false, int? customWidth = null, int? customHeight = null);
        Task<string> GenerateThumbnailAsync(string sourceImagePath, string outputDirectory,
            int width, int height, bool crop = false);
        Task OptimizeImageAsync(string imagePath, int quality = 85);
        Task<Dictionary<string, string>> ExtractExifDataAsync(string imagePath);
        Task RemoveExifDataAsync(string imagePath);
        bool IsImageFile(string fileName);
    }
}
