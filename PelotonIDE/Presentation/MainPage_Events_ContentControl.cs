using Microsoft.UI;
using Microsoft.UI.Xaml.Input;

using TabSettingJson = System.Collections.Generic.Dictionary<string, System.Collections.Generic.Dictionary<string, object>>;


namespace PelotonIDE.Presentation
{
    public sealed partial class MainPage : Page
    {
        private void ContentControl_LanguageName_RightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            ContentControl me = (ContentControl)sender;

            object prevContent = me.Content;

            MenuFlyout mf = new();

            CustomTabItem navigationViewItem = (CustomTabItem)tabControl.SelectedItem;
            if (navigationViewItem == null) return;
            string? inFocusTabLanguageName = GetLanguageNameFromID((long)navigationViewItem.TabSettingsDict["pOps.Language"]["Value"]);


            string interfaceLanguageName = Type_1_GetVirtualRegistry<string>("ideOps.InterfaceLanguageName");
            Dictionary<string, string> globals = LanguageSettings[Type_1_GetVirtualRegistry<string>("ideOps.InterfaceLanguageName")]["GLOBAL"];
            int count = LanguageSettings.Keys.Count;
            for (int i = 0; i < count; i++)
            {
                IEnumerable<string> names = from lang in LanguageSettings.Keys
                                            where LanguageSettings.ContainsKey(lang) && LanguageSettings[lang]["GLOBAL"]["ID"] == i.ToString()
                                            let name = LanguageSettings[lang]["GLOBAL"]["Name"]
                                            select name;
                if (names.Any())
                {
                    MenuFlyoutItem menuFlyoutItem = new()
                    {
                        Text = globals[$"{100 + i + 1}"],
                        Name = names.First(),
                        Foreground = names.First() == inFocusTabLanguageName ? new SolidColorBrush(Colors.White) : new SolidColorBrush(Colors.Black),
                        Background = names.First() == inFocusTabLanguageName ? new SolidColorBrush(Colors.Black) : new SolidColorBrush(Colors.White),
                        Tag = new Dictionary<string, object>()
                        {
                            {"MenuFlyout",mf },
                            {"ContentControlPreviousContent",prevContent },
                            {"ContentControl" ,me}
                        }
                    };
                    menuFlyoutItem.Click += ContentControl_Click; // this has to reset the cell to its original value
                    mf.Items.Add(menuFlyoutItem);
                }
            }
            FrameworkElement? senderElement = sender as FrameworkElement;

            //the code can show the flyout in your mouse click 
            mf.ShowAt(sender as UIElement, e.GetPosition(sender as UIElement));

            //me.Content = mfsu;
        }
        private void ContentControl_Click(object sender, RoutedEventArgs e)
        {
            MenuFlyoutItem me = (MenuFlyoutItem)sender;
            string name = me.Name;
            sbLanguageName.Text = me.Text;
            // change the current tab to that lang but don't change the pertab settings
            Dictionary<string, string> globals = LanguageSettings[name]["GLOBAL"];
            string id = globals["ID"];

            Type_3_UpdateInFocusTabSettings<long>("pOps.Language", true, long.Parse(id));

            UpdateLanguageInContextualMenu(me, me.Text, name);
            if (me.Tag is Dictionary<string, object> parent)
            {
                if (parent.ContainsKey("ContentControl") && parent.ContainsKey("ContentControlPreviousContent"))
                    parent["ContentControl"] = parent["ContentControlPreviousContent"];
            }
            UpdateStatusBar();
        }
        private void UpdateLanguageInContextualMenu(MenuFlyoutItem me, string internationalizedName, string name)
        {
            Telemetry.EnableIfMethodNameInFactorySettingsTelemetry();
            if (me.Tag is Dictionary<string, object> parent)
            {
                IList<MenuFlyoutItemBase> subMenus = ((MenuFlyout)parent["MenuFlyout"]).Items; //  from menu in ((MenuFlyoutSubItem)me.Tag).Items select menu;
                Telemetry.Transmit("subMenus != null", subMenus != null);
                if (subMenus != null)
                {
                    foreach (MenuFlyoutItemBase item in subMenus)
                    {
                        Telemetry.Transmit("item.Name=", item.Name, "name=", name);
                        if (item.Name == name)
                        {
                            Telemetry.Transmit("foreground=white, background=black");
                            item.Foreground = new SolidColorBrush(Colors.White);
                            item.Background = new SolidColorBrush(Colors.Black);
                        }
                        else
                        {
                            Telemetry.Transmit("foreground=black, background=white");
                            item.Foreground = new SolidColorBrush(Colors.Black);
                            item.Background = new SolidColorBrush(Colors.White);
                        }
                    }
                }
            }
        }
        private void ContentControl_FixedVariable_RightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            Telemetry.EnableIfMethodNameInFactorySettingsTelemetry();

            ContentControl me = (ContentControl)sender;

            object prevContent = me.Content;

            MenuFlyout mf = new();

            string interfaceLanguageName = Type_1_GetVirtualRegistry<string>("ideOps.InterfaceLanguageName");

            if (!AnInFocusTabExists()) return;

            bool inFocusTabVariableLength = Type_3_GetInFocusTab<bool>("pOps.VariableLength");
            Telemetry.Transmit("inFocusTabTimeout=", inFocusTabVariableLength);

            Dictionary<string, string> globals = LanguageSettings[Type_1_GetVirtualRegistry<string>("ideOps.InterfaceLanguageName")]["GLOBAL"];

            foreach (string key in new string[] { "variableLength", "fixedLength" })
            {
                MenuFlyoutItem menuFlyoutItem = new()
                {
                    Name = key,
                    Text = globals[key],
                    Foreground = inFocusTabVariableLength ? new SolidColorBrush(Colors.White) : new SolidColorBrush(Colors.Black),
                    Background = inFocusTabVariableLength ? new SolidColorBrush(Colors.Black) : new SolidColorBrush(Colors.White),
                    Tag = new Dictionary<string, object>()
                        {
                            { "Globals", globals },
                            { "CurrentValue", inFocusTabVariableLength }
                        }
                };
                menuFlyoutItem.Click += ContentControl_FixedVariable_MenuFlyoutItem_Click; // this has to reset the cell to its original value
                Telemetry.Transmit(menuFlyoutItem.Text, menuFlyoutItem.Name, menuFlyoutItem.Foreground.ToString(), menuFlyoutItem.Background.ToString());
                mf.Items.Add(menuFlyoutItem);
                inFocusTabVariableLength = !inFocusTabVariableLength;
            }


            FrameworkElement? senderElement = sender as FrameworkElement;

            //the code can show the flyout in your mouse click 
            mf.ShowAt(sender as UIElement, e.GetPosition(sender as UIElement));

        }
        private void ContentControl_FixedVariable_MenuFlyoutItem_Click(object sender, RoutedEventArgs e)
        {
            Telemetry.EnableIfMethodNameInFactorySettingsTelemetry();

            MenuFlyoutItem me = (MenuFlyoutItem)sender;

            Dictionary<string, object> dict = (Dictionary<string, object>)me.Tag;

            Dictionary<string, string> globals = (Dictionary<string, string>)dict["Globals"];

            bool isVariableLength = me.Name == "variableLength";
            if (AnInFocusTabExists())
                Type_3_UpdateInFocusTabSettings<bool>("pOps.VariableLength", isVariableLength, isVariableLength);

            sbFixedVariable.Text = (isVariableLength ? "#" : "@") + (string)globals[isVariableLength ? "variableLength" : "fixedLength"];
            // UpdateCommandLineInStatusBar();
            UpdateStatusBar();
        }
        private void ContentControl_Quietude_RightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            Telemetry.EnableIfMethodNameInFactorySettingsTelemetry();

            ContentControl me = (ContentControl)sender;

            object prevContent = me.Content;

            MenuFlyout mf = new();

            string interfaceLanguageName = Type_1_GetVirtualRegistry<string>("ideOps.InterfaceLanguageName");

            if (!AnInFocusTabExists()) return;

            long inFocusTabQuietude = Type_3_GetInFocusTab<long>("pOps.Quietude");
            Telemetry.Transmit("inFocusTabTimeout=", inFocusTabQuietude);

            Dictionary<string, string> frmMain = LanguageSettings[interfaceLanguageName]["frmMain"];

            int i = 0;
            foreach (string key in new string[] { "mnuQuiet", "mnuVerbose", "mnuVerbosePauseOnExit" })
            {
                MenuFlyoutItem menuFlyoutItem = new()
                {
                    Name = key,
                    Text = frmMain[key],
                    Foreground = inFocusTabQuietude == i ? new SolidColorBrush(Colors.White) : new SolidColorBrush(Colors.Black),
                    Background = inFocusTabQuietude == i ? new SolidColorBrush(Colors.Black) : new SolidColorBrush(Colors.White),
                    Tag = new Dictionary<string, object>()
                        {
                            { "Globals", frmMain },
                            { "CurrentValue", inFocusTabQuietude }
                        }
                };
                menuFlyoutItem.Click += ContentControl_Quietude_MenuFlyoutItem_Click; // this has to reset the cell to its original value
                Telemetry.Transmit(menuFlyoutItem.Text, menuFlyoutItem.Name, menuFlyoutItem.Foreground.ToString(), menuFlyoutItem.Background.ToString());
                mf.Items.Add(menuFlyoutItem);
                i++;
            }

            FrameworkElement? senderElement = sender as FrameworkElement;

            //the code can show the flyout in your mouse click 
            mf.ShowAt(sender as UIElement, e.GetPosition(sender as UIElement));


        }
        private void ContentControl_Quietude_MenuFlyoutItem_Click(object sender, RoutedEventArgs e)
        {
            Telemetry.EnableIfMethodNameInFactorySettingsTelemetry();

            string[] quietudes = ["mnuQuiet", "mnuVerbose", "mnuVerbosePauseOnExit"];
            MenuFlyoutItem me = (MenuFlyoutItem)sender;

            Dictionary<string, object> dict = (Dictionary<string, object>)me.Tag;

            Dictionary<string, string> globals = (Dictionary<string, string>)dict["Globals"];

            int quietude = quietudes.IndexOf(me.Name);

            if (AnInFocusTabExists())
                Type_3_UpdateInFocusTabSettings<long>("pOps.Quietude", true, quietude);

            sbQuietude.Text = (string)globals[quietudes.ElementAt(quietude)];
            
            UpdateStatusBar();
            
        }
        private void ContentControl_Timeout_RightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            Telemetry.EnableIfMethodNameInFactorySettingsTelemetry();

            ContentControl me = (ContentControl)sender;

            object prevContent = me.Content;

            MenuFlyout mf = new();

            string interfaceLanguageName = Type_1_GetVirtualRegistry<string>("ideOps.InterfaceLanguageName");

            if (!AnInFocusTabExists()) return;

            long inFocusTabTimeout = Type_3_GetInFocusTab<long>("ideOps.Timeout");
            Telemetry.Transmit("inFocusTabTimeout=", inFocusTabTimeout);

            Dictionary<string, string> frmMain = LanguageSettings[interfaceLanguageName]["frmMain"];

            int i = 0;
            foreach (string key in new string[] { "mnu20Seconds", "mnu100Seconds", "mnu200Seconds", "mnu1000Seconds", "mnuInfinite" })
            {
                MenuFlyoutItem menuFlyoutItem = new()
                {
                    Name = key,
                    Text = frmMain[key],
                    Foreground = inFocusTabTimeout == i ? new SolidColorBrush(Colors.White) : new SolidColorBrush(Colors.Black),
                    Background = inFocusTabTimeout == i ? new SolidColorBrush(Colors.Black) : new SolidColorBrush(Colors.White),
                    Tag = new Dictionary<string, object>()
                        {
                            { "Globals", frmMain },
                            { "CurrentValue", inFocusTabTimeout }
                        }
                };
                menuFlyoutItem.Click += ContentControl_Timeout_MenuFlyoutItem_Click; // this has to reset the cell to its original value
                Telemetry.Transmit(menuFlyoutItem.Text, menuFlyoutItem.Name, menuFlyoutItem.Foreground.ToString(), menuFlyoutItem.Background.ToString());
                mf.Items.Add(menuFlyoutItem);
                i++;
            }

            FrameworkElement? senderElement = sender as FrameworkElement;

            //the code can show the flyout in your mouse click 
            mf.ShowAt(sender as UIElement, e.GetPosition(sender as UIElement));
        }
        private void ContentControl_Timeout_MenuFlyoutItem_Click(object sender, RoutedEventArgs e)
        {
            Telemetry.EnableIfMethodNameInFactorySettingsTelemetry();

            string[] timeouts = ["mnu20Seconds", "mnu100Seconds", "mnu200Seconds", "mnu1000Seconds", "mnuInfinite"];
            MenuFlyoutItem me = (MenuFlyoutItem)sender;

            //Dictionary<string, object> dict = (Dictionary<string, object>)me.Tag;

            //Dictionary<string, string> globals = (Dictionary<string, string>)dict["Globals"];

            var timeout = timeouts.IndexOf(me.Name);

            if (AnInFocusTabExists())
            {
                Type_3_UpdateInFocusTabSettings<long>("ideOps.Timeout", true, timeout);
            }

            UpdateStatusBar();
        }
        private void ContentControl_Interpreter_RightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            Telemetry.EnableIfMethodNameInFactorySettingsTelemetry();

            var white = new SolidColorBrush(Colors.White);
            var black = new SolidColorBrush(Colors.Black);

            ContentControl me = (ContentControl)sender;

            object prevContent = me.Content;

            MenuFlyout mf = new();

            string interfaceLanguageName = Type_1_GetVirtualRegistry<string>("ideOps.InterfaceLanguageName");

            long inFocusInterpreter = AnInFocusTabExists() ? Type_3_GetInFocusTab<long>("ideOps.Engine") : Type_1_GetVirtualRegistry<long>("ideOps.Engine");

            Telemetry.Transmit("inFocusInterpreter=", inFocusInterpreter);

            foreach (long key in new long[] { 2, 3 })
            {
                MenuFlyoutItem menuFlyoutItem = new()
                {
                    Name = $"P{key}",
                    Text = $"P{key}",
                    Foreground = inFocusInterpreter == key ? white : black,
                    Background = inFocusInterpreter == key ? black : white,
                    Tag = key
                };
                menuFlyoutItem.Click += ContentControl_Interpreter_MenuFlyoutItem_Click; // this has to reset the cell to its original value
                Telemetry.Transmit(menuFlyoutItem.Text, menuFlyoutItem.Name, menuFlyoutItem.Foreground.ToString(), menuFlyoutItem.Background.ToString());
                mf.Items.Add(menuFlyoutItem);
            }

            mf.ShowAt(sender as UIElement, e.GetPosition(sender as UIElement));

        }
        private void ContentControl_Interpreter_MenuFlyoutItem_Click(object sender, RoutedEventArgs e)
        {
            Telemetry.EnableIfMethodNameInFactorySettingsTelemetry();

            MenuFlyoutItem me = (MenuFlyoutItem)sender;

            if (AnInFocusTabExists())
            {
                Type_3_UpdateInFocusTabSettings<long>("ideOps.Engine", true, (long)me.Tag);
            }
            UpdateStatusBar();
        }
        private void ContentControl_Rendering_RightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            Telemetry.EnableIfMethodNameInFactorySettingsTelemetry();

            if (!AnInFocusTabExists()) return;

            string interfaceLanguageName = Type_1_GetVirtualRegistry<string>("ideOps.InterfaceLanguageName");
            Dictionary<string, string> frmMain = LanguageSettings[interfaceLanguageName]["frmMain"];

            MenuFlyout mf = new();

            SolidColorBrush white = new(Colors.White);
            SolidColorBrush black = new(Colors.Black);
            SolidColorBrush darkGrey = new(Colors.DarkGray);

            //ContentControl me = (ContentControl)sender;

            //object prevContent = me.Content;

            string? inFocusTabRenderers = Type_3_GetInFocusTab<string>("outputOps.ActiveRenderers");

            foreach (TabViewItem tvi in outputPanelTabView.TabItems.Cast<TabViewItem>())
            {
                long renderNumber = long.Parse((string)tvi.Tag);
                MenuFlyoutItem menuFlyoutItem = new()
                {
                    Name = tvi.Name,
                    Text = frmMain[$"{tvi.Name}"],
                    Foreground = inFocusTabRenderers.Contains(renderNumber.ToString()) ? white : black,
                    Background = inFocusTabRenderers.Contains(renderNumber.ToString()) ? black : white,
                    Tag = tvi.Name.Replace("tab", ""),
                };
                menuFlyoutItem.Click += ContentControl_Rendering_MenuFlyoutItem_Click; // this has to reset the cell to its original value
                Telemetry.Transmit(menuFlyoutItem.Text, menuFlyoutItem.Name);
                mf.Items.Add(menuFlyoutItem);
            }

            UpdateOutputTabs();

            mf.ShowAt(sender as UIElement, e.GetPosition(sender as UIElement));
        }
        private void ContentControl_Rendering_MenuFlyoutItem_Click(object sender, RoutedEventArgs e)
        {
            Telemetry.EnableIfMethodNameInFactorySettingsTelemetry();

            MenuFlyoutItem me = (MenuFlyoutItem)sender;
            //string meName = me.Name.Replace("tab", "");
            string key = (string)me.Tag;

            string render = ((long)RenderingConstants["outputOps.ActiveRenderers"][key.ToUpper()]).ToString();
            long tapped = Type_3_GetInFocusTab<long>("outputOps.TappedRenderer");

            if (AnInFocusTabExists())
            {
                List<string> keys = [.. Type_3_GetInFocusTab<string>("outputOps.ActiveRenderers").Split(',', StringSplitOptions.RemoveEmptyEntries)];
                if (keys.Contains(render))
                {
                    keys.Remove(render);
                }
                else
                {
                    keys.Add(render);
                }
                Type_3_UpdateInFocusTabSettings<string>("outputOps.ActiveRenderers", true, string.Join(",", keys));

                if (Type_3_GetInFocusTab<string>("outputOps.ActiveRenderers").Trim().Length == 0)
                {
                    Type_3_UpdateInFocusTabSettings<long>("outputOps.TappedRenderer", true, -1);
                }

                DeselectAndDisableAllOutputPanelTabs();
                EnableAllOutputPanelTabsMatchingRendering();

                List<long> lkeys = Type_3_GetInFocusTab<string>("outputOps.ActiveRenderers").Split(',', StringSplitOptions.RemoveEmptyEntries).Select(n => long.Parse(n)).ToList();
                if (!lkeys.Contains(tapped))
                {
                    Type_3_UpdateInFocusTabSettings<long>("outputOps.TappedRenderer", true, -1);
                }

                UpdateStatusBar();
                UpdateOutputTabs();
            }
        }

    }
}
