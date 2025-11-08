using System.Diagnostics;

namespace SimpleLauncher.Domain.Abstractions
{
    public interface IProcessInjectionService
    {
        void Dispose();
        Task<bool> InjectLibraryAsync(Process process, 
            string libraryPath, 
            CancellationToken cancellationToken);
    }
}