# وضعیت پیاده‌سازی — ۲۰۲۶-۱۰-۲۸

این گزارش جایگزین گزارش قدیمی «فقط فاز صفر» است. درخواست کاربر همهٔ مستندات را پوشش می‌دهد، اما **همهٔ آن درخواست هنوز پیاده نشده است**. موارد زیر تفکیک کد اجرایی از کار باقی‌مانده‌اند.

## بخش‌های اجراشده

- SharedKernel، Clean Architecture، مدیریت مرکزی بسته‌ها و lock fileها.
- Platform: Tenant/Locale، migration، Audit، Outbox با lease/retry، idempotency تراکنشی SQL، health و Serilog.
- Identity: ثبت‌نام عادی، PBKDF2، قفل ورود، JWT رسمی، refresh rotation، تشخیص reuse و logout با ابطال خانوادهٔ نشست.
- Role: تعریف نقش با مجوز مشخص، انتساب نقش در همان tenant، بازخوانی مجوزها در هر درخواست؛ bootstrap مدیر فقط از CLI.
- Content: نوع محتوای immutable، اعتبارسنجی فیلدهای پایه، Entry برای هر locale، revision/restore، گردش Draft/Review/Published/Archived، زمان‌بندی visibility، تغییر اسلاگ و redirect.
- API: Problem Details و traceId، rate limit محلی و Retry-After، CORS allowlist، If-Match، ETag و محدودیت بدنهٔ یک مگابایت.
- چهار migration: InitialPlatform، Identity، Content و Roles؛ ارتقای کاربران موجود نیز آزموده شده است.
- فایل‌های Docker و CI اضافه شده‌اند؛ اجرای آن‌ها در این دستگاه تأیید نشده است.

## موارد باقی‌مانده

| بخش | موارد پیاده‌نشده / محدودیت |
|---|---|
| Platform | Redis/cache، telemetry، pipeline عمومی logging/authorization، پاک‌سازی دوره‌ای، اعلان واقعی Outbox |
| Identity | تأیید ایمیل/موبایل، بازیابی رمز، GDPR، مدیریت کامل/حذف نقش، 2FA و OIDC؛ امضا فعلاً HS256 بدون key rotation است |
| Content | Taxonomy، گروه ترجمه/hreflang، API فهرست/مقایسهٔ revision، schema evolution، preview؛ یکتایی مشترک alias/slug بین دو جدول تضمین نشده و slug جاری اولویت دارد |
| فاز ۴ | Media، پردازش تصویر/ضدبدافزار، Menus، Blocks، FTS، SEO/sitemap/robots |
| فاز ۵ | Forms، ۱۸ نوع فیلد، منطق شرطی، submission، captcha، اعلان و retention |
| فاز ۶ | محصول، سبد، سفارش، پرداخت، موجودی و کوپن |
| فاز ۷ | افزونه، import/export، webhook و امضای تحویل |
| عملیات | اجرای Docker/CI، تست بار، اثبات p95/پوشش ۸۰٪/ASVS، backup/restore آزموده‌شده و پنل مدیریت |

ویرایش محتوا آن را به Draft برمی‌گرداند؛ نسخهٔ منتشرشدهٔ جدا از پیش‌نویس نگهداری نمی‌شود. زمان‌بندی در query اعمال می‌شود، نه job. Revisionها همراه aggregate کامل خوانده می‌شوند؛ retention و صفحه‌بندی تاریخچه هنوز لازم است.

## شواهد اعتبارسنجی

تست SQL با Windows LocalDB واقعی اجرا شده است؛ از EF InMemory استفاده نشده است. تست‌ها migration از صفر، ارتقای حساب موجود، tenant isolation در EF/Dapper، rollback، audit/outbox، idempotency هم‌زمان، hash/lockout، refresh هم‌زمان/replay، logout، نقش، JWT، 401/403، rate limit، schema، workflow، schedule، revision/restore و stale edit را بررسی می‌کنند.
نتایج اجرای نهایی در ادامه ثبت می‌شود. لاگ و TRX در artifacts قرار می‌گیرد و وارد Git نمی‌شود.

## اطلاعات موجودنبوده در مستندات

Master BRD و Volume 2 ارجاع‌شده ارائه نشده‌اند. انتخاب سرویس واقعی پرداخت، ایمیل/SMS، storage و هدف استقرار نیز مشخص نشده است. نبود این اطلاعات به معنی تکمیل سایر موارد نیست؛ مشخصات فعلی همچنان کار اجرایی باقی‌مانده دارند.

## نتیجهٔ اجرای نهایی

| بررسی | نتیجهٔ واقعی |
|---|---|
| restore --locked-mode | موفق برای ۱۲ پروژه |
| build Release --no-restore | موفق؛ صفر هشدار و صفر خطا |
| test Release --no-build --no-restore | ۱۰۷ موفق؛ صفر شکست و صفر skipped |
| SharedKernel | ۲۵ موفق |
| Domain | ۲۴ موفق |
| Application | ۲۲ موفق |
| Architecture | ۱۸ موفق |
| API integration | ۹ موفق |
| SQL integration | ۹ موفق |
| dotnet format --verify-no-changes --no-restore | موفق؛ exit code 0 |
| NuGet vulnerability audit مستقیم و transitive | هیچ آسیب‌پذیری در منبع فعلی گزارش نشد |
| Docker / GitHub Actions | اجرا نشده؛ Docker CLI در دستگاه موجود نیست |

Build log در artifacts/build-final.log و شش فایل final_*.trx در artifacts/TestResults ذخیره شده‌اند. تست‌های موفق، فقط دامنهٔ رفتارهای پیاده‌شده را تأیید می‌کنند و به معنی تکمیل تمام مشخصات نیستند.
