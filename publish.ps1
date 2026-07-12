#!/usr/bin/env pwsh
<#
.SYNOPSIS
    One-shot NuGet publish for the Nextended monorepo — one version number, no waiting.

.DESCRIPTION
    The pain this removes: Nextended.CodeGen is a Roslyn analyzer that references Nextended.Core as a
    *NuGet package* (pinned to <UsedCorePackageVersion>). Every other project references Core as a
    ProjectReference and is uncritical. So historically you had to:
        1. publish Core (+ the rest) to nuget.org,
        2. WAIT for nuget.org to index it,
        3. bump <UsedCorePackageVersion> to match,
        4. only then build & publish CodeGen.

    This script instead packs Core locally and feeds it to the CodeGen build via a transient local
    source (RestoreAdditionalProjectSources) — so CodeGen restores Core X.Y.Z from disk, not nuget.org.
    Result: a single version, a single run, no indexing wait.

    Flow:
        set both versions -> pack all non-CodeGen libs -> stage Core into a local feed
        -> pack CodeGen against that feed -> preflight-check nuget.org -> (optional) push (Core first)
        -> (optional) tag v<version>.

    Nothing is pushed unless you pass -Push. Without it you get a full dry run that produces the
    .nupkg files under .\artifacts for inspection.

.PARAMETER Version
    The package version to publish, e.g. 10.1.13. Sets BOTH <PackageVersion> and
    <UsedCorePackageVersion> in Version.props to this value.

.PARAMETER Push
    Actually push to nuget.org. Omit for a build/pack-only dry run.

.PARAMETER Preflight
    Query nuget.org and report which packages already exist at this version — then exit. Builds
    nothing, changes nothing (this is the "-WhatIf" pre-check). No API key required.

.PARAMETER ApiKey
    nuget.org API key. Falls back to $env:NUGET_API_KEY. Only needed with -Push.

.PARAMETER Configuration
    Build configuration. Default: Release.

.PARAMETER Fresh
    Purge the global-package-cache entries for nextended.core / nextended.codegen at this version
    before packing CodeGen. Use only when RE-publishing the same version after changing Core.

.PARAMETER SkipTag
    Do not create/push the git tag v<version> after a successful -Push.

.EXAMPLE
    ./publish.ps1 -Version 10.1.13 -Preflight       # just check nuget.org, build nothing
    ./publish.ps1 -Version 10.1.13                   # dry run: pack everything into .\artifacts
    ./publish.ps1 -Version 10.1.13 -Push             # pack + push + tag
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [ValidatePattern('^\d+\.\d+\.\d+(-[0-9A-Za-z.-]+)?$')]
    [string]$Version,

    [switch]$Push,
    [switch]$Preflight,
    [string]$ApiKey = $env:NUGET_API_KEY,
    [string]$Configuration = 'Release',
    [switch]$Fresh,
    [switch]$SkipTag
)

$ErrorActionPreference = 'Stop'
$repo      = $PSScriptRoot
$artifacts = Join-Path $repo 'artifacts'
$feed      = Join-Path $repo 'localfeed'
$source    = 'https://api.nuget.org/v3/index.json'

function Step($msg) { Write-Host "`n=== $msg ===" -ForegroundColor Cyan }
function Info($msg) { Write-Host "    $msg" -ForegroundColor DarkGray }
function Warn($msg) { Write-Host "    $msg" -ForegroundColor Yellow }

function Invoke-Dotnet {
    param([string[]]$DotnetArgs)
    & dotnet @DotnetArgs
    if ($LASTEXITCODE -ne 0) { throw "dotnet $($DotnetArgs -join ' ') failed (exit $LASTEXITCODE)" }
}

# Is <version> already published for <id> on nuget.org? (404 = id never published = not present.)
function Test-VersionPublished([string]$id, [string]$ver) {
    $url = "https://api.nuget.org/v3-flatcontainer/$($id.ToLowerInvariant())/index.json"
    try   { return @((Invoke-RestMethod $url -ErrorAction Stop).versions) -contains $ver }
    catch { return $false }
}

# Discover packable projects (GeneratePackageOnBuild/IsPackable, excluding Tests), ordered Core-first,
# CodeGen-last. PackageId == project name (AssemblyName), so BaseName is the package id.
function Resolve-Packages {
    $packable = Get-ChildItem -Path $repo -Recurse -Filter *.csproj |
        Where-Object { $_.FullName -notmatch '\\Tests\\' } |
        Where-Object {
            $c = Get-Content $_.FullName -Raw
            ($c -match '<GeneratePackageOnBuild>\s*true' -or $c -match '<IsPackable>\s*true') -and
            ($c -notmatch '<IsPackable>\s*false')
        } | ForEach-Object { [pscustomobject]@{ Id = $_.BaseName; Path = $_.FullName } }

    $core    = $packable | Where-Object { $_.Id -eq 'Nextended.Core' }
    $codegen = $packable | Where-Object { $_.Id -eq 'Nextended.CodeGen' }
    if (-not $core)    { throw "Nextended.Core not found among packable projects." }
    if (-not $codegen) { throw "Nextended.CodeGen not found among packable projects." }
    $middle  = $packable | Where-Object { $_.Id -notin @('Nextended.Core', 'Nextended.CodeGen') } | Sort-Object Id

    return @($core) + @($middle) + @($codegen)
}

$packages = Resolve-Packages

# ── Preflight (the "-WhatIf"): report nuget.org state, build nothing ──────────
if ($Preflight) {
    Step "Preflight — checking nuget.org for version $Version"
    foreach ($p in $packages) {
        $exists = Test-VersionPublished $p.Id $Version
        $tag = if ($exists) { 'ALREADY PUBLISHED (would be skipped)' } else { 'new' }
        $color = if ($exists) { 'Yellow' } else { 'Green' }
        Write-Host ("    {0,-40} {1}" -f $p.Id, $tag) -ForegroundColor $color
    }
    Write-Host "`nPreflight only — nothing built or pushed." -ForegroundColor Yellow
    return
}

# CodeGen's build target (UpdateGuid) and Output.props both use $(SolutionDir); pass it explicitly so
# building single projects (not the .sln) behaves identically.
# GeneratePackageOnBuild=false: the projects set it to true, which makes `build` re-enter the Pack
# target and can race the assembly copy on a clean tree (NU5026 "file to be packed was not found").
# `dotnet pack` produces the .nupkg itself, so disabling the on-build pack is correct here.
$common = @("-c", $Configuration, "-p:SolutionDir=$repo\", "-p:GeneratePackageOnBuild=false", "--nologo")

# ── 1. Version ───────────────────────────────────────────────────────────────
Step "Setting version $Version in Version.props (PackageVersion + UsedCorePackageVersion)"
$vpPath = Join-Path $repo 'Version.props'
$vp = Get-Content $vpPath -Raw
$vp = [regex]::Replace($vp, '<PackageVersion>.*?</PackageVersion>',                 "<PackageVersion>$Version</PackageVersion>")
$vp = [regex]::Replace($vp, '<UsedCorePackageVersion>.*?</UsedCorePackageVersion>', "<UsedCorePackageVersion>$Version</UsedCorePackageVersion>")
Set-Content -Path $vpPath -Value $vp -NoNewline
Info "Version.props updated."

# ── 2. Clean staging ─────────────────────────────────────────────────────────
Step "Preparing staging folders"
New-Item -ItemType Directory -Force -Path $artifacts | Out-Null
New-Item -ItemType Directory -Force -Path $feed | Out-Null
Get-ChildItem $artifacts -Filter *.nupkg  -ErrorAction SilentlyContinue | Remove-Item -Force
Get-ChildItem $artifacts -Filter *.snupkg -ErrorAction SilentlyContinue | Remove-Item -Force
Info "artifacts\ cleaned."

# ── 3. Pack the non-CodeGen libraries (Core first) ───────────────────────────
$libs    = $packages | Where-Object { $_.Id -ne 'Nextended.CodeGen' }
$codegen = $packages | Where-Object { $_.Id -eq 'Nextended.CodeGen' }
Step "Packing $($libs.Count) libraries (Core first) -> artifacts\"
foreach ($p in $libs) {
    Info $p.Id
    Invoke-Dotnet (@("pack", $p.Path, "-o", $artifacts) + $common)
}

# ── 4. Stage Core into the local feed ────────────────────────────────────────
Step "Staging Nextended.Core $Version into local feed for the CodeGen build"
$coreNupkg = Join-Path $artifacts "Nextended.Core.$Version.nupkg"
if (-not (Test-Path $coreNupkg)) { throw "Expected package not found: $coreNupkg" }
Copy-Item $coreNupkg $feed -Force
Info "localfeed\ now serves Nextended.Core $Version."

if ($Fresh) {
    Step "Purging global-cache entries for this version (-Fresh)"
    foreach ($id in @('nextended.core', 'nextended.codegen')) {
        $cached = Join-Path $env:USERPROFILE ".nuget\packages\$id\$Version"
        if (Test-Path $cached) { Remove-Item $cached -Recurse -Force; Info "removed $cached" }
    }
}

# ── 5. Pack CodeGen against the local feed (no nuget.org wait) ────────────────
Step "Packing Nextended.CodeGen against the local feed"
Invoke-Dotnet (@("pack", $codegen.Path, "-o", $artifacts,
                 "-p:RestoreAdditionalProjectSources=$feed") + $common)

# ── 6. Preflight report + push order ─────────────────────────────────────────
$staged = Get-ChildItem $artifacts -Filter *.nupkg
# Push order mirrors the discovery order: Core first, CodeGen last.
$ordered = foreach ($p in $packages) {
    $staged | Where-Object { $_.Name -eq "$($p.Id).$Version.nupkg" }
}

Step "Packages staged in artifacts\ (nuget.org state)"
foreach ($nupkg in $ordered) {
    $id = $nupkg.Name -replace "\.$([regex]::Escape($Version))\.nupkg$", ''
    if (Test-VersionPublished $id $Version) { Warn "$($nupkg.Name)  — already on nuget.org (will be skipped)" }
    else                                    { Info "$($nupkg.Name)" }
}

if (-not $Push) {
    Write-Host "`nDry run complete — nothing pushed. Re-run with -Push to publish." -ForegroundColor Yellow
    return
}

if (-not $ApiKey) { throw "No API key. Pass -ApiKey or set `$env:NUGET_API_KEY." }

# ── 7. Push (Core first, CodeGen last) ───────────────────────────────────────
Step "Pushing to nuget.org (Core first, CodeGen last)"
foreach ($nupkg in $ordered) {
    Info "push $($nupkg.Name)"
    Invoke-Dotnet @("nuget", "push", $nupkg.FullName,
                    "--source", $source, "--api-key", $ApiKey, "--skip-duplicate")
}
Write-Host "`nPublished $($ordered.Count) package(s) at $Version." -ForegroundColor Green

# ── 8. Tag the release ───────────────────────────────────────────────────────
if (-not $SkipTag) {
    $tag = "v$Version"
    Step "Tagging release $tag"
    if ((& git tag --list $tag)) {
        Warn "Tag $tag already exists — skipping."
    }
    else {
        try {
            & git tag -a $tag -m "Release $Version"; if ($LASTEXITCODE -ne 0) { throw "git tag failed" }
            & git push origin $tag;                 if ($LASTEXITCODE -ne 0) { throw "git push failed" }
            Info "Created and pushed $tag."
        }
        catch {
            # Packages are already published; a tag hiccup must not fail the run.
            Warn "Could not create/push tag ${tag}: $_"
        }
    }
}
