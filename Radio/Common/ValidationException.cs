namespace Radio.Common
{
    /// <summary>
    /// استثناء مخصص لأخطاء التحقق من البيانات
    /// </summary>
    public class ValidationException : Exception
    {
        public List<string> Errors { get; }

        public ValidationException(string message) : base(message)
        {
            Errors = new List<string> { message };
        }

        public ValidationException(List<string> errors) : base("حدثت أخطاء في التحقق من البيانات.")
        {
            Errors = errors;
        }
    }
}
