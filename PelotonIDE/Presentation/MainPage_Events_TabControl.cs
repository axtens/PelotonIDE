using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

using Newtonsoft.Json;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PelotonIDE.Presentation
{
    public sealed partial class MainPage : Page
    {
        private void TabControl_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {
            Telemetry.SetEnabled(false);
            var me = (NavigationView)sender;
            if (args.SelectedItem != null)
            {
                Telemetry.Transmit(args.SelectedItem.ToString());

                if (tabControl.SelectedItem != null)
                {
                    CustomTabItem navigationViewItem = (CustomTabItem)tabControl.SelectedItem;
                    tabControl.Content = _richEditBoxes[navigationViewItem.Tag];
                    if (navigationViewItem.TabSettingsDict != null)
                    {
                        string currentLanguageName = GetLanguageNameOfCurrentTab(navigationViewItem.TabSettingsDict);
                        if (languageName.Text != currentLanguageName)
                        {
                            languageName.Text = currentLanguageName;
                        }
                        UpdateStatusBar(navigationViewItem.TabSettingsDict);

                        //UpdateCommandLineInStatusBar();
                        //UpdateStatusBarFromInFocusTab();
                        //UpdateInterpreterInStatusBar();
                        //UpdateTopMostRendererInCurrentTab();
                    }
                }
            }
            //AssertSelectedOutputTab();
            UpdateOutputTabs();
        }
        private void TabControl_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            Telemetry.SetEnabled(false);
            CustomTabItem me = (CustomTabItem)sender;
            Telemetry.Transmit(me.Name, e.GetType().FullName);
        }

        private void CustomTabItem_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            Telemetry.SetEnabled(false);
            CustomTabItem me = (CustomTabItem)sender;
            Telemetry.Transmit(me.Name, e.GetType().FullName);

        }

        private async void TabControl_RightTapped(object sender, RightTappedRoutedEventArgs e) // fires first for all tabs other than tab1
        {
            Telemetry.SetEnabled(false);
            CustomTabItem selectedItem = (CustomTabItem)((NavigationView)sender).SelectedItem;

            CustomRichEditBox currentRichEditBox = _richEditBoxes[selectedItem.Tag];
            // var t1 = tab1;
            if (currentRichEditBox.IsDirty)
            {
                if (!await AreYouSureToClose()) return;
            }
            _richEditBoxes.Remove(selectedItem.Tag);
            tabControl.MenuItems.Remove(selectedItem);
            if (tabControl.MenuItems.Count > 0)
            {
                tabControl.SelectedItem = tabControl.MenuItems[tabControl.MenuItems.Count - 1];
            }
            else
            {
                tabControl.Content = null;
                tabControl.SelectedItem = null;
            }
            UpdateCommandLineInStatusBar();
            UpdateStatusBarFromInFocusTab();
        }

        private void CustomTabItem_RightTapped(object sender, RightTappedRoutedEventArgs e) // fires on tab1 then fires TabControl_RightTapped
        {
            Telemetry.SetEnabled(false);
            Telemetry.Transmit(((CustomTabItem)sender).Name, e.GetType().FullName);

        }
    }
}
