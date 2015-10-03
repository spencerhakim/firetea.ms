using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fireteams.Common.Models;
using Microsoft.Azure.Documents;
using Microsoft.WindowsAzure.Storage.Queue;

namespace Fireteams.Common.Interfaces
{
    public interface IDataStore
    {
        int DocumentDbPopulation { get; }
        int? ApproxQueueLength { get; }

        /// <summary>
        /// Tries to insert the provided document into both DocumentDB and the Azure Queue
        /// </summary>
        /// <param name="doc">Object to insert</param>
        /// <param name="addToQueue">Whether or not to add a message to the queue</param>
        /// <returns>True if successful, otherwise false</returns>
        Task<bool> TryCreateDocumentAsync(MatchmakingDoc doc, bool addToQueue);

        /// <summary>
        /// Tries to select an existing document with the specified ID from DocumentDB
        /// </summary>
        /// <param name="id">ID of the document to select</param>
        /// <returns>The document with the specified ID, or null</returns>
        Task<MatchmakingDoc> TrySelectDocumentByIdAsync(string id);

        /// <summary>
        /// Tries to delete the document with the specified ID from DocumentDB
        /// </summary>
        /// <param name="id">ID of the document to delete</param>
        /// <returns>True if successful, otherwise false</returns>
        Task<bool> TryDeleteDocumentByIdAsync(string id);

        /// <summary>
        /// Tries to delete the document from DocumentDB
        /// </summary>
        /// <param name="doc">Document to delete</param>
        /// <returns>True if successful, otherwise false</returns>
        Task<bool> TryDeleteDocumentAsync(MatchmakingDoc doc);

        /// <summary>
        /// Tries to delete all the documents in the provided enumerable
        /// </summary>
        /// <param name="docs">Documents to try to delete</param>
        Task TryDeleteDocumentsAsync(IEnumerable<MatchmakingDoc> docs);

        /// <summary>
        /// Creates a DocumentDB queryable
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        IOrderedQueryable<T> Query<T>();

        /// <summary>
        /// Creates a DocumentDB queryable
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="query">SELECT queries only</param>
        /// <returns></returns>
        IQueryable<T> Query<T>(SqlQuerySpec query);

        /// <summary>
        /// Gets the next message from the queue
        /// </summary>
        /// <returns>Queue message</returns>
        Task<CloudQueueMessage> TryGetMessageAsync();

        /// <summary>
        /// Tries to delete the provided message from the Azure Queue
        /// </summary>
        /// <param name="msg">Message to delete</param>
        /// <returns>True if successful, otherwise false</returns>
        Task<bool> TryDeleteMessageAsync(CloudQueueMessage msg);
    }
}