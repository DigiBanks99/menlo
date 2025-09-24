# PowerShell script to discover your Windows 10 Home server's network information
# This helps you find the HOME_SERVER_HOST value for GitHub Actions deployment

Write-Host "🔍 Menlo Home Server Network Discovery" -ForegroundColor Green
Write-Host "=======================================" -ForegroundColor Green
Write-Host ""

# Get all network adapters with IP addresses
Write-Host "📡 Active Network Interfaces:" -ForegroundColor Cyan
$networkAdapters = Get-NetIPAddress -AddressFamily IPv4 | Where-Object {
    $_.IPAddress -ne "127.0.0.1" -and
    $_.IPAddress -notlike "169.254.*" -and
    $_.InterfaceAlias -notlike "*Loopback*"
} | Sort-Object IPAddress

foreach ($adapter in $networkAdapters) {
    $interface = Get-NetAdapter | Where-Object { $_.InterfaceIndex -eq $adapter.InterfaceIndex }
    Write-Host "  Interface: $($interface.Name)" -ForegroundColor White
    Write-Host "  IP Address: $($adapter.IPAddress)" -ForegroundColor Yellow
    Write-Host "  Status: $($interface.Status)" -ForegroundColor Green
    Write-Host ""
}

# Try to identify the main network interface
$mainAdapter = $networkAdapters | Where-Object {
    $_.IPAddress -like "192.168.*" -or
    $_.IPAddress -like "10.*" -or
    $_.IPAddress -like "172.*"
} | Select-Object -First 1

if ($mainAdapter) {
    Write-Host "🎯 Recommended HOME_SERVER_HOST:" -ForegroundColor Green
    Write-Host "  $($mainAdapter.IPAddress)" -ForegroundColor Yellow -BackgroundColor Black
    Write-Host ""
    Write-Host "📝 Add this to your GitHub repository secrets:" -ForegroundColor Cyan
    Write-Host "  Secret Name: HOME_SERVER_HOST" -ForegroundColor White
    Write-Host "  Secret Value: $($mainAdapter.IPAddress)" -ForegroundColor White
} else {
    Write-Host "⚠️ Could not automatically detect main network interface" -ForegroundColor Red
    Write-Host "Please manually select from the interfaces listed above" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "🌐 Additional Network Information:" -ForegroundColor Cyan

# Get default gateway
$gateway = Get-NetRoute -DestinationPrefix "0.0.0.0/0" | Select-Object -First 1
if ($gateway) {
    Write-Host "  Default Gateway: $($gateway.NextHop)" -ForegroundColor White
}

# Get DNS servers
$dnsServers = Get-DnsClientServerAddress -AddressFamily IPv4 | Where-Object { $_.ServerAddresses.Count -gt 0 } | Select-Object -First 1
if ($dnsServers) {
    Write-Host "  DNS Servers: $($dnsServers.ServerAddresses -join ', ')" -ForegroundColor White
}

# Test internet connectivity
Write-Host ""
Write-Host "🔌 Testing Internet Connectivity:" -ForegroundColor Cyan
try {
    $pingResult = Test-NetConnection -ComputerName "8.8.8.8" -Port 53 -InformationLevel Quiet
    if ($pingResult) {
        Write-Host "  ✅ Internet connection: Working" -ForegroundColor Green
    } else {
        Write-Host "  ❌ Internet connection: Failed" -ForegroundColor Red
    }
} catch {
    Write-Host "  ❌ Internet connection: Error testing" -ForegroundColor Red
}

# WSL2 detection
Write-Host ""
Write-Host "🐧 WSL2 Information:" -ForegroundColor Cyan
try {
    $wslStatus = wsl --status 2>$null
    if ($LASTEXITCODE -eq 0) {
        Write-Host "  ✅ WSL2 is available" -ForegroundColor Green

        # Get WSL2 IP if available
        try {
            $wslIP = wsl ip addr show eth0 2>$null | Select-String "inet " | ForEach-Object { ($_ -split " ")[5] -split "/" | Select-Object -First 1 }
            if ($wslIP) {
                Write-Host "  WSL2 IP Address: $wslIP" -ForegroundColor White
            }
        } catch {
            Write-Host "  WSL2 IP: Could not determine" -ForegroundColor Yellow
        }
    } else {
        Write-Host "  ❌ WSL2 is not available" -ForegroundColor Red
    }
} catch {
    Write-Host "  ❌ WSL2 is not installed" -ForegroundColor Red
}

Write-Host ""
Write-Host "📋 Next Steps:" -ForegroundColor Green
Write-Host "1. Use the recommended IP address as HOME_SERVER_HOST in GitHub secrets" -ForegroundColor White
Write-Host "2. Ensure your Windows firewall allows SSH connections (port 22)" -ForegroundColor White
Write-Host "3. Make sure Docker Desktop is running in WSL2 mode" -ForegroundColor White
Write-Host "4. Test SSH access from another machine: ssh username@$($mainAdapter.IPAddress)" -ForegroundColor White

Write-Host ""
Write-Host "⚠️ Note: If your IP address changes (DHCP), update the GitHub secret" -ForegroundColor Yellow