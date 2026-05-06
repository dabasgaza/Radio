namespace DataAccess.DTOs;

/// <summary>
/// سجل نشر مُوحّد — يُستخدم لعرض أي نوع من سجلات النشر
/// (تنفيذ / سوشال ميديا / موقع إلكتروني) في قائمة واحدة
/// دون الحاجة لمعرفة النوع مسبقاً
/// </summary>
public record PublishingRecordDto
{
    /// <summary>معرّف السجل في جدوله الأصلي</summary>
    public int RecordId { get; init; }

    /// <summary>نوع السجل: "Execution" أو "SocialMedia" أو "Website"</summary>
    public string RecordType { get; init; } = string.Empty;

    /// <summary>معرّف الحلقة المرتبط بها السجل</summary>
    public int EpisodeId { get; init; }

    /// <summary>اسم الحلقة — لعرضه في القائمة الشاملة</summary>
    public string? EpisodeName { get; init; }

    /// <summary>اسم البرنامج — لعرضه في القائمة الشاملة</summary>
    public string? ProgramName { get; init; }

    /// <summary>ملخص قصير: اسم الضيف أو عنوان المقطع أو عنوان النشر</summary>
    public string? Summary { get; init; }

    /// <summary>تاريخ إنشاء السجل</summary>
    public DateTime RecordDate { get; init; }

    /// <summary>اسم المستخدم الذي أنشأ السجل</summary>
    public string? RecordedBy { get; init; }

    /// <summary>أيقونة نوع السجل — تُستخدم في الواجهة</summary>
    public string RecordIcon { get; init; } = "ClipboardTextOutline";

    /// <summary>لون نوع السجل — يُستخدم في الواجهة</summary>
    public string RecordColor { get; init; } = "#757575";
}
