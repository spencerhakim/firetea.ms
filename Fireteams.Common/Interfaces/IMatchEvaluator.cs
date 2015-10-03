using System.Collections.Generic;
using System.Linq;
using Fireteams.Common.Models;

namespace Fireteams.Common.Interfaces
{
    public interface IMatchEvaluator
    {
        /// <summary>
        /// Returns an enumerable of parties that meet the minimum requirements for the fireteam
        /// </summary>
        /// <param name="queryable"></param>
        /// <param name="fireteam"></param>
        /// <returns></returns>
        IEnumerable<MatchmakingDoc> Search(IQueryable<MatchmakingDoc> queryable, Fireteam fireteam);

        /// <summary>
        /// Returns a sorted enumerable of parties in the order of best matching the fireteam
        /// </summary>
        /// <param name="enumerable"></param>
        /// <param name="fireteam"></param>
        /// <returns></returns>
        IEnumerable<MatchmakingDoc> Evaluate(IEnumerable<MatchmakingDoc> enumerable, Fireteam fireteam);
    }
}
