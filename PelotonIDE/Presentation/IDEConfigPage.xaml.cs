using Windows.Storage;
using Windows.Storage.Pickers;

using LanguageConfigurationStructureSelection =
    System.Collections.Generic.Dictionary<string,
        System.Collections.Generic.Dictionary<string, string>>;

namespace PelotonIDE.Presentation
{
    public sealed partial class IDEConfigPage : Page
    {
        public IDEConfigPage()
        {
            this.InitializeComponent();
        }
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            NavigationData parameters = (NavigationData)e.Parameter;

            if (parameters.Source == "MainPage")
            {
                protiumInterpreterTextBox.Text = parameters.KVPs["ideOps.Engine.2"].ToString();
                pelotonInterpreterTextBox.Text = parameters.KVPs["ideOps.Engine.3"].ToString();
                sourceTextBox.Text = parameters.KVPs["ideOps.ScriptsFolder"].ToString();
                dataTextBox.Text = parameters.KVPs["ideOps.DataFolder"].ToString();
                LanguageConfigurationStructureSelection lcs = (LanguageConfigurationStructureSelection)parameters.KVPs["pOps.Language"];
                cmdCancel.Content = lcs["frmMain"]["cmdCancel"];
                cmdSaveMemory.Content = lcs["frmMain"]["cmdSaveMemory"];
                lblSourceDirectory.Text = lcs["frmMain"]["lblSourceDirectory"];
            }
        }
        private async void ProtiumInterpreterLocationBtn_Click(object sender, RoutedEventArgs e)
        {
            FileOpenPicker open = new()
            {
                SuggestedStartLocation = PickerLocationId.DocumentsLibrary
            };
            open.FileTypeFilter.Add(".exe");

            // For Uno.WinUI-based apps
            nint hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App._window);
            WinRT.Interop.InitializeWithWindow.Initialize(open, hwnd);

            StorageFile pickedFile = await open.PickSingleFileAsync();
            if (pickedFile != null)
            {
                protiumInterpreterTextBox.Text = pickedFile.Path;
            }
        }
        private async void PelotonInterpreterLocationBtn_Click(object sender, RoutedEventArgs e)
        {
            FileOpenPicker open = new()
            {
                SuggestedStartLocation = PickerLocationId.DocumentsLibrary
            };
            open.FileTypeFilter.Add(".exe");

            // For Uno.WinUI-based apps
            nint hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App._window);
            WinRT.Interop.InitializeWithWindow.Initialize(open, hwnd);

            StorageFile pickedFile = await open.PickSingleFileAsync();
            if (pickedFile != null)
            {
                pelotonInterpreterTextBox.Text = pickedFile.Path;
            }
        }
        private async void SourceDirectoryBtn_Click(object sender, RoutedEventArgs e)
        {
            FolderPicker folderPicker = new()
            {
                SuggestedStartLocation = PickerLocationId.DocumentsLibrary
            };
            folderPicker.FileTypeFilter.Add("*");

            // For Uno.WinUI-based apps
            nint hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App._window);
            WinRT.Interop.InitializeWithWindow.Initialize(folderPicker, hwnd);

            StorageFolder pickedFolder = await folderPicker.PickSingleFolderAsync();
            if (pickedFolder != null)
            {
                sourceTextBox.Text = pickedFolder.Path;
            }
        }
        private async void DataDirectoryBtn_Click(object sender, RoutedEventArgs e)
        {
            FolderPicker folderPicker = new()
            {
                SuggestedStartLocation = PickerLocationId.DocumentsLibrary
            };
            folderPicker.FileTypeFilter.Add("*");

            // For Uno.WinUI-based apps
            nint hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App._window);
            WinRT.Interop.InitializeWithWindow.Initialize(folderPicker, hwnd);

            StorageFolder pickedFolder = await folderPicker.PickSingleFolderAsync();
            if (pickedFolder != null)
            {
                dataTextBox.Text = pickedFolder.Path;
            }

        }
        private void IDEConfig_Apply_Button_Click(object sender, RoutedEventArgs e)
        {
            NavigationData nd = new()
            {
                Source = "IDEConfig",
                KVPs = new()
                {
                    { "ideOps.Engine.2" , protiumInterpreterTextBox.Text },
                    { "ideOps.Engine.3" , pelotonInterpreterTextBox.Text },
                    { "ideOps.ScriptsFolder" ,  sourceTextBox.Text},
                    { "ideOps.DataFolder", dataTextBox.Text },
                }
            };
            Frame.Navigate(typeof(MainPage), nd);

        }
        private void IDEConfig_Cancel_Button_Click(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(MainPage), null);
        }

    }
}