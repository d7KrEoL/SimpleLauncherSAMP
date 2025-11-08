using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace SimpleLauncher.Presentation.ViewModels
{
    public class ServerInfoWindowViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
