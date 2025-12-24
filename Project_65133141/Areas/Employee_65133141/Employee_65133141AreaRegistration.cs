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
            context.MapRoute(
                "Employee_65133141_default",
                "Employee_65133141/{controller}/{action}/{id}",
                new { action = "Index", id = UrlParameter.Optional }
            );
        }
    }
}