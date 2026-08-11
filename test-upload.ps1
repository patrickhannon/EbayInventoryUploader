# Test script to upload sample item
Write-Host "Testing Marketplace Inventory Manager..." -ForegroundColor Cyan

# Navigate to project directory
Set-Location "D:\One drive phannon\OneDrive\_Docs\___Projects\EbayInventoryUploader"

# Run the application and select option 4 (Test sample item) then y to confirm
$input = "4`ny`n7`n"
$input | dotnet run
