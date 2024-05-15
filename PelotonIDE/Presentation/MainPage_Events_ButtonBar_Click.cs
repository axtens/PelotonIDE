using Microsoft.UI.Text;

namespace PelotonIDE.Presentation
{
    public sealed partial class MainPage : Page
    {
        private void MnuTranslate_Click(object sender, RoutedEventArgs e)
        {
            CustomTabItem? inFocusTab = InFocusTab();
            //CustomTabItem navigationViewItem = (CustomTabItem)tabControl.SelectedItem;
            CustomRichEditBox currentRichEditBox = _richEditBoxes[inFocusTab.Tag];
            currentRichEditBox.Document.GetText(TextGetOptions.None, out string? text);
            long tabLangId = Type_3_GetInFocusTab<long>("pOps.Language");
            IEnumerable<string> tabLangName = from lang in LanguageSettings where long.Parse(lang.Value["GLOBAL"]["ID"]) == tabLangId select lang.Key;
            string? savedFilePath = inFocusTab.SavedFilePath != null ? Path.GetDirectoryName(inFocusTab.SavedFilePath.Path) : null;
            string? mostRecentPickedFilePath;
            if (Type_1_GetVirtualRegistry<string>("MostRecentPickedFilePath") != null)
            {
                mostRecentPickedFilePath = Type_1_GetVirtualRegistry<string>("MostRecentPickedFilePath").ToString();
            }
            else
            {
                mostRecentPickedFilePath = (string?)string.Empty;
            }

            var sourceSpec = inFocusTab.SavedFilePath == null ? inFocusTab.Content : inFocusTab.SavedFilePath.Path;
            var sourcePath = $"{savedFilePath ?? mostRecentPickedFilePath ?? Type_1_GetVirtualRegistry<string>("ideOps.ScriptsFolder")}"; // Scripts
            string? dataPath;
            if (savedFilePath != null)
            {
                dataPath = savedFilePath;
            } 
            else
            {
                
                    dataPath = Type_3_GetInFocusTab<string>("ideOps.DataFolder");
                
            }

        Frame.Navigate(typeof(TranslatePage), new NavigationData()
            {
                Source = "MainPage",
                KVPs = new()
                {
                    { "RichEditBox", (CustomRichEditBox)tabControl!.Content },
                    { "TabLanguageID",tabLangId },
                    { "TabLanguageName", tabLangName.First() },
                    { "TabVariableLength", text.Contains("<# ") && text.Contains("</#>") },
                    { "InterpreterLanguage",  Type_1_GetVirtualRegistry<long>("mainOps.InterpreterLanguageID")},
                    { "ideOps.InterfaceLanguageID", Type_1_GetVirtualRegistry<long>("ideOps.InterfaceLanguageID")},
                    { "ideOps.InterfaceLanguageName",Type_1_GetVirtualRegistry<string>("ideOps.InterfaceLanguageName") },
                    { "Languages", LanguageSettings! },
                    { "SourceSpec", sourceSpec},
                    { "SourcePath", sourcePath },
                    { "DataPath", dataPath! },
                    { "pOps.Quietude", Type_3_GetInFocusTab<long>("pOps.Quietude") },
                    { "InFocusTabSettingsDict", inFocusTab.TabSettingsDict! },
                    { "Plexes", Plexes! }
                }
            });

        }
        private void MnuTranslateButton_Click(object sender, RoutedEventArgs e)
        {
            MnuTranslate_Click(sender, e);
        }
        private void ToggleOutput_Click(object sender, RoutedEventArgs e)
        {
            bool outputPanelShowing = Type_1_GetVirtualRegistry<bool>("ideOps.OutputPanelShowing");
            outputPanel.Visibility = outputPanelShowing ? Visibility.Collapsed : Visibility.Visible;
            outputPanelShowing = !outputPanelShowing;
            Type_1_UpdateVirtualRegistry<bool>("ideOps.OutputPanelShowing", outputPanelShowing);

        }
        private void ToggleOutputButton_Click(object sender, RoutedEventArgs e)
        {
            ToggleOutput_Click(sender, e);
        }
        private void RunCode_Click(object sender, RoutedEventArgs e)
        {
            CustomTabItem navigationViewItem = (CustomTabItem)tabControl.SelectedItem;
            CustomRichEditBox currentRichEditBox = _richEditBoxes[navigationViewItem.Tag];
            currentRichEditBox.Document.GetText(TextGetOptions.UseCrlf, out string selectedText);
            selectedText = selectedText.TrimEnd("\r\n");
            if (selectedText.Length > 0)
                ExecuteInterpreter(selectedText);
        }
        private void RunCodeButton_Click(object sender, RoutedEventArgs e)
        {
            RunCode_Click(sender, e);
        }
        private void RunSelectedCode_Click(Object sender, RoutedEventArgs e)
        {
            Windows.UI.Color highlight = Windows.UI.Color.FromArgb(0x00, 0x8d, 0x6e, 0x5b);
            Windows.UI.Color normal = Windows.UI.Color.FromArgb(0x00, 0xf9, 0xf8, 0xbd);

            CustomTabItem navigationViewItem = (CustomTabItem)tabControl.SelectedItem;
            CustomRichEditBox currentRichEditBox = _richEditBoxes[navigationViewItem.Tag];

            ITextSelection selection = currentRichEditBox.Document.Selection;

            selection.CharacterFormat.BackgroundColor = highlight;
            string selectedText = selection.Text;
            selectedText = selectedText.TrimEnd('\r');
            if (selectedText.Length > 0)
            {
                ExecuteInterpreter(selectedText.Replace("\r", "\r\n"));
            }

            selection.CharacterFormat.BackgroundColor = normal;
            selection.SelectOrDefault(x => x);
        }
        private void RunSelectedCodeButton_Click(object sender, RoutedEventArgs e)
        {
            RunSelectedCode_Click(sender, e);
        }
        private void FileOpenButton_Click(object sender, RoutedEventArgs e)
        {
            FileOpen_Click(sender, e);
        }
        private void FileCloseButton_Click(object sender, RoutedEventArgs e)
        {
            FileClose_Click(sender, e);
        }
        private void FileSaveButton_Click(object sender, RoutedEventArgs e)
        {
            FileSaveAs_Click(sender, e);
        }
        private void FileSaveAsButton_Click(object sender, RoutedEventArgs e)
        {
            FileSaveAs_Click(sender, e);
        }
        private void EditCopyButton_Click(object sender, RoutedEventArgs e)
        {
            EditCopy_Click(sender, e);
        }
        private void EditCutButton_Click(object sender, RoutedEventArgs e)
        {
            EditCut_Click(sender, e);
        }
        private void EditPasteButton_Click(object sender, RoutedEventArgs e)
        {
            EditPaste_Click(sender, e);
        }
        private void EditSelectAllButton_Click(object sender, RoutedEventArgs e)
        {
            EditSelectAll_Click(sender, e);
        }
        private void FileNewButton_Click(object sender, RoutedEventArgs e)
        {
            FileNew_Click(sender, e);
        }
    }
}
