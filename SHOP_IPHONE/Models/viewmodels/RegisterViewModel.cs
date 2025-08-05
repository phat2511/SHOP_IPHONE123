using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace SHOP_IPHONE.ViewModels
{
    public class RegisterViewModel
    {
        [Required]
        [RegularExpression(@"^([A-ZÀ-Ỵ][a-zà-ỹ]+)(\s[A-ZÀ-Ỵ][a-zà-ỹ]+)*$",
    ErrorMessage = "Viết hoa chữ cái đầu, không chứa số/ký tự đặc biệt")]
        [Display(Name = "Họ và tên")]
        public string FullName { get; set; }

        [Required]
        [Display(Name = "Tên đăng nhập")]
        public string username { get; set; }

        [Required, EmailAddress]
        public string email { get; set; }

        [Required]
        [StringLength(20, MinimumLength = 5)]
        [RegularExpression(@"^(?=.*[A-Za-z])(?=.*\d)(?=.*[@$!%*?&]).{5,20}$",
            ErrorMessage = "Phải có chữ, số, ký tự đặc biệt")]
        public string password { get; set; }

        [Compare("password", ErrorMessage = "Mật khẩu nhập lại không đúng")]
        [Display(Name = "Nhập lại mật khẩu")]
        public string confirmPassword { get; set; }

        [Required]
        [RegularExpression(@"^\d{10}$", ErrorMessage = "Số điện thoại phải đúng 10 số")]
        [Display(Name = "Số điện thoại")]
        public string phone { get; set; }

        public string address { get; set; }
    }
}