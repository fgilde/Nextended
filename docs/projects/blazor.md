# Nextended.Blazor

Blazor-specific helpers, extensions, and utilities for building modern web applications.

## Overview

Nextended.Blazor provides a collection of utilities and extensions specifically designed for Blazor applications, including component helpers, JavaScript interop utilities, and navigation extensions.

## Installation

```bash
dotnet add package Nextended.Blazor
```

## Key Features

### 1. JavaScript Interop Helpers

Simplified JavaScript interop for common operations.

```csharp
@inject IJSRuntime JSRuntime
@using Nextended.Blazor.Extensions

@code {
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            // Call JavaScript function
            await JSRuntime.InvokeVoidAsync("console.log", "Component initialized");
            
            // Get value from JavaScript
            var windowWidth = await JSRuntime.InvokeAsync<int>("eval", "window.innerWidth");
        }
    }
}
```

### 2. Component Helpers

Utilities for working with Blazor components.

```csharp
@using Nextended.Blazor.Helper

@code {
    private ElementReference myElement;
    
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            // Focus element
            await ComponentHelper.FocusAsync(myElement);
        }
    }
}
```

### 3. Navigation Extensions

Enhanced navigation utilities for Blazor applications.

```csharp
@inject NavigationManager Navigation
@using Nextended.Blazor.Extensions

@code {
    private void NavigateToProfile()
    {
        // Navigate with parameters
        Navigation.NavigateTo($"/profile/{userId}");
        
        // Navigate with force reload
        Navigation.NavigateTo("/home", forceLoad: true);
    }
    
    private string GetCurrentPage()
    {
        return Navigation.ToBaseRelativePath(Navigation.Uri);
    }
}
```

### 4. Localization Support

Simplified localization helpers for Blazor components.

```csharp
@inject IStringLocalizer<Resources> Localizer
@using Nextended.Blazor.Extensions

<h1>@Localizer["Welcome"]</h1>
<p>@Localizer["Description"]</p>

@code {
    private string GetLocalizedMessage(string key)
    {
        return Localizer[key];
    }
}
```

## Usage Examples

### Custom Component with Extensions

```razor
@using Nextended.Blazor.Extensions
@using Nextended.Core.Extensions
@inject IJSRuntime JSRuntime

<div @ref="containerRef" class="user-card">
    <h3>@User.Name.ToPascalCase()</h3>
    <p>@User.Email</p>
    <button @onclick="CopyEmail">Copy Email</button>
</div>

@code {
    [Parameter]
    public User User { get; set; }
    
    private ElementReference containerRef;
    
    private async Task CopyEmail()
    {
        await JSRuntime.InvokeVoidAsync(
            "navigator.clipboard.writeText", 
            User.Email
        );
        
        // Show notification
        await JSRuntime.InvokeVoidAsync(
            "alert", 
            "Email copied to clipboard!"
        );
    }
}
```

### Data Loading Component

```razor
@using Nextended.Blazor.Models
@using Nextended.Core.Extensions
@typeparam TItem

<div class="data-container">
    @if (IsLoading)
    {
        <div class="spinner">Loading...</div>
    }
    else if (HasError)
    {
        <div class="error">@ErrorMessage</div>
        <button @onclick="Reload">Retry</button>
    }
    else if (Items?.Any() == true)
    {
        @ChildContent(Items)
    }
    else
    {
        <div class="no-data">No items found</div>
    }
</div>

@code {
    [Parameter]
    public Func<Task<List<TItem>>> LoadData { get; set; }
    
    [Parameter]
    public RenderFragment<List<TItem>> ChildContent { get; set; }
    
    private List<TItem> Items { get; set; }
    private bool IsLoading { get; set; }
    private bool HasError { get; set; }
    private string ErrorMessage { get; set; }
    
    protected override async Task OnInitializedAsync()
    {
        await LoadDataAsync();
    }
    
    private async Task LoadDataAsync()
    {
        IsLoading = true;
        HasError = false;
        StateHasChanged();
        
        try
        {
            Items = await LoadData();
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsLoading = false;
            StateHasChanged();
        }
    }
    
    private async Task Reload()
    {
        await LoadDataAsync();
    }
}
```

### Form Component with Validation

```razor
@using Microsoft.AspNetCore.Components.Forms
@using Nextended.Core.Extensions

<EditForm Model="@Model" OnValidSubmit="@HandleValidSubmit">
    <DataAnnotationsValidator />
    <ValidationSummary />
    
    <div class="form-group">
        <label>Name:</label>
        <InputText @bind-Value="Model.Name" class="form-control" />
        <ValidationMessage For="@(() => Model.Name)" />
    </div>
    
    <div class="form-group">
        <label>Email:</label>
        <InputText @bind-Value="Model.Email" class="form-control" />
        <ValidationMessage For="@(() => Model.Email)" />
    </div>
    
    <button type="submit" class="btn btn-primary">Submit</button>
</EditForm>

@code {
    [Parameter]
    public UserModel Model { get; set; }
    
    [Parameter]
    public EventCallback<UserModel> OnSubmit { get; set; }
    
    private async Task HandleValidSubmit()
    {
        await OnSubmit.InvokeAsync(Model);
    }
}
```

### Interactive Data Table

```razor
@using Nextended.Core.Extensions
@typeparam TItem

<div class="data-table">
    <div class="table-header">
        <input type="text" 
               @bind="searchTerm" 
               @bind:event="oninput"
               placeholder="Search..." 
               class="search-box" />
    </div>
    
    <table class="table">
        <thead>
            <tr>
                @HeaderContent
            </tr>
        </thead>
        <tbody>
            @if (FilteredItems?.Any() == true)
            {
                foreach (var item in FilteredItems)
                {
                    <tr>
                        @RowContent(item)
                    </tr>
                }
            }
            else
            {
                <tr>
                    <td colspan="100">No items found</td>
                </tr>
            }
        </tbody>
    </table>
    
    <div class="table-footer">
        <span>Showing @FilteredItems.Count of @Items.Count items</span>
    </div>
</div>

@code {
    [Parameter]
    public List<TItem> Items { get; set; } = new();
    
    [Parameter]
    public RenderFragment HeaderContent { get; set; }
    
    [Parameter]
    public RenderFragment<TItem> RowContent { get; set; }
    
    [Parameter]
    public Func<TItem, string, bool> FilterFunction { get; set; }
    
    private string searchTerm = string.Empty;
    
    private List<TItem> FilteredItems => 
        string.IsNullOrWhiteSpace(searchTerm) 
            ? Items 
            : Items.Where(item => FilterFunction?.Invoke(item, searchTerm) ?? true).ToList();
}
```

## Best Practices

### 1. Use StateHasChanged Wisely

```csharp
protected override async Task OnInitializedAsync()
{
    // Load data
    data = await LoadDataAsync();
    
    // Trigger re-render
    StateHasChanged();
}
```

### 2. Implement Proper Disposal

```csharp
@implements IDisposable

@code {
    private Timer timer;
    
    protected override void OnInitialized()
    {
        timer = new Timer(async _ => await UpdateData(), null, 0, 5000);
    }
    
    public void Dispose()
    {
        timer?.Dispose();
    }
}
```

### 3. Use Cascading Parameters for Shared State

```razor
<CascadingValue Value="@currentUser">
    @ChildContent
</CascadingValue>

@code {
    [Parameter]
    public RenderFragment ChildContent { get; set; }
    
    private User currentUser;
}
```

### 4. Optimize Rendering with ShouldRender

```csharp
private int updateCount = 0;

protected override bool ShouldRender()
{
    updateCount++;
    
    // Only render every 5th update
    return updateCount % 5 == 0;
}
```

## Configuration

### Program.cs Setup

```csharp
using Nextended.Blazor.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Add Blazor services
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();

// Add Nextended.Blazor services (if any)
// builder.Services.AddNextendedBlazor();

var app = builder.Build();

app.UseStaticFiles();
app.UseRouting();

app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Run();
```

## Supported Frameworks

- .NET 8.0
- .NET 9.0

## Dependencies

- `Nextended.Core` - Core utilities and extensions
- `Microsoft.AspNetCore.Components.Web` - Blazor web components
- `Microsoft.Extensions.Localization.Abstractions` - Localization support

## Related Projects

- [Nextended.Core](core.md) - Foundation library with extensions
- [Nextended.Web](web.md) - ASP.NET Core web utilities

## Links

- [NuGet Package](https://www.nuget.org/packages/Nextended.Blazor/)
- [Source Code](https://github.com/fgilde/Nextended/tree/main/Nextended.Blazor)
- [Report Issues](https://github.com/fgilde/Nextended/issues)
