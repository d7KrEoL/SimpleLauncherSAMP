using Microsoft.Win32;
using System.IO;

namespace SimpleLauncher.Infrastructure.System.Utils
{
    public static class FileSelectionDialogTools
    {
        public static (bool result, string? pathOrError) ShowGameFolderSelectionDialog()
        {
            OpenFolderDialog dialog = new OpenFolderDialog();
            // dialog.CheckPathExists = true;
            dialog.Title = "Select game build directory";
            dialog.Tag = "SimpleLauncher_SelectGameBuild";
            var dialogResult = dialog.ShowDialog();
            if (dialogResult is null)
                return (false, "Unexpected game folder selection dialog close");
            if (!dialogResult.Value)
                return(false, "Game folder was not selected by user");
            if (!Directory.Exists(dialog.FolderName))
                return (false, "Selected folder does not exist");
            return (true, dialog.FolderName);
        }
    }
}
