using DataAccess.DTOs;
using Domain.Models;
using System.ComponentModel.DataAnnotations;

namespace DataAccess.Validation
{
    /// <summary>
    /// أنبوب تحقق مركزي — يجمع أخطاء المدخلات ويطرح ValidationException
    /// عند وجود أي خطأ. يُستدعى من طبقة العرض (Code-Behind) قبل إرسال الـ DTO للـ Service.
    /// </summary>
    public static class ValidationPipeline
    {
        /// <summary>
        /// التحقق من بيانات الضيف.
        /// </summary>
        public static void ValidateGuest(GuestDto dto)
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(dto.FullName))
                errors.Add("الاسم الكامل للضيف مطلوب.");

            if (string.IsNullOrWhiteSpace(dto.PhoneNumber) && string.IsNullOrWhiteSpace(dto.EmailAddress))
                errors.Add("يجب إدخال رقم الهاتف أو البريد الإلكتروني على الأقل للتواصل.");

            ThrowIfHasErrors(errors);
        }

        /// <summary>
        /// التحقق من بيانات المستخدم.
        /// </summary>
        public static void ValidateUser(User dto, bool isNew)
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

            ThrowIfHasErrors(errors);
        }

        /// <summary>
        /// التحقق من بيانات المراسل.
        /// </summary>
        public static void ValidateCorrespondent(CorrespondentDto dto)
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(dto.FullName))
                errors.Add("اسم المراسل مطلوب.");

            if (string.IsNullOrWhiteSpace(dto.PhoneNumber))
                errors.Add("رقم هاتف المراسل مطلوب.");

            ThrowIfHasErrors(errors);
        }

        /// <summary>
        /// التحقق من بيانات التغطية الميدانية.
        /// </summary>
        public static void ValidateCoverage(CoverageDto dto)
        {
            var errors = new List<string>();

            if (dto.CorrespondentId <= 0)
                errors.Add("يرجى اختيار المراسل المسؤول.");

            if (string.IsNullOrWhiteSpace(dto.Topic))
                errors.Add("يرجى إدخال موضوع التغطية.");

            ThrowIfHasErrors(errors);
        }

        #region Shared Infrastructure

        /// <summary>
        /// يطرح ValidationException إذا كانت هناك أخطاء.
        /// </summary>
        private static void ThrowIfHasErrors(List<string> errors)
        {
            if (errors.Count > 0)
                throw new ValidationException(string.Join(Environment.NewLine, errors));
        }

        public static void ValidateEpisode(EpisodeDto dto)
        {
            var errors = new List<string>();

            if (dto.ProgramId <= 0)
                errors.Add("يرجى اختيار البرنامج من القائمة المنسدلة.");

            if (string.IsNullOrWhiteSpace(dto.EpisodeName))
                errors.Add("عنوان الحلقة مطلوب ولا يمكن تركه فارغاً.");

            if (dto.ScheduledTime is null)
                errors.Add("يرجى تحديد تاريخ ووقت تنفيذ الحلقة.");

            ThrowIfHasErrors(errors);
        }
        public static void ValidateProgram(ProgramDto dto)
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(dto.ProgramName))
                errors.Add("اسم البرنامج مطلوب.");

            ThrowIfHasErrors(errors);

        }
        /// <summary>
        /// التحقق من بيانات المستخدم قبل الحفظ.
        /// </summary>
        public static void ValidateUser(UserDto dto, string? password)
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(dto.FullName))
                errors.Add("اسم المستخدم مطلوب.");

            if (string.IsNullOrWhiteSpace(dto.Username))
                errors.Add("اسم الدخول مطلوب.");

            if (dto.RoleId <= 0)
                errors.Add("يرجى اختيار دور للمستخدم.");

            // كلمة المرور مطلوبة فقط عند الإضافة
            if (dto.UserId == 0 && string.IsNullOrWhiteSpace(password))
                errors.Add("كلمة المرور مطلوبة للمستخدم الجديد.");

            // إذا أُدخلت كلمة مرور، تحقق من قوتها
            if (!string.IsNullOrWhiteSpace(password) && password.Length < 6)
                errors.Add("كلمة المرور يجب أن تكون 6 أحرف على الأقل.");

            ThrowIfHasErrors(errors);
        }

        #endregion
    }
}