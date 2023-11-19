using Microsoft.UI;
using Microsoft.UI.Input;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;

using Newtonsoft.Json;
using Newtonsoft.Json.Bson;

using System.Diagnostics;
using System.Timers;

using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using Windows.System;
using Windows.UI.Core;

namespace PelotonIDE.Presentation
{
    public sealed partial class MainPage : Page
    {
        public void TimerTick(object? source, ElapsedEventArgs e)
        {
            TIME.Text = DateTime.Now.ToString("HH':'mm':'ss");
        }

        /// <summary>
        /// Load previous editor settings
        /// </summary>
        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            //{
            //    foreach (var setting in ApplicationData.Current.LocalSettings.Values)
            //    {
            //        ApplicationData.Current.LocalSettings.DeleteContainer(setting.Key);
            //    }
            //    await ApplicationData.Current.ClearAsync();
            //}

            ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
            LanguageSettings = await GetLanguageConfiguration();

            CAPS.Foreground = Console.CapsLock ? new SolidColorBrush(Colors.Black) : new SolidColorBrush(Colors.LightGray);
            NUM.Foreground = Console.NumberLock ? new SolidColorBrush(Colors.Black) : new SolidColorBrush(Colors.LightGray);

            GlobalInterpreterParameters = await MainPage.GetGlobalInterpreterParameters();
            PerTabInterpreterParameters = await MainPage.GetPerTabInterpreterParameters();

            FactorySettings = await GetFactorySettings();

            outputPanelShowing = GetFactorySettingsWithLocalSettingsOverrideOrDefault<bool>("OutputPanelShowing", true, FactorySettings, localSettings);

            var outputPanelPosition = GetFactorySettingsWithLocalSettingsOverrideOrDefault("OutputPanelPosition", (OutputPanelPosition)Enum.Parse(typeof(OutputPanelPosition), "Bottom"), FactorySettings, localSettings);

            HandleOutputPanelChange(outputPanelPosition);

            outputPanel.Height = GetFactorySettingsWithLocalSettingsOverrideOrDefault<double>("OutputPanelHeight", 200, FactorySettings, localSettings);
            InterfaceLanguageName = GetFactorySettingsWithLocalSettingsOverrideOrDefault<string>("InterfaceLanguageName", "English", FactorySettings, localSettings);
            InterfaceLanguageID = GetFactorySettingsWithLocalSettingsOverrideOrDefault<long>("InterfaceLanguageID", 0, FactorySettings, localSettings);
            if (InterfaceLanguageName != null)
                HandleLanguageChange(InterfaceLanguageName);

            // Engine selection:
            //  Engine will contain either "Interpreter.P2" or "Interpreter.P3"
            //  if Engine is present in LocalSettings, use that value, otherwise retrieve it from FactorySettings and update local settings
            //  if Engine is null (for some reason FactorySettings is broken), use "Interpreter.P3"
            SetEngine();
            SetScripts();
            SetInterpreterNew();
            SetInterpreterOld();

            LastSelectedInterpreterLanguageName = GetFactorySettingsWithLocalSettingsOverrideOrDefault<string>("LastSelectedInterpreterLanguageName", "English", FactorySettings, localSettings);
            LastSelectedInterpreterLanguageID = GetFactorySettingsWithLocalSettingsOverrideOrDefault<long>("LastSelectedInterpreterLanguageID", 0, FactorySettings, localSettings);
            PerTabInterpreterParameters["Language"]["Defined"] = true;
            PerTabInterpreterParameters["Language"]["Value"] = LastSelectedInterpreterLanguageID;

            LastSelectedVariableLength = GetFactorySettingsWithLocalSettingsOverrideOrDefault<bool>("LastSelectedVariableLength", false, FactorySettings, localSettings);
            PerTabInterpreterParameters["VariableLength"]["Defined"] = LastSelectedVariableLength;
            PerTabInterpreterParameters["VariableLength"]["Value"] = LastSelectedVariableLength;

            //LastSelectedSpaced = GetFactorySettingsWithLocalSettingsOverrideOrDefault<bool>("LastSelectedSpaced", false, FactorySettings, localSettings);
            //PerTabInterpreterParameters["Spaced"]["Defined"] = LastSelectedSpaced;
            //PerTabInterpreterParameters["Spaced"]["Value"] = LastSelectedSpaced;

            LastSelectedQuietude = GetFactorySettingsWithLocalSettingsOverrideOrDefault<long>("Quietude", 2, FactorySettings, localSettings);
            GlobalInterpreterParameters["Quietude"]["Defined"] = true;
            GlobalInterpreterParameters["Quietude"]["Value"] = LastSelectedQuietude;

            if (tab1.TabSettingsDict == null)
                tab1.TabSettingsDict = Clone(PerTabInterpreterParameters);

            tab1.TabSettingsDict["Language"]["Defined"] = true;
            tab1.TabSettingsDict["Language"]["Value"] = LastSelectedInterpreterLanguageID;

            InterfaceLanguageSelectionBuilder(mnuSelectLanguage, "mnuSelectLanguage", Internationalization_Click);
            InterpreterLanguageSelectionBuilder(mnuRun, "mnuLanguage", MnuLanguage_Click);
            UpdateEngineSelectionFromFactorySettings();

            UpdateMenuRunningMode(GlobalInterpreterParameters["Quietude"]);
            UpdateVariableLengthMode(tab1.TabSettingsDict["VariableLength"]);
            // UpdateSpacedMode(tab1.TabSettingsDict["Spaced"]);
            UpdateLanguageName(tab1.TabSettingsDict);
            UpdateTabCommandLine();

            void SetEngine()
            {
                if (LocalSettings.Values.TryGetValue("Engine", out object? value))
                {
                    Engine = value.ToString();
                }
                else
                {
                    Engine = FactorySettings["Engine"].ToString();
                    LocalSettings.Values["Engine"] = Engine;
                }
                Engine ??= "Interpreter.P3";
            }
            void SetScripts()
            {
                if (localSettings.Values.TryGetValue("Scripts", out object? value))
                {
                    Scripts = value.ToString();
                }
                else
                {
                    Scripts = FactorySettings["Scripts"].ToString();
                    LocalSettings.Values["Scripts"] = Scripts;
                }
                Scripts ??= @"C:\peloton\code";
            }
            void SetInterpreterOld()
            {
                if (localSettings.Values.TryGetValue("Interpreter.P2", out object? value))
                {
                    InterpreterOld = value.ToString();
                }
                else
                {
                    InterpreterOld = FactorySettings["Interpreter.P2"].ToString();
                    LocalSettings.Values["Interpreter.P2"] = InterpreterOld;
                }
                InterpreterOld ??= "Interpreter.P2";
            }
            void SetInterpreterNew()
            {
                if (localSettings.Values.TryGetValue("Interpreter.P3", out object? value))
                {
                    InterpreterNew = value.ToString();
                }
                else
                {
                    InterpreterNew = FactorySettings["Interpreter.P3"].ToString();
                    LocalSettings.Values["Interpreter.P3"] = InterpreterNew;
                }
                InterpreterNew ??= "Interpreter.P3";
            }
        }

        private void UpdateEngineSelectionFromFactorySettings()
        {
            if (LocalSettings.Values["Engine"].ToString() == "Interpreter.P2")
            {
                ControlHighligter(mnuNewEngine, false);
                ControlHighligter(mnuOldEngine, true);
            }
            else
            {
                ControlHighligter(mnuNewEngine, true);
                ControlHighligter(mnuOldEngine, false);
            }
        }

        private void InterpretBar_RunningMode_Click(object sender, RoutedEventArgs e)
        {
            foreach (var item in from key in new string[] { "mnuQuiet", "mnuVerbose", "mnuVerbosePauseOnExit" }
                                 let items = from item in mnuRunningMode.Items where item.Name == key select item
                                 from item in items
                                 select item)
            {
                ControlHighligter((MenuFlyoutItem)item, false);
                // (item as MenuFlyoutItem).Icon = null;
            }
            GlobalInterpreterParameters["Quietude"]["Defined"] = false;

            var me = sender as MenuFlyoutItem;
            var clicked = me.Name;
            mnuRunningMode.Tag = clicked;
            switch (clicked)
            {
                case "mnuQuiet":
                    ControlHighligter(me, true);
                    LastSelectedQuietude = 0;
                    GlobalInterpreterParameters["Quietude"]["Value"] = 0;
                    break;
                case "mnuVerbose":
                    ControlHighligter(me, true);
                    LastSelectedQuietude = 1;
                    GlobalInterpreterParameters["Quietude"]["Value"] = 1;
                    break;
                case "mnuVerbosePauseOnExit":
                    ControlHighligter(me, true);
                    LastSelectedQuietude = 2;
                    GlobalInterpreterParameters["Quietude"]["Value"] = 2;
                    break;
            }
            GlobalInterpreterParameters["Quietude"]["Defined"] = true;
            UpdateTabCommandLine();
        }

        private void FileNew_Click(object sender, RoutedEventArgs e)
        {
            CreateNewRichEditBox();
        }

        private async void FileOpen_Click(object sender, RoutedEventArgs e)
        {
            Open();
        }

        private void FileClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private async void FileSave_Click(object sender, RoutedEventArgs e)
        {
            Save();
        }

        private async void FileSaveAs_Click(object sender, RoutedEventArgs e)
        {
            SaveAs();
        }

        private void EditCopy_Click(object sender, RoutedEventArgs e)
        {
            CopyText();
        }

        private void EditCut_Click(object sender, RoutedEventArgs e)
        {
            Cut();
        }

        private async void EditPaste_Click(object sender, RoutedEventArgs e)
        {
            Paste();
        }

        private void EditSelectAll_Click(object sender, RoutedEventArgs e)
        {
            SelectAll();
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

        private void TabControl_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {
            if (tabControl.SelectedItem != null)
            {
                CustomTabItem navigationViewItem = (CustomTabItem)tabControl.SelectedItem;
                tabControl.Content = _richEditBoxes[navigationViewItem.Tag];
                if (navigationViewItem.TabSettingsDict != null)
                {
                    var currentLanguageName = GetTabsLanguageName(navigationViewItem.TabSettingsDict);
                    if (languageName.Text != currentLanguageName)
                    {
                        languageName.Text = currentLanguageName;
                    }
                    UpdateTabCommandLine();
                }
            }
        }

        private void RichEditBox_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            var Black = new SolidColorBrush(Colors.Black);
            var LightGrey = new SolidColorBrush(Colors.LightGray);

            Debug.WriteLine($"{e.Key}");
            if (e.Key == VirtualKey.CapitalLock)
            {
                CAPS.Text = "CAPS";
                CAPS.Foreground = Console.CapsLock ? Black : (Brush)LightGrey;
            }
            if (e.Key == VirtualKey.NumberKeyLock)
            {
                NUM.Text = "NUM";
                NUM.Foreground = Console.NumberLock ? Black : (Brush)LightGrey;
            }
            if (e.Key == VirtualKey.Scroll)
            {
            }
            if (e.Key == VirtualKey.Insert)
            {
            }
            if (tabControl.Content is CustomRichEditBox currentRichEditBox && !e.KeyStatus.IsExtendedKey && e.Key != VirtualKey.Control)
            {
                currentRichEditBox.IsDirty = true;
            }
        }

        private void CustomREBox_SelectionChanged(object sender, RoutedEventArgs e)
        {
            if (tabControl.Content is CustomRichEditBox currentRichEditBox)
            {
                currentRichEditBox.Document.GetText(TextGetOptions.None, out string text);
                //wordCount.Text = text.Split(' ').Length - 1 + " words";
                int caretPosition = currentRichEditBox.Document.Selection.StartPosition;
                int lineNumber = 1;
                int charNumber = 0;
                for (int i = 0; i < caretPosition; i++)
                {
                    charNumber++;
                    if (text[i] == '\v' || text[i] == '\r')
                    {
                        lineNumber++;
                        charNumber = 0;
                    }
                }
                int charsSinceLastLineBreak = 1;
                for (int i = caretPosition - 1; i >= 0; i--)
                {
                    if (text[i] == '\v' || text[i] == '\r')
                    {
                        break;
                    }
                    charsSinceLastLineBreak++;
                }
                cursorPosition.Text = "Line " + lineNumber + ", Char " + charNumber;
            }
        }

        private void Thumb_DragDelta(object sender, Microsoft.UI.Xaml.Controls.Primitives.DragDeltaEventArgs e)
        {
            var yadjust = outputPanel.Height - e.VerticalChange;
            var xRightAdjust = outputPanel.Width - e.HorizontalChange;
            var xLeftAdjust = outputPanel.Width + e.HorizontalChange;
            if (outputPanelPosition == OutputPanelPosition.Bottom)
            {
                if (yadjust >= 0)
                {
                    outputPanel.Height = yadjust;
                }
            }
            else if (outputPanelPosition == OutputPanelPosition.Left)
            {
                if (xLeftAdjust >= 0)
                {
                    outputPanel.Width = xLeftAdjust;
                }
            }
            else if (outputPanelPosition == OutputPanelPosition.Right)
            {
                if (xRightAdjust >= 0)
                {
                    outputPanel.Width = xRightAdjust;
                }
            }

            if (outputPanelPosition == OutputPanelPosition.Bottom)
            {
                this.ProtectedCursor = InputCursor.CreateFromCoreCursor(new CoreCursor(CoreCursorType.SizeNorthSouth, 0));
            }
            else
            {
                this.ProtectedCursor = InputCursor.CreateFromCoreCursor(new CoreCursor(CoreCursorType.SizeWestEast, 0));
            }
        }

        private void OutputPanel_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (outputPanelPosition == OutputPanelPosition.Bottom)
            {
                outputPanelTabView.Width = outputPanel.ActualWidth;
                outputPanelTabView.Height = outputPanel.ActualHeight;
                outputThumb.Width = outputPanel.ActualWidth;
                outputThumb.Height = 5;
            }
            else if (outputPanelPosition == OutputPanelPosition.Right)
            {
                outputPanelTabView.Width = outputPanel.ActualWidth;
                outputPanelTabView.Height = outputPanel.ActualHeight;
                outputThumb.Width = 5;
                outputThumb.Height = outputPanel.ActualHeight;
            }
            else if (outputPanelPosition == OutputPanelPosition.Left)
            {
                outputPanelTabView.Width = outputPanel.ActualWidth;
                outputPanelTabView.Height = outputPanel.ActualHeight;
                outputThumb.Width = 5;
                outputThumb.Height = outputPanel.ActualHeight;
                Canvas.SetLeft(outputThumb, outputPanel.ActualWidth - 1);
            }
        }

        private async void OutputThumb_PointerEntered(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            if (outputPanelPosition == OutputPanelPosition.Bottom)
            {
                this.ProtectedCursor = InputCursor.CreateFromCoreCursor(new CoreCursor(CoreCursorType.SizeNorthSouth, 0));
            }
            else
            {
                this.ProtectedCursor = InputCursor.CreateFromCoreCursor(new CoreCursor(CoreCursorType.SizeWestEast, 0));
            }
        }

        private void OutputThumb_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            this.ProtectedCursor = InputCursor.CreateFromCoreCursor(new CoreCursor(CoreCursorType.Arrow, 0));
        }

        private void OutputThumb_DragCompleted(object sender, DragCompletedEventArgs e)
        {
            this.ProtectedCursor = InputCursor.CreateFromCoreCursor(new CoreCursor(CoreCursorType.Arrow, 0));
        }

        private void Internationalization_Click(object sender, RoutedEventArgs e)
        {
            var me = (MenuFlyoutItem)sender;
            var name = me.Name;

            foreach (var item in mnuSelectLanguage.Items)
            {
                ControlHighligter((MenuFlyoutItem)item, item.Name == name);
            }

            HandleLanguageChange(name);
        }

        private async void HandleLanguageChange(string langName)
        {
            var selectedLanguage = LanguageSettings[langName];
            SetMenuText(selectedLanguage["frmMain"]);
            var selLang = selectedLanguage["GLOBAL"]["153"];
            InterfaceLanguageName = langName;
            InterfaceLanguageID = long.Parse(selectedLanguage["GLOBAL"]["ID"]);
            LocalSettings.Values["InterfaceLanguageName"] = InterfaceLanguageName;
            LocalSettings.Values["InterfaceLanguageID"] = InterfaceLanguageID;
            languageName.Text = selectedLanguage["GLOBAL"]["101"];
        }

        private void SetMenuText(Dictionary<string, string> selectedLanguage)
        {
            foreach (var mi in menuBar.Items)
            {
                Debug.WriteLine($"mi {mi.Name}");
                HandlePossibleAmpersand(selectedLanguage[mi.Name], mi);

                foreach (var mii in mi.Items)
                {
                    Debug.WriteLine($"mii {mii.Name}");
                    if (selectedLanguage.TryGetValue(mii.Name, out string? value))
                        HandlePossibleAmpersand(value, mii);
                }
            }

            HandlePossibleAmpersand(selectedLanguage["mnuQuiet"], mnuQuiet);
            HandlePossibleAmpersand(selectedLanguage["mnuVerbose"], mnuVerbose);
            HandlePossibleAmpersand(selectedLanguage["mnuVerbosePauseOnExit"], mnuVerbosePauseOnExit);

            // HandlePossibleAmpersand(selectedLanguage["chkSpaceOut"], mnuSpaced);
            // HandlePossibleAmpersand(selectedLanguage["cmdClearAll"], mnuReset);

            ToolTipService.SetToolTip(butNew, selectedLanguage["new.Tip"]);
            ToolTipService.SetToolTip(butOpen, selectedLanguage["open.Tip"]);
            ToolTipService.SetToolTip(butSave, selectedLanguage["save.Tip"]);
            ToolTipService.SetToolTip(butSaveAs, selectedLanguage["save.Tip"]);
            // ToolTipService.SetToolTip(butClose, selectedLanguage["close.Tip"]);
            ToolTipService.SetToolTip(butCopy, selectedLanguage["copy.Tip"]);
            ToolTipService.SetToolTip(butCut, selectedLanguage["cut.Tip"]);
            ToolTipService.SetToolTip(butPaste, selectedLanguage["paste.Tip"]);
            ToolTipService.SetToolTip(butSelectAll, selectedLanguage["mnuSelectOther(7)"]);
            ToolTipService.SetToolTip(butTransform, selectedLanguage["mnuTranslate"]);
            // ToolTipService.SetToolTip(toggleOutputButton, selectedLanguage["mnuToggleOutput"]);
            ToolTipService.SetToolTip(butGo, selectedLanguage["run.Tip"]);
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

        private async void TransformButton_Click(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(TranslatePage), new NavigationData()
            {
                Source = "MainPage",
                KVPs = new()
                {
                    { "RichEditBox", (CustomRichEditBox)tabControl!.Content },
                    { "InterpreterLanguage",  LastSelectedInterpreterLanguageID},
                    { "InterfaceLanguageID", InterfaceLanguageID},
                    { "InterfaceLanguageName",InterfaceLanguageName! },
                    { "Languages", LanguageSettings! }
                }
            });
        }

        private void ToggleOutputButton_Click(object sender, RoutedEventArgs e)
        {
            outputPanel.Visibility = outputPanelShowing ? Visibility.Collapsed : Visibility.Visible;
            outputPanelShowing = !outputPanelShowing;
        }

        // InsertCodeTemplate_Click

        private void InsertCodeTemplate_Click(object sender, RoutedEventArgs e)
        {
            CustomTabItem navigationViewItem = (CustomTabItem)tabControl.SelectedItem;
            CustomRichEditBox currentRichEditBox = _richEditBoxes[navigationViewItem.Tag];
            var selection = currentRichEditBox.Document.Selection;
            if (selection != null)
            {
                selection.StartPosition = selection.EndPosition;
                selection.Text = "<@ ></@>";
                selection.EndPosition = selection.StartPosition;
                currentRichEditBox.Document.Selection.Move(TextRangeUnit.Character, 3);
            }
        }

        private void InsertVariableLengthCodeTemplate_Click(object sender, RoutedEventArgs e)
        {
            CustomTabItem navigationViewItem = (CustomTabItem)tabControl.SelectedItem;
            CustomRichEditBox currentRichEditBox = _richEditBoxes[navigationViewItem.Tag];
            var selection = currentRichEditBox.Document.Selection;
            if (selection != null)
            {
                selection.StartPosition = selection.EndPosition;
                selection.Text = "<# ></#>";
                selection.EndPosition = selection.StartPosition;
                currentRichEditBox.Document.Selection.Move(TextRangeUnit.Character, 3);
            }
        }

        private void RunCodeButton_Click(object sender, RoutedEventArgs e)
        {
            CustomTabItem navigationViewItem = (CustomTabItem)tabControl.SelectedItem;
            CustomRichEditBox currentRichEditBox = _richEditBoxes[navigationViewItem.Tag];
            currentRichEditBox.Document.GetText(TextGetOptions.None, out string selectedText);
            ExecuteInterpreter(selectedText.TrimEnd('\r').Replace("\r", "\r\n"));  // FIXME pass in some kind of identifier to connect to the tab
        }

        private void RunSelectedCodeButton_Click(object sender, RoutedEventArgs e)
        {
            CustomTabItem navigationViewItem = (CustomTabItem)tabControl.SelectedItem;
            CustomRichEditBox currentRichEditBox = _richEditBoxes[navigationViewItem.Tag];

            var selection = currentRichEditBox.Document.Selection;

            string selectedText = selection.Text;
            selectedText = selectedText.TrimEnd('\r');
            if (selectedText.Length > 0)
            {
                ExecuteInterpreter(selectedText.Replace("\r", "\r\n")); // FIXME pass in some kind of identifier to connect to the tab
            }
        }

        private void VariableLength_Click(object sender, RoutedEventArgs e)
        {
            CustomTabItem navigationViewItem = (CustomTabItem)tabControl.SelectedItem;
            CustomRichEditBox currentRichEditBox = _richEditBoxes[navigationViewItem.Tag];

            if (!LastSelectedVariableLength /*mnuVariableLength.Icon == null*/ )
            {
                ControlHighligter(mnuVariableLength, true);
                navigationViewItem.TabSettingsDict["VariableLength"]["Defined"] = true;
                navigationViewItem.TabSettingsDict["VariableLength"]["Value"] = true;
                LastSelectedVariableLength = true;
            }
            else
            {
                ControlHighligter(mnuVariableLength, false);
                navigationViewItem.TabSettingsDict["VariableLength"]["Defined"] = false;
                navigationViewItem.TabSettingsDict["VariableLength"]["Value"] = false;
                LastSelectedVariableLength = false;
            }

            UpdateTabCommandLine();
        }

        private void MnuLanguage_Click(object sender, RoutedEventArgs e)
        {
            var me = (MenuFlyoutItem)sender;
            var lang = me.Name;

            // iterate the list, and turn off the highlight then assert highlight on chosen

            CustomTabItem navigationViewItem = (CustomTabItem)tabControl.SelectedItem;
            CustomRichEditBox currentRichEditBox = _richEditBoxes[navigationViewItem.Tag];

            var text = me.Text;

            var id = LanguageSettings[lang]["GLOBAL"]["ID"];

            var currentTabSettings = navigationViewItem.TabSettingsDict;
            currentTabSettings["Language"]["Defined"] = true;
            currentTabSettings["Language"]["Value"] = long.Parse(id);

            LastSelectedInterpreterLanguageName = lang;
            LastSelectedInterpreterLanguageID = long.Parse(id);

            LocalSettings.Values["LastSelectedInterpreterLanguageName"] = LastSelectedInterpreterLanguageName;
            LocalSettings.Values["LastSelectedInterpreterLanguageID"] = LastSelectedInterpreterLanguageID;
            LocalSettings.Values["LastSelectedVariableLength"] = LastSelectedVariableLength;

            PerTabInterpreterParameters["Language"]["Defined"] = true;
            PerTabInterpreterParameters["Language"]["Value"] = long.Parse(id);

            UpdateLanguageInInterpreterMenu(mnuRun, LastSelectedInterpreterLanguageName);

            UpdateLanguageName(navigationViewItem.TabSettingsDict);
            //languageName.Text = LanguageSettings[InterfaceLanguageName]["GLOBAL"][$"{101+int.Parse(id)}"]; // FIXME? the international language setting actually, not lang

            UpdateTabCommandLine();
        }

        private void UpdateLanguageInInterpreterMenu(MenuBarItem mnuRun, string lastSelectedInterpreterLanguageName)
        {
            var subMenus = from menu in mnuRun.Items where menu.Name == "mnuLanguage" select menu;
            if (subMenus != null)
            {
                var first = subMenus.First();
                foreach (var item in ((MenuFlyoutSubItem)first).Items)
                {
                    if (item.Name == lastSelectedInterpreterLanguageName)
                    {
                        item.Foreground = new SolidColorBrush(Colors.White);
                        item.Background = new SolidColorBrush(Colors.Black);
                    }
                    else
                    {
                        item.Foreground = new SolidColorBrush(Colors.Black);
                        item.Background = new SolidColorBrush(Colors.White);
                    }
                }
            }
        }

        private async void ResetToFactorySettings_Click(object sender, RoutedEventArgs e)
        {
            ContentDialog dialog = new()
            {
                XamlRoot = this.XamlRoot,
                Style = Application.Current.Resources["DefaultContentDialogStyle"] as Style,
                Title = "Factory Reset",
                Content = "Confirm reset. Application will shut down after reset.",
                PrimaryButtonText = "OK",
                SecondaryButtonText = "Cancel",
                DefaultButton = ContentDialogButton.Secondary,
            };
            var result = await dialog.ShowAsync();

            if (result is ContentDialogResult.Secondary)
            {
                return;
            }

            Dictionary<string, object> dict = [];
            var fac = await GetFactorySettings();
            File.WriteAllText(Path.Combine(Path.GetTempPath(), "PelotonIDE_FactorySettings_log.json"), JsonConvert.SerializeObject(fac));

            foreach (var key in ApplicationData.Current.LocalSettings.Values)
            {
                dict.Add(key.Key, key.Value);
            }
            File.WriteAllText(Path.Combine(Path.GetTempPath(), "PelotonIDE_LocalSettings_log.json"), JsonConvert.SerializeObject(dict));

            foreach (var setting in ApplicationData.Current.LocalSettings.Values)
            {
                ApplicationData.Current.LocalSettings.DeleteContainer(setting.Key);
            }
            await ApplicationData.Current.ClearAsync();

            Environment.Exit(0);
        }
        private void HelpAbout_Click(object sender, RoutedEventArgs e)
        {
            ContentDialog dialog = new()
            {
                XamlRoot = this.XamlRoot,
                Style = Application.Current.Resources["DefaultContentDialogStyle"] as Style,
                Title = "PelotonIDE v1.0",
                Content = "", // Based on original code by\r\nHakob Chalikyan <hchalikyan3@gmail.com>",
                CloseButtonText = "OK"
            };
            _ = dialog.ShowAsync();
        }

        private void MnuIDEConfiguration_Click(object sender, RoutedEventArgs e)
        {
            var interp = LocalSettings.Values["Interpreter.P3"].ToString();

            Frame.Navigate(typeof(IDEConfigPage), new NavigationData()
            {
                Source = "MainPage",
                KVPs = new()
                {
                    { "Interpreter", interp!},
                    { "Scripts",  Scripts!},
                    { "Language", LanguageSettings[InterfaceLanguageName!] }
                }
            });
        }

        private void MnuTranslate_Click(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(TranslatePage), new NavigationData()
            {
                Source = "MainPage",
                KVPs = new()
                {
                    { "RichEditBox", (CustomRichEditBox)tabControl!.Content },
                    { "InterpreterLanguage",  LastSelectedInterpreterLanguageID},
                    { "InterfaceLanguageID", InterfaceLanguageID},
                    { "InterfaceLanguageName",InterfaceLanguageName! },
                    { "Languages", LanguageSettings! }
                }
            });
        }

        private void ChooseNewEngine_Click(object sender, RoutedEventArgs e)
        {
            ControlHighligter(mnuNewEngine, true);
            ControlHighligter(mnuOldEngine, false);
            Engine = LocalSettings.Values["Interpreter.P3"].ToString();
        }

        private void ChooseOldEngine_Click(object sender, RoutedEventArgs e)
        {
            ControlHighligter(mnuNewEngine, false);
            ControlHighligter(mnuOldEngine, true);
            Engine = LocalSettings.Values["Interpreter.P2"].ToString();
        }
    }
}
