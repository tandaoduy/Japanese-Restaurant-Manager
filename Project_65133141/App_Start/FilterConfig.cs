using System.Web;
using System.Web.Mvc;
using Project_65133141.Filters;

namespace Project_65133141
{
    public class FilterConfig
    {
        public static void RegisterGlobalFilters(GlobalFilterCollection filters)
        {
            // Use custom area error handler instead of default
            filters.Add(new AreaErrorHandlerAttribute());
        }
    }
}
