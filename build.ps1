# Build script for  NameBuilderConfigurator
param(
    [string]$Configuration = "Release"
)

function Update-VersionNumber {
    param([string]$version)

    if ([string]::IsNullOrWhiteSpace($version)) {
        throw "Version value cannot be empty."
    }

    $segments = $version.Split('.')
    while ($segments.Count -lt 4) {
        $segments += "0"
    }

    $lastIndex = $segments.Count - 1
    try {
        $segments[$lastIndex] = ([int]$segments[$lastIndex] + 1).ToString()
    } catch {
        throw "Unable to parse version segment '$($segments[$lastIndex])'."
    }

    return ($segments -join '.')
}

Write-Host "Building NameBuilderConfigurator..." -ForegroundColor Green

$assemblyInfoPath = Join-Path $PSScriptRoot "Properties\AssemblyInfo.cs"
if (Test-Path $assemblyInfoPath) {
    Write-Host "`nIncrementing AssemblyInfo version..." -ForegroundColor Green
    $assemblyInfoContent = Get-Content -Path $assemblyInfoPath -Raw

    $assemblyVersionPattern = '(?m)^\s*\[assembly:\s*AssemblyVersion\("([^"]+)"\)\]'
    $assemblyVersionMatch = [regex]::Match($assemblyInfoContent, $assemblyVersionPattern)
    if (-not $assemblyVersionMatch.Success) {
        throw "AssemblyVersion attribute not found in AssemblyInfo.cs"
    }

    $currentVersion = $assemblyVersionMatch.Groups[1].Value
    $newVersion = Update-VersionNumber -version $currentVersion

    $assemblyInfoContent = [regex]::Replace(
        $assemblyInfoContent,
        $assemblyVersionPattern,
        "[assembly: AssemblyVersion(`"$newVersion`")]",
        1
    )

    $assemblyFileVersionPattern = '(?m)^\s*\[assembly:\s*AssemblyFileVersion\("([^"]+)"\)\]'
    if ([regex]::IsMatch($assemblyInfoContent, $assemblyFileVersionPattern)) {
        $assemblyInfoContent = [regex]::Replace(
            $assemblyInfoContent,
            $assemblyFileVersionPattern,
            "[assembly: AssemblyFileVersion(`"$newVersion`")]",
            1
        )
    } else {
        Write-Host "AssemblyFileVersion attribute not found. Skipping." -ForegroundColor Yellow
    }

    Set-Content -Path $assemblyInfoPath -Value $assemblyInfoContent -Encoding UTF8
    Write-Host "Assembly version updated: $currentVersion -> $newVersion" -ForegroundColor Cyan
} else {
    Write-Host "AssemblyInfo.cs not found at $assemblyInfoPath" -ForegroundColor Yellow
}

# Find MSBuild
$preferredPaths = @(
    "C:\Program Files\Microsoft Visual Studio\18\Insiders\MSBuild\Current\Bin\MSBuild.exe"
)

$msbuild = $preferredPaths | Where-Object { Test-Path $_ } | Select-Object -First 1

if (-not $msbuild) {
    $vswhere = "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe"

    if (Test-Path $vswhere) {
        $vsPath = & $vswhere -latest -products * -requires Microsoft.Component.MSBuild -property installationPath
        if ($vsPath) {
            $msbuild = Join-Path $vsPath "MSBuild\Current\Bin\MSBuild.exe"
            if (-not (Test-Path $msbuild)) {
                $msbuild = Join-Path $vsPath "MSBuild\15.0\Bin\MSBuild.exe"
            }
        }
    }
}

if (-not $msbuild -or -not (Test-Path $msbuild)) {
    Write-Host "MSBuild not found in preferred or vswhere locations, trying known fallbacks..." -ForegroundColor Yellow
    $knownPaths = @(
        "C:\Program Files\Microsoft Visual Studio\18\Insiders\MSBuild\Current\Bin\MSBuild.exe",
        "C:\Program Files\Microsoft Visual Studio\18\Enterprise\MSBuild\Current\Bin\MSBuild.exe",
        "C:\Program Files\Microsoft Visual Studio\18\Professional\MSBuild\Current\Bin\MSBuild.exe",
        "C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\MSBuild.exe",
        "C:\Program Files\Microsoft Visual Studio\2022\Enterprise\MSBuild\Current\Bin\MSBuild.exe",
        "C:\Program Files\Microsoft Visual Studio\2022\Professional\MSBuild\Current\Bin\MSBuild.exe",
        "C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe",
        "C:\Program Files (x86)\Microsoft Visual Studio\2019\Enterprise\MSBuild\Current\Bin\MSBuild.exe",
        "C:\Program Files (x86)\Microsoft Visual Studio\2019\Professional\MSBuild\Current\Bin\MSBuild.exe",
        "C:\Program Files (x86)\Microsoft Visual Studio\2019\Community\MSBuild\Current\Bin\MSBuild.exe"
    )
    
    foreach ($path in $knownPaths) {
        if (Test-Path $path) {
            $msbuild = $path
            break
        }
    }
}

if (-not $msbuild -or -not (Test-Path $msbuild)) {
    Write-Host "ERROR: MSBuild not found. Please install Visual Studio 2019 or 2022 with .NET desktop development workload." -ForegroundColor Red
    Write-Host "Download from: https://visualstudio.microsoft.com/downloads/" -ForegroundColor Yellow
    exit 1
}

Write-Host "Using MSBuild: $msbuild" -ForegroundColor Cyan

# Restore NuGet packages
Write-Host "`nRestoring NuGet packages..." -ForegroundColor Green
& $msbuild  NameBuilderConfigurator.csproj /t:Restore /p:Configuration=$Configuration /v:m

if ($LASTEXITCODE -ne 0) {
    Write-Host "ERROR: Package restore failed" -ForegroundColor Red
    exit $LASTEXITCODE
}

# Build the project
Write-Host "`nBuilding project..." -ForegroundColor Green
& $msbuild  NameBuilderConfigurator.csproj /t:Rebuild /p:Configuration=$Configuration /v:m

if ($LASTEXITCODE -eq 0) {
    Write-Host "`nBuild succeeded!" -ForegroundColor Green
    Write-Host "Output: bin\$Configuration\NameBuilderConfigurator.dll" -ForegroundColor Cyan
    
    $dll = "bin\$Configuration\NameBuilderConfigurator.dll"
    if (Test-Path $dll) {
        $buildAssets = Join-Path (Join-Path "bin" $Configuration) "Assets"
        $sourceAssets = Join-Path $PSScriptRoot "Assets"
        $xrmRoot = "$env:APPDATA\MscrmTools\XrmToolBox"
        $pluginsRoot = Join-Path $xrmRoot "Plugins"
        $pluginFolder = Join-Path $pluginsRoot "NameBuilderConfigurator"
        if (Test-Path $xrmRoot) {
            Write-Host "`nDeploying to XrmToolBox..." -ForegroundColor Green
            New-Item -Path $pluginFolder -ItemType Directory -Force | Out-Null
            New-Item -Path $pluginsRoot -ItemType Directory -Force | Out-Null

            Copy-Item $dll (Join-Path $pluginsRoot "NameBuilderConfigurator.dll") -Force

            $configPath = "bin\$Configuration\NameBuilderConfigurator.dll.config"
            if (Test-Path $configPath) {
                Copy-Item $configPath (Join-Path $pluginFolder "NameBuilderConfigurator.dll.config") -Force
            }

            $assetDestination = Join-Path $pluginFolder "Assets"
            if (Test-Path $assetDestination) {
                Remove-Item $assetDestination -Recurse -Force
            }

            if (Test-Path $buildAssets) {
                Copy-Item $buildAssets $assetDestination -Recurse -Force
            } elseif (Test-Path $sourceAssets) {
                Copy-Item $sourceAssets $assetDestination -Recurse -Force
            }

            Write-Host "Plugin deployed to: $pluginFolder" -ForegroundColor Cyan

        } else {
            Write-Host "`nXrmToolBox not found. To deploy manually, run:" -ForegroundColor Yellow
            Write-Host "  Copy-Item '$dll' '$pluginsRoot'" -ForegroundColor White
        }
    }
} else {
    Write-Host "`nBuild failed with exit code: $LASTEXITCODE" -ForegroundColor Red
    exit $LASTEXITCODE
}
