namespace Radio.Messaging;

public static class Messages
{
    public static string Added(string entityType, string entityName)
        => $"تم إضافة {entityType} «{entityName}» بنجاح.";

    public static string Updated(string entityType, string entityName)
        => $"تم تعديل {entityType} «{entityName}» بنجاح.";

    public static string Deleted(string entityType, string entityName)
        => $"تم حذف {entityType} «{entityName}» بنجاح.";

    public static string Actioned(string action, string entityType)
        => $"تم {action} {entityType} بنجاح.";

    public static string ActionedWithName(string action, string entityType, string entityName)
        => $"تم {action} {entityType} «{entityName}» بنجاح.";

    public static string BatchActioned(string action, int success, int fail = 0)
        => fail > 0
            ? $"تم {action} {success}، تعذر {action} {fail}."
            : $"تم {action} {success} بنجاح.";

    public static string Reverted(string entityType, string entityName)
        => $"تم التراجع عن حالة {entityType} «{entityName}» بنجاح.";

    public static string Cancelled(string entityType, string entityName)
        => $"تم إلغاء {entityType} «{entityName}» بنجاح.";
}
