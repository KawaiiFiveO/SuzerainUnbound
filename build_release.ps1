# ==========================================
# Suzerain Unbound - Automated Release Build
# ==========================================

$version = "1.1.1"
$modName = "SuzerainUnbound"

# Paths
$dllPath = "bin\Release\net6.0\SuzerainUnbound.dll"
$assetsDir = "ReleaseAssets"
$outputDir = "Releases"
$stagingDir = "$outputDir\Staging"

# Configuration File Name
$configFile = "com.onehalf.suzerainunbound.cfg"

Write-Host "Starting build process for v$version..." -ForegroundColor Cyan

# 1. Compile a fresh Release DLL
Write-Host "Compiling project..."
dotnet build -c Release
if ($LASTEXITCODE -ne 0) {
    Write-Host "Build failed! Aborting." -ForegroundColor Red
    Pause
    exit
}

# 2. Prepare Directories
if (Test-Path $stagingDir) { Remove-Item -Recurse -Force $stagingDir }
if (!(Test-Path $outputDir)) { New-Item -ItemType Directory -Path $outputDir | Out-Null }
New-Item -ItemType Directory -Path $stagingDir | Out-Null

# ==========================================
# BUILD 1: PLUGIN ONLY
# ==========================================
Write-Host "Packaging Plugin-Only Build..."
$pluginDir = "$stagingDir\PluginOnly"
New-Item -ItemType Directory -Path $pluginDir | Out-Null

# Copy DLL and Config
Copy-Item $dllPath -Destination $pluginDir
Copy-Item "$assetsDir\$configFile" -Destination $pluginDir

# Read the README, replace the {{VERSION}} tag, and write it to the staging folder
(Get-Content "$assetsDir\README_PluginOnly.txt") -replace "\{\{VERSION\}\}", $version | Set-Content "$pluginDir\README.txt" -Encoding UTF8

# Zip it
$pluginZip = "$outputDir\${modName}_v${version}_PluginOnly.zip"
if (Test-Path $pluginZip) { Remove-Item $pluginZip -Force }
Compress-Archive -Path "$pluginDir\*" -DestinationPath $pluginZip -Force


# ==========================================
# BUILD 2: WITH MODLOADER
# ==========================================
Write-Host "Packaging With Modloader Build..."
$bundleDir = "$stagingDir\WithModloader"

# Find the newest BepInEx zip in the ReleaseAssets folder
$bepinexZips = Get-ChildItem -Path $assetsDir -Filter "BepInEx-Unity.IL2CPP*.zip" | Sort-Object LastWriteTime -Descending
if ($bepinexZips.Count -eq 0) {
    Write-Host "ERROR: Could not find a BepInEx zip file in the $assetsDir folder!" -ForegroundColor Red
    Pause
    exit
}
$bepinexZip = $bepinexZips[0]

Write-Host "Extracting $($bepinexZip.Name)... (This might take a few seconds)"
Expand-Archive -Path $bepinexZip.FullName -DestinationPath $bundleDir -Force

# Delete changelog.txt from the bundle
$changelogPath = "$bundleDir\changelog.txt"
if (Test-Path $changelogPath) { Remove-Item $changelogPath -Force }

# Ensure BepInEx folders exist (Extracting a fresh zip doesn't create empty folders)
$pluginsPath = "$bundleDir\BepInEx\plugins"
$configPath = "$bundleDir\BepInEx\config"
if (!(Test-Path $pluginsPath)) { New-Item -ItemType Directory -Path $pluginsPath | Out-Null }
if (!(Test-Path $configPath)) { New-Item -ItemType Directory -Path $configPath | Out-Null }

# Copy Mod Files
Copy-Item $dllPath -Destination $pluginsPath
Copy-Item "$assetsDir\$configFile" -Destination $configPath

# Read the README, replace the {{VERSION}} tag, and write it
(Get-Content "$assetsDir\README_WithModloader.txt") -replace "\{\{VERSION\}\}", $version | Set-Content "$bundleDir\README.txt" -Encoding UTF8

# Zip it
$bundleZip = "$outputDir\${modName}_v${version}_WithModloader.zip"
if (Test-Path $bundleZip) { Remove-Item $bundleZip -Force }
Compress-Archive -Path "$bundleDir\*" -DestinationPath $bundleZip -Force


# ==========================================
# CLEANUP
# ==========================================
Remove-Item -Recurse -Force $stagingDir
Write-Host "Build Complete! Files saved to the '$outputDir' folder." -ForegroundColor Green
Pause