namespace DataAccess.Common
{
    public class ConcurrencyException : Exception
    {
        // قاموس يحتوي على اسم الحقل والقيمة الموجودة حالياً في الداتابيز
        public Dictionary<string, object?> DatabaseValues { get; }

        public ConcurrencyException(Dictionary<string, object?> databaseValues)
            : base("قام مستخدم آخر بتعديل هذه البيانات أثناء قيامك بالتحرير.")
        {
            DatabaseValues = databaseValues;
        }
    }
}
