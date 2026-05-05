# 📡 دليل تطوير نظام إدارة البث الإذاعي — Radio Broadcast Management System
**الإصدار:** 5.2 | **آخر تحديث:** مايو 2026 | **Build Status:** ✅ 0 Errors

> هذا الملف هو **المصدر الوحيد للحقيقة** لأي مطور أو نموذج ذكاء اصطناعي يعمل على المشروع.
> اقرأه كاملاً قبل أي تعديل. يمثل هذا الملف دمجاً لجميع الخطط والسجلات السابقة.

---

## الفهرس

1. [نظرة عامة عن المشروع](#1-نظرة-عامة-عن-المشروع)
2. [بنية المشروع](#2-بنية-المشروع)
3. [قواعد معمارية أساسية](#3-قواعد-معمارية-أساسية)
4. [الكيانات وقاعدة البيانات](#4-الكيانات-وقاعدة-البيانات)
5. [سير عمل الحلقات (Episode Workflow)](#5-سير-عمل-الحلقات-episode-workflow)
6. [طبقة البيانات (DTOs)](#6-طبقة-البيانات-dtos)
7. [الخدمات (Services)](#7-الخدمات-services)
8. [معايير واجهة المستخدم (UI Standards)](#8-معايير-واجهة-المستخدم-ui-standards)
9. [نظام التنقل (MainWindow)](#9-نظام-التنقل-mainwindow)
10. [نظام الصلاحيات](#10-نظام-الصلاحيات)
11. [قائمة المهام المتبقية](#11-قائمة-المهام-المتبقية)
12. [أوامر البناء](#12-أوامر-البناء)
13. [الأخطاء الشائعة](#13-الأخطاء-الشائعة)
14. [مسرد المصطلحات](#14-مسرد-المصطلحات)

---

## 1. نظرة عامة عن المشروع

تطبيق سطح مكتب **WPF** (.NET 10, C#) لإدارة سير عمل البث الإذاعي:
- جدولة الحلقات مع الضيوف والمراسلين الميدانيين وطاقم الإنتاج
- تتبع دورة حياة الحلقة: مجدولة → منفذة → منشورة → منشورة على الموقع
- نشر المقاطع على منصات التواصل الاجتماعي والموقع الإلكتروني
- نظام صلاحيات قائم على الأدوار مع تحكم كامل بالوصول

### تقنيات المشروع
| الطبقة | التقنية |
|:---|:---|
| واجهة المستخدم | WPF (.NET 10) |
| أدوات الواجهة | MaterialDesignInXamlToolkit + MahApps.Metro |
| ORM | Entity Framework Core 10 |
| قاعدة البيانات | SQL Server / LocalDB |
| DI Container | Microsoft.Extensions.DependencyInjection |
| النمط | Service Layer + Result Pattern |
| التدقيق (Audit) | AuditInterceptor (تلقائي) |
| إدارة الأيقونات | MaterialDesign PackIcons |

---

## 2. بنية المشروع

`
Radio.slnx
├── Domain/                           — كيانات EF Core + DbContext + Migrations
│   ├── Models/
│   │   ├── BaseEntity.cs             — الفئة الأساسية (IsActive, CreatedAt, ...)
│   │   ├── Configurations/          — Fluent API configurations
│   │   ├── Enums.cs                  — MediaType, GuestClipStatus
│   │   ├── Episode.cs, Guest.cs, Program.cs, ...
│   │   ├── Employee.cs, StaffRole.cs, EpisodeEmployee.cs
│   │   ├── EpisodeCorrespondent.cs, Correspondent.cs
│   │   ├── SocialMediaPlatform.cs, SocialMediaPublishingLog.cs
│   │   ├── SocialMediaPublishingLogPlatform.cs, WebsitePublishingLog.cs
│   │   └── BroadcastWorkflowDBContext.cs
│   └── Migrations/
├── DataAccess/                       — خدمات + DTOs + صلاحيات + تدقيق
│   ├── Common/
│   │   ├── Result.cs                 — Result<T> Pattern
│   │   ├── AppPermissions.cs         — ثوابت الصلاحيات
│   │   ├── UserSession.cs            — معلومات المستخدم + صلاحياته
│   │   └── EpisodeStatus.cs          — ثوابت حالة الحلقة
│   ├── Data/
│   │   └── AuditInterceptor.cs       — تدقيق تلقائي
│   ├── DTOs/
│   │   ├── ActiveEpisodeDto.cs       — عرض الحلقات
│   │   ├── EpisodeGuestDto.cs, EpisodeCorrespondentDto.cs
│   │   ├── EmployeeDto.cs, StaffRoleDto.cs, SocialMediaPlatformDto.cs
│   │   ├── SocialMediaPublishingLogDto.cs, PlatformPublishDto.cs
│   │   └── WebsitePublishingLogDto.cs
│   ├── Services/
│   │   ├── EpisodeService.cs         — IEpisodeService
│   │   ├── PublishingService.cs      — IPublishingService
│   │   ├── PlatformService.cs        — IPlatformService 🆕
│   │   ├── EmployeeService.cs        — IEmployeeService
│   │   ├── GuestService.cs, CorrespondentService.cs
│   │   ├── CoverageService.cs, ExecutionService.cs
│   │   ├── ProgramService.cs, AuthService.cs
│   │   └── UserService.cs, ReportsService.cs
│   ├── Validation/
│   │   └── ValidationPipeline.cs
│   └── Seeding/
│       └── DbSeeder.cs               — بيانات أولية
└── Radio/                             — WPF UI
    ├── App.xaml / App.xaml.cs         — بدء التشغيل + DI
    ├── MainWindow.xaml / .cs          — التنقل الرئيسي
    ├── Views/
    │   ├── Episodes/                  — EpisodesView, EpisodeFormControl
    │   ├── Publishing/               — PublishingLogDialog, WebsitePublishDialog
    │   ├── Common/                    — ReasonInputDialog
    │   ├── Admin/ 🆕                 — SocialPlatformsView, PlatformFormDialog
    │   ├── Home/, Programs/, Guests/, ...
    │   └── ...
    ├── Forms/
    │   └── LoginWindow.xaml
    ├── Resources/                     — Styles, Themes
    └── Converters/                    — محولات القيم
`

---

## 3. قواعد معمارية أساسية (Non-Negotiable)

| القاعدة | الشرح |
|:---|:---|
| **Result Pattern** | كل خدمة ترجع Result أو Result<T> — لا throws للbusiness errors |
| **BaseEntity** | كل الكيانات (عدا جداول التعداد) ترث من BaseEntity |
| **No Hard Delete** | context.Remove() ممنوع — استخدم IsActive = false |
| **Global Query Filters** | مفعّلة تلقائياً على IsActive في DbContext |
| **AuditInterceptor** | يعالج CreatedAt, UpdatedAt, CreatedByUserId, UpdatedByUserId تلقائياً |
| **No InverseProperty on User** | تجنب ICollection<> على كيان User لتقليل التعقيد |
| **IDbContextFactory** | أنشئ DbContext جديد لكل عملية — لا حقن مباشر للـ DbContext |
| **StaffRoleId على Employee** | دور الموظف خاصية ثابتة كشخص — ليس على EpisodeEmployee |
| **Primary Constructor** | الخدمات تستخدم (IDbContextFactory<...> ctx) — اختيارياً مع : this(ctx) |
| **Collection Sync** | Id == 0 → INSERT, Id != 0 → UPDATE, غير موجود → IsActive = false |
| **Dialog Pattern** | DialogHost.Show(UserControl, "RootDialog") ← إرجاع النتيجة عبر Close() |
| **View → Service فقط** | الـ View تحقن الخدمة — لا تستخدم 
ew ServiceClass() |
| **UI عبر code-behind** | تعيين ItemsSource في code-behind، وليس في XAML |

---

## 4. الكيانات وقاعدة البيانات

### جميع الكيانات

| الكيان | الجدول | المفتاح | يرث من |
|:---|:---|:---|:---|
| Episode | Episodes | EpisodeId | BaseEntity |
| Program | Programs | ProgramId | BaseEntity |
| Guest | Guests | GuestId | BaseEntity |
| EpisodeGuest | EpisodeGuests | EpisodeGuestId | BaseEntity |
| Correspondent | Correspondents | CorrespondentId | BaseEntity |
| EpisodeCorrespondent | EpisodeCorrespondents | Id | BaseEntity |
| Employee | Employees | EmployeeId | BaseEntity |
| StaffRole | StaffRoles | StaffRoleId | BaseEntity |
| EpisodeEmployee | EpisodeEmployees | EpisodeEmployeeId | BaseEntity |
| SocialMediaPlatform | SocialMediaPlatforms | SocialMediaPlatformId | BaseEntity |
| SocialMediaPublishingLog | SocialMediaPublishingLogs | SocialMediaPublishingLogId | BaseEntity |
| SocialMediaPublishingLogPlatform | SocialMediaPublishingLogPlatforms | Id | BaseEntity |
| WebsitePublishingLog | WebsitePublishingLogs | WebsitePublishingLogId | BaseEntity |
| User | Users | UserId | BaseEntity |
| ExecutionLog | ExecutionLogs | ExecutionLogId | BaseEntity |
| AuditLog | AuditLogs | AuditLogId | لا (جدول تدقيق خاص) |

### مخطط العلاقات

`
Episode (1) ──── (N) EpisodeGuest (N) ───── (1) Guest
   │                                      └─ (N) SocialMediaPublishingLog
   │                                              └─ (N) SocialMediaPublishingLogPlatform (N) ─ (1) SocialMediaPlatform
   │
   ├── (N) EpisodeCorrespondent (N) ────── (1) Correspondent
   │
   ├── (N) EpisodeEmployee (N) ─────────── (1) Employee (N) ── (1) StaffRole
   │
   ├── (N) WebsitePublishingLog
   │
   └── (N) ExecutionLog
`

### حالات الحلقة (EpisodeStatus)

| StatusId | الاسم | العرض | الانتقال المسموح |
|:---:|:---|:---|:---|
| 0 | Planned | مجدولة | ← Executed أو Cancelled |
| 1 | Executed | منفذة | ← Published أو Planned (تراجع) |
| 2 | Published | منشورة | ← WebsitePublished أو Executed (تراجع) |
| 3 | WebsitePublished | منشورة على الموقع | ← Published (تراجع) |
| 4 | Cancelled | ملغاة | لا انتقال |

### MediaType (للنشر)

| القيمة | الاسم | الوصف |
|:---:|:---|:---|
| 0 | Audio | مقطع صوتي |
| 1 | Video | مقطع فيديو |
| 2 | Both | صوت وفيديو معاً |

### GuestClipStatus (للمقاطع)

| القيمة | الاسم | الوصف |
|:---:|:---|:---|
| 0 | Pending | قيد الانتظار |
| 1 | Ready | جاهز للنشر |
| 2 | Published | منشور |

---

## 5. سير عمل الحلقات (Episode Workflow)

### المرحلة 1: إنشاء حلقة جديدة (Planned)
**المسؤول:** قسم التنسيق
1. فتح EpisodeFormControl ← زر إضافة حلقة
2. تعبئة: البرنامج، التاريخ (DatePicker)، الوقت (TimePicker)، العنوان، الملاحظات
3. تبويب الضيوف: اختيار ضيف + موضوع + موعد استضافة ← DataGrid
4. تبويب المراسلين: اختيار مراسل + موضوع + وقت الظهور
5. تبويب طاقم التنفيذ: اختيار موظفين (مخرج، مذيع، فني صوت)
6. عند الحفظ: Episodes (StatusId=0) + EpisodeGuests + EpisodeCorrespondents + EpisodeEmployees

### المرحلة 2: تسجيل التنفيذ (Executed)
**المسؤول:** قسم الإنتاج
1. زر تسجيل التنفيذ على حلقة Planned
2. ExecutionLogDialog ← إدخال تفاصيل التنفيذ
3. StatusId = 1 + سجل في ExecutionLogs

### المرحلة 3: النشر الاجتماعي (Published)
**المسؤول:** قسم النشر الرقمي
1. زر النشر الرقمي على حلقة Executed
2. PublishingLogDialog ← قائمة ضيوف الحلقة
3. لكل ضيف: نوع المحتوى (Audio/Video/Both)، عنوان المقطع، مدته
4. اختيار منصات متعددة + رابط لكل منصة
5. StatusId = 2 + SocialMediaPublishingLog + SocialMediaPublishingLogPlatform

### المرحلة 4: النشر على الموقع (WebsitePublished)
**المسؤول:** قسم النشر الرقمي
1. زر النشر على الموقع على حلقة Executed أو Published
2. WebsitePublishDialog ← MediaType, Title, Notes
3. StatusId = 3 + WebsitePublishingLog

### التراجع (Revert)
- Executed → Planned | Published → Executed | WebsitePublished → Published
- **شرط:** إدخال سبب التراجع عبر ReasonInputDialog
- يتم تحديث StatusId وتسجيل السبب في CancellationReason

---

## 6. طبقة البيانات (DTOs)

### DTOs الرئيسية

`csharp
// -- ActiveEpisodeDto.cs -- عرض الحلقات
public record ActiveEpisodeDto
{
    public int EpisodeId { get; init; }
    public int ProgramId { get; init; }
    public string? EpisodeName { get; init; }
    public string? ProgramName { get; init; }
    public string? GuestsDisplay { get; init; }
    public DateTime? ScheduledExecutionTime { get; init; }
    public string? StatusText { get; init; }
    public byte StatusId { get; init; }
    public string? SpecialNotes { get; init; }
    public string? CancellationReason { get; set; }

    public List<GuestDisplayItem> GuestItems { get; init; } = [];
    public List<EpisodeCorrespondentDto> CorrespondentItems { get; init; } = [];
    public List<EpisodeEmployeeDto> EmployeeItems { get; init; } = [];

    // خصائص حسابية للأزرار
    public bool CanMarkExecuted   => StatusId == EpisodeStatus.Planned;
    public bool CanMarkPublished  => StatusId == EpisodeStatus.Executed;
    public bool CanToggleWebsite  => StatusId >= EpisodeStatus.Executed && StatusId != EpisodeStatus.Cancelled;
    public bool CanRevert         => StatusId is EpisodeStatus.Executed or EpisodeStatus.Published or EpisodeStatus.WebsitePublished;
    public bool CanCancel         => StatusId is EpisodeStatus.Planned or EpisodeStatus.Executed;
}

// -- EpisodeDto.cs -- إنشاء/تحديث
public record EpisodeDto(
    int EpisodeId, int ProgramId,
    List<EpisodeGuestDto> Guests,
    List<EpisodeCorrespondentDto> Correspondents,
    List<EpisodeEmployeeDto> Employees,
    string EpisodeName,
    DateTime? ScheduledDate,       // من DatePicker
    TimeSpan? BroadcastTime,       // من TimePicker
    string? SpecialNotes);

public record EpisodeGuestDto(int EpisodeGuestId, int GuestId, string FullName, string? Topic, TimeSpan? HostingTime);
public record EpisodeCorrespondentDto(int Id, int CorrespondentId, string FullName, string? Topic);
public record EpisodeEmployeeDto(int EpisodeEmployeeId, int EmployeeId);
public record GuestDisplayItem(string Name, string? Topic, TimeSpan? HostingTime);

// -- الموظفون والأدوار
public record EmployeeDto(int EmployeeId, string FullName, int? StaffRoleId, string? StaffRoleName, string? Notes);
public record StaffRoleDto(int StaffRoleId, string RoleName);

// -- منصات التواصل الاجتماعي 🆕
public record SocialMediaPlatformDto(int SocialMediaPlatformId, string Name, string? Icon);

// -- النشر الاجتماعي
public record SocialMediaPublishingLogDto(int LogId, int EpisodeGuestId, MediaType MediaType, string? ClipTitle, TimeSpan? Duration, List<PlatformPublishDto> Platforms);
public record PlatformPublishDto(int PlatformId, string PlatformName, string? Url);

// -- النشر على الموقع
public record WebsitePublishingLogDto(int Id, int EpisodeId, string? MediaType, string? Title, string? Notes, DateTime PublishedAt);
`

---

## 7. الخدمات (Services)

### جميع الخدمات (مرتبة أبجدياً)

| الواجهة | الملف | الغرض | طريقة الحقن | الحالة |
|:---|:---|:---|:---|:---:|
| IAuthService | AuthService.cs | تسجيل الدخول | Transient | ✅ |
| ICorrespondentService | CorrespondentService.cs | CRUD مراسلين | Transient | ✅ |
| ICoverageService | CoverageService.cs | إدارة التغطيات | Transient | ✅ |
| IEmployeeService | EmployeeService.cs | CRUD موظفين + أدوار | Transient | ✅ |
| IEpisodeService | EpisodeService.cs | CRUD الحلقات + transitions | Transient | ✅ |
| IExecutionService | ExecutionService.cs | تسجيل التنفيذ | Transient | ✅ |
| IGuestService | GuestService.cs | CRUD ضيوف | Transient | ✅ |
| **IPlatformService** 🆕 | **PlatformService.cs** | **CRUD منصات التواصل** | **Transient** | **✅** |
| IProgramService | ProgramService.cs | CRUD برامج | Transient | ✅ |
| IPublishingService | PublishingService.cs | نشر اجتماعي + موقع | Transient | ✅ |
| IReportsService | ReportsService.cs | تقارير | Transient | ✅ |
| IUserService | UserService.cs | مستخدمين + صلاحيات | Transient | ✅ |

### نمط الخدمة (Primary Constructor + IDbContextFactory)

`csharp
public class ExampleService(IDbContextFactory<BroadcastWorkflowDBContext> contextFactory) : IExampleService
{
    public async Task<Result<int>> DoSomethingAsync(Dto dto, UserSession session)
    {
        var permCheck = session.EnsurePermission(AppPermissions.PermissionName);
        if (!permCheck.IsSuccess)
            return Result<int>.Fail(permCheck.ErrorMessage!);

        using var context = await contextFactory.CreateDbContextAsync();
        // ... logic ...
        await context.SaveChangesAsync();
        return Result<int>.Success(value);
    }
}
`

### ⚠️ قاعدة مهمة: تسجيل الخدمات
عند إضافة خدمة جديدة، سجلها في Radio/App.xaml.cs بهذه الطريقة:
`csharp
builder.Services.AddTransient<IServiceInterface, ServiceImplementation>();
`
لا تستخدم AddScoped أو AddSingleton إلا لسبب واضح.

---

## 8. معايير واجهة المستخدم (UI Standards)

### مساحات الأسماء (مهم — خطأ شائع)
`xml
✅ xmlns:materialDesign="http://materialdesigninxaml.net/winfx/xaml/themes"
❌ xmlns:md="..."  ← يسبب فشل تحميل XAML
`

### مفاتيح الـ Styles المعتمدة

| العنصر | المفتاح |
|:---|:---|
| TextBox | Input.Text |
| TextBox متعدد الأسطر | Input.Text.Multiline |
| ComboBox | Input.ComboBox |
| DatePicker | Input.DatePicker |
| TimePicker | Input.TimePicker |
| زر رئيسي | Btn.Primary |
| زر إلغاء | Btn.Cancel |
| زر إضافة | Btn.AddNew |
| زر حذف (أيقونة) | Btn.Icon.Delete |
| زر تعديل (أيقونة) | Btn.Icon.Edit |
| Header color zone | Zone.Header.Primary |
| Footer | Window.Footer |
| DataGrid | DataGrid.Main |
| Row | DataGrid.Row |
| Cell | DataGrid.Cell |
| Cell (وسط) | DataGrid.Cell.Center |
| Cell (إجراءات) | DataGrid.Cell.Actions |
| ColumnHeader | DataGrid.ColumnHeader.Center |
| View Base | View.Base |
| بطاقة إحصائية | Card.Stat |
| بحث | Input.Search |
| بطاقة بث | BroadcastCard |

### أيقونات PackIcon الشائعة

| الاستخدام | الأيقونة |
|:---|:---|
| منصات التواصل | FacebookWorkplace |
| موظفون | PeopleGroup |
| أدوار | BadgeAccount |
| مستخدمون | AccountGroup |
| ضيف | AccountVoice |
| مراسل | Microphone |
| برنامج | Radio |
| حلقة | PlaylistEdit |
| تقارير | ChartBar |
| تغطية | CameraDocument |
| تراجع | Undo |
| حذف | Delete |
| تعديل | Pencil |
| إضافة | Plus |
| حفظ | ContentSave |
| إلغاء | Close |
| بحث | Magnify |

### نمط نافذة الحوار

**فتح من المتصل:**
`csharp
var view = new MyFormDialog(services...);
var result = await DialogHost.Show(view, "RootDialog");
if (result is true) await LoadDataAsync();
`

**إغلاق من داخل الحوار (حفظ):**
`csharp
DialogHost.Close("RootDialog", yourResultObject);
`

**إغلاق من داخل الحوار (إلغاء):**
`csharp
DialogHost.Close("RootDialog", null);
`

### تخطيط الشاشة القياسي

`xml
<UserControl Style="{StaticResource View.Base}" FlowDirection="RightToLeft">
  <Grid>
    <!-- Header -->
    <materialDesign:ColorZone Style="{StaticResource Zone.Header.Primary}">
      <DockPanel>
        <materialDesign:PackIcon Kind="IconName" />
        <TextBlock Text="العنوان" FontSize="20" FontWeight="Bold" />
        <Button Style="{StaticResource Btn.AddNew}" DockPanel.Dock="Left" />
      </DockPanel>
    </materialDesign:ColorZone>

    <!-- Search + Stats -->
    <Grid Grid.Row="1" Margin="24,20,24,10">
      <TextBox Style="{StaticResource Input.Search}" />
    </Grid>

    <!-- DataGrid -->
    <materialDesign:Card Grid.Row="2" Margin="24,10,24,24"
                          UniformCornerRadius="12"
                          materialDesign:ElevationAssist.Elevation="Dp1">
      <DataGrid Style="{StaticResource DataGrid.Main}" />
    </materialDesign:Card>
  </Grid>
</UserControl>
`

### تخطيط نموذج الحوار

`xml
<UserControl Width="520" Height="Auto" FlowDirection="RightToLeft">
  <materialDesign:Card Background="{StaticResource SurfaceBrush}"
                        Effect="{StaticResource Shadow.Dialog}"
                        UniformCornerRadius="16">
    <Grid>
      <!-- Header -->
      <materialDesign:ColorZone Grid.Row="0" Style="{StaticResource Zone.Header.Primary}">
        <DockPanel>
          <TextBlock Text="{Binding Title}" FontSize="18" FontWeight="Bold" />
          <Button Style="{StaticResource Btn.Icon.Delete}" DockPanel.Dock="Left"
                  Click="BtnClose_Click" />
        </DockPanel>
      </materialDesign:ColorZone>

      <!-- Form Content -->
      <StackPanel Grid.Row="1" Margin="24,16">
        <TextBox Style="{StaticResource Input.Text}" materialDesign:HintAssist.Hint="الاسم *" />
        <TextBox Style="{StaticResource Input.Text}" materialDesign:HintAssist.Hint="أيقونة (اختياري)" />
      </StackPanel>

      <!-- Footer -->
      <Border Grid.Row="2" Style="{StaticResource Window.Footer}">
        <StackPanel Orientation="Horizontal" HorizontalAlignment="Left">
          <Button Style="{StaticResource Btn.Cancel}" Content="إلغاء" Click="BtnCancel_Click" />
          <Button Style="{StaticResource Btn.Primary}" Content="حفظ" Click="BtnSave_Click"
                  Margin="8,0,0,0" />
        </StackPanel>
      </Border>
    </Grid>
  </materialDesign:Card>
</UserControl>
`

---

## 9. نظام التنقل (MainWindow)

### التبويبات (بالترتيب من اليمين لليسار — حسب الـ FlowDirection)

| Tag | المحتوى | الصلاحية المطلوبة | Icon | الحالة |
|:---|:---|:---|:---|:---:|
| Home | لوحة التحكم | متاح للجميع | Home | ✅ |
| Programs | إدارة البرامج | PROGRAM_MANAGE | Radio | ✅ |
| Episodes | سجل الحلقات | متاح للجميع | PlaylistEdit | ✅ |
| Guests | إدارة الضيوف | GUEST_MANAGE | AccountVoice | ✅ |
| Correspondents | المراسلون الميدانيون | CORR_MANAGE | Microphone | ✅ |
| Coverage | التغطيات | CORR_MANAGE | CameraDocument | ✅ |
| Reports | التقارير | VIEW_REPORTS | ChartBar | ✅ |
| Users | إدارة المستخدمين | USER_MANAGE | AccountGroup | ✅ |
| Employees | طاقم العمل 🆕 | STAFF_MANAGE | PeopleGroup | ✅ |
| StaffRoles | المسميات الوظيفية 🆕 | STAFF_MANAGE | BadgeAccount | ✅ |
| **SocialPlatforms** 🆕 | **منصات التواصل** | **STAFF_MANAGE** | **FacebookWorkplace** | **✅** |
| Permissions | الصلاحيات | USER_MANAGE | Lock | ✅ |

> ملاحظة: تبويب SocialPlatforms يعيد استخدام صلاحية STAFF_MANAGE (لا حاجة لصلاحية جديدة).

### إضافة تبويب جديد (4 خطوات)

1. **MainWindow.xaml** ← إضافة RadioButton بـ Tag و Style=HubTabItem و Click=Tab_Click
2. **MainWindow.xaml.cs → LoadView()** ← إضافة case "TagName": مع حقن الخدمات و NavigateTo()
3. **MainWindow.xaml.cs → ApplyPermissionSecurity()** ← إخفاء/إظهار حسب الصلاحية
4. **App.xaml.cs** ← تسجيل الخدمة في DI Container إذا كانت جديدة

**مثال كودي للإضافة:**
`csharp
// في MainWindow.xaml.cs → ApplyPermissionSecurity()
bool canManageStaff = _session.HasPermission(AppPermissions.StaffManage);
MenuSocialPlatforms.Visibility = canManageStaff ? Visibility.Visible : Visibility.Collapsed;

// في MainWindow.xaml.cs → LoadView()
case "SocialPlatforms":
    var platformService = _serviceProvider.GetRequiredService<IPlatformService>();
    NavigateTo(new Views.Admin.SocialPlatformsView(platformService, _session));
    break;
`

---

## 10. نظام الصلاحيات

### جميع الصلاحيات (AppPermissions.cs)

`csharp
// المستخدمون
UserManage         = "USER_MANAGE"

// البرامج
ProgramManage      = "PROGRAM_MANAGE"

// الحلقات
EpisodeManage      = "EPISODE_MANAGE"    // إضافة + تعديل
EpisodeExecute     = "EPISODE_EXECUTE"   // تسجيل التنفيذ
EpisodePublish     = "EPISODE_PUBLISH"   // النشر الاجتماعي
EpisodeWebPublish  = "EPISODE_WEB_PUBLISH" // النشر على الموقع
EpisodeEdit        = "EPISODE_EDIT"      // تعديل بعد الإنشاء
EpisodeDelete      = "EPISODE_DELETE"    // حذف ناعم
EpisodeRevert      = "EPISODE_REVERT"    // تراجع عن حالة

// الضيوف
GuestManage        = "GUEST_MANAGE"

// المراسلون
CoordinationManage = "CORR_MANAGE"

// طاقم العمل 🆕 (يُستخدم أيضاً لـ SocialMediaPlatform)
StaffManage        = "STAFF_MANAGE"

// التقارير
ViewReports        = "VIEW_REPORTS"
`

### 🔑 ملاحظات مهمة عن الصلاحيات
- STAFF_MANAGE تُستخدم لـ **ثلاث تبويبات**: Employees, StaffRoles, SocialPlatforms
- ليس كل تبويب يحتاج صلاحية جديدة — أعد استخدام الصلاحيات الموجودة حيثما أمكن
- UserSession تحتوي على HasPermission() و EnsurePermission() للتحقق

---

## 11. قائمة المهام المتبقية

### ✅ كافة المهام المخطط لها مكتملة

| المجموعة | المهام | الحالة |
|:---|:---|:---:|
| **الكيانات** | Employee, StaffRole, EpisodeEmployee, EpisodeCorrespondent, SocialMediaPlatform, SocialMediaPublishingLog, WebsitePublishingLog | ✅ |
| **الخدمات** | IEmployeeService, IPublishingService, IPlatformService + كافة الخدمات الأخرى | ✅ |
| **واجهات المستخدم** | EmployeesView, EmployeeFormDialog, StaffRolesView, StaffRoleFormDialog, SocialPlatformsView, PlatformFormDialog, PublishingLogDialog, WebsitePublishDialog | ✅ |
| **التكامل** | ربط EpisodeFormControl بـ IEmployeeService، تسجيل DI، تفعيل MainWindow | ✅ |
| **Seed Data** | SocialMediaPlatforms (5 منصات)، StaffRoles (4 أدوار) | ✅ |
| **الصلاحيات** | STAFF_MANAGE مضاف ومعتمد في التبويبات الثلاثة | ✅ |
| **التوثيق** | AI_DEVELOPMENT_GUIDE.md محدث وكامل | ✅ |

### 🔮 مقترحات للتطوير المستقبلي

#### أولوية عالية
- اختبارات آلية (Unit Tests) للخدمات الأساسية: EpisodeService, PublishingService, PlatformService
- التحقق من أداء الاستعلامات ومراجعة N+1 patterns

#### أولوية متوسطة
- **تقارير متقدمة** — رسوم بيانية، إحصائيات شهرية، تصدير Excel/PDF
- **نظام إشعارات** — تنبيهات المستخدمين عند تغيير حالة الحلقة
- **تحسين الـ UI** — إضافة شريط تقدم، تأكيدات بصرية، رسائل نجاح/فشل بعد العمليات

#### أولوية منخفضة
- **تدويل (Localization)** — دعم لغات إضافية
- **سجل التدقيق (Audit Log Viewer)** — واجهة لعرض سجل التغييرات
- **إعادة تعيين كلمة المرور** — نافذة مخصصة مع التحقق

---

## 12. أوامر البناء

`powershell
# بناء المشروع
cd D:\\Radio
dotnet build Radio/Radio.csproj

# تشغيل مع مشاهدة التغييرات
dotnet watch run --project Radio/Radio.csproj

# تشغيل عادي
dotnet run --project Radio/Radio.csproj

# إضافة Migration
dotnet ef migrations add MigrationName --project Domain --startup-project Radio

# تطبيق Migration
dotnet ef database update --project Domain --startup-project Radio

# إزالة آخر Migration (إذا فشل)
dotnet ef migrations remove --project Domain --startup-project Radio

# تنظيف وإعادة بناء (عند أخطاء غريبة)
dotnet clean Radio/Radio.csproj && dotnet build Radio/Radio.csproj
`

---

## 13. الأخطاء الشائعة

| ❌ خطأ | ✅ الصحيح |
|:---|:---|
| xmlns:md="...materialDesign..." | xmlns:materialDesign="...materialDesign..." |
| StaffRoleId على EpisodeEmployee | StaffRoleId على Employee مباشرة |
| context.Remove(entity) | entity.IsActive = false |
| 	hrow new Exception() في الخدمة | eturn Result.Fail("msg") |
| ItemsSource=... في XAML | تعيين ItemsSource في code-behind |
| 
ew ServiceClass() في View | _serviceProvider.GetRequiredService<IService>() |
| dto.Id في SyncGuests | dto.EpisodeGuestId |
| BroadcastTextBox / BroadcastComboBox | Input.Text / Input.ComboBox |
| dto.ScheduledTime | dto.ScheduledDate + dto.BroadcastTime |
| تعديل حقول Audit يدوياً | تركها لـ AuditInterceptor |
| استخدام MetroWindow في DialogHost | استخدام UserControl في DialogHost |
| صلاحية جديدة لكل تبويب صغير | أعد استخدام صلاحيات موجودة (مثل STAFF_MANAGE) |

---

## 14. مسرد المصطلحات

| المصطلح | المعنى |
|:---|:---|
| **Episode** | حلقة — وحدة البث الأساسية |
| **Program** | برنامج إذاعي — تصنيف للحلقات |
| **Guest** | ضيف — مشارك في الحلقة |
| **Correspondent** | مراسل ميداني — يقدم تقارير من الميدان |
| **Employee** | موظف — عضو طاقم الإنتاج (مخرج، مذيع، فني) |
| **StaffRole** | مسمى وظيفي — دور الموظف في المؤسسة |
| **Coverage** | تغطية — مهمة ميدانية |
| **SocialMediaPlatform** | منصة تواصل اجتماعي — وجهة النشر الرقمي |
| **SocialMediaPublishingLog** | سجل النشر الاجتماعي — توثيق نشر مقطع |
| **WebsitePublishingLog** | سجل نشر الموقع — توثيق نشر على الموقع الإلكتروني |
| **ExecutionLog** | سجل التنفيذ — توثيق وقت تنفيذ الحلقة |
| **AuditLog** | سجل التدقيق — تتبع جميع التغييرات |
| **MediaType** | نوع الوسائط — Audio, Video, Both |
| **Result Pattern** | نمط النتيجة — إرجاح نجاح/فشل مع رسالة خطأ |
| **Soft Delete** | حذف ناعم — تعطيل بدلاً من حذف فعلي |
| **Revert** | تراجع — العودة لحالة سابقة في سير العمل |

---

> **⚠️ تحذير للنماذج المستقبلية:**
> - لا تعدّل ملفات الـ Domain مباشرة إلا عبر Migration
> - استخدم Result Pattern في جميع دوال الخدمات
> - احترم Global Query Filter للـ IsActive
> - لا تستخدم InverseProperty على كيان User
> - استخدم IDbContextFactory ولا تحقن DbContext مباشرة
> - سجّل كل خدمة جديدة في App.xaml.cs بـ AddTransient
> - **حدّث هذا الملف بعد إتمام كل مرحلة**

---
*آخر تحديث: مايو 2026 — AI Agent Development Guide v5.2*
