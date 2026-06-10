[CmdletBinding()]
param(
    [string]$Version = "1.0.0",
    [string]$Runtime = "win-x64",
    [string]$Configuration = "Release",
    [switch]$FrameworkDependent,
    [switch]$NoZip
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Normalize-Version {
    param([string]$InputVersion)

    $normalized = $InputVersion.Trim()
    if ($normalized.StartsWith("v", [System.StringComparison]::OrdinalIgnoreCase)) {
        $normalized = $normalized.Substring(1)
    }

    if ([string]::IsNullOrWhiteSpace($normalized)) {
        return "1.0.0"
    }

    return $normalized
}

function Get-NumericFileVersion {
    param([string]$InputVersion)

    if ($InputVersion -notmatch '^\d+(\.\d+){0,3}$') {
        return $null
    }

    $parts = [System.Collections.Generic.List[string]]::new()
    $parts.AddRange($InputVersion.Split("."))
    while ($parts.Count -lt 4) {
        $parts.Add("0")
    }

    return [string]::Join(".", $parts)
}

function Invoke-CommandStep {
    param(
        [string]$Title,
        [scriptblock]$Command
    )

    Write-Host ""
    Write-Host "==> $Title"
    & $Command
}

$root = $PSScriptRoot
if ([string]::IsNullOrWhiteSpace($root)) {
    $root = Split-Path -Parent $MyInvocation.MyCommand.Path
}

$versionValue = Normalize-Version $Version
$fileVersion = Get-NumericFileVersion $versionValue
$selfContained = -not $FrameworkDependent.IsPresent
$packageKind = if ($selfContained) { "self-contained" } else { "framework-dependent" }
$releaseRoot = Join-Path $root "release"
$appDir = Join-Path $releaseRoot "RecPlay"
$zipName = if ($selfContained) {
    "RecPlay-v$versionValue-$Runtime.zip"
} else {
    "RecPlay-v$versionValue-$Runtime-framework-dependent.zip"
}
$zipPath = Join-Path $releaseRoot $zipName
$projectPath = Join-Path $root "RecPlay.csproj"
$exePath = Join-Path $appDir "RecPlay.exe"
$iconFiles = @("Idle.png", "Recording.png", "Replaying.png")

Push-Location $root
try {
    if (-not (Test-Path $projectPath)) {
        throw "Project file not found: $projectPath"
    }

    foreach ($iconFile in $iconFiles) {
        $iconPath = Join-Path $root $iconFile
        if (-not (Test-Path $iconPath)) {
            throw "Required tray icon not found: $iconPath"
        }
    }

    Invoke-CommandStep "Build $Configuration" {
        & dotnet build $projectPath -c $Configuration
    }

    Invoke-CommandStep "Clean release output" {
        if (Test-Path $appDir) {
            Remove-Item -LiteralPath $appDir -Recurse -Force
        }

        if (-not (Test-Path $releaseRoot)) {
            New-Item -ItemType Directory -Path $releaseRoot | Out-Null
        }
    }

    $publishArgs = @(
        "publish",
        $projectPath,
        "-c", $Configuration,
        "-r", $Runtime,
        "--self-contained", $selfContained.ToString().ToLowerInvariant(),
        "-p:Version=$versionValue",
        "-p:InformationalVersion=$versionValue",
        "-o", $appDir
    )

    if ($selfContained) {
        $publishArgs += "-p:PublishSingleFile=true"
        $publishArgs += "-p:IncludeNativeLibrariesForSelfExtract=true"
    }

    if ($fileVersion) {
        $publishArgs += "-p:FileVersion=$fileVersion"
        $publishArgs += "-p:AssemblyVersion=$fileVersion"
    }

    Invoke-CommandStep "Publish $packageKind package" {
        & dotnet @publishArgs
    }

    foreach ($iconFile in $iconFiles) {
        Copy-Item -Path (Join-Path $root $iconFile) -Destination $appDir -Force
    }

    if (-not (Test-Path $exePath)) {
        throw "Published executable not found: $exePath"
    }

    if (-not $NoZip) {
        Invoke-CommandStep "Create zip artifact" {
            if (Test-Path $zipPath) {
                Remove-Item -LiteralPath $zipPath -Force
            }

            Compress-Archive -Path $appDir -DestinationPath $zipPath -Force
        }
    }

    Write-Host ""
    Write-Host "Package ready"
    Write-Host "  Type: $packageKind"
    Write-Host "  App: $appDir"
    Write-Host "  Exe: $exePath"
    if (-not $NoZip) {
        Write-Host "  Zip: $zipPath"
    }
}
finally {
    Pop-Location
}
