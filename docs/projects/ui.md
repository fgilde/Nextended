# Nextended.UI

WPF and Windows Forms utilities for desktop application development.

## Overview

Nextended.UI provides comprehensive utilities, behaviors, and helpers for building Windows desktop applications with WPF and Windows Forms.

## Installation

```bash
dotnet add package Nextended.UI
```

**Note**: This package is Windows-only and targets `net8.0-windows` and `net9.0-windows`.

## Key Features

### 1. ViewUtility

Comprehensive utility class for UI operations and manipulations.

```csharp
using Nextended.UI;

// UI helper operations
var element = ViewUtility.FindVisualChild<Button>(parent);
var parent = ViewUtility.FindVisualParent<Grid>(child);
```

### 2. WPF Behaviors

Custom behaviors for enhanced WPF functionality.

```xaml
<TextBox>
    <i:Interaction.Behaviors>
        <behaviors:WatermarkBehavior Watermark="Enter text here..." />
    </i:Interaction.Behaviors>
</TextBox>
```

### 3. ViewModel Base Classes

Base classes for implementing MVVM pattern.

```csharp
using Nextended.UI.ViewModels;

public class MainViewModel : ViewModelBase
{
    private string _title;
    
    public string Title
    {
        get => _title;
        set => SetProperty(ref _title, value);
    }
}
```

### 4. Theming Support

Built-in theming capabilities for WPF applications.

```csharp
using Nextended.UI.Theming;

// Apply theme
ThemeManager.ApplyTheme(ThemeType.Dark);
```

## Usage Examples

### MVVM ViewModel

```csharp
using Nextended.UI.ViewModels;
using System.Windows.Input;
using Nextended.Core.Extensions;

public class UserViewModel : ViewModelBase
{
    private string _name;
    private string _email;
    private bool _isLoading;
    
    public string Name
    {
        get => _name;
        set => SetProperty(ref _name, value);
    }
    
    public string Email
    {
        get => _email;
        set => SetProperty(ref _email, value);
    }
    
    public bool IsLoading
    {
        get => _isLoading;
        set => SetProperty(ref _isLoading, value);
    }
    
    public ICommand SaveCommand { get; }
    public ICommand CancelCommand { get; }
    
    public UserViewModel()
    {
        SaveCommand = new RelayCommand(SaveUser, CanSaveUser);
        CancelCommand = new RelayCommand(Cancel);
    }
    
    private bool CanSaveUser()
    {
        return !string.IsNullOrWhiteSpace(Name) && 
               !string.IsNullOrWhiteSpace(Email);
    }
    
    private async void SaveUser()
    {
        IsLoading = true;
        try
        {
            await SaveUserToDatabase();
            MessageBox.Show("User saved successfully!");
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }
    
    private void Cancel()
    {
        // Reset or close
        Name = string.Empty;
        Email = string.Empty;
    }
}
```

### Visual Tree Helper

```csharp
using Nextended.UI;
using System.Windows;
using System.Windows.Controls;

public class ControlFinder
{
    public T FindChild<T>(DependencyObject parent, string childName = null) 
        where T : DependencyObject
    {
        return ViewUtility.FindVisualChild<T>(parent, childName);
    }
    
    public void FindAllButtons(DependencyObject parent)
    {
        var buttons = ViewUtility.FindVisualChildren<Button>(parent);
        foreach (var button in buttons)
        {
            Console.WriteLine($"Found button: {button.Name}");
        }
    }
    
    public T FindParent<T>(DependencyObject child) 
        where T : DependencyObject
    {
        return ViewUtility.FindVisualParent<T>(child);
    }
}
```

### Custom Behavior Example

```csharp
using Microsoft.Xaml.Behaviors;
using System.Windows;
using System.Windows.Controls;

public class NumericOnlyBehavior : Behavior<TextBox>
{
    protected override void OnAttached()
    {
        base.OnAttached();
        AssociatedObject.PreviewTextInput += OnPreviewTextInput;
    }
    
    protected override void OnDetaching()
    {
        base.OnDetaching();
        AssociatedObject.PreviewTextInput -= OnPreviewTextInput;
    }
    
    private void OnPreviewTextInput(object sender, TextCompositionEventArgs e)
    {
        e.Handled = !IsTextNumeric(e.Text);
    }
    
    private static bool IsTextNumeric(string text)
    {
        return int.TryParse(text, out _);
    }
}
```

```xaml
<TextBox>
    <i:Interaction.Behaviors>
        <local:NumericOnlyBehavior />
    </i:Interaction.Behaviors>
</TextBox>
```

### Dialog Service

```csharp
using System.Windows;

public class DialogService
{
    public bool? ShowDialog(Window dialog, Window owner = null)
    {
        if (owner != null)
        {
            dialog.Owner = owner;
            dialog.WindowStartupLocation = WindowStartupLocation.CenterOwner;
        }
        else
        {
            dialog.WindowStartupLocation = WindowStartupLocation.CenterScreen;
        }
        
        return dialog.ShowDialog();
    }
    
    public MessageBoxResult ShowMessage(
        string message, 
        string title = "Information",
        MessageBoxButton buttons = MessageBoxButton.OK,
        MessageBoxImage icon = MessageBoxImage.Information)
    {
        return MessageBox.Show(message, title, buttons, icon);
    }
    
    public bool ShowConfirmation(string message, string title = "Confirm")
    {
        var result = MessageBox.Show(
            message, 
            title, 
            MessageBoxButton.YesNo, 
            MessageBoxImage.Question
        );
        return result == MessageBoxResult.Yes;
    }
}
```

### Property Grid Searcher

```csharp
using Nextended.UI.Helper;
using System.Windows.Controls;

public class PropertySearchExample
{
    private readonly PropertyGridSearcher _searcher;
    
    public PropertySearchExample()
    {
        _searcher = new PropertyGridSearcher();
    }
    
    public void SearchProperties(PropertyGrid propertyGrid, string searchText)
    {
        _searcher.Search(propertyGrid, searchText);
    }
}
```

## Best Practices

### 1. Use MVVM Pattern

```csharp
// ViewModel
public class MainViewModel : ViewModelBase
{
    private ObservableCollection<User> _users;
    
    public ObservableCollection<User> Users
    {
        get => _users;
        set => SetProperty(ref _users, value);
    }
}
```

```xaml
<!-- View -->
<Window DataContext="{Binding Source={StaticResource MainViewModel}}">
    <ListBox ItemsSource="{Binding Users}" />
</Window>
```

### 2. Implement INotifyPropertyChanged

```csharp
public class User : NotificationObject
{
    private string _name;
    
    public string Name
    {
        get => _name;
        set => SetProperty(ref _name, value);
    }
}
```

### 3. Use Commands for User Actions

```csharp
public ICommand DeleteUserCommand { get; }

public MainViewModel()
{
    DeleteUserCommand = new RelayCommand<User>(
        user => DeleteUser(user),
        user => user != null
    );
}
```

### 4. Handle UI Thread Properly

```csharp
public async Task LoadDataAsync()
{
    IsLoading = true;
    
    var data = await Task.Run(() => LoadFromDatabase());
    
    // Update UI on UI thread
    Application.Current.Dispatcher.Invoke(() =>
    {
        Users.Clear();
        foreach (var user in data)
        {
            Users.Add(user);
        }
    });
    
    IsLoading = false;
}
```

## WPF-Specific Features

### Resource Dictionaries

```xaml
<ResourceDictionary>
    <Style x:Key="ModernButtonStyle" TargetType="Button">
        <Setter Property="Background" Value="#2196F3"/>
        <Setter Property="Foreground" Value="White"/>
        <Setter Property="Padding" Value="10,5"/>
        <Setter Property="BorderThickness" Value="0"/>
        <Setter Property="Template">
            <Setter.Value>
                <ControlTemplate TargetType="Button">
                    <!-- Custom template -->
                </ControlTemplate>
            </Setter.Value>
        </Setter>
    </Style>
</ResourceDictionary>
```

### Data Templates

```xaml
<DataTemplate x:Key="UserItemTemplate">
    <StackPanel Orientation="Horizontal" Margin="5">
        <TextBlock Text="{Binding Name}" FontWeight="Bold" Margin="0,0,10,0"/>
        <TextBlock Text="{Binding Email}" Foreground="Gray"/>
    </StackPanel>
</DataTemplate>
```

## Configuration

### App.xaml.cs

```csharp
using System.Windows;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        
        // Initialize services
        InitializeServices();
        
        // Set up exception handling
        DispatcherUnhandledException += OnDispatcherUnhandledException;
        
        // Show main window
        var mainWindow = new MainWindow();
        mainWindow.Show();
    }
    
    private void InitializeServices()
    {
        // Register services, set up DI, etc.
    }
    
    private void OnDispatcherUnhandledException(
        object sender, 
        DispatcherUnhandledExceptionEventArgs e)
    {
        MessageBox.Show(
            $"An error occurred: {e.Exception.Message}",
            "Error",
            MessageBoxButton.OK,
            MessageBoxImage.Error
        );
        
        e.Handled = true;
    }
}
```

## Supported Frameworks

- .NET 8.0 (Windows)
- .NET 9.0 (Windows)

## Platform Support

**Windows Only** - This package requires Windows and will not build or run on Linux or macOS.

## Dependencies

- `Nextended.Core` - Core utilities and extensions
- `Microsoft.Xaml.Behaviors.Wpf` - WPF behaviors support

## Related Projects

- [Nextended.Core](core.md) - Foundation library with NotificationObject base classes

## Links

- [NuGet Package](https://www.nuget.org/packages/Nextended.UI/)
- [Source Code](https://github.com/fgilde/Nextended/tree/main/Nextended.UI)
- [Report Issues](https://github.com/fgilde/Nextended/issues)
