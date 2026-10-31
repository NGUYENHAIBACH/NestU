# NestU

Nền tảng quản lý nhà trọ — đồ án cuối môn PRN232.

| Project | Vai trò | Địa chỉ |
| --- | --- | --- |
| `NestU.API` | ASP.NET Core 8 Web API, EF Core, SQL Server, OData, JWT | https://localhost:7001/swagger |
| `NestU.WebApp` | ASP.NET Core 8 MVC, Bootstrap 5, gọi API qua HTTP | https://localhost:7002 |

## Chạy lần đầu

```powershell
.\setup.ps1          # restore, build, tạo migration + CSDL
cd NestU.API    ; dotnet run      # https://localhost:7001/swagger
cd NestU.WebApp ; dotnet run      # https://localhost:7002
```
Chi tiết, tài khoản demo và kịch bản demo: `docs/HUONG-DAN-CHAY.md`.

## Tiếp tục với Claude Code

Mở thư mục này trong Claude Code (tab Code của app Claude, VS Code, hoặc `claude` trong terminal). Claude Code tự đọc `CLAUDE.md`.
Dán lần lượt các lời nhắc A → D trong `docs/PROMPTS.md` để biên dịch, chạy và sửa lỗi.

## Tài liệu

- `CLAUDE.md` — ngữ cảnh và quy tắc code cho Claude Code
- `docs/NestU-design.md` — tài liệu thiết kế bản chốt (phạm vi, CSDL, ràng buộc, luồng, endpoint, kế hoạch 7 ngày)
- `docs/PROMPTS.md` — lời nhắc cho Claude Code (biên dịch, chạy, sửa lỗi)
- `docs/HUONG-DAN-CHAY.md` — cách chạy, tài khoản demo, kịch bản demo, case lỗi cố ý
- `docs/de-bai-PRN232.png` — đề bài
- `docs/tham-khao-mo-hinh-thuong-mai.md` — mô hình thương mại BHMS + BHRP, chỉ dùng cho mục "Hướng phát triển" trong báo cáo, **không code**
