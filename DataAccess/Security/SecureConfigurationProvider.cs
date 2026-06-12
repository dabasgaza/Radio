using Microsoft.Extensions.Configuration;
using Serilog;

namespace DataAccess.Security;

/// <summary>
/// مزود التهيئة الآمن — يقرأ نص الاتصال من عدة مصادر بترتيب أولوية:
///
/// 🥇 الأولوية 1: متغير البيئة RADIO_CONNECTION_STRING
///     - الأمان الأعلى — لا يُخزّن في ملفات المشروع
///     - مفضل في بيئات الإنتاج والخوادم
///     - يُعين عبر: setx RADIO_CONNECTION_STRING "Server=.;Database=..."
///
/// 🥈 الأولوية 2: نص الاتصال المشفر في appsettings.json (بادئة ENC: أو ENCM:)
///     - أمان متوسط — مشفر بـ DPAPI، لا يُقرأ على جهاز آخر
///     - مفضل في بيئات الإنتاج على أجهزة المستخدمين
///
/// 🥉 الأولوية 3: نص الاتصال بصيغة نص عادي في appsettings.json
///     - بدون أمان — مخصص لبيئة التطوير فقط
///     - ⚠️ لا يُنصح به في الإنتاج
///
/// مثال appsettings.json:
/// {
///   "ConnectionStrings": {
///     "DefaultConnection": "ENC:AQAAANCMnd8BFdERjH..."    // مشفر
///   }
/// }
///
/// أو نص عادي (للتطوير):
/// {
///   "ConnectionStrings": {
///     "DefaultConnection": "Server=.;Database=BroadcastWorkflowDB;Trusted_Connection=True;TrustServerCertificate=True;"
///   }
/// }
/// </summary>
public static class SecureConfigurationProvider
{
    /// <summary>
    /// اسم متغير البيئة لنص الاتصال (الأولوية القصوى).
    /// </summary>
    public const string EnvironmentVariableName = "RADIO_CONNECTION_STRING";

    /// <summary>
    /// قراءة نص الاتصال الآمن من التهيئة مع دعم التشفير ومتغيرات البيئة.
    /// </summary>
    /// <param name="configuration">كائن التهيئة (IConfiguration)</param>
    /// <param name="connectionName">اسم نص الاتصال في التهيئة (الافتراضي: DefaultConnection)</param>
    /// <returns>نص الاتصال بصيغة نص عادي (جاهز للاستخدام)</returns>
    public static string GetSecureConnectionString(IConfiguration configuration, string connectionName = "DefaultConnection")
    {
        // 🥇 الأولوية 1: متغير البيئة
        var envValue = Environment.GetEnvironmentVariable(EnvironmentVariableName);
        if (!string.IsNullOrWhiteSpace(envValue))
        {
            Log.Information("نص الاتصال مقروء من متغير البيئة {EnvVar}", EnvironmentVariableName);
            return envValue;
        }

        // 🥈 الأولوية 2 و 3: من appsettings.json (مشفر أو نص عادي)
        var configValue = configuration.GetConnectionString(connectionName);
        if (string.IsNullOrWhiteSpace(configValue))
        {
            Log.Warning("نص الاتصال '{ConnName}' غير موجود في التهيئة أو متغير البيئة", connectionName);
            return string.Empty;
        }

        // إذا كانت القيمة مشفرة، فك التشفير
        if (ConnectionStringProtector.IsEncrypted(configValue))
        {
            try
            {
                var decrypted = ConnectionStringProtector.UnprotectIfEncrypted(configValue);
                Log.Information("نص الاتصال مشفر — تم فك التشفير بنجاح (النطاق: {Scope})",
                    configValue.StartsWith(ConnectionStringProtector.EncryptedMachinePrefix) ? "Machine" : "User");
                return decrypted;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "فشل فك تشفير نص الاتصال — قد يكون التشفير تم على جهاز/حساب مختلف");
                throw new InvalidOperationException(
                    "فشل فك تشفير نص الاتصال. تأكد من أن التشفير تم على نفس الجهاز والحساب، " +
                    "أو استخدم متغير البيئة RADIO_CONNECTION_STRING كبديل.", ex);
            }
        }

        // نص عادي (بيئة التطوير)
        Log.Debug("نص الاتصال مقروء بصيغة نص عادي من appsettings.json — يُنصح بالتشفير للإنتاج");
        return configValue;
    }

    /// <summary>
    /// فحص ما إذا كان نص الاتصال الحالي مشفراً.
    /// يُستخدم في شاشات التشخيص والإدارة.
    /// </summary>
    public static ConnectionStringSecurityStatus GetSecurityStatus(IConfiguration configuration, string connectionName = "DefaultConnection")
    {
        // فحص متغير البيئة أولاً
        var envValue = Environment.GetEnvironmentVariable(EnvironmentVariableName);
        if (!string.IsNullOrWhiteSpace(envValue))
        {
            return new ConnectionStringSecurityStatus
            {
                Source = "متغير البيئة",
                IsEncrypted = true, // متغير البيئة يُعتبر آمناً
                IsSecure = true,
                Description = $"نص الاتصال مقروء من متغير البيئة {EnvironmentVariableName}"
            };
        }

        var configValue = configuration.GetConnectionString(connectionName);
        if (string.IsNullOrWhiteSpace(configValue))
        {
            return new ConnectionStringSecurityStatus
            {
                Source = "غير موجود",
                IsEncrypted = false,
                IsSecure = false,
                Description = "نص الاتصال غير موجود في التهيئة أو متغير البيئة"
            };
        }

        if (ConnectionStringProtector.IsEncrypted(configValue))
        {
            var scope = configValue.StartsWith(ConnectionStringProtector.EncryptedMachinePrefix)
                ? "نطاق الجهاز (LocalMachine)"
                : "نطاق المستخدم (CurrentUser)";

            return new ConnectionStringSecurityStatus
            {
                Source = "appsettings.json (مشفر)",
                IsEncrypted = true,
                IsSecure = true,
                Description = $"نص الاتصال مشفر بـ DPAPI — {scope}"
            };
        }

        return new ConnectionStringSecurityStatus
        {
            Source = "appsettings.json (نص عادي)",
            IsEncrypted = false,
            IsSecure = false,
            Description = "⚠️ نص الاتصال بصيغة نص عادي — يُنصح بالتشفير للإنتاج"
        };
    }
}

/// <summary>
/// حالة أمان نص الاتصال — تُستخدم في شاشات التشخيص.
/// </summary>
public class ConnectionStringSecurityStatus
{
    public string Source { get; set; } = string.Empty;
    public bool IsEncrypted { get; set; }
    public bool IsSecure { get; set; }
    public string Description { get; set; } = string.Empty;
}
