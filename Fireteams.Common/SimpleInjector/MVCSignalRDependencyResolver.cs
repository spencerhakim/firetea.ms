using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using Microsoft.AspNet.SignalR;

namespace SimpleInjector
{
    public class MvcSignalRDependencyResolver : DefaultDependencyResolver
    {
        public override object GetService(Type serviceType)
        {
            return DependencyResolver.Current.GetService(serviceType) ?? base.GetService(serviceType);
        }

        public override IEnumerable<object> GetServices(Type serviceType)
        {
            return DependencyResolver.Current.GetServices(serviceType).Concat(base.GetServices(serviceType));
        }
    }
}
