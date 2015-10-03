using System;
using System.Linq;
using Fireteams.Common.Interfaces;
using Fireteams.Common.Models;
using Microsoft.WindowsAzure.ServiceRuntime;
using NLog;

namespace Fireteams.Common.Services
{
    public class SchemaTrimmer
    {
        private Logger _log;
        private IDataStore _dataStore;

        public SchemaTrimmer(Logger log, IDataStore dataStore)
        {
            _log = log;
            _dataStore = dataStore;
            
            //trim after updates
            RoleEnvironment.Changed += trimDocs;
            RoleEnvironment.SimultaneousChanged += trimDocs;
        }

        private async void trimDocs(object sender, EventArgs args)
        {
            try
            {
                _log.Info("Trimming documents...");

                var docs = _dataStore.Query<MatchmakingDoc>().Where( x => x.Version < MatchmakingDoc.SCHEMA_VERSION );
                await _dataStore.TryDeleteDocumentsAsync(docs);
            }
            catch(Exception ex)
            {
                _log.Error(ex);
            }
        }
    }
}
