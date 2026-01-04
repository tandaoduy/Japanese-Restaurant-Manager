using System;
using System.Web;
using System.Web.Mvc;
using Project_65133141.Filters;

namespace Project_65133141.Areas.Employee_65133141.Controllers
{
    /// <summary>
    /// Base controller for Employee area - handles errors to stay in area
    /// </summary>
    [AreaErrorHandler]
    public abstract class BaseAreaController : Controller
    {
        protected override void OnException(ExceptionContext filterContext)
        {
            if (!filterContext.ExceptionHandled)
            {
                // Log the error (you can add logging here)
                var exception = filterContext.Exception;
                System.Diagnostics.Debug.WriteLine($"Employee Area Error: {exception.Message}");
                
                // Check if user is properly authenticated before redirecting to Employee area
                var userRole = filterContext.HttpContext.Session["UserRole"] as string;
                var isAuthenticated = filterContext.HttpContext.User.Identity.IsAuthenticated;
                
                if (!isAuthenticated || string.IsNullOrEmpty(userRole))
                {
                    // User is not properly authenticated, redirect to main Home to prevent loop
                    filterContext.Result = RedirectToAction("Index", "Home", new { area = "", signedOut = "1" });
                    filterContext.ExceptionHandled = true;
                    return;
                }
                
                // Stay in Employee area, redirect to Home/Index of Employee area
                filterContext.Result = RedirectToAction("Index", "Home", new { area = "Employee_65133141" });
                filterContext.ExceptionHandled = true;
            }
            
            base.OnException(filterContext);
        }
    }
}




