using DataAccess.Common;
using DataAccess.DTOs;
using Domain.Models;

namespace DataAccess.Validation
{
    /// <summary>
    /// أنبوب تحقق مركزي — يجمع أخطاء المدخلات ويعيد Result.Success/Fail
    /// بدلاً من رمي استثناءات، لتجنب الضغط على الموارد.
    /// </summary>
    public static class ValidationPipeline
    {
        public static Result ValidateGuest(GuestDto dto)
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(dto.FullName))
                errors.Add("الاسم الكامل للضيف مطلوب.");

            if (string.IsNullOrWhiteSpace(dto.PhoneNumber) && string.IsNullOrWhiteSpace(dto.EmailAddress))
                errors.Add("يجب إدخال رقم الهاتف أو البريد الإلكتروني على الأقل للتواصل.");

            return BuildResult(errors);
        }

        public static Result ValidateUser(User dto, bool isNew)
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(dto.FullName))
                errors.Add("اسم المستخدم مطلوب.");

            if (string.IsNullOrWhiteSpace(dto.Username))
                errors.Add("اسم المستخدم مطلوب.");

            if (isNew && string.IsNullOrWhiteSpace(dto.PasswordHash))
                errors.Add("كلمة المرور مطلوبة للمستخدم الجديد.");

            if (dto.RoleId <= 0)
                errors.Add("يجب تحديد دور وظيفي للمستخدم.");

            return BuildResult(errors);
        }

        public static Result ValidateCorrespondent(CorrespondentDto dto)
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(dto.FullName))
                errors.Add("اسم المراسل مطلوب.");

            if (string.IsNullOrWhiteSpace(dto.PhoneNumber))
                errors.Add("رقم هاتف المراسل مطلوب.");

            return BuildResult(errors);
        }

        public static Result ValidateCoverage(CoverageDto dto)
        {
            var errors = new List<string>();

            if (dto.CorrespondentId <= 0)
                errors.Add("يرجى اختيار المراسل المسؤول.");

            if (string.IsNullOrWhiteSpace(dto.Topic))
                errors.Add("يرجى إدخال موضوع التغطية.");

            return BuildResult(errors);
        }

        public static Result ValidateEpisode(EpisodeDto dto)
        {
            var errors = new List<string>();

            if (dto.ProgramId <= 0)
                errors.Add("يرجى اختيار البرنامج من القائمة المنسدلة.");

            if (string.IsNullOrWhiteSpace(dto.EpisodeName))
                errors.Add("عنوان الحلقة مطلوب ولا يمكن تركه فارغاً.");

            if (dto.ScheduledDate is null)
                errors.Add("يرجى تحديد تاريخ تنفيذ الحلقة.");

            return BuildResult(errors);
        }

        public static Result ValidateProgram(ProgramDto dto)
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(dto.ProgramName))
                errors.Add("اسم البرنامج مطلوب.");

            return BuildResult(errors);
        }

        public static Result ValidateUser(UserDto dto, string? password)
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(dto.FullName))
                errors.Add("اسم المستخدم مطلوب.");

            if (string.IsNullOrWhiteSpace(dto.Username))
                errors.Add("اسم الدخول مطلوب.");

            if (dto.RoleId <= 0)
                errors.Add("يرجى اختيار دور للمستخدم.");

            if (dto.UserId == 0 && string.IsNullOrWhiteSpace(password))
                errors.Add("كلمة المرور مطلوبة للمستخدم الجديد.");

            if (!string.IsNullOrWhiteSpace(password) && password.Length < 6)
                errors.Add("كلمة المرور يجب أن تكون 6 أحرف على الأقل.");

            return BuildResult(errors);
        }

        #region Infrastructure

        private static Result BuildResult(List<string> errors)
        {
            if (errors.Count > 0)
                return Result.Fail(string.Join(Environment.NewLine, errors));

            return Result.Success();
        }

        #endregion
    }
}