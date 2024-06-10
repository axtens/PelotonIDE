using Microsoft.UI;
using Microsoft.UI.Input;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml.Input;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Windows.System;
using Windows.UI.Core;

namespace PelotonIDE.Presentation
{
    public sealed partial class MainPage : Page
    {
        private void RichEditBox_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            Telemetry.Disable();

            var me = (RichEditBox)sender;
            SolidColorBrush Black = new(Colors.Black);
            SolidColorBrush LightGrey = new(Colors.LightGray);

            CoreVirtualKeyStates appState = InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Application);
            CoreVirtualKeyStates insState = InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Insert);
            CoreVirtualKeyStates ctrlState = InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Control);
            CoreVirtualKeyStates shiftState = InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Shift);

            bool ctrlIsDown = ctrlState.HasFlag(CoreVirtualKeyStates.Down);
            bool ctrlIsLocked = ctrlState.HasFlag(CoreVirtualKeyStates.Locked);
            bool shiftIsDown = shiftState.HasFlag(CoreVirtualKeyStates.Down);
            bool shiftIsLocked = shiftState.HasFlag(CoreVirtualKeyStates.Locked);
            bool insIsDown = insState.HasFlag(CoreVirtualKeyStates.Down);
            bool insIsLocked = insState.HasFlag(CoreVirtualKeyStates.Locked);
            bool appIsDown = appState.HasFlag(CoreVirtualKeyStates.Down);
            bool appIsLocked = appState.HasFlag(CoreVirtualKeyStates.Locked);

            Telemetry.Transmit("ctrlIsDown=", ctrlIsDown, "ctrlIsLocked=", ctrlIsLocked);
            Telemetry.Transmit("shiftIsDown=", shiftIsDown, "shiftIsLocked=", shiftIsLocked);
            Telemetry.Transmit("insIsDown=", insIsDown, "insIsLocked=", insIsLocked);
            Telemetry.Transmit("appIsDown=", appIsDown, "appIsLocked=", appIsLocked);


            var insertState = InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Insert);

            Debug.WriteLine($"{e.Key}");
            if (e.Key == VirtualKey.CapitalLock)
            {
                //CAPS.Text = "CAPS";
                CAPS.Foreground = Console.CapsLock ? Black : LightGrey;
            }
            if (e.Key == VirtualKey.NumberKeyLock)
            {
                //NUM.Text = "NUM";
                NUM.Foreground = Console.NumberLock ? Black : LightGrey;
            }
            //if (insertState.HasFlag(CoreVirtualKeyStates.Locked))
            //{
            //    //INS.Text = "INS";
            //    INS.Foreground = LightGrey;
            //}
            //else
            //{
            //    //INS.Text = "INS";
            //    INS.Foreground = Black;
            //}

            if (e.Key == VirtualKey.Scroll)
            {
            }
            if (!e.KeyStatus.IsMenuKeyDown && !e.KeyStatus.IsExtendedKey && e.Key != VirtualKey.Control)
            {
                ((CustomRichEditBox)me).IsDirty = true;
            }
        }
    }
}
