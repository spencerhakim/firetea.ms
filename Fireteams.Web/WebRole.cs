using Fireteams.Web;
using Microsoft.AspNet.SignalR;
using Microsoft.Owin;
using Microsoft.WindowsAzure.ServiceRuntime;
using Owin;

[assembly: OwinStartup(typeof(WebRole))]
namespace Fireteams.Web
{
    public class WebRole : RoleEntryPoint
    {
        public void Configuration(IAppBuilder app)
        {
            app.MapSignalR(new HubConfiguration
            {
#if DEBUG
                EnableDetailedErrors = true
#endif
            });
        }
    }
}
