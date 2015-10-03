using System;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Fireteams.Common.Interfaces;
using Fireteams.Common.Models;
using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Hubs;
using Microsoft.WindowsAzure.ServiceRuntime;
using NLog;

namespace Fireteams.Common.SignalR
{
    /// <summary>
    /// Short-lived SignalR hub for matchmaking
    /// </summary>
    [HubName("v1")]
    public class MatchmakingHub : Hub<IMatchmakingClient>
    {
        private Logger _log;
        private IDataStore _dataStore;
        private ITracker _tracker;
        private CircularBuffer<TimeSpan> _timeToMatch;

        public MatchmakingHub(Logger log, IDataStore dataStore, ITracker tracker, CircularBuffer<TimeSpan> timeToMatch)
        {
            _log = log;
            _dataStore = dataStore;
            _tracker = tracker;
            _timeToMatch = timeToMatch;
        }

        #region Public methods
        /// <summary>
        /// Called when the connection connects to this hub instance.
        /// </summary>
        /// <returns>
        /// A <see cref="T:System.Threading.Tasks.Task"/>
        /// </returns>
        public override async Task OnConnected()
        {
            try
            {
                validateQS();
            }
            catch(Exception ex)
            {
                _log.Error(ex);

                if( ex is AggregateException )
                    ex = (ex as AggregateException).Flatten().InnerException;

                if( ex is WarningException )
                    Clients.Caller.DisplayWarning(ex.Message);
                else
                    Clients.Caller.DisplayError(ex.Message);
            }

            await base.OnConnected();
        }

        /// <summary>
        /// Called when the connection reconnects to this hub instance.
        /// </summary>
        /// <returns>
        /// A <see cref="T:System.Threading.Tasks.Task"/>
        /// </returns>
        public override async Task OnReconnected()
        {
            await base.OnReconnected();
        }

        /// <summary>
        /// Called when a connection disconnects from this hub gracefully or due to a timeout.
        /// </summary>
        /// <param name="stopCalled">true, if stop was called on the client closing the connection gracefully;
        ///             false, if the connection has been lost for longer than the
        ///             <see cref="P:Microsoft.AspNet.SignalR.Configuration.IConfigurationManager.DisconnectTimeout"/>.
        ///             Timeouts can be caused by clients reconnecting to another SignalR server in scaleout.
        ///             </param>
        /// <returns>
        /// A <see cref="T:System.Threading.Tasks.Task"/>
        /// </returns>
        public override async Task OnDisconnected(bool stopCalled)
        {
            await unregister();
            await base.OnDisconnected(stopCalled);
        }

        /// <summary>
        /// Adds the calling SignalR client to the database with the provided user data
        /// </summary>
        /// <param name="party"></param>
        public async Task<bool> Register(Party party)
        {
            return await register(party);
        }

        public string GetTimeToMatch()
        {
            try
            {
                var ttmAvg = _timeToMatch.Average();
                return ttmAvg > TimeSpan.FromMinutes(10) ?
                    "10+ mins" :
                    ttmAvg.ToString("m'm 'ss's'");
            }
            catch
            {
                return "N/A";
            }
        }
        #endregion

        private async Task<bool> register(Party party)
        {
            try
            {
                validateQS();
                party.Validate();

                //make sure they're not already registered
                if( await _dataStore.TrySelectDocumentByIdAsync(Context.ConnectionId) != null )
                    return true;

                var success = await _dataStore.TryCreateDocumentAsync(new MatchmakingDoc
                {
                    Id = Context.ConnectionId,
                    Party = party,
                    RegisteredAt = DateTimeOffset.UtcNow,
                    Version = MatchmakingDoc.SCHEMA_VERSION
                }, true);

                if( !success )
                    throw new ApplicationException("Failed to join matchmaking, please try again.");

                trackRegister(party);
                Clients.Caller.UpdateStatus("Searching");
                Clients.Caller.UpdateProgress(party.PartySize, party.Activity.GetUsersNeeded());

                if( party.Activity == Activity.TrialsOfOsiris )
                    Clients.Caller.DisplayWarning("Make sure you have obtained a Trials Passage from Brother Vance, otherwise you cannot compete!");

                else if( party.Level < party.Activity.GetRecommendedLevel() )
                    Clients.Caller.DisplayWarning("You are below the recommended level for this activity! Please don't assume other Guardians will be willing to carry you!");

                return true;
            }
            catch(Exception ex)
            {
                _log.Error(ex);
                _tracker.TrackEventAsync(Context.ConnectionId, "Matchmaking", "RegisterFail", null, null);

                if( ex is AggregateException )
                    ex = (ex as AggregateException).Flatten().InnerException;

                Clients.Caller.DisplayError(ex.Message);

                return false;
            }
        }

        private async Task unregister()
        {
            var doc = await _dataStore.TrySelectDocumentByIdAsync(Context.ConnectionId);
            if( doc != null && await _dataStore.TryDeleteDocumentAsync(doc) )
                trackUnregister(doc);
        }

        private void validateQS()
        {
            var client = Context.QueryString["client"];
            var version = Context.QueryString["version"];

            if( String.IsNullOrWhiteSpace(client) )
                throw new ApplicationException("Missing client parameter");

            if( client == "web" && version != RoleEnvironment.DeploymentId )
                throw new ApplicationException("Firetea.ms has been updated, please refresh the page.");

            ///////////////////////////////////////////////////////////////////////////////////////
            //API blocklist
            var blocked = new[]{
                "example.com"
            };

            if( blocked.Contains(client) )
                throw new ApplicationException("Sorry, this app has been blocked for violating the Firetea.ms API Terms of Use.");
        }

        private void trackRegister(Party party)
        {
            _tracker.TrackEventAsync(Context.ConnectionId, "Matchmaking", "RegisterSuccess", null, null);
            _tracker.TrackEventAsync(Context.ConnectionId, "Registered", "Language", Enum.GetName(typeof(Language), party.Language), null);
            _tracker.TrackEventAsync(Context.ConnectionId, "Registered", "Platform", Enum.GetName(typeof(Platform), party.Platform), null);
            _tracker.TrackEventAsync(Context.ConnectionId, "Registered", "PartySize", party.PartySize.ToString(), null);
            _tracker.TrackEventAsync(Context.ConnectionId, "Registered", "Activity", Enum.GetName(typeof(Activity), party.Activity), null);
            _tracker.TrackEventAsync(Context.ConnectionId, "Registered", "Level", party.Level.ToString(), null);
        }

        private void trackUnregister(MatchmakingDoc doc)
        {
            int age = (int)(DateTimeOffset.UtcNow - doc.RegisteredAt).TotalSeconds;
            _tracker.TrackEventAsync(Context.ConnectionId, "Matchmaking", "Unregister", null, age);
            _tracker.TrackEventAsync(Context.ConnectionId, "Unregistered", "Language", Enum.GetName(typeof(Language), doc.Party.Language), null);
            _tracker.TrackEventAsync(Context.ConnectionId, "Unregistered", "Platform", Enum.GetName(typeof(Platform), doc.Party.Platform), null);
            _tracker.TrackEventAsync(Context.ConnectionId, "Unregistered", "doc.PartySize", doc.Party.PartySize.ToString(), null);
            _tracker.TrackEventAsync(Context.ConnectionId, "Unregistered", "Activity", Enum.GetName(typeof(Activity), doc.Party.Activity), null);
            _tracker.TrackEventAsync(Context.ConnectionId, "Unregistered", "Level", doc.Party.Level.ToString(), null);
        }
    }
}