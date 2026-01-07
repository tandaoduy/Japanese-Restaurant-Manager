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
            // Route chi tiết tin tức theo slug: /User_65133141/TinTuc/Details/{slug}
            context.MapRoute(
                "User_65133141_TinTuc_Details",
                "User_65133141/TinTuc/Details/{slug}",
                new { controller = "TinTuc", action = "Details", slug = UrlParameter.Optional }
            );

            context.MapRoute(
                "User_65133141_default",
                "User_65133141/{controller}/{action}/{id}",
                new { action = "Index", id = UrlParameter.Optional }
            );
        }
    }
}