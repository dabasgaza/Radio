using DataAccess.DTOs;
using Domain.Models;
using System.ComponentModel.DataAnnotations;

namespace DataAccess.Validation
{
    public static class ValidationPipeline
    {
        // التحقق من بيانات الضيف
        public static void ValidateGuest(GuestDto dto)
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(dto.FullName))
                errors.Add("الاسم الكامل للضيف مطلوب.");

            if (string.IsNullOrWhiteSpace(dto.PhoneNumber) && string.IsNullOrWhiteSpace(dto.EmailAddress))
                errors.Add("يجب إدخال رقم الهاتف أو البريد الإلكتروني على الأقل للتواصل.");

            if (errors.Count > 0) throw new ValidationException(string.Join(Environment.NewLine, errors));
        }

        // التحقق من بيانات المستخدم
        public static void ValidateUser(User dto, bool isNew)
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(dto.FullName)) errors.Add("اسم المستخدم مطلوب.");
            if (string.IsNullOrWhiteSpace(dto.Username)) errors.Add("اسم المستخدم مطلوب.");
            if (isNew && string.IsNullOrWhiteSpace(dto.PasswordHash)) errors.Add("كلمة المرور مطلوبة للمستخدم الجديد.");
            if (dto.RoleId <= 0) errors.Add("يجب تحديد دور وظيفي للمستخدم.");

            if (errors.Count > 0) throw new ValidationException(string.Join(Environment.NewLine, errors));
        }

        // التحقق من بيانات المستخدم
        //public static void ValidateUser(UserDto dto, bool isNew)
        //{
        //    var errors = new List<string>();

        //    if (string.IsNullOrWhiteSpace(dto.FullName)) errors.Add("اسم الموظف مطلوب.");
        //    if (string.IsNullOrWhiteSpace(dto.Username)) errors.Add("اسم المستخدم مطلوب.");
        //    if (isNew && string.IsNullOrWhiteSpace(dto.Password)) errors.Add("كلمة المرور مطلوبة للمستخدم الجديد.");
        //    if (dto.RoleId <= 0) errors.Add("يجب تحديد دور وظيفي للمستخدم.");

        //    if (errors.Count > 0) throw new ValidationException(errors);
        //}

    }
}
