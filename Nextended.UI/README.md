# Nextended.UI

[![NuGet](https://img.shields.io/nuget/v/Nextended.UI.svg)](https://www.nuget.org/packages/Nextended.UI/)

WPF and Windows Forms utilities for desktop application development.

## Overview

Nextended.UI provides comprehensive utilities, behaviors, and helpers for building Windows desktop applications with WPF and Windows Forms.

**Platform**: Windows only (net8.0-windows, net9.0-windows)

## Installation

```bash
dotnet add package Nextended.UI
```

## Key Features

- **ViewUtility**: Comprehensive utility class for UI operations
- **WPF Behaviors**: Custom behaviors for enhanced WPF functionality
- **ViewModel Base Classes**: MVVM pattern implementation helpers
- **Theming Support**: Built-in theming capabilities
- **Visual Tree Helpers**: Find and manipulate visual tree elements

## Quick Start

```csharp
using Nextended.UI;
using Nextended.UI.ViewModels;

// Find visual children
var button = ViewUtility.FindVisualChild<Button>(parent);

// MVVM ViewModel
public class MainViewModel : ViewModelBase
{
    private string _title;
    
    public string Title
    {
        get => _title;
        set => SetProperty(ref _title, value);
    }
    
    public ICommand SaveCommand { get; }
    
    public MainViewModel()
    {
        SaveCommand = new RelayCommand(Save, CanSave);
    }
}
```

## Documentation

For comprehensive documentation, examples, and API reference, see:
- 📚 [Complete Documentation](https://github.com/fgilde/Nextended/blob/main/docs/projects/ui.md)
- 🏠 [Main Documentation Portal](https://fgilde.github.io/Nextended/)

## Supported Frameworks

- .NET 8.0 (Windows)
- .NET 9.0 (Windows)

## Platform Support

**Windows Only** - This package requires Windows and will not build or run on Linux or macOS.

## Dependencies

- Nextended.Core
- Microsoft.Xaml.Behaviors.Wpf

## Links

- [NuGet Package](https://www.nuget.org/packages/Nextended.UI/)
- [Documentation](https://github.com/fgilde/Nextended/blob/main/docs/projects/ui.md)
- [Source Code](https://github.com/fgilde/Nextended/tree/main/Nextended.UI)
- [Report Issues](https://github.com/fgilde/Nextended/issues)

## License

This project is licensed under the MIT License.
