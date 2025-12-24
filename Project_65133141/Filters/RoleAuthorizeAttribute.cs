using System;
using System.Web;
using System.Web.Mvc;

namespace Project_65133141.Filters
{
    public class RoleAuthorizeAttribute : AuthorizeAttribute
    {
        private readonly string[] _allowedRoles;

        public RoleAuthorizeAttribute(params string[] allowedRoles)
        {
            _allowedRoles = allowedRoles ?? new string[0];
        }

        protected override bool AuthorizeCore(HttpContextBase httpContext)
        {
            if (!httpContext.User.Identity.IsAuthenticated)
            {
                return false;
            }

            var userRole = httpContext.Session["UserRole"] as string;
            if (string.IsNullOrEmpty(userRole))
            {
                return false;
            }

            // Check if user role matches any allowed role
            string roleLower = userRole.ToLower().Trim();
            foreach (var allowedRole in _allowedRoles)
            {
                if (roleLower == allowedRole.ToLower().Trim())
                {
                    return true;
                }
            }

            return false;
        }

        protected override void HandleUnauthorizedRequest(AuthorizationContext filterContext)
        {
            // Get user role to determine redirect URL
            var userRole = filterContext.HttpContext.Session["UserRole"] as string;
            string redirectUrl = "~/Home/Index"; // Default redirect

            if (!string.IsNullOrEmpty(userRole))
            {
                string roleLower = userRole.ToLower().Trim();
                if (roleLower == "admin")
                {
                    redirectUrl = "~/Admin_65133141/Home/Index";
                }
                else if (roleLower == "nhân viên" || roleLower == "nhan vien" || roleLower == "employee")
                {
                    redirectUrl = "~/Employee_65133141/Home/Index";
                }
                else if (roleLower == "khách hàng" || roleLower == "khach hang" || roleLower == "user")
                {
                    redirectUrl = "~/User_65133141/Home/Index";
                }
            }

            // Create HTML with alert and redirect
            string errorMessage = "Bạn không có quyền truy cập trang này. Vui lòng đăng nhập với tài khoản phù hợp.";
            string html = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8' />
    <title>Không có quyền truy cập</title>
</head>
<body>
    <script>
        alert('{errorMessage.Replace("'", "\\'")}');
        window.location.href = '{redirectUrl}';
    </script>
</body>
</html>";

            // Return HTML content with alert
            filterContext.Result = new ContentResult
            {
                Content = html,
                ContentType = "text/html; charset=utf-8"
            };
        }
    }
}





















