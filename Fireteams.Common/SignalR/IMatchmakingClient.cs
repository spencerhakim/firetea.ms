using System.Collections.Generic;
using Fireteams.Common.Models;

namespace Fireteams.Common.SignalR
{
    public interface IMatchmakingClient
    {
        //blue, yellow, and red alert dialogs
        void DisplayInfo(string message);
        void DisplayWarning(string message);
        void DisplayError(string message);

        void UpdateStatus(string text); //Searching, Evaluating, whatever
        void UpdateProgress(int currentUsers, int totalNeeded); // 2/3, etc
        void MatchFound(IEnumerable<Party> parties);
    }
}
