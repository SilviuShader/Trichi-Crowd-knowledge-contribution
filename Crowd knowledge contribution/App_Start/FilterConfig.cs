using System.Web;
using System.Web.Mvc;

namespace Crowd_knowledge_contribution
{
    public class FilterConfig
    {
        public static void RegisterGlobalFilters(GlobalFilterCollection filters)
        {
            filters.Add(new HandleErrorAttribute());
        }
    }
}
