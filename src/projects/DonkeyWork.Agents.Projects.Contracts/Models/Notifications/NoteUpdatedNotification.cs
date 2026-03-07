namespace DonkeyWork.Agents.Projects.Contracts.Models.Notifications;

/// <summary>
/// Notification sent when a note is updated.
/// </summary>
public class NoteUpdatedNotification
{
    /// <summary>
    /// The ID of the note that was updated.
    /// </summary>
    public required Guid NoteId { get; init; }

    /// <summary>
    /// The title of the note.
    /// </summary>
    public required string Title { get; init; }
}
