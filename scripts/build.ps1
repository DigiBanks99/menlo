# Navigate to the Web UI directory
Push-Location -Path $PSScriptRoot\..\src\ui\web

# Build lib
Write-Host "🏗️ Building menlo-lib..."
npm run build:lib
if ($LASTEXITCODE -ne 0) {
    Pop-Location
    exit $LASTEXITCODE
}

# Build app
Write-Host "🏗️ Building menlo-app..."
npm run build:app
if ($LASTEXITCODE -ne 0) {
    Pop-Location
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
Write-Host "🧹 Clearing wwwroot..."
Remove-Item -Path wwwroot\* -Recurse -Force

# Copy the built files to the wwwroot directory
Write-Host "📦 Copying files to wwwroot..."
Copy-Item -Path $PSScriptRoot\..\src\ui\web\dist\menlo-app\browser\* -Destination wwwroot -Recurse -Force

# Navigate back to the original directory
Pop-Location

# Navigate to the src directory
Push-Location -Path $PSScriptRoot\..\src

# Build the Application
Write-Host "🏗️ Building application..."
dotnet build --configuration release --no-restore
if ($LASTEXITCODE -ne 0) {
    Pop-Location
    exit $LASTEXITCODE
}

# Publish the linux-x64 binaries
Write-Host "📦 Publishing the linux x64 binaries..."
Remove-Item -Path artifacts\publish\Menlo.Api\release_linux-x64\* -Recurse -Force
Remove-Item -Path artifacts\api\* -Recurse -Force
dotnet publish api\Menlo.Api\ --configuration Release -r linux-x64
Copy-Item -Path artifacts\publish\Menlo.Api\release_linux-x64\* -Destination artifacts\api -Recurse -Force
if ($LASTEXITCODE -ne 0) {
    Pop-Location
    exit $LASTEXITCODE
}

# Build docker image
Write-Host "🐋 Buidling Docker image"
podman build -f Dockerfile -t menlo:test . --build-arg VERSION=8.0

# Navigate back to the original directory
Pop-Location
