using System.Web.Optimization;
using BundleTransformer.Core.Bundles;

namespace Fireteams.Web
{
    public static class BundleConfig
    {
        // For more information on bundling, visit http://go.microsoft.com/fwlink/?LinkId=301862
        public static void RegisterBundles(BundleCollection bundles)
        {
            bundles.Add(new CustomStyleBundle("~/bundles/css").Include(
                "~/Content/bootstrap/bootstrap.less",
                "~/Content/number-polyfill.css"
            ));

            bundles.Add(new ScriptBundle("~/bundles/script").Include(
                "~/Scripts/jquery-{version}.js",
                "~/Scripts/number-polyfill.js",
                "~/Scripts/jquery.storageapi.js",
                "~/Scripts/jquery.phoenix.js",
                "~/Scripts/jquery.signalR-{version}.js",
                "~/Scripts/bootstrap.js",
                "~/Scripts/main.js"
            ));

#if !DEBUG
            BundleTable.EnableOptimizations = true;
#endif
        }
    }
}