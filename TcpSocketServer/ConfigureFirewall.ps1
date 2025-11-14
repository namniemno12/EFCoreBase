# ===================================================================
# Script: Configure Windows Firewall for TCP Socket Server (Port 9000)
# ===================================================================

# Tạo firewall rule cho Inbound (cho phép kết nối từ bên ngoài vào)
New-NetFirewallRule -DisplayName "TCP Socket Server - Port 9000 (Inbound)" `
   -Direction Inbound `
        -LocalPort 9000 `
           -Protocol TCP `
          -Action Allow `
    -Profile Any `
        -Description "Allow TCP connections on port 9000 for Socket Server"

# Tạo firewall rule cho Outbound (cho phép server gửi data ra ngoài)
New-NetFirewallRule -DisplayName "TCP Socket Server - Port 9000 (Outbound)" `
     -Direction Outbound `
         -LocalPort 9000 `
    -Protocol TCP `
          -Action Allow `
     -Profile Any `
       -Description "Allow TCP connections on port 9000 for Socket Server"

Write-Host "✅ Firewall rules created successfully!" -ForegroundColor Green
Write-Host "📡 Port 9000 is now open for TCP connections" -ForegroundColor Cyan
