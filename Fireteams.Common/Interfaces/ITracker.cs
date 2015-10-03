namespace Fireteams.Common.Interfaces
{
    public interface ITracker
    {
        void TrackEvent(string clientId, string category, string action, string label, int? value);
        void TrackEventAsync(string clientId, string category, string action, string label, int? value);
    }
}