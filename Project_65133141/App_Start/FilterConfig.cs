using System.Web;
using System.Web.Mvc;
using Project_65133141.Filters;

namespace Project_65133141
{
    public class FilterConfig
    {
        public static void RegisterGlobalFilters(GlobalFilterCollection filters)
        {
            // TEMPORARILY DISABLED - global error handler was contributing to redirect loop
            // filters.Add(new AreaErrorHandlerAttribute());
        }
    }
}
