param(
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$assemblyInfo = Join-Path $root "Properties\AssemblyInfo.cs"
if (-not (Test-Path $assemblyInfo)) {
    throw "AssemblyInfo.cs not found at $assemblyInfo"
}

$assemblyContent = Get-Content $assemblyInfo -Raw
$versionPattern = '(?m)^\s*\[assembly:\s*AssemblyVersion\("([^\"]+)"\)\]'
$versionMatch = [regex]::Match($assemblyContent, $versionPattern)
if (-not $versionMatch.Success) {
    throw "Unable to parse AssemblyVersion from AssemblyInfo.cs"
}
$version = $versionMatch.Groups[1].Value

$buildOutput = Join-Path $root "bin\$Configuration\NameBuilderConfigurator.dll"
if (-not (Test-Path $buildOutput)) {
    throw "Build output $buildOutput not found. Run pwsh -File .\\build.ps1 -Configuration $Configuration first."
}

$minimumNugetVersion = [Version]"5.10.0"
$nugetExe = $null
$nuget = Get-Command nuget.exe -ErrorAction SilentlyContinue
if ($nuget) {
    try {
        $nugetVersion = [Version]([System.Diagnostics.FileVersionInfo]::GetVersionInfo($nuget.Source).FileVersion)
        if ($nugetVersion -ge $minimumNugetVersion) {
            $nugetExe = $nuget.Source
        }
    } catch {
        Write-Warning "Unable to read nuget.exe version from $($nuget.Source); falling back to local copy."
    }
}

if (-not $nugetExe) {
    $toolsDir = Join-Path $root ".nuget"
    New-Item -ItemType Directory -Force -Path $toolsDir | Out-Null
    $nugetPath = Join-Path $toolsDir "nuget.exe"

    $downloadRequired = $true
    if (Test-Path $nugetPath) {
        try {
            $localVersion = [Version]([System.Diagnostics.FileVersionInfo]::GetVersionInfo($nugetPath).FileVersion)
            if ($localVersion -ge $minimumNugetVersion) {
                $downloadRequired = $false
            }
        } catch {
            Write-Warning "Unable to read nuget.exe version from $nugetPath; re-downloading."
        }
    }

    if ($downloadRequired) {
        Write-Host "Downloading nuget.exe..." -ForegroundColor Cyan
        Invoke-WebRequest -Uri "https://dist.nuget.org/win-x86-commandline/latest/nuget.exe" -OutFile $nugetPath
    }

    $nugetExe = $nugetPath
}

$outputDir = Join-Path $root "artifacts\nuget"
New-Item -ItemType Directory -Force -Path $outputDir | Out-Null

$nuspecPath = Join-Path $root "NameBuilderConfigurator.nuspec"
if (-not (Test-Path $nuspecPath)) {
    throw "Nuspec not found at $nuspecPath"
}

$arguments = @(
    "pack", $nuspecPath,
    "-Version", $version,
    "-OutputDirectory", $outputDir,
    "-BasePath", $root,
    "-NoPackageAnalysis"
)

Write-Host "Packing NameBuilderConfigurator v$version..." -ForegroundColor Green
& $nugetExe $arguments

if ($LASTEXITCODE -ne 0) {
    throw "nuget.exe pack failed with exit code $LASTEXITCODE"
}

Write-Host "NuGet package created in $outputDir" -ForegroundColor Green
