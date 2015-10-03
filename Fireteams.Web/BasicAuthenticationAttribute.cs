using System;
using System.Text;
using System.Web.Mvc;

namespace Fireteams.Web
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public sealed class BasicAuthenticationAttribute : ActionFilterAttribute
    {
        public string Username { get; private set; }
        public string Password { get; private set; }
        public string Realm { get; private set; }

        public BasicAuthenticationAttribute(string username, string password, string realm)
        {
            Username = username;
            Password = password;
            Realm = realm;
        }

        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            var req = filterContext.HttpContext.Request;
            var auth = req.Headers["Authorization"];
            if( !String.IsNullOrWhiteSpace(auth) )
            {
                var cred = Encoding.ASCII.GetString(Convert.FromBase64String(auth.Substring(6))).Split(':');
                var user = new
                {
                    Username = cred[0],
                    Password = cred[1]
                };

                if( user.Username == Username && user.Password == Password )
                    return;
            }

            var res = filterContext.HttpContext.Response;
            res.StatusCode = 401;
            res.AddHeader("WWW-Authenticate", String.Format("Basic realm=\"{0}\"", Realm ?? "Restricted"));
            res.Write("Access denied");
            res.End();
        }
    }
}