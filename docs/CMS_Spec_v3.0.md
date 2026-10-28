# سند جامع نیازمندی‌ها و معماری — CMS ماژولار (نسخه ۳.۰ — پیش‌نویس برای تأیید)

**هدف سند:** مرجع واحد برای ساخت CMS از صفر (محتوا + فروشگاه + فرم‌ساز + کاربران + رسانه + افزونه‌پذیری)، هم‌سطح WordPress+WooCommerce، Drupal و Joomla. این سند جایگزین بخش‌های فنی `Master_BRD_v2.0_FINAL.md` است و محتوای کارکردی آن (CNT / FRM / STR / PLT) را به‌عنوان پیوست الزام‌آور نگه می‌دارد.

**وضعیت:** پروژه خالی است (فقط اسکلت Solution). همه اعداد با برچسب «پیشنهاد» قابل تغییرند.

---

## بخش ۱ — وضعیت فعلی Solution و یافته‌ها

| مورد | وضعیت دیده‌شده | اقدام |
|---|---|---|
| پروژه‌های `src` | Api، Application، Domain، Infrastructure، Migrator | حفظ (ساختار Clean Architecture درست است) |
| پروژه‌های `tests` | Api.IntegrationTests، Application.UnitTests، Domain.UnitTests، Infrastructure.IntegrationTests، ArchitectureTests | حفظ؛ فایل‌های نمونه `UnitTest1.cs` حذف و `FormsAndStoreTests.cs` بازنویسی شود |
| پروژه `Contracts`/`Shared` | وجود ندارد | **افزودن `CMS.SharedKernel`** (پایه‌های مشترک بدون وابستگی) — بخش ۳ |
| **`Faithlife.Utility.Dapper` 2.0.1** | در Infrastructure نصب است | **این بسته Dapper اصلی نیست.** حذف و جایگزینی با `Dapper` |
| **`MediatR` 14.2.0** | در Application | نسخه ۱۳ به بعد **مجوز تجاری** دارد (بخش ۲، تصمیم T-01) |
| `Swashbuckle.AspNetCore` 10.3.1 + `Microsoft.AspNetCore.OpenApi` | هر دو در Api | **دوگانه است.** فقط OpenApi داخلی + یک UI (Scalar) |
| `Microsoft.Data.SqlClient` 7.1.1 | هشدار آسیب‌پذیری در Infrastructure | ارتقا/تثبیت به نسخه وصله‌شده؛ در غیر این‌صورت استثنای مستند |
| `NetArchTest.Rules` 1.3.2، `NSubstitute`، `xunit`، `coverlet` | نصب‌شده | حفظ |
| بسته‌های ناموجود | AutoMapper، FluentValidation، JwtBearer، Redis، EF Tools، Testcontainers، Respawn | در مرحله‌ای که مصرف می‌شوند نصب شوند |

---

## بخش ۲ — تصمیم‌های فناوری (نیازمند تأیید)

> قاعده: هر بسته فقط در مرحله‌ای نصب شود که استفاده می‌شود. هیچ نسخه‌ای بدون تأیید تغییر نکند. CI روی آسیب‌پذیری High/Critical شکست بخورد.

| شناسه | موضوع | تصمیم پیشنهادی | دلیل |
|---|---|---|---|
| T-01 | **MediatR** | استفاده برای CQRS. نسخه ۱۲.۵.x (Apache-2.0، رایگان) یا نسخه ۱۴ با کلید مجوز Community (رایگان برای درآمد سالانه زیر ۵ میلیون دلار) | از ۱۳ به بعد کلید مجوز لازم است؛ نبود کلید هشدار/خطا می‌دهد |
| T-02 | **AutoMapper** | نیاز به تصمیم شما. نسخه ۱۴ (آخرین MIT) **آسیب‌پذیری High** دارد (DoS روی گراف‌های تودرتو)، ۱۵+ تجاری است | گزینه‌ها در بخش ۱۰ سؤال Q-02 |
| T-03 | **FluentValidation** 12.x | اعتبارسنجی Command ها در Pipeline Behavior؛ Apache-2.0 | رایگان |
| T-04 | **EF Core** (SQL Server) | **فقط Command (نوشتن)** + Migration | تراکنش و Change Tracking |
| T-05 | **Dapper** | **فقط Query سنگین/گزارش/لیست‌های پیچیده**، فقط SELECT | سرعت و کنترل SQL |
| T-06 | **Redis** (`StackExchange.Redis`) | cache توزیع‌شده (`HybridCache`/`IDistributedCache`)، rate-limit، قفل توزیع‌شده، idempotency key، blacklist توکن، صف سبک | فقط جایی که کاربرد واقعی دارد (جدول ۸.۳) |
| T-07 | **JWT** | بسته رسمی `Microsoft.AspNetCore.Authentication.JwtBearer`؛ بدون handler دست‌نویس | استاندارد مایکروسافت |
| T-08 | **پایگاه داده** | **SQL Server** (تصمیم شما). Full-Text Search، JSON به‌صورت `nvarchar(max)` + `ISJSON` + ستون محاسباتی ایندکس‌شده | جایگزین PostgreSQL در Master BRD |
| T-09 | **Logging** | `Serilog` (Console + File/Seq) + `OpenTelemetry` (trace/metrics) | قابلیت مشاهده‌پذیری |
| T-10 | **OpenAPI** | `Microsoft.AspNetCore.OpenApi` + `Scalar.AspNetCore`؛ حذف Swashbuckle | یک مسیر پشتیبانی‌شده |
| T-11 | **API Versioning** | `Asp.Versioning.Http` + مسیر `/api/v1/` | |
| T-12 | **Resilience** | `Microsoft.Extensions.Http.Resilience` (Polly v8) برای فراخوانی بیرونی (درگاه، webhook) | |
| T-13 | **Background jobs** | `BackgroundService` + Outbox در پایگاه داده؛ در صورت نیاز Hangfire (آیتم باز) | |
| T-14 | **تست** | xUnit، NSubstitute، NetArchTest، **Testcontainers.MsSql**، **Respawn**، `Bogus`، `Microsoft.AspNetCore.Mvc.Testing`. برای assertion: **Shouldly یا AwesomeAssertions** (FluentAssertions 8 تجاری است) | |
| T-15 | **Health Checks** | `AspNetCore.HealthChecks` برای SQL Server و Redis | |
| T-16 | **Hashing رمز** | `PasswordHasher` از ASP.NET Core Identity (PBKDF2) یا Argon2id | |
| T-17 | **شناسه (Primary Key)** | `Guid`؛ در SQL Server ترتیب مقایسه GUID با نسخه ۷ همخوان نیست؛ از مولد sequential سازگار با SQL Server استفاده شود (آیتم باز O-12) | جلوگیری از fragmentation |
| T-18 | **ASP.NET Identity** | استفاده نمی‌شود (کنترل کامل مدل کاربر/نقش/مجوز)؛ فقط `PasswordHasher` | |

---

## بخش ۳ — معماری (Clean Architecture)

### ۳.۱ لایه‌ها و قاعده وابستگی

```
CMS.Api ─────────────► CMS.Application ──► CMS.Domain ──► CMS.SharedKernel
   │                         ▲                                  ▲
   └──► CMS.Infrastructure ──┘ (پیاده‌سازی رابط‌های Application)─┘
CMS.Migrator ► Infrastructure (اجرای migration)
```

| لایه | مسئولیت | وابسته به | ممنوع |
|---|---|---|---|
| **SharedKernel** | `Result<T>`، `Error`، `Entity<TId>`، `AggregateRoot`، `IDomainEvent`، `ValueObject`، `Guard` | هیچ | هر بسته |
| **Domain** | موجودیت‌ها، Value Object، قوانین دامنه، رویداد دامنه، رابط‌های دامنه | SharedKernel | EF، MediatR، هر چارچوب |
| **Application** | Command/Query/Handler، Validator، DTO، Mapping Profile، رابط‌ها (`IRepository`، `IUnitOfWork`، `ICacheService`، `IClock`، `ICurrentUser`)، Pipeline Behavior | Domain | EF Core، Dapper، Redis، ASP.NET |
| **Infrastructure** | EF DbContext + Configurations + Repository، Query سمت Dapper، Redis، JWT service، ایمیل، ذخیره فایل، Outbox، Migration | Application | وابستگی به Api |
| **Api** | Controller/Endpoint، Middleware، Auth، DI Composition Root | Application، Infrastructure | منطق کسب‌وکار |

**قواعد (با تست معماری الزامی):**
1. Domain به هیچ لایه‌ای وابسته نیست. Application به Infrastructure وابسته نیست.
2. ماژول‌های Content، Forms، Store: Content به Forms/Store وابسته نیست؛ Forms و Store به Content و Platform وابسته‌اند (X-10).
3. Controller فقط `ISender.Send` صدا می‌زند؛ هیچ `DbContext` در Api نیست.
4. Handler مستقیم از `DbContext` در Application استفاده نمی‌کند (فقط از رابط‌ها).
5. همه کلاس‌های Handler، Validator، Configuration و Repository **`sealed`** هستند.

### ۳.۲ ساختار پوشه‌ها (Feature-based داخل هر لایه)

```
src/
  CMS.SharedKernel/        Primitives/ Results/ Guards/
  CMS.Domain/
    Platform/              Locales/ Tenants/ Media/ Workflows/ Permissions/
    Identity/              Users/ Roles/ RefreshTokens/
    Content/               Entries/ ContentTypes/ Fields/ Revisions/ Taxonomy/ Menus/ Blocks/
    Forms/                 Forms/ FormVersions/ Submissions/
    Store/                 Products/ Variants/ Carts/ Orders/ Payments/ Inventory/ Coupons/
    Abstractions/          (رابط‌های دامنه)
  CMS.Application/
    Common/                Abstractions/ Behaviors/ Mappings/ Exceptions/ Models/
    Platform/ Identity/ Content/ Forms/ Store/
       └─ <Feature>/       Commands/<Name>/{Command,Handler,Validator}.cs
                           Queries/<Name>/{Query,Handler,Dto}.cs
                           EventHandlers/
  CMS.Infrastructure/
    Persistence/           Write/ (DbContext, Configurations/, Repositories/, Interceptors/)
                           Read/  (Dapper: IDbConnectionFactory، QueryObjects/)
                           Migrations/
    Caching/ Identity/ Messaging/ Storage/ Email/ Payments/ Search/ BackgroundJobs/
    DependencyInjection.cs
  CMS.Api/
    Controllers/ (یا Endpoints/)  v1/<Feature>/
    Middleware/ Filters/ Extensions/ Authorization/ Contracts/ (Request/Response)
    Program.cs
  CMS.Migrator/
tests/
  CMS.Domain.UnitTests/  CMS.Application.UnitTests/  CMS.Infrastructure.IntegrationTests/
  CMS.Api.IntegrationTests/  CMS.ArchitectureTests/
```

### ۳.۳ الگوهای الزامی

| الگو | قاعده |
|---|---|
| **CQRS** | Command تغییر وضعیت می‌دهد و فقط شناسه/`Result` برمی‌گرداند. Query هرگز وضعیت را تغییر نمی‌دهد. جدا در پوشه و جدا در دسترسی داده |
| **EF برای Command** | Handler از طریق `IRepository<T>` و `IUnitOfWork` کار می‌کند. `SaveChanges` فقط در UnitOfWork (Behavior تراکنشی) |
| **Dapper برای Query** | رابط `IReadDbConnectionFactory`؛ SQL پارامتری (بدون الحاق رشته)؛ هر Query در کلاس جدا؛ خروجی مستقیم DTO. برای کوئری‌های ساده تک‌رکوردی مجاز است از EF با `AsNoTracking` استفاده شود (تصمیم در SRS) |
| **Generic Repository** | `IRepository<TEntity>` (Add، Update، Remove، GetById) + `IReadRepository<TEntity>` با **Specification** برای فیلتر. فقط برای Aggregate Root؛ نه برای هر جدول |
| **Abstract class** | `Entity<TId>`، `AggregateRoot<TId>`، `AuditableEntity`، `SoftDeletableEntity`، `BaseConfiguration<T>`، `ApiControllerBase` |
| **Sealed class** | همه پیاده‌سازی‌های نهایی؛ Value Object ها؛ Handler ها |
| **Pipeline Behaviors (ترتیب)** | Logging ← Validation (FluentValidation) ← Authorization ← Idempotency ← Transaction ← Caching (فقط Query) |
| **Domain Events** | تولید در Aggregate؛ انتشار بعد از commit؛ ذخیره در Outbox برای تحویل مطمئن |
| **Result Pattern** | خطای قابل پیش‌بینی با `Result<T>`؛ Exception فقط برای خطای غیرمنتظره |
| **Options Pattern** | همه تنظیمات با `IOptions<T>` + `ValidateOnStart` |
| **Specification** | فیلتر قابل‌ترکیب برای Repository |
| **Strategy/Registry (OCP)** | نوع بلوک، نوع فیلد فرم، درگاه پرداخت، روش حمل، Storage، Captcha، کانال اعلان |
| **Mapping** | AutoMapper (یا گزینه تأییدشده) فقط در Application برای Entity↔DTO. Dapper مستقیم DTO می‌سازد و Mapping نمی‌خواهد |

### ۳.۴ قراردادهای کدنویسی (استاندارد مایکروسافت)

- نام‌گذاری: PascalCase برای نوع/متد/ویژگی، `_camelCase` برای فیلد خصوصی، پیشوند `I` برای رابط، پسوند `Async` برای متد ناهمگام. namespace هم‌نام پوشه (File-scoped).
- `Nullable` و `ImplicitUsings` فعال؛ `TreatWarningsAsErrors` برای Domain/Application.
- همه متدهای I/O ناهمگام با `CancellationToken`.
- یک نوع در هر فایل. `record` برای Command/Query/DTO. `init`/`required` برای ویژگی‌های اجباری.
- `.editorconfig` و `Directory.Build.props` و `Directory.Packages.props` (مدیریت مرکزی نسخه بسته‌ها).
- **کامنت:** یک‌خطی `//` بالای هر کلاس، رابط، متد و ویژگی عمومی. کامنت توضیح «چرا» است، نه تکرار نام. در رابط‌ها بالای هر عضو. زبان کامنت: انگلیسی (پیشنهاد؛ آیتم باز Q-06).
- بدون سایه‌گذاری `catch (Exception)` خالی؛ بدون `async void`؛ بدون `static` وضعیت‌دار.

---

## بخش ۴ — احراز هویت و مجوز (JWT)

| الزام | شرح |
|---|---|
| توکن دسترسی | JWT امضاشده، عمر کوتاه (پیشنهاد ۱۵ دقیقه)، ادعاها: `sub`، `tenant_id`، `role`، `jti`، `permissions` (یا مراجعه به cache) |
| امضا | HS256 با کلید از Secret، یا **RS256/ES256** (توصیه برای محیط تولید با چرخش کلید `kid`) |
| Refresh Token | تصادفی مبهم، **فقط hash در پایگاه داده**، چرخش در هر استفاده (rotation)، تشخیص استفاده مجدد (reuse detection) ← ابطال کل خانواده |
| Logout | ابطال refresh؛ `jti` توکن دسترسی در Redis blacklist تا انقضا |
| رمز | PBKDF2/Argon2id، سیاست طول و ضعف رمز، قفل حساب پس از N تلاش ناموفق، rate limit |
| مجوز | RBAC + مجوز ریز (`content.entry.publish`) با `IAuthorizationHandler` و Policy؛ مجوز مبتنی بر منبع (مالک/مستاجر) |
| احراز دو مرحله‌ای | P2 (TOTP) |
| اعتبارسنجی | `ValidateIssuer/Audience/Lifetime/IssuerSigningKey`، `ClockSkew` کوچک |
| کلیدها | فقط از Secret/Environment؛ هرگز در مخزن |

---

## بخش ۵ — کاتالوگ ویژگی‌ها (مقایسه با WordPress / WooCommerce / Drupal / Joomla)

برچسب‌ها: **M** = MVP، **P2** = فاز دوم، **P3** = فاز سوم. ستون «مرجع» نشان می‌دهد این قابلیت در کدام محصول الگو است.

### ۵.۱ هسته محتوا
| کد | ویژگی | مرجع | فاز |
|---|---|---|---|
| C-01 | نوع‌های محتوای سفارشی (Content Types) با فیلدهای قابل تعریف | Drupal، WP CPT | M |
| C-02 | فیلدهای سفارشی (متن، عدد، تاریخ، رسانه، ارجاع، JSON، گروه تکرارشونده) | Drupal Fields، ACF | M |
| C-03 | ویرایشگر بلوکی/صفحه‌ساز (بلوک، سکشن، ستون، قالب بلوک قابل‌استفاده مجدد) | Gutenberg، Joomla | M |
| C-04 | Taxonomy: دسته، برچسب، واژگان سلسله‌مراتبی | WP، Drupal | M |
| C-05 | Revision، مقایسه نسخه، بازگردانی | WP، Drupal | M |
| C-06 | گردش‌کار انتشار (Draft/Review/Published/Archived) + زمان‌بندی انتشار | Drupal Workflows | M |
| C-07 | اسلاگ یکتا، تغییر مسیر 301 هنگام تغییر اسلاگ، URL Alias | Drupal Pathauto | M |
| C-08 | منو و ناوبری (چندسطحی، چندمکانه) | WP، Joomla | M |
| C-09 | ویجت/ناحیه (Region/Position) | Joomla، WP | P2 |
| C-10 | کتابخانه رسانه: آپلود، نسخه‌های تصویر (resize/WebP)، متن جایگزین، پوشه، مجوز دسترسی | WP Media | M |
| C-11 | جستجو (Full-Text) با رتبه‌بندی و فیلتر | Drupal Search API | M |
| C-12 | نظرات (Moderation، spam، پاسخ تودرتو) | WP | P2 |
| C-13 | SEO: meta، Open Graph، canonical، sitemap.xml، robots، schema.org JSON-LD | Yoast | M |
| C-14 | چندزبانه (entry-per-locale) + hreflang + جلالی/RTL | Drupal، WPML | M |
| C-15 | قالب/تم (Theme) و Headless (API-first) | WP، Drupal | M (Headless) / P3 (Theme) |
| C-16 | سیستم افزونه (Module/Plugin) با رجیستری و رویداد | WP Hooks، Drupal Modules | P2 |
| C-17 | ورود/خروج داده (Import/Export) JSON/CSV/XML | WP Importer | P2 |
| C-18 | Preview (پیش‌نمایش پیش‌نویس با لینک امضاشده) | Drupal | M |

### ۵.۲ کاربران و امنیت
| کد | ویژگی | مرجع | فاز |
|---|---|---|---|
| U-01 | ثبت‌نام، ورود، بازیابی رمز، تأیید ایمیل/موبایل | همه | M |
| U-02 | نقش‌ها و مجوزهای ریز، نقش سفارشی | Drupal، Joomla ACL | M |
| U-03 | چندمستاجری (Tenant) با جداسازی داده | WP Multisite | M |
| U-04 | ثبت ممیزی (Audit Log) همه تغییرات حساس | Drupal | M |
| U-05 | 2FA (TOTP) | افزونه‌ها | P2 |
| U-06 | ورود اجتماعی/OIDC | | P3 |
| U-07 | کلید API و Webhook با امضای HMAC | WooCommerce | P2 |
| U-08 | GDPR: خروجی داده شخصی، حذف/ناشناس‌سازی | WP Privacy Tools | M |

### ۵.۳ فروشگاه (معادل WooCommerce)
| کد | ویژگی | مرجع | فاز |
|---|---|---|---|
| S-01 | محصول ساده/متغیر، ویژگی (Attribute)، SKU، گالری | WooCommerce | M |
| S-02 | قیمت، قیمت تخفیفی زمان‌دار، چندارزی (`amount_minor`) | WooCommerce | M |
| S-03 | موجودی، رزرو موقت (۱۵ دقیقه)، پیش‌سفارش | WooCommerce | M |
| S-04 | سبد خرید (مهمان و کاربر)، ادغام سبد | WooCommerce | M |
| S-05 | تسویه‌حساب، آدرس، روش حمل، مالیات (قاعده‌محور) | WooCommerce | M |
| S-06 | درگاه پرداخت (رجیستری) + Idempotency + Webhook تأیید | WooCommerce | M |
| S-07 | سفارش: وضعیت‌ها، یادداشت، بازپرداخت، لغو | WooCommerce | M |
| S-08 | کوپن و قوانین تخفیف | WooCommerce | M |
| S-09 | مشتری، تاریخچه سفارش، لیست علاقه‌مندی | WooCommerce | P2 |
| S-10 | دسته‌بندی/فیلتر محصول (Faceted) | WooCommerce | M |
| S-11 | امتیاز و نقد محصول | WooCommerce | P2 |
| S-12 | محصول دیجیتال و لینک دانلود امضاشده | WooCommerce | P2 |
| S-13 | گزارش فروش، موجودی، مشتریان | WooCommerce Analytics | P2 |
| S-14 | فاکتور/رسید PDF | افزونه‌ها | P2 |
| S-15 | اشتراک و پرداخت دوره‌ای | WC Subscriptions | P3 |

### ۵.۴ فرم‌ساز
| کد | ویژگی | مرجع | فاز |
|---|---|---|---|
| F-01 | سازنده فرم با ۱۸ نوع فیلد (رجیستری)، چینش چندمرحله‌ای | Gravity Forms | M |
| F-02 | منطق شرطی، اعتبارسنجی، فیلد محاسباتی | Gravity Forms | M |
| F-03 | نسخه‌بندی فرم (`form_version` هنگام انتشار) | | M |
| F-04 | ذخیره ارسال‌ها، فیلتر، خروجی CSV، حذف خودکار | WPForms | M |
| F-05 | اعلان ایمیل/Webhook با Outbox | | M |
| F-06 | Captcha (خودمیزبان پیش‌فرض + Turnstile اختیاری)، honeypot، rate limit | | M |
| F-07 | آپلود فایل امن (نوع/حجم/ضدبدافزار) | | M |
| F-08 | پرداخت داخل فرم | | P3 |

### ۵.۵ مدیریت و عملیات
| کد | ویژگی | فاز |
|---|---|---|
| A-01 | پنل مدیریت (SPA جدا) با داشبورد | P2 |
| A-02 | Health Checks، Metrics، Tracing، Log ساختاریافته | M |
| A-03 | Backup/Restore و Migration نسخه‌دار | M |
| A-04 | Cache چندلایه (Output cache، Redis) و CDN | M |
| A-05 | صف و کارهای زمان‌بندی‌شده (انتشار، پاک‌سازی، سفارش منقضی) | M |
| A-06 | ایمیل/SMS/Push از طریق کانال‌های رجیستری | M |
| A-07 | بین‌المللی‌سازی پیام‌های سیستم (resx) | M |

---

## بخش ۶ — مدل داده (SQL Server)

- نام جدول‌ها snake_case با پیشوند ماژول (`cnt_`، `str_`، `frm_`، `plt_`، `idn_`)، کلید اصلی `uniqueidentifier`، زمان `datetimeoffset(7)` (UTC)، پول `bigint amount_minor` + `char(3) currency`.
- JSON: `nvarchar(max)` + `CHECK (ISJSON(col)=1)`؛ برای جستجو، ستون محاسباتی `JSON_VALUE` + ایندکس.
- جستجو: SQL Server Full-Text Index (مورد نیاز: نصب قابلیت FTS؛ آیتم باز O-10).
- هر جدول چندمستاجری دارای `tenant_id`؛ Global Query Filter در EF و شرط صریح در Dapper (تست خودکار برای نبود نشت).
- Soft delete فقط جایی که قانون تجاری می‌خواهد؛ رفتار حذف پیش‌فرض `Restrict`.
- ستون `row_version` (rowversion) برای همزمانی خوش‌بینانه در Aggregate های قابل ویرایش هم‌زمان.
- الگوی چندزبانه: `entry_per_locale` (محتوا)، `shared_structure` (فرم)، `shared_commercial` (محصول).
- یک جدول `locale` تنها منبع زبان است.

## بخش ۷ — API

- مسیر `/api/v1/…`، REST، JSON، `camelCase`.
- خطا: RFC 9457 Problem Details با `traceId` و کد خطای ثابت.
- صفحه‌بندی Cursor برای لیست بزرگ؛ Offset فقط در پنل مدیریت.
- فیلتر/مرتب‌سازی با فهرست سفید فیلدها (ضد injection).
- Idempotency-Key برای POST های حساس (پرداخت، سفارش).
- ETag/If-Match برای ویرایش هم‌زمان؛ `429` با `Retry-After`.
- Rate Limiting داخلی ASP.NET (policy به‌ازای IP/کاربر/کلید).
- CORS با فهرست سفید؛ هدرهای امنیتی (HSTS، CSP، X-Content-Type-Options).
- سند OpenAPI تولیدی + تست قرارداد.

## بخش ۸ — Redis و کش

### ۸.۱ اصول
Redis اختیاری برای درستی نیست؛ فقط بهینه‌سازی و هماهنگی. قطع Redis نباید ثبت سفارش یا ورود را مختل کند (Fail-open برای cache، Fail-closed برای قفل پرداخت).

### ۸.۲ جدول کاربرد
| کاربرد | الگو | TTL پیشنهادی |
|---|---|---|
| Cache Query های پرتکرار (منو، تنظیمات، صفحه منتشرشده) | Cache-aside + باطل‌سازی با رویداد | ۵–۶۰ دقیقه |
| Blacklist توکن | کلید `jti` | تا انقضای توکن |
| Rate limit توزیع‌شده | Sliding window | بر اساس policy |
| قفل توزیع‌شده (رزرو موجودی، پرداخت) | `SET NX PX` + token | ≤ ۳۰ ثانیه |
| Idempotency | کلید + نتیجه | ۲۴ ساعت |
| Output cache API عمومی | Tag-based | ۱–۵ دقیقه |

### ۸.۳ جایی که Redis **استفاده نمی‌شود**
منبع حقیقت (سفارش، موجودی نهایی، کاربر)، ذخیره Session دائمی، صف تحویل مطمئن (Outbox در SQL Server).

## بخش ۹ — الزامات غیرکارکردی (پیشنهاد)

| ویژگی | هدف |
|---|---|
| عملکرد | p95 خواندن API عمومی < ۲۰۰ms (با cache)؛ نوشتن < ۵۰۰ms |
| دسترس‌پذیری | ۹۹.۹٪ |
| امنیت | OWASP ASVS سطح ۲؛ بدون آسیب‌پذیری High/Critical |
| مشاهده‌پذیری | Trace و Log با CorrelationId در همه درخواست‌ها |
| نگهداشت‌پذیری | پوشش تست دامنه ≥ ۸۰٪، برنامه‌ریزی تست معماری |
| قابلیت حمل | Docker، بدون وابستگی به سیستم‌عامل |
| بومی‌سازی | UTF-8، RTL، نمایش جلالی فقط برای `fa` |

## بخش ۱۰ — استراتژی تست

| سطح | ابزار | محتوا |
|---|---|---|
| Domain Unit | xUnit | قوانین دامنه، ماشین حالت انتشار/سفارش، Value Object |
| Application Unit | xUnit + NSubstitute | Handler، Validator، Behavior |
| Architecture | NetArchTest | جهت وابستگی لایه‌ها، `sealed`، نبود وابستگی Content→Store/Forms، نبود `DbContext` در Api |
| Integration (Infra) | Testcontainers SQL Server + Respawn | Repository، Migration، Query های Dapper، فیلتر tenant |
| API Integration | `WebApplicationFactory` | احراز هویت، مجوز، قرارداد خطا |
| Security | تست‌های مجوز (IDOR، tenant leakage) | |
| Performance | k6/NBomber (P2) | |

**قانون:** هر Handler حداقل یک تست موفق و یک تست شکست؛ هر Validator تست مرز؛ هر Query Dapper یک تست یکپارچه.

## بخش ۱۱ — مراحل ساخت (بدون جهش)

| فاز | محتوا | خروجی قابل تأیید |
|---|---|---|
| 0 | تمیزکاری: حذف Faithlife، Directory.Packages.props، .editorconfig، SharedKernel، تست معماری پایه | build سبز بدون هشدار |
| 1 | Platform: Tenant، Locale، Audit، Outbox، Idempotency، Health، Serilog | تست‌ها + migration اول |
| 2 | Identity: کاربر، نقش، JWT، Refresh | تست‌های امنیتی |
| 3 | Content: انواع، فیلد، Entry، Revision، گردش‌کار، Taxonomy، Slug/Redirect | |
| 4 | Media + Menus + Blocks + Search + SEO | |
| 5 | Forms | |
| 6 | Store (محصول → سبد → سفارش → پرداخت → موجودی → کوپن) | |
| 7 | افزونه‌پذیری، Import/Export، Webhook | |

هر فاز: تأیید شما ← سپس فاز بعد. نصب بسته فقط در فاز مصرف‌کننده، با اطلاع قبلی.

## بخش ۱۲ — موارد باز و سؤال‌ها

| شناسه | سؤال | پیشنهاد من |
|---|---|---|
| Q-01 | نسخه .NET هدف (۹ یا ۱۰)؟ بسته‌های دیده‌شده با ۱۰ می‌خوانند | ۱۰ (LTS) |
| Q-02 | AutoMapper: (الف) حذف و Mapping دستی/Mapperly (MIT، Source Generator)، (ب) نسخه ۱۴ با ریسک آسیب‌پذیری، (ج) خرید مجوز | **(الف) Mapperly** — بی‌ریسک و سریع‌تر؛ ولی شما AutoMapper خواستید، تأیید لازم |
| Q-03 | MediatR: نسخه ۱۲.۵ رایگان یا ۱۴ با کلید Community؟ | ۱۲.۵ |
| Q-04 | اسکوپ اولیه: همه فازها یا ابتدا Content+Identity؟ | فازبندی بخش ۱۱ |
| Q-05 | Dapper فقط برای Query؟ و مجاز بودن EF `AsNoTracking` برای Query ساده؟ | بله، بله |
| Q-06 | زبان کامنت‌ها: انگلیسی یا فارسی؟ | انگلیسی |
| Q-07 | مقدار پروژه (Single/Multi-tenant از اول)؟ | Multi-tenant از اول |
| Q-08 | نیاز به SPA مدیریت (React/Angular) در این پروژه یا فقط API؟ | فقط API |
| Q-09 | استقرار (Docker/IIS/Cloud) و SQL Server نسخه/Edition (برای FTS) | Docker + SQL 2022 |
| Q-10 | متن کامل Volume 2 را بدهید تا ردیف‌های STR تأیید شود (O-01) | |
| Q-11 | قانون فاکتور/مالیات ایران: خارج اسکوپ (O-04) | خارج از اسکوپ |
| Q-12 | اجازه نصب بسته‌ها: Dapper، JwtBearer، Redis، EF Tools در فاز مربوط؟ | بله، هر فاز با اطلاع |

## بخش ۱۳ — پرامپت اجرایی برای ایجنت (پس از تأیید)

```
You are building a CMS from scratch in C:\My Projects\CMS following CMS_Spec_v3.0.md.
Rules:
1. Work one phase at a time (section 11). Stop and report after each phase with build + test output.
2. Never install/upgrade NuGet packages or dotnet-ef without my explicit approval. Never delete files outside the phase scope.
3. Clean Architecture: Domain has no dependencies; Application depends on Domain only; Infrastructure implements Application interfaces; Api is the composition root.
4. CQRS: commands via EF Core + repository/UnitOfWork; queries via Dapper (parameterized SQL only).
5. sealed by default; abstract bases for entities/configurations; generic repository for aggregate roots only.
6. One-line comment above every public type and member. No dead code, no unused packages.
7. Each handler: >=1 success and >=1 failure test; add architecture tests before feature code.
8. Connection strings only from env/User Secrets. Fail fast on missing config.
9. Report honestly: show actual command output; do not claim success without running it.
```
