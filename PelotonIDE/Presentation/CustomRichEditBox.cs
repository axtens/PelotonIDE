using DocumentFormat.OpenXml.Office2019.Presentation;

using Microsoft.UI;
using Microsoft.UI.Input;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

using Newtonsoft.Json;

using System.Diagnostics;

using Uno.Extensions;



// using Uno.Extensions.Authentication.WinUI;

using Windows.ApplicationModel.DataTransfer;
using Windows.System;
using Windows.UI;
using Windows.UI.Core;

namespace PelotonIDE.Presentation
{
    public partial class CustomRichEditBox : RichEditBox
    {
        public bool IsRTF { get; set; }
        public bool IsDirty { get; set; }
        //public string PreviousSelection { get; set; }

        public CustomRichEditBox()
        {
            IsSpellCheckEnabled = false;
            IsRTF = true;
            SelectionFlyout = null;
            ContextFlyout = null;
            TextAlignment = TextAlignment.DetectFromContent;
            FlowDirection = FlowDirection.LeftToRight;
            FontFamily = new FontFamily("Lucida Sans Unicode,Tahoma");
            PointerReleased += CustomRichEditBox_PointerReleased;
            SelectionChanged += CustomRichEditBox_SelectionChanged;
        }

        private void CustomRichEditBox_SelectionChanged(object sender, RoutedEventArgs e)
        {
            Telemetry.SetEnabled(false);
            CustomRichEditBox me = ((CustomRichEditBox)sender);
            ITextSelection selection = me.Document.Selection;
            selection.GetText(TextGetOptions.None, out string text);
            Telemetry.Transmit(text);
            selection.SelectOrDefault(x => x);
            int caretPosition = selection.StartPosition;
            int start = selection.StartPosition;
            int end = selection.EndPosition;
            Telemetry.Transmit("start=", start, "end=", end);
            if (start != end)
            {

            }
        }

        private void CustomRichEditBox_PointerReleased(object sender, PointerRoutedEventArgs e)
        {
            Telemetry.SetEnabled(false);
            Telemetry.Transmit(((RichEditBox)sender).Name, e.GetType().FullName);
            base.OnPointerReleased(e);
        }

        protected override void OnKeyDown(KeyRoutedEventArgs e)
        {
            Telemetry.SetEnabled(false);
            CoreVirtualKeyStates ctrlState = InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Control);
            CoreVirtualKeyStates shiftState = InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Shift);
            bool isCtrlPressed = ctrlState.HasFlag(CoreVirtualKeyStates.Down);
            bool isShiftPressed = shiftState.HasFlag(CoreVirtualKeyStates.Locked);

            if (e.Key == VirtualKey.X && isCtrlPressed)
            {
                Cut();
                return;
            }
            if (e.Key == VirtualKey.C && isCtrlPressed)
            {
                CopyText();
                return;
            }
            if (e.Key == VirtualKey.V && isCtrlPressed)
            {
                PasteText();
                return;
            }
            if (e.Key == VirtualKey.A && isCtrlPressed)
            {
                SelectAll();
                return;
            }
            if (e.Key == VirtualKey.Tab && isCtrlPressed)
            {
                int direction = ctrlState.ToString().Contains("Locked") ? -1 : 1;
                Telemetry.Transmit("e.Key=", e.Key, "ctrlState=", ctrlState, "shiftState=", shiftState, "isCtrlPressed=", isCtrlPressed, "isShiftPressed=", isShiftPressed);
                e.Handled = true;
                return;
            }
            if (e.Key == VirtualKey.Tab)
            {
                Telemetry.Transmit("e.Key=", e.Key, "ctrlState=", ctrlState, "shiftState=", shiftState, "isCtrlPressed=", isCtrlPressed, "isShiftPressed=", isShiftPressed);
                if (!isShiftPressed)
                    Document.Selection.TypeText("\t");
                e.Handled = true;
                return;
            }
            base.OnKeyDown(e);
        }

        private void Cut()
        {
            string selectedText = Document.Selection.Text;
            DataPackage dataPackage = new();
            dataPackage.SetText(selectedText);
            Clipboard.SetContent(dataPackage);
            Document.Selection.Delete(Microsoft.UI.Text.TextRangeUnit.Character, 1);
        }

        private void CopyText()
        {
            string selectedText = Document.Selection.Text;
            DataPackage dataPackage = new();
            dataPackage.SetText(selectedText);
            Clipboard.SetContent(dataPackage);
        }

        private async void PasteText()
        {
            DataPackageView dataPackageView = Clipboard.GetContent();
            if (dataPackageView.Contains(StandardDataFormats.Text))
            {
                string textToPaste = await dataPackageView.GetTextAsync();

                if (!string.IsNullOrEmpty(textToPaste))
                {
                    Document.Selection.Paste(0);
                }
            }
        }

        private void SelectAll()
        {
            Focus(FocusState.Pointer);
            Document.GetText(Microsoft.UI.Text.TextGetOptions.None, out string? allText);
            int endPosition = allText.Length - 1;
            Document.Selection.SetRange(0, endPosition);
        }
    }
}
