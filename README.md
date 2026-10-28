# CMS

CMS چندمستاجری و API-first روی .NET 10 و SQL Server، مطابق [مشخصات](docs/CMS_Spec_v3.0.md).
این مخزن **هنوز CMS کامل سند نیست**. وضعیت واقعی و کمبودها در [گزارش وضعیت](docs/IMPLEMENTATION_STATUS.md) ثبت شده است.

قابلیت‌های اجرایی: Tenant/Locale، Audit، Outbox، idempotency، ثبت‌نام و ورود، JWT، refresh rotation و logout، نقش و مجوز، نوع محتوا، Entry، revision، restore و گردش انتشار.

## راه‌اندازی

پیش‌نیاز: SDK تعیین‌شده در global.json و SQL Server یا Windows LocalDB. API خودکار migration اجرا نمی‌کند.

```powershell
dotnet restore CMS.slnx --locked-mode
dotnet build CMS.slnx -c Release --no-restore
$env:ConnectionStrings__Cms = 'Server=(localdb)\MSSQLLocalDB;Database=CMS_Development;Integrated Security=true;TrustServerCertificate=true'
$env:Jwt__SigningKey = [Convert]::ToBase64String([System.Security.Cryptography.RandomNumberGenerator]::GetBytes(32))
dotnet run --project src/CMS.Migrator -- migrate
$tenantId = [Guid]::NewGuid().ToString()
dotnet run --project src/CMS.Migrator -- provision $tenantId demo 'Demo tenant' setup-tenant
dotnet run --project src/CMS.Migrator -- create-locale $tenantId fa-IR 'فارسی' setup-locale
$adminPassword = Read-Host 'Admin password' -AsSecureString
$env:CMS_ADMIN_PASSWORD = [System.Net.NetworkCredential]::new('', $adminPassword).Password
dotnet run --project src/CMS.Migrator -- create-admin $tenantId admin@example.test
Remove-Item Env:CMS_ADMIN_PASSWORD
dotnet run --project src/CMS.Api --launch-profile http
```

رمز مدیر باید ۱۲ تا ۱۲۸ نویسه باشد. create-admin حساب جدید می‌سازد و حساب موجود را خودکار ارتقا نمی‌دهد.
کلید تصادفی بالا مخصوص توسعه است؛ برای حفظ نشست‌ها پس از restart، کلید ثابت تصادفی را در secret store محیط نگهداری کنید. اتصال، کلید و رمز واقعی نباید وارد Git شوند.
در Development: [OpenAPI](http://localhost:5045/openapi/v1.json) و [Scalar](http://localhost:5045/scalar/v1).

## مسیرهای فعلی

| مسیر | کاربرد / مجوز |
|---|---|
| POST /api/v1/identity/register | حساب عادی؛ tenant، email، password |
| POST /api/v1/identity/login | ورود؛ tenant، email، password |
| POST /api/v1/identity/refresh | tenant و refreshToken؛ مصرف یک‌باره |
| POST /api/v1/identity/logout | ابطال نشست احرازشده؛ بدون بدنه |
| POST /api/v1/identity/roles | تعریف نقش؛ identity.manage |
| POST /api/v1/identity/roles/assign | انتساب نقش؛ identity.manage |
| GET/POST /api/v1/platform/locales/ | زبان‌های مستاجر؛ platform.manage |
| POST /api/v1/content/types | تعریف نوع محتوا؛ content.edit |
| GET/POST /api/v1/content/entries | فهرست، ایجاد یا ویرایش؛ content.edit |
| POST /api/v1/content/entries/transition | گردش انتشار؛ content.edit و content.publish |
| POST /api/v1/content/entries/restore | بازگردانی نسخه؛ content.edit |
| GET /api/v1/public/{tenant}/{locale}/entries/{slug} | فقط محتوای منتشرشده و موعدرسیده؛ اسلاگ قدیمی 301 می‌گیرد |
| GET /api/v1/health/live | زنده بودن فرایند |
| GET /api/v1/health/ready | اتصال واقعی SQL |

در مسیرهای مدیریتی از Authorization: Bearer استفاده کنید. tenant فقط از claim تأییدشده می‌آید؛ header دلخواه tenant معتبر نیست.
فرمان‌های مدیریتی idempotencyKey دارند. ویرایش، transition و restore نیازمند header برابر `If-Match: "<rowVersion>"` هستند؛ rowVersion در فهرست مدیریت Base64 است. نسخهٔ قدیمی پاسخ 412 می‌گیرد.

نمونهٔ نوع محتوا:

```json
{
  "key": "article",
  "name": "مقاله",
  "fieldsJson": "{\"body\":{\"type\":\"text\",\"required\":true}}",
  "idempotencyKey": "create-article-type"
}
```

fieldsJson فعلاً رشتهٔ JSON است. نوع‌های text، number، boolean، date، media، reference، group و json پشتیبانی می‌شوند. برای media/reference فقط شکل GUID بررسی می‌شود؛ وجود رسانه هنوز بررسی نمی‌شود. نوع محتوا پس از ایجاد تغییر نمی‌کند.

## تنظیمات

| متغیر محیطی | کاربرد |
|---|---|
| ConnectionStrings__Cms | اتصال SQL؛ دیتابیس سیستمی و MARS پذیرفته نیست |
| Jwt__SigningKey | کلید تصادفی حداقل ۳۲ بایت، خارج مخزن |
| Jwt__Issuer / Jwt__Audience | پیش‌فرض cms / cms-api |
| Outbox__Enabled | پیش‌فرض true؛ false برای توقف worker |
| Cors__Origins__0 | origin مجاز؛ پیش‌فرض هیچ origin خارجی مجاز نیست |
| CMS_TEST_SQL_CONNECTION | اتصال SQL تست؛ پیش‌فرض Windows، LocalDB است |

عمر access token برابر ۱۵ دقیقه و خانوادهٔ refresh حداکثر ۳۰ روز است. پنج خطای ورود، حساب را ۱۵ دقیقه قفل می‌کند. ابطال نشست و مجوزها از SQL بررسی می‌شوند؛ Redis هنوز پیاده نشده است. Rate limit محلی هر instance است.
برای production، TLS و reverse proxy مورداعتماد باید متناسب با محیط تنظیم شوند؛ اعتماد عمومی به forwarded headers فعال نیست.

## اعتبارسنجی و استقرار

```powershell
dotnet test CMS.slnx -c Release --no-build --no-restore
dotnet format CMS.slnx --verify-no-changes --no-restore
dotnet list CMS.slnx package --vulnerable --include-transitive --no-restore
```

تست‌های SQL دیتابیس تصادفی با پیشوند CMS_PlatformTests_ می‌سازند و فقط همان را پاک می‌کنند. حساب تست به مجوز ساخت و حذف دیتابیس تست نیاز دارد.
Dockerfile دو target به نام api و migrator دارد. Migration پیش از اجرای API از target دوم اجرا می‌شود. فایل CI سرویس SQL Server 2022 را برای تست راه‌اندازی می‌کند. اجرای Docker و GitHub Actions در محیط فعلی تأیید نشده است؛ Docker روی دستگاه نصب نیست.
