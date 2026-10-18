namespace WardogsXpTracker.Core.Tracking;

public sealed partial class SessionTracker
{
    /// <summary>Removes one match and all of its records. File persistence belongs to the caller.</summary>
    public TrackerActionResult DeleteSession(Guid sessionId)
    {
        if (sessionId == Guid.Empty) return Fail("Choose a saved match to delete.");
        var index = Document.Sessions.FindIndex(session => session.Id == sessionId);
        if (index < 0) return Fail("This match is no longer in the selected season.");
        Document.Sessions.RemoveAt(index);
        if (Document.ActiveSessionId == sessionId) Document.ActiveSessionId = null;
        return Ok("Match deleted. Its XP and analytics records were removed.");
    }
}
