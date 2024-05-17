using System.Diagnostics;
using System.Xml.XPath;

using Windows.Storage;


namespace PelotonIDE.Presentation
{
    internal static class FileFolderPicking
    {
        public static string[] GetFile(string? title,
                                       string? initialDirectory,
                                       string? filter = "PR files (*.pr)|*.pr|P files (*.p)|*.p|All files (*.*)|*.*",
                                       int index = 1)
        {
            string guid = Guid.NewGuid().ToString();
            string code = $@"
[System.Reflection.Assembly]::LoadWithPartialName('System.windows.forms') | Out-Null
$dlg = New-Object System.Windows.Forms.OpenFileDialog
$dlg.initialDirectory = '{initialDirectory}'
$dlg.filter = '{filter}'
$dlg.FilterIndex = {index}
$dlg.Title = '{title}'
$dlg.ShowHelp = $False
$dlg.AutoUpgradeEnabled = $True
$dlg.AddToRecent = $True
$result = $dlg.ShowDialog()
'{{0}} :: {{1}}' -f $result, $dlg.FileName
";
            var ps1 = Path.Combine(ApplicationData.Current.LocalFolder.Path, guid + ".ps1");
            File.WriteAllText(ps1, code);
            Process process = new()
            {
                StartInfo = new ProcessStartInfo()
                {
                    FileName = @"pwsh.exe",
                    Arguments = $@"{Path.Combine(ApplicationData.Current.LocalFolder.Path, guid + ".ps1")}",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };
            process.Start();


            var result = process.StandardOutput.ReadToEnd().Trim().Split(" :: ");
            try
            {
                File.Delete(ps1);
            }
            catch (Exception ex)
            {
                Telemetry.Transmit(ex.Message);
            }
            return result;
        }
        public static string[] SaveFile(string? title,
                                      string? initialDirectory,
                                      string? filename,
                                      string? filter = "PR files (*.pr)|*.pr|P files (*.p)|*.p|All files (*.*)|*.*",
                                      bool checkFileExists = false,
                                      bool checkPathExists = false,
                                      bool checkWriteAccess = false,
                                      int index = 1)
        {
            string guid = Guid.NewGuid().ToString();
            string code = $@"
[System.Reflection.Assembly]::LoadWithPartialName('System.windows.forms') | Out-Null
$dlg = New-Object System.Windows.Forms.SaveFileDialog 
$dlg.initialDirectory = '{initialDirectory}'
$dlg.filter = '{filter}'
$dlg.FilterIndex = {index}
$dlg.Title = '{title}'
$dlg.FileName = '{filename}'
$dlg.ShowHelp = $False
$dlg.AutoUpgradeEnabled = $True
$dlg.CheckFileExists = ${(checkFileExists ? "True" : "False")}
$dlg.CheckWriteAccess = ${(checkWriteAccess ? "True" : "False")}
$dlg.AddToRecent = $True
$dlg.DefaultExt = 'pr'
$result = $dlg.ShowDialog()
'{{0}} :: {{1}}' -f $result, $dlg.FileName
";
            var ps1 = Path.Combine(ApplicationData.Current.LocalFolder.Path, guid + ".ps1");
            File.WriteAllText(ps1, code);
            Process process = new()
            {
                StartInfo = new ProcessStartInfo()
                {
                    FileName = @"pwsh.exe",
                    Arguments = $@"{Path.Combine(ApplicationData.Current.LocalFolder.Path, $"{guid}.ps1")}",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };
            process.Start();


            var result = process.StandardOutput.ReadToEnd().Trim().Split(" :: ");
            try
            {
                File.Delete(ps1);
            }
            catch (Exception ex)
            {
                Telemetry.Transmit(ex.Message);
            }
            return result;
        } 
        public static string[] GetFolder(string? initialDirectory)
        {
            string guid = Guid.NewGuid().ToString();
            string code = $@"
[System.Reflection.Assembly]::LoadWithPartialName('System.windows.forms') | Out-Null
$dlg = New-Object System.Windows.Forms.FolderBrowserDialog
$dlg.initialDirectory = '{initialDirectory}'
$dlg.ShowNewFolderButton = $True
$dlg.ShowPinnedPlaces = $True
$dlg.AutoUpgradeEnabled = $True
$dlg.AddToRecent = $True
$result = $dlg.ShowDialog()
'{{0}} :: {{1}}' -f $result, $dlg.SelectedPath 
";
            var ps1 = Path.Combine(ApplicationData.Current.LocalFolder.Path, guid + ".ps1");
            File.WriteAllText(ps1, code);
            Process process = new()
            {
                StartInfo = new ProcessStartInfo()
                {
                    FileName = @"pwsh.exe",
                    Arguments = $@"{Path.Combine(ApplicationData.Current.LocalFolder.Path, guid + ".ps1")}",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };
            process.Start();

            var result = process.StandardOutput.ReadToEnd().Trim().Split(" :: ");
            try
            {
                File.Delete(ps1);
            }
            catch (Exception ex)
            {
                Telemetry.Transmit(ex.Message);
            }
            return result;
        }
    }
}
