# ===================================================================
# LAN Network Configuration Guide
# ===================================================================

Write-Host "?? TCP Socket Server - LAN Configuration Guide" -ForegroundColor Cyan
Write-Host "================================================" -ForegroundColor Cyan
Write-Host ""

# 1. L?y IP Address c?a server
$ipAddresses = Get-NetIPAddress -AddressFamily IPv4 | Where-Object { $_.IPAddress -notlike "127.*" -and $_.IPAddress -notlike "169.*" }

Write-Host "?? Step 1: Find Server IP Address" -ForegroundColor Yellow
Write-Host "-----------------------------------"
Write-Host "Your machine's IP addresses:" -ForegroundColor Green
foreach ($ip in $ipAddresses) {
    Write-Host "   • $($ip.IPAddress) ($($ip.InterfaceAlias))" -ForegroundColor White
}
Write-Host ""

# 2. Ki?m tra port 9000
Write-Host "?? Step 2: Check if Port 9000 is available" -ForegroundColor Yellow
Write-Host "-------------------------------------------"
$port9000 = Get-NetTCPConnection -LocalPort 9000 -ErrorAction SilentlyContinue
if ($port9000) {
    Write-Host "   ? Port 9000 is IN USE (Server might be running)" -ForegroundColor Green
    Write-Host "   Process: $($port9000.OwningProcess)" -ForegroundColor Gray
} else {
    Write-Host "   ?? Port 9000 is FREE (Server is not running yet)" -ForegroundColor Yellow
}
Write-Host ""

# 3. Ki?m tra Firewall
Write-Host "?? Step 3: Check Firewall Rules" -ForegroundColor Yellow
Write-Host "--------------------------------"
$firewallRules = Get-NetFirewallRule -DisplayName "*TCP Socket Server*" -ErrorAction SilentlyContinue
if ($firewallRules) {
Write-Host "   ? Firewall rules found:" -ForegroundColor Green
    foreach ($rule in $firewallRules) {
      Write-Host "      • $($rule.DisplayName) - Enabled: $($rule.Enabled)" -ForegroundColor Gray
    }
} else {
    Write-Host "   ? No firewall rules found!" -ForegroundColor Red
    Write-Host "   ?? Run: .\ConfigureFirewall.ps1 (as Administrator)" -ForegroundColor Yellow
}
Write-Host ""

# 4. H??ng d?n client k?t n?i
Write-Host "?? Step 4: Configure Client Applications" -ForegroundColor Yellow
Write-Host "-----------------------------------------"
Write-Host "Update your client applications to connect to:" -ForegroundColor Green
Write-Host ""
foreach ($ip in $ipAddresses) {
    if ($ip.InterfaceAlias -like "*Wi-Fi*" -or $ip.InterfaceAlias -like "*Ethernet*") {
  Write-Host "   Server IP: $($ip.IPAddress)" -ForegroundColor Cyan
  Write-Host "   Port:      9000" -ForegroundColor Cyan
        Write-Host "   Example:   tcpClient.ConnectAsync(`"$($ip.IPAddress)`", 9000)" -ForegroundColor Gray
        Write-Host ""
    }
}

# 5. Test connection t? remote machine
Write-Host "?? Step 5: Test Connection from Remote Machine" -ForegroundColor Yellow
Write-Host "-----------------------------------------------"
Write-Host "On the CLIENT machine, run:" -ForegroundColor Green
Write-Host "   Test-NetConnection -ComputerName <SERVER_IP> -Port 9000" -ForegroundColor Gray
Write-Host ""
Write-Host "Or use telnet:" -ForegroundColor Green
Write-Host "   telnet <SERVER_IP> 9000" -ForegroundColor Gray
Write-Host ""

Write-Host "================================================" -ForegroundColor Cyan
Write-Host "? Configuration check complete!" -ForegroundColor Green
