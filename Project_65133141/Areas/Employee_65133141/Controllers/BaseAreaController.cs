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
                
                // Stay in Employee area, redirect to Home/Index of Employee area
                filterContext.Result = RedirectToAction("Index", "Home", new { area = "Employee_65133141" });
                filterContext.ExceptionHandled = true;
            }
            
            base.OnException(filterContext);
        }
    }
}




