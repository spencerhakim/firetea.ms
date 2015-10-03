using System;
using Microsoft.Azure.Documents;

namespace Fireteams.Common.Models
{
    /// <summary>
    /// Struct for DocumentDB
    /// </summary>
    public class MatchmakingDoc : Resource
    {
        public const int SCHEMA_VERSION = 3; //TODO - increment whenever there's a schema change

        public Party Party { get; set; }

        public int Version { get; set; }

        public DateTimeOffset RegisteredAt { get; set; }
    }
}
