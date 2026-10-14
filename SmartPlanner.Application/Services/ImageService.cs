using Microsoft.Extensions.Logging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Metadata.Profiles.Exif;
using SixLabors.ImageSharp.Processing;
using SmartPlanner.Application.Dtos.Files;
using SmartPlanner.Application.Interfaces.Services;

namespace SmartPlanner.Application.Services
{
    public class ImageService : IImageService
    {
        private readonly ILogger<ImageService> _logger;

        private readonly Dictionary<ThumbnailSize, (int Width, int Height)> _defaultSizes = new()
        {
            [ThumbnailSize.Small] = (200, 200),
            [ThumbnailSize.Medium] = (800, 600),
            [ThumbnailSize.Large] = (1920, 1080)
        };

        public ImageService(ILogger<ImageService> logger)
        {
            _logger = logger;
        }

        public bool IsImageFile(string fileName)
        {
            var extension = Path.GetExtension(fileName).ToLowerInvariant();
            return extension switch
            {
                ".jpg" or ".jpeg" or ".png" or ".gif" or ".webp" or ".bmp" => true,
                _ => false
            };
        }

        public async Task<(int Width, int Height)> GetImageDimensionsAsync(string imagePath)
        {
            try
            {
                using var image = await Image.LoadAsync(imagePath);
                return (image.Width, image.Height);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Не удалось определить размеры изображения: {Path}", imagePath);
                return (0, 0);
            }
        }

        public async Task<string> GenerateThumbnailAsync(string sourceImagePath, string outputDirectory,
            ThumbnailSize size, bool crop = false, int? customWidth = null, int? customHeight = null)
        {
            var (width, height) = _defaultSizes[size];

            if (customWidth.HasValue) width = customWidth.Value;
            if (customHeight.HasValue) height = customHeight.Value;

            return await GenerateThumbnailAsync(sourceImagePath, outputDirectory, width, height, crop);
        }

        public async Task<string> GenerateThumbnailAsync(string sourceImagePath, string outputDirectory,
            int width, int height, bool crop = false)
        {
            try
            {
                _logger.LogInformation("Генерация thumbnail {Width}x{Height} для {Source}",
                    width, height, sourceImagePath);

                // Создаем директорию если нет
                Directory.CreateDirectory(outputDirectory);

                // Генерируем имя файла для thumbnail
                var sourceFileName = Path.GetFileNameWithoutExtension(sourceImagePath);
                var extension = Path.GetExtension(sourceImagePath);
                var thumbnailFileName = $"{sourceFileName}_{width}x{height}{extension}";
                var thumbnailPath = Path.Combine(outputDirectory, thumbnailFileName);

                // Если thumbnail уже существует - возвращаем его
                if (File.Exists(thumbnailPath))
                {
                    _logger.LogDebug("Thumbnail уже существует: {Path}", thumbnailPath);
                    return thumbnailPath;
                }

                // Загружаем изображение
                using var image = await Image.LoadAsync(sourceImagePath);

                // Определяем режим ресайза
                var resizeOptions = new ResizeOptions
                {
                    Size = new Size(width, height),
                    Mode = crop ? ResizeMode.Crop : ResizeMode.Max,
                    Position = crop ? AnchorPositionMode.Center : AnchorPositionMode.Center,
                    Compand = true
                };

                // Ресайзим изображение
                image.Mutate(x => x.Resize(resizeOptions));

                // Настройки качества
                var encoder = GetImageEncoder(extension);

                // Сохраняем thumbnail
                await image.SaveAsync(thumbnailPath, encoder);

                _logger.LogInformation("Thumbnail создан: {Path}", thumbnailPath);
                return thumbnailPath;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при генерации thumbnail для {Path}", sourceImagePath);
                throw;
            }
        }

        public async Task OptimizeImageAsync(string imagePath, int quality = 85)
        {
            try
            {
                _logger.LogInformation("Оптимизация изображения: {Path}, quality: {Quality}",
                    imagePath, quality);

                using var image = await Image.LoadAsync(imagePath);
                var extension = Path.GetExtension(imagePath).ToLowerInvariant();
                var encoder = GetImageEncoder(extension, quality);

                var tempPath = imagePath + ".optimized";
                await image.SaveAsync(tempPath, encoder);

                File.Delete(imagePath);
                File.Move(tempPath, imagePath);

                _logger.LogInformation("Изображение оптимизировано: {Path}", imagePath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при оптимизации изображения: {Path}", imagePath);
                throw;
            }
        }

        public async Task<Dictionary<string, string>> ExtractExifDataAsync(string imagePath)
        {
            var exifData = new Dictionary<string, string>();

            try
            {
                using var image = await Image.LoadAsync(imagePath);

                if (image.Metadata.ExifProfile != null)
                {
                    foreach (var value in image.Metadata.ExifProfile.Values)
                    {
                        if (!string.IsNullOrEmpty(value.GetValue()?.ToString()))
                        {
                            exifData[value.Tag.ToString()] = value.GetValue()?.ToString() ?? string.Empty;
                        }
                    }

                    _logger.LogDebug("Извлечено {Count} EXIF тегов из {Path}", exifData.Count, imagePath);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Не удалось извлечь EXIF данные из {Path}", imagePath);
            }

            return exifData;
        }

        public async Task RemoveExifDataAsync(string imagePath)
        {
            try
            {
                _logger.LogInformation("Удаление EXIF данных из: {Path}", imagePath);

                using var image = await Image.LoadAsync(imagePath);
                image.Metadata.ExifProfile = null; // Удаляем EXIF

                var tempPath = imagePath + ".noexif";
                var encoder = GetImageEncoder(Path.GetExtension(imagePath));

                await image.SaveAsync(tempPath, encoder);

                // Заменяем файл
                File.Delete(imagePath);
                File.Move(tempPath, imagePath);

                _logger.LogInformation("EXIF данные удалены: {Path}", imagePath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при удалении EXIF данных из {Path}", imagePath);
                throw;
            }
        }

        private IImageEncoder GetImageEncoder(string extension, int quality = 85)
        {
            return extension.ToLowerInvariant() switch
            {
                ".jpg" or ".jpeg" => new JpegEncoder { Quality = quality },
                ".png" => new PngEncoder(),
                ".gif" => new SixLabors.ImageSharp.Formats.Gif.GifEncoder(),
                ".webp" => new SixLabors.ImageSharp.Formats.Webp.WebpEncoder { Quality = quality },
                ".bmp" => new SixLabors.ImageSharp.Formats.Bmp.BmpEncoder(),
                _ => new JpegEncoder { Quality = quality }
            };
        }
    }
}
