#requires -Version 7.0
<#
.SYNOPSIS
    Renders every package listing, README header/footer and the docs-site icon from
    one source of truth: docs/_data/packages.json.

.DESCRIPTION
    Nextended ships 18 NuGet packages. Keeping the package table, the framework
    list, the dependency list and the cross-links correct by hand across the root
    README, 18 project READMEs and the documentation site does not work — it drifted
    before. This script owns those parts instead.

    What it writes
    --------------
      * root README.md .......... HEADER + full PACKAGES table + FOOTER
      * <Package>/README.md ..... HEADER + FOOTER (the body in between is hand-written
                                  and never touched)
      * docs/assets/icon.png .... copy of the single root icon.png, for the Jekyll site
      * docs/_config.yml ........ favicon/logo reference (only if absent)

    Everything it writes lives between HTML comment markers:

        <!-- NEXTENDED:HEADER:START -->  ... generated ...  <!-- NEXTENDED:HEADER:END -->
        <!-- NEXTENDED:PACKAGES:START --> ... generated ... <!-- NEXTENDED:PACKAGES:END -->
        <!-- NEXTENDED:FOOTER:START -->  ... generated ...  <!-- NEXTENDED:FOOTER:END -->

    A README without a given marker pair simply does not get that block — the script
    never invents structure, so hand-written prose is safe.

    The documentation site does NOT use this script for its listings: docs/index.md
    and docs/projects/README.md (plus the German mirror) render the same
    docs/_data/packages.json through Liquid at build time, so they are always current
    without a generator step.

.PARAMETER Check
    Validate and report drift without writing anything. Exit code 1 when a package is
    missing from packages.json, when packages.json names a project that does not
    exist, or when any generated block is out of date. Use this in CI.

.PARAMETER SkipIcon
    Do not copy the root icon.png into docs/assets/.

.NOTES
    The data file format (docs/_data/packages.json) is JSON rather than YAML on
    purpose: Jekyll and PowerShell both parse JSON with zero extra dependencies.
    It therefore cannot carry comments, so the field reference lives here:

      meta.repo          repository base URL
      meta.docsSite      published documentation site URL
      meta.iconSource    the ONE icon file, relative to the repo root
      meta.iconRawUrl    raw URL of that icon (READMEs must use an absolute URL,
                         because relative image paths break on nuget.org)
      categories[]       id + display name per language, defines listing order
      packages[]         id, name (= PackageId = folder name), category, slug
                         (docs page file name), frameworks[], dependencies[],
                         optional sample (repo-relative path to a sample project),
                         summary.en / summary.de (one sentence, used everywhere)

    Deliberately excluded from all listings: Nextended.AutoDto (superseded by
    Nextended.CodeGen, not part of Nextended.sln).

.EXAMPLE
    pwsh tools/Update-PackageDocs.ps1
    Regenerate every block. Run this after editing docs/_data/packages.json or after
    replacing icon.png.

.EXAMPLE
    pwsh tools/Update-PackageDocs.ps1 -Check
    CI mode: fail if anything is stale.
#>
[CmdletBinding()]
param(
    [switch] $Check,
    [switch] $SkipIcon,
    [switch] $IfNeeded
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent $PSScriptRoot
$dataFile = Join-Path $repoRoot 'docs/_data/packages.json'

# ---------------------------------------------------------------------------
# -IfNeeded: the mode the MSBuild target (Directory.Build.targets) uses.
#
# MSBuild has no clean "run once per build" hook for a multi-targeting project: building
# anything that references Nextended.Core invokes Core's inner builds per TFM, so the target
# can fire up to three times concurrently. Rather than fight that in MSBuild, the run-once
# logic lives here:
#
#   1. a stamp file newer than every input means there is nothing to do -> return immediately
#   2. otherwise take a machine-wide named mutex, so concurrent inner builds serialise instead
#      of writing the same README files at the same time
#   3. re-check inside the lock, because the build that held the mutex may have just done it
#
# Without -IfNeeded the script always does its work, which is what a human or CI wants.
# ---------------------------------------------------------------------------

$stampFile = Join-Path $repoRoot 'artifacts/docs-sync.stamp'
$iconFile = Join-Path $repoRoot 'icon.png'

function Test-DocsSyncNeeded {
    if (-not (Test-Path $stampFile)) { return $true }
    $stamp = (Get-Item $stampFile).LastWriteTimeUtc
    foreach ($input in @($dataFile, $iconFile, $PSCommandPath)) {
        if ((Test-Path $input) -and (Get-Item $input).LastWriteTimeUtc -gt $stamp) { return $true }
    }
    return $false
}

$mutex = $null
if ($IfNeeded) {
    if (-not (Test-DocsSyncNeeded)) { exit 0 }

    $mutex = [System.Threading.Mutex]::new($false, 'Global\Nextended-UpdatePackageDocs')
    try { $null = $mutex.WaitOne(60000) } catch [System.Threading.AbandonedMutexException] { }

    # Another inner build may have finished the work while we waited.
    if (-not (Test-DocsSyncNeeded)) {
        $mutex.ReleaseMutex(); $mutex.Dispose()
        exit 0
    }
}

# Packable projects that are intentionally absent from the listings.
$ignoredProjects = @('Nextended.AutoDto')

$generatedNotice = 'generated by tools/Update-PackageDocs.ps1 — do not edit by hand'

if (-not (Test-Path $dataFile)) {
    throw "Data file not found: $dataFile"
}

$data = Get-Content -Raw -LiteralPath $dataFile | ConvertFrom-Json
$meta = $data.meta
$packages = $data.packages
$categories = $data.categories

$problems = [System.Collections.Generic.List[string]]::new()
$staleFiles = [System.Collections.Generic.List[string]]::new()
$writtenFiles = [System.Collections.Generic.List[string]]::new()

# ---------------------------------------------------------------------------
# Validation — packages.json against the projects that actually exist
# ---------------------------------------------------------------------------

function Get-PackableProjectNames {
    Get-ChildItem -LiteralPath $repoRoot -Directory |
        Where-Object { $_.Name -like 'Nextended*' } |
        ForEach-Object {
            $csproj = Join-Path $_.FullName "$($_.Name).csproj"
            if (Test-Path $csproj) { $_.Name }
        }
}

$onDisk = @(Get-PackableProjectNames)
$listed = @($packages | ForEach-Object { $_.name })

foreach ($name in $onDisk) {
    if ($ignoredProjects -contains $name) { continue }
    if ($listed -notcontains $name) {
        $problems.Add("Project '$name' has a csproj but no entry in docs/_data/packages.json.")
    }
}
foreach ($name in $listed) {
    if ($onDisk -notcontains $name) {
        $problems.Add("packages.json lists '$name' but $name/$name.csproj does not exist.")
    }
}
foreach ($pkg in $packages) {
    if ($categories.id -notcontains $pkg.category) {
        $problems.Add("Package '$($pkg.name)' uses unknown category '$($pkg.category)'.")
    }
    $docsPage = Join-Path $repoRoot "docs/projects/$($pkg.slug).md"
    if (-not (Test-Path $docsPage)) {
        $problems.Add("Package '$($pkg.name)' points at missing docs page docs/projects/$($pkg.slug).md.")
    }
    if ($pkg.PSObject.Properties.Name -contains 'sample' -and $pkg.sample) {
        if (-not (Test-Path (Join-Path $repoRoot $pkg.sample))) {
            $problems.Add("Package '$($pkg.name)' points at missing sample '$($pkg.sample)'.")
        }
    }
}

# ---------------------------------------------------------------------------
# Rendering helpers
# ---------------------------------------------------------------------------

function Get-NuGetUrl([string] $name) { "https://www.nuget.org/packages/$name/" }
function Get-DocsUrlEn([object] $pkg) { "$($meta.repo)/blob/main/docs/projects/$($pkg.slug).md" }
function Get-DocsUrlDe([object] $pkg) { "$($meta.repo)/blob/main/docs/de/projects/$($pkg.slug).md" }
function Get-SourceUrl([object] $pkg) { "$($meta.repo)/tree/main/$($pkg.name)" }
function Get-SampleUrl([object] $pkg) { "$($meta.repo)/tree/main/$($pkg.sample)" }

function Test-HasSample([object] $pkg) {
    ($pkg.PSObject.Properties.Name -contains 'sample') -and $pkg.sample
}

function Get-CategoryName([string] $id, [string] $lang) {
    $cat = $categories | Where-Object { $_.id -eq $id } | Select-Object -First 1
    if ($lang -eq 'de') { $cat.de } else { $cat.en }
}

# Full grouped package table, used in the root README.
function Format-PackageTable([string] $lang) {
    $sb = [System.Text.StringBuilder]::new()
    $headers = if ($lang -eq 'de') {
        @('Paket', 'Beschreibung', 'NuGet', 'Doku')
    } else {
        @('Package', 'Description', 'NuGet', 'Docs')
    }

    foreach ($cat in $categories) {
        $inCat = @($packages | Where-Object { $_.category -eq $cat.id })
        if ($inCat.Count -eq 0) { continue }

        [void]$sb.AppendLine("### $(Get-CategoryName $cat.id $lang)")
        [void]$sb.AppendLine()
        [void]$sb.AppendLine("| $($headers -join ' | ') |")
        [void]$sb.AppendLine('| --- | --- | --- | --- |')

        foreach ($pkg in $inCat) {
            $summary = if ($lang -eq 'de') { $pkg.summary.de } else { $pkg.summary.en }
            $docsUrl = if ($lang -eq 'de') { Get-DocsUrlDe $pkg } else { Get-DocsUrlEn $pkg }
            $docsLabel = if ($lang -eq 'de') { 'Doku' } else { 'Docs' }
            $links = "[$docsLabel]($docsUrl)"
            if (Test-HasSample $pkg) {
                $sampleLabel = if ($lang -eq 'de') { 'Beispiel' } else { 'Sample' }
                $links += " · [$sampleLabel]($(Get-SampleUrl $pkg))"
            }
            $badge = "[![NuGet](https://img.shields.io/nuget/v/$($pkg.name).svg)]($(Get-NuGetUrl $pkg.name))"
            [void]$sb.AppendLine("| **[$($pkg.name)]($($pkg.name)/README.md)** | $summary | $badge | $links |")
        }
        [void]$sb.AppendLine()
    }

    $sb.ToString().TrimEnd()
}

# Compact cross-reference list for a project README, self excluded, collapsed.
function Format-RelatedPackages([object] $self) {
    $sb = [System.Text.StringBuilder]::new()
    [void]$sb.AppendLine('<details>')
    [void]$sb.AppendLine('<summary>All 18 Nextended packages</summary>')
    [void]$sb.AppendLine()

    foreach ($cat in $categories) {
        $inCat = @($packages | Where-Object { $_.category -eq $cat.id })
        if ($inCat.Count -eq 0) { continue }
        [void]$sb.AppendLine("**$(Get-CategoryName $cat.id 'en')**")
        [void]$sb.AppendLine()
        foreach ($pkg in $inCat) {
            if ($pkg.name -eq $self.name) {
                [void]$sb.AppendLine("- **$($pkg.name)** — $($pkg.summary.en) _(this package)_")
            } else {
                [void]$sb.AppendLine("- [$($pkg.name)]($($meta.repo)/blob/main/$($pkg.name)/README.md) — $($pkg.summary.en)")
            }
        }
        [void]$sb.AppendLine()
    }

    [void]$sb.AppendLine('</details>')
    $sb.ToString().TrimEnd()
}

function Format-ProjectHeader([object] $pkg) {
    $sb = [System.Text.StringBuilder]::new()
    [void]$sb.AppendLine("<p align=""center"">")
    [void]$sb.AppendLine("  <img src=""$($meta.iconRawUrl)"" alt=""Nextended"" width=""110"" height=""110"">")
    [void]$sb.AppendLine('</p>')
    [void]$sb.AppendLine()
    [void]$sb.AppendLine("# $($pkg.name)")
    [void]$sb.AppendLine()
    [void]$sb.AppendLine("[![NuGet](https://img.shields.io/nuget/v/$($pkg.name).svg)]($(Get-NuGetUrl $pkg.name))")
    [void]$sb.AppendLine("[![Downloads](https://img.shields.io/nuget/dt/$($pkg.name).svg)]($(Get-NuGetUrl $pkg.name))")
    [void]$sb.AppendLine("[![License](https://img.shields.io/github/license/fgilde/Nextended)]($($meta.repo)/blob/main/LICENSE)")
    [void]$sb.AppendLine()
    [void]$sb.AppendLine("> $($pkg.summary.en)")
    [void]$sb.AppendLine()

    $nav = "📖 **Documentation:** [English]($(Get-DocsUrlEn $pkg)) · [Deutsch]($(Get-DocsUrlDe $pkg))"
    if (Test-HasSample $pkg) {
        $nav += " &nbsp;|&nbsp; 🧪 **Runnable sample:** [$(Split-Path -Leaf $pkg.sample)]($(Get-SampleUrl $pkg))"
    }
    [void]$sb.AppendLine($nav)

    $sb.ToString().TrimEnd()
}

function Format-ProjectFooter([object] $pkg) {
    $sb = [System.Text.StringBuilder]::new()

    [void]$sb.AppendLine('## Supported frameworks')
    [void]$sb.AppendLine()
    foreach ($fw in $pkg.frameworks) { [void]$sb.AppendLine("- ``$fw``") }
    [void]$sb.AppendLine()

    [void]$sb.AppendLine('## Dependencies')
    [void]$sb.AppendLine()
    if (@($pkg.dependencies).Count -eq 0) {
        [void]$sb.AppendLine('No Nextended dependencies — this is the root of the dependency tree.')
    } else {
        foreach ($dep in $pkg.dependencies) {
            if ($listed -contains $dep) {
                [void]$sb.AppendLine("- [$dep]($($meta.repo)/blob/main/$dep/README.md)")
            } else {
                [void]$sb.AppendLine("- $dep")
            }
        }
    }
    [void]$sb.AppendLine()

    [void]$sb.AppendLine('## The Nextended family')
    [void]$sb.AppendLine()
    [void]$sb.AppendLine((Format-RelatedPackages $pkg))
    [void]$sb.AppendLine()

    [void]$sb.AppendLine('## Links')
    [void]$sb.AppendLine()
    [void]$sb.AppendLine("- 📦 [NuGet package]($(Get-NuGetUrl $pkg.name))")
    [void]$sb.AppendLine("- 📖 [Documentation — English]($(Get-DocsUrlEn $pkg))")
    [void]$sb.AppendLine("- 📖 [Dokumentation — Deutsch]($(Get-DocsUrlDe $pkg))")
    [void]$sb.AppendLine("- 🏠 [Documentation portal]($($meta.docsSite))")
    if (Test-HasSample $pkg) {
        [void]$sb.AppendLine("- 🧪 [Runnable sample]($(Get-SampleUrl $pkg))")
    }
    [void]$sb.AppendLine("- 🧑‍💻 [Source code]($(Get-SourceUrl $pkg))")
    [void]$sb.AppendLine("- 🐛 [Report an issue]($($meta.repo)/issues)")
    [void]$sb.AppendLine()
    [void]$sb.AppendLine('## License')
    [void]$sb.AppendLine()
    [void]$sb.AppendLine("GPL-3.0-or-later — see [LICENSE]($($meta.repo)/blob/main/LICENSE).")

    $sb.ToString().TrimEnd()
}

# ---------------------------------------------------------------------------
# Marker replacement
# ---------------------------------------------------------------------------

function Set-MarkerBlock {
    param(
        [Parameter(Mandatory)] [string] $Content,
        [Parameter(Mandatory)] [string] $Marker,
        [Parameter(Mandatory)] [AllowEmptyString()] [string] $Body,
        [Parameter(Mandatory)] [ref] $Changed
    )

    $startTag = "<!-- NEXTENDED:${Marker}:START -->"
    $endTag = "<!-- NEXTENDED:${Marker}:END -->"
    $startTagAny = "<!-- NEXTENDED:${Marker}:START"

    # Tolerate the trailing notice inside the start tag, e.g.
    #   <!-- NEXTENDED:HEADER:START - generated by ... -->
    $startIdx = $Content.IndexOf($startTagAny, [StringComparison]::Ordinal)
    if ($startIdx -lt 0) { return $Content }

    $startLineEnd = $Content.IndexOf('-->', $startIdx, [StringComparison]::Ordinal)
    if ($startLineEnd -lt 0) { return $Content }
    $startLineEnd += 3

    $endIdx = $Content.IndexOf($endTag, $startLineEnd, [StringComparison]::Ordinal)
    if ($endIdx -lt 0) {
        throw "Marker '$Marker' has a START but no END tag."
    }

    $replacement = "<!-- NEXTENDED:${Marker}:START $generatedNotice -->" + [Environment]::NewLine +
                   $Body + [Environment]::NewLine

    $newContent = $Content.Substring(0, $startIdx) + $replacement + $Content.Substring($endIdx)
    if ($newContent -ne $Content) { $Changed.Value = $true }
    return $newContent
}

function Save-IfChanged([string] $path, [string] $content, [bool] $changed) {
    $rel = [System.IO.Path]::GetRelativePath($repoRoot, $path)
    if (-not $changed) { return }

    if ($Check) {
        $staleFiles.Add($rel)
        return
    }
    # UTF-8 without BOM, LF-agnostic (git handles eol via .gitattributes/core.autocrlf)
    [System.IO.File]::WriteAllText($path, $content, [System.Text.UTF8Encoding]::new($false))
    $writtenFiles.Add($rel)
}

# ---------------------------------------------------------------------------
# Project READMEs
# ---------------------------------------------------------------------------

foreach ($pkg in $packages) {
    $readme = Join-Path $repoRoot "$($pkg.name)/README.md"
    if (-not (Test-Path $readme)) {
        $problems.Add("Missing README: $($pkg.name)/README.md")
        continue
    }

    $content = Get-Content -Raw -LiteralPath $readme
    $changed = $false
    $content = Set-MarkerBlock -Content $content -Marker 'HEADER' -Body (Format-ProjectHeader $pkg) -Changed ([ref]$changed)
    $content = Set-MarkerBlock -Content $content -Marker 'FOOTER' -Body (Format-ProjectFooter $pkg) -Changed ([ref]$changed)
    Save-IfChanged $readme $content $changed
}

# ---------------------------------------------------------------------------
# Root README
# ---------------------------------------------------------------------------

$rootReadme = Join-Path $repoRoot 'README.md'
if (Test-Path $rootReadme) {
    $content = Get-Content -Raw -LiteralPath $rootReadme
    $changed = $false

    $rootHeader = @"
<p align="center">
  <img src="$($meta.iconRawUrl)" alt="Nextended" width="140" height="140">
</p>

<h1 align="center">Nextended</h1>

<p align="center">
  <a href="$(Get-NuGetUrl 'Nextended.Core')"><img src="https://img.shields.io/nuget/v/Nextended.Core.svg" alt="NuGet"></a>
  <a href="$($meta.repo)/blob/main/LICENSE"><img src="https://img.shields.io/github/license/fgilde/Nextended" alt="License"></a>
  <a href="$($meta.docsSite)"><img src="https://img.shields.io/badge/docs-en%20%7C%20de-blue" alt="Documentation"></a>
</p>

<p align="center">
  A suite of $(@($packages).Count) .NET libraries: extension methods, custom types, caching, EF Core,
  ASP.NET Core response filtering, source generation and .NET Aspire hosting integrations.
</p>

<p align="center">
  📖 <a href="$($meta.docsSite)">Documentation (English)</a> ·
  📖 <a href="$($meta.repo)/blob/main/docs/de/index.md">Dokumentation (Deutsch)</a>
</p>
"@
    $content = Set-MarkerBlock -Content $content -Marker 'HEADER' -Body $rootHeader.TrimEnd() -Changed ([ref]$changed)
    $content = Set-MarkerBlock -Content $content -Marker 'PACKAGES' -Body (Format-PackageTable 'en') -Changed ([ref]$changed)
    Save-IfChanged $rootReadme $content $changed
}

# ---------------------------------------------------------------------------
# Icon — one source file, copied where a copy is unavoidable (Jekyll assets)
# ---------------------------------------------------------------------------

if (-not $SkipIcon) {
    $iconSource = Join-Path $repoRoot $meta.iconSource
    if (-not (Test-Path $iconSource)) {
        $problems.Add("Icon source not found: $($meta.iconSource)")
    } else {
        $iconTarget = Join-Path $repoRoot 'docs/assets/icon.png'
        $targetDir = Split-Path -Parent $iconTarget
        if (-not (Test-Path $targetDir)) {
            if (-not $Check) { New-Item -ItemType Directory -Path $targetDir -Force | Out-Null }
        }

        $sourceHash = (Get-FileHash -LiteralPath $iconSource -Algorithm SHA256).Hash
        $targetHash = if (Test-Path $iconTarget) {
            (Get-FileHash -LiteralPath $iconTarget -Algorithm SHA256).Hash
        } else { '' }

        if ($sourceHash -ne $targetHash) {
            if ($Check) {
                $staleFiles.Add('docs/assets/icon.png')
            } else {
                Copy-Item -LiteralPath $iconSource -Destination $iconTarget -Force
                $writtenFiles.Add('docs/assets/icon.png')
            }
        }
    }
}

# ---------------------------------------------------------------------------
# Report
# ---------------------------------------------------------------------------

function Complete-Run([int] $exitCode, [bool] $writeStamp) {
    if ($writeStamp) {
        $stampDir = Split-Path -Parent $stampFile
        if (-not (Test-Path $stampDir)) { New-Item -ItemType Directory -Force $stampDir | Out-Null }
        Set-Content -LiteralPath $stampFile -Value (Get-Date -Format 'o') -NoNewline
    }
    if ($null -ne $mutex) {
        try { $mutex.ReleaseMutex() } catch { }
        $mutex.Dispose()
    }
    exit $exitCode
}

foreach ($p in $problems) { Write-Warning $p }

if ($Check) {
    foreach ($f in $staleFiles) { Write-Host "STALE   $f" -ForegroundColor Yellow }
    if ($problems.Count -eq 0 -and $staleFiles.Count -eq 0) {
        Write-Host "All package listings, README blocks and the docs icon are up to date." -ForegroundColor Green
        Complete-Run 0 $false
    }
    Write-Host ""
    Write-Host "$($staleFiles.Count) stale file(s), $($problems.Count) problem(s). Run: pwsh tools/Update-PackageDocs.ps1" -ForegroundColor Red
    Complete-Run 1 $false
}

foreach ($f in $writtenFiles) { Write-Host "updated $f" -ForegroundColor Green }
Write-Host ""
Write-Host "$($writtenFiles.Count) file(s) updated, $($problems.Count) problem(s)."

# Only stamp a clean run, so a consistency problem keeps re-reporting on the next build.
Complete-Run ($problems.Count -gt 0 ? 1 : 0) ($problems.Count -eq 0)
