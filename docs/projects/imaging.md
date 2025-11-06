---
layout: default
title: Nextended.Imaging
parent: Projects
nav_order: 3
---

# Nextended.Imaging

Image processing and manipulation utilities for .NET applications.

## Overview

Nextended.Imaging provides a comprehensive `ImageHelper` class for common image operations including resizing, format conversion, quality adjustment, and caching support.

## Installation

```bash
dotnet add package Nextended.Imaging
```

## Key Features

### 1. Image Resizing

```csharp
using Nextended.Imaging;

// Resize image to specific dimensions
var resized = ImageHelper.Resize(originalImage, 800, 600);

// Resize maintaining aspect ratio
var resized = ImageHelper.ResizeKeepAspect(originalImage, 800, 600);

// Resize by percentage
var resized = ImageHelper.ResizeByPercent(originalImage, 50); // 50%
```

### 2. Format Conversion

```csharp
// Convert to different format
var jpegBytes = ImageHelper.ConvertToJpeg(pngBytes);
var pngBytes = ImageHelper.ConvertToPng(jpegBytes);

// Convert with quality setting
var jpegBytes = ImageHelper.ConvertToJpeg(imageBytes, quality: 85);
```

### 3. Image Quality Adjustment

```csharp
// Adjust JPEG quality
var compressed = ImageHelper.CompressJpeg(originalBytes, quality: 75);

// Optimize for web
var optimized = ImageHelper.OptimizeForWeb(imageBytes, maxWidth: 1920);
```

### 4. Image Information

```csharp
// Get image dimensions
var size = ImageHelper.GetImageSize(imageBytes);
Console.WriteLine($"Width: {size.Width}, Height: {size.Height}");

// Get image format
var format = ImageHelper.GetImageFormat(imageBytes);
```

### 5. Thumbnail Generation

```csharp
// Generate thumbnail
var thumbnail = ImageHelper.CreateThumbnail(originalImage, 150, 150);

// Generate square thumbnail (crop to center)
var squareThumbnail = ImageHelper.CreateSquareThumbnail(originalImage, 100);
```

## Usage Examples

### Basic Image Processing

```csharp
using Nextended.Imaging;
using System.IO;

public class ImageService
{
    public byte[] ProcessUserAvatar(byte[] originalImage)
    {
        // Resize to standard avatar size
        var resized = ImageHelper.ResizeKeepAspect(originalImage, 200, 200);
        
        // Convert to JPEG with good quality
        var processed = ImageHelper.ConvertToJpeg(resized, quality: 90);
        
        return processed;
    }
    
    public byte[] CreateProductImage(byte[] originalImage)
    {
        // Resize for product display
        var resized = ImageHelper.ResizeKeepAspect(originalImage, 800, 800);
        
        // Optimize file size
        var optimized = ImageHelper.CompressJpeg(resized, quality: 80);
        
        return optimized;
    }
}
```

### Image Upload Processing

```csharp
public class PhotoUploadService
{
    public async Task<ProcessedImages> ProcessPhotoAsync(Stream imageStream)
    {
        // Read image bytes
        using var ms = new MemoryStream();
        await imageStream.CopyToAsync(ms);
        var originalBytes = ms.ToArray();
        
        // Validate image
        var size = ImageHelper.GetImageSize(originalBytes);
        if (size.Width < 100 || size.Height < 100)
        {
            throw new ArgumentException("Image too small");
        }
        
        // Create different sizes
        var large = ImageHelper.ResizeKeepAspect(originalBytes, 1920, 1920);
        var medium = ImageHelper.ResizeKeepAspect(originalBytes, 800, 800);
        var thumbnail = ImageHelper.CreateThumbnail(originalBytes, 150, 150);
        
        return new ProcessedImages
        {
            Original = originalBytes,
            Large = large,
            Medium = medium,
            Thumbnail = thumbnail
        };
    }
}

public class ProcessedImages
{
    public byte[] Original { get; set; }
    public byte[] Large { get; set; }
    public byte[] Medium { get; set; }
    public byte[] Thumbnail { get; set; }
}
```

### Cached Image Processing

```csharp
using Nextended.Cache;

public class CachedImageService
{
    private readonly CacheProvider _cache;
    
    public CachedImageService(CacheProvider cache)
    {
        _cache = cache;
    }
    
    public byte[] GetResizedImage(string imageId, int width, int height)
    {
        var cacheKey = $"image:{imageId}:{width}x{height}";
        
        return _cache.GetOrCreate(cacheKey, () =>
        {
            // Load original image
            var original = LoadImageFromStorage(imageId);
            
            // Resize and cache
            return ImageHelper.ResizeKeepAspect(original, width, height);
            
        }, TimeSpan.FromHours(24));
    }
    
    private byte[] LoadImageFromStorage(string imageId)
    {
        // Load from database, file system, etc.
        throw new NotImplementedException();
    }
}
```

### Image Gallery Service

```csharp
public class GalleryService
{
    public async Task<GalleryImage> CreateGalleryImageAsync(
        Stream imageStream, 
        string fileName)
    {
        using var ms = new MemoryStream();
        await imageStream.CopyToAsync(ms);
        var originalBytes = ms.ToArray();
        
        // Get image info
        var size = ImageHelper.GetImageSize(originalBytes);
        var format = ImageHelper.GetImageFormat(originalBytes);
        
        // Process images
        var displayImage = ImageHelper.ResizeKeepAspect(originalBytes, 1200, 1200);
        var thumbnail = ImageHelper.CreateSquareThumbnail(originalBytes, 200);
        
        // Compress for storage
        var displayCompressed = ImageHelper.CompressJpeg(displayImage, 85);
        var thumbnailCompressed = ImageHelper.CompressJpeg(thumbnail, 90);
        
        // Save to storage (implementation depends on your storage)
        var displayUrl = await SaveImageAsync(displayCompressed, $"{fileName}_display.jpg");
        var thumbnailUrl = await SaveImageAsync(thumbnailCompressed, $"{fileName}_thumb.jpg");
        
        return new GalleryImage
        {
            OriginalWidth = size.Width,
            OriginalHeight = size.Height,
            OriginalFormat = format.ToString(),
            DisplayUrl = displayUrl,
            ThumbnailUrl = thumbnailUrl,
            FileSize = originalBytes.Length
        };
    }
    
    private Task<string> SaveImageAsync(byte[] imageBytes, string fileName)
    {
        // Implementation depends on storage (blob, file system, etc.)
        throw new NotImplementedException();
    }
}

public class GalleryImage
{
    public int OriginalWidth { get; set; }
    public int OriginalHeight { get; set; }
    public string OriginalFormat { get; set; }
    public string DisplayUrl { get; set; }
    public string ThumbnailUrl { get; set; }
    public long FileSize { get; set; }
}
```

### Watermark Application

```csharp
public class WatermarkService
{
    public byte[] ApplyWatermark(byte[] originalImage, string watermarkText)
    {
        using var image = Image.Load(originalImage);
        using var graphics = image.CreateGraphics();
        
        // Configure watermark
        var font = new Font("Arial", 20, FontStyle.Bold);
        var brush = new SolidBrush(Color.FromArgb(128, 255, 255, 255));
        
        // Calculate position (bottom-right corner)
        var textSize = graphics.MeasureString(watermarkText, font);
        var x = image.Width - textSize.Width - 10;
        var y = image.Height - textSize.Height - 10;
        
        // Draw watermark
        graphics.DrawString(watermarkText, font, brush, x, y);
        
        // Save to byte array
        using var ms = new MemoryStream();
        image.Save(ms, ImageFormat.Jpeg);
        return ms.ToArray();
    }
}
```

## Best Practices

### 1. Validate Images Before Processing

```csharp
public bool ValidateImage(byte[] imageBytes)
{
    try
    {
        var size = ImageHelper.GetImageSize(imageBytes);
        
        // Check minimum size
        if (size.Width < 100 || size.Height < 100)
            return false;
        
        // Check maximum size
        if (size.Width > 5000 || size.Height > 5000)
            return false;
        
        // Check file size (e.g., max 10MB)
        if (imageBytes.Length > 10 * 1024 * 1024)
            return false;
        
        return true;
    }
    catch
    {
        return false;
    }
}
```

### 2. Use Appropriate Quality Settings

```csharp
// High quality for important images (logos, hero images)
var highQuality = ImageHelper.CompressJpeg(imageBytes, quality: 95);

// Medium quality for general content
var mediumQuality = ImageHelper.CompressJpeg(imageBytes, quality: 85);

// Lower quality for thumbnails (still acceptable)
var thumbnailQuality = ImageHelper.CompressJpeg(imageBytes, quality: 75);
```

### 3. Implement Caching

```csharp
// Cache processed images to avoid repeated processing
var cached = _cache.GetOrCreate(
    $"image:{id}:{width}x{height}",
    () => ImageHelper.ResizeKeepAspect(original, width, height),
    TimeSpan.FromDays(7)
);
```

### 4. Handle Exceptions Gracefully

```csharp
public byte[] SafeProcessImage(byte[] imageBytes)
{
    try
    {
        return ImageHelper.ResizeKeepAspect(imageBytes, 800, 600);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Failed to process image");
        return imageBytes; // Return original on failure
    }
}
```

## Performance Considerations

- **Caching**: Cache processed images to avoid repeated operations
- **Async Processing**: Process large batches asynchronously
- **Quality vs Size**: Balance image quality with file size
- **Format Choice**: JPEG for photos, PNG for graphics with transparency
- **Memory Management**: Dispose of image objects properly

## Supported Frameworks

- .NET 8.0
- .NET 9.0

## Dependencies

- `Nextended.Core` - Core utilities
- `Nextended.Cache` - Caching support
- `System.Drawing.Common` - Image processing
- `MediaTypeMap.Core` - MIME type mapping

## Related Projects

- [Nextended.Core](core.md) - Foundation library
- [Nextended.Cache](cache.md) - Caching utilities

## Links

- [NuGet Package](https://www.nuget.org/packages/Nextended.Imaging/)
- [Source Code](https://github.com/fgilde/Nextended/tree/main/Nextended.Imaging)
- [Report Issues](https://github.com/fgilde/Nextended/issues)
