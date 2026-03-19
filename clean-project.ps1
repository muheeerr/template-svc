# PowerShell script to clean the .NET template project
# Removes bin/obj folders, user files, and build artifacts for a fresh state

$root = "$(Split-Path $MyInvocation.MyCommand.Path -Parent)"

Write-Host "Cleaning project at $root..."

# Remove bin and obj folders recursively
Get-ChildItem -Path $root -Recurse -Directory | Where-Object { $_.Name -in @('bin','obj') } | ForEach-Object {
    Write-Host "Removing $($_.FullName)"
    Remove-Item $_.FullName -Recurse -Force -ErrorAction SilentlyContinue
}

# Remove .user files
Get-ChildItem -Path $root -Recurse -Include *.user | ForEach-Object {
    Write-Host "Removing $($_.FullName)"
    Remove-Item $_.FullName -Force -ErrorAction SilentlyContinue
}

# Remove build artifacts (DLLs, EXEs, pdbs)
Get-ChildItem -Path $root -Recurse -Include *.dll,*.exe,*.pdb | ForEach-Object {
    Write-Host "Removing $($_.FullName)"
    Remove-Item $_.FullName -Force -ErrorAction SilentlyContinue
}

# Remove migrations if present
$migrations = Get-ChildItem -Path $root -Recurse -Directory | Where-Object { $_.Name -eq 'Migrations' }
foreach ($dir in $migrations) {
    Write-Host "Removing migrations folder: $($dir.FullName)"
    Remove-Item $dir.FullName -Recurse -Force -ErrorAction SilentlyContinue
}

Write-Host "Project clean complete."
