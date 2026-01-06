using System.Web.Mvc;

namespace Project_65133141.Areas.Employee_65133141
{
    public class Employee_65133141AreaRegistration : AreaRegistration 
    {
        public override string AreaName 
        {
            get 
            {
                return "Employee_65133141";
            }
        }

        public override void RegisterArea(AreaRegistrationContext context) 
        {
            // Explicit route for employee profile to avoid any ambiguity
            context.MapRoute(
                "Employee_65133141_Account_Profile",
                "Employee_65133141/Account/Profile",
                new { controller = "Account", action = "Profile" },
                new[] { "Project_65133141.Areas.Employee_65133141.Controllers" }
            );

            context.MapRoute(
                "Employee_65133141_default",
                "Employee_65133141/{controller}/{action}/{id}",
                new { action = "Index", id = UrlParameter.Optional },
                new[] { "Project_65133141.Areas.Employee_65133141.Controllers" }
            );
        }
    }
}