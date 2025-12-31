using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace Project_65133141.Models.Form
{
    public class LoginForm
    {
        [Required(ErrorMessage = "Email/Tên đăng nhập là bắt buộc")]
        [Display(Name = "Email/Tên đăng nhập")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Mật khẩu là bắt buộc")]
        [Display(Name = "Mật khẩu")]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        [Display(Name = "Ghi nhớ đăng nhập")]
        public bool RememberMe { get; set; }

        [Required(ErrorMessage = "Mã CAPTCHA là bắt buộc")]
        [Display(Name = "Mã CAPTCHA")]
        public string CaptchaCode { get; set; }
    }
}