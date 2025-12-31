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
            
            // Resilience: If Session is empty but User is authenticated, recover role from Auth Ticket
            if (string.IsNullOrEmpty(userRole) && httpContext.User.Identity is System.Web.Security.FormsIdentity formsIdentity)
            {
                var ticket = formsIdentity.Ticket;
                if (ticket != null && !string.IsNullOrEmpty(ticket.UserData))
                {
                     userRole = ticket.UserData;
                     // Restore to session
                     httpContext.Session["UserRole"] = userRole;
                     httpContext.Session["UserName"] = httpContext.User.Identity.Name;
                     // Log for debugging if needed: System.Diagnostics.Debug.WriteLine($"Recovered role {userRole} from ticket for {httpContext.User.Identity.Name}");
                }
            }

            if (string.IsNullOrEmpty(userRole))
            {
                return false;
            }

            // Check if user role matches any allowed role
            string roleLower = userRole.ToLower().Trim();
            foreach (var allowedRole in _allowedRoles)
            {
                string allowedRoleLower = allowedRole.ToLower().Trim();
                
                // Exact match
                if (roleLower == allowedRoleLower)
                {
                    return true;
                }
                
                // Special handling for employee role variations
                if (allowedRoleLower == "employee")
                {
                    if (roleLower == "nhân viên" || roleLower == "nhan vien" || 
                        roleLower.Contains("nhân viên") || roleLower.Contains("nhan vien") ||
                        roleLower.Contains("employee"))
                    {
                        return true;
                    }
                }
                
                // Special handling for admin role variations
                if (allowedRoleLower == "admin")
                {
                    if (roleLower == "admin" || roleLower == "administrator" || 
                        roleLower.Contains("admin"))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        protected override void HandleUnauthorizedRequest(AuthorizationContext filterContext)
        {
            // Get user role to determine redirect URL
            var userRole = filterContext.HttpContext.Session["UserRole"] as string;
            string redirectUrl = "~/Home/Index"; // Default redirect to home

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

            // Redirect directly to home page without alert
            filterContext.Result = new RedirectResult(redirectUrl);
        }
    }
}





























