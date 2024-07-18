using Microsoft.UI.Input;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml.Input;

using System.Diagnostics;




// using Uno.Extensions.Authentication.WinUI;

using Windows.ApplicationModel.DataTransfer;
using Windows.System;
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
            SelectionHighlightColorWhenNotFocused = new SolidColorBrush(Microsoft.UI.Colors.DarkSlateGray);
        }
        private void CustomRichEditBox_SelectionChanged(object sender, RoutedEventArgs e)
        {
            Telemetry.Disable();
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
            Telemetry.Disable();
            Telemetry.Transmit(((RichEditBox)sender).Name, e.GetType().FullName);
            base.OnPointerReleased(e);
        }
        protected override void OnKeyDown(KeyRoutedEventArgs e)
        {
            Telemetry.Enable();
            CoreVirtualKeyStates appState = InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Application);
            CoreVirtualKeyStates insState = InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Insert);
            CoreVirtualKeyStates ctrlState = InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Control);
            CoreVirtualKeyStates shiftState = InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Shift);

            bool CtrlIsDown = ctrlState.HasFlag(CoreVirtualKeyStates.Down);
            bool CtrlIsLocked = ctrlState.HasFlag(CoreVirtualKeyStates.Locked);
            bool ShiftIsDown = shiftState.HasFlag(CoreVirtualKeyStates.Down);
            bool ShiftIsLocked = shiftState.HasFlag(CoreVirtualKeyStates.Locked);
            bool InsIsDown = insState.HasFlag(CoreVirtualKeyStates.Down);
            bool InsIsLocked = insState.HasFlag(CoreVirtualKeyStates.Locked);
            bool AppIsDown = appState.HasFlag(CoreVirtualKeyStates.Down);
            bool AppIsLocked = appState.HasFlag(CoreVirtualKeyStates.Locked);

            Telemetry.Transmit("appState=", appState);
            Telemetry.Transmit("insState=", insState);
            Telemetry.Transmit("ctrlState=", ctrlState);
            Telemetry.Transmit("shiftState=", shiftState);
            Telemetry.Transmit("e.Key=", e.Key);

            if (e.Key == VirtualKey.X && CtrlIsDown)
            {
                Cut();
                return;
            }
            if (e.Key == VirtualKey.C && CtrlIsDown)
            {
                CopyText();
                return;
            }
            if (e.Key == VirtualKey.V && CtrlIsDown)
            {
                PasteText();
                return;
            }
            if (e.Key == VirtualKey.A && CtrlIsDown)
            {
                SelectAll();
                return;
            }
            if (e.Key == VirtualKey.Tab && ShiftIsDown && CtrlIsDown)
            {
                Telemetry.Transmit("^ShiftTab");
                e.Handled = false;
                return;
            }
            if (e.Key == VirtualKey.Tab && CtrlIsDown)
            {
                Telemetry.Transmit("^Tab");
                e.Handled = false;
                return;
            }
            if (e.Key == VirtualKey.Tab && ShiftIsDown)
            {
                Telemetry.Transmit("ShiftTab");
                e.Handled = true;
                return;
            }

            if (e.Key == VirtualKey.Tab)
            {
                Telemetry.Transmit("Tab");
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
