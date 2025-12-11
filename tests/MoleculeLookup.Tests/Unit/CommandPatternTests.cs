using FluentAssertions;
using Moq;
using MoleculeLookup.Core.Interfaces;
using MoleculeLookup.Core.Models;
using MoleculeLookup.Core.Patterns.Command;
using Xunit;

namespace MoleculeLookup.Tests.Unit;

/// <summary>
/// Unit tests for the Command pattern implementation.
/// </summary>
public class CommandPatternTests
{
    private readonly Mock<ISearchHistoryRepository> _mockRepository;
    private readonly SearchCommandInvoker _invoker;

    public CommandPatternTests()
    {
        _mockRepository = new Mock<ISearchHistoryRepository>();
        _invoker = new SearchCommandInvoker();
    }

    #region AddToHistoryCommand Tests

    [Fact]
    public async Task AddToHistoryCommand_Execute_AddsEntry()
    {
        // Arrange
        var entry = CreateTestEntry();
        _mockRepository
            .Setup(r => r.AddAsync(It.IsAny<SearchHistoryEntry>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(entry);

        var command = new AddToHistoryCommand(_mockRepository.Object, entry);

        // Act
        await command.ExecuteAsync();

        // Assert
        _mockRepository.Verify(r => r.AddAsync(entry, It.IsAny<CancellationToken>()), Times.Once);
        command.Result.Should().NotBeNull();
    }

    [Fact]
    public async Task AddToHistoryCommand_Undo_DeletesEntry()
    {
        // Arrange
        var entry = CreateTestEntry();
        _mockRepository
            .Setup(r => r.AddAsync(It.IsAny<SearchHistoryEntry>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(entry);
        _mockRepository
            .Setup(r => r.DeleteAsync(entry.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var command = new AddToHistoryCommand(_mockRepository.Object, entry);
        await command.ExecuteAsync();

        // Act
        await command.UndoAsync();

        // Assert
        _mockRepository.Verify(r => r.DeleteAsync(entry.Id, It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region DeleteFromHistoryCommand Tests

    [Fact]
    public async Task DeleteFromHistoryCommand_Execute_DeletesEntry()
    {
        // Arrange
        var entry = CreateTestEntry();
        _mockRepository
            .Setup(r => r.GetByIdAsync(entry.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entry);
        _mockRepository
            .Setup(r => r.DeleteAsync(entry.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var command = new DeleteFromHistoryCommand(_mockRepository.Object, entry.Id);

        // Act
        await command.ExecuteAsync();

        // Assert
        _mockRepository.Verify(r => r.DeleteAsync(entry.Id, It.IsAny<CancellationToken>()), Times.Once);
        command.Result.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteFromHistoryCommand_Undo_RestoresEntry()
    {
        // Arrange
        var entry = CreateTestEntry();
        _mockRepository
            .Setup(r => r.GetByIdAsync(entry.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entry);
        _mockRepository
            .Setup(r => r.DeleteAsync(entry.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _mockRepository
            .Setup(r => r.AddAsync(It.IsAny<SearchHistoryEntry>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(entry);

        var command = new DeleteFromHistoryCommand(_mockRepository.Object, entry.Id);
        await command.ExecuteAsync();

        // Act
        await command.UndoAsync();

        // Assert
        _mockRepository.Verify(r => r.AddAsync(It.IsAny<SearchHistoryEntry>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region ToggleFavoriteCommand Tests

    [Fact]
    public async Task ToggleFavoriteCommand_Execute_TogglesFavorite()
    {
        // Arrange
        var entry = CreateTestEntry();
        entry.IsFavorite = false;

        _mockRepository
            .Setup(r => r.GetByIdAsync(entry.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entry);
        _mockRepository
            .Setup(r => r.UpdateAsync(It.IsAny<SearchHistoryEntry>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SearchHistoryEntry e, CancellationToken _) => e);

        var command = new ToggleFavoriteCommand(_mockRepository.Object, entry.Id);

        // Act
        await command.ExecuteAsync();

        // Assert
        command.Result!.IsFavorite.Should().BeTrue();
    }

    #endregion

    #region SearchCommandInvoker Tests

    [Fact]
    public async Task Invoker_ExecuteCommand_AddsToUndoStack()
    {
        // Arrange
        var entry = CreateTestEntry();
        _mockRepository
            .Setup(r => r.AddAsync(It.IsAny<SearchHistoryEntry>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(entry);

        var command = new AddToHistoryCommand(_mockRepository.Object, entry);

        // Act
        await _invoker.ExecuteAsync(command);

        // Assert
        _invoker.CanUndo.Should().BeTrue();
        _invoker.UndoCount.Should().Be(1);
    }

    [Fact]
    public async Task Invoker_Undo_MovesToRedoStack()
    {
        // Arrange
        var entry = CreateTestEntry();
        _mockRepository
            .Setup(r => r.AddAsync(It.IsAny<SearchHistoryEntry>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(entry);
        _mockRepository
            .Setup(r => r.DeleteAsync(entry.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var command = new AddToHistoryCommand(_mockRepository.Object, entry);
        await _invoker.ExecuteAsync(command);

        // Act
        var undoResult = await _invoker.UndoAsync();

        // Assert
        undoResult.Should().BeTrue();
        _invoker.CanUndo.Should().BeFalse();
        _invoker.CanRedo.Should().BeTrue();
    }

    [Fact]
    public async Task Invoker_Redo_RestoresCommand()
    {
        // Arrange
        var entry = CreateTestEntry();
        _mockRepository
            .Setup(r => r.AddAsync(It.IsAny<SearchHistoryEntry>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(entry);
        _mockRepository
            .Setup(r => r.DeleteAsync(entry.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var command = new AddToHistoryCommand(_mockRepository.Object, entry);
        await _invoker.ExecuteAsync(command);
        await _invoker.UndoAsync();

        // Act
        var redoResult = await _invoker.RedoAsync();

        // Assert
        redoResult.Should().BeTrue();
        _invoker.CanUndo.Should().BeTrue();
        _invoker.CanRedo.Should().BeFalse();
    }

    [Fact]
    public async Task Invoker_NewCommand_ClearsRedoStack()
    {
        // Arrange
        var entry1 = CreateTestEntry();
        var entry2 = CreateTestEntry();

        _mockRepository
            .Setup(r => r.AddAsync(It.IsAny<SearchHistoryEntry>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SearchHistoryEntry e, CancellationToken _) => e);
        _mockRepository
            .Setup(r => r.DeleteAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var command1 = new AddToHistoryCommand(_mockRepository.Object, entry1);
        var command2 = new AddToHistoryCommand(_mockRepository.Object, entry2);

        await _invoker.ExecuteAsync(command1);
        await _invoker.UndoAsync();
        _invoker.CanRedo.Should().BeTrue();

        // Act - Execute new command
        await _invoker.ExecuteAsync(command2);

        // Assert - Redo stack should be cleared
        _invoker.CanRedo.Should().BeFalse();
    }

    [Fact]
    public async Task Invoker_CommandHistory_TracksAllActions()
    {
        // Arrange
        var entry = CreateTestEntry();
        _mockRepository
            .Setup(r => r.AddAsync(It.IsAny<SearchHistoryEntry>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(entry);
        _mockRepository
            .Setup(r => r.DeleteAsync(entry.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var command = new AddToHistoryCommand(_mockRepository.Object, entry);

        // Act
        await _invoker.ExecuteAsync(command);
        await _invoker.UndoAsync();
        await _invoker.RedoAsync();

        // Assert
        _invoker.CommandHistory.Should().HaveCount(3);
        _invoker.CommandHistory[0].Action.Should().Be(CommandAction.Execute);
        _invoker.CommandHistory[1].Action.Should().Be(CommandAction.Undo);
        _invoker.CommandHistory[2].Action.Should().Be(CommandAction.Redo);
    }

    #endregion

    #region Helper Methods

    private SearchHistoryEntry CreateTestEntry()
    {
        return new SearchHistoryEntry
        {
            Id = Guid.NewGuid(),
            SmilesString = "CCO",
            MoleculeName = "Ethanol",
            IsFavorite = false
        };
    }

    #endregion
}
