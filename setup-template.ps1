# Template Setup Script
# Run this script to convert the template into your new project
# Usage: ./setup-template.ps1 -ProjectName MyApp

param(
    [Parameter(Mandatory=$true)]
    [string]$ProjectName
)

$Placeholder = "projectname"

Write-Host "Setting up project: $ProjectName" -ForegroundColor Cyan

# --- Rename files and folders ---

if (Test-Path "$Placeholder.slnx") {
    Rename-Item -Path "$Placeholder.slnx" -NewName "$ProjectName.slnx"
    Write-Host "Renamed solution file" -ForegroundColor Green
}

if (Test-Path "$Placeholder.Host") {
    Rename-Item -Path "$Placeholder.Host" -NewName "$ProjectName.Host"
    Write-Host "Renamed Host folder" -ForegroundColor Green
}

if (Test-Path "$ProjectName.Host\$Placeholder.Host.csproj") {
    Rename-Item -Path "$ProjectName.Host\$Placeholder.Host.csproj" -NewName "$ProjectName.Host.csproj"
    Write-Host "Renamed Host project file" -ForegroundColor Green
}

if (Test-Path "$Placeholder.Tests") {
    Rename-Item -Path "$Placeholder.Tests" -NewName "$ProjectName.Tests"
    Write-Host "Renamed Tests folder" -ForegroundColor Green
}

if (Test-Path "$ProjectName.Tests\$Placeholder.Tests.csproj") {
    Rename-Item -Path "$ProjectName.Tests\$Placeholder.Tests.csproj" -NewName "$ProjectName.Tests.csproj"
    Write-Host "Renamed Tests project file" -ForegroundColor Green
}

# --- Replace placeholder in file contents ---

Write-Host "Replacing placeholders in file contents..." -ForegroundColor Yellow

$extensions = @("*.cs","*.csproj","*.json","*.slnx","*.md","*.yml","*.yaml","*.props","*.xml","Dockerfile*")
$files = Get-ChildItem -Recurse -File -Include $extensions |
    Where-Object {
        $_.FullName -notlike "*\bin\*" -and
        $_.FullName -notlike "*\obj\*" -and
        $_.Name -ne "setup-template.ps1"
    }

foreach ($file in $files) {
    $content = Get-Content $file.FullName -Raw -ErrorAction SilentlyContinue
    if ($content -and $content -cmatch $Placeholder) {
        $content -creplace $Placeholder, $ProjectName | Set-Content $file.FullName -NoNewline
        Write-Host "  Updated: $($file.Name)" -ForegroundColor Gray
    }
}

# --- Regenerate UserSecretsId ---

$csprojPath = "$ProjectName.Host\$ProjectName.Host.csproj"
if (Test-Path $csprojPath) {
    $NewGuid = [guid]::NewGuid().ToString()
    $content = Get-Content $csprojPath -Raw
    $content -replace '<UserSecretsId>[^<]*</UserSecretsId>', "<UserSecretsId>$NewGuid</UserSecretsId>" |
        Set-Content $csprojPath -NoNewline
    Write-Host "Generated new UserSecretsId: $NewGuid" -ForegroundColor Green
}

# --- Clean build artifacts ---

Write-Host "Cleaning build artifacts..." -ForegroundColor Yellow
Get-ChildItem -Directory -Recurse |
    Where-Object { $_.Name -eq "bin" -or $_.Name -eq "obj" } |
    Remove-Item -Recurse -Force -ErrorAction SilentlyContinue

Write-Host ""
Write-Host "Template setup complete!" -ForegroundColor Green
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Cyan
Write-Host "1. Open $ProjectName.slnx in Visual Studio or Rider"
Write-Host "2. Update environment variables in $ProjectName.Host\Properties\launchSettings.json"
Write-Host "3. Build:     dotnet build .\$ProjectName.slnx"
Write-Host "4. Migration: dotnet ef migrations add Init --project DA --startup-project $ProjectName.Host"
Write-Host "5. Run:       dotnet run --project $ProjectName.Host"
