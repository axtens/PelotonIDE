using DocumentFormat.OpenXml.Office2019.Presentation;

using Microsoft.UI.Input;
using Microsoft.UI.Xaml.Input;
using Microsoft.VisualBasic;

using Newtonsoft.Json;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.System;
using Windows.UI.Core;

using TabSettingJson = System.Collections.Generic.Dictionary<string, System.Collections.Generic.Dictionary<string, object>>;

namespace PelotonIDE.Presentation
{
    public partial class CustomTabItem : NavigationViewItem
    {
        public bool IsNewFile { get; set; }

        //public StorageFile? SavedFilePath { get; set; }
        public string? SavedFilePath { get; set; }
        public string? SavedFileName { get; set; }
        public string? SavedFileFolder { get; set; }
        public string? SavedFileExtension { get; set; }


        public TabSettingJson? TabSettingsDict { get; set; }

        public CustomTabItem()
        {
            SavedFilePath = null;
            SavedFileFolder = null;
            SavedFileName = null;
            SavedFileExtension = null;
        }

        protected override void OnKeyDown(KeyRoutedEventArgs e)
        {
            Telemetry.Disable();
            //Telemetry.Transmit("CustomTabItem", e.DeviceId, e.Handled, e.Key, e.KeyStatus, e.OriginalKey, e.OriginalSource);
            //CoreVirtualKeyStates appState = InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Application);
            //CoreVirtualKeyStates insState = InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Insert);
            //CoreVirtualKeyStates ctrlState = InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Control);
            //CoreVirtualKeyStates shiftState = InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Shift);

            //bool CtrlIsDown = ctrlState.HasFlag(CoreVirtualKeyStates.Down);
            //bool CtrlIsLocked = ctrlState.HasFlag(CoreVirtualKeyStates.Locked);
            //bool ShiftIsDown = shiftState.HasFlag(CoreVirtualKeyStates.Down);
            //bool ShiftIsLocked = shiftState.HasFlag(CoreVirtualKeyStates.Locked);
            //bool InsIsDown = insState.HasFlag(CoreVirtualKeyStates.Down);
            //bool InsIsLocked = insState.HasFlag(CoreVirtualKeyStates.Locked);
            //bool AppIsDown = appState.HasFlag(CoreVirtualKeyStates.Down);
            //bool AppIsLocked = appState.HasFlag(CoreVirtualKeyStates.Locked);

            //if (e.Key == VirtualKey.Tab && CtrlIsDown && ShiftIsDown)
            //{
            //    Telemetry.Transmit("+^Tab");
            //    // SwitchToTab(-1);
            //    e.Handled = true;
            //    return;
            //}
            //if (e.Key == VirtualKey.Tab && CtrlIsDown)
            //{
            //    Telemetry.Transmit("^Tab");
            //    // SwitchToTab(+1);
            //    e.Handled = true;
            //    return;
            //}

            //if (e.Key == (VirtualKey.Tab|VirtualKey.Control))
            //{
            //    Debug.WriteLine(e.KeyStatus);
            //}
            base.OnKeyDown(e);
        }
    }
}
