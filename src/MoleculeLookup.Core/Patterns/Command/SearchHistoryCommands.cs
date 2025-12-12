using System.Threading.Tasks;
using MoleculeLookup.Core.Interfaces;
using MoleculeLookup.Core.Models;

namespace MoleculeLookup.Core.Patterns.Command;

/// <summary>
/// Command to add a new entry to the search history.
/// </summary>
public class AddToHistoryCommand : ISearchCommand<SearchHistoryEntry>
{
    private readonly ISearchHistoryRepository _repository;
    private readonly SearchHistoryEntry _entry;
    private SearchHistoryEntry? _addedEntry;

    public Guid CommandId { get; } = Guid.NewGuid();
    public string Description => $"Add '{_entry.MoleculeName ?? _entry.SmilesString}' to history";
    public DateTime CreatedAt { get; } = DateTime.UtcNow;
    public bool CanUndo => _addedEntry != null;
    public SearchHistoryEntry? Result => _addedEntry;

    public AddToHistoryCommand(ISearchHistoryRepository repository, SearchHistoryEntry entry)
    {
        _repository = repository;
        _entry = entry;
    }

    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        _addedEntry = await _repository.AddAsync(_entry, cancellationToken);
    }

    public async Task UndoAsync(CancellationToken cancellationToken = default)
    {
        if (_addedEntry != null)
        {
            await _repository.DeleteAsync(_addedEntry.Id, cancellationToken);
            _addedEntry = null;
        }
    }
}

/// <summary>
/// Command to delete an entry from the search history.
/// </summary>
public class DeleteFromHistoryCommand : ISearchCommand<bool>
{
    private readonly ISearchHistoryRepository _repository;
    private readonly Guid _entryId;
    private SearchHistoryEntry? _deletedEntry;
    private bool _wasDeleted;

    public Guid CommandId { get; } = Guid.NewGuid();
    public string Description => $"Delete history entry {_entryId}";
    public DateTime CreatedAt { get; } = DateTime.UtcNow;
    public bool CanUndo => _deletedEntry != null;
    public bool Result => _wasDeleted;

    public DeleteFromHistoryCommand(ISearchHistoryRepository repository, Guid entryId)
    {
        _repository = repository;
        _entryId = entryId;
    }

    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        // Store the entry before deletion for undo
        _deletedEntry = await _repository.GetByIdAsync(_entryId, cancellationToken);
        _wasDeleted = await _repository.DeleteAsync(_entryId, cancellationToken);
    }

    public async Task UndoAsync(CancellationToken cancellationToken = default)
    {
        if (_deletedEntry != null)
        {
            await _repository.AddAsync(_deletedEntry, cancellationToken);
            _deletedEntry = null;
        }
    }
}

/// <summary>
/// Command to update an existing search history entry.
/// </summary>
public class UpdateHistoryCommand : ISearchCommand<SearchHistoryEntry>
{
    private readonly ISearchHistoryRepository _repository;
    private readonly SearchHistoryEntry _updatedEntry;
    private SearchHistoryEntry? _originalEntry;
    private SearchHistoryEntry? _result;

    public Guid CommandId { get; } = Guid.NewGuid();
    public string Description => $"Update history entry {_updatedEntry.Id}";
    public DateTime CreatedAt { get; } = DateTime.UtcNow;
    public bool CanUndo => _originalEntry != null;
    public SearchHistoryEntry? Result => _result;

    public UpdateHistoryCommand(ISearchHistoryRepository repository, SearchHistoryEntry updatedEntry)
    {
        _repository = repository;
        _updatedEntry = updatedEntry;
    }

    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        // Store the original entry for undo
        _originalEntry = await _repository.GetByIdAsync(_updatedEntry.Id, cancellationToken);
        _result = await _repository.UpdateAsync(_updatedEntry, cancellationToken);
    }

    public async Task UndoAsync(CancellationToken cancellationToken = default)
    {
        if (_originalEntry != null)
        {
            await _repository.UpdateAsync(_originalEntry, cancellationToken);
            _originalEntry = null;
        }
    }
}

/// <summary>
/// Command to toggle the favorite status of a history entry.
/// </summary>
public class ToggleFavoriteCommand : ISearchCommand<SearchHistoryEntry>
{
    private readonly ISearchHistoryRepository _repository;
    private readonly Guid _entryId;
    private bool _previousFavoriteStatus;
    private SearchHistoryEntry? _result;

    public Guid CommandId { get; } = Guid.NewGuid();
    public string Description => $"Toggle favorite for entry {_entryId}";
    public DateTime CreatedAt { get; } = DateTime.UtcNow;
    public bool CanUndo => _result != null;
    public SearchHistoryEntry? Result => _result;

    public ToggleFavoriteCommand(ISearchHistoryRepository repository, Guid entryId)
    {
        _repository = repository;
        _entryId = entryId;
    }

    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var entry = await _repository.GetByIdAsync(_entryId, cancellationToken);
        if (entry != null)
        {
            _previousFavoriteStatus = entry.IsFavorite;
            entry.IsFavorite = !entry.IsFavorite;
            _result = await _repository.UpdateAsync(entry, cancellationToken);
        }
    }

    public async Task UndoAsync(CancellationToken cancellationToken = default)
    {
        if (_result != null)
        {
            _result.IsFavorite = _previousFavoriteStatus;
            await _repository.UpdateAsync(_result, cancellationToken);
        }
    }
}

/// <summary>
/// Command to add notes to a history entry.
/// </summary>
public class AddNotesCommand : ISearchCommand<SearchHistoryEntry>
{
    private readonly ISearchHistoryRepository _repository;
    private readonly Guid _entryId;
    private readonly string _notes;
    private string? _previousNotes;
    private SearchHistoryEntry? _result;

    public Guid CommandId { get; } = Guid.NewGuid();
    public string Description => $"Add notes to entry {_entryId}";
    public DateTime CreatedAt { get; } = DateTime.UtcNow;
    public bool CanUndo => _result != null;
    public SearchHistoryEntry? Result => _result;

    public AddNotesCommand(ISearchHistoryRepository repository, Guid entryId, string notes)
    {
        _repository = repository;
        _entryId = entryId;
        _notes = notes;
    }

    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var entry = await _repository.GetByIdAsync(_entryId, cancellationToken);
        if (entry != null)
        {
            _previousNotes = entry.Notes;
            entry.Notes = _notes;
            _result = await _repository.UpdateAsync(entry, cancellationToken);
        }
    }

    public async Task UndoAsync(CancellationToken cancellationToken = default)
    {
        if (_result != null)
        {
            _result.Notes = _previousNotes;
            await _repository.UpdateAsync(_result, cancellationToken);
        }
    }
}

/// <summary>
/// Command to clear all search history.
/// </summary>
public class ClearHistoryCommand : ISearchCommand<int>
{
    private readonly ISearchHistoryRepository _repository;
    private List<SearchHistoryEntry>? _clearedEntries;
    private int _clearedCount;

    public Guid CommandId { get; } = Guid.NewGuid();
    public string Description => "Clear all search history";
    public DateTime CreatedAt { get; } = DateTime.UtcNow;
    public bool CanUndo => _clearedEntries != null && _clearedEntries.Count > 0;
    public int Result => _clearedCount;

    public ClearHistoryCommand(ISearchHistoryRepository repository)
    {
        _repository = repository;
    }

    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        // Store all entries for undo
        _clearedEntries = (await _repository.GetAllAsync(cancellationToken)).ToList();
        _clearedCount = _clearedEntries.Count;
        await _repository.ClearAllAsync(cancellationToken);
    }

    public async Task UndoAsync(CancellationToken cancellationToken = default)
    {
        if (_clearedEntries != null)
        {
            foreach (var entry in _clearedEntries)
            {
                await _repository.AddAsync(entry, cancellationToken);
            }
            _clearedEntries = null;
        }
    }
}
