using System.Security.Cryptography;
using System.Text;

namespace DataAccess.Security;

/// <summary>
/// تشفير وفك تشفير نص الاتصال بقاعدة البيانات باستخدام DPAPI
/// (Data Protection API) — واجهة حماية البيانات المدمجة في Windows.
///
/// ✨ المزايا:
///   - لا حاجة لإدارة مفاتيح التشفير (Windows يتولى ذلك)
///   - التشفير مرتبط بحساب المستخدم الحالي (CurrentUser) أو الجهاز (LocalMachine)
///   - لا يمكن فك التشفير على جهاز/حساب مختلف
///
/// 📋 الاستخدام:
///   1. في وضع التطوير: نص الاتصال يبقى بصيغة نص عادي في appsettings.json
///   2. في وضع الإنتاج: شفّر النص باستخدام ConnectionStringProtector.Encrypt()
///      ثم ضعه في appsettings.json مع بادئة "ENC:" مثل:
///      "DefaultConnection": "ENC:AQAAANCMnd8BFdERjH..."
///   3. يمكن أيضاً استخدام متغير البيئة RADIO_CONNECTION_STRING كأولوية قصوى
///
/// ⚠️ ملاحظة: DPAPI يعمل على Windows فقط. لا يعمل على Linux/macOS.
/// </summary>
public static class ConnectionStringProtector
{
    /// <summary>
    /// بادئة تحدد أن القيمة مشفرة بـ DPAPI.
    /// عند وجود هذه البادئة، يقوم النظام بفك التشفير تلقائياً.
    /// </summary>
    public const string EncryptedPrefix = "ENC:";

    /// <summary>
    /// بادئة تحدد أن القيمة مشفرة بـ DPAPI بنطاق الجهاز (Machine scope).
    /// تستخدم عندما يحتاج عدة مستخدمين على نفس الجهاز الوصول لنفس النص المشفر.
    /// </summary>
    public const string EncryptedMachinePrefix = "ENCM:";

    // إنتروبي إضافية لزيادة أمان التشفير (ليست سرية — فقط تمنع هجمات القاموس)
    private static readonly byte[] s_additionalEntropy = Encoding.UTF8.GetBytes("Radio_BroadcastWorkflowDB_2024");

    /// <summary>
    /// تشفير نص الاتصال باستخدام DPAPI بنطاق المستخدم الحالي.
    /// النتيجة تكون Base64 يمكن تخزينها مباشرة في appsettings.json مع بادئة "ENC:".
    /// </summary>
    /// <param name="plainText">نص الاتصال بصيغة نص عادي</param>
    /// <returns>النص المشفر بصيغة Base64 (بدون بادئة)</returns>
    public static string Encrypt(string plainText)
    {
        if (string.IsNullOrWhiteSpace(plainText))
            throw new ArgumentException("نص الاتصال لا يمكن أن يكون فارغاً.", nameof(plainText));

        var plainBytes = Encoding.Unicode.GetBytes(plainText);
        var encryptedBytes = ProtectedData.Protect(plainBytes, s_additionalEntropy, DataProtectionScope.CurrentUser);
        return Convert.ToBase64String(encryptedBytes);
    }

    /// <summary>
    /// تشفير نص الاتصال باستخدام DPAPI بنطاق الجهاز.
    /// النتيجة تكون Base64 يمكن تخزينها مباشرة في appsettings.json مع بادئة "ENCM:".
    /// بنطاق الجهاز: يمكن لأي مستخدم على نفس الجهاز فك التشفير.
    /// </summary>
    /// <param name="plainText">نص الاتصال بصيغة نص عادي</param>
    /// <returns>النص المشفر بصيغة Base64 (بدون بادئة)</returns>
    public static string EncryptForMachine(string plainText)
    {
        if (string.IsNullOrWhiteSpace(plainText))
            throw new ArgumentException("نص الاتصال لا يمكن أن يكون فارغاً.", nameof(plainText));

        var plainBytes = Encoding.Unicode.GetBytes(plainText);
        var encryptedBytes = ProtectedData.Protect(plainBytes, s_additionalEntropy, DataProtectionScope.LocalMachine);
        return Convert.ToBase64String(encryptedBytes);
    }

    /// <summary>
    /// فك تشفير نص الاتصال المشفر بـ DPAPI.
    /// </summary>
    /// <param name="encryptedBase64">النص المشفر بصيغة Base64 (بدون بادئة)</param>
    /// <param name="scope">نطاق فك التشفير (CurrentUser أو LocalMachine)</param>
    /// <returns>نص الاتصال بصيغة نص عادي</returns>
    public static string Decrypt(string encryptedBase64, DataProtectionScope scope = DataProtectionScope.CurrentUser)
    {
        if (string.IsNullOrWhiteSpace(encryptedBase64))
            throw new ArgumentException("النص المشفر لا يمكن أن يكون فارغاً.", nameof(encryptedBase64));

        var encryptedBytes = Convert.FromBase64String(encryptedBase64);
        var plainBytes = ProtectedData.Unprotect(encryptedBytes, s_additionalEntropy, scope);
        return Encoding.Unicode.GetString(plainBytes);
    }

    /// <summary>
    /// فحص ما إذا كانت القيمة مشفرة بـ DPAPI (تبدأ بـ "ENC:" أو "ENCM:").
    /// </summary>
    public static bool IsEncrypted(string value)
    {
        return !string.IsNullOrEmpty(value) &&
               (value.StartsWith(EncryptedPrefix) || value.StartsWith(EncryptedMachinePrefix));
    }

    /// <summary>
    /// فك تشفير القيمة تلقائياً إذا كانت مشفرة، أو إرجاعها كما هي إذا كانت نص عادي.
    /// هذا هو المدخل الرئيسي المستخدم من SecureConfigurationProvider.
    /// </summary>
    /// <param name="value">القيمة من appsettings.json (قد تكون نص عادي أو مشفرة مع بادئة)</param>
    /// <returns>نص الاتصال بصيغة نص عادي</returns>
    public static string UnprotectIfEncrypted(string value)
    {
        if (string.IsNullOrEmpty(value))
            return value;

        // بنطاق المستخدم الحالي (ENC:)
        if (value.StartsWith(EncryptedPrefix))
        {
            var encryptedPart = value[EncryptedPrefix.Length..];
            return Decrypt(encryptedPart, DataProtectionScope.CurrentUser);
        }

        // بنطاق الجهاز (ENCM:)
        if (value.StartsWith(EncryptedMachinePrefix))
        {
            var encryptedPart = value[EncryptedMachinePrefix.Length..];
            return Decrypt(encryptedPart, DataProtectionScope.LocalMachine);
        }

        // نص عادي — إرجاع كما هو
        return value;
    }

    /// <summary>
    /// محاولة فك التشفير بأمان — ترجع false بدلاً من رمي استثناء عند الفشل.
    /// يُستخدم عند عدم التأكد من صلاحية فك التشفير (مثل بيئة مختلفة).
    /// </summary>
    public static bool TryUnprotect(string value, out string result)
    {
        try
        {
            result = UnprotectIfEncrypted(value);
            return true;
        }
        catch (Exception)
        {
            result = string.Empty;
            return false;
        }
    }
}
