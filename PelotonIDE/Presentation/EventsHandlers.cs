using Microsoft.UI;
using Microsoft.UI.Input;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Storage.Provider;
using Windows.System;
using Windows.UI.Core;

namespace PelotonIDE.Presentation
{
    public sealed partial class MainPage : Page
    {
 
        private async void Open()
        {
            FileOpenPicker open = new()
            {
                SuggestedStartLocation = PickerLocationId.DocumentsLibrary
            };
            open.FileTypeFilter.Add(".pr");
            open.FileTypeFilter.Add(".p");

            // For Uno.WinUI-based apps
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App._window);
            WinRT.Interop.InitializeWithWindow.Initialize(open, hwnd);

            StorageFile pickedFile = await open.PickSingleFileAsync();
            if (pickedFile != null)
            {
                CreateNewRichEditBox();
                CustomTabItem navigationViewItem = (CustomTabItem)tabControl.MenuItems[tabControl.MenuItems.Count - 1];
                navigationViewItem.IsNewFile = false;
                navigationViewItem.SavedFilePath = pickedFile;
                navigationViewItem.Content = pickedFile.Name;
                navigationViewItem.Height = 30; // FIXME is this where to do it?
                navigationViewItem.TabSettingsDict = Clone(PerTabInterpreterParameters);
                // navigationViewItem.MaxHeight = 60; // FIXME is this where to do it?
                // navigationViewItem.VerticalAlignment = VerticalAlignment.Bottom;
                CustomRichEditBox newestRichEditBox = _richEditBoxes[navigationViewItem.Tag];
                using (Windows.Storage.Streams.IRandomAccessStream randAccStream =
                    await pickedFile.OpenAsync(FileAccessMode.Read))
                {
                    // Load the file into the Document property of the RichEditBox.
                    if (pickedFile.FileType == ".pr")
                    {
                        newestRichEditBox.Document.LoadFromStream(TextSetOptions.FormatRtf, randAccStream);
                        newestRichEditBox.isRTF = true;
                        newestRichEditBox.isDirty = false;
                    }
                    else if (pickedFile.FileType == ".p")
                    {
                        string text = File.ReadAllText(pickedFile.Path, Encoding.UTF8);
                        newestRichEditBox.Document.SetText(TextSetOptions.UnicodeBidi, text);
                        newestRichEditBox.isRTF = false;
                        newestRichEditBox.isDirty = false;
                    }
                }
                if (newestRichEditBox.isRTF)
                {
                    HandleCustomPropertyLoading(pickedFile, newestRichEditBox, navigationViewItem);
                }
                // FIXME the language id of the RTF, usually
                // navigationViewItem.tabSettingJson["Language"]["Defined"] = true;
                // navigationViewItem.tabSettingJson["Language"]["Value"] = InterfaceLanguageID;
                UpdateLanguageName(navigationViewItem.TabSettingsDict);
                UpdateTabCommandLine();
            }
        }

        private async void Save()
        {
            CustomTabItem navigationViewItem = (CustomTabItem)tabControl.SelectedItem;

            if (navigationViewItem != null)
            {
                if (navigationViewItem.IsNewFile)
                {
                    FileSavePicker savePicker = new()
                    {
                        SuggestedStartLocation = PickerLocationId.DocumentsLibrary
                    };

                    // Dropdown of file types the user can save the file as
                    savePicker.FileTypeChoices.Add("Rich Text", new List<string>() { ".pr" });
                    savePicker.FileTypeChoices.Add("UTF-8", new List<string>() { ".p" });

                    string? tabTitle = navigationViewItem.Content.ToString();
                    savePicker.SuggestedFileName = tabTitle ?? "New Document";

                    // For Uno.WinUI-based apps
                    var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App._window);
                    WinRT.Interop.InitializeWithWindow.Initialize(savePicker, hwnd);

                    StorageFile file = await savePicker.PickSaveFileAsync();
                    if (file != null)
                    {
                        CustomRichEditBox currentRichEditBox = _richEditBoxes[navigationViewItem.Tag];
                        // Prevent updates to the remote version of the file until we
                        // finish making changes and call CompleteUpdatesAsync.
                        CachedFileManager.DeferUpdates(file);
                        // write to file
                        using (Windows.Storage.Streams.IRandomAccessStream randAccStream =
                        await file.OpenAsync(Windows.Storage.FileAccessMode.ReadWrite))
                        {
                            randAccStream.Size = 0;
                            if (file.FileType == ".pr")
                            {
                                currentRichEditBox.Document.SaveToStream(Microsoft.UI.Text.TextGetOptions.FormatRtf, randAccStream);
                                currentRichEditBox.isRTF = true;
                            }
                            else if (file.FileType == ".p")
                            {
                                currentRichEditBox.Document.GetText(Microsoft.UI.Text.TextGetOptions.None, out string plainText);
                                using (var dataWriter = new Windows.Storage.Streams.DataWriter(randAccStream))
                                {
                                    dataWriter.WriteString(plainText);
                                    await dataWriter.StoreAsync();
                                    await randAccStream.FlushAsync();
                                }
                                currentRichEditBox.isRTF = false;
                            }
                        }

                        // Let Windows know that we're finished changing the file so the
                        // other app can update the remote version of the file.
                        FileUpdateStatus status = await CachedFileManager.CompleteUpdatesAsync(file);
                        if (status != FileUpdateStatus.Complete)
                        {
                            Windows.UI.Popups.MessageDialog errorBox =
                                new($"File {file.Name} couldn't be saved.");
                            await errorBox.ShowAsync();
                        }

                        CustomTabItem savedItem = (CustomTabItem)tabControl.SelectedItem;
                        savedItem.IsNewFile = false;
                        savedItem.Content = file.Name;
                        savedItem.SavedFilePath = file;
                        if (currentRichEditBox.isRTF)
                        {
                            HandleCustomPropertySaving(file, currentRichEditBox, navigationViewItem);
                        }
                    }
                }
                else
                {
                    if (navigationViewItem.SavedFilePath != null)
                    {
                        CustomRichEditBox currentRichEditBox = _richEditBoxes[navigationViewItem.Tag];
                        StorageFile file = navigationViewItem.SavedFilePath;
                        CachedFileManager.DeferUpdates(file);
                        // write to file
                        using (Windows.Storage.Streams.IRandomAccessStream randAccStream =
                            await file.OpenAsync(Windows.Storage.FileAccessMode.ReadWrite))
                        {
                            randAccStream.Size = 0;

                            if (file.FileType == ".pr")
                            {
                                currentRichEditBox.Document.SaveToStream(Microsoft.UI.Text.TextGetOptions.FormatRtf, randAccStream);
                                currentRichEditBox.isRTF = true;
                            }
                            else if (file.FileType == ".p")
                            {
                                currentRichEditBox.Document.GetText(Microsoft.UI.Text.TextGetOptions.None, out string plainText);
                                using (var dataWriter = new Windows.Storage.Streams.DataWriter(randAccStream))
                                {
                                    dataWriter.WriteString(plainText);
                                    await dataWriter.StoreAsync();
                                    await randAccStream.FlushAsync();
                                }
                                currentRichEditBox.isRTF = false;
                            }
                        }

                        // Let Windows know that we're finished changing the file so the
                        // other app can update the remote version of the file.
                        FileUpdateStatus status = await CachedFileManager.CompleteUpdatesAsync(file);
                        if (status != FileUpdateStatus.Complete)
                        {
                            Windows.UI.Popups.MessageDialog errorBox =
                                new("File " + file.Name + " couldn't be saved.");
                            await errorBox.ShowAsync();
                        }
                        CustomTabItem savedItem = (CustomTabItem)tabControl.SelectedItem;
                        savedItem.IsNewFile = false;
                        savedItem.Content = file.Name;

                        if (currentRichEditBox.isRTF)
                        {
                            HandleCustomPropertySaving(file, currentRichEditBox, navigationViewItem);
                        }
                    }
                }
            }
        }

        private async void SaveAs()
        {
            CustomTabItem navigationViewItem = (CustomTabItem)tabControl.SelectedItem;

            if (navigationViewItem != null)
            {
                FileSavePicker savePicker = new()
                {
                    SuggestedStartLocation = PickerLocationId.DocumentsLibrary
                };

                // Dropdown of file types the user can save the file as
                if ((navigationViewItem.Content as string).EndsWith(".p"))
                {
                    savePicker.FileTypeChoices.Add("UTF-8", new List<string>() { ".p" });
                    savePicker.FileTypeChoices.Add("Rich Text", new List<string>() { ".pr" });
                }
                else
                {
                    savePicker.FileTypeChoices.Add("Rich Text", new List<string>() { ".pr" });
                    savePicker.FileTypeChoices.Add("UTF-8", new List<string>() { ".p" });
                }

                string? tabTitle = navigationViewItem.Content.ToString();
                savePicker.SuggestedFileName = tabTitle ?? "New Document";

                // For Uno.WinUI-based apps
                var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App._window);
                WinRT.Interop.InitializeWithWindow.Initialize(savePicker, hwnd);

                StorageFile file = await savePicker.PickSaveFileAsync();
                if (file != null)
                {
                    CustomRichEditBox currentRichEditBox = _richEditBoxes[navigationViewItem.Tag];
                    // Prevent updates to the remote version of the file until we
                    // finish making changes and call CompleteUpdatesAsync.
                    CachedFileManager.DeferUpdates(file);
                    // write to file
                    using (Windows.Storage.Streams.IRandomAccessStream randAccStream =
                        await file.OpenAsync(Windows.Storage.FileAccessMode.ReadWrite))
                    {
                        randAccStream.Size = 0;

                        if (file.FileType == ".pr")
                        {
                            currentRichEditBox.Document.SaveToStream(Microsoft.UI.Text.TextGetOptions.FormatRtf, randAccStream);
                            currentRichEditBox.isRTF = true;
                        }
                        else if (file.FileType == ".p")
                        {
                            currentRichEditBox.Document.GetText(Microsoft.UI.Text.TextGetOptions.None, out string plainText);
                            using (var dataWriter = new Windows.Storage.Streams.DataWriter(randAccStream))
                            {
                                dataWriter.WriteString(plainText);
                                await dataWriter.StoreAsync();
                                await randAccStream.FlushAsync();
                            }
                            currentRichEditBox.isRTF = false;
                        }
                    }

                    // Let Windows know that we're finished changing the file so the
                    // other app can update the remote version of the file.
                    FileUpdateStatus status = await CachedFileManager.CompleteUpdatesAsync(file);
                    if (status != FileUpdateStatus.Complete)
                    {
                        Windows.UI.Popups.MessageDialog errorBox =
                            new($"File {file.Name} couldn't be saved.");
                        await errorBox.ShowAsync();
                    }
                    CustomTabItem savedItem = (CustomTabItem)tabControl.SelectedItem;
                    savedItem.IsNewFile = false;
                    savedItem.Content = file.Name;
                    savedItem.SavedFilePath = file;

                    if (currentRichEditBox.isRTF)
                    {
                        HandleCustomPropertySaving(file, currentRichEditBox, navigationViewItem);
                    }
                }
            }
        }

        private void CopyText()
        {
            CustomTabItem navigationViewItem = (CustomTabItem)tabControl.SelectedItem;
            CustomRichEditBox currentRichEditBox = _richEditBoxes[navigationViewItem.Tag];
            string selectedText = currentRichEditBox.Document.Selection.Text;
            DataPackage dataPackage = new();
            dataPackage.SetText(selectedText);
            Clipboard.SetContent(dataPackage);
        }

        private async void Close()
        {
            if (tabControl.MenuItems.Count > 0)
            {
                CustomTabItem navigationViewItem = (CustomTabItem)tabControl.SelectedItem;
                CustomRichEditBox currentRichEditBox = _richEditBoxes[navigationViewItem.Tag];
                // var t1 = tab1;
                if (currentRichEditBox.isDirty) {
                    if (!await AreYouSureToClose()) return;
                }
                _richEditBoxes.Remove(navigationViewItem.Tag);
                tabControl.MenuItems.Remove(tabControl.SelectedItem);
                if (tabControl.MenuItems.Count > 0)
                {
                    tabControl.SelectedItem = tabControl.MenuItems[tabControl.MenuItems.Count - 1];
                }
                else
                {
                    tabControl.Content = null;
                    tabControl.SelectedItem = null;
                }
            }
        }

        private async Task<bool> AreYouSureToClose()
        {
            ContentDialog dialog = new()
            {
                XamlRoot = this.XamlRoot,
                Style = Application.Current.Resources["DefaultContentDialogStyle"] as Style,
                Title = "Document changed but not saved. Close?",
                PrimaryButtonText = "No",
                SecondaryButtonText = "Yes"
            };
            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Secondary) { return true; }
            if (result == ContentDialogResult.Primary) { return false; }
            return false;
        }

        private async void Paste()
        {
            var dataPackageView = Clipboard.GetContent();
            if (dataPackageView.Contains(StandardDataFormats.Text))
            {
                string textToPaste = await dataPackageView.GetTextAsync();

                if (!string.IsNullOrEmpty(textToPaste))
                {
                    CustomTabItem navigationViewItem = (CustomTabItem)tabControl.SelectedItem;
                    CustomRichEditBox currentRichEditBox = _richEditBoxes[navigationViewItem.Tag];
                    currentRichEditBox.Document.Selection.Paste(0);
                }
            }
        }

        private void SelectAll()
        {
            CustomTabItem navigationViewItem = (CustomTabItem)tabControl.SelectedItem;
            CustomRichEditBox currentRichEditBox = _richEditBoxes[navigationViewItem.Tag];
            currentRichEditBox.Focus(FocusState.Pointer);
            currentRichEditBox.Document.GetText(Microsoft.UI.Text.TextGetOptions.None, out var allText);
            var endPosition = allText.Length - 1;
            currentRichEditBox.Document.Selection.SetRange(0, endPosition);
        }

        private void HandleOutputPanelChange(OutputPanelPosition panelPos)
        {
            if (panelPos == OutputPanelPosition.Left)
            {
                outputPanelPosition = OutputPanelPosition.Left;

                RelativePanel.SetAlignLeftWithPanel(outputPanel, true);
                RelativePanel.SetAlignRightWithPanel(outputPanel, false);
                RelativePanel.SetBelow(outputPanel, mnuNew);
                outputPanel.Width = 200;
                outputPanel.MinWidth = 175;
                outputPanel.MaxWidth = 700;
                outputPanel.ClearValue(HeightProperty);
                outputPanel.ClearValue(MaxHeightProperty);

                RelativePanel.SetAbove(tabControl, statusBar);
                RelativePanel.SetRightOf(tabControl, outputPanel);
                RelativePanel.SetAlignLeftWithPanel(tabControl, false);
                RelativePanel.SetAlignRightWithPanel(tabControl, true);

                outputLeftButton.BorderBrush = new SolidColorBrush(Colors.DodgerBlue);
                outputBottomButton.BorderBrush = new SolidColorBrush(Colors.LightGray);
                outputRightButton.BorderBrush = new SolidColorBrush(Colors.LightGray);

                outputLeftButton.Background = new SolidColorBrush(Colors.DeepSkyBlue);
                outputBottomButton.Background = new SolidColorBrush(Colors.Transparent);
                outputRightButton.Background = new SolidColorBrush(Colors.Transparent);

                Canvas.SetLeft(outputThumb, outputPanel.Width - 1);
                Canvas.SetTop(outputThumb, 0);

                outputDockingFlyout.Hide();
            }
            else if (panelPos == OutputPanelPosition.Bottom)
            {
                outputPanelPosition = OutputPanelPosition.Bottom;

                RelativePanel.SetAlignLeftWithPanel(tabControl, true);
                RelativePanel.SetAlignRightWithPanel(tabControl, true);
                RelativePanel.SetRightOf(tabControl, null);
                RelativePanel.SetAbove(tabControl, outputPanel);

                RelativePanel.SetAlignLeftWithPanel(outputPanel, true);
                RelativePanel.SetAlignRightWithPanel(outputPanel, true);
                RelativePanel.SetBelow(outputPanel, null);
                outputPanel.Height = 200;
                outputPanel.MaxHeight = 500;
                outputPanel.ClearValue(WidthProperty);
                outputPanel.ClearValue(MaxWidthProperty);

                outputBottomButton.BorderBrush = new SolidColorBrush(Colors.DodgerBlue);
                outputLeftButton.BorderBrush = new SolidColorBrush(Colors.LightGray);
                outputRightButton.BorderBrush = new SolidColorBrush(Colors.LightGray);

                outputBottomButton.Background = new SolidColorBrush(Colors.DeepSkyBlue);
                outputLeftButton.Background = new SolidColorBrush(Colors.Transparent);
                outputRightButton.Background = new SolidColorBrush(Colors.Transparent);

                Canvas.SetLeft(outputThumb, 0);
                Canvas.SetTop(outputThumb, -4);

                outputDockingFlyout.Hide();
            }
            else if (panelPos == OutputPanelPosition.Right)
            {
                outputPanelPosition = OutputPanelPosition.Right;

                RelativePanel.SetAlignLeftWithPanel(outputPanel, false);
                RelativePanel.SetAlignRightWithPanel(outputPanel, true);
                RelativePanel.SetBelow(outputPanel, mnuNew);
                outputPanel.Width = 200;
                outputPanel.MinWidth = 175;
                outputPanel.MaxWidth = 700;
                outputPanel.ClearValue(HeightProperty);
                outputPanel.ClearValue(MaxHeightProperty);

                RelativePanel.SetAbove(tabControl, statusBar);
                RelativePanel.SetLeftOf(tabControl, outputPanel);
                RelativePanel.SetAlignLeftWithPanel(tabControl, true);
                RelativePanel.SetAlignRightWithPanel(tabControl, false);

                outputRightButton.BorderBrush = new SolidColorBrush(Colors.DodgerBlue);
                outputBottomButton.BorderBrush = new SolidColorBrush(Colors.LightGray);
                outputLeftButton.BorderBrush = new SolidColorBrush(Colors.LightGray);

                outputRightButton.Background = new SolidColorBrush(Colors.DeepSkyBlue);
                outputBottomButton.Background = new SolidColorBrush(Colors.Transparent);
                outputLeftButton.Background = new SolidColorBrush(Colors.Transparent);

                Canvas.SetLeft(outputThumb, -4);
                Canvas.SetTop(outputThumb, 0);

                outputDockingFlyout.Hide();
            }
        }

    }
}
