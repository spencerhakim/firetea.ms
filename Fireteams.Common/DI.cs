using System;
using System.Diagnostics;
using System.Reflection;
using System.Web.Mvc;
using Fireteams.Common.Impl;
using Fireteams.Common.Interfaces;
using Fireteams.Common.Services;
using Fireteams.Common.SignalR;
using Microsoft.AspNet.SignalR;
using Microsoft.Azure;
using Microsoft.WindowsAzure.ServiceRuntime;
using NLog;
using SimpleInjector;
using SimpleInjector.Integration.Web.Mvc;
using UniversalAnalyticsHttpWrapper;

namespace Fireteams.Common
{
    /// <summary>
    /// Dependency Injection and IoC
    /// </summary>
    public static class DI
    {
        private static Container _container = new Container();

        [DebuggerStepThrough]
        public static TService GetInstance<TService>() where TService : class
        {
            return _container.GetInstance<TService>();
        }

        static DI()
        {
            //basic stuff
            _container.RegisterWithContext( dc => LogManager.GetLogger((dc.ImplementationType ?? typeof(object)).FullName) );
            _container.RegisterSingleton<QueueProcessor>();
            _container.RegisterSingleton<SchemaTrimmer>();
            _container.RegisterSingleton( () => new CircularBuffer<TimeSpan>(100) ); //for average time to match
            _container.Register<IDataStore, DataStore>();
            _container.Register<IEventTracker, EventTracker>();
            _container.Register<ITracker, Tracker>();
            _container.Register<IMatchEvaluator, MatchEvaluator>();

            //MVC
            _container.RegisterMvcControllers(Assembly.GetExecutingAssembly());
            _container.RegisterMvcIntegratedFilterProvider();
            DependencyResolver.SetResolver(new SimpleInjectorDependencyResolver(_container));

            //SignalR
            _container.RegisterSingleton( () => GlobalHost.ConnectionManager.GetHubContext<MatchmakingHub, IMatchmakingClient>() );
            GlobalHost.DependencyResolver = new MvcSignalRDependencyResolver();
            if( !RoleEnvironment.IsEmulated )
            {
                GlobalHost.DependencyResolver.UseServiceBus(
                    CloudConfigurationManager.GetSetting("SBConnection"),
                    (RoleEnvironment.IsEmulated ? "fireteams-debug" : "fireteams")
                );
            }

            _container.Verify();
        }
    }
}
