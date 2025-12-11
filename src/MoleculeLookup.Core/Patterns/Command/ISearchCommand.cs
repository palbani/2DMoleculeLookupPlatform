using System.Threading;
using System.Threading.Tasks;

namespace MoleculeLookup.Core.Patterns.Command;

/// <summary>
/// Interface for the Command pattern.
/// Each search operation is encapsulated as a command object,
/// allowing for undo/redo functionality and history management.
/// </summary>
public interface ISearchCommand
{
    /// <summary>
    /// Unique identifier for this command instance.
    /// </summary>
    Guid CommandId { get; }

    /// <summary>
    /// Human-readable description of the command.
    /// </summary>
    string Description { get; }

    /// <summary>
    /// Timestamp when the command was created.
    /// </summary>
    DateTime CreatedAt { get; }

    /// <summary>
    /// Executes the command.
    /// </summary>
    Task ExecuteAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Undoes the command (reverses its effects).
    /// </summary>
    Task UndoAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Indicates whether this command can be undone.
    /// </summary>
    bool CanUndo { get; }
}

/// <summary>
/// Interface for commands that return a result.
/// </summary>
/// <typeparam name="TResult">The type of result returned by the command</typeparam>
public interface ISearchCommand<TResult> : ISearchCommand
{
    /// <summary>
    /// Gets the result of the command execution.
    /// Null if command has not been executed yet.
    /// </summary>
    TResult? Result { get; }
}
