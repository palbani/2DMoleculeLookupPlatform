namespace MoleculeLookup.Core.Patterns.Command;

/// <summary>
/// Command invoker that manages execution, undo, and redo of search commands.
/// Maintains a history of executed commands for undo/redo functionality.
/// </summary>
public class SearchCommandInvoker
{
    private readonly Stack<ISearchCommand> _undoStack = new();
    private readonly Stack<ISearchCommand> _redoStack = new();
    private readonly List<CommandHistoryEntry> _commandHistory = new();
    private readonly int _maxHistorySize;

    /// <summary>
    /// Event raised when a command is executed.
    /// </summary>
    public event EventHandler<CommandExecutedEventArgs>? CommandExecuted;

    /// <summary>
    /// Event raised when a command is undone.
    /// </summary>
    public event EventHandler<CommandExecutedEventArgs>? CommandUndone;

    /// <summary>
    /// Event raised when a command is redone.
    /// </summary>
    public event EventHandler<CommandExecutedEventArgs>? CommandRedone;

    public SearchCommandInvoker(int maxHistorySize = 50)
    {
        _maxHistorySize = maxHistorySize;
    }

    /// <summary>
    /// Executes a command and adds it to the undo stack.
    /// </summary>
    public async Task ExecuteAsync(ISearchCommand command, CancellationToken cancellationToken = default)
    {
        await command.ExecuteAsync(cancellationToken);

        // Add to undo stack if command supports undo
        if (command.CanUndo)
        {
            _undoStack.Push(command);
            TrimStackIfNeeded(_undoStack);
        }

        // Clear redo stack when new command is executed
        _redoStack.Clear();

        // Record in history
        RecordCommand(command, CommandAction.Execute);

        // Raise event
        CommandExecuted?.Invoke(this, new CommandExecutedEventArgs(command));
    }

    /// <summary>
    /// Undoes the last command.
    /// </summary>
    public async Task<bool> UndoAsync(CancellationToken cancellationToken = default)
    {
        if (_undoStack.Count == 0)
            return false;

        var command = _undoStack.Pop();
        await command.UndoAsync(cancellationToken);

        _redoStack.Push(command);

        // Record in history
        RecordCommand(command, CommandAction.Undo);

        // Raise event
        CommandUndone?.Invoke(this, new CommandExecutedEventArgs(command));

        return true;
    }

    /// <summary>
    /// Redoes the last undone command.
    /// </summary>
    public async Task<bool> RedoAsync(CancellationToken cancellationToken = default)
    {
        if (_redoStack.Count == 0)
            return false;

        var command = _redoStack.Pop();
        await command.ExecuteAsync(cancellationToken);

        _undoStack.Push(command);

        // Record in history
        RecordCommand(command, CommandAction.Redo);

        // Raise event
        CommandRedone?.Invoke(this, new CommandExecutedEventArgs(command));

        return true;
    }

    /// <summary>
    /// Gets whether undo is available.
    /// </summary>
    public bool CanUndo => _undoStack.Count > 0;

    /// <summary>
    /// Gets whether redo is available.
    /// </summary>
    public bool CanRedo => _redoStack.Count > 0;

    /// <summary>
    /// Gets the number of commands in the undo stack.
    /// </summary>
    public int UndoCount => _undoStack.Count;

    /// <summary>
    /// Gets the number of commands in the redo stack.
    /// </summary>
    public int RedoCount => _redoStack.Count;

    /// <summary>
    /// Gets the description of the next command to undo.
    /// </summary>
    public string? NextUndoDescription => _undoStack.Count > 0 ? _undoStack.Peek().Description : null;

    /// <summary>
    /// Gets the description of the next command to redo.
    /// </summary>
    public string? NextRedoDescription => _redoStack.Count > 0 ? _redoStack.Peek().Description : null;

    /// <summary>
    /// Gets the command history.
    /// </summary>
    public IReadOnlyList<CommandHistoryEntry> CommandHistory => _commandHistory.AsReadOnly();

    /// <summary>
    /// Clears all command history and stacks.
    /// </summary>
    public void ClearHistory()
    {
        _undoStack.Clear();
        _redoStack.Clear();
        _commandHistory.Clear();
    }

    private void RecordCommand(ISearchCommand command, CommandAction action)
    {
        _commandHistory.Add(new CommandHistoryEntry
        {
            CommandId = command.CommandId,
            Description = command.Description,
            Action = action,
            Timestamp = DateTime.UtcNow
        });

        // Trim history if needed
        while (_commandHistory.Count > _maxHistorySize)
        {
            _commandHistory.RemoveAt(0);
        }
    }

    private void TrimStackIfNeeded(Stack<ISearchCommand> stack)
    {
        if (stack.Count > _maxHistorySize)
        {
            var temp = stack.ToList();
            stack.Clear();
            foreach (var cmd in temp.Take(_maxHistorySize).Reverse())
            {
                stack.Push(cmd);
            }
        }
    }
}

/// <summary>
/// Record of a command execution.
/// </summary>
public class CommandHistoryEntry
{
    public Guid CommandId { get; set; }
    public string Description { get; set; } = string.Empty;
    public CommandAction Action { get; set; }
    public DateTime Timestamp { get; set; }
}

/// <summary>
/// Type of command action.
/// </summary>
public enum CommandAction
{
    Execute,
    Undo,
    Redo
}

/// <summary>
/// Event args for command execution events.
/// </summary>
public class CommandExecutedEventArgs : EventArgs
{
    public ISearchCommand Command { get; }

    public CommandExecutedEventArgs(ISearchCommand command)
    {
        Command = command;
    }
}
