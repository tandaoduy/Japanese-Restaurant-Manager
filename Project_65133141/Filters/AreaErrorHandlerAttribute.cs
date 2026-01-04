using System;
using System.Web;
using System.Web.Mvc;

namespace Project_65133141.Filters
{
    /// <summary>
    /// Custom error handler for areas - keeps user in the same area when errors occur
    /// </summary>
    public class AreaErrorHandlerAttribute : HandleErrorAttribute
    {
        public override void OnException(ExceptionContext filterContext)
        {
            if (!filterContext.ExceptionHandled)
            {
                var area = filterContext.RouteData.DataTokens["area"] as string;
                
                // If we're in an area, stay in that area
                if (!string.IsNullOrEmpty(area))
                {
                    // Log the error
                    var exception = filterContext.Exception;
                    System.Diagnostics.Debug.WriteLine($"{area} Area Error: {exception.Message}");
                    
                    // Check if this is an authorization-related issue - if so, redirect to main Home
                    // to prevent redirect loops with RoleAuthorize
                    var userRole = filterContext.HttpContext.Session["UserRole"] as string;
                    var isAuthenticated = filterContext.HttpContext.User.Identity.IsAuthenticated;
                    
                    if (!isAuthenticated || string.IsNullOrEmpty(userRole))
                    {
                        // User is not properly authenticated, redirect to main Home
                        filterContext.Controller.TempData["ErrorMessage"] = "Vui lòng đăng nhập để truy cập.";
                        filterContext.Result = new RedirectToRouteResult(
                            new System.Web.Routing.RouteValueDictionary(
                                new
                                {
                                    area = "",
                                    controller = "Home",
                                    action = "Index",
                                    signedOut = "1"
                                }
                            )
                        );
                        filterContext.ExceptionHandled = true;
                        return;
                    }
                    
                    // Set error message in TempData
                    filterContext.Controller.TempData["ErrorMessage"] = "Đã xảy ra lỗi. Vui lòng thử lại.";
                    
                    // Redirect to Home/Index of the same area
                    filterContext.Result = new RedirectToRouteResult(
                        new System.Web.Routing.RouteValueDictionary(
                            new
                            {
                                area = area,
                                controller = "Home",
                                action = "Index"
                            }
                        )
                    );
                    
                    filterContext.ExceptionHandled = true;
                    return;
                }
                
                // If not in an area, redirect to main Home
                filterContext.Controller.TempData["ErrorMessage"] = "Đã xảy ra lỗi. Vui lòng thử lại.";
                filterContext.Result = new RedirectToRouteResult(
                    new System.Web.Routing.RouteValueDictionary(
                        new
                        {
                            controller = "Home",
                            action = "Index"
                        }
                    )
                );
                filterContext.ExceptionHandled = true;
            }
        }
    }
}

