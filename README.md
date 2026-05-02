<div align="center">

# 📻 Radio — نظام إدارة البث الإذاعي (بث برو)

[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![WPF](https://img.shields.io/badge/WPF-Desktop-0078D7?logo=windows)](https://github.com/dotnet/wpf)
[![EF Core](https://img.shields.io/badge/EF_Core-10.0-68217A?logo=dotnet)](https://learn.microsoft.com/ef/core)
[![SQL Server](https://img.shields.io/badge/SQL_Server-CC2927?logo=microsoftsqlserver)](https://www.microsoft.com/sql-server)
[![Material Design](https://img.shields.io/badge/UI-Material_Design-757575?logo=materialdesign)](https://github.com/MaterialDesignInXAML/MaterialDesignInXamlToolkit)

**نظام مكتبي متكامل لإدارة دورة العمل الإذاعي — من الجدولة الذكية حتى الأرشفة والنشر الرقمي.**

</div>

---

## 📖 نظرة عامة

**Radio (بث برو)** هو تطبيق سطح مكتب متقدم مبني على تقنيات **.NET 10** و **WPF**، مصمم خصيصاً لتلبية احتياجات الإذاعات العصرية. يهدف النظام إلى أتمتة دورة حياة "الحلقة الإذاعية" بالكامل، مع التركيز على دقة البيانات، سهولة الاستخدام، والرقابة الكاملة عبر نظام صلاحيات صارم وسجل تدقيق شامل.

تمت إعادة هيكلة النظام مؤخراً (مايو 2026) لتبني معايير برمجية أكثر حداثة واستقراراً.

---

## ✨ المميزات الرئيسية

- 🚀 **واجهة مستخدم عصرية**: تصميم يعتمد على Material Design مع انتقالات سلسة واستخدام لـ `DialogHost` لتوفير تجربة مستخدم خالية من تشتت النوافذ المتعددة.
- 🏗️ **معمارية نظيفة (Clean Architecture)**: فصل تام بين طبقة المجال (Domain)، الخدمات (DataAccess)، وواجهة العرض (WPF).
- 🔐 **نظام أمني متكامل**: إدارة دقيقة للمستخدمين والأدوار مع صلاحيات تصل لمستوى العمليات الفردية.
- 📝 **سجل تدقيق ذكي (Audit System)**: تتبع آلي لكل تغيير (مَن، متى، ماذا تغير، والسبب) بفضل الـ Interceptors في EF Core.
- 🔄 **إدارة دورة حياة الحلقة**: معالجة حالات الجدولة، التنفيذ، النشر الرقمي، والنشر على الموقع مع إمكانية التراجع الذكي.
- 🛡️ **حذف منطقي (Soft Delete)**: حماية البيانات من الحذف النهائي مع إمكانية الاسترجاع، مدعومة بفلترة عالمية تلقائية.
- ⚡ **نمط Result Pattern**: معالجة الأخطاء والتحقق من البيانات بدون استثناءات مكلفة، مما يضمن أداءً فائقاً واستقراراً في الواجهة.

---

## 🏗️ المعمارية التقنية

### تقسيم الطبقات
1.  **Domain**: يحتوي على الكيانات (Entities)، التكوينات (Fluent API)، والـ `DbContext`. تم توحيد إدارة التدقيق عبر `BaseEntity`.
2.  **DataAccess**: تضم منطق الأعمال (Services)، كائنات نقل البيانات (DTOs)، ونظام التحقق المركزي (Validation).
3.  **Radio (Presentation)**: واجهة WPF تعتمد على `MahApps.Metro` و `MaterialDesignInXAML`.

### قاعدة البيانات
يعتمد النظام على **SQL Server** مع بنية جداول محسنة تمنع التكرار (Normalized Data). تم تنظيف العلاقات المعقدة مع جدول المستخدمين لضمان سرعة الاستعلامات ومنع تعارض المسارات.

---

## 🛠️ التقنيات المستخدمة

| التقنية | الغرض |
|---------|-------|
| **.NET 10.0** | منصة التطوير الأساسية |
| **WPF** | إطار عمل واجهة المستخدم |
| **EF Core 10.0** | التعامل مع قاعدة البيانات (ORM) |
| **SQL Server** | محرك قاعدة البيانات |
| **Material Design In XAML** | التصميم البصري والأيقونات |
| **MahApps.Metro** | إطارات النوافذ والتحكم المتقدم |
| **Fody / PropertyChanged** | أتمتة إشعارات تغيير الخصائص |
| **Microsoft DI** | حقن التبعيات وإدارة دورة حياة الخدمات |

---

## 🚀 التشغيل والتثبيت

### المتطلبات
- نظام تشغيل Windows 10/11.
- .NET 10.0 SDK أو أحدث.
- SQL Server Express أو LocalDB.

### الخطوات
1.  **استنساخ المستودع**:
    ```bash
    git clone https://github.com/dabasgaza/Radio.git
    ```
2.  **إعداد قاعدة البيانات**:
    تأكد من ضبط `ConnectionStrings` في ملف `Radio/appsettings.json`.
3.  **تطبيق التهجيرات (Migrations)**:
    ```bash
    dotnet ef database update --project Domain --startup-project Radio
    ```
4.  **التشغيل**:
    افتح ملف الحل `Radio.slnx` عبر Visual Studio أو استخدم الأمر:
    ```bash
    dotnet run --project Radio
    ```

---

## 📝 سجل التحديثات الأخيرة (مايو 2026)

- ✅ **Refactoring الشامل**: حذف الحقول المكررة وتوحيد منطق النشر.
- ✅ **إعادة ضبط الـ Migrations**: إنشاء بنية قاعدة بيانات نظيفة ومستقرة تماماً.
- ✅ **تطوير الواجهة**: تحويل نوافذ الإدخال إلى `UserControls` عصرية تعمل داخل `DialogHost`.
- ✅ **تحسين الأداء**: تبسيط علاقات الكيانات مع كلاس `User` لتقليل حجم الكائنات المسترجعة.
- ✅ **التحقق المركزي**: دمج نظام `ValidationPipeline` مع `Result Pattern`.

---

## 📄 الترخيص
خاص — للاستخدام الداخلي لمنظمة "إذاعة صوت القدس".

<div align="center">
تم التطوير بواسطة <b>Antigravity AI</b> بالتعاون مع فريق التطوير.
</div>
