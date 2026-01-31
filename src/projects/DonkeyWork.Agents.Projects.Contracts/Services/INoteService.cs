using DonkeyWork.Agents.Projects.Contracts.Models;

namespace DonkeyWork.Agents.Projects.Contracts.Services;

/// <summary>
/// Service for managing notes.
/// </summary>
public interface INoteService
{
    /// <summary>
    /// Creates a new note (standalone or within a project/milestone).
    /// </summary>
    Task<NoteV1> CreateAsync(CreateNoteRequestV1 request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a note by ID.
    /// </summary>
    Task<NoteV1?> GetByIdAsync(Guid noteId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists all standalone notes for the current user (not associated with any project or milestone).
    /// </summary>
    Task<IReadOnlyList<NoteV1>> GetStandaloneAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists all notes for the current user.
    /// </summary>
    Task<IReadOnlyList<NoteV1>> ListAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists all notes for a project.
    /// </summary>
    Task<IReadOnlyList<NoteV1>> GetByProjectIdAsync(Guid projectId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists all notes for a milestone.
    /// </summary>
    Task<IReadOnlyList<NoteV1>> GetByMilestoneIdAsync(Guid milestoneId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates a note.
    /// </summary>
    Task<NoteV1?> UpdateAsync(Guid noteId, UpdateNoteRequestV1 request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a note.
    /// </summary>
    Task<bool> DeleteAsync(Guid noteId, CancellationToken cancellationToken = default);
}
