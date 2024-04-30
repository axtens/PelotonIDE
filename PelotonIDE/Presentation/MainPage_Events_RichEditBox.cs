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
            var me = (RichEditBox)sender;
            SolidColorBrush Black = new(Colors.Black);
            SolidColorBrush LightGrey = new(Colors.LightGray);


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
