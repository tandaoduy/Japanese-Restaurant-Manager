using System;
using System.Web;
using System.Web.Mvc;
using Project_65133141.Filters;

namespace Project_65133141.Areas.Employee_65133141.Controllers
{
    [RoleAuthorize("employee", "admin")]
    public class SessionController : Controller
    {
        // POST: Employee_65133141/Session/Extend
        [HttpPost]
        public JsonResult Extend()
        {
            try
            {
                if (!User.Identity.IsAuthenticated)
                {
                    return Json(new { success = false, message = "Chưa đăng nhập" });
                }

                // Extend session by 60 minutes
                Session["SessionExpiryTime"] = DateTime.Now.AddMinutes(60);
                Session.Timeout = 60;

                // Extend authentication cookie
                var ticket = new System.Web.Security.FormsAuthenticationTicket(
                    1,
                    User.Identity.Name,
                    DateTime.Now,
                    DateTime.Now.AddMinutes(60),
                    false, // NOT persistent
                    Session["UserRole"] as string ?? "",
                    System.Web.Security.FormsAuthentication.FormsCookiePath
                );
                var encryptedTicket = System.Web.Security.FormsAuthentication.Encrypt(ticket);
                var cookie = new HttpCookie(System.Web.Security.FormsAuthentication.FormsCookieName, encryptedTicket)
                {
                    HttpOnly = true,
                    Secure = false,
                    Path = System.Web.Security.FormsAuthentication.FormsCookiePath
                };
                Response.Cookies.Add(cookie);

                return Json(new { success = true, message = "Đã mở rộng thời gian làm việc", expiryTime = DateTime.Now.AddMinutes(60) });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi: " + ex.Message });
            }
        }

        // GET: Employee_65133141/Session/Check
        [HttpGet]
        public JsonResult Check()
        {
            try
            {
                if (!User.Identity.IsAuthenticated)
                {
                    return Json(new { authenticated = false }, JsonRequestBehavior.AllowGet);
                }

                var expiryTime = Session["SessionExpiryTime"] as DateTime?;
                if (!expiryTime.HasValue)
                {
                    expiryTime = DateTime.Now.AddMinutes(60);
                    Session["SessionExpiryTime"] = expiryTime;
                }

                var timeRemaining = expiryTime.Value.Subtract(DateTime.Now).TotalSeconds; // Use seconds for 60 minutes timeout
                var isExpiringSoon = timeRemaining <= 1800 && timeRemaining > 0; // 30 minutes warning (half of 60 minutes)
                var isExpired = timeRemaining <= 0;

                return Json(new
                {
                    authenticated = true,
                    expiryTime = expiryTime.Value,
                    timeRemaining = timeRemaining, // in seconds
                    timeRemainingMinutes = timeRemaining / 60.0, // in minutes for display
                    isExpiringSoon = isExpiringSoon,
                    isExpired = isExpired
                }, JsonRequestBehavior.AllowGet);
            }
            catch
            {
                return Json(new { authenticated = false }, JsonRequestBehavior.AllowGet);
            }
        }
    }
}

