# Template Setup Script
# Run this script to convert the template into your new project

param(
    [Parameter(Mandatory=$true)]
    [string]$ProjectName
)

Write-Host "Setting up project: $ProjectName" -ForegroundColor Cyan

# Rename solution file
if (Test-Path "__ProjectName__.slnx") {
    Rename-Item -Path "__ProjectName__.slnx" -NewName "$ProjectName.slnx"
    Write-Host "Renamed solution file" -ForegroundColor Green
}

# Rename Host folder
if (Test-Path "__ProjectName__.Host") {
    Rename-Item -Path "__ProjectName__.Host" -NewName "$ProjectName.Host"
    Write-Host "Renamed Host folder" -ForegroundColor Green
}

# Rename Host project file
if (Test-Path "$ProjectName.Host\__ProjectName__.Host.csproj") {
    Rename-Item -Path "$ProjectName.Host\__ProjectName__.Host.csproj" -NewName "$ProjectName.Host.csproj"
    Write-Host "Renamed Host project file" -ForegroundColor Green
}

# Rename Tests folder
if (Test-Path "__ProjectName__.Tests") {
    Rename-Item -Path "__ProjectName__.Tests" -NewName "$ProjectName.Tests"
    Write-Host "Renamed Tests folder" -ForegroundColor Green
}

# Rename Tests project file
if (Test-Path "$ProjectName.Tests\__ProjectName__.Tests.csproj") {
    Rename-Item -Path "$ProjectName.Tests\__ProjectName__.Tests.csproj" -NewName "$ProjectName.Tests.csproj"
    Write-Host "Renamed Tests project file" -ForegroundColor Green
}

# Replace placeholders in all files
Write-Host "Replacing placeholders..." -ForegroundColor Yellow
$files = Get-ChildItem -Recurse -File -Include *.cs,*.csproj,*.json,*.slnx,*.md | Where-Object { $_.FullName -notlike "*\bin\*" -and $_.FullName -notlike "*\obj\*" }
foreach ($file in $files) {
    $content = Get-Content $file.FullName -Raw -ErrorAction SilentlyContinue
    if ($content -and $content -match '__ProjectName__') {
        $content -replace '__ProjectName__', $ProjectName | Set-Content $file.FullName
        Write-Host "  Updated: $($file.Name)" -ForegroundColor Gray
    }
}

# Replace lowercase placeholder (used in URLs)
foreach ($file in $files) {
    $content = Get-Content $file.FullName -Raw -ErrorAction SilentlyContinue
    if ($content -and $content -match '__projectname__') {
        $content -replace '__projectname__', $ProjectName.ToLower() | Set-Content $file.FullName
    }
}

# Generate new UserSecretsId
$NewGuid = [guid]::NewGuid().ToString()
$csprojPath = "$ProjectName.Host\$ProjectName.Host.csproj"
if (Test-Path $csprojPath) {
    $content = Get-Content $csprojPath -Raw
    $content -replace '__USER_SECRETS_ID__', $NewGuid | Set-Content $csprojPath
    Write-Host "Generated new UserSecretsId: $NewGuid" -ForegroundColor Green
}

# Clean up bin/obj folders
Write-Host "Cleaning build artifacts..." -ForegroundColor Yellow
Get-ChildItem -Directory -Recurse | Where-Object { $_.Name -eq "bin" -or $_.Name -eq "obj" } | Remove-Item -Recurse -Force -ErrorAction SilentlyContinue

Write-Host ""
Write-Host "Template setup complete!" -ForegroundColor Green
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Cyan
Write-Host "1. Open $ProjectName.slnx in Visual Studio or Rider"
Write-Host "2. Update environment variables in Properties\launchSettings.json"
Write-Host "3. Build: dotnet build .\$ProjectName.slnx"
Write-Host "4. Create initial migration: dotnet ef migrations add Init --project DA --startup-project $ProjectName.Host"
Write-Host "5. Run: dotnet run --project $ProjectName.Host"
