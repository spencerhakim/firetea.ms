using System.Collections.Generic;
using System.Linq;

namespace Fireteams.Common.Models
{
    public class Fireteam : List<MatchmakingDoc>
    {
        public Language Language { get; private set; }
        public Platform Platform { get; private set; }
        public Activity Activity { get; private set; }

        public int PartySize
        {
            get { return this.Sum( x => x.Party.PartySize ); }
        }

        public int AverageLevel
        {
            get { return (int)this.Average( x => x.Party.Level ); }
        }

        public Fireteam(MatchmakingDoc doc)
        {
            Add(doc);

            Language = doc.Party.Language;
            Platform = doc.Party.Platform;
            Activity = doc.Party.Activity;
        }
    }
}
