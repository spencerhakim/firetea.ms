using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fireteams.Common.Interfaces;
using Fireteams.Common.Models;
using Fireteams.Common.SignalR;
using Microsoft.AspNet.SignalR;
using Microsoft.Azure;
using NLog;

namespace Fireteams.Common.Services
{
    public class QueueProcessor
    {
        private Logger _log;
        private IDataStore _dataStore;
        private IHubContext<IMatchmakingClient> _hub;
        private ITracker _tracker;
        private IMatchEvaluator _matchBuilder;
        private CircularBuffer<TimeSpan> _timeToMatch;

        private readonly int _queueSleepMin;
        private readonly int _queueSleepMax;
        private readonly int _queueSleepLength;

        public QueueProcessor(Logger log, IDataStore dataStore, IHubContext<IMatchmakingClient> hub, ITracker tracker, IMatchEvaluator matchBuilder, CircularBuffer<TimeSpan> timeToMatch)
        {
            _log = log;
            _dataStore = dataStore;
            _hub = hub;
            _tracker = tracker;
            _matchBuilder = matchBuilder;
            _timeToMatch = timeToMatch;

            _queueSleepMin = Int32.Parse( CloudConfigurationManager.GetSetting("QueueSleepMin") );
            _queueSleepMax = Int32.Parse( CloudConfigurationManager.GetSetting("QueueSleepMax") );
            _queueSleepLength = Int32.Parse( CloudConfigurationManager.GetSetting("QueueSleepLength") );

            Task.Run( async () =>
            {
                _log.Info("Running QueueProcessor...");

                while( true )
                {
                    var sleepTime = _queueSleepMax;

                    try
                    {
                        await processQueue();
                        sleepTime = _queueSleepMax - (_dataStore.DocumentDbPopulation * (_queueSleepMax/_queueSleepLength));
                    }
                    catch(Exception ex)
                    {
                        _log.Error(ex);
                    }

                    Thread.Sleep(sleepTime < _queueSleepMin ? _queueSleepMin : sleepTime);
                }
            });
        }

        private string _lastMsgId;
        private async Task processQueue()
        {
            //pop the queue...
            var msg = await _dataStore.TryGetMessageAsync();
            if( msg == null || msg.Id == _lastMsgId )
                return;

            //...then grab the specified document...
            _lastMsgId = msg.Id;
            var doc = await _dataStore.TrySelectDocumentByIdAsync(msg.AsString);
            if( doc == null )
            {
                await _dataStore.TryDeleteMessageAsync(msg);
                return;
            }

            //... and make sure the queued user hasn't already been added to another fireteam
            if( !await _dataStore.TryDeleteDocumentAsync(doc) )
                return;

            //start building the fireteam
            _hub.Clients.Client(doc.Id).UpdateStatus("Evaluating");
            var fireteam = new Fireteam(doc);
            var possibleMatches = _matchBuilder.Search(_dataStore.Query<MatchmakingDoc>(), fireteam);
            possibleMatches = _matchBuilder.Evaluate(possibleMatches, fireteam);

            foreach( var match in possibleMatches )
            {
                //make sure the party wouldn't overflow the fireteam, and then try to remove the party from the DB
                if( fireteam.PartySize + match.Party.PartySize > fireteam.Activity.GetUsersNeeded() ||
                    !await _dataStore.TryDeleteDocumentAsync(match) )
                    continue;

                fireteam.Add(match);
                if( fireteam.PartySize == fireteam.Activity.GetUsersNeeded() )
                    break;
            }

            var fireteamClients = _hub.Clients.Clients( fireteam.Select( x => x.Id ).ToList() );
            if( fireteam.PartySize == fireteam.Activity.GetUsersNeeded() )
            {
                //send successful doc to clients
                await _dataStore.TryDeleteMessageAsync(msg);
                fireteamClients.MatchFound( fireteam.Select( x => x.Party ) );

                //track analytics for each user
                foreach( var match in fireteam )
                    trackMatch(match);
            }
            else
            {
                //evaluating takes almost no time at all, so we fake it; update all users with progress
                Task.Run( () =>
                {
                    Thread.Sleep(2500);
                    _hub.Clients.Client(doc.Id).UpdateStatus("Searching");
                });
                fireteamClients.UpdateProgress(fireteam.PartySize, fireteam.Activity.GetUsersNeeded());

                Task.WaitAll( fireteam.Select( x => _dataStore.TryCreateDocumentAsync(x, false) ).ToArray<Task>() );
            }
        }

        private void trackMatch(MatchmakingDoc doc)
        {
            var age = DateTimeOffset.UtcNow - doc.RegisteredAt;
            _timeToMatch.PushBack(age);

            _tracker.TrackEventAsync(doc.Id, "Matchmaking", "MatchFound", null, (int)age.TotalSeconds);
            _tracker.TrackEventAsync(doc.Id, "Matched", "Language", Enum.GetName(typeof(Language), doc.Party.Language), null);
            _tracker.TrackEventAsync(doc.Id, "Matched", "Platform", Enum.GetName(typeof(Platform), doc.Party.Platform), null);
            _tracker.TrackEventAsync(doc.Id, "Matched", "PartySize", doc.Party.PartySize.ToString(), null);
            _tracker.TrackEventAsync(doc.Id, "Matched", "Activity", Enum.GetName(typeof(Activity), doc.Party.Activity), null);
            _tracker.TrackEventAsync(doc.Id, "Matched", "Level", doc.Party.Level.ToString(), null);
        }
    }
}