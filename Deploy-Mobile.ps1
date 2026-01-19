# DCMS Mobile Build and Deploy Script
# This script builds the Mobile React app and copies it to the publish folder

param(
    [string]$ProjectRoot = "D:\MO-Gamal\Projects\DCMS-Chat - Mobile",
    [string]$MobileDir = "Mobile",
    [string]$PublishDir = "publish_web_v14",
    [switch]$SkipBuild
)

Write-Host "================================" -ForegroundColor Cyan
Write-Host "DCMS Mobile Build & Deploy" -ForegroundColor Cyan
Write-Host "================================" -ForegroundColor Cyan
Write-Host ""

$mobilePath = Join-Path $ProjectRoot $MobileDir
$publishPath = Join-Path $ProjectRoot $PublishDir
$wwwrootMobilePath = Join-Path (Join-Path $publishPath "wwwroot") "Mobile"

# Step 1: Build the Mobile app
if (-not $SkipBuild) {
    Write-Host "[1/4] Building Mobile React App..." -ForegroundColor Yellow
    Push-Location $mobilePath
    
    try {
        # Install dependencies if needed
        if (-not (Test-Path "node_modules")) {
            Write-Host "  Installing dependencies..." -ForegroundColor Gray
            npm install
        }
        
        # Build the app
        Write-Host "  Running build..." -ForegroundColor Gray
        npm run build
        
        if ($LASTEXITCODE -ne 0) {
            Write-Host "Build failed!" -ForegroundColor Red
            Pop-Location
            exit 1
        }
        
        Write-Host "Build successful!" -ForegroundColor Green
    }
    finally {
        Pop-Location
    }
}
else {
    Write-Host "[1/4] Skipping build (using existing dist folder)" -ForegroundColor Gray
}

# Step 2: Create wwwroot/Mobile directory
Write-Host "[2/4] Creating target directory..." -ForegroundColor Yellow
New-Item -ItemType Directory -Path $wwwrootMobilePath -Force | Out-Null
Write-Host "Directory ready!" -ForegroundColor Green

# Step 3: Copy built files
Write-Host "[3/4] Copying files to publish folder..." -ForegroundColor Yellow
$distPath = Join-Path $mobilePath "dist"
Copy-Item -Path "$distPath\*" -Destination $wwwrootMobilePath -Recurse -Force
Write-Host "Files copied!" -ForegroundColor Green

# Step 4: Create zip file
Write-Host "[4/4] Creating publish archive..." -ForegroundColor Yellow
$zipPath = Join-Path $ProjectRoot "publish_web_v14.zip"

# Remove old zip if exists
if (Test-Path $zipPath) {
    Remove-Item $zipPath -Force
}

# Create new zip
Compress-Archive -Path "$publishPath\*" -DestinationPath $zipPath -Force
Write-Host "Archive created: $zipPath" -ForegroundColor Green

Write-Host ""
Write-Host "================================" -ForegroundColor Cyan
Write-Host "Deployment package ready!" -ForegroundColor Green
Write-Host "================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Yellow
Write-Host "1. Upload the zip file to MonsterASP" -ForegroundColor White
Write-Host "2. Extract the files on the server" -ForegroundColor White
Write-Host "3. Access the Mobile page via browser" -ForegroundColor White
Write-Host ""
