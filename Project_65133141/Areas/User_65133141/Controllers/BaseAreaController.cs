using System;
using System.Web;
using System.Web.Mvc;
using Project_65133141.Filters;

namespace Project_65133141.Areas.User_65133141.Controllers
{
    /// <summary>
    /// Base controller for User area - handles errors to stay in area
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
                System.Diagnostics.Debug.WriteLine($"User Area Error: {exception.Message}");
                
                // Stay in User area, redirect to Home/Index of User area
                filterContext.Result = RedirectToAction("Index", "Home", new { area = "User_65133141" });
                filterContext.ExceptionHandled = true;
            }
            
            base.OnException(filterContext);
        }
    }
}




