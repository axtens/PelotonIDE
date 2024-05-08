using Microsoft.UI.Text;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;

using System.Text;

using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Storage.Provider;

using TabSettingJson = System.Collections.Generic.Dictionary<string, System.Collections.Generic.Dictionary<string, object>>;

namespace PelotonIDE.Presentation
{
    public sealed partial class MainPage : Page
    {
        private void TabViewItem_Output_RightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            FrameworkElement? senderElement = sender as FrameworkElement;
            FlyoutBase flyoutBase = FlyoutBase.GetAttachedFlyout(senderElement);
            flyoutBase.ShowAt(senderElement);
        }
        private void TabViewItem_Error_RightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            FrameworkElement? senderElement = sender as FrameworkElement;
            FlyoutBase flyoutBase = FlyoutBase.GetAttachedFlyout(senderElement);
            flyoutBase.ShowAt(senderElement);
        }
        private async void OutputTab_Contextual_SaveToFile_Click(object sender, RoutedEventArgs e)
        {
            FileSavePicker savePicker = new()
            {
                SuggestedStartLocation = PickerLocationId.DocumentsLibrary
            };

            // Dropdown of file types the user can save the file as
            savePicker.FileTypeChoices.Add("Text File", new List<string>() { ".txt" });

            savePicker.SuggestedFileName = "Output";

            // For Uno.WinUI-based apps
            nint hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App._window);
            WinRT.Interop.InitializeWithWindow.Initialize(savePicker, hwnd);

            StorageFile file = await savePicker.PickSaveFileAsync();
            if (file != null)
            {
                // Prevent updates to the remote version of the file until we
                // finish making changes and call CompleteUpdatesAsync.
                CachedFileManager.DeferUpdates(file);

                outputText.Focus(FocusState.Pointer);
                outputText.Document.GetText(TextGetOptions.UseCrlf, out string? output);

                // write to file
                await FileIO.WriteTextAsync(file, output);

                // Let Windows know that we're finished changing the file so the
                // other app can update the remote version of the file.
                FileUpdateStatus status = await CachedFileManager.CompleteUpdatesAsync(file);
                if (status != FileUpdateStatus.Complete)
                {
                    Windows.UI.Popups.MessageDialog errorBox =
                        new($"File {file.Name} couldn't be saved.");
                    await errorBox.ShowAsync();
                }
            }
        }
        private void OutputTab_Contextual_SaveToClipboard_Click(object sender, RoutedEventArgs e)
        {
            Focus(FocusState.Pointer);
            outputText.Document.GetText(TextGetOptions.None, out string? allText);
            int endPosition = allText.Length - 1;
            if (endPosition > 0)
            {
                outputText.Document.Selection.SetRange(0, endPosition);
                DataPackage dataPackage = new();
                dataPackage.SetText(allText);
                Clipboard.SetContent(dataPackage);
            }
        }
        private void OutputTab_Contextual_Clear_Click(object sender, RoutedEventArgs e)
        {
            //outputText.IsReadOnly = false;
            outputText.Document.SetText(TextSetOptions.None, null);
            //outputText.IsReadOnly = true;
        }
        private async void ErrorTab_Contextual_SaveToFile_Click(object sender, RoutedEventArgs e)
        {
            FileSavePicker savePicker = new()
            {
                SuggestedStartLocation = PickerLocationId.DocumentsLibrary
            };

            // Dropdown of file types the user can save the file as
            savePicker.FileTypeChoices.Add("Text File", new List<string>() { ".txt" });

            savePicker.SuggestedFileName = "Error";

            // For Uno.WinUI-based apps
            nint hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App._window);
            WinRT.Interop.InitializeWithWindow.Initialize(savePicker, hwnd);

            StorageFile file = await savePicker.PickSaveFileAsync();
            if (file != null)
            {
                // Prevent updates to the remote version of the file until we
                // finish making changes and call CompleteUpdatesAsync.
                CachedFileManager.DeferUpdates(file);

                errorText.Focus(FocusState.Pointer);
                errorText.Document.GetText(TextGetOptions.UseCrlf, out string? error);
                // write to file
                await FileIO.WriteTextAsync(file, error);

                // Let Windows know that we're finished changing the file so the
                // other app can update the remote version of the file.
                FileUpdateStatus status = await CachedFileManager.CompleteUpdatesAsync(file);
                if (status == FileUpdateStatus.Complete) return;
                Windows.UI.Popups.MessageDialog errorBox =
                    new($"File {file.Name} couldn't be saved.");
                await errorBox.ShowAsync();
            }
        }
        private void ErrorTab_Contextual_SaveToClipboard_Click(object sender, RoutedEventArgs e)
        {
            {
                errorText.Focus(FocusState.Pointer);
                errorText.Document.GetText(TextGetOptions.None, out string? allText);
                int endPosition = allText.Length - 1;
                if (endPosition <= 0) return;
                errorText.Document.Selection.SetRange(0, endPosition);
                DataPackage dataPackage = new();
                dataPackage.SetText(allText);
                Clipboard.SetContent(dataPackage);
            }
        }
        private void ErrorTab_Contextual_Clear_Click(object sender, RoutedEventArgs e)
        {
            //errorText.IsReadOnly = false;
            errorText.Document.SetText(TextSetOptions.None, null);
            //errorText.IsReadOnly = true;
        }
        private void HtmlTab_Contextual_SaveToFile_Click(object sender, RoutedEventArgs e)
        {
            Telemetry.EnableIfMethodNameInFactorySettingsTelemetry();
            MenuFlyoutItem me = (MenuFlyoutItem)sender;
            Telemetry.Transmit(me.Name);
        }
        private void HtmlTab_Contextual_SaveToClipboard_Click(object sender, RoutedEventArgs e)
        {
            Telemetry.EnableIfMethodNameInFactorySettingsTelemetry();
            MenuFlyoutItem me = (MenuFlyoutItem)sender;
            Telemetry.Transmit(me.Name);
        }
        private void HtmlTab_Contextual_Clear_Click(object sender, RoutedEventArgs e)
        {
            Telemetry.EnableIfMethodNameInFactorySettingsTelemetry();
            MenuFlyoutItem me = (MenuFlyoutItem)sender;
            Telemetry.Transmit(me.Name);

            string code = Convert.ToBase64String(Encoding.UTF8.GetBytes("<html></html>"));
            LogoText.Source = new Uri($"data:text/html;base64,{code}");
        }
        private void LogoTab_Contextual_SaveToFile_Click(object sender, RoutedEventArgs e)
        {
            Telemetry.EnableIfMethodNameInFactorySettingsTelemetry();
            MenuFlyoutItem me = (MenuFlyoutItem)sender;
            Telemetry.Transmit(me.Name);
        }
        private void LogoTab_Contextual_SaveToClipboard_Click(object sender, RoutedEventArgs e)
        {
            Telemetry.EnableIfMethodNameInFactorySettingsTelemetry();
            MenuFlyoutItem me = (MenuFlyoutItem)sender;
            Telemetry.Transmit(me.Name);
        }
        private void LogoTab_Contextual_Clear_Click(object sender, RoutedEventArgs e)
        {
            Telemetry.EnableIfMethodNameInFactorySettingsTelemetry();
            MenuFlyoutItem me = (MenuFlyoutItem)sender;
            Telemetry.Transmit(me.Name);

            string code = Convert.ToBase64String(Encoding.UTF8.GetBytes(TurtleFrameworkPlus("turtle.clear()")));
            LogoText.Source = new Uri($"data:text/html;base64,{code}");
        }
        private void TabViewItem_RTF_RightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            FrameworkElement? senderElement = sender as FrameworkElement;
            FlyoutBase flyoutBase = FlyoutBase.GetAttachedFlyout(senderElement);
            flyoutBase.ShowAt(senderElement);
        }
        private void RtfTab_Contextual_SaveToFile_Click(object sender, RoutedEventArgs e)
        {
        }
        private void RtfTab_Contextual_SaveToClipboard_Click(object sender, RoutedEventArgs e)
        {
        }
        private void RtfTab_Contextual_Clear_Click(object sender, RoutedEventArgs e)
        {
            rtfText.Document.SetText(TextSetOptions.None, null);
        }
        private void TabViewItem_Tapped(object sender, TappedRoutedEventArgs e)
        {
            Telemetry.EnableIfMethodNameInFactorySettingsTelemetry();
            TabViewItem me = (TabViewItem)sender;
            long tag = long.Parse((string)me.Tag);
            Type_3_UpdateInFocusTabSettings<long>("outputOps.TappedRenderer", true, tag);
            Type_2_UpdatePerTabSettings<long>("outputOps.TappedRenderer", true, tag);
            Type_1_UpdateVirtualRegistry<long>("outputOps.TappedRenderer", tag);

            UpdateStatusBar();
            UpdateOutputTabs();
        }
        private void TabViewItem_Html_RightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            Telemetry.EnableIfMethodNameInFactorySettingsTelemetry();
            TabViewItem me = (TabViewItem)sender;
            Telemetry.Transmit(me.Name);

            FrameworkElement? senderElement = sender as FrameworkElement;
            FlyoutBase flyoutBase = FlyoutBase.GetAttachedFlyout(senderElement);
            flyoutBase.ShowAt(senderElement);
        }
        private void TabViewItem_Logo_RightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            Telemetry.EnableIfMethodNameInFactorySettingsTelemetry();
            TabViewItem me = (TabViewItem)sender;
            Telemetry.Transmit(me.Name);

            FrameworkElement? senderElement = sender as FrameworkElement;
            FlyoutBase flyoutBase = FlyoutBase.GetAttachedFlyout(senderElement);
            flyoutBase.ShowAt(senderElement);
        }
    }
}