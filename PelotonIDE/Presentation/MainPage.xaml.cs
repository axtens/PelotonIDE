﻿using Microsoft.UI;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using System.Diagnostics;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Storage.Provider;
using Windows.System;
using Windows.UI.Core;
using Newtonsoft.Json;
using System.Text;

namespace PelotonIDE.Presentation
{
    public sealed partial class MainPage : Page
    {
        readonly Dictionary<object, CustomRichEditBox> _richEditBoxes = new();
        bool outputPanelShowing = true;
        enum IDELanguage
        {
            US_English,
            Australian_English,
            French
        }
        enum OutputPanelPosition
        {
            Left,
            Bottom,
            Right
        }
        IDELanguage currentLanguage = IDELanguage.US_English;
        OutputPanelPosition outputPosition = OutputPanelPosition.Bottom;
        public MainPage()
        {
            this.InitializeComponent();
            CustomRichEditBox richEditBox = new()
            {
                Tag = "Tab1"
            };
            richEditBox.KeyDown += RichEditBox_KeyDown;
            tabControl.Content = richEditBox;
            _richEditBoxes[richEditBox.Tag] = richEditBox;
            tabControl.SelectedItem = tab1;
            var isCapsLocked = Console.CapsLock;
            var isNumLocked = Console.NumberLock;
            capsLock.Text = isCapsLocked ? "Caps Lock: On" : "Caps Lock: Off";
            numsLock.Text = isNumLocked ? "Num Lock: On" : "Num Lock: Off";
            App._window.Closed += mainWindow_Closed;
        }

        /// <summary>
        /// Load previous editor settings
        /// </summary>
        private void mainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
            if (localSettings.Values.ContainsKey("OutputPanelPosition"))
            {
                string outputPos = localSettings.Values["OutputPanelPosition"] as string ?? "Bottom";
                outputPosition = (OutputPanelPosition)Enum.Parse(typeof(OutputPanelPosition), outputPos);
                HandleOutputPanelChange(outputPosition);
            }

            if (localSettings.Values.ContainsKey("OutputHeight"))
            {
                outputPanel.Height = (double)localSettings.Values["OutputHeight"];
            }

            if (localSettings.Values.ContainsKey("OutputWidth"))
            {
                outputPanel.Width = (double)localSettings.Values["OutputWidth"];
            }

            if (localSettings.Values.ContainsKey("Language"))
            {
                string savedLang = localSettings.Values["Language"] as string ?? "US_English";
                currentLanguage = (IDELanguage)Enum.Parse(typeof(IDELanguage), savedLang);
                HandleLanguageChange(currentLanguage);
            }
        }

        /// <summary>
        /// Save current editor settings
        /// </summary>
        private void mainWindow_Closed(object sender, object e)
        {
            ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
            localSettings.Values["OutputPanelPosition"] = outputPosition.ToString();
            localSettings.Values["OutputHeight"] = outputPanel.Height;
            localSettings.Values["OutputWidth"] = outputPanel.Width;
            localSettings.Values["Language"] = currentLanguage.ToString();
        }

        #region Event Handlers

        private void RichEditBox_KeyDown(object sender, Microsoft.UI.Xaml.Input.KeyRoutedEventArgs e)
        {
            if (e.Key == VirtualKey.CapitalLock)
            {
                var isCapsLocked = Console.CapsLock;
                capsLock.Text = isCapsLocked ? "Caps Lock: On" : "Caps Lock: Off";
            }
            if (e.Key == VirtualKey.NumberKeyLock)
            {
                var isNumLocked = Console.NumberLock;
                numsLock.Text = isNumLocked ? "Num Lock: On" : "Num Lock: Off";
            }
            if (tabControl.Content is CustomRichEditBox currentRichEditBox)
            {
                currentRichEditBox.Document.GetText(Microsoft.UI.Text.TextGetOptions.None, out string text);
                wordCount.Text = text.Split(' ').Length - 1 + " words";
                int caretPosition = currentRichEditBox.Document.Selection.StartPosition;
                int lineNumber = 1;
                int charNumber = 0;
                for (int i = 0; i < caretPosition; i++)
                {
                    charNumber++;
                    if (text[i] == '\r')
                    {
                        lineNumber++;
                        charNumber = 0;
                    }
                }
                int charsSinceLastLineBreak = 1;
                for (int i = caretPosition - 1; i >= 0; i--)
                {
                    if (text[i] == '\r')
                    {
                        break;
                    }
                    charsSinceLastLineBreak++;
                }
                cursorPosition.Text = "Line " + lineNumber + ", Char " + charsSinceLastLineBreak;
            }
        }

        private void newFileButton_Click(object sender, RoutedEventArgs e)
        {
            CreateNewRichEditBox();
        }

        private async void openFileButton_Click(object sender, RoutedEventArgs e)
        {
            Open();
        }

        private void closeFileButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private async void saveFileButton_Click(object sender, RoutedEventArgs e)
        {
            Save();
        }

        private async void saveAsFileButton_Click(object sender, RoutedEventArgs e)
        {
            SaveAs();
        }

        private void copyButton_Click(object sender, RoutedEventArgs e)
        {
            CopyText();
        }

        private void cutButton_Click(object sender, RoutedEventArgs e)
        {
            Cut();
        }

        private async void pasteButton_Click(object sender, RoutedEventArgs e)
        {
            Paste();
        }

        private void selectAllButton_Click(object sender, RoutedEventArgs e)
        {
            SelectAll();
        }

        private void tabControl_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {
            if (tabControl.SelectedItem != null)
            {
                CustomTabItem navigationViewItem = (CustomTabItem)tabControl.SelectedItem;
                tabControl.Content = _richEditBoxes[navigationViewItem.Tag];
            }
        }

        private void OutputLeft_Click(object sender, RoutedEventArgs e)
        {
            HandleOutputPanelChange(OutputPanelPosition.Left);
        }

        private void OutputBottom_Click(object sender, RoutedEventArgs e)
        {
            HandleOutputPanelChange(OutputPanelPosition.Bottom);
        }

        private void OutputRight_Click(object sender, RoutedEventArgs e)
        {
            HandleOutputPanelChange(OutputPanelPosition.Right);
        }

        private async void transformButton_Click(object sender, RoutedEventArgs e)
        {
            ContentDialog dialog = new()
            {
                XamlRoot = this.XamlRoot,
                Style = Application.Current.Resources["DefaultContentDialogStyle"] as Style,
                Title = "Reverse?",
                PrimaryButtonText = "Yes",
                SecondaryButtonText = "No"
            };
            DialogContentPage dialogContentPage = new();

            CustomTabItem navigationViewItem = (CustomTabItem)tabControl.SelectedItem;
            CustomRichEditBox currentRichEditBox = _richEditBoxes[navigationViewItem.Tag];
            currentRichEditBox.Document.GetText(Microsoft.UI.Text.TextGetOptions.None, out string selectedText);
            selectedText = selectedText.TrimEnd('\r');
            dialogContentPage.SetText(selectedText);
            dialog.Content = dialogContentPage;

            var result = await dialog.ShowAsync();

            if (result == ContentDialogResult.Primary)
            {
                string reversedString = Reverse(selectedText);
                CreateNewRichEditBox();
                navigationViewItem = (CustomTabItem)tabControl.SelectedItem;
                currentRichEditBox = _richEditBoxes[navigationViewItem.Tag];
                currentRichEditBox.Document.SetText(Microsoft.UI.Text.TextSetOptions.None, reversedString);
            }
        }

        private void toggleOutputButton_Click(object sender, RoutedEventArgs e)
        {
            outputPanel.Visibility = outputPanelShowing ? Visibility.Collapsed : Visibility.Visible;
            outputPanelShowing = !outputPanelShowing;
        }

        private void Thumb_DragDelta(object sender, Microsoft.UI.Xaml.Controls.Primitives.DragDeltaEventArgs e)
        {
            var yadjust = outputPanel.Height - e.VerticalChange;
            var xRightAdjust = outputPanel.Width - e.HorizontalChange;
            var xLeftAdjust = outputPanel.Width + e.HorizontalChange;
            if (outputPosition == OutputPanelPosition.Bottom)
            {
                if (yadjust >= 0)
                {
                    outputPanel.Height = yadjust;
                }
            }
            else if (outputPosition == OutputPanelPosition.Left)
            {
                if (xLeftAdjust >= 0)
                {
                    outputPanel.Width = xLeftAdjust;
                }
            }
            else if (outputPosition == OutputPanelPosition.Right)
            {
                if (xRightAdjust >= 0)
                {
                    outputPanel.Width = xRightAdjust;
                }
            }

            if (outputPosition == OutputPanelPosition.Bottom)
            {
                this.ProtectedCursor = InputCursor.CreateFromCoreCursor(new CoreCursor(CoreCursorType.SizeNorthSouth, 0));
            }
            else
            {
                this.ProtectedCursor = InputCursor.CreateFromCoreCursor(new CoreCursor(CoreCursorType.SizeWestEast, 0));
            }
        }

        private void outputPanel_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (outputPosition == OutputPanelPosition.Bottom)
            {
                outputPanelTabView.Width = outputPanel.ActualWidth;
                outputThumb.Width = outputPanel.ActualWidth;
                outputThumb.Height = 5;
            }
            else if (outputPosition == OutputPanelPosition.Right)
            {
                outputPanelTabView.Width = outputPanel.ActualWidth;
                outputThumb.Width = 5;
                outputThumb.Height = outputPanel.ActualHeight;
            }
            else if (outputPosition == OutputPanelPosition.Left)
            {
                outputPanelTabView.Width = outputPanel.ActualWidth;
                outputThumb.Width = 5;
                outputThumb.Height = outputPanel.ActualHeight;
                Canvas.SetLeft(outputThumb, outputPanel.ActualWidth - 1);
            }
        }

        private async void outputThumb_PointerEntered(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            if (outputPosition == OutputPanelPosition.Bottom)
            {
                this.ProtectedCursor = InputCursor.CreateFromCoreCursor(new CoreCursor(CoreCursorType.SizeNorthSouth, 0));
            }
            else
            {
                this.ProtectedCursor = InputCursor.CreateFromCoreCursor(new CoreCursor(CoreCursorType.SizeWestEast, 0));
            }
        }

        private void outputThumb_PointerExited(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            this.ProtectedCursor = InputCursor.CreateFromCoreCursor(new CoreCursor(CoreCursorType.Arrow, 0));
        }

        private void outputThumb_DragCompleted(object sender, DragCompletedEventArgs e)
        {
            this.ProtectedCursor = InputCursor.CreateFromCoreCursor(new CoreCursor(CoreCursorType.Arrow, 0));
        }

        private void USEnglish_Click(object sender, RoutedEventArgs e)
        {
            HandleLanguageChange(IDELanguage.US_English);
        }

        private void AusEnglish_Click(object sender, RoutedEventArgs e)
        {
            HandleLanguageChange(IDELanguage.Australian_English);
        }

        private void French_Click(object sender, RoutedEventArgs e)
        {
            HandleLanguageChange(IDELanguage.French);
        }

        #endregion

        #region Edit Handlers

        private void CreateNewRichEditBox()
        {
            CustomRichEditBox richEditBox = new();
            richEditBox.KeyDown += RichEditBox_KeyDown;
            CustomTabItem navigationViewItem = new()
            {
                Content = "Tab " + (tabControl.MenuItems.Count + 1),
                Tag = "Tab" + (tabControl.MenuItems.Count + 1),
                IsNewFile = true,
            };
            richEditBox.Tag = navigationViewItem.Tag;
            tabControl.Content = richEditBox;
            _richEditBoxes[richEditBox.Tag] = richEditBox;
            tabControl.MenuItems.Add(navigationViewItem);
            tabControl.SelectedItem = navigationViewItem;
            richEditBox.Focus(FocusState.Keyboard);
        }

        private void Cut()
        {
            CustomTabItem navigationViewItem = (CustomTabItem)tabControl.SelectedItem;
            CustomRichEditBox currentRichEditBox = _richEditBoxes[navigationViewItem.Tag];
            string selectedText = currentRichEditBox.Document.Selection.Text;
            var dataPackage = new DataPackage();
            dataPackage.SetText(selectedText);
            Clipboard.SetContent(dataPackage);
            currentRichEditBox.Document.Selection.Delete(Microsoft.UI.Text.TextRangeUnit.Character, 1);
        }

        private async void Open()
        {
            FileOpenPicker open = new FileOpenPicker();
            open.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
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
                CustomRichEditBox newestRichEditBox = _richEditBoxes[navigationViewItem.Tag];
                using (Windows.Storage.Streams.IRandomAccessStream randAccStream =
                    await pickedFile.OpenAsync(Windows.Storage.FileAccessMode.Read))
                {
                    // Load the file into the Document property of the RichEditBox.
                    if (pickedFile.FileType == ".pr")
                    {
                        newestRichEditBox.Document.LoadFromStream(Microsoft.UI.Text.TextSetOptions.FormatRtf, randAccStream);
                        newestRichEditBox.isRTF = true;
                    }
                    else if (pickedFile.FileType == ".p")
                    {
                        string text = File.ReadAllText(pickedFile.Path, Encoding.UTF8);
                        newestRichEditBox.Document.SetText(Microsoft.UI.Text.TextSetOptions.UnicodeBidi, text);
                        newestRichEditBox.isRTF = false;
                    }
                }
                if (newestRichEditBox.isRTF)
                {
                    HandleCustomPropertyLoading(pickedFile, newestRichEditBox);
                }
            }
        }

        private async void Save()
        {
            CustomTabItem navigationViewItem = (CustomTabItem)tabControl.SelectedItem;

            if (navigationViewItem != null)
            {
                if (navigationViewItem.IsNewFile == true)
                {

                    FileSavePicker savePicker = new FileSavePicker
                    {
                        SuggestedStartLocation = PickerLocationId.DocumentsLibrary
                    };

                    // Dropdown of file types the user can save the file as
                    savePicker.FileTypeChoices.Add("Rich Text", new List<string>() { ".pr" });
                    savePicker.FileTypeChoices.Add("UTF-8", new List<string>() { ".p" });

                    string? tabTitle = navigationViewItem.Content.ToString();
                    if (tabTitle == null)
                    {
                        savePicker.SuggestedFileName = "New Document";
                    }
                    else
                    {
                        savePicker.SuggestedFileName = tabTitle;
                    }

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
                                new Windows.UI.Popups.MessageDialog("File " + file.Name + " couldn't be saved.");
                            await errorBox.ShowAsync();
                        }

                        CustomTabItem savedItem = (CustomTabItem)tabControl.SelectedItem;
                        savedItem.IsNewFile = false;
                        savedItem.Content = file.Name;
                        savedItem.SavedFilePath = file;
                        if (currentRichEditBox.isRTF)
                        {
                            HandleCustomPropertySaving(file, currentRichEditBox);
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
                                new Windows.UI.Popups.MessageDialog("File " + file.Name + " couldn't be saved.");
                            await errorBox.ShowAsync();
                        }
                        CustomTabItem savedItem = (CustomTabItem)tabControl.SelectedItem;
                        savedItem.IsNewFile = false;
                        savedItem.Content = file.Name;

                        if (currentRichEditBox.isRTF)
                        {
                            HandleCustomPropertySaving(file, currentRichEditBox);
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

                FileSavePicker savePicker = new FileSavePicker
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
                if (tabTitle == null)
                {
                    savePicker.SuggestedFileName = "New Document";
                }
                else
                {
                    savePicker.SuggestedFileName = tabTitle;
                }

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
                            new Windows.UI.Popups.MessageDialog("File " + file.Name + " couldn't be saved.");
                        await errorBox.ShowAsync();
                    }
                    CustomTabItem savedItem = (CustomTabItem)tabControl.SelectedItem;
                    savedItem.IsNewFile = false;
                    savedItem.Content = file.Name;
                    savedItem.SavedFilePath = file;

                    if (currentRichEditBox.isRTF)
                    {
                        HandleCustomPropertySaving(file, currentRichEditBox);
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

        private void Close()
        {
            if (tabControl.MenuItems.Count > 0)
            {
                CustomTabItem navigationViewItem = (CustomTabItem)tabControl.SelectedItem;
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

        #endregion

        public static string Reverse(string s)
        {
            char[] charArray = s.ToCharArray();
            Array.Reverse(charArray);
            return new string(charArray);
        }

        public void HandleCustomPropertySaving(StorageFile file, CustomRichEditBox customRichEditBox)
        {
            string rtfContent = File.ReadAllText(file.Path);

            string settingsJson = System.Text.Json.JsonSerializer.Serialize(customRichEditBox.tabSettings);

            // Manipulate the RTF content
            StringBuilder rtfBuilder = new StringBuilder(rtfContent);

            // Add a \language section
            rtfBuilder.Insert(rtfBuilder.Length, settingsJson);

            // Write the modified RTF content back to the file
            File.WriteAllText(file.Path, rtfBuilder.ToString());

        }

        public void HandleCustomPropertyLoading(StorageFile file, CustomRichEditBox customRichEditBox)
        {
            string modifiedRtfContent = File.ReadAllText(file.Path);
            int startIndex = modifiedRtfContent.LastIndexOf('{');
            int endIndex = modifiedRtfContent.Length;
            string serializedObject = modifiedRtfContent.Substring(startIndex, endIndex - startIndex);
            
            try
            {
                // Deserialize the object from JSON
                TabSpecificSettings? tabSpecificSettings = System.Text.Json.JsonSerializer.Deserialize(serializedObject, typeof(TabSpecificSettings)) as TabSpecificSettings;
                if (tabSpecificSettings != null)
                {
                    customRichEditBox.tabSettings.Setting1 = tabSpecificSettings.Setting1;
                    customRichEditBox.tabSettings.Setting2 = tabSpecificSettings.Setting2;
                }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }

        private async void HandleLanguageChange(IDELanguage lang)
        {
            var languageJsonFile = await StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///PelotonIDE\\Presentation\\LanguageConfig.json"));
            string languageJsonString = File.ReadAllText(languageJsonFile.Path);
            var languageJson = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, string>>>(languageJsonString);
            var selectedLanguage = languageJson[lang.ToString()];

            fileBar.Title = selectedLanguage["File"];
            menuNew.Text = selectedLanguage["New"];
            menuOpen.Text = selectedLanguage["Open"];
            menuSave.Text = selectedLanguage["Save"];
            menuSaveAs.Text = selectedLanguage["SaveAs"];
            menuClose.Text = selectedLanguage["Close"];
            editBar.Title = selectedLanguage["Edit"];
            menuCopy.Text = selectedLanguage["Copy"];
            menuCut.Text = selectedLanguage["Cut"];
            menuPaste.Text = selectedLanguage["Paste"];
            menuSelectAll.Text = selectedLanguage["SelectAll"];
            helpBar.Title = selectedLanguage["Help"];
            menuAbout.Text = selectedLanguage["About"];

            ToolTipService.SetToolTip(newFileButton, selectedLanguage["New"]);
            ToolTipService.SetToolTip(openFileButton, selectedLanguage["Open"]);
            ToolTipService.SetToolTip(saveFileButton, selectedLanguage["Save"]);
            ToolTipService.SetToolTip(saveAsFileButton, selectedLanguage["SaveAs"]);
            ToolTipService.SetToolTip(closeFileButton, selectedLanguage["Close"]);
            ToolTipService.SetToolTip(copyButton, selectedLanguage["Copy"]);
            ToolTipService.SetToolTip(cutButton, selectedLanguage["Cut"]);
            ToolTipService.SetToolTip(pasteButton, selectedLanguage["Paste"]);
            ToolTipService.SetToolTip(selectAllButton, selectedLanguage["SelectAll"]);
            ToolTipService.SetToolTip(transformButton, selectedLanguage["Transform"]);
            ToolTipService.SetToolTip(toggleOutputButton, selectedLanguage["ToggleOutput"]);

            currentLanguage = lang;
        }

        private void HandleOutputPanelChange(OutputPanelPosition panelPos)
        {
            if (panelPos == OutputPanelPosition.Left)
            {
                outputPosition = OutputPanelPosition.Left;

                RelativePanel.SetAlignLeftWithPanel(outputPanel, true);
                RelativePanel.SetAlignRightWithPanel(outputPanel, false);
                RelativePanel.SetBelow(outputPanel, newFileButton);
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
                outputPosition = OutputPanelPosition.Bottom;

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
                outputPosition = OutputPanelPosition.Right;

                RelativePanel.SetAlignLeftWithPanel(outputPanel, false);
                RelativePanel.SetAlignRightWithPanel(outputPanel, true);
                RelativePanel.SetBelow(outputPanel, newFileButton);
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