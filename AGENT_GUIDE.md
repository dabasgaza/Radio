# 📡 دليل تطوير نظام راديو (Radio System) — دليل الوكلاء الذكي (AI Agent Guide)

هذا المستند مصمم ليكون المرجع الأول والأخير لأي وكيل ذكاء اصطناعي يعمل على هذا المشروع. تم تنظيمه لتوفير استهلاك التوكينز (Tokens) وتوفير أقصى قدر من المعلومات التقنية الدقيقة.

---

## 🏗️ 1. المخطط المعماري (Architectural Blueprint)

النظام مبني باستخدام **.NET 10 / WPF** بنظام الطبقات المنفصلة:

1.  **Domain**: يحتوي على الكيانات (Entities) وإعدادات EF Core.
2.  **DataAccess**: يحتوي على منطق الأعمال (Services)، الـ DTOs، ونظام الصلاحيات.
3.  **Radio**: يحتوي على واجهة المستخدم (UI)، المصادر (Resources)، وتهيئة حقن الاعتماديات (DI).

### 📏 القواعد الذهبية (Golden Rules) - لا تقبل النقاش:
- **Result Pattern**: جميع الخدمات يجب أن ترجع `Result` أو `Result<T>`. لا تستخدم الاستثناءات (Exceptions) للأخطاء المتوقعة.
- **BaseEntity**: أي جدول يحتاج تدقيق (Audit) يجب أن يرث من `BaseEntity`.
- **Soft Delete**: لا تستخدم `Remove()`. استخدم `IsActive = false`.
- **Auto-Auditing**: الـ `AuditInterceptor` يعالج التواريخ ومعرفات المستخدمين تلقائياً.
- **IDbContextFactory**: لا تحقن الـ `DbContext` مباشرة. استخدم المصنع لإنشاء نسخة جديدة داخل كل عملية.
- **No InverseProperty on User**: لتجنب تضارب EF Core في علاقات CreatedBy/UpdatedBy.

---

## 📂 2. خريطة الملفات (Project Map)

| المسار | المحتوى |
| :--- | :--- |
| `Domain/Models/` | الكيانات (Episode, Guest, Program, etc.) |
| `Domain/Models/Configurations/` | قيود قاعدة البيانات (Fluent API) |
| `DataAccess/Services/` | منطق العمل (EpisodeService, PublishingService, etc.) |
| `DataAccess/DTOs/` | سجلات نقل البيانات (Immutable Records) |
| `DataAccess/Common/` | `Result.cs`, `AppPermissions.cs`, `UserSession.cs` |
| `Radio/Views/` | واجهات المستخدم (UserControls) |
| `Radio/Resources/Styles/` | القوالب الموحدة (Standard Styles) |

---

## 🔄 3. سير العمل (Workflows)

### دورة حياة الحلقة (Episode Lifecycle):
1.  **Planned (0)**: حلقة مجدولة (إدخال البيانات الأساسية).
2.  **Executed (1)**: حلقة نُفذت (تسجيل وقت التنفيذ الفعلي).
3.  **Published (2)**: حلقة نُشرت رقمياً (مقاطع السوشيال ميديا).
4.  **WebsitePublished (3)**: حلقة نُشرت على الموقع الإلكتروني.
5.  **Cancelled (4)**: حلقة ملغاة (مع ذكر السبب).

---

## 💻 4. الأنماط البرمجية (Coding Patterns)

### 🔹 نمط الخدمة (Service Template):
```csharp
public async Task<Result<int>> DoWorkAsync(WorkDto dto, UserSession session)
{
    // 1. التحقق من الصلاحيات
    var perm = session.EnsurePermission(AppPermissions.SomePermission);
    if (!perm.IsSuccess) return Result<int>.Fail(perm.ErrorMessage!);

    // 2. استخدام DbContextFactory
    using var context = await contextFactory.CreateDbContextAsync();
    
    // 3. التنفيذ والحفظ
    // ... logic ...
    await context.SaveChangesAsync();
    
    return Result<int>.Success(entity.Id);
}
```

### 🔹 نمط مزامنة المجموعات (Collection Sync):
عند تعديل قائمة (مثل ضيوف الحلقة)، اتبع هذا النمط:
1. جلب العناصر الحالية بـ `Include`.
2. العناصر التي في الـ DB وليست في الـ DTO ← `IsActive = false`.
3. العناصر التي `Id == 0` ← إضافة جديدة.
4. العناصر التي `Id != 0` ← تحديث.

---

## 🎨 5. معايير واجهة المستخدم (UI Standards)

يجب استخدام الـ Styles الموحدة لضمان اتساق الواجهة:
- **نصوص**: `Style="{StaticResource Input.Text}"`
- **أزرار**: `Style="{StaticResource Btn.Primary}"`, `Btn.Cancel`, `Btn.AddNew`
- **جداول**: `Style="{StaticResource DataGrid.Main}"`
- **رأس الصفحة**: `Style="{StaticResource Zone.Header.Primary}"`

---

## 🛡️ 6. الصلاحيات (Permissions)

الصلاحيات معرفة في `AppPermissions.cs`. لا تقم بإنشاء سلاسل نصية يدوياً.
أمثلة: `USER_MANAGE`, `EPISODE_MANAGE`, `STAFF_MANAGE`.

---

## 📉 7. استراتيجية توفير التوكينز (Token-Saving Strategy)

للمساعدة في العمل بكفاءة:
- **لا تقرأ ملفات الخدمة كاملة**: إذا كنت تحتاج فقط لمعرفة التوقيع (Signature)، اطلب قراءة الـ Interface أو أول 50 سطر.
- **استخدم `KNOWLEDGE_BASE.md`**: للحقائق السريعة بدلاً من البحث في الكود.
- **اعتمد على المخططات**: لفهم العلاقات المعقدة.
- **لا تطلب شرحاً للمعمارية**: هي مشروحة هنا بالتفصيل.

---
*هذا الدليل هو رفيقك الذكي لبناء نظام إذاعي احترافي.*
