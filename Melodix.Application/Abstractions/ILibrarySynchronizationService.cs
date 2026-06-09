using Melodix.Application.DTOs;

namespace Melodix.Application.Abstractions;

public interface ILibrarySynchronizationService
{
    Task<LibraryLoadResult> SynchronizeAsync(CancellationToken cancellationToken = default);
}
