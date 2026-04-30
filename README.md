<div align="center">

# 📻 Radio — نظام إدارة البث الإذاعي

[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![WPF](https://img.shields.io/badge/WPF-Desktop-0078D7?logo=windows)](https://github.com/dotnet/wpf)
[![EF Core](https://img.shields.io/badge/EF_Core-10.0-68217A?logo=dotnet)](https://learn.microsoft.com/ef/core)
[![SQL Server](https://img.shields.io/badge/SQL_Server-CC2927?logo=microsoftsqlserver)](https://www.microsoft.com/sql-server)
[![License](https://img.shields.io/badge/License-خاص-orange)](./LICENSE)

**نظام مكتبي احترافي لإدارة دورة العمل الإذاعي اليومية — من الجدولة حتى النشر الرقمي والموقع الإلكتروني.**

</div>

---

## 📖 نظرة عامة

Radio هو تطبيق سطح مكتب مبني على **WPF** و **.NET 10**، مصمم خصيصاً لإدارة العمليات اليومية داخل محطة إذاعية. يغطي النظام دورة حياة الحلقة الكاملة: **جدولة → تنفيذ → نشر رقمي → نشر على الموقع**، مع إمكانية التراجع أو الإلغاء عند الحاجة.

يتميز المشروع بـ:

- 🎯 **واجهة عربية كاملة** مصممة خصيصاً لتجربة المستخدم العربي
- 🔐 **نظام صلاحيات دقيق** يتحكم في كل عملية داخل النظام
- 📋 **نمط Result Pattern** للتعامل مع الأخطاء المتوقعة دون استثناءات مكلفة
- 📝 **سجل تدقيق كامل** لكل تغيير على مستوى قاعدة البيانات
- 🗑️ **حذف منطقي** لجميع السجلات مع إمكانية الاسترجاع
- 🔄 **ميزة التراجع** عن التنفيذ أو النشر الخاطئ مع تسجيل السبب
- ❌ **إلغاء الحلقات** مع سبب إجباري يظهر في الواجهة

---

## 🏗️ المعمارية — ثلاث طبقات

```
┌──────────────────────────────────────────────────────────┐
│                        Radio                             │
│               واجهة المستخدم (WPF + MahApps)             │
│         النوافذ · الشاشات · الموارد البصرية · المحولات   │
├──────────────────────────────────────────────────────────┤
│                      DataAccess                          │
│     الخدمات · DTOs · التحقق · الصلاحيات · Result Pattern │
├──────────────────────────────────────────────────────────┤
│                       Domain                             │
│      الكيانات · DbContext · EF Core · Configurations     │
└──────────────────────────────────────────────────────────┘
```

الطبقات منفصلة تماماً — واجهة المستخدم لا تعرف شيئاً عن قاعدة البيانات، والخدمات لا تعرف شيئاً عن WPF.

---

## 🔄 دورة حياة الحلقة

```
   ┌──────────┐      ┌──────────┐      ┌──────────┐      ┌──────────────────┐
   │  مجدولة  │ ───→ │  منفّذة   │ ───→ │ منشورة   │ ───→ │ منشورة على الموقع │
   │ Planned  │      │ Executed  │      │ Published │      │ WebsitePublished  │
   └────┬─────┘      └────┬─────┘      └──────────┘      └──────────────────┘
        │                 │
        │     ↙ التراجع    │     ↙ التراجع
        │   (يحذف سجل      │   (يحذف سجل
        │    التنفيذ)      │    النشر)
        ↘                 ↙
   ┌──────────┐    
   │  ملغاة    │    
   │ Cancelled │    
   └──────────┘    
```

---

## 📦 الميزات

### 🎙️ إدارة المحتوى الإذاعي

| الوحدة | العمليات |
|--------|---------|
| **البرامج** | إضافة · تعديل · حذف منطقي · عرض |
| **الحلقات** | جدولة · تنفيذ · نشر رقمي · نشر موقع · تراجع · إلغاء |
| **الضيوف** | إضافة · تعديل · حذف منطقي · ربط بالحلقات |
| **المراسلين** | إضافة · تعديل · حذف منطقي |
| **التغطيات الميدانية** | إضافة · تعديل · حذف مع ربط بالمراسل والضيف |

### 👥 إدارة المستخدمين والصلاحيات

- إنشاء مستخدمين بأدوار وصلاحيات مخصصة
- تفعيل / تعطيل الحسابات
- **مصفوفة صلاحيات** كاملة: 12 صلاحية موزعة على 4 أدوار
- Admin يتجاوز جميع فحوصات الصلاحيات تلقائياً

### 📊 التقارير والتتبع

- عرض حلقات اليوم مع تفاصيل الضيوف
- عرض البرامج النشطة مع آخر الحلقات
- **سجل تدقيق كامل** (`AuditLogs`) لكل تغيير: ماذا تغير · متى · بواسطة مَن
- إحصاءات مباشرة: إجمالي الحلقات · المنشورة · المجدولة

### 🛡️ الحذف المنطقي

جميع الكيانات الأساسية تستخدم `IsActive`:
- السجلات "المحذوفة" تبقى في القاعدة ولا تُفقد
- **Global Query Filter** يخفيها تلقائياً من جميع الاستعلامات
- إمكانية الاسترجاع مستقبلاً

---

## 💡 نمط Result Pattern

المشروع يستخدم نمط `Result` / `Result<T>` بدلاً من الاستثناءات للأخطاء المتوقعة:

```csharp
// ✅ بدلاً من throw new KeyNotFoundException("غير موجود")
if (entity == null) return Result.Fail("الحلقة غير موجودة.");

// ✅ بدلاً من throw new UnauthorizedAccessException
var perm = session.EnsurePermission(AppPermissions.EpisodeExecute);
if (!perm.IsSuccess) return Result.Fail(perm.ErrorMessage!);

// ✅ استهلاك النتيجة في الواجهة
if (result.IsSuccess)
    MessageService.Current.ShowSuccess("تمت العملية بنجاح");
else
    MessageService.Current.ShowWarning(result.ErrorMessage);
```

**تم تحويله إلى Result Pattern:**
- جميع الـ Services (8 خدمات)
- `SecurityHelper` (EnsurePermission / EnsureRole)
- `ValidationPipeline` (جميع دوال التحقق)
- `AuthService.LoginAsync` → `Result<UserSession>`

---

## 🔐 الصلاحيات

| الصلاحية | الوصف | الوحدة |
|----------|------|--------|
| `USER_MANAGE` | إدارة المستخدمين | المستخدمين |
| `PROGRAM_MANAGE` | إدارة البرامج | البرامج |
| `EPISODE_MANAGE` | إدارة الحلقات | الحلقات |
| `EPISODE_EXECUTE` | تنفيذ الحلقات | الحلقات |
| `EPISODE_PUBLISH` | نشر رقمي | الحلقات |
| `EPISODE_WEB_PUBLISH` | نشر الموقع | الحلقات |
| `EPISODE_EDIT` | تعديل الحلقات | الحلقات |
| `EPISODE_DELETE` | حذف الحلقات | الحلقات |
| `EPISODE_REVERT` | تراجع / إلغاء | الحلقات |
| `GUEST_MANAGE` | إدارة الضيوف | الضيوف |
| `CORR_MANAGE` | إدارة المراسلين والتغطيات | التنسيق |
| `VIEW_REPORTS` | عرض التقارير | التقارير |

---

## 🗄️ قاعدة البيانات

### الكيانات الأساسية (15 جدولاً)

| الكيان | المفتاح | RowVersion | IsActive | Soft Delete |
|--------|---------|------------|----------|-------------|
| `EpisodeStatuses` | byte | — | — | — |
| `Permissions` | int | — | — | — |
| `Roles` | int | ✅ | ✅ | ✅ |
| `RolePermissions` | composite | — | — | — |
| `Users` | int | ✅ | ✅ | ✅ |
| `Programs` | int | ✅ | ✅ | ✅ |
| `Episodes` | int | ✅ | ✅ | ✅ |
| `EpisodeGuests` | int | ✅ | ✅ | ✅ |
| `Guests` | int | ✅ | ✅ | ✅ |
| `Correspondents` | int | ✅ | ✅ | ✅ |
| `CorrespondentCoverage` | int | ✅ | ✅ | ✅ |
| `ExecutionLogs` | int | ✅ | ✅ | — |
| `PublishingLogs` | int | ✅ | ✅ | — |
| `AuditLogs` | int | — | — | — |

### الفهارس المهمة

- `UQ_Users_Username` — يمنع تكرار اسم المستخدم
- `UQ_Programs_ProgramName` — يمنع تكرار اسم البرنامج
- `UQ_EpisodeGuests (EpisodeId, GuestId)` — يمنع تكرار الضيف في نفس الحلقة
- `IX_AuditLog_Table_Record (TableName, RecordId)` — استعلامات التدقيق السريعة
- جميع المفاتيح الخارجية مفهرسة تلقائياً

---

## 🛠️ التقنيات

| التقنية | الإصدار | الغرض |
|---------|---------|-------|
| .NET | 10.0 | المنصة الأساسية |
| WPF | — | واجهة المستخدم الرسومية |
| Entity Framework Core | 10.0.7 | ORM |
| SQL Server | — | قاعدة البيانات |
| MahApps.Metro | 2.4.11 | نوافذ وأنماط WPF |
| MaterialDesignThemes | 5.3.1 | مكونات وأيقونات |
| MaterialDesignColors | 5.3.1 | لوحات الألوان |
| BCrypt.Net-Next | — | تشفير كلمات المرور |
| Fody + PropertyChanged | — | INotifyPropertyChanged التلقائي |
| Microsoft.Extensions.Hosting | 10.0.7 | DI + Configuration |

---

## 🚀 التشغيل المحلي

### المتطلبات

- **Windows** (10/11)
- **.NET 10.0 SDK**
- **SQL Server** (LocalDB أو كامل)
- **Visual Studio** (اختياري، يمكن استخدام `dotnet CLI`)

### الخطوات

```bash
# 1. استنساخ المستودع
git clone https://github.com/dabasgaza/Radio.git
cd Radio

# 2. ضبط سلسلة الاتصال
# عدّل Radio/appsettings.json:
# "DefaultConnection": "Server=.;Database=BroadcastWorkflowDB;Trusted_Connection=True;TrustServerCertificate=True;"

# 3. تطبيق الترحيلات على قاعدة البيانات
dotnet ef database update --project Domain --startup-project Radio

# 4. بناء وتشغيل
dotnet build
dotnet run --project Radio
```

### بيانات الدخول الافتراضية (Seeding)

| المستخدم | كلمة المرور | الدور |
|----------|------------|-------|
| `admin` | `Admin@123` | Admin (جميع الصلاحيات) |

> للتسجيل الأول: `dotnet run --project Radio` — سيقوم `DbSeeder` تلقائياً بإنشاء البيانات الأساسية.

---

## 📁 هيكل المشروع

```
Radio/
├── Domain/                         # طبقة المجال
│   └── Models/
│       ├── BroadcastWorkflowDBContext.cs
│       ├── BaseEntity.cs           # الكيان الأساسي (IsActive, Audit, RowVersion)
│       ├── Episode.cs              # الحلقة — كيان دورة الحياة الرئيسي
│       ├── Program.cs / Guest.cs / Correspondent.cs
│       ├── ExecutionLog.cs / PublishingLog.cs
│       ├── User.cs / Role.cs / Permission.cs
│       ├── AuditLog.cs             # سجل التدقيق + عمود Reason
│       ├── EpisodeStatus.cs / EpisodeGuest.cs
│       ├── CorrespondentCoverage.cs / RolePermission.cs
│       ├── Configurations/         # EF Core Fluent API (12 ملف)
│       └── Migrations/             # ترحيلات EF Core
│
├── DataAccess/                     # طبقة التطبيق
│   ├── Common/
│   │   ├── Result.cs               # Result / Result<T> Pattern
│   │   ├── SecurityHelper.cs       # EnsurePermission / EnsureRole → Result
│   │   ├── UserSession.cs          # جلسة المستخدم الحالي
│   │   ├── AppPermissions.cs       # ثوابت الصلاحيات
│   │   └── ConcurrencyException.cs
│   ├── DTOs/                       # كائنات نقل البيانات (16 DTO)
│   ├── Data/
│   │   └── AuditInterceptor.cs     # SaveChangesInterceptor للتدقيق
│   ├── Seeding/
│   │   └── DbSeeder.cs             # البذور الأولية للتشغيل
│   ├── Services/                   # خدمات الأعمال
│   │   ├── EpisodeService.cs       # ✨ الأكثر تعقيداً — Workflow + تراجع + إلغاء
│   │   ├── ProgramService.cs
│   │   ├── GuestService.cs
│   │   ├── CorrespondentService.cs
│   │   ├── CoverageService.cs
│   │   ├── ExecutionService.cs
│   │   ├── PublishingService.cs
│   │   ├── AuthService.cs
│   │   ├── UserService.cs
│   │   ├── ReportsService.cs
│   │   └── Messaging/MessageService.cs
│   └── Validation/
│       └── ValidationPipeline.cs   # تحقق مركزي → Result
│
├── Radio/                          # طبقة العرض
│   ├── App.xaml / App.xaml.cs      # DI Setup + Resources
│   ├── MainWindow.xaml             # النافذة الرئيسية + القائمة
│   ├── Forms/
│   │   └── LoginWindow.xaml        # نافذة تسجيل الدخول
│   ├── Views/
│   │   ├── Programs/               # شاشات البرامج
│   │   ├── Episodes/               # ✨ شاشات الحلقات (الأكثر تعقيداً)
│   │   ├── Guests/                 # شاشات الضيوف
│   │   ├── Correspondents/         # المراسلين + التغطيات
│   │   ├── Users/                  # المستخدمين + مصفوفة الصلاحيات
│   │   ├── Reports/                # التقارير
│   │   └── Common/
│   │       ├── ConcurrencyDialog   # نافذة حل تعارض التزامن
│   │       └── ReasonInputDialog   # نافذة إدخال سبب التراجع/الإلغاء
│   ├── Converter/
│   │   ├── StringToVisibilityConverter.cs
│   │   ├── TimeSpanToStringConverter.cs
│   │   └── PermissionVisibilityConverter.cs
│   ├── Messaging/
│   │   └── WpfMessageService.cs    # تنفيذ WPF لإشعارات MessageService
│   ├── Common/
│   │   └── ValidationException.cs  # (ملغى — تم استبداله بـ Result)
│   └── Resources/                  # الأنماط، الألوان، القوالب
│
└── Radio.slnx                      # ملف الحل
```

---

## 🎨 تحسينات التصميم

تم تحسين شاشة الحلقات (`EpisodesView`) بشكل كبير:

- **قبل**: أزرار عمودية جانبية (9 أزرار) تجعل البطاقة طويلة حتى مع محتوى قليل
- **بعد**: شريط أفقي في الأسفل مع أزرار دائرية ملونة (FAB) مقسمة إلى 3 أقسام:

```
┌─────────────── سير العمل ───────────────┐ ┆ ┌──── تصحيحي ────┐ ┆ ┌── إدارة ──┐
│    ●▶ تنفيذ   ●☁ نشر    ●🌐 موقع        │ │ │ ●↩ تراجع  ●✕ إلغاء │ │ │ ✎ تعديل  🗑 حذف │
│   (أخضر)      (أخضر)     (أزرق)         │ │ │ (برتقالي)  (أحمر)  │ │ │ (شفاف)   (شفاف)  │
└────────────────────────────────────────┘ ┆ └────────────────┘ ┆ └───────────┘
```

---

## 📝 سجل التغييرات — آخر تحديث (2026-04-30)

### 🚀 تحويل كامل إلى Result Pattern
- جميع الخدمات (8) ترجع `Result` بدلاً من رمي استثناءات
- `SecurityHelper` → `Result` — إلغاء 28 catch block من الـ UI
- `ValidationPipeline` → `Result` — إلغاء `ValidationException`
- `AuthService.LoginAsync` → `Result<UserSession>`

### ✨ ميزة التراجع والإلغاء
- `RevertEpisodeStatusAsync` — تراجع عن تنفيذ/نشر مع Soft Delete للسجلات + سبب إجباري
- `CancelEpisodeAsync` — إلغاء حلقة مع سبب إجباري
- `UpdateCancellationReasonAsync` — تعديل سبب الإلغاء لاحقاً
- `ReasonInputDialog` — نافذة إدخال السبب
- شريط سبب الإلغاء في واجهة الحلقات (أحمر مع أيقونة Cancel)

### 🗄️ قاعدة البيانات
- إضافة `EPISODE_REVERT` صلاحية (ID=12) مع بذور `DbSeeder`
- إضافة عمود `Reason nvarchar(500)` لجدول `AuditLogs`

---

## 🤝 المساهمة

المشروع مخصص للاستخدام الداخلي. إذا كنت تطور داخلياً:

1. أنشئ فرعاً من `master`: `git checkout -b feature/اسم-الميزة`
2. نفّذ تغييراتك مع الالتزام برسائل commit واضحة
3. تأكد من نجاح البناء: `dotnet build`
4. ارفع الفرع واطلب `Pull Request`

---

## 📄 الترخيص

خاص — للاستخدام الداخلي.
