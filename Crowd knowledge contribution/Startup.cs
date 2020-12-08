using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(Crowd_knowledge_contribution.Startup))]
namespace Crowd_knowledge_contribution
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
        }
    }
}
