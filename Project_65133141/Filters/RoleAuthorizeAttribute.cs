using System;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;

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
            // Prevent caching of this response
            filterContext.HttpContext.Response.Cache.SetCacheability(System.Web.HttpCacheability.NoCache);
            filterContext.HttpContext.Response.Cache.SetNoStore();
            
            // Khi truy cập sai role hoặc chưa đăng nhập, luôn đăng xuất và đưa về Home/Index
            System.Web.Security.FormsAuthentication.SignOut();

            var session = filterContext.HttpContext.Session;
            if (session != null)
            {
                session.Clear();
                session.Abandon();
            }

            // Xoá cookie xác thực nếu có
            var authCookieName = System.Web.Security.FormsAuthentication.FormsCookieName;
            if (filterContext.HttpContext.Request.Cookies[authCookieName] != null)
            {
                var cookie = new HttpCookie(authCookieName)
                {
                    Expires = DateTime.Now.AddDays(-1),
                    Value = string.Empty
                };
                filterContext.HttpContext.Response.Cookies.Add(cookie);
            }

            // Use direct URL redirect to avoid routing issues
            var homeUrl = System.Web.VirtualPathUtility.ToAbsolute("~/Home/Index") + "?signedOut=1";
            filterContext.Result = new RedirectResult(homeUrl, permanent: false);
        }
    }
}





























