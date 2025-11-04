# Nextended.Imaging

[![NuGet](https://img.shields.io/nuget/v/Nextended.Imaging.svg)](https://www.nuget.org/packages/Nextended.Imaging/)

Image processing and manipulation utilities for .NET applications.

## Overview

Nextended.Imaging provides a comprehensive `ImageHelper` class for common image operations including resizing, format conversion, quality adjustment, and caching support.

## Installation

```bash
dotnet add package Nextended.Imaging
```

## Key Features

- **Image Resizing**: Resize with or without aspect ratio preservation
- **Format Conversion**: Convert between image formats (JPEG, PNG, etc.)
- **Quality Adjustment**: Compress and optimize images
- **Thumbnail Generation**: Create thumbnails and square crops
- **Image Information**: Get dimensions, format, and metadata
- **Caching Support**: Built-in caching for processed images

## Quick Start

```csharp
using Nextended.Imaging;

// Resize image maintaining aspect ratio
var resized = ImageHelper.ResizeKeepAspect(originalImage, 800, 600);

// Convert to JPEG with quality setting
var jpegBytes = ImageHelper.ConvertToJpeg(imageBytes, quality: 85);

// Generate thumbnail
var thumbnail = ImageHelper.CreateThumbnail(originalImage, 150, 150);

// Get image information
var size = ImageHelper.GetImageSize(imageBytes);
Console.WriteLine($"Width: {size.Width}, Height: {size.Height}");
```

## Documentation

For comprehensive documentation, examples, and API reference, see:
- 📚 [Complete Documentation](https://github.com/fgilde/Nextended/blob/main/docs/projects/imaging.md)
- 🏠 [Main Documentation Portal](https://fgilde.github.io/Nextended/)

## Supported Frameworks

- .NET 8.0
- .NET 9.0

## Dependencies

- Nextended.Core
- Nextended.Cache
- System.Drawing.Common

## Links

- [NuGet Package](https://www.nuget.org/packages/Nextended.Imaging/)
- [Documentation](https://github.com/fgilde/Nextended/blob/main/docs/projects/imaging.md)
- [Source Code](https://github.com/fgilde/Nextended/tree/main/Nextended.Imaging)
- [Report Issues](https://github.com/fgilde/Nextended/issues)

## License

This project is licensed under the MIT License.
