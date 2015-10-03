using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web.Mvc;
using Fireteams.Common.Models;
using Microsoft.Azure;
using Microsoft.WindowsAzure.ServiceRuntime;

namespace Fireteams.Web.Controllers
{
#if !DEBUG
    [OutputCache(Duration=600, NoStore=true)]
#endif
    public class MainController : Controller
    {
        public MainController()
        {
            ViewBag.Version = RoleEnvironment.DeploymentId;
            ViewBag.Title = "Firetea.ms";
            ViewBag.ShowAds = Boolean.Parse(CloudConfigurationManager.GetSetting("ShowAds"));
        }

        [Route("~/")]
        public ActionResult Index()
        {
            var levelRange = typeof(Party).GetProperty("Level").CustomAttributes.First( x => x.AttributeType == typeof(RangeAttribute) );
            var unixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var msgDateTime = DateTimeOffset.Parse(CloudConfigurationManager.GetSetting("MessageDate"));

            ViewBag.MessageText = CloudConfigurationManager.GetSetting("MessageText");
            ViewBag.MessageDate = msgDateTime;
            ViewBag.MessageTimestamp = (long)(msgDateTime.UtcDateTime - unixEpoch).TotalMilliseconds;
            ViewBag.LevelMin = (int)levelRange.ConstructorArguments[0].Value;
            ViewBag.LevelMax = (int)levelRange.ConstructorArguments[1].Value;
            return View("~/Views/Index.cshtml");
        }

        [Route("~/faq")]
        public ActionResult FAQ()
        {
            ViewBag.Title = "FAQ • Firetea.ms";
            return View("~/Views/FAQ.cshtml");
        }

        [Route("~/help")]
        public ActionResult Help()
        {
            ViewBag.Title = "Help • Firetea.ms";
            return View("~/Views/Help.cshtml");
        }

        /*[Route("~/api")]
        public ActionResult API()
        {
            ViewBag.Title = "API • Firetea.ms";
            return View("~/Views/API.cshtml");
        }*/

        [Route("~/noscript")]
        public ActionResult NoScript()
        {
            ViewBag.Title = "JavaScript Required • Firetea.ms";
            return View("~/Views/NoScript.cshtml");
        }

        [Route("~/unsupported")]
        public ActionResult Unsupported()
        {
            ViewBag.Title = "Unsupported Browser • Firetea.ms";
            return View("~/Views/Unsupported.cshtml");
        }

        [Route("~/Error"), OutputCache(Duration=600, VaryByParam="*")]
        public ActionResult Error()
        {
            var statusCode = Uri.UnescapeDataString(Request.QueryString.ToString()).Split(';')[0]; //httpErrors handling appends a querystring
            Response.StatusCode = Int32.Parse(statusCode);

            //401 MUST also provide WWW-Authenticate header
            if( Response.StatusCode == 401 )
                Response.AddHeader("WWW-Authenticate", "Basic");

            ViewBag.ErrorMessage = String.Format("{0} - {1}", Response.StatusCode, Response.StatusDescription);
            ViewBag.Title = String.Format("{0} • Firetea.ms", Response.StatusDescription);
            return View("~/Views/Error.cshtml");
        }
    }
}