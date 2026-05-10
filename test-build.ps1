Write-Host "=== BUILD VERIFICATION TEST ===" -ForegroundColor Cyan
Write-Host ""

$projectPath = "D:\One drive phannon\OneDrive\_Docs\___Projects\EbayInventoryUploader"
Set-Location $projectPath

# Check if DLL exists
$dllPath = "$projectPath\bin\Debug\net10.0\EbayInventoryUploader.dll"
$exePath = "$projectPath\bin\Debug\net10.0\EbayInventoryUploader.exe"

Write-Host "Checking build artifacts..." -ForegroundColor Yellow

if (Test-Path $dllPath) {
    $dllInfo = Get-Item $dllPath
    Write-Host "[OK] DLL exists: $($dllInfo.FullName)" -ForegroundColor Green
    Write-Host "     Size: $($dllInfo.Length) bytes" -ForegroundColor Gray
    Write-Host "     Modified: $($dllInfo.LastWriteTime)" -ForegroundColor Gray
} else {
    Write-Host "[FAIL] DLL not found" -ForegroundColor Red
}

if (Test-Path $exePath) {
    $exeInfo = Get-Item $exePath
    Write-Host "[OK] EXE exists: $($exeInfo.FullName)" -ForegroundColor Green
    Write-Host "     Size: $($exeInfo.Length) bytes" -ForegroundColor Gray
    Write-Host "     Modified: $($exeInfo.LastWriteTime)" -ForegroundColor Gray
} else {
    Write-Host "[FAIL] EXE not found" -ForegroundColor Red
}

Write-Host ""
Write-Host "Running fresh build..." -ForegroundColor Yellow

# Kill any running instances
Stop-Process -Name "EbayInventoryUploader" -Force -ErrorAction SilentlyContinue
Stop-Process -Name "dotnet" -Force -ErrorAction SilentlyContinue
Start-Sleep -Milliseconds 500

# Build
$buildStart = Get-Date
dotnet build --nologo --verbosity quiet 2>&1 | Out-Null
$buildEnd = Get-Date
$buildTime = ($buildEnd - $buildStart).TotalSeconds

if ($LASTEXITCODE -eq 0) {
    Write-Host "[OK] Build succeeded in $([math]::Round($buildTime, 2)) seconds" -ForegroundColor Green
    
    # Check DLL timestamp
    if (Test-Path $dllPath) {
        $dllInfo = Get-Item $dllPath
        Write-Host "[OK] DLL updated: $($dllInfo.LastWriteTime)" -ForegroundColor Green
    }
    
    Write-Host ""
    Write-Host "=== BUILD STATUS: SUCCESS ===" -ForegroundColor Green -BackgroundColor Black
    Write-Host ""
    Write-Host "You can now run the application with:" -ForegroundColor Cyan
    Write-Host "  dotnet run" -ForegroundColor White
    Write-Host ""
    
} else {
    Write-Host "[FAIL] Build failed with exit code $LASTEXITCODE" -ForegroundColor Red
    Write-Host ""
    Write-Host "Running verbose build to see errors..." -ForegroundColor Yellow
    dotnet build --verbosity normal
}
