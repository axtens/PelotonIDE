using DocumentFormat.OpenXml.Wordprocessing;

using Microsoft.UI;
using Microsoft.UI.Input;
using Microsoft.UI.Text;

using Newtonsoft.Json;

using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Timers;

using Windows.Storage;
using Windows.System;
using Windows.UI.Core;
using System.Linq;
using System.Linq.Expressions;
using Frame = Microsoft.UI.Xaml.Controls.Frame;
using DocumentFormat.OpenXml.InkML;
using static System.Runtime.InteropServices.JavaScript.JSType;
using System.Reflection;
using Uno.Toolkit.UI;
using System.Runtime.InteropServices;

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

            //var selectedLanguage = parameters.selectedLangauge;
            //var translatedREB = parameters.translatedREB;
            switch (parameters.Source)
            {
                case "IDEConfig":
                    //string? engine = LocalSettings.Values["Engine"].ToString();
                    Type_1_UpdateVirtualRegistry("Interpreter.P3", parameters.KVPs["Interpreter"].ToString());
                    Type_1_UpdateVirtualRegistry("Scripts", parameters.KVPs["Scripts"].ToString());
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

                    string? langname = LocalSettings.Values["InterfaceLanguageName"].ToString();
                    long quietude = (long)parameters.KVPs["Quietude"];
                    //Type_2_UpdatePerTabSettings("Quietude", true, virtRegQuietude);

                    CustomTabItem navigationViewItem = new()
                    {
                        Content = LanguageSettings[langname!]["GLOBAL"]["Document"] + " " + TabControlCounter, // (tabControl.MenuItems.Count + 1),
                        //Content = "Tab " + (tabControl.MenuItems.Count + 1),
                        Tag = "Tab" + TabControlCounter, // (tabControl.MenuItems.Count + 1),
                        IsNewFile = true,
                        TabSettingsDict = ClonePerTabSettings(PerTabInterpreterParameters),
                        Height = 30,
                    };

                    TabControlCounter += 1;

                    richEditBox.Tag = navigationViewItem.Tag;
                    //richEditBox.Language = LanguageSettings[langname!]["GLOBAL"]["Locale"];

                    _richEditBoxes[richEditBox.Tag] = richEditBox;
                    tabControl.MenuItems.Add(navigationViewItem);
                    tabControl.SelectedItem = navigationViewItem;

                    Type_3_UpdateInFocusTabSettings("Language", true, (long)parameters.KVPs["TargetLanguageID"]);
                    if (parameters.KVPs.TryGetValue("TargetVariableLength", out object? value))
                    {
                        Type_3_UpdateInFocusTabSettings("VariableLength", (bool)value, (bool)value);
                    }

                    richEditBox.Focus(FocusState.Keyboard);
                    languageName.Text = null;
                    languageName.Text = GetLanguageNameOfCurrentTab(navigationViewItem.TabSettingsDict);
                    tabCommandLine.Text = BuildTabCommandLine();

                    AfterTranslation = true;

                    break;
            }
        }
        /// <summary>
        /// Load previous editor settings
        /// </summary>
        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            Telemetry t = new();
            t.SetEnabled(true);

            LanguageSettings ??= await GetLanguageConfiguration();
            RenderingConstants ??= new Dictionary<string, Dictionary<string, object>>()
                    {
                        { "Rendering", new Dictionary<string, object>()
                        {
                            { "Output", 3L },
                            { "Error", 0L },
                            { "RTF", 31L },
                            { "Html", 21L },
                            { "Logo", 42L }
                        }
                    }
                };

            if (LangLangs.Count == 0)
                LangLangs = GetLangLangs(LanguageSettings);

            SetKeyboardFlags();

            FactorySettings ??= await GetFactorySettings();

            // #MainPage-LoadingVirtReg
            IfNotInVirtualRegistryUpdateItFromFactorySettingsOrDefault<string>("Rendering", FactorySettings, "0,3");
            IfNotInVirtualRegistryUpdateItFromFactorySettingsOrDefault<long>("Transput", FactorySettings, 3);

            IfNotInVirtualRegistryUpdateItFromFactorySettingsOrDefault<long>("Timeout", FactorySettings, 1);
            UpdateTimeoutInMenu();
            UpdateRenderingInMenu();
            // UpdateFontSizeInMenu();
            UpdateTransputInMenu();

            IfNotInVirtualRegistryUpdateItFromFactorySettingsOrDefault<string>("OutputPanelSettings", FactorySettings, "True|Bottom|200|400");

            var position = FromBarredString_String(Type_1_GetVirtualRegistry<string>("OutputPanelSettings"), 1);
            Type_1_UpdateVirtualRegistry<string>("OutputPanelPosition", position);

            Type_1_UpdateVirtualRegistry<bool>("OutputPanelShowing", FromBarredString_Boolean(Type_1_GetVirtualRegistry<string>("OutputPanelSettings"), 0));
            Type_1_UpdateVirtualRegistry<double>("OutputPanelHeight", (double)FromBarredString_Double(Type_1_GetVirtualRegistry<string>("OutputPanelSettings"), 2));
            Type_1_UpdateVirtualRegistry<double>("OutputPanelWidth", (double)FromBarredString_Double(Type_1_GetVirtualRegistry<string>("OutputPanelSettings"), 3));
            
            //t.Transmit(outputPanelTabViewSettings, tabControlSettings);

            //IfNotInVirtualRegistryUpdateItFromFactorySettingsOrDefault<bool>("OutputPanelShowing", FactorySettings, true);
            //IfNotInVirtualRegistryUpdateItFromFactorySettingsOrDefault<OutputPanelPosition>("OutputPanelPosition", FactorySettings, (OutputPanelPosition)Enum.Parse(typeof(OutputPanelPosition), "Bottom"));
            //IfNotInVirtualRegistryUpdateItFromFactorySettingsOrDefault<double>("OutputPanelHeight", FactorySettings, 200);
            //IfNotInVirtualRegistryUpdateItFromFactorySettingsOrDefault<double>("OutputPanelWidth", FactorySettings, 400);

            HandleOutputPanelChange(position);

            //outputPanel.Height = Type_1_GetVirtualRegistry<double>("OutputPanelHeight");
            //outputPanel.Width = Type_1_GetVirtualRegistry<double>("OutputPanelWidth");

            IfNotInVirtualRegistryUpdateItFromFactorySettingsOrDefault<string>("InterfaceLanguageName", FactorySettings, "English");
            IfNotInVirtualRegistryUpdateItFromFactorySettingsOrDefault<long>("InterfaceLanguageID", FactorySettings, 0);
            IfNotInVirtualRegistryUpdateItFromFactorySettingsOrDefault<string>("InterpreterLanguageName", FactorySettings, "English");
            IfNotInVirtualRegistryUpdateItFromFactorySettingsOrDefault<long>("InterpreterLanguageID", FactorySettings, 0);

            if (Type_1_GetVirtualRegistry<string>("InterfaceLanguageName") != null)
            {
                HandleInterfaceLanguageChange(Type_1_GetVirtualRegistry<string>("InterfaceLanguageName"));
            }

            // Engine selection:
            //  Engine will contain either "Interpreter.P2" or "Interpreter.P3"
            //  if Engine is present in LocalSettings, use that value, otherwise retrieve it from FactorySettings and update local settings
            //  if Engine is null (for some reason FactorySettings is broken), use "Interpreter.P3"

            SetEngine();
            SetScripts();
            SetInterpreterNew();
            SetInterpreterOld();

            PerTabInterpreterParameters ??= await MainPage.GetPerTabInterpreterParameters();

            if (!AfterTranslation)
            {

                IfNotInVirtualRegistryUpdateItFromFactorySettingsOrDefault<bool>("VariableLength", FactorySettings, false);
                IfNotInVirtualRegistryUpdateItFromFactorySettingsOrDefault<long>("Quietude", FactorySettings, 2);

                Type_2_UpdatePerTabSettings("Language", true, Type_1_GetVirtualRegistry<long>("InterpreterLanguageID"));
                Type_2_UpdatePerTabSettings("VariableLength", Type_1_GetVirtualRegistry<bool>("VariableLength"), Type_1_GetVirtualRegistry<bool>("VariableLength"));
                Type_2_UpdatePerTabSettings("Quietude", true, Type_1_GetVirtualRegistry<long>("Quietude"));
                Type_2_UpdatePerTabSettings("Timeout", true, Type_1_GetVirtualRegistry<long>("Timeout"));
                Type_2_UpdatePerTabSettings("Rendering", true, Type_1_GetVirtualRegistry<string>("Rendering"));
            }

            CustomTabItem navigationViewItem = (CustomTabItem)tabControl.SelectedItem;
            navigationViewItem.TabSettingsDict ??= ClonePerTabSettings(PerTabInterpreterParameters);

            UpdateTabDocumentNameIfOnlyOneAndFirst(tabControl, Type_1_GetVirtualRegistry<string>("InterfaceLanguageName"));

            if (!AfterTranslation)
            {
                // So what to we do 
                //Type_3_UpdateInFocusTabSettings("Language", true, Type_1_GetVirtualRegistry<long>("InterpreterLanguageID"));
                // Do we also set the VariableLength of the inFocusTab?
                //bool VariableLength = GetFactorySettingsWithLocalSettingsOverrideOrDefault<bool>("VariableLength", FactorySettings, LocalSettings, false);
                IfNotInVirtualRegistryUpdateItFromFactorySettingsOrDefault("VariableLength", FactorySettings, false);
                Type_3_UpdateInFocusTabSettings("VariableLength", Type_1_GetVirtualRegistry<bool>("VariableLength"), Type_1_GetVirtualRegistry<bool>("VariableLength"));
                UpdateStatusBarFromInFocusTab();
                DeserializeTabsFromVirtualRegistry();
            }
            InterfaceLanguageSelectionBuilder(mnuSelectLanguage, Internationalization_Click);
            InterpreterLanguageSelectionBuilder(mnuRun, "mnuLanguage", MnuLanguage_Click);
            UpdateEngineSelectionFromFactorySettingsInMenu();

            if (!AfterTranslation)
            {
                UpdateMenuRunningModeInMenu(PerTabInterpreterParameters["Quietude"]);
            }
            AfterTranslation = false;

            SetVariableLengthModeInMenu(mnuVariableLength, Type_1_GetVirtualRegistry<bool>("VariableLength"));

            UpdateLanguageNameInStatusBar(navigationViewItem.TabSettingsDict);

            UpdateStatusBarFromVirtualRegistry();
            UpdateCommandLineInStatusBar();

            spOutput.Visibility = Visibility.Visible;

            // outputPanel.Width = relativePanel.ActualSize.X;            
            
            (tabControl.Content as CustomRichEditBox).Focus(FocusState.Keyboard);

            string currentLanguageName = GetLanguageNameOfCurrentTab(navigationViewItem.TabSettingsDict);
            if (languageName.Text != currentLanguageName)
            {
                languageName.Text = currentLanguageName;
            }

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
                if (LocalSettings.Values.TryGetValue("Engine", out object? value))
                {
                    Engine = value.ToString();
                }
                else
                {
                    Engine = FactorySettings["Engine"].ToString();
                }
                Engine ??= "Interpreter.P3";
                Type_1_UpdateVirtualRegistry("Engine", Engine);
            }
            void SetScripts()
            {
                if (LocalSettings.Values.TryGetValue("Scripts", out object? value))
                {
                    Scripts = value.ToString();
                }
                else
                {
                    Scripts = FactorySettings["Scripts"].ToString();
                }
                Scripts ??= @"C:\peloton\code";
                Type_1_UpdateVirtualRegistry("Scripts", Scripts);
            }
            void SetInterpreterOld()
            {
                if (LocalSettings.Values.TryGetValue("Interpreter.P2", out object? value))
                {
                    InterpreterP2 = value.ToString();
                }
                else
                {
                    InterpreterP2 = FactorySettings["Interpreter.P2"].ToString();
                }
                InterpreterP2 ??= @"c:\protium\bin\pdb.exe";
                Type_1_UpdateVirtualRegistry("Interpreter.P2", InterpreterP2);
            }
            void SetInterpreterNew()
            {
                if (LocalSettings.Values.TryGetValue("Interpreter.P3", out object? value))
                {
                    InterpreterP3 = value.ToString();
                }
                else
                {
                    InterpreterP3 = FactorySettings["Interpreter.P3"].ToString();
                }
                InterpreterP3 ??= @"c:\peloton\bin\p3.exe";
                Type_1_UpdateVirtualRegistry("Interpreter.P3", InterpreterP3);
            }
        }


        private void UpdateTransputInMenu()
        {
            Telemetry t = new();
            t.SetEnabled(true);

            string transput = Type_1_GetVirtualRegistry<long>("Transput").ToString();
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
            List<string> renderers = Type_1_GetVirtualRegistry<string>("Rendering").Split(',').Select(x => x.Trim()).ToList();

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
                var myLanguageInMyLanguage = LanguageSettings[key]["GLOBAL"]["153"];
                List<string> strings = [];

                foreach (var kee in kees)
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
                var xid = long.Parse(languageSettings[x]["GLOBAL"]["ID"]);
                var yid = long.Parse(languageSettings[y]["GLOBAL"]["ID"]);
                if (xid < yid) { return -1; }
                if (xid > yid) { return 1; }
                return 0;
            }
        }

        private void UpdateStatusBarFromVirtualRegistry()
        {
            string interfaceLanguageName = Type_1_GetVirtualRegistry<string>("InterfaceLanguageName");

            bool isVariableLength = Type_1_GetVirtualRegistry<bool>("VariableLength");
            fixedVariableStatus.Text = (isVariableLength ? "#" : "@") + LanguageSettings[interfaceLanguageName]["GLOBAL"][isVariableLength ? "variableLength" : "fixedLength"];

            string[] quietudes = ["mnuQuiet", "mnuVerbose", "mnuVerbosePauseOnExit"];
            long quietude = Type_1_GetVirtualRegistry<long>("Quietude");
            quietudeStatus.Text = LanguageSettings[interfaceLanguageName]["frmMain"][quietudes.ElementAt((int)quietude)];

            string[] timeouts = ["mnu20Seconds", "mnu100Seconds", "mnu200Seconds", "mnu1000Seconds", "mnuInfinite"];
            long timeout = Type_1_GetVirtualRegistry<long>("Timeout");
            timeoutStatus.Text = $"{LanguageSettings[interfaceLanguageName]["frmMain"]["mnuTimeout"]}: {LanguageSettings[interfaceLanguageName]["frmMain"][timeouts.ElementAt((int)timeout)]}";
        }

        private void UpdateStatusBarFromInFocusTab()
        {
            string interfaceLanguageName = Type_1_GetVirtualRegistry<string>("InterfaceLanguageName");

            bool isVariableLength = Type_3_GetInFocusTab<bool>("VariableLength");
            fixedVariableStatus.Text = (isVariableLength ? "#" : "@") + LanguageSettings[interfaceLanguageName]["GLOBAL"][isVariableLength ? "variableLength" : "fixedLength"];

            string[] quietudes = ["mnuQuiet", "mnuVerbose", "mnuVerbosePauseOnExit"];
            long quietude = Type_3_GetInFocusTab<long>("Quietude");
            quietudeStatus.Text = LanguageSettings[interfaceLanguageName]["frmMain"][quietudes.ElementAt((int)quietude)];

            string[] timeouts = ["mnu20Seconds", "mnu100Seconds", "mnu200Seconds", "mnu1000Seconds", "mnuInfinite"];
            long timeout = Type_3_GetInFocusTab<long>("Timeout");
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
            if (LocalSettings.Values["Engine"].ToString() == "Interpreter.P2")
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

            //Type_1_UpdateVirtualRegistry<double>("MainWindowHeight", mainWindow.ActualHeight); 
            //Type_1_UpdateVirtualRegistry<double>("MainWindowWidth", mainWindow.ActualWidth);
            SerializeTabsToVirtualRegistry();
        }
    }
}
