# Navigate to the Web UI directory
Push-Location -Path $PSScriptRoot\..\src\ui\web

# Build lib
Write-Host "🏗️ Building library..."
npm run build:lib
if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}

# Build app
Write-Host "🏗️ Building application..."
npm run build:app
if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}

# Navigate back to the original directory
Pop-Location

# Navigate to the API directory
Push-Location -Path $PSScriptRoot\..\src\api\Menlo.Api

# Ensure the wwwroot directory exists
if (-not (Test-Path -Path wwwroot)) {
    New-Item -ItemType Directory -Path wwwroot
}

# Clear the wwwroot directory
Write-Host "🧹 Clearing wwwroot...
Remove-Item -Path wwwroot\* -Recurse -Force"

# Copy the built files to the wwwroot directory
Write-Host "📦 Copying files to wwwroot..."
Copy-Item -Path $PSScriptRoot\..\src\ui\web\dist\menlo-app\browser\* -Destination wwwroot -Recurse -Force

# Navigate back to the original directory
Pop-Location

# Navigate to the src directory
Push-Location -Path $PSScriptRoot\..\src

# Build the Application
Write-Host "🏗️ Building application..."
dotnet build
