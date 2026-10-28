# تصمیم‌های مبنای بازسازی — ۲۰۲۶-۱۰-۲۸

## مرجع و دامنه

- `p1.md` و `p2.md` پس از یکسان‌سازی فاصله‌ها دقیقاً همان متن `CMS_Spec_v3.0.md` هستند. یک نسخهٔ کامل و بدون تغییر از فایل اصلی در همین پوشه نگهداری می‌شود؛ دو مرجع موازی ساخته نمی‌شود.
- مبنای اجرایی، درخواست جدید کاربر برای پاک‌سازی کار قبلی، شروع یکپارچه و حل ابهام‌ها مطابق استاندارد و مستندات است. تصمیم‌های فنی پیشنهادی در زیر ثبت شده‌اند؛ نیازی به تکرار پرسش‌های Q-01 تا Q-12 برای شروع فاز صفر نیست.
- ترتیب فازهای ۰ تا ۷ و گزارش build/test هر فاز حفظ می‌شود. گزارش پایان فاز صفر به معنای آماده بودن کل CMS نیست.
- فایل‌های محلی پروژه و ارجاع‌های منسوخ در دامنهٔ پاک‌سازی هستند. تاریخچهٔ گفتگو، تنظیمات سراسری ابزار، Downloads و مخازن دیگر در دامنهٔ تغییر نیستند.
- Git در بررسی اولیه هیچ commit نداشت. ساختار `.git` حفظ می‌شود؛ commit، branch یا تغییر remote ایجاد نمی‌شود.

## تصمیم‌های فنی

| موضوع | تصمیم |
|---|---|
| Q-01 | هدف `net10.0`، SDK نصب‌شدهٔ `10.0.302`، C# 14؛ بدون ادعای «آخرین نسخه» |
| Q-02 | Mapping دستی برای تبدیل‌های کوچک؛ بررسی Mapperly فقط در فاز دارای مصرف واقعی؛ AutoMapper نصب نمی‌شود |
| Q-03 | MediatR سری 12.5 مطابق گزینهٔ سند، در اولین فاز CQRS و پس از بررسی بسته/امنیت آن؛ اکنون بدون مصرف است و حذف شده |
| Q-04 | ساخت به ترتیب فازهای سند، ابتدا زیرساخت قابل آزمون |
| Q-05 | نوشتن با EF + Repository/UnitOfWork؛ خواندن پیچیده با Dapper پارامتری؛ خواندن ساده با EF بدون tracking مجاز |
| Q-06 | کامنت‌های کد انگلیسی، راهنمای پروژه فارسی |
| Q-07 | چندمستاجری از نخستین مدل persisted؛ هویت tenant از دادهٔ معتبر سرور، نه اعتماد به header دلخواه |
| Q-08 | API-first؛ پنل SPA جزو خروجی فاز صفر نیست |
| Q-09 | هدف استقرار Docker و SQL Server 2022؛ دسترس‌پذیری Docker/SQL/FTS در فاز یک بررسی می‌شود |
| Q-10 | Master BRD و Volume 2 همراه این سه فایل نبودند؛ الزام‌های ذکرنشده از آن‌ها ساخته یا تأییدشده فرض نمی‌شوند |
| Q-11 | الزامات اختصاصی قانونی مالیات/فاکتور ایران طبق سند خارج دامنه‌اند |
| Q-12 | بسته‌ها فقط هنگام مصرف، با اطلاع در گزارش و نسخهٔ متمرکز اضافه می‌شوند |

## رفع تعارض‌های سند

1. عبارت «Domain هیچ وابستگی ندارد» به معنای نداشتن وابستگی به framework/لایه‌های بیرونی تفسیر می‌شود. گراف بخش ۳٫۱ صریح‌تر است: Domain به SharedKernel وابسته است. SharedKernel هیچ بسته یا ProjectReference ندارد.
2. Infrastructure فقط به Application ارجاع مستقیم دارد؛ Domain و SharedKernel از مسیر وابستگی پروژه در دسترس‌اند. API فقط به Application و Infrastructure متصل می‌شود.
3. وابستگی‌های EF، SqlClient، Dapper، MediatR و NSubstitute فعلاً مصرف ندارند. نسخه‌های قبلی حفظ مصنوعی نمی‌شوند؛ نصب مجدد به فاز مصرف موکول است. حذف Faithlife و Swashbuckle قطعی است.
4. OpenAPI و Mvc.Testing با runtime نصب‌شدهٔ `10.0.10` هم‌نسخه شده‌اند. Scalar `2.18.3` از فهرست رسمی NuGet انتخاب شده است. نسخه‌ها و dependencies با Restore و audit واقعی اعتبارسنجی می‌شوند؛ این انتخاب ادعای به‌روز بودن runtime سیستم نیست.
5. Redis منبع حقیقت نیست. در فاز persistence، یکتایی/تراکنش SQL تضمین نهایی idempotency و موجودی خواهد بود؛ Redis نباید تنها ضامن درستی پرداخت باشد. رفتار قطعی آن برای cache و قابلیت‌های امنیتی جدا تعریف خواهد شد.
6. ثبت event در Outbox باید همراه دادهٔ business در همان تراکنش انجام شود؛ تحویل event پس از commit است. SharedKernel فقط رویدادهای pending را نگه می‌دارد و dispatch نمی‌کند.
7. `Entity<TId>` هویت غیرپیش‌فرض و تغییرناپذیر دارد؛ equality موجودیت‌ها فعلاً reference-based است. `ValueObject` بر اساس نوع و اجزای مقدار مقایسه می‌شود. پایه‌های audit/soft delete هنگام وجود مدل واقعی اضافه می‌شوند.
8. Guard برای خطاهای برنامه‌نویسی و invariant اولیه است؛ خطاهای تجاری قابل پیش‌بینی باید `Result` برگردانند. شکست نباید Error.None داشته باشد و Result موفقِ generic نمی‌تواند payload برابر null داشته باشد.
9. هیچ endpoint قدیمیِ سلامت یا ورودِ صوری باقی نمی‌ماند. readiness واقعی SQL/Redis، پیکربندی امن و migration در فاز یک اضافه می‌شوند. Migrator تا آن زمان موفقیت کاذب گزارش نمی‌کند.
10. Restore اولیه به‌درستی با `NU1903` روی وابستگی transitive یعنی `Microsoft.OpenApi 2.0.0` شکست خورد. مطابق advisory رسمی `GHSA-v5pm-xwqc-g5wc`، نسخهٔ وصله‌شدهٔ `2.7.5` با CentralPackageTransitivePinning تثبیت شد؛ هیچ هشدار امنیتی suppress نشده است.

## قواعد اعتبارسنجی

- build همهٔ پروژه‌ها با warnings-as-errors؛ هشدارهای audit با شدت پایین/متوسط همچنان گزارش می‌شوند و High/Critical خطا هستند.
- بررسی امنیت وابستگی مستقیم و transitive فعال است؛ suppress امنیتی اضافه نشده است.
- تست معماری علاوه بر اسمبلی‌ها، ProjectReferenceهای واقعی را بررسی می‌کند تا پروژه‌های فعلاً خالی باعث موفقیت کاذب گراف نشوند.
- تست‌های runtime برای namespaceهای feature در فاز صفر ممکن است بدون نوع منطبق باشند؛ این‌ها guard آینده هستند و پوشش feature محسوب نمی‌شوند.
- تست‌های SharedKernel رفتار نتیجه‌ها، هویت، رویداد، equality و guardها را پوشش می‌دهند؛ تست API با WebApplicationFactory خود برنامه را راه‌اندازی می‌کند.

## منابع فنی بررسی‌شده

- Microsoft Learn: Central Package Management — `https://learn.microsoft.com/nuget/consume-packages/central-package-management`
- Microsoft Learn: Auditing package dependencies — `https://learn.microsoft.com/nuget/concepts/auditing-packages`
- Microsoft Learn: ASP.NET Core integration tests — `https://learn.microsoft.com/aspnet/core/test/integration-tests?view=aspnetcore-10.0`
- نسخه‌های موجود بسته‌ها از endpoint رسمی `https://api.nuget.org/v3-flatcontainer/` بررسی شدند.

## به‌روزرسانی اجرای فعلی

- درخواست فعلی ادامه روی پروژهٔ موجود است؛ پاک‌سازی دوباره انجام نشد. دستور نمونهٔ داخل سند برای توقف هر فاز، بر درخواست مستقیم کاربر برای ادامه و سؤال در پایان مقدم نیست.
- فایل‌های موجود حفظ شدند؛ commit یا push انجام نشده است. بخش‌های بالای این فایل تاریخچهٔ فاز صفرند؛ وضعیت فعلی در IMPLEMENTATION_STATUS است.
- JwtBearer 10.0.10 هنگام مصرف اضافه شد. Infrastructure برای PasswordHasher/JWT به ASP.NET Core FrameworkReference دارد؛ Domain/Application مستقل مانده‌اند. PackageReference تکراری Hosting و HealthChecks از Infrastructure حذف شد.
- ابطال نشست و مجوزها فعلاً از SQL خوانده می‌شوند؛ Redis blacklist پیاده نشده است. ردیف کاربر در login و نشست در refresh با UPDLOCK در transaction خوانده می‌شوند تا رقابت هم‌زمان شمارنده یا مصرف توکن را دور نزند.
- نقش‌ها از catalogue صریح استفاده می‌کنند. ثبت‌نام عمومی مدیر ایجاد نمی‌کند. bootstrap مدیر فقط CLI و رمز از environment است.
- CORS پیش‌فرض بسته است؛ rate limit فعلاً per-instance است. secrets واقعی وارد مخزن نشده‌اند.
- ContentType فعلاً immutable است؛ محدودیت‌های schema evolution، تاریخچه و alias در گزارش وضعیت ثبت شده‌اند.
- مبنای JwtBearer: [Microsoft Learn](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/configure-jwt-bearer-authentication?view=aspnetcore-10.0).
- مبنای container: [NET containers](https://learn.microsoft.com/en-us/dotnet/core/docker/build-container) و [SQL containers](https://learn.microsoft.com/en-us/sql/linux/containers/deploy?view=sql-server-ver17). Docker در این دستگاه اجرا نشده است.
