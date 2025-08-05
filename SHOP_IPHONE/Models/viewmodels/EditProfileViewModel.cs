using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace SHOP_IPHONE.Models
{
    public class EditProfileViewModel
    {
        public int account_id { get; set; }

        [Required(ErrorMessage = "Họ và tên không được để trống")]
        [RegularExpression(@"^([A-ZÀ-Ỵ][a-zà-ỹ]+)(\s[A-ZÀ-Ỵ][a-zà-ỹ]+)*$",
  ErrorMessage = "Viết hoa chữ cái đầu, không chứa số/ký tự đặc biệt")]
        public string fullname { get; set; }

        [Required(ErrorMessage = "Tên đăng nhập không được để trống")]
        public string username { get; set; }

        [Required(ErrorMessage = "Email không được để trống")]
        [EmailAddress(ErrorMessage = "Email không đúng định dạng")]
        public string email { get; set; }

        [Required(ErrorMessage = "Số điện thoại không được để trống")]
        [RegularExpression(@"^\d{10}$", ErrorMessage = "Số điện thoại phải đúng 10 số")]
        public string phone { get; set; }

        [Required(ErrorMessage = "Địa chỉ không được để trống")]
        public string address { get; set; }

        // Phần đổi mật khẩu
        [DataType(DataType.Password)]
        public string CurrentPassword { get; set; }

        [StringLength(20, MinimumLength = 5, ErrorMessage = "Mật khẩu từ 5 đến 20 ký tự")]
        [RegularExpression(@"^(?=.*[A-Za-z])(?=.*\d)(?=.*[@$!%*?&]).+$", ErrorMessage = "Phải có chữ, số, ký tự đặc biệt")]
        [DataType(DataType.Password)]
        public string NewPassword { get; set; }

        [Compare("NewPassword", ErrorMessage = "Mật khẩu xác nhận không đúng")]
        [DataType(DataType.Password)]
        public string ConfirmPassword { get; set; }
    }
}
