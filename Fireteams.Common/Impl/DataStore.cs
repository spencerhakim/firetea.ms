using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fireteams.Common.Interfaces;
using Fireteams.Common.Models;
using Microsoft.Azure;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.Documents.Linq;
using Microsoft.WindowsAzure.ServiceRuntime;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;
using NLog;

namespace Fireteams.Common.Impl
{
    internal class DataStore : DisposableBase, IDataStore
    {
        private static int _debugClearedOnStartup;

        private readonly DocumentClient _client;
        private readonly DocumentCollection _collection;
        private readonly CloudQueue _queue;
        private readonly Logger _log;

        public int DocumentDbPopulation
        {
            get
            {
                try
                {
                    return _client.CreateDocumentQuery(_collection.SelfLink, "SELECT c.id FROM c")
                        .AsEnumerable()
                        .Count();
                }
                catch(Exception ex)
                {
                    _log.Error(ex);
                    return -1;
                }
            }
        }

        public int? ApproxQueueLength
        {
            get
            {
                _queue.FetchAttributes();
                return _queue.ApproximateMessageCount;
            }
        }

        public DataStore(Logger log)
        {
            _log = log;

            //Configs
            var uri = new Uri(CloudConfigurationManager.GetSetting("DbUrl"));
            var key = CloudConfigurationManager.GetSetting("DbKey");
            var scs = CloudConfigurationManager.GetSetting("StorageConnectionString");
            var collectionName = (RoleEnvironment.IsEmulated ? "mmdebug" : "matchmaking");

            //DocumentDB
            _client = new DocumentClient(uri, key);
            var db = _client.CreateDatabaseQuery().Where( x => x.Id == "fireteams" ).AsEnumerable().First();
            _collection = _client.CreateDocumentCollectionQuery(db.CollectionsLink).Where( x => x.Id == collectionName ).AsEnumerable().First();

            //Storage Queue
            var queueClient = CloudStorageAccount.Parse(scs).CreateCloudQueueClient();
            _queue = queueClient.GetQueueReference("matchmaking");
            _queue.CreateIfNotExists();

            //clear data on emulated start
            if( RoleEnvironment.IsEmulated && Interlocked.CompareExchange(ref _debugClearedOnStartup, 1, 0) == 0 )
            {
                _log.Debug("Clearing data storage");
                _queue.Clear();

                try
                {
                    var tasks = _client.CreateDocumentQuery<Document>(_collection.DocumentsLink, "SELECT * FROM c")
                        .AsEnumerable()
                        .Select( x => _client.DeleteDocumentAsync(x.SelfLink) );
                    Task.WaitAll( tasks.ToArray<Task>() );
                }
                catch(Exception ex)
                {
                    _log.Error(ex);
                }
            }
        }

        protected override void Dispose(bool disposing)
        {
            if( !Disposed && disposing )
            {
                if( _client != null )
                    _client.Dispose();
            }

            base.Dispose(disposing);
        }

        /// <summary>
        /// Tries to insert the provided document into both DocumentDB and the Azure Queue
        /// </summary>
        /// <param name="doc">Object to insert</param>
        /// <param name="addToQueue">Whether or not to add a message to the queue</param>
        /// <returns>True if successful, otherwise false</returns>
        public async Task<bool> TryCreateDocumentAsync(MatchmakingDoc doc, bool addToQueue)
        {
            if( doc == null )
                return false;

            try
            {
                await _client.CreateDocumentAsync(_collection.SelfLink, doc);
            }
            catch(Exception ex)
            {
                _log.Error(ex);
                return false;
            }

            try
            {
                if( addToQueue )
                    await _queue.AddMessageAsync(new CloudQueueMessage(doc.Id), TimeSpan.FromHours(6), null, null, null);
            }
            catch(Exception ex)
            {
                _log.Error(ex);
            }

            return true;
        }

        /// <summary>
        /// Tries to select an existing document with the specified ID from DocumentDB
        /// </summary>
        /// <param name="id">ID of the document to select</param>
        /// <returns>The document with the specified ID, or null</returns>
        public async Task<MatchmakingDoc> TrySelectDocumentByIdAsync(string id)
        {
            try
            {
                return await Task.Run( () =>
                {
                    return Query<MatchmakingDoc>()
                        .Where( x => x.Id == id )
                        .AsEnumerable() //because DocumentDB doesn't support First/FirstOrDefault as of 0.9.1-preview
                        .FirstOrDefault();
                });
            }
            catch(Exception ex)
            {
                _log.Error(ex);
                return null;
            }
        }

        /// <summary>
        /// Tries to delete the document with the specified ID from DocumentDB
        /// </summary>
        /// <param name="id">ID of the document to delete</param>
        /// <returns>True if successful, otherwise false</returns>
        public async Task<bool> TryDeleteDocumentByIdAsync(string id)
        {
            try
            {
                var doc = await TrySelectDocumentByIdAsync(id);
                return await TryDeleteDocumentAsync(doc);
            }
            catch(Exception ex)
            {
                _log.Error(ex);
                return false;
            }
        }

        /// <summary>
        /// Tries to delete the document from DocumentDB
        /// </summary>
        /// <param name="doc">Document to delete</param>
        /// <returns>True if successful, otherwise false</returns>
        public async Task<bool> TryDeleteDocumentAsync(MatchmakingDoc doc)
        {
            try
            {
                if( doc == null )
                    return false;

                await _client.DeleteDocumentAsync(doc.SelfLink);
                return true;
            }
            catch(Exception ex)
            {
                _log.Error(ex);
                return false;
            }
        }

        /// <summary>
        /// Tries to delete all the documents in the provided enumerable
        /// </summary>
        /// <param name="docs">Documents to try to delete</param>
        public async Task TryDeleteDocumentsAsync(IEnumerable<MatchmakingDoc> docs)
        {
            try
            {
                await Task.WhenAll( docs.Select( x => _client.DeleteDocumentAsync(x.SelfLink) ).ToArray<Task>() );
            }
            catch(Exception ex)
            {
                _log.Error(ex);
            }
        }

        /// <summary>
        /// Creates a DocumentDB queryable
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public IOrderedQueryable<T> Query<T>()
        {
            return _client.CreateDocumentQuery<T>(_collection.DocumentsLink);
        }

        /// <summary>
        /// Creates a DocumentDB queryable
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="query">SELECT queries only</param>
        /// <returns></returns>
        public IQueryable<T> Query<T>(SqlQuerySpec query)
        {
            return _client.CreateDocumentQuery<T>(_collection.DocumentsLink, query);
        }

        /// <summary>
        /// Gets the next message from the queue
        /// </summary>
        /// <returns>Queue message</returns>
        public async Task<CloudQueueMessage> TryGetMessageAsync()
        {
            try
            {
                return await _queue.GetMessageAsync(TimeSpan.FromSeconds(10), null, null);
            }
            catch(Exception ex)
            {
                _log.Error(ex);
                return null;
            }
        }

        /// <summary>
        /// Tries to delete the provided message from the Azure Queue
        /// </summary>
        /// <param name="msg">Message to delete</param>
        /// <returns>True if successful, otherwise false</returns>
        public async Task<bool> TryDeleteMessageAsync(CloudQueueMessage msg)
        {
            try
            {
                await _queue.DeleteMessageAsync(msg);
                return true;
            }
            catch(Exception ex)
            {
                _log.Error(ex);
                return false;
            }
        }
    }
}