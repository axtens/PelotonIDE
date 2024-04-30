using Microsoft.UI;
using Microsoft.UI.Text;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
                    { "SourceSpec", inFocusTab.SavedFilePath == null ? inFocusTab.Content : inFocusTab.SavedFilePath.Path},
                    { "SourcePath", $"{savedFilePath ?? mostRecentPickedFilePath ?? Scripts}" },
                    { "pOps.Quietude", Type_3_GetInFocusTab<long>("pOps.Quietude") },
                    { "InFocusTabSettingsDict", inFocusTab.TabSettingsDict! },
                    { "Plexes", Plexes! }
                }
            });
        }
        private void ToggleOutputButton_Click(object sender, RoutedEventArgs e)
        {
            bool outputPanelShowing = Type_1_GetVirtualRegistry<bool>("ideOps.OutputPanelShowing");
            outputPanel.Visibility = outputPanelShowing ? Visibility.Collapsed : Visibility.Visible;
            outputPanelShowing = !outputPanelShowing;
            Type_1_UpdateVirtualRegistry<bool>("ideOps.OutputPanelShowing", outputPanelShowing);
        }
        private void RunCodeButton_Click(object sender, RoutedEventArgs e)
        {
            CustomTabItem navigationViewItem = (CustomTabItem)tabControl.SelectedItem;
            CustomRichEditBox currentRichEditBox = _richEditBoxes[navigationViewItem.Tag];
            currentRichEditBox.Document.GetText(TextGetOptions.UseCrlf, out string selectedText); // FIXME don't interpret nothig
            selectedText = selectedText.TrimEnd("\r\n");
            if (selectedText.Length > 0)
                ExecuteInterpreter(selectedText);
        }
        private void RunSelectedCodeButton_Click(object sender, RoutedEventArgs e)
        {
            Windows.UI.Color highlight = Windows.UI.Color.FromArgb(0x00, 0x8d, 0x6e, 0x5b);
            Windows.UI.Color normal = Windows.UI.Color.FromArgb(0x00,0xf9,0xf8, 0xbd);

            CustomTabItem navigationViewItem = (CustomTabItem)tabControl.SelectedItem;
            CustomRichEditBox currentRichEditBox = _richEditBoxes[navigationViewItem.Tag];

            ITextSelection selection = currentRichEditBox.Document.Selection;

            selection.CharacterFormat.BackgroundColor = highlight;
            string selectedText = selection.Text;
            selectedText = selectedText.TrimEnd('\r');
            if (selectedText.Length > 0)
            {
                ExecuteInterpreter(selectedText.Replace("\r", "\r\n")); // FIXME pass in some kind of identifier to connect to the tab
            }

            selection.CharacterFormat.BackgroundColor = normal;
            selection.SelectOrDefault(x => x);
        }
    }
}
