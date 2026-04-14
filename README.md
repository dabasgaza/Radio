إليك ملف `README.md` احترافي مصاغ باللغتين العربية والإنجليزية، مصمم خصيصاً ليعكس جودة العمل المعماري الذي قمنا به، وليكون دليلاً مثالياً لأي مطور (أو ذكاء اصطناعي) يقرأ المشروع مستقبلاً.

---

# 📡 Broadcast Pro | نظام إدارة سير العمل الإذاعي

[![Framework](https://img.shields.io/badge/Framework-.NET%209-blueviolet)](https://dotnet.microsoft.com/download)
[![UI](https://img.shields.io/badge/UI-WPF%20%7C%20Material%20Design-blue)](https://github.com/MaterialDesignInXAML/MaterialDesignInXamlToolkit)
[![Database](https://img.shields.io/badge/Database-SQL%20Server%202019+-red)](https://www.microsoft.com/en-us/sql-server/)

نظام مؤسسي (Enterprise-Grade) متكامل لإدارة العمليات اليومية في الإذاعات المحلية، يغطي كافة المراحل من جدولة الحلقات إلى التنفيذ والنشر الرقمي، مع نظام أمان وتتبع متطور.

---

## ✨ المميزات الرئيسية (Key Features)

*   **🛡️ نظام صلاحيات ديناميكي (Dynamic Permissions):** مصفوفة صلاحيات متكاملة تسمح بالتحكم في كل زر ونافذة بناءً على دور المستخدم (Admin, تنسيق، إنتاج، نشر).
*   **📝 تتبع آلي شامل (Automated Audit Trail):** معترض (Interceptor) ذكي يسجل كافة التعديلات (القيم القديمة والجديدة) بصيغة **JSON** تلقائياً.
*   **💬 نظام رسائل مركزي (Ambient Context Messaging):** إدارة الإشعارات (Snackbar) ورسائل التأكيد (DialogHost) بشكل Thread-safe من طبقة الخدمات مباشرة.
*   **🔄 دورة حياة الحلقة (Episode Lifecycle):** نظام صارم لانتقال الحالات (Planned → Executed → Published) يضمن دقة سير العمل.
*   **🗑️ حذف منطقي عالمي (Global Soft Delete):** حماية البيانات من الحذف النهائي مع فلترة تلقائية للمحذوفات في كافة استعلامات النظام.
*   **🎨 واجهة عصرية (Modern UI/UX):** تصميم مظلم (Dark Theme) متباين واحترافي مع دعم كامل للغة العربية (RTL).

---

## 🏗️ المعمارية والتقنيات (Architecture & Tech Stack)

| التقنية | الوصف |
| :--- | :--- |
| **C# 13 / .NET 9** | أحدث إصدارات لغة البرمجة ومنصة دوت نت. |
| **WPF (Code-Behind)** | معمارية نظيفة تعتمد على فصل المنطق في الخدمات دون تعقيد MVVM. |
| **EF Core 9** | استخدام `IDbContextFactory` لإدارة السياقات قصيرة العمر وتحسين الأداء. |
| **SQL Server** | قاعدة بيانات علائقية مع دعم كامل للترتيب العربي `Arabic_CI_AS`. |
| **Material Design & MahApps** | دمج أفضل مكتبات التصميم للوصول لواجهة مستخدم احترافية. |

---

## ⚙️ القواعد البرمجية (Architectural Rules)

تم بناء المشروع وفق قواعد صارمة لضمان استقرار الكود:
1.  **No MVVM:** الاعتماد على Code-Behind لتبسيط الربط وتقليل الـ Boilerplate code.
2.  **Pure EF Core:** لا توجد Views أو Stored Procedures؛ كل المنطق البرمجي مكتوب بـ LINQ.
3.  **Concurrency Handling:** حماية البيانات من التضارب باستخدام `RowVersion` وعرض نوافذ مقارنة (Diff Dialog).
4.  **Validation Pipeline:** نظام تحقق مركزي يمنع دخول بيانات خاطئة لقاعدة البيانات من طبقة الخدمات.

---

## 🗄️ مخطط قاعدة البيانات (Database Schema)

يتكون النظام من 13 جدولاً رئيسياً مقسمة إلى:
*   **الأمان:** `Roles`, `Users`, `Permissions`, `RolePermissions`.
*   **المحتوى:** `Programs`, `Episodes`, `EpisodeStatuses`, `Guests`, `EpisodeGuests`.
*   **التدقيق:** `ExecutionLogs`, `PublishingLogs`, `AuditLogs`.

---

## 🚀 التشغيل السريع (Quick Start)

1.  **قاعدة البيانات:** قم بتنفيذ سكريبت SQL الموجود في مجلد `/Database/Final_Schema.sql`.
2.  **الإعدادات:** قم بتعديل نص الاتصال (Connection String) في ملف `appsettings.json`.
3.  **تسجيل الدخول:** استخدم الحساب الافتراضي للمدير:
    *   **Username:** `superadmin`
    *   **Password:** `Admin@2025`

---

## 🛠️ المتطلبات (Requirements)

*   Visual Studio 2022 (v17.12+)
*   .NET 9 SDK
*   SQL Server 2019 or later

---

### 📝 ملاحظة للمطورين
هذا المشروع تم تصميمه ليكون قابلاً للتوسع بسهولة. عند إضافة مديول جديد، يرجى الالتزام باستخدام `MessageService.Current` للإشعارات و `BaseEntity` لضمان التتبع التلقائي.

---
*تم تطوير هذا النظام ليكون معياراً في إدارة المؤسسات الإعلامية الصغيرة والمتوسطة.*# Radio
