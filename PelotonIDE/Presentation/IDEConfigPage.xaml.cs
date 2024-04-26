using Microsoft.UI;
using Microsoft.UI.Input;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Input;

using Newtonsoft.Json;

using System.Diagnostics;
using System.Text;

using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Storage.Provider;
using Windows.System;
using Windows.UI;
using Windows.UI.Core;

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
                interpreterTextBox.Text = parameters.KVPs["Interpreter"].ToString();
                sourceTextBox.Text = parameters.KVPs["ideOps.ScriptsFolder"].ToString();
                LanguageConfigurationStructureSelection lcs = (LanguageConfigurationStructureSelection)parameters.KVPs["Language"];
                cmdCancel.Content = lcs["frmMain"]["cmdCancel"];
                cmdSaveMemory.Content = lcs["frmMain"]["cmdSaveMemory"];
                lblSourceDirectory.Text = lcs["frmMain"]["lblSourceDirectory"];
            }
        }
        private async void InterpreterLocationBtn_Click(object sender, RoutedEventArgs e)
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
                interpreterTextBox.Text = pickedFile.Path;
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

        private void IDEConfig_Apply_Button_Click(object sender, RoutedEventArgs e)
        {
            NavigationData nd = new()
            {
                Source = "IDEConfig",
                KVPs = new()
                {
                    { "Interpreter" , interpreterTextBox.Text },
                    { "ideOps.ScriptsFolder" ,  sourceTextBox.Text}
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