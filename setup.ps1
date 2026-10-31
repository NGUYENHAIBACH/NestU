# Chạy một lần sau khi clone: kiểm tra môi trường, restore, tạo migration, tạo CSDL, build cả hai project.
# Mở PowerShell tại thư mục NestU rồi:  .\setup.ps1
$ErrorActionPreference = "Stop"

Write-Host "== Kiểm tra môi trường ==" -ForegroundColor Cyan
dotnet --version
if (-not (Get-Command dotnet-ef -ErrorAction SilentlyContinue)) {
    Write-Host "Cài dotnet-ef..." -ForegroundColor Yellow
    dotnet tool install --global dotnet-ef
}
try { sqllocaldb info | Out-Null; Write-Host "SQL Server LocalDB: OK" } catch { Write-Host "Không thấy SQL Server LocalDB. Cài qua Visual Studio Installer (ASP.NET workload) hoặc SQL Server Express LocalDB." -ForegroundColor Red }

Write-Host "`n== NestU.API ==" -ForegroundColor Cyan
Push-Location NestU.API
dotnet restore
dotnet build -c Debug
if (-not (Test-Path "Migrations")) {
    Write-Host "Tạo migration InitialCreate..." -ForegroundColor Yellow
    dotnet ef migrations add InitialCreate
}
dotnet ef database update
Pop-Location

Write-Host "`n== NestU.WebApp ==" -ForegroundColor Cyan
Push-Location NestU.WebApp
dotnet restore
dotnet build -c Debug
Pop-Location

Write-Host "`n== Xong ==" -ForegroundColor Green
Write-Host "Chạy API : cd NestU.API    ; dotnet run   (https://localhost:7001/swagger)"
Write-Host "Chạy Web : cd NestU.WebApp ; dotnet run   (https://localhost:7002)"
Write-Host "Tài khoản demo: admin@nestu.vn / chutro1@nestu.vn / chutro2@nestu.vn / sinhvien1@nestu.vn / khach1@nestu.vn — mật khẩu NestU@123"
