using System.Web.Mvc;

namespace Project_65133141.Areas.Admin_65133141
{
    public class Admin_65133141AreaRegistration : AreaRegistration 
    {
        public override string AreaName 
        {
            get 
            {
                return "Admin_65133141";
            }
        }

        public override void RegisterArea(AreaRegistrationContext context) 
        {
            context.MapRoute(
                "Admin_65133141_default",
                "Admin_65133141/{controller}/{action}/{id}",
                new { action = "Index", id = UrlParameter.Optional }
            );
        }
    }
}