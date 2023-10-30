using Microsoft.UI;
using Microsoft.UI.Input;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;

using Newtonsoft.Json;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Storage.Provider;
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

        private void InterpretBar_RunningMode_Click(object sender, RoutedEventArgs e)
        {
            FontIcon tickIcon = new FontIcon()
            {
                FontFamily = new FontFamily("Segoe MDL2 Assets"),
                Glyph = "\uF0B7"
            };

            foreach (var item in from key in new string[] { "mnuQuiet", "mnuVerbose", "mnuVerbosePauseOnExit" }
                                 let items = from item in mnuRunningMode.Items where item.Name == key select item
                                 from item in items
                                 select item)
            {
                (item as MenuFlyoutItem).Icon = null;
            }
            GlobalInterpreterParameters["Quietude"]["Defined"] = false;

            var me = sender as MenuFlyoutItem;
            var clicked = me.Name;
            mnuRunningMode.Tag = clicked;
            switch (clicked)
            {
                case "mnuQuiet":
                    me.Icon = tickIcon;
                    GlobalInterpreterParameters["Quietude"]["Value"] = 0;
                    break;
                case "mnuVerbose":
                    me.Icon = tickIcon;
                    GlobalInterpreterParameters["Quietude"]["Value"] = 1;
                    break;
                case "mnuVerbosePauseOnExit":
                    me.Icon = tickIcon;
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
            if (tabControl.Content is CustomRichEditBox currentRichEditBox)
            {
                currentRichEditBox.isDirty = true;
                currentRichEditBox.Document.GetText(TextGetOptions.None, out string text);
                //wordCount.Text = text.Split(' ').Length - 1 + " words";
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
            HandleLanguageChange(name);
        }

        private async void HandleLanguageChange(string langName)
        {
            var selectedLanguage = LanguageSettings[langName];
            SetMenuText(selectedLanguage["frmMain"]);
            var selLang = selectedLanguage["GLOBAL"]["153"];
            InterfaceLanguageName = langName;
            InterfaceLanguageID = long.Parse(selectedLanguage["GLOBAL"]["ID"]);
            languageName.Text = selectedLanguage["GLOBAL"]["101"];
            PerTabInterpreterParameters["Language"]["Defined"] = true;
            PerTabInterpreterParameters["Language"]["Value"] = InterfaceLanguageID;
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
                    if (selectedLanguage.ContainsKey(mii.Name))
                        HandlePossibleAmpersand(selectedLanguage[mii.Name], mii);
                }
            }

            HandlePossibleAmpersand(selectedLanguage["mnuQuiet"], mnuQuiet);
            HandlePossibleAmpersand(selectedLanguage["mnuVerbose"], mnuVerbose);
            HandlePossibleAmpersand(selectedLanguage["mnuVerbosePauseOnExit"], mnuVerbosePauseOnExit);

            HandlePossibleAmpersand(selectedLanguage["chkSpaceOut"], mnuSpaced);
            HandlePossibleAmpersand(selectedLanguage["cmdClearAll"], mnuReset);

            ToolTipService.SetToolTip(butNew, selectedLanguage["new.Tip"]);
            ToolTipService.SetToolTip(butOpen, selectedLanguage["open.Tip"]);
            ToolTipService.SetToolTip(butSave, selectedLanguage["save.Tip"]);
            ToolTipService.SetToolTip(butSaveAs, selectedLanguage["save.Tip"]);
            // ToolTipService.SetToolTip(butClose, selectedLanguage["close.Tip"]);
            ToolTipService.SetToolTip(butCopy, selectedLanguage["copy.Tip"]);
            ToolTipService.SetToolTip(butCut, selectedLanguage["cut.Tip"]);
            ToolTipService.SetToolTip(butPaste, selectedLanguage["paste.Tip"]);
            //ToolTipService.SetToolTip(butSelectAll, selectedLanguage["mnuDeselect"]);
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
            /*ContentDialog dialog = new()
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
            }*/
            ContentDialog dialog = new()
            {
                XamlRoot = this.XamlRoot,
                Style = Application.Current.Resources["DefaultContentDialogStyle"] as Style,
                Title = "Translator - Currently not implemented",
                PrimaryButtonText = "OK",
            };
            var result = await dialog.ShowAsync();

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
            ExecuteInterpreter(selectedText.TrimEnd('\r'));  // FIXME pass in some kind of identifier to connect to the tab
        }

        private void RunSelectedCodeButton_Click(object sender, RoutedEventArgs e)
        {
            CustomTabItem navigationViewItem = (CustomTabItem)tabControl.SelectedItem;
            CustomRichEditBox currentRichEditBox = _richEditBoxes[navigationViewItem.Tag];

            var selection = currentRichEditBox.Document.Selection;

            string selectedText = selection.Text;
            selectedText.TrimEnd('\r');
            if (selectedText.Length > 0)
            {
                ExecuteInterpreter(selectedText); // FIXME pass in some kind of identifier to connect to the tab
            }
        }

        private void VariableLength_Click(object sender, RoutedEventArgs e)
        {
            FontIcon tickIcon = new FontIcon()
            {
                FontFamily = new FontFamily("Segoe MDL2 Assets"),
                Glyph = "\uF0B7"
            };

            CustomTabItem navigationViewItem = (CustomTabItem)tabControl.SelectedItem;
            CustomRichEditBox currentRichEditBox = _richEditBoxes[navigationViewItem.Tag];

            if (mnuVariableLength.Icon == null)
            {
                mnuVariableLength.Icon = tickIcon;
                navigationViewItem.TabSettingsDict["VariableLength"]["Defined"] = true;
                navigationViewItem.TabSettingsDict["VariableLength"]["Value"] = true;
                LastSelectedVariableLength = 1;
            }
            else
            {
                mnuVariableLength.Icon = null;
                navigationViewItem.TabSettingsDict["VariableLength"]["Defined"] = false;
                navigationViewItem.TabSettingsDict["VariableLength"]["Value"] = false;
                LastSelectedVariableLength = 0;
            }

            UpdateTabCommandLine();
        }

        private void Spaced_Click(object sender, RoutedEventArgs e)
        {
            FontIcon tickIcon = new FontIcon()
            {
                FontFamily = new FontFamily("Segoe MDL2 Assets"),
                Glyph = "\uF0B7"
            };

            CustomTabItem navigationViewItem = (CustomTabItem)tabControl.SelectedItem;
            CustomRichEditBox currentRichEditBox = _richEditBoxes[navigationViewItem.Tag];

            if (mnuSpaced.Icon == null)
            {
                mnuSpaced.Icon = tickIcon;
                navigationViewItem.TabSettingsDict["Spaced"]["Defined"] = true;
                navigationViewItem.TabSettingsDict["Spaced"]["Value"] = true;
                LastSelectedSpaced = 1;
            }
            else
            {
                mnuSpaced.Icon = null;
                navigationViewItem.TabSettingsDict["Spaced"]["Defined"] = false;
                navigationViewItem.TabSettingsDict["Spaced"]["Value"] = false;
                LastSelectedSpaced = 0;
            }

            UpdateTabCommandLine();
        }

        private void MnuLanguage_Click(object sender, RoutedEventArgs e)
        {
            var me = (MenuFlyoutItem)sender;
            var lang = me.Name;

            CustomTabItem navigationViewItem = (CustomTabItem)tabControl.SelectedItem;
            CustomRichEditBox currentRichEditBox = _richEditBoxes[navigationViewItem.Tag];

            var text = me.Text;

            var id = LanguageSettings[lang]["GLOBAL"]["ID"];

            navigationViewItem.TabSettingsDict["Language"]["Defined"] = true;
            navigationViewItem.TabSettingsDict["Language"]["Value"] = long.Parse(id);

            LastSelectedInterpreterLanguageName = lang;
            LastSelectedInterpreterLanguageID = long.Parse(id);

            UpdateLanguageName(navigationViewItem.TabSettingsDict);
            //languageName.Text = LanguageSettings[InterfaceLanguageName]["GLOBAL"][$"{101+int.Parse(id)}"]; // FIXME? the international language setting actually, not lang

            UpdateTabCommandLine();
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

            Dictionary<string, object> dict = new();
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
                Title = "PelotonIDE v20230614",
                Content = "Based on original code by\r\nHakob Chalikyan <hchalikyan3@gmail.com>",
                CloseButtonText = "OK"
            };
            _ = dialog.ShowAsync();

        }

    }
}
