# ?? H??ng d?n ch?y h? th?ng Login Request v?i Admin Approval

## ?? C?u trúc h? th?ng

```
???????????????         ????????????????????     ???????????????
?  Client WPF ? ??TCP????  TCP Server      ????TCP?? ?  Admin WPF  ?
?    (UI)     ?      ?  (Port 9000)     ?         ?  (AdminUI)  ?
???????????????      ????????????????????     ???????????????
       ?
        ?
        ????????????????????
          ?  PostgreSQL DB   ?
     ?  (AuthServices)  ?
            ????????????????????
```

## ??? C?u hình Database

### 1. C?p nh?t Connection String
M? file `TcpSocketServer/appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=Prj;Username=postgres;Password=yourpassword;Trust Server Certificate=true"
  }
}
```

### 2. Ch?y Migration (n?u c?n)
```bash
cd MyProject.Infrastructure
dotnet ef database update
```

## ?? Cách ch?y h? th?ng

### B??c 1: Start TCP Server
```bash
cd TcpSocketServer
dotnet run
```

**Output:**
```
?? Starting TCP Socket Server with Database Integration...
? TCP Socket Server started on port 9000
?? Listening for connections...
```

### B??c 2: Start Admin WPF
```bash
cd AdminUI
dotnet run
```

**Admin Interface:**
1. Nh?p Admin Name (default: "Admin01")
2. Click "Connect to Server"
3. Admin s? th?y danh sách pending requests (n?u có)

### B??c 3: Start Client WPF
```bash
cd UI
dotnet run
```

**Client Interface:**
1. App t? ??ng connect ??n server
2. Nh?p Username (default: "TestUser")
3. Xem User ID (auto-generated)
4. Click "Send Login Request"

## ?? Flow ho?t ??ng

### 1. Client g?i Login Request
```
Client WPF ? TCP Server
  ?
Server.HandleLoginRequestAsync()
  ?
AuthServices.AddLoginRequest() ? Save to DB (Status = 0 Pending)
  ?
Broadcast "NewLoginRequest" ? All connected Admins
  ?
Send "LoginRequestAck" ? Client
```

**Client Activity Log:**
```
[10:30:15] ?? Connecting to server...
[10:30:15] ? Connected to TCP Server successfully!
[10:30:20] ?? Login request sent for user: TestUser
[10:30:20] ?? Login request received. Waiting for admin approval...
[10:30:20] ?? Request ID: abc123...
[10:30:20] ? Waiting for admin approval...
```

### 2. Admin nh?n request (Real-time)
**Admin DataGrid t? ??ng update:**
- ? Request m?i ???c **insert lên ??u list**
- ?? Sound notification
- ?? Pending count t?ng lên

**Admin Activity Log:**
```
[10:30:20] ?? New login request from: TestUser
```

### 3. Admin Approve/Reject

**Admin clicks "? Approve":**
```
Admin WPF ? TCP Server (Method: "AcceptLogin", Status: 1)
  ?
Server.HandleAcceptLoginAsync()
  ?
AuthServices.AcceptLoginRequest() ? Update DB (Status = 1 Approved)
  ?
AuthServices.AddLoginHistory() ? Save login history
  ?
Send "LoginResult" ? Client (if online)
  ?
Send "AcceptLoginAck" ? Admin
```

**Admin Activity Log:**
```
[10:30:25] ?? Login request approved: abc123...
[10:30:25] ? Login request accepted
```

**Admin DataGrid:**
- ? Request ???c **t? ??ng remove kh?i list**
- ?? Pending count gi?m xu?ng

**Client nh?n k?t qu?:**
```
[10:30:25] 
[10:30:25] ?? ???????????????????????????
[10:30:25] ??   LOGIN APPROVED!   ??
[10:30:25] ?? ???????????????????????????
[10:30:25] ? Login approved by admin
[10:30:25] ?? Approved by: Admin01
[10:30:25] ?? Time: 2024-01-15 10:30:25
[10:30:25] ???????????????????????????
```

### 4. Admin Reject
**Admin clicks "? Reject":**
- Same flow nh?ng Status = 2
- Client nh?n "? LOGIN REJECTED"

## ?? Giao di?n Features

### Client WPF
- ? Modern gradient background (purple theme)
- ? Connection status indicator (?? Red / ?? Green)
- ? Username input v?i default value
- ? User ID display (read-only GUID)
- ? Send Login Request button
- ? Real-time Activity Log v?i timestamps
- ? Responsive feedback messages

### Admin WPF
- ? Professional dashboard layout (dark blue theme)
- ? Admin ID và Status display trong header
- ? Admin Name input
- ? Total Pending counter (real-time)
- ? **DataGrid v?i ??y ?? thông tin:**
  - Request ID (Consolas font)
  - User Name (Bold)
  - User ID
  - IP Address
  - Device Info
  - Requested At (formatted timestamp)
  - **Approve ?** button (Green)
  - **Reject ?** button (Red)
- ? Hover effects trên buttons và rows
- ? Alternating row colors
- ? Activity Log v?i Consolas font
- ? Sound notification cho requests m?i

## ?? Database Tables

### LoginRequest
```sql
Id (Guid, PK)
UserId (Guid, FK ? Users)
Status (int) -- 0=Pending, 1=Approved, 2=Rejected
IpAddress (string)
DeviceInfo (string)
RequestedAt (DateTime)
ReviewedAt (DateTime, nullable)
ReviewedByAdminId (Guid, nullable, FK ? Users)
```

### LoginHistory
```sql
Id (Guid, PK)
UserId (Guid, FK ? Users)
LoginTime (DateTime)
IpAddress (string)
DeviceInfo (string)
IsSuccessful (bool)
```

## ?? Testing Scenarios

### Scenario 1: Single Client, Single Admin
1. Start Server
2. Start Admin ? Connect
3. Start Client ? Send request
4. Admin sees request in DataGrid
5. Admin clicks Approve
6. Client sees "LOGIN APPROVED"
7. Request removed from Admin DataGrid

### Scenario 2: Multiple Clients
1. Start Server
2. Start Admin
3. Start Client 1 ? Request
4. Start Client 2 ? Request
5. Admin sees 2 requests in DataGrid
6. Pending count shows "2"
7. Admin approves both sequentially
8. Both clients receive approval
9. DataGrid becomes empty

### Scenario 3: Multiple Admins
1. Start Server
2. Start Admin 1 ? Connect
3. Start Admin 2 ? Connect
4. Start Client ? Request
5. **Both admins** see the request
6. Admin 1 clicks Approve first
7. Request processed, removed from **both admins' DataGrids**

### Scenario 4: Client Offline
1. Client sends request
2. Client disconnects
3. Admin approves
4. Request processed in DB
5. Server logs "User offline"
6. When client reconnects ? Already approved (no new notification)

## ?? Troubleshooting

### Server won't start
```
? Error: Address already in use
```
**Solution:** Port 9000 ?ang ???c s? d?ng
- Check: `netstat -ano | findstr :9000`
- Kill process ho?c ??i port trong appsettings.json

### Database connection failed
```
? Connection failed: Host not found
```
**Solution:** 
- Check PostgreSQL is running
- Verify connection string trong appsettings.json
- Test: `psql -U postgres -h localhost`

### Admin DataGrid không update
**Check:**
- TCP connection status (should be Green)
- Activity Log shows "?? New login request"
- ObservableCollection ?ang ???c dùng

### Client không nh?n response
**Check:**
- Client connection status
- Username tracking (Server uses username as key)
- Server logs shows "?? Result sent to user"

## ?? Keyboard Shortcuts

### Client
- **Enter** trên Username TextBox ? Send Login Request

### Admin
- **Double-click** trên DataGrid row ? Show details
- **Delete** key ? Quick reject (can implement)

## ?? Best Practices

1. **Always start Server first**
2. **Connect Admin before sending requests** (?? test real-time)
3. **Check Activity Logs** cho debugging
4. **Monitor Database** ?? verify data persistence
5. **Test offline scenarios** (disconnect clients/admins)

## ?? Performance Tips

- Server x? lý **async/await** cho all I/O operations
- **ObservableCollection** t? ??ng update UI thread-safe
- **ConcurrentDictionary** cho multiple connections
- Database operations trong **scoped service** (proper DI)

## ?? Security Notes

- Passwords ???c **encrypt** trong database
- TCP messages dùng **JSON serialization**
- Admin approval required cho **t?t c? logins**
- IP Address và Device Info ???c **log** cho audit

---

## ?? Demo Flow

```
Terminal 1: dotnet run (TcpSocketServer)
Terminal 2: dotnet run (AdminUI)
Terminal 3: dotnet run (UI)

Watch magic happen! ??
```
