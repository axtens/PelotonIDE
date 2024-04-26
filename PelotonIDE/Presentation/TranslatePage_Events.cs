using DocumentFormat.OpenXml.Wordprocessing;

using Microsoft.UI.Text;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Style = Microsoft.UI.Xaml.Style;

namespace PelotonIDE.Presentation
{
    public sealed partial class TranslatePage : Microsoft.UI.Xaml.Controls.Page
    {
        private static bool LanguageHasVariableLengthInstanceInPlexes(string name) => (from plex in Plexes where plex.Meta.Language == name.Replace(" ", "") && plex.Meta.Variable select plex).Any();
        private async void ApplyButton_Click(object sender, RoutedEventArgs e)
        {
            if (targetLanguageList.SelectedIndex == -1)
            {
                ContentDialog dialog = new()
                {
                    XamlRoot = this.XamlRoot,
                    Style = Application.Current.Resources["DefaultContentDialogStyle"] as Style,
                    Title = "Target Language Not Selected",
                    PrimaryButtonText = "OK",
                };
                _ = await dialog.ShowAsync();
            }
            else
            {
                targetText.Document.GetText(TextGetOptions.None, out string txt);
                while (txt.EndsWith('\r')) txt = txt.Remove(txt.Length - 1);

                Frame.Navigate(typeof(MainPage), new NavigationData()
                {
                    Source = "TranslatePage",
                    KVPs = new() {
                        { "TargetLanguageID" , (long)targetLanguageList.SelectedIndex },
                        { "TargetVariableLength", chkVarLengthTo.IsChecked ?? false},
                        { "TargetPadOutCode", chkSpaceOut.IsChecked ?? false},
                        { "TargetText" ,  txt},
                        { "pOps.Quietude",   Quietude}
                    }
                });
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(MainPage), null);
        }

        private void ChkSpaceOut_Click(object sender, RoutedEventArgs e)
        {
            if (targetLanguageList.SelectedItem == null) return;
            if (sourceLanguageList.SelectedItem == null) return;

            sourceText.Document.GetText(TextGetOptions.None, out string code);
            targetText
                .Document
                .SetText(
                    TextSetOptions.None,
                    TranslateCode(code, ((ListBoxItem)sourceLanguageList.SelectedItem).Name, ((ListBoxItem)targetLanguageList.SelectedItem).Name));
            targetText.FlowDirection = GetFlowDirection(((ListBoxItem)targetLanguageList.SelectedItem).Name);
        }

        private void TargetLanguageList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            sourceText.Document.GetText(TextGetOptions.None, out string code);
            if (!LanguageHasVariableLengthInstanceInPlexes(((ListBoxItem)targetLanguageList.SelectedItem).Name))
            {
                chkVarLengthTo.IsChecked = false;
            }
            targetText
                .Document
                .SetText(
                    TextSetOptions.None,
                    TranslateCode(code, ((ListBoxItem)sourceLanguageList.SelectedItem).Name, ((ListBoxItem)targetLanguageList.SelectedItem).Name));
            targetText.FlowDirection = GetFlowDirection(((ListBoxItem)targetLanguageList.SelectedItem).Name);
        }

        private void SourceLanguageList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            sourceText.Document.GetText(TextGetOptions.None, out string code);
            if ((ListBoxItem)targetLanguageList.SelectedItem != null)
            {
                if (!LanguageHasVariableLengthInstanceInPlexes(((ListBoxItem)sourceLanguageList.SelectedItem).Name))
                {
                    chkVarLengthFrom.IsChecked = false;
                }
                targetText
                .Document
                .SetText(
                    TextSetOptions.None,
                    TranslateCode(code, ((ListBoxItem)sourceLanguageList.SelectedItem).Name, ((ListBoxItem)targetLanguageList.SelectedItem).Name));
                targetText.FlowDirection = GetFlowDirection(((ListBoxItem)targetLanguageList.SelectedItem).Name);
            }
        }

        private void ChkVarLengthFrom_Click(object sender, RoutedEventArgs e)
        {
            if (targetLanguageList.SelectedItem == null) return;
            if (sourceLanguageList.SelectedItem == null) return;

            sourceText.Document.GetText(TextGetOptions.None, out string code);
            if (!LanguageHasVariableLengthInstanceInPlexes(((ListBoxItem)sourceLanguageList.SelectedItem).Name))
            {
                chkVarLengthFrom.IsChecked = false;
            }
            targetText
                .Document
                .SetText(
                    TextSetOptions.None,
                    TranslateCode(code, ((ListBoxItem)sourceLanguageList.SelectedItem).Name, ((ListBoxItem)targetLanguageList.SelectedItem).Name));
            targetText.FlowDirection = GetFlowDirection(((ListBoxItem)targetLanguageList.SelectedItem).Name);
        }

        private void ChkVarLengthTo_Click(object sender, RoutedEventArgs e)
        {
            if (targetLanguageList.SelectedItem == null) return;
            if (sourceLanguageList.SelectedItem == null) return;

            sourceText.Document.GetText(TextGetOptions.None, out string code);
            if (!LanguageHasVariableLengthInstanceInPlexes(((ListBoxItem)targetLanguageList.SelectedItem).Name))
            {
                chkVarLengthTo.IsChecked = false;
            }
            targetText
                .Document
                .SetText(
                    TextSetOptions.None,
                    TranslateCode(code, ((ListBoxItem)sourceLanguageList.SelectedItem).Name, ((ListBoxItem)targetLanguageList.SelectedItem).Name));
            targetText.FlowDirection = GetFlowDirection(((ListBoxItem)targetLanguageList.SelectedItem).Name);
        }

        private void ChkSpaceIn_Click(object sender, RoutedEventArgs e)
        {
        }

        private FlowDirection GetFlowDirection(string name)
        {
            Dictionary<string, string> globals = Langs[name]["GLOBAL"];
            if (!globals.TryGetValue("TextOrientation", out string? td))
            {
                return FlowDirection.LeftToRight;
            }
            return td.Substring(1, 1) == "0" ? FlowDirection.LeftToRight : FlowDirection.RightToLeft;
        }
    }
}
