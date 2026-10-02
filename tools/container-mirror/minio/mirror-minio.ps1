<#
.SYNOPSIS
  Rebuilds the MinIO server and client images that Nextended's AddMinioS3OnNfs pulls,
  verifies them end to end, and optionally pushes them to ghcr.io/fgilde.

.DESCRIPTION
  MinIO stopped publishing images: minio/minio and minio/mc are gone from Docker Hub and
  quay.io, and dl.min.io answers 410. What is left are the GitHub releases, which still carry
  the official binaries with checksums. This script turns those back into images.

  1. Base: the last official images, retagged to ghcr.io/fgilde/... (byte-identical). On a
     machine that no longer has them locally they are pulled from ghcr.io instead.
  2. minio.Dockerfile / mc.Dockerfile swap in the exact release binary (ADD --checksum).
  3. The result is started exactly the way Nextended starts it — "server /data
     --console-address :9001" and the bucket-init script — and an object is written and read.
  4. -Push uploads all four tags.

  The pushed packages are private until their visibility is switched to Public once in the
  GitHub package settings; Azure Container Apps pulls them anonymously after that.

.EXAMPLE
  ./mirror-minio.ps1            # build + verify only
  ./mirror-minio.ps1 -Push      # build + verify + push (needs a token with write:packages)
#>
#Requires -Version 7.0
[CmdletBinding()]
param([switch]$Push)

$ErrorActionPreference = "Stop"
$PSNativeCommandUseErrorActionPreference = $true
Set-Location $PSScriptRoot

$owner = "ghcr.io/fgilde"
$server = @{ Base = "RELEASE.2025-04-22T22-12-26Z"; Exact = "RELEASE.2025-09-07T16-13-09Z"; Official = "minio/minio" }
$client = @{ Base = "RELEASE.2025-04-16T18-13-26Z"; Exact = "RELEASE.2025-08-13T08-35-41Z"; Official = "minio/mc" }

function Resolve-Base([string]$repository, [hashtable]$spec) {
    $target = "$owner/${repository}:$($spec.Base)"
    $official = "$($spec.Official):$($spec.Base)"
    # A missing image is the expected case on a fresh machine, not a failure - but with
    # PSNativeCommandUseErrorActionPreference the non-zero exit would end the script here.
    $hasLocal = $true
    try { docker image inspect $official *> $null } catch { $hasLocal = $false }
    if ($hasLocal) {
        docker tag $official $target
        Write-Host "base $target <- local $official" -ForegroundColor Gray
    } else {
        docker pull -q $target | Out-Null
        Write-Host "base $target <- ghcr.io" -ForegroundColor Gray
    }
}

Write-Host "=== bases ===" -ForegroundColor Cyan
Resolve-Base "minio" $server
Resolve-Base "minio-mc" $client

Write-Host "=== build ===" -ForegroundColor Cyan
docker build -q -f minio.Dockerfile -t "$owner/minio:$($server.Exact)" . | Out-Null
docker build -q -f mc.Dockerfile -t "$owner/minio-mc:$($client.Exact)" . | Out-Null

Write-Host "=== verify ===" -ForegroundColor Cyan
$serverVersion = docker run --rm "$owner/minio:$($server.Exact)" --version | Select-Object -First 1
$clientVersion = docker run --rm "$owner/minio-mc:$($client.Exact)" --version | Select-Object -First 1
if ($serverVersion -notmatch [regex]::Escape($server.Exact)) { throw "server reports '$serverVersion'" }
if ($clientVersion -notmatch [regex]::Escape($client.Exact)) { throw "client reports '$clientVersion'" }
Write-Host $serverVersion
Write-Host $clientVersion

# Same arguments and the same init script AddMinioS3OnNfs uses, so a passing run here means
# the images behave in Azure the way the ones they replace did.
$net = "minio-mirror-verify"
$user = "mirror-admin"
$pass = "Mirror-Verify-" + [guid]::NewGuid().ToString("N").Substring(0, 12)
$init = 'mc --insecure alias set m "$MINIO_ENDPOINT" "$MINIO_USER" "$MINIO_PASS" >/dev/null 2>&1; ' +
        'until mc --insecure mb --ignore-existing m/"$BUCKET"; do echo "waiting for minio..."; sleep 2; done; ' +
        'echo "bucket ready"; tail -f /dev/null'
try {
    docker network create $net | Out-Null
    docker run -d --rm --name "$net-server" --network $net -e MINIO_ROOT_USER=$user -e MINIO_ROOT_PASSWORD=$pass `
        "$owner/minio:$($server.Exact)" server /data --console-address :9001 | Out-Null
    Start-Sleep -Seconds 4
    docker run -d --rm --name "$net-init" --network $net -e "MINIO_ENDPOINT=http://$net-server:9000" `
        -e MINIO_USER=$user -e MINIO_PASS=$pass -e BUCKET=supabase-storage --entrypoint /bin/sh `
        "$owner/minio-mc:$($client.Exact)" -c $init | Out-Null

    $ready = $false
    for ($i = 0; $i -lt 30 -and -not $ready; $i++) {
        Start-Sleep -Seconds 1
        $ready = (docker logs "$net-init" 2>&1 | Out-String) -match 'bucket ready'
    }
    if (-not $ready) { throw "bucket init never reported 'bucket ready'" }

    $roundtrip = docker run --rm --network $net --entrypoint /bin/sh "$owner/minio-mc:$($client.Exact)" -c `
        "mc alias set m http://$net-server:9000 $user $pass >/dev/null && echo mirror-ok > /tmp/t && mc cp -q /tmp/t m/supabase-storage/t >/dev/null && mc cat m/supabase-storage/t"
    if ($roundtrip -ne "mirror-ok") { throw "object roundtrip returned '$roundtrip'" }
    Write-Host "bucket init + object roundtrip: ok" -ForegroundColor Green
} finally {
    $PSNativeCommandUseErrorActionPreference = $false
    docker rm -f "$net-init" "$net-server" *> $null
    docker network rm $net *> $null
}

if (-not $Push) {
    Write-Host "Verified. Re-run with -Push to upload." -ForegroundColor Yellow
    return
}

Write-Host "=== push ===" -ForegroundColor Cyan
foreach ($tag in "$owner/minio:$($server.Base)", "$owner/minio-mc:$($client.Base)",
                 "$owner/minio:$($server.Exact)", "$owner/minio-mc:$($client.Exact)") {
    docker push -q $tag | Out-Null
    Write-Host "pushed $tag" -ForegroundColor Green
}
Write-Host ""
Write-Host "Switch both packages to Public once (Package settings -> Change visibility):" -ForegroundColor Yellow
Write-Host "  https://github.com/users/fgilde/packages/container/minio/settings"
Write-Host "  https://github.com/users/fgilde/packages/container/minio-mc/settings"
