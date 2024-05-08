using Microsoft.UI.Xaml.Input;

using RenderingConstantsStructure = System.Collections.Generic.Dictionary<string,
        System.Collections.Generic.Dictionary<string, object>>;
using TabSettingJson = System.Collections.Generic.Dictionary<string, System.Collections.Generic.Dictionary<string, object>>;

namespace PelotonIDE.Presentation
{
    public sealed partial class MainPage : Page
    {
        private void TabControl_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {
            Telemetry.EnableIfMethodNameInFactorySettingsTelemetry();
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
                        if (sbLanguageName.Text != currentLanguageName)
                        {
                            sbLanguageName.Text = currentLanguageName;
                        }
                        UpdateStatusBar();

                        //UpdateCommandLineInStatusBar();
                        //UpdateStatusBarFromInFocusTab();
                        //UpdateInterpreterInStatusBar();
                        //UpdateTopMostRendererInCurrentTab();
                    }
                }
            }
            //UpdateOutputTabs();
            UpdateOutputTabs();
        }
        //private void TabControl_KeyDown(object sender, KeyRoutedEventArgs e)
        //{
        //    Telemetry.EnableIfMethodNameInFactorySettingsTelemetry();
        //    CustomTabItem me = (CustomTabItem)sender;
        //    Telemetry.Transmit(me.Name, e.GetType().FullName);
        //}
        private void CustomTabItem_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            Telemetry.EnableIfMethodNameInFactorySettingsTelemetry();
            CustomTabItem me = (CustomTabItem)sender;
            Telemetry.Transmit(me.Name, e.GetType().FullName);

        }
        private async void TabControl_RightTapped(object sender, RightTappedRoutedEventArgs e) // fires first for all tabs other than tab1
        {
            Telemetry.EnableIfMethodNameInFactorySettingsTelemetry();
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

            UpdateStatusBar();
        }
        private void CustomTabItem_RightTapped(object sender, RightTappedRoutedEventArgs e) // fires on tab1 then fires TabControl_RightTapped
        {
            Telemetry.EnableIfMethodNameInFactorySettingsTelemetry();
            Telemetry.Transmit(((CustomTabItem)sender).Name, e.GetType().FullName);

        }
    }
}
