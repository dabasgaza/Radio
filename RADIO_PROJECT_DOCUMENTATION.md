"# 📻 مشروع إدارة البث الإذاعي — Broadcast Radio Workflow

**آخر تحديث:** 2026-05-03  
**النسخة:** 2.0  
**الهدف:** توثيق كامل لبنية المشروع، سير العمل، والاتفاقيات لأي نموذج ذكاء اصطناعي أو مطور جديد.

---

## 🗺️ فهرس المحتويات

1. [نظرة عامة](#1-نظرة-عامة)
2. [هيكل المشروع](#2-هيكل-المشروع)
3. [طبقات التطبيق](#3-طبقات-التطبيق)
4. [قاعدة البيانات — الكيانات والعلاقات](#4-قاعدة-البيانات--الكيانات-والعلاقات)
5. [طبقة البيانات (DTOs)](#5-طبقة-البيانات-dtos)
6. [قاعدة الـ Result Pattern](#6-قاعدة-الـ-result-pattern)
7. [الأدوار والصلاحيات](#7-الأدوار-والصلاحيات)
8. [نظام تسجيل الخدمات (DI)](#8-نظام-تسجيل-الخدمات-di)
9. [سير العمل التشغيلي للحلقات](#9-سير-العمل-التشغيلي-للحلقات)
10. [اتفاقيات واجهة المستخدم (WPF)](#10-اتفاقيات-واجهة-المستخدم-wpf)
11. [نظام التنقل (MainWindow)](#11-نظام-التنقل-mainwindow)
12. [نظام الإشعارات والرسائل](#12-نظام-الإشعارات-والرسائل)
13. [نظام التدقيق (Audit)](#13-نظام-التدقيق-audit)
14. [الأعمال المكتملة والمتبقية](#14-الأعمال-المكتملة-والمتبقية)

---

## 1. نظرة عامة

نظام WPF متكامل لإدارة محطات البث الإذاعي. يغطي دورة الحياة الكاملة للحلقة الإذاعية:
- **التخطيط** ← **التنفيذ** ← **النشر الرقمي** ← **النشر على الموقع**

### التقنيات المستخدمة

| التقنية | الغرض |
|:--------|:------|
| .NET 10 / C# 13 | لغة التطوير |
| WPF + MaterialDesignInXaml | واجهة المستخدم |
| EF Core 10 | الوصول إلى البيانات |
| SQL Server | قاعدة البيانات |
| MahApps.Metro | نوافذ الحوار |
| PropertyChanged.Fody | إشعارات التغيير |
| DialogHost | صناديق الحوار |

---

## 2. هيكل المشروع

```
📦 Radio.sln
├── 📂 Domain/                  # طبقة المجال (Domain Layer)
│   ├── 📂 Models/              # كيانات EF Core
│   ├── 📂 Models/Configurations/ # Fluent API Configurations
│   └── 📂 Migrations/          # EF Core Migrations
├── 📂 DataAccess/              # طبقة الوصول للبيانات
│   ├── 📂 Common/              # AppPermissions, Result, UserSession
│   ├── 📂 Data/                # AuditInterceptor
│   ├── 📂 DTOs/                # Data Transfer Objects
│   ├── 📂 Seeding/             # DbSeeder
│   ├── 📂 Services/            # تطبيقات الخدمات
│   │   └── 📂 Messaging/       # MessageService
│   └── 📂 Validation/          # ValidationPipeline
└── 📂 Radio/                   # طبقة العرض (WPF)
    ├── 📂 Forms/               # LoginWindow
    ├── 📂 Messaging/           # WpfMessageService, NotificationControl
    ├── 📂 Resources/           # الثيمات والموارد
    ├── 📂 Converter/           # محولات WPF
    └── 📂 Views/               # شاشات التطبيق
        ├── 📂 Common/          # ReasonInputDialog, ConcurrencyDialog
        ├── 📂 Employees/       # إدارة الموظفين
        ├── 📂 Episodes/        # الحلقات (النواة)
        ├── 📂 Guests/          # الضيوف
        ├── 📂 Correspondents/  # المراسلون والتغطيات
        ├── 📂 StaffRoles/      # المسميات الوظيفية
        ├── 📂 Programs/        # البرامج
        ├── 📂 Users/           # المستخدمين والصلاحيات
        ├── 📂 Reports/         # التقارير
        └── 📂 Home/            # لوحة التحكم
```

---

## 3. طبقات التطبيق

### 3.1 Domain (طبقة المجال)
- **لا تعتمد على أي طبقة أخرى**
- تحتوي على كيانات EF Core، التهيئات، التعدادات
- جميع الكيانات ترث من `BaseEntity`
- تتم إدارة `CreatedAt`, `UpdatedAt`, `IsActive`, `RowVersion` عبر الـ Base

### 3.2 DataAccess (طبقة الوصول للبيانات)
- **تعتمد فقط على Domain**
- تحتوي على DTOs, Services, DbSeeder, AuditInterceptor
- جميع الخدمات تطبق `Result Pattern`
- جميع الاستعلامات تحترم `IsActive` عبر Global Query Filter

### 3.3 Radio (طبقة العرض — WPF)
- **تعتمد على DataAccess**
- تحتوي على Views, Windows, Converters, Resources

---

## 4. قاعدة البيانات — الكيانات والعلاقات

### 4.1 الكيانات الأساسية

| الكيان | الجدول | الوصف |
|:-------|:-------|:------|
| `BaseEntity` | — | `IsActive`, `CreatedAt`, `UpdatedAt`, `RowVersion` |
| `User` | `Users` | مستخدمي النظام |
| `Role` | `Roles` | الأدوار (Admin, Producer, WebPublisher, Reporter) |
| `Permission` | `Permissions` | الصلاحيات (USER_MANAGE, EPISODE_MANAGE, ...) |
| `RolePermission` | `RolePermissions` | ربط الدور بالصلاحية |
| `Program` | `Programs` | البرامج الإذاعية |
| `Episode` | `Episodes` | الحلقات |
| `EpisodeStatus` | `EpisodeStatuses` | حالات الحلقة (Planned=0, Executed=1, Published=2, WebsitePublished=3, Cancelled=4) |
| `Guest` | `Guests` | الضيوف |
| `EpisodeGuest` | `EpisodeGuests` | ربط الحلقة بالضيف مع Topic, HostingTime, ClipStatus |
| `Correspondent` | `Correspondents` | المراسلون الميدانيون |
| `EpisodeCorrespondent` | `EpisodeCorrespondents` | ربط الحلقة بالمراسل |
| `Employee` | `Employees` | الموظفون (مذيع، مخرج، فني صوت...) |
| `StaffRole` | `StaffRoles` | المسميات الوظيفية |
| `EpisodeEmployee` | `EpisodeEmployees` | ربط الحلقة بالموظف |
| `SocialMediaPlatform` | `SocialMediaPlatforms` | منصات النشر (Facebook, TikTok, YouTube...) |
| `SocialMediaPublishingLog` | `SocialMediaPublishingLogs` | سجل نشر مقطع ضيف |
| `SocialMediaPublishingLogPlatform` | `SocialMediaPublishingLogPlatforms` | ربط السجل بالمنصة والرابط |
| `WebsitePublishingLog` | `WebsitePublishingLogs` | سجل نشر الحلقة على الموقع |
| `ExecutionLog` | `ExecutionLogs` | سجل توثيق التنفيذ |
| `CorrespondentCoverage` | `CorrespondentCoverages` | التغطيات الميدانية |
| `AuditLog` | `AuditLogs` | سجل التدقيق |

### 4.2 علاقات الكيانات الرئيسية

```
Role (1) ── (N) RolePermission (N) ── (1) Permission
Role (1) ── (N) User
Program (1) ── (N) Episode
Episode (1) ── (N) EpisodeGuest (N) ── (1) Guest
Episode (1) ── (N) EpisodeCorrespondent (N) ── (1) Correspondent
Episode (1) ── (N) EpisodeEmployee (N) ── (1) Employee (N) ── (1) StaffRole
Episode (1) ── (N) WebsitePublishingLog
Episode (1) ── (N) ExecutionLog
EpisodeGuest (1) ── (N) SocialMediaPublishingLog (N) ── (N) SocialMediaPublishingLogPlatform (N) ── (1) SocialMediaPlatform
```

### 4.3 قاعدة: StaffRoleId في Employee وليس EpisodeEmployee

> `StaffRoleId` موجود في جدول `Employee` كمفتاح خارجي لـ `StaffRole`.  
> دور الموظف هو خاصية ثابتة له كشخص، وليس مرتبطة بالحلقة.  
> عند إضافة موظف لحلقة، يتم الاستعلام عن دوره من `Employee`.

### 4.4 الحالات التشغيلية للحلقة

| StatusId | StatusName | الانتقال المسموح |
|:--------:|:-----------|:-----------------|
| 0 | `Planned` | → Executed, Cancelled |
| 1 | `Executed` | → Published, WebsitePublished, ← Planned |
| 2 | `Published` | → WebsitePublished ← Executed |
| 3 | `WebsitePublished` | ← Published |
| 4 | `Cancelled` | لا انتقال |

### 4.5 التعدادات العامة

```csharp
// Domain/Models/Enums.cs
public enum MediaType { Audio = 0, Video = 1, Both = 2 }
public enum GuestClipStatus { Pending = 0, Published = 1, Skipped = 2 }
```

---

## 5. طبقة البيانات (DTOs)

جميع الـ DTOs هي `record` باستثناء `ActiveEpisodeDto` و `PublishingLogDto`.

| DTO | الاستخدام |
|:----|:----------|
| `ActiveEpisodeDto` | العرض الكامل للحلقة (مع قوائم الضيوف والمراسلين والموظفين) |
| `ActiveProgramDto` | عرض البرامج |
| `EpisodeGuestDto` | ضيف داخل حلقة (EpisodeGuestId, GuestId, Name, Topic, HostingTime) |
| `EpisodeCorrespondentDto` | مراسل داخل حلقة (Id, CorrespondentId, FullName, Topic, HostingTime) |
| `EpisodeEmployeeDto` | موظف داخل حلقة (Id, EmployeeId) |
| `GuestDisplayItem` | عنصر عرض ضيف (للواجهة) |
| `EmployeeDto` | موظف (EmployeeId, FullName, StaffRoleId, StaffRoleName, Notes) |
| `StaffRoleDto` | مسمى وظيفي (StaffRoleId, RoleName) |
| `UserDto` | مستخدم (لمحة) |
| `SocialMediaPlatformDto` | منصة نشر (PlatformId, Name, Icon) |
| `SocialMediaPublishingLogDto` | سجل نشر (LogId, EpisodeGuestId, ClipTitle, Duration, Platforms[]) |
| `PlatformPublishDto` | رابط على منصة (PlatformId, PlatformName, Url) |
| `WebsitePublishingLogDto` | سجل نشر موقع (Id, EpisodeId, MediaType, Title, Notes, PublishedAt) |
| `PublishingLogDto` | سجل نشر قديم (EpisodeId, YouTubeUrl, FacebookUrl, ...) |
| `ExecutionLogDto` | سجل تنفيذ |
| `CoverageDto` | تغطية ميدانية |
| `CorrespondentCoverageDto` | ربط مراسل بتغطية |
| `PermissionViewModel` | عرض الصلاحية مع حالة التحديد |
| `RoleDto` | دور (RoleId, RoleName, Description) |

---

## 6. قاعدة الـ Result Pattern

جميع دوال الخدمات يجب أن تعيد `Result` أو `Result<T>`.

```csharp
// DataAccess/Common/Result.cs
public class Result
{
    public bool IsSuccess { get; }
    public string? ErrorMessage { get; }

    public static Result Success() => new(true, null);
    public static Result Fail(string errorMessage) => new(false, errorMessage);
}

public class Result<T> : Result
{
    public T? Value { get; }
    public static Result<T> Success(T value) => new(true, null, value);
    public static new Result<T> Fail(string errorMessage) => new(false, errorMessage, default);
}
```

### نمط الاستخدام في الخدمات:
```csharp
var permCheck = session.EnsurePermission(AppPermissions.EpisodePublish);
if (!permCheck.IsSuccess) return Result.Fail(permCheck.ErrorMessage!);
// ... منطق العمل ...
return Result.Success();
```

---

## 7. الأدوار والصلاحيات

### 7.1 الأدوار الأساسية (من DbSeeder)

| الدور | الوصف |
|:------|:------|
| `Admin` | مدير النظام — كل الصلاحيات |
| `Producer` | منتج البرامج — إدارة البرامج، الحلقات، الضيوف، المراسلين، طاقم العمل |
| `WebPublisher` | ناشر الموقع — نشر على الموقع فقط |
| `Reporter` | مراسل — عرض التقارير وإدارة التنسيق |

### 7.2 الصلاحيات (ثوابت في AppPermissions.cs)

```csharp
UserManage        = "USER_MANAGE"
ProgramManage     = "PROGRAM_MANAGE"
EpisodeManage     = "EPISODE_MANAGE"
EpisodeExecute    = "EPISODE_EXECUTE"
EpisodePublish    = "EPISODE_PUBLISH"
EpisodeWebPublish = "EPISODE_WEB_PUBLISH"
EpisodeEdit       = "EPISODE_EDIT"
EpisodeDelete     = "EPISODE_DELETE"
EpisodeRevert     = "EPISODE_REVERT"
GuestManage       = "GUEST_MANAGE"
CoordinationManage= "CORR_MANAGE"
StaffManage       = "STAFF_MANAGE"
ViewReports       = "VIEW_REPORTS"
```

### 7.3 توزيع الصلاحيات على الأدوار

| الصلاحية | Admin | Producer | WebPublisher | Reporter |
|:--------|:-----:|:--------:|:------------:|:--------:|
| USER_MANAGE | ✅ | ❌ | ❌ | ❌ |
| PROGRAM_MANAGE | ✅ | ✅ | ❌ | ❌ |
| EPISODE_MANAGE | ✅ | ✅ | ❌ | ❌ |
| EPISODE_EXECUTE | ✅ | ✅ | ❌ | ❌ |
| EPISODE_PUBLISH | ✅ | ✅ | ❌ | ❌ |
| EPISODE_WEB_PUBLISH | ✅ | ❌ | ✅ | ❌ |
| EPISODE_EDIT | ✅ | ✅ | ❌ | ❌ |
| EPISODE_DELETE | ✅ | ✅ | ❌ | ❌ |
| EPISODE_REVERT | ✅ | ❌ | ❌ | ❌ |
| GUEST_MANAGE | ✅ | ✅ | ❌ | ❌ |
| CORR_MANAGE | ✅ | ✅ | ❌ | ✅ |
| STAFF_MANAGE | ✅ | ✅ | ❌ | ❌ |
| VIEW_REPORTS | ✅ | ✅ | ❌ | ✅ |

---

## 8. نظام تسجيل الخدمات (DI)

### التهيئة في `App.xaml.cs` ضمن `new HostApplicationBuilder()`

```csharp
// DB
builder.Services.AddDbContextFactory<BroadcastWorkflowDBContext>((sp, options) =>
{
    var interceptor = sp.GetRequiredService<AuditInterceptor>();
    options.UseSqlServer(connectionString).AddInterceptors(interceptor);
});

// Singleton
builder.Services.AddSingleton<CurrentSessionProvider>();
builder.Services.AddSingleton<AuditInterceptor>();
builder.Services.AddSingleton<IMessageService, WpfMessageService>();

// Transient (Services)
builder.Services.AddTransient<IAuthService, AuthService>();
builder.Services.AddTransient<IGuestService, GuestService>();
builder.Services.AddTransient<ICorrespondentService, CorrespondentService>();
builder.Services.AddTransient<IEpisodeService, EpisodeService>();
builder.Services.AddTransient<IProgramService, ProgramService>();
builder.Services.AddTransient<IExecutionService, ExecutionService>();
builder.Services.AddTransient<IPublishingService, PublishingService>();
builder.Services.AddTransient<IReportsService, ReportsService>();
builder.Services.AddTransient<IUserService, UserService>();
builder.Services.AddTransient<ICoverageService, CoverageService>();
builder.Services.AddTransient<IEmployeeService, EmployeeService>();
builder.Services.AddTransient<LoginWindow>();
```

### ⚠️ All Services use `IDbContextFactory` — NOT direct `DbContext` injection.

```csharp
public class EpisodeService(IDbContextFactory<BroadcastWorkflowDBContext> contextFactory) : IEpisodeService
```

---

## 9. سير العمل التشغيلي للحلقات

### 9.1 إنشاء حلقة جديدة (Planned)
**المسؤول:** قسم التنسيق  
**الشاشة:** `EpisodeFormControl` (يُفتح عبر `DialogHost`)

1. اختيار البرنامج، كتابة العنوان والتاريخ والملاحظات
2. إضافة ضيوف (ComboBox → DataGrid) مع Topic و HostingTime
3. إضافة مراسلين ميدانيين مع Topic و HostingTime
4. إضافة طاقم التنفيذ (مخرج، مذيع، فني صوت)
5. عند الحفظ → `CreateEpisodeAsync` يُنشئ:
   - `Episode` (StatusId = 0)
   - `EpisodeGuest` لكل ضيف
   - `EpisodeCorrespondent` لكل مراسل
   - `EpisodeEmployee` لكل موظف

### 9.2 تسجيل التنفيذ (Executed)
**المسؤول:** قسم الإنتاج  
**الشاشة:** `ExecutionLogDialog`

1. يضغط زر "تسجيل التنفيذ" على حلقة بحالة `Planned`
2. عند التأكيد: `StatusId = 1` + سجل في `ExecutionLogs`

### 9.3 النشر الاجتماعي (Published)
**المسؤول:** قسم النشر الرقمي  
**الشاشة:** `PublishingLogDialog` (قيد التطوير → `SocialPublishingLogDialog`)

1. يضغط زر "النشر الرقمي" على حلقة بحالة `Executed`
2. يعرض قائمة ضيوف الحلقة
3. لكل ضيف: اختيار MediaType + ClipTitle + ClipDuration + اختيار المنصات والروابط
4. عند الحفظ: `SocialMediaPublishingLog` لكل ضيف + `SocialMediaPublishingLogPlatform` لكل منصة
5. `StatusId = 2`

### 9.4 النشر على الموقع (WebsitePublished)
**المسؤول:** قسم النشر الرقمي  
**الشاشة:** زر تبديل مباشر في `EpisodesView`

1. ينشئ `WebsitePublishingLog` ويحدّث StatusId=3 أو يعود لـPublished

### 9.5 التراجع (Revert)
- من Executed → Planned
- من Published → Executed
- من WebsitePublished → Published
- **يتطلب إدخال سبب** عبر `ReasonInputDialog`
- يتم تحديث `CancellationReason` في `Episode`

### 9.6 الإلغاء (Cancel)
- من Planned أو Executed
- **يتطلب إدخال سبب**
- StatusId = 4 (Cancelled)

---

## 10. اتفاقيات واجهة المستخدم (WPF)

### 10.1 Styles المعتمدة (من Resources)

| العنصر | الـ Style |
|:-------|:----------|
| الحقول النصية | `{StaticResource Input.Text}` |
| القوائم المنسدلة | `{StaticResource Input.ComboBox}` |
| منتقي التاريخ | `{StaticResource Input.DatePicker}` |
| زر رئيسي | `{StaticResource Btn.Primary}` |
| زر إلغاء | `{StaticResource Btn.Cancel}` |
| زر حذف (أيقونة) | `{StaticResource Btn.Icon.Delete}` |
| زر تعديل (أيقونة) | `{StaticResource Btn.Icon.Edit}` |
| البطاقات | `{StaticResource BroadcastCard}` / `{StaticResource Card.Form}` |
| الجداول | `{StaticResource DataGrid.Main}` |
| رأس النافذة | `{StaticResource Zone.Header.Primary}` / `{StaticResource Zone.Header.Neutral}` |
| تذييل النافذة | `{StaticResource Window.Footer}` |
| ظل الحوار | `{StaticResource Shadow.Dialog}` |
| بطاقة المعلومات | `{StaticResource Card.Info}` |
| عنوان كبير | `{StaticResource Type.TitleLarge.OnColor}` |
| عنوان وسيط | `{StaticResource Type.TitleMedium.OnColor}` |

### 10.2 نمط نافذة الحوار (Dialog Pattern)

```csharp
// فتح نموذج داخل DialogHost
var view = new EpisodeFormControl(epService, progService, ..., session);
var result = await DialogHost.Show(view, "RootDialog");
if (result is true) await LoadDataAsync();

// إغلاق من داخل النموذج
DialogHost.Close("RootDialog", true);   // حفظ ناجح
DialogHost.Close("RootDialog", false);  // إلغاء
```

### 10.3 نمط MetroWindow (للمستقلات)

```csharp
// فتح نافذة منفصلة (ليست داخل DialogHost)
var dialog = new ExecutionLogDialog(ep.EpisodeId, execService, session);
if (dialog.ShowDialog() == true) await LoadDataAsync();
```

### 10.4 ⚠️ قاعدة حاسمة: لا تستخدم MessageBox.Show أبداً

```csharp
// ✅ الصحيح:
MessageService.Current.ShowWarning("رسالة تحذير");
MessageService.Current.ShowError("رسالة خطأ");
MessageService.Current.ShowSuccess("تم بنجاح");

// ❌ الممنوع:
MessageBox.Show("رسالة", "عنوان", MessageBoxButton.OK, MessageBoxImage.Warning);
```

### 10.5 تنسيق الوقت في DataGrid

عرض `HostingTime` في الجدول:
```csharp
public string HostingTimeDisplay => HostingTime.HasValue
    ? HostingTime.Value.ToString(@"hh\:mm")
    : "—";
```

---

## 11. نظام التنقل (MainWindow)

### 11.1 التبويبات الموجودة (بالترتيب)

| Tag | المحتوى | الصلاحية المطلوبة |
|:----|:--------|:-----------------:|
| `Home` | لوحة التحكم | متاح للجميع |
| `Programs` | إدارة البرامج | PROGRAM_MANAGE |
| `Episodes` | سجل الحلقات | متاح للجميع |
| `Guests` | إدارة الضيوف | GUEST_MANAGE |
| `Correspondents` | المراسلون | CORR_MANAGE |
| `Coverage` | التغطيات | CORR_MANAGE |
| `Reports` | التقارير | VIEW_REPORTS |
| `Users` | إدارة المستخدمين | USER_MANAGE |
| `Employees` | طاقم العمل | STAFF_MANAGE |
| `StaffRoles` | المسميات الوظيفية | STAFF_MANAGE |
| `Permissions` | الصلاحيات | USER_MANAGE |

### 11.2 قاعدة إضافة تبويب جديد

```
1. MainWindow.xaml    → إضافة RadioButton بـ Tag و Style=HubTabItem
2. MainWindow.xaml.cs → إضافة case في LoadView() مع حقن الخدمات
3. MainWindow.xaml.cs → إضافة سطر في ApplyPermissionSecurity()
4. App.xaml.cs        → تسجيل الخدمة في DI Container
5. AppPermissions.cs  → إضافة ثابت الصلاحية
```

### 11.3 نمط LoadView

```csharp
private void LoadView(string viewName)
{
    MainContentArea.Content = null;
    var userService = _serviceProvider.GetRequiredService<IUserService>();

    try
    {
        switch (viewName)
        {
            case "Episodes":
                var epService = _serviceProvider.GetRequiredService<IEpisodeService>();
                var empService = _serviceProvider.GetRequiredService<IEmployeeService>();
                NavigateTo(new EpisodesView(epService, ..., empService));
                break;
            // ...
        }
    }
    catch (Exception ex)
    {
        MainContentArea.Content = new TextBlock
        {
            Text = $"خطأ في تحميل الشاشة: {ex.Message}",
            Foreground = Brushes.Red
        };
    }
}
```

---

## 12. نظام الإشعارات والرسائل

### 12.1 تهيئة MessageService

في `App.OnStartup()`:
```csharp
MessageService.Initialize(AppHost.Services.GetRequiredService<IMessageService>());
```

### 12.2 الاستخدام

```csharp
using DataAccess.Services.Messaging;

MessageService.Current.ShowInfo("معلومات");
MessageService.Current.ShowSuccess("نجاح");
MessageService.Current.ShowWarning("تحذير");
MessageService.Current.ShowError("خطأ");

// تأكيد (Yes/No)
bool confirmed = await MessageService.Current.ShowConfirmationAsync("رسالة", "عنوان");
```

### 12.3 الواجهة والتنفيذ

- `IMessageService` في `DataAccess/Services/Messaging/MessageService.cs`
- `WpfMessageService` في `Radio/Messaging/WpfMessageService.cs` (تطبيق WPF باستخدام Snackbar)
- `NotificationControl` في `Radio/Messaging/` (تحكم الإشعارات)

### 12.4 تسجيل NotificationHost

```csharp
// في MainWindow.xaml.cs
Loaded += (_, _) => NotificationManager.RegisterHost(NotificationHost);
```

---

## 13. نظام التدقيق (Audit)

- يتم عبر `AuditInterceptor` (EF Core Interceptor)
- يُسجّل التغييرات على الكيانات في جدول `AuditLogs`
- يشمل: `EntityName`, `EntityId`, `OldValues`, `NewValues`, `ChangedByUserId`, `ChangedAt`

---

## 14. الأعمال المكتملة والمتبقية

### ✅ مكتمل

- [x] Domain Models — جميع الكيانات (Episode, Guest, Employee, StaffRole, SocialMedia...)
- [x] Enums — MediaType, GuestClipStatus, EpisodeStatus
- [x] EF Core Configurations + Migrations (4 migrations)
- [x] DbSeeder — Seed EpisodeStatuses, Permissions, Roles, RolePermissions, Admin User, SocialMediaPlatforms, StaffRoles
- [x] AuthService — تسجيل الدخول (SHA256 مؤقت)
- [x] EmployeeService — CRUD للموظفين والأدوار الوظيفية
- [x] EpisodeService — CRUD للحلقات مع دعم تغيير الحالة
- [x] ExecutionService — تسجيل التنفيذ
- [x] PublishingService — النشر على الموقع (WebsitePublishingLog)
- [x] GuestService + CorrespondentService + CoverageService + ProgramService + UserService
- [x] EmployeesView + EmployeeFormDialog
- [x] StaffRolesView + StaffRoleFormDialog
- [x] ربط EpisodeFormControl بـ IEmployeeService (بدلاً من IUserService)
- [x] استبدال جميع MessageBox.Show بـ MessageService.Current
- [x] تطبيق `ApplyPermissionSecurity()` في MainWindow
- [x] Navigation Cases لـ Employees و StaffRoles

### 🔧 قيد التطوير

- [ ] **تطوير `SocialPublishingLogDialog`** — النشر الاجتماعي لكل ضيف مع اختيار المنصات والروابط
- [ ] إضافة دوال `LogSocialPublishingAsync` في `IPublishingService` / `PublishingService`

### 📝 ملاحظات للمطورين المستقبليين

1. **خدمات EF Core**: جميع الخدمات تستخدم `IDbContextFactory` لإنشاء `DbContext` جديد لكل عملية
2. **Result Pattern**: إلزامي لجميع دوال الخدمات
3. **Global Query Filter**: `IsActive = true` مفعل على جميع الكيانات — لا حذف حقيقي
4. **الصلاحيات**: التحقق الداخلي عبر `session.EnsurePermission()` داخل الخدمات
5. **WPF**: جميع النوافذ المستقلة ترث `MetroWindow`، والنماذج داخل `DialogHost` هي `UserControl`
6. **MaterialDesign**: الثيم يأتي عبر `BundledTheme` في Resources ويعتمد على `MaterialDesignColors`
7. **التعريب**: جميع نصوص الواجهة بالعربية، التواريخ بالتنسيق dd-MM-yyyy

---

> **⚠️ تنبيه**: هذا التوثيق يُقرأ بواسطة نماذج الذكاء الاصطناعي. يرجى التأكد من تحديثه عند إجراء تغييرات جوهرية على البنية.
"