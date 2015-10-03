using System;
using System.Threading.Tasks;
using Fireteams.Common.Interfaces;
using Microsoft.Azure;
using NLog;
using UniversalAnalyticsHttpWrapper;

namespace Fireteams.Common.Impl
{
    internal class Tracker : ITracker
    {
        private Logger _log;
        private IEventTracker _tracker;

        public Tracker(Logger log, IEventTracker tracker)
        {
            _log = log;
            _tracker = tracker;
        }

        public void TrackEvent(string clientId, string category, string action, string label, int? value)
        {
#if !DEBUG
            try
            {
                var evnt = new UniversalAnalyticsEvent(
                    CloudConfigurationManager.GetSetting("TrackingID"),
                    clientId ?? "developer",
                    category,
                    action,
                    label,
                    value.ToString()
                );

                _tracker.TrackEvent(evnt);
            }
            catch(Exception ex)
            {
                _log.Error(ex);
            }
#endif
        }

        public void TrackEventAsync(string clientId, string category, string action, string label, int? value)
        {
            Task.Run( () => TrackEvent(clientId, category, action, label, value) );
        }
    }
}
