# FIX INSTRUCTIONS

## UI/MainWindow.xaml

Line 19-21: Remove duplicate Effect property, ch? gi? l?i Border.Effect block

C?n thêm x:Name vào các controls sau:
1. `UsernameTextBox` - TextBox cho username input
2. `UserIdTextBox` - TextBox hi?n th? User ID
3. `StatusIndicator` - Ellipse hi?n th? connection status
4. `StatusText` - TextBlock hi?n th? "Connected"/"Disconnected"
5. `LoginButton` - Button ?? g?i login request
6. `LogTextBlock` - TextBlock trong ScrollViewer ?? hi?n th? log

## AdminUI/MainWindow.xaml

C?n thêm x:Name vào các controls sau:
1. `AdminIdTextBlock` - TextBlock hi?n th? Admin ID (trong header)
2. `AdminStatusIndicator` - Ellipse connection status
3. `AdminStatusText` - TextBlock "Connected"/"Disconnected"
4. `AdminNameTextBox` - TextBox nh?p admin name
5. `TotalPendingTextBlock` - TextBlock hi?n th? s? pending requests
6. `ConnectButton` - Button connect to server
7. `LoginRequestsDataGrid` - DataGrid hi?n th? login requests
8. `ActivityLogTextBlock` - TextBlock trong ScrollViewer log

Vì response quá dài, b?n c?n manually add `x:Name="..."` vào các control ?ó trong XAML files.
