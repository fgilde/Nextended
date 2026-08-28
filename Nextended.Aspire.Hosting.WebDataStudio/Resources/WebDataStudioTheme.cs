using System.ComponentModel;

namespace Nextended.Aspire.Hosting.WebDataStudio.Resources;

/// <summary>
/// The theme a studio comes up in. The description of each value is the id the studio itself uses,
/// so the two lists cannot drift apart silently: <c>WithTheme(WebDataStudioTheme.Ocean)</c> sets
/// <c>ocean</c>, and <c>WithTheme(string)</c> stays available for a theme this enum does not know
/// yet.
/// </summary>
/// <remarks>
/// It is the <em>initial</em> theme. Whoever opens the studio may pick another one, and that choice
/// is theirs from then on — a deployment says where to start, not what to look at forever.
/// </remarks>
public enum WebDataStudioTheme
{
    /// <summary>The studio's own dark theme, and its default.</summary>
    [Description("ocean")]
    Ocean,

    /// <summary>GitHub's dark palette.</summary>
    [Description("github-dark")]
    GitHubDark,

    /// <summary>GitHub's light palette.</summary>
    [Description("github-light")]
    GitHubLight,

    /// <summary>The Aspire dashboard's violet on near-black — the one to pick inside Aspire.</summary>
    [Description("aspire")]
    AspireDashboard,

    /// <summary>Blazor purple on white.</summary>
    [Description("blazor")]
    Blazor,

    /// <summary>Dracula.</summary>
    [Description("dracula")]
    Dracula,

    /// <summary>Nord.</summary>
    [Description("nord")]
    Nord,

    /// <summary>One Dark, as in the editor.</summary>
    [Description("one-dark")]
    OneDark,

    /// <summary>Monokai.</summary>
    [Description("monokai")]
    Monokai,

    /// <summary>Green on black, monospaced everywhere, no rounded corners.</summary>
    [Description("terminal")]
    Terminal,

    /// <summary>Solarized, dark.</summary>
    [Description("solarized-dark")]
    SolarizedDark,

    /// <summary>Solarized, light.</summary>
    [Description("solarized-light")]
    SolarizedLight,

    /// <summary>Magenta neon on near-black.</summary>
    [Description("neon-glow")]
    NeonGlow,

    /// <summary>Synthwave pink and violet.</summary>
    [Description("synthwave")]
    Synthwave,

    /// <summary>Cyan on deep teal.</summary>
    [Description("hologram")]
    Hologram,

    /// <summary>Hot pink on aubergine.</summary>
    [Description("nightlife")]
    Nightlife,

    /// <summary>Near-black with very little colour — the quiet one.</summary>
    [Description("obsidian")]
    Obsidian,

    /// <summary>High contrast and large type, for a screen somebody is presenting from.</summary>
    [Description("stage")]
    Stage,

    /// <summary>Dense and plain, for the machine you work on all day.</summary>
    [Description("dev")]
    Dev,

    /// <summary>For a studio that is mostly a page of links.</summary>
    [Description("link-hub")]
    LinkHub,

    /// <summary>Light, calm and large, for a screen nobody sits in front of.</summary>
    [Description("kiosk")]
    Kiosk,
}
