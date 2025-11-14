# TCP Socket Server - LAN Configuration Guide

## ?? H??ng d?n c?u hình ?? các máy trong LAN có th? k?t n?i

### B??c 1: Chu?n b? Server (Máy ch?y TcpSocketServer)

#### 1.1. Tìm IP Address c?a Server
M? PowerShell và ch?y:
```powershell
cd D:\ProjectNek\EFCore\TcpSocketServer
.\CheckLANConfiguration.ps1
```

Ho?c dùng l?nh:
```powershell
ipconfig | findstr IPv4
```

Ví d? k?t qu?:
```
IPv4 Address. . . . . . . . . . . : 192.168.1.100
```

**Ghi nh? IP này!** (ví d?: `192.168.1.100`)

#### 1.2. C?u hình Windows Firewall

**Ch?y PowerShell as Administrator** và execute:
```powershell
cd D:\ProjectNek\EFCore\TcpSocketServer
.\ConfigureFirewall.ps1
```

Ho?c c?u hình th? công:
1. M? **Windows Defender Firewall**
2. Click **Advanced settings**
3. Click **Inbound Rules** ? **New Rule**
4. Ch?n **Port** ? Next
5. Ch?n **TCP** ? Specific local ports: `9000` ? Next
6. Ch?n **Allow the connection** ? Next
7. Ch?n t?t c? profiles (Domain, Private, Public) ? Next
8. Name: `TCP Socket Server - Port 9000` ? Finish

#### 1.3. Kh?i ??ng Server
```powershell
cd D:\ProjectNek\EFCore\TcpSocketServer
dotnet run
```

B?n s? th?y:
```
? TCP Socket Server started on port 9000
?? Listening for connections...
```

---

### B??c 2: C?u hình Client (Máy khác trong LAN)

#### 2.1. C?p nh?t `appsettings.json`

**Cho AdminUI:**
File: `D:\ProjectNek\EFCore\AdminUI\appsettings.json`
```json
{
  "TcpServer": {
    "Host": "192.168.1.100",  // ? Thay b?ng IP c?a Server
    "Port": 9000
  }
}
```

**Cho ClientUI:**
File: `D:\ProjectNek\EFCore\UI\appsettings.json`
```json
{
  "TcpServer": {
    "Host": "192.168.1.100",  // ? Thay b?ng IP c?a Server
    "Port": 9000
  }
}
```

#### 2.2. Test k?t n?i t? Client

Trên máy Client, m? PowerShell và test:
```powershell
Test-NetConnection -ComputerName 192.168.1.100 -Port 9000
```

**K?t qu? mong ??i:**
```
TcpTestSucceeded : True
```

N?u `False`, ki?m tra:
- ? Server ?ang ch?y?
- ? Firewall ?ã ???c c?u hình?
- ? IP address ?úng ch?a?
- ? Cùng m?ng LAN không?

#### 2.3. Ch?y Client Application

```powershell
# Admin UI
cd D:\ProjectNek\EFCore\AdminUI
dotnet run

# Client UI
cd D:\ProjectNek\EFCore\UI
dotnet run
```

---

### B??c 3: Troubleshooting

#### L?i: "Connection timeout"
**Nguyên nhân:**
- Server ch?a ch?y
- Firewall ch?n port 9000
- IP address sai
- Không cùng m?ng LAN

**Gi?i pháp:**
1. Ki?m tra Server ?ang ch?y:
   ```powershell
   netstat -an | findstr :9000
   ```

2. T?t t?m Firewall ?? test:
```powershell
   # T?t (Administrator)
   Set-NetFirewallProfile -Profile Domain,Public,Private -Enabled False

   # Test k?t n?i...

   # B?t l?i
 Set-NetFirewallProfile -Profile Domain,Public,Private -Enabled True
   ```

3. Ping server:
   ```powershell
   ping 192.168.1.100
   ```

#### L?i: "Connection refused"
**Nguyên nhân:**
- Port 9000 b? process khác s? d?ng
- Server ch?a bind ?úng interface

**Gi?i pháp:**
1. Ki?m tra process ?ang dùng port 9000:
   ```powershell
   netstat -ano | findstr :9000
   ```

2. Kill process n?u c?n:
   ```powershell
   taskkill /PID <process_id> /F
   ```

---

### B??c 4: Network Architecture

```
???????????????????????????????????????
?         Router/Switch               ?
?      (192.168.1.1)        ?
???????????????????????????????????????
  ?   ?
   ?      ?
    ???????????????  ???????????????
    ?   SERVER    ??   CLIENT 1  ?
    ? 192.168.1.100?  ?192.168.1.101?
    ?          ?  ?             ?
    ? TCP Server  ?  ?  AdminUI    ?
    ?  Port 9000  ????  Connect    ?
    ???????????????  ???????????????
           ?
    ?
    ???????????????
    ?  CLIENT 2   ?
    ?192.168.1.102?
    ?    ?
    ?  ClientUI   ?
    ?  Connect    ?
  ???????????????
```

---

### B??c 5: Security Considerations

?? **L?u ý an toàn:**

1. **Firewall:** Ch? m? port 9000 cho Private network, KHÔNG m? cho Public
2. **Authentication:** Server ?ã có xác th?c qua username/password
3. **Encryption:** Cân nh?c dùng TLS/SSL cho production
4. **IP Whitelist:** Có th? thêm IP filtering trong code n?u c?n

---

### Các l?nh h?u ích

```powershell
# Xem t?t c? k?t n?i ?ang active
netstat -an | findstr :9000

# Xem firewall rules
Get-NetFirewallRule -DisplayName "*TCP Socket*"

# Xem IP c?a t?t c? network adapters
Get-NetIPAddress -AddressFamily IPv4

# Test port t? xa
Test-NetConnection -ComputerName <SERVER_IP> -Port 9000

# Xem logs real-time t? TcpSocketServer
# (Console output c?a dotnet run)
```

---

## ? Checklist

Server Setup:
- [ ] Tìm ???c IP address c?a server
- [ ] C?u hình Windows Firewall (port 9000)
- [ ] TcpSocketServer ?ang ch?y
- [ ] Test port b?ng `netstat -an | findstr :9000`

Client Setup:
- [ ] C?p nh?t `appsettings.json` v?i server IP
- [ ] Test k?t n?i b?ng `Test-NetConnection`
- [ ] AdminUI ho?c ClientUI k?t n?i thành công

---

## ?? Support

N?u g?p v?n ??:
1. Check server logs (Console output c?a TcpSocketServer)
2. Check client logs (Console output c?a AdminUI/ClientUI)
3. Run `CheckLANConfiguration.ps1` ?? ki?m tra c?u hình
4. Xem Troubleshooting section ? trên

**Happy coding! ??**
