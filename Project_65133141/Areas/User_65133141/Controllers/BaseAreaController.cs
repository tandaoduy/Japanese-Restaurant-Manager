using System;
using System.Web;
using System.Web.Mvc;
using Project_65133141.Filters;

namespace Project_65133141.Areas.User_65133141.Controllers
{
    /// <summary>
    /// Base controller for User area - handles errors to stay in area
    /// </summary>
    // TEMPORARILY DISABLED - [AreaErrorHandler]
    public abstract class BaseAreaController : Controller
    {
        // TEMPORARILY DISABLED - OnException was causing redirect loop when database errors occurred
        /*
        protected override void OnException(ExceptionContext filterContext)
        {
            if (!filterContext.ExceptionHandled)
            {
                // Log the error (you can add logging here)
                var exception = filterContext.Exception;
                System.Diagnostics.Debug.WriteLine($"User Area Error: {exception.Message}");
                
                // Check if user is properly authenticated before redirecting to User area
                var userRole = filterContext.HttpContext.Session["UserRole"] as string;
                var isAuthenticated = filterContext.HttpContext.User.Identity.IsAuthenticated;
                
                if (!isAuthenticated || string.IsNullOrEmpty(userRole))
                {
                    // User is not properly authenticated, redirect to main Home to prevent loop
                    filterContext.Result = RedirectToAction("Index", "Home", new { area = "", signedOut = "1" });
                    filterContext.ExceptionHandled = true;
                    return;
                }
                
                // Stay in User area, redirect to Home/Index of User area
                filterContext.Result = RedirectToAction("Index", "Home", new { area = "User_65133141" });
                filterContext.ExceptionHandled = true;
            }
            
            base.OnException(filterContext);
        }
        */
    }
}




