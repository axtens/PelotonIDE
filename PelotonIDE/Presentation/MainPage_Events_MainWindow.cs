using Microsoft.UI;
using Microsoft.UI.Text;

using Newtonsoft.Json;

using System.Diagnostics;

using Windows.Storage;

using RenderingConstantsStructure = System.Collections.Generic.Dictionary<string,
        System.Collections.Generic.Dictionary<string, object>>;
using TabSettingJson = System.Collections.Generic.Dictionary<string, System.Collections.Generic.Dictionary<string, object>>;

namespace PelotonIDE.Presentation
{
    public sealed partial class MainPage : Page
    {
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            if (e.Parameter == null)
            {
                return;
            }

            NavigationData parameters = (NavigationData)e.Parameter;

            switch (parameters.Source)
            {
                case "IDEConfig":
                    Type_1_UpdateVirtualRegistry("ideOps.Engine.2", parameters.KVPs["ideOps.Engine.2"].ToString());
                    Type_1_UpdateVirtualRegistry("ideOps.Engine.3", parameters.KVPs["ideOps.Engine.3"].ToString());
                    Type_1_UpdateVirtualRegistry("ideOps.ScriptsFolder", parameters.KVPs["ideOps.ScriptsFolder"].ToString());
                    break;
                case "TranslatePage":

                    CustomRichEditBox richEditBox = new()
                    {
                        IsDirty = true,
                        IsRTF = true,
                    };
                    richEditBox.KeyDown += RichEditBox_KeyDown;
                    richEditBox.AcceptsReturn = true;
                    richEditBox.Document.SetText(TextSetOptions.UnicodeBidi, parameters.KVPs["TargetText"].ToString());

                    string? langname = LocalSettings.Values["ideOps.InterfaceLanguageName"].ToString();
                    //long quietude = (long)parameters.KVPs["pOps.Quietude"];

                    CustomTabItem TargetInFocusTab = new()
                    {
                        Content = LanguageSettings[langname!]["GLOBAL"]["Document"] + " " + TabControlCounter, // (tabControl.MenuItems.Count + 1),
                        Tag = "Tab" + TabControlCounter, // (tabControl.MenuItems.Count + 1),
                        IsNewFile = true,
                        TabSettingsDict = ShallowCopyPerTabSetting(PerTabInterpreterParameters),
                        Height = 30,
                    };

                    TabControlCounter += 1;

                    richEditBox.Tag = TargetInFocusTab.Tag;

                    _richEditBoxes[richEditBox.Tag] = richEditBox;
                    tabControl.MenuItems.Add(TargetInFocusTab);
                    tabControl.SelectedItem = TargetInFocusTab;

                    SourceInFocusTabSettings = (TabSettingJson?)parameters.KVPs["SourceInFocusTabSettings"];
                    
                    TransferOriginalInFocusTabSettingsToInFocusTab(SourceInFocusTabSettings, TargetInFocusTab.TabSettingsDict);

                    Type_3_UpdateInFocusTabSettings("pOps.Language", true, (long)parameters.KVPs["TargetLanguageID"]);
                    if (parameters.KVPs.TryGetValue("TargetVariableLength", out object? value))
                    {
                        Type_3_UpdateInFocusTabSettings("pOps.VariableLength", (bool)value, (bool)value);
                    }

                    if (parameters.KVPs.TryGetValue("TargetPadOutCode", out object? poc))
                    {
                        Type_3_UpdateInFocusTabSettings("pOps.Padding", (bool)poc, (bool)poc);
                    }


                    richEditBox.Focus(FocusState.Keyboard);
                    //languageName.Text = null;
                    //languageName.Text = GetLanguageNameOfCurrentTab(TargetInFocusTab.TabSettingsDict);
                    //tabCommandLine.Text = BuildTabCommandLine();
                    if (AnInFocusTabExists())
                    {
                        TabSettingJson? tabset = InFocusTab().TabSettingsDict;
                        if (tabset != null)
                        {
                            UpdateStatusBar(tabset);
                        }
                    }
                    AfterTranslation = true;

                    break;
            }
        }
        private void TransferOriginalInFocusTabSettingsToInFocusTab(TabSettingJson? sourceInFocusTabSettings, TabSettingJson? targetTabSettings)
        {
            foreach (var key in sourceInFocusTabSettings.Keys)
            {
                var cluster = sourceInFocusTabSettings[key];
                if (cluster != null)
                {
                    if (key.StartsWith("pOps.")|| key.StartsWith("ideOps.")|| key.StartsWith("outputOps."))
                    {
                        if ((bool)cluster["Defined"])
                        {
                            targetTabSettings[key]["Value"] = cluster["Value"];
                        }
                    }
                }
            }
        }
        /// <summary>
        /// Load previous editor settings
        /// </summary>
        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            Telemetry.SetEnabled(false);
           
            LanguageSettings ??= await GetLanguageConfiguration();
            RenderingConstants ??= await GetRenderingConstants(); /*
                new Dictionary<string, Dictionary<string, object>>()
                    {
                        { "outputOps.ActiveRenderers", new Dictionary<string, object>()
                        {
                            { "Output", 3L },
                            { "Error", 0L },
                            { "RTF", 31L },
                            { "Html", 21L },
                            { "Logo", 42L }
                        }
                    }
                };*/
            
            if (LangLangs.Count == 0)
                LangLangs = GetLangLangs(LanguageSettings);

            SetKeyboardFlags();

            FactorySettings ??= await GetFactorySettings();

            // #MainPage-LoadingVirtReg
            IfNotInVirtualRegistryUpdateItFromFactorySettingsOrDefaultTo<string>("outputOps.AvailableRenderers", FactorySettings, "3,0,21,42,31");
            IfNotInVirtualRegistryUpdateItFromFactorySettingsOrDefaultTo<string>("outputOps.ActiveRenderers", FactorySettings, "3,0");
            IfNotInVirtualRegistryUpdateItFromFactorySettingsOrDefaultTo<long>("outputOps.TappedRenderer", FactorySettings,-1);

            IfNotInVirtualRegistryUpdateItFromFactorySettingsOrDefaultTo<long>("pOps.Transput", FactorySettings, 3);

            IfNotInVirtualRegistryUpdateItFromFactorySettingsOrDefaultTo<long>("ideOps.Timeout", FactorySettings, 1);
            UpdateTimeoutInMenu();
            UpdateRenderingInMenu();

            UpdateTransputInMenu();

            IfNotInVirtualRegistryUpdateItFromFactorySettingsOrDefaultTo<string>("ideOps.OutputPanelSettings", FactorySettings, "True|Bottom|200|400");

            var position = FromBarredString_String(Type_1_GetVirtualRegistry<string>("ideOps.OutputPanelSettings"), 1);
            Type_1_UpdateVirtualRegistry<string>("ideOps.OutputPanelPosition", position);

            Type_1_UpdateVirtualRegistry<bool>("ideOps.OutputPanelShowing", FromBarredString_Boolean(Type_1_GetVirtualRegistry<string>("ideOps.OutputPanelSettings"), 0));
            Type_1_UpdateVirtualRegistry<double>("ideOps.OutputPanelHeight", (double)FromBarredString_Double(Type_1_GetVirtualRegistry<string>("ideOps.OutputPanelSettings"), 2));
            Type_1_UpdateVirtualRegistry<double>("ideOps.OutputPanelWidth", (double)FromBarredString_Double(Type_1_GetVirtualRegistry<string>("ideOps.OutputPanelSettings"), 3));
            
            HandleOutputPanelChange(position);

            IfNotInVirtualRegistryUpdateItFromFactorySettingsOrDefaultTo<string>("ideOps.InterfaceLanguageName", FactorySettings, "English");
            IfNotInVirtualRegistryUpdateItFromFactorySettingsOrDefaultTo<long>("ideOps.InterfaceLanguageID", FactorySettings, 0);
            IfNotInVirtualRegistryUpdateItFromFactorySettingsOrDefaultTo<string>("mainOps.InterpreterLanguageName", FactorySettings, "English");
            IfNotInVirtualRegistryUpdateItFromFactorySettingsOrDefaultTo<long>("mainOps.InterpreterLanguageID", FactorySettings, 0);

            if (Type_1_GetVirtualRegistry<string>("ideOps.InterfaceLanguageName") != null)
            {
                HandleInterfaceLanguageChange(Type_1_GetVirtualRegistry<string>("ideOps.InterfaceLanguageName"));
            }

            // Engine selection:
            //  Engine will contain either 2 or 3
            
            SetEngine();
            SetScripts();
            SetInterpreterNew();
            SetInterpreterOld();

            PerTabInterpreterParameters ??= await MainPage.GetPerTabInterpreterParameters();

            if (!AfterTranslation)
            {

                IfNotInVirtualRegistryUpdateItFromFactorySettingsOrDefaultTo<bool>("pOps.VariableLength", FactorySettings, false);
                IfNotInVirtualRegistryUpdateItFromFactorySettingsOrDefaultTo<long>("pOps.Quietude", FactorySettings, 2);

                Type_2_UpdatePerTabSettings("pOps.Language", true, Type_1_GetVirtualRegistry<long>("mainOps.InterpreterLanguageID"));
                Type_2_UpdatePerTabSettings("pOps.VariableLength", Type_1_GetVirtualRegistry<bool>("pOps.VariableLength"), Type_1_GetVirtualRegistry<bool>("pOps.VariableLength"));
                Type_2_UpdatePerTabSettings("pOps.Quietude", true, Type_1_GetVirtualRegistry<long>("pOps.Quietude"));
                Type_2_UpdatePerTabSettings("ideOps.Timeout", true, Type_1_GetVirtualRegistry<long>("ideOps.Timeout"));
                Type_2_UpdatePerTabSettings("outputOps.ActiveRenderers", true, Type_1_GetVirtualRegistry<string>("outputOps.ActiveRenderers"));
            }

            CustomTabItem navigationViewItem = (CustomTabItem)tabControl.SelectedItem;
            navigationViewItem.TabSettingsDict ??= ShallowCopyPerTabSetting(PerTabInterpreterParameters);

            UpdateTabDocumentNameIfOnlyOneAndFirst(tabControl, Type_1_GetVirtualRegistry<string>("ideOps.InterfaceLanguageName"));

            if (!AfterTranslation)
            {
                // So what to we do 
                //Type_3_UpdateInFocusTabSettings("pOps.Language", true, Type_1_GetVirtualRegistry<long>("mainOps.InterpreterLanguageID"));
                // Do we also set the VariableLength of the inFocusTab?
                //bool VariableLength = GetFactorySettingsWithLocalSettingsOverrideOrDefault<bool>("pOps.VariableLength", FactorySettings, LocalSettings, false);
                IfNotInVirtualRegistryUpdateItFromFactorySettingsOrDefaultTo("pOps.VariableLength", FactorySettings, false);
                Type_3_UpdateInFocusTabSettings("pOps.VariableLength", Type_1_GetVirtualRegistry<bool>("pOps.VariableLength"), Type_1_GetVirtualRegistry<bool>("pOps.VariableLength"));
                //UpdateStatusBarFromInFocusTab();
                if (AnInFocusTabExists())
                {
                    RenderingConstantsStructure? tabset = InFocusTab().TabSettingsDict;
                    if (tabset != null)
                    {
                        UpdateStatusBar(tabset);
                    }
                }
                DeserializeTabsFromVirtualRegistry();
            }
            InterfaceLanguageSelectionBuilder(mnuSelectLanguage, Internationalization_Click);
            InterpreterLanguageSelectionBuilder(mnuRun, "mnuLanguage", MnuLanguage_Click);
            UpdateEngineSelectionFromFactorySettingsInMenu();

            if (!AfterTranslation)
            {
                UpdateMenuRunningModeInMenu(PerTabInterpreterParameters["pOps.Quietude"]);
            }
            AfterTranslation = false;

            SetVariableLengthModeInMenu(mnuVariableLength, Type_1_GetVirtualRegistry<bool>("pOps.VariableLength"));

            UpdateLanguageNameInStatusBar(navigationViewItem.TabSettingsDict);

            UpdateStatusBarFromVirtualRegistry();
            //UpdateCommandLineInStatusBar();
            if (AnInFocusTabExists())
            {
                RenderingConstantsStructure? tabset = InFocusTab().TabSettingsDict;
                if (tabset != null)
                {
                    UpdateStatusBar(tabset);
                }
            }
            spOutput.Visibility = Visibility.Visible;

            // outputPanel.Width = relativePanel.ActualSize.X;            
            
            (tabControl.Content as CustomRichEditBox).Focus(FocusState.Keyboard);

            string currentLanguageName = GetLanguageNameOfCurrentTab(navigationViewItem.TabSettingsDict);
            if (languageName.Text != currentLanguageName)
            {
                languageName.Text = currentLanguageName;
            }

            await HtmlText.EnsureCoreWebView2Async();
            HtmlText.NavigateToString("<body style='background-color: papayawhip;'></body>");

            await LogoText.EnsureCoreWebView2Async();
            LogoText.NavigateToString("<body style='background-color: #ffdad5;'></body>");

            // UpdateTopMostRendererInCurrentTab();
            UpdateOutputTabs();
            return;

            void SetKeyboardFlags()
            {
                var lightGrey = new SolidColorBrush(Colors.LightGray);
                var black = new SolidColorBrush(Colors.Black);
                CAPS.Foreground = Console.CapsLock ? black : lightGrey;
                NUM.Foreground = Console.NumberLock ? black : lightGrey;
                //INS.Foreground = InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Insert).HasFlag(CoreVirtualKeyStates.Locked) ? black : lightGrey;
            }

            void SetEngine()
            {
                if (LocalSettings.Values.TryGetValue("ideOps.Engine", out object? value))
                {
                    Engine = (long)value;
                }
                else
                {
                    Engine = (long)FactorySettings["ideOps.Engine"];
                }
                Type_1_UpdateVirtualRegistry("ideOps.Engine", Engine);
            }
            void SetScripts()
            {
                if (LocalSettings.Values.TryGetValue("ideOps.ScriptsFolder", out object? value))
                {
                    Scripts = value.ToString();
                }
                else
                {
                    Scripts = FactorySettings["ideOps.ScriptsFolder"].ToString();
                }
                Scripts ??= @"C:\peloton\code";
                Type_1_UpdateVirtualRegistry("ideOps.ScriptsFolder", Scripts);
            }
            void SetInterpreterOld()
            {
                if (LocalSettings.Values.TryGetValue("ideOps.Engine.2", out object? value))
                {
                    InterpreterP2 = value.ToString();
                }
                else
                {
                    InterpreterP2 = FactorySettings["ideOps.Engine.2"].ToString();
                }
                InterpreterP2 ??= @"c:\protium\bin\pdb.exe";
                Type_1_UpdateVirtualRegistry("ideOps.Engine.2", InterpreterP2);
            }
            void SetInterpreterNew()
            {
                if (LocalSettings.Values.TryGetValue("ideOps.Engine.3", out object? value))
                {
                    InterpreterP3 = value.ToString();
                }
                else
                {
                    InterpreterP3 = FactorySettings["ideOps.Engine.3"].ToString();
                }
                InterpreterP3 ??= @"c:\peloton\bin\p3.exe";
                Type_1_UpdateVirtualRegistry("ideOps.Engine.3", InterpreterP3);
            }
        }
        private void UpdateTransputInMenu()
        {
            Telemetry.SetEnabled(false);

            string transput = Type_1_GetVirtualRegistry<long>("pOps.Transput").ToString();
            foreach (var mfi in from MenuFlyoutSubItem mfsi in mnuTransput.Items.Cast<MenuFlyoutSubItem>()
                                where mfsi != null
                                where mfsi.Items.Count > 0
                                from MenuFlyoutItem mfi in mfsi.Items.Cast<MenuFlyoutItem>()
                                select mfi)
            {
                MenuItemHighlightController((MenuFlyoutItem)mfi, false);
                if (transput == (string)mfi.Tag)
                {
                    MenuItemHighlightController((MenuFlyoutItem)mfi, true);
                }
            }
        }
        private void UpdateRenderingInMenu()
        {
            List<string> renderers = Type_1_GetVirtualRegistry<string>("outputOps.ActiveRenderers").Split(',').Select(x => x.Trim()).ToList();

            mnuRendering.Items.ForEach(item =>
            {
                MenuItemHighlightController((MenuFlyoutItem)item, false);
                if (renderers.Contains((string)item.Tag))
                {
                    MenuItemHighlightController((MenuFlyoutItem)item, true);
                }

            });
        }
        private Dictionary<string, List<string>> GetLangLangs(Dictionary<string, Dictionary<string, Dictionary<string, string>>>? languageSettings)
        {
            Dictionary<string, List<string>> dict = [];
            List<string> kees = [.. languageSettings.Keys];
            kees.Sort(CompareLanguagesById);

            foreach (string key in kees)
            {
                long myid = long.Parse(LanguageSettings[key]["GLOBAL"]["ID"]);
                string myLanguageInMyLanguage = LanguageSettings[key]["GLOBAL"]["153"];
                List<string> strings = [];

                foreach (string kee in kees)
                {
                    long theirId = long.Parse(LanguageSettings[kee]["GLOBAL"]["ID"]);
                    string theirLanguageInTheirLanguage = LanguageSettings[kee]["GLOBAL"]["153"];
                    string theirLanguageInMyLanguage = LanguageSettings[key]["GLOBAL"][$"{125 + theirId}"];
                    strings.Add(myLanguageInMyLanguage == theirLanguageInTheirLanguage ? myLanguageInMyLanguage : $"{theirLanguageInMyLanguage} - {theirLanguageInTheirLanguage}");
                }
                dict[key] = strings;
            }
            return dict;

            int CompareLanguagesById(string x, string y)
            {
                long xid = long.Parse(languageSettings[x]["GLOBAL"]["ID"]);
                long yid = long.Parse(languageSettings[y]["GLOBAL"]["ID"]);
                if (xid < yid) { return -1; }
                if (xid > yid) { return 1; }
                return 0;
            }
        }
        private void UpdateStatusBarFromVirtualRegistry()
        {
            string interfaceLanguageName = Type_1_GetVirtualRegistry<string>("ideOps.InterfaceLanguageName");

            bool isVariableLength = Type_1_GetVirtualRegistry<bool>("pOps.VariableLength");
            fixedVariableStatus.Text = (isVariableLength ? "#" : "@") + LanguageSettings[interfaceLanguageName]["GLOBAL"][isVariableLength ? "variableLength" : "fixedLength"];

            string[] quietudes = ["mnuQuiet", "mnuVerbose", "mnuVerbosePauseOnExit"];
            long quietude = Type_1_GetVirtualRegistry<long>("pOps.Quietude");
            quietudeStatus.Text = LanguageSettings[interfaceLanguageName]["frmMain"][quietudes.ElementAt((int)quietude)];

            string[] timeouts = ["mnu20Seconds", "mnu100Seconds", "mnu200Seconds", "mnu1000Seconds", "mnuInfinite"];
            long timeout = Type_1_GetVirtualRegistry<long>("ideOps.Timeout");
            timeoutStatus.Text = $"{LanguageSettings[interfaceLanguageName]["frmMain"]["mnuTimeout"]}: {LanguageSettings[interfaceLanguageName]["frmMain"][timeouts.ElementAt((int)timeout)]}";
        }
        private void UpdateStatusBarFromInFocusTab()
        {
            string interfaceLanguageName = Type_1_GetVirtualRegistry<string>("ideOps.InterfaceLanguageName");

            bool isVariableLength = Type_3_GetInFocusTab<bool>("pOps.VariableLength");
            fixedVariableStatus.Text = (isVariableLength ? "#" : "@") + LanguageSettings[interfaceLanguageName]["GLOBAL"][isVariableLength ? "variableLength" : "fixedLength"];

            string[] quietudes = ["mnuQuiet", "mnuVerbose", "mnuVerbosePauseOnExit"];
            long quietude = Type_3_GetInFocusTab<long>("pOps.Quietude");
            quietudeStatus.Text = LanguageSettings[interfaceLanguageName]["frmMain"][quietudes.ElementAt((int)quietude)];

            string[] timeouts = ["mnu20Seconds", "mnu100Seconds", "mnu200Seconds", "mnu1000Seconds", "mnuInfinite"];
            long timeout = Type_3_GetInFocusTab<long>("ideOps.Timeout");
            timeoutStatus.Text = $"{LanguageSettings[interfaceLanguageName]["frmMain"]["mnuTimeout"]}: {LanguageSettings[interfaceLanguageName]["frmMain"][timeouts.ElementAt((int)timeout)]}";
        }
        private void UpdateTabDocumentNameIfOnlyOneAndFirst(NavigationView tabControl, string? interfaceLanguageName)
        {
            if (tabControl.MenuItems.Count == 1 && interfaceLanguageName != null && interfaceLanguageName != "English")
            {
                string? content = (string?)((CustomTabItem)tabControl.SelectedItem).Content;
                content = content.Replace(LanguageSettings["English"]["GLOBAL"]["Document"], LanguageSettings[interfaceLanguageName]["GLOBAL"]["Document"]);
                ((CustomTabItem)tabControl.SelectedItem).Content = content;
            }
        }
        private void UpdateEngineSelectionFromFactorySettingsInMenu()
        {
            if (LocalSettings.Values["ideOps.Engine"].ToString() == "ideOps.Engine.2")
            {
                MenuItemHighlightController(mnuNewEngine, false);
                MenuItemHighlightController(mnuOldEngine, true);
                interpreter.Text = "P2";
            }
            else
            {
                MenuItemHighlightController(mnuNewEngine, true);
                MenuItemHighlightController(mnuOldEngine, false);
                interpreter.Text = "P3";
            }
        }
        /// <summary>
        /// Save current editor settings
        /// </summary>
        private void MainWindow_Closed(object sender, object e)
        {
            if (_richEditBoxes.Count > 0)
            {
                foreach (KeyValuePair<object, CustomRichEditBox> _reb in _richEditBoxes)
                {
                    if (_reb.Value.IsDirty)
                    {
                        object key = _reb.Key;
                        CustomRichEditBox aRichEditBox = _richEditBoxes[key];
                        foreach (object? item in tabControl.MenuItems)
                        {
                            CustomTabItem? cti = item as CustomTabItem;
                            string content = cti.Content.ToString().Replace(" ", "");
                            if (content == key as string)
                            {
                                Debug.WriteLine(cti.Content);
                                cti.Focus(FocusState.Keyboard); // was Pointer
                            }
                        }
                    }
                }
            }

            SerializeTabsToVirtualRegistry();
            SerializeLayoutToVirtualRegistry();
        }
        private void SerializeLayoutToVirtualRegistry()
        {
            Telemetry.SetEnabled(false);
            List<string> list =
            [
                Type_1_GetVirtualRegistry<bool>("ideOps.OutputPanelShowing") ? "True" : "False",
                Type_1_GetVirtualRegistry<string>("ideOps.OutputPanelPosition"),
                Type_1_GetVirtualRegistry<double>("ideOps.OutputPanelHeight").ToString(),
                Type_1_GetVirtualRegistry<double>("ideOps.OutputPanelWidth").ToString(),
            ];
            Type_1_UpdateVirtualRegistry<string>("ideOps.OutputPanelSettings", list.JoinBy("|"));
        }
    }
}
