using System;
using System.Web;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;
using Fireteams.Common;
using Fireteams.Common.Services;
using Microsoft.AspNet.SignalR;
using Microsoft.WindowsAzure.ServiceRuntime;
using NLog;

namespace Fireteams.Web
{
    public class Global : HttpApplication
    {
        //background workers
        private static QueueProcessor _queue = DI.GetInstance<QueueProcessor>();
        private static SchemaTrimmer _trimmer = DI.GetInstance<SchemaTrimmer>();

        private Logger _log = DI.GetInstance<Logger>();

        void Application_Start(object sender, EventArgs e)
        {
            _log.Info("Starting up... " + RoleEnvironment.CurrentRoleInstance.Id);

            //testing shows a dropped connection takes about 20 secs to detect with this config
            GlobalHost.Configuration.ConnectionTimeout = TimeSpan.FromSeconds(15); //long polling
            GlobalHost.Configuration.DisconnectTimeout = TimeSpan.FromSeconds(15); //everything else; KeepAlive set to 5 secs

            AreaRegistration.RegisterAllAreas();
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);
        }

        void Application_Error(object sender, EventArgs e)
        {
            var ex = Server.GetLastError();
            Server.ClearError();
            _log.Error(ex);
        }
    }
}