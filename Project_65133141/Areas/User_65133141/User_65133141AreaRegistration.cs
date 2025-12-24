using System.Web.Mvc;

namespace Project_65133141.Areas.User_65133141
{
    public class User_65133141AreaRegistration : AreaRegistration 
    {
        public override string AreaName 
        {
            get 
            {
                return "User_65133141";
            }
        }

        public override void RegisterArea(AreaRegistrationContext context) 
        {
            context.MapRoute(
                "User_65133141_default",
                "User_65133141/{controller}/{action}/{id}",
                new { action = "Index", id = UrlParameter.Optional }
            );
        }
    }
}