@echo off
cd "D:\One drive phannon\OneDrive\_Docs\___Projects\EbayInventoryUploader"
echo Building project...
dotnet build --verbosity normal
echo.
echo Exit Code: %ERRORLEVEL%
if %ERRORLEVEL% EQU 0 (
    echo BUILD SUCCESSFUL!
) else (
    echo BUILD FAILED!
)
pause
