using System;
using System.Collections.Generic;
using System.Linq;
using Fireteams.Common.Interfaces;
using Fireteams.Common.Models;

namespace Fireteams.Common.Impl
{
    /*
     * select users with the same region/platform/activity, whose party size doesn't exceed what we need and then
     * order them by level difference, then by party size desc, and then by how long they've been waiting
     * 
     * ...I really hope this isn't too much data to fit into memory
     */
    public class MatchEvaluator : IMatchEvaluator
    {
        public IEnumerable<MatchmakingDoc> Search(IQueryable<MatchmakingDoc> queryable, Fireteam fireteam)
        {
            return queryable.Where( x =>
                x.Version == MatchmakingDoc.SCHEMA_VERSION &&
                (x.Party.Language == fireteam.Language || 
                    (x.Party.Activity < Activity.VaultOfGlass && x.Party.Language == Language.English)) && //only fallback to English for non-raids
                x.Party.Platform == fireteam.Platform &&
                x.Party.Activity == fireteam.Activity &&
                x.Party.PartySize <= fireteam.Activity.GetUsersNeeded() - fireteam.PartySize
            )
            .AsEnumerable(); //resolves DocumentDB result
        }

        public IEnumerable<MatchmakingDoc> Evaluate(IEnumerable<MatchmakingDoc> enumerable, Fireteam fireteam)
        {
            return enumerable.OrderBy( x => x.Party.Language == fireteam.Language ? 0 : 1 )
                .ThenBy( x => Math.Abs(x.Party.Level - fireteam.AverageLevel) )
                .ThenByDescending( x => x.Party.PartySize )
                .ThenBy( x => x.RegisteredAt )
                .AsEnumerable();
        }
    }
}
