# TCP Socket Server v?i Database Integration

## T?ng quan
TCP Socket Server này ?ã ???c tích h?p v?i database thông qua Entity Framework Core và `IAuthServices` ?? l?u tr? các login request và history vào PostgreSQL database.

## Các thay ??i chính

### 1. **TcpSocketServer/Program.cs**
- Thêm Dependency Injection (DI) v?i `Microsoft.Extensions.Hosting`
- Tích h?p `ApplicationDbContext` v?i PostgreSQL
- Inject `IAuthServices` ?? g?i các methods l?u database
- Khi client g?i login request ? L?u vào b?ng `LoginRequest` (status = 0 - Pending)
- Khi admin accept/reject ? C?p nh?t status trong database
- Khi admin accept ? T?o record trong b?ng `LoginHistory`

### 2. **MyProject.Application/Services/AuthServices.cs**
- Make `IHttpContextAccessor` optional trong constructor
- TCP Server không c?n HTTP context, ch? Web API m?i c?n

### 3. **TcpSocketServer/appsettings.json**
- C?u hình connection string ??n PostgreSQL
- C?u hình Encryption keys
- C?u hình JWT settings
- C?u hình TCP Server port

## C?u hình

### Database Connection
M? `TcpSocketServer/appsettings.json` và c?p nh?t connection string:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=Prj;Username=postgres;Password=yourpassword;Trust Server Certificate=true"
  }
}
```

### Encryption & JWT
C?p nh?t các keys trong `appsettings.json`:

```json
{
  "Encryption": {
"Key": "Your32ByteEncryptionKeyHere!!!!!",
    "IV": "Your16BytesIVHer"
  },
  "Jwt": {
    "Secret": "YourJwtSecretKeyAtLeast32CharactersLong!!!",
    "Issuer": "YourIssuer",
    "Audience": "YourAudience"
  }
}
```

## Cách ch?y

### 1. Ch?y TCP Socket Server
```bash
cd TcpSocketServer
dotnet run
```

### 2. Ch?y Client Test Console
```bash
cd Client
dotnet run
```

### 3. Ch?y Admin Console
```bash
cd Admin
dotnet run
```

## Lu?ng ho?t ??ng

### Client Login Request
1. Client g?i message v?i method `"LoginRequest"`:
```json
{
  "Method": "LoginRequest",
  "Data": {
    "UserId": "guid-here",
    "UserName": "testuser",
    "IpAddress": "127.0.0.1",
    "DeviceInfo": "Windows 10"
  }
}
```

2. **TCP Server x? lý:**
   - G?i `AuthServices.AddLoginRequest()` ? L?u vào database
   - L?u vào memory dictionary ?? tracking
   - G?i ACK cho client
   - Broadcast ??n t?t c? admin ?ang online

3. **Database:**
   - Insert vào b?ng `LoginRequest` v?i:
     - `Id`: Auto-generated
     - `UserId`: From client request
  - `Status`: 0 (Pending)
     - `IpAddress`, `DeviceInfo`: From client
     - `RequestedAt`: Current UTC time

### Admin Accept/Reject
1. Admin g?i message v?i method `"AcceptLogin"`:
```json
{
  "Method": "AcceptLogin",
  "Data": {
    "LoginRequestId": "guid-here",
    "Status": 1  // 1 = approve, 2 = reject
  }
}
```

2. **TCP Server x? lý:**
   - G?i `AuthServices.AcceptLoginRequest(adminId, request)` ? Update database
   - N?u Status = 1 (approved):
     - G?i `AuthServices.AddLoginHistory()` ? Insert vào database
   - Remove t? pending dictionary
   - G?i k?t qu? cho client (n?u còn online)
   - G?i ACK cho admin

3. **Database:**
   - **LoginRequest table:** Update
     - `Status`: 1 ho?c 2
     - `ReviewedByAdminId`: Admin's Guid
     - `ReviewedAt`: Current UTC time
 
   - **LoginHistory table:** Insert (n?u approved)
     - `Id`: Auto-generated
     - `UserId`: From login request
     - `LoginTime`: Current UTC time
     - `IpAddress`, `DeviceInfo`: From original request
     - `IsSuccessful`: true

## Database Schema

### LoginRequest Table
```sql
- Id (Guid, PK)
- UserId (Guid, FK ? Users)
- Status (int): 0 = Pending, 1 = Approved, 2 = Rejected
- IpAddress (string, nullable)
- DeviceInfo (string, nullable)
- RequestedAt (DateTime)
- ReviewedAt (DateTime, nullable)
- ReviewedByAdminId (Guid, nullable, FK ? Users)
- LoginHistoryId (Guid, nullable, FK ? LoginHistory)
```

### LoginHistory Table
```sql
- Id (Guid, PK)
- UserId (Guid, FK ? Users)
- LoginTime (DateTime)
- IpAddress (string, nullable)
- DeviceInfo (string, nullable)
- IsSuccessful (bool)
```

## Dependencies

### TcpSocketServer.csproj
```xml
<ItemGroup>
  <PackageReference Include="Microsoft.Extensions.DependencyInjection" Version="9.0.10" />
  <PackageReference Include="Microsoft.Extensions.Hosting" Version="9.0.10" />
  <PackageReference Include="Microsoft.Extensions.Configuration.Json" Version="9.0.10" />
</ItemGroup>

<ItemGroup>
  <ProjectReference Include="..\MyProject.Application\MyProject.Application.csproj" />
  <ProjectReference Include="..\MyProject.Infrastructure\MyProject.Infrastructure.csproj" />
  <ProjectReference Include="..\MyProject.Helper\MyProject.Helper.csproj" />
</ItemGroup>
```

## L?u ý quan tr?ng

1. **Connection String:** ??m b?o PostgreSQL server ?ang ch?y và connection string ?úng
2. **Migration:** Ch?y migrations ?? t?o database schema tr??c khi start server
3. **Optional HttpContextAccessor:** `AuthServices` không yêu c?u `IHttpContextAccessor` khi ???c g?i t? TCP Server
4. **Thread Safety:** S? d?ng `ConcurrentDictionary` ?? ??m b?o thread-safe khi nhi?u clients k?t n?i ??ng th?i
5. **Dependency Injection Scope:** M?i l?n g?i database c?n t?o scope m?i ?? ??m b?o DbContext ???c s? d?ng ?úng

## Troubleshooting

### L?i connection string
- Ki?m tra PostgreSQL ?ang ch?y
- Verify username/password trong connection string
- Ensure database "Prj" t?n t?i

### L?i DI
- ??m b?o t?t c? dependencies ???c register trong `ConfigureServices`
- Check project references trong .csproj file

### L?i Entity Framework
- Run migrations: `dotnet ef database update`
- Verify DbContext configuration trong `ApplicationDbContext.cs`

## Testing

1. Start TCP Server
2. Start Client ? Send login request
3. Verify in database: `SELECT * FROM "LoginRequest" WHERE "Status" = 0`
4. Start Admin ? Accept the request
5. Verify in database:
   - `SELECT * FROM "LoginRequest" WHERE "Status" = 1`
   - `SELECT * FROM "LoginHistory" WHERE "IsSuccessful" = true`

## Tác gi?
Updated by GitHub Copilot - Database Integration Feature
