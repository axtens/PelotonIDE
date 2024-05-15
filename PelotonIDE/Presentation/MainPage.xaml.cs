using DocumentFormat.OpenXml.Drawing.Charts;

using Microsoft.UI.Text;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;

using Newtonsoft.Json;

using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;

using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using Windows.System;

using Colors = Microsoft.UI.Colors;
using FactorySettingsStructure = System.Collections.Generic.Dictionary<string, object>;
using InterpreterParametersStructure = System.Collections.Generic.Dictionary<string,
    System.Collections.Generic.Dictionary<string, object>>;
using InterpreterParameterStructure = System.Collections.Generic.Dictionary<string, object>;
using LanguageConfigurationStructure = System.Collections.Generic.Dictionary<string,
    System.Collections.Generic.Dictionary<string,
        System.Collections.Generic.Dictionary<string, string>>>;
using RenderingConstantsStructure = System.Collections.Generic.Dictionary<string,
        System.Collections.Generic.Dictionary<string, object>>;
using Thickness = Microsoft.UI.Xaml.Thickness;
using System.Linq;
using TabSettingJson = System.Collections.Generic.Dictionary<string, System.Collections.Generic.Dictionary<string, object>>;
using Windows.Storage.Provider;

namespace PelotonIDE.Presentation
{
    public sealed partial class MainPage : Microsoft.UI.Xaml.Controls.Page
    {
        [GeneratedRegex("\\{\\*?\\\\[^{}]+}|[{}]|\\\\\\n?[A-Za-z]+\\n?(?:-?\\d+)?[ ]?", RegexOptions.IgnoreCase | RegexOptions.Compiled, "en-AU")]
        private static partial Regex CustomRTFRegex();
        readonly Dictionary<object, CustomRichEditBox> _richEditBoxes = [];
        // bool outputPanelShowing = true;
        enum OutputPanelPosition
        {
            Left,
            Bottom,
            Right
        }

        long Engine = 3;

        string? Scripts = string.Empty;
        string? Datas = string.Empty;

        string? InterpreterP2 = string.Empty;
        string? InterpreterP3 = string.Empty;

        int TabControlCounter = 2; // Because the XAML defines the first tab

        InterpreterParametersStructure? PerTabInterpreterParameters;
        TabSettingJson? SourceInFocusTabSettings;
        RenderingConstantsStructure? RenderingConstants = null;

        /// <summary>
        /// does not change
        /// </summary>
        LanguageConfigurationStructure? LanguageSettings;
        FactorySettingsStructure? FactorySettings;
        readonly ApplicationDataContainer LocalSettings = ApplicationData.Current.LocalSettings;

        // public LanguageConfigurationStructure? LanguageSettings1 { get => LanguageSettings; set => LanguageSettings = value; }
        readonly List<Plex>? Plexes = GetAllPlexes();

        Dictionary<string, List<string>> LangLangs = [];

        bool AfterTranslation = false;

        public MainPage()
        {
            this.InitializeComponent();

            CustomRichEditBox customREBox = new()
            {
                Tag = tab1.Tag
            };
            customREBox.KeyDown += RichEditBox_KeyDown;
            customREBox.AcceptsReturn = true;

            tabControl.Content = customREBox;
            _richEditBoxes[customREBox.Tag] = customREBox;
            tab1.TabSettingsDict = null;
            tabControl.SelectedItem = tab1;
            App._window.Closed += MainWindow_Closed;
            UpdateStatusBar();
            customREBox.Document.Selection.SetIndex(TextRangeUnit.Character, 1, false);

        }
        public static async Task<InterpreterParametersStructure?> GetPerTabInterpreterParameters()
        {
            StorageFile tabSettingStorage = await StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///PelotonIDE\\Presentation\\PerTabInterpreterParameters.json"));
            string tabSettings = File.ReadAllText(tabSettingStorage.Path);
            return JsonConvert.DeserializeObject<InterpreterParametersStructure>(tabSettings);
        }
        private static async Task<LanguageConfigurationStructure?> GetLanguageConfiguration()
        {
            StorageFile languageConfig = await StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///PelotonIDE\\Presentation\\LanguageConfiguration.json"));
            string languageConfigString = File.ReadAllText(languageConfig.Path);
            LanguageConfigurationStructure? languages = JsonConvert.DeserializeObject<LanguageConfigurationStructure>(languageConfigString);
            languages.Remove("Viet");
            return languages;
        }
        private static async Task<RenderingConstantsStructure?> GetRenderingConstants()
        {
            StorageFile renderingConfig = await StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///PelotonIDE\\Presentation\\RenderingConstants.json"));
            string renderingConfigText = File.ReadAllText(renderingConfig.Path);
            RenderingConstantsStructure? renderers = JsonConvert.DeserializeObject<RenderingConstantsStructure>(renderingConfigText);
            return renderers;
        }
        private static async Task<FactorySettingsStructure?> GetFactorySettings()
        {
            StorageFile globalSettings = await StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///PelotonIDE\\Presentation\\FactorySettings.json"));
            string globalSettingsString = File.ReadAllText(globalSettings.Path);
            return JsonConvert.DeserializeObject<FactorySettingsStructure>(globalSettingsString);
        }
        private async void InterfaceLanguageSelectionBuilder(MenuFlyoutSubItem menuBarItem, RoutedEventHandler routedEventHandler)
        {
            string interfaceLanguageName = Type_1_GetVirtualRegistry<string>("ideOps.InterfaceLanguageName");
            if (interfaceLanguageName == null || !LanguageSettings.ContainsKey(interfaceLanguageName))
            {
                return;
            }

            menuBarItem.Items.Clear();

            // what is current language?
            Dictionary<string, string> globals = LanguageSettings[interfaceLanguageName]["GLOBAL"];
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
                        Foreground = names.First() == Type_1_GetVirtualRegistry<string>("ideOps.InterfaceLanguageName") ? new SolidColorBrush(Colors.White) : new SolidColorBrush(Colors.Black),
                        Background = names.First() == Type_1_GetVirtualRegistry<string>("ideOps.InterfaceLanguageName") ? new SolidColorBrush(Colors.Black) : new SolidColorBrush(Colors.White),
                        Tag = i.ToString()
                    };
                    menuFlyoutItem.Click += routedEventHandler; //  Internationalization_Click;
                    menuBarItem.Items.Add(menuFlyoutItem);
                }
            }
        }
        private async void InterpreterLanguageSelectionBuilder(MenuBarItem menuBarItem, string menuLabel, RoutedEventHandler routedEventHandler)
        {
            Telemetry.EnableIfMethodNameInFactorySettingsTelemetry();

            LanguageSettings ??= await GetLanguageConfiguration();
            string interfaceLanguageName = Type_1_GetVirtualRegistry<string>("ideOps.InterfaceLanguageName");

            if (interfaceLanguageName == null || !LanguageSettings.ContainsKey(interfaceLanguageName))
            {
                return;
            }

            menuBarItem.Items.Remove(item => item.Name == menuLabel && item.GetType().Name == "MenuFlyoutSubItem");

            MenuFlyoutSubItem sub = new()
            {
                Text = LanguageSettings[interfaceLanguageName]["frmMain"][menuLabel],
                BorderThickness = new Thickness(1, 1, 1, 1),
                BorderBrush = new SolidColorBrush() { Color = Colors.LightGray },
                Name = menuLabel
            };

            // what is current language?
            Dictionary<string, string> globals = LanguageSettings[interfaceLanguageName]["GLOBAL"];
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
                        Foreground = names.First() == Type_1_GetVirtualRegistry<string>("mainOps.InterpreterLanguageName") ? new SolidColorBrush(Colors.White) : new SolidColorBrush(Colors.Black),
                        Background = names.First() == Type_1_GetVirtualRegistry<string>("mainOps.InterpreterLanguageName") ? new SolidColorBrush(Colors.Black) : new SolidColorBrush(Colors.White),
                    };
                    menuFlyoutItem.Click += routedEventHandler;
                    sub.Items.Add(menuFlyoutItem);
                }
            }
            menuBarItem.Items.Add(sub);
        }
        private static void MenuItemHighlightController(MenuFlyoutItem? menuFlyoutItem, bool onish)
        {
            Telemetry.EnableIfMethodNameInFactorySettingsTelemetry();

            Telemetry.Transmit("menuFlyoutItem.Name=", menuFlyoutItem.Name, "onish=", onish);
            if (onish)
            {
                menuFlyoutItem.Background = new SolidColorBrush(Colors.Black);
                menuFlyoutItem.Foreground = new SolidColorBrush(Colors.White);
            }
            else
            {
                menuFlyoutItem.Foreground = new SolidColorBrush(Colors.Black);
                menuFlyoutItem.Background = new SolidColorBrush(Colors.White);
            }
        }
        // private void ToggleVariableLengthModeInMenu(InterpreterParameterStructure variableLength) => MenuItemHighlightController(mnuVariableLength, (bool)variableLength["Defined"]);
        //private void SetVariableLengthModeInMenu(MenuFlyoutItem? menuFlyoutItem, bool showEnabled)
        //{
        //    Telemetry.EnableIfMethodNameInFactorySettingsTelemetry();
        //    Telemetry.Transmit("menuFlyoutItem.Name=", menuFlyoutItem.Name, "showEnabled=", showEnabled);
        //    if (showEnabled)
        //    {
        //        menuFlyoutItem.Background = new SolidColorBrush(Colors.Black);
        //        menuFlyoutItem.Foreground = new SolidColorBrush(Colors.White);
        //    }
        //    else
        //    {
        //        menuFlyoutItem.Background = new SolidColorBrush(Colors.White);
        //        menuFlyoutItem.Foreground = new SolidColorBrush(Colors.Black);
        //    }
        //}
        //private void ToggleVariableLengthModeInMenu(bool flag) => MenuItemHighlightController(mnuVariableLength, flag);
        //private void UpdateTimeoutInMenu()
        //{
        //    foreach (MenuFlyoutItemBase? item in mnuTimeout.Items)
        //    {
        //        MenuItemHighlightController((MenuFlyoutItem)item!, false);
        //    }
        //    long currTimeout = Type_1_GetVirtualRegistry<long>("ideOps.Timeout");

        //    switch (currTimeout)
        //    {
        //        case 0:
        //            MenuItemHighlightController(mnu20Seconds, true);
        //            break;

        //        case 1:
        //            MenuItemHighlightController(mnu100Seconds, true);
        //            break;

        //        case 2:
        //            MenuItemHighlightController(mnu200Seconds, true);
        //            break;

        //        case 3:
        //            MenuItemHighlightController(mnu1000Seconds, true);
        //            break;

        //        case 4:
        //            MenuItemHighlightController(mnuInfinite, true);
        //            break;

        //    }
        //}
        //private void UpdateMenuRunningModeInMenu(InterpreterParameterStructure quietude)
        //{
        //    if ((bool)quietude["Defined"])
        //    {
        //        mnuRunningMode.Items.ForEach(item =>
        //        {
        //            MenuItemHighlightController((MenuFlyoutItem)item, false);
        //            if ((long)quietude["Value"] == long.Parse((string)item.Tag))
        //            {
        //                MenuItemHighlightController((MenuFlyoutItem)item, true);
        //            }
        //        });
        //    }
        //}

        #region Event Handlers
        private InterpreterParametersStructure ShallowCopyPerTabSetting(InterpreterParametersStructure? perTabInterpreterParameters)
        {
            if (Type_1_GetVirtualRegistry<bool>("ideOps.UsePerTabSettingsWhenCreatingTab"))
            {
                return parameterBlock(perTabInterpreterParameters);
            }
            else
            {
                if (InFocusTab().TabSettingsDict != null)
                    return parameterBlock(InFocusTab().TabSettingsDict);
                else
                    return parameterBlock(perTabInterpreterParameters);
            }

            static InterpreterParametersStructure parameterBlock(RenderingConstantsStructure? parameterBlk)
            {
                InterpreterParametersStructure clone = [];
                foreach (string outerKey in parameterBlk.Keys)
                {
                    FactorySettingsStructure inner = [];
                    foreach (string innerKey in parameterBlk[outerKey].Keys)
                    {
                        inner[innerKey] = parameterBlk[outerKey][innerKey];
                    }
                    clone[outerKey] = inner;
                }
                return clone;
            }
        }
        public string GetLanguageNameOfCurrentTab(InterpreterParametersStructure? tabSettingJson)
        {
            Telemetry.EnableIfMethodNameInFactorySettingsTelemetry();

            long langValue;
            string langName;
            if (AnInFocusTabExists())
            {
                langValue = Type_3_GetInFocusTab<long>("pOps.Language");
                langName = LanguageSettings[Type_1_GetVirtualRegistry<string>("ideOps.InterfaceLanguageName")]["GLOBAL"][$"{101 + langValue}"];
            }
            else
            {
                langValue = Type_2_GetPerTabSettings<long>("pOps.Language");
                langName = LanguageSettings[Type_1_GetVirtualRegistry<string>("ideOps.InterfaceLanguageName")]["GLOBAL"][$"{101 + langValue}"];
            }
            Telemetry.Transmit("langValue=", langValue, "langName=", langName);
            return langName;
        }
        //private void UpdateLanguageNameInStatusBar(InterpreterParametersStructure? tabSettingJson)
        //{
        //    sbLanguageName.Text = GetLanguageNameOfCurrentTab(tabSettingJson);
        //}
        private string? GetLanguageNameFromID(long interpreterLanguageID) => (from lang
                                                                              in LanguageSettings
                                                                              where long.Parse(lang.Value["GLOBAL"]["ID"]) == interpreterLanguageID
                                                                              select lang.Value["GLOBAL"]["Name"]).First();
        #endregion
        public void HandleCustomPropertySaving(StorageFile file, CustomTabItem navigationViewItem)
        {
            Telemetry.EnableIfMethodNameInFactorySettingsTelemetry();

            string rtfContent = File.ReadAllText(file.Path);
            StringBuilder rtfBuilder = new(rtfContent);

            const int ONCE = 1;

            InterpreterParametersStructure? inFocusTab = navigationViewItem.TabSettingsDict;
            Regex ques = new(Regex.Escape("?"));
            string info = @"{\info {\*\ilang ?} {\*\ilength ?} {\*\itimeout ?} {\*\iquietude ?} {\*\itransput ?} {\*\irendering ?} {\*\iinterpreter ?} {\*\iselected ?} {\*\ipadded ?} }"; // 
            info = ques.Replace(info, $"{inFocusTab["pOps.Language"]["Value"]}", ONCE);
            info = ques.Replace(info, (bool)inFocusTab["pOps.VariableLength"]["Value"] ? "1" : "0", ONCE);
            info = ques.Replace(info, $"{(long)inFocusTab["ideOps.Timeout"]["Value"]}", ONCE);
            info = ques.Replace(info, $"{(long)inFocusTab["pOps.Quietude"]["Value"]}", ONCE);
            info = ques.Replace(info, $"{(long)inFocusTab["pOps.Transput"]["Value"]}", ONCE);
            info = ques.Replace(info, $"{(string)inFocusTab["outputOps.ActiveRenderers"]["Value"]}", ONCE);
            info = ques.Replace(info, $"{(long)inFocusTab["ideOps.Engine"]["Value"]}", ONCE);
            info = ques.Replace(info, $"{(long)inFocusTab["outputOps.TappedRenderer"]["Value"]}", ONCE);
            info = ques.Replace(info, $"{(long)inFocusTab["pOps.Padding"]["Value"]}", ONCE);

            Telemetry.Transmit("info=", info);

            Regex regex = CustomRTFRegex();

            MatchCollection matches = regex.Matches(rtfContent);

            IEnumerable<Match> infos = from match in matches where match.Value == @"\info" select match;

            if (infos.Any())
            {
                string fullBlock = rtfContent.Substring(infos.First().Index, infos.First().Length);
                MatchCollection blockMatches = regex.Matches(fullBlock);
            }
            else
            {
                const string start = @"{\rtf1";
                int i = rtfContent.IndexOf(start);
                int j = i + start.Length;
                rtfBuilder.Insert(j, info);
            }

            Telemetry.Transmit("rtfBuilder=", rtfBuilder.ToString());

            string? text = rtfBuilder.ToString();
            if (text.EndsWith((char)0x0)) text = text.Remove(text.Length - 1);
            while (text.LastIndexOf("\\par\r\n}") > -1)
            {
                text = text.Remove(text.LastIndexOf("\\par\r\n}"), 6);
            }

            File.WriteAllText(file.Path, text, Encoding.ASCII);
        }
        public void HandleCustomPropertyLoading(StorageFile file, CustomRichEditBox customRichEditBox)
        {
            string rtfContent = File.ReadAllText(file.Path);
            Regex regex = CustomRTFRegex();
            string orientation = "00";
            MatchCollection matches = regex.Matches(rtfContent);

            IEnumerable<Match> infos = from match in matches where match.Value.StartsWith(@"\info") select match;
            if (infos.Any())
            {
                IEnumerable<Match> ilang = from match in matches where match.Value.Contains(@"\ilang") select match;
                if (ilang.Any())
                {
                    string[] items = ilang.First().Value.Split(' ');
                    if (items.Any())
                    {
                        (long id, string orientation) internalLanguageIdAndOrientation = ConvertILangToInternalLanguageAndOrientation(long.Parse(items[1].Replace("}", "")));
                        Type_3_UpdateInFocusTabSettings("pOps.Language", true, internalLanguageIdAndOrientation.id);
                        orientation = internalLanguageIdAndOrientation.orientation;
                    }
                }
                IEnumerable<Match> ilength = from match in matches where match.Value.Contains(@"\ilength") select match;
                if (ilength.Any())
                {
                    string[] items = ilength.First().Value.Split(' ');
                    if (items.Any())
                    {
                        string flag = items[1].Replace("}", "");
                        if (flag == "0")
                        {
                            Type_3_UpdateInFocusTabSettings("pOps.VariableLength", false, false);
                        }
                        else
                        {
                            Type_3_UpdateInFocusTabSettings("pOps.VariableLength", true, true);
                        }
                    }
                }

                MarkupToInFocusSettingLong(matches, @"\itimeout", "ideOps.Timeout");
                MarkupToInFocusSettingLong(matches, @"\iquietude", "pOps.Quietude");
                MarkupToInFocusSettingLong(matches, @"\itransput", "pOps.Transput");
                MarkupToInFocusSettingString(matches, @"\irendering", "outputOps.ActiveRenderers");
                MarkupToInFocusSettingLong(matches, @"\iselected", "outputOps.TappedRenderer");
                MarkupToInFocusSettingLong(matches, @"\iinterpreter", "ideOps.Engine");
                MarkupToInFocusSettingBoolean(matches, @"\ipadded", "pOps.Padding");

            }
            else
            {
                IEnumerable<Match> deflang = from match in matches where match.Value.StartsWith(@"\deflang") select match;
                if (deflang.Any())
                {
                    string deflangId = deflang.First().Value.Replace(@"\deflang", "");
                    (long id, string orientation) internalLanguageIdAndOrientation = ConvertILangToInternalLanguageAndOrientation(long.Parse(deflangId));
                    Type_3_UpdateInFocusTabSettings("pOps.Language", true, internalLanguageIdAndOrientation.id);
                    orientation = internalLanguageIdAndOrientation.orientation;
                }
                else
                {
                    IEnumerable<Match> lang = from match in matches where match.Value.StartsWith(@"\lang") select match;
                    if (lang.Any())
                    {
                        string langId = lang.First().Value.Replace(@"\lang", "");
                        (long id, string orientation) internalLanguageIdAndOrientation = ConvertILangToInternalLanguageAndOrientation(long.Parse(langId));
                        Type_3_UpdateInFocusTabSettings("pOps.Language", true, internalLanguageIdAndOrientation.id);
                        orientation = internalLanguageIdAndOrientation.orientation;
                    }
                    else
                    {
                        Type_3_UpdateInFocusTabSettings("pOps.Language", true, Type_2_GetPerTabSettings<long>("pOps.Language")); // whatever the current perTab value is
                    }
                }
                if (rtfContent.Contains("<# ") && rtfContent.Contains("</#>"))
                {
                    Type_3_UpdateInFocusTabSettings("pOps.VariableLength", true, true);
                }
            }
            if (orientation[1] == '1')
            {
                customRichEditBox.FlowDirection = FlowDirection.RightToLeft;
            }
        }
        private void MarkupToInFocusSettingLong(MatchCollection matches, string markup, string setting)
        {
            IEnumerable<Match> markups = from match in matches where match.Value.Contains(markup) select match;
            if (markups.Any())
            {
                string[] marked = markups.First().Value.Split(' ');
                if (marked.Any())
                {
                    string arg = marked[1].Replace("}", "");
                    Type_3_UpdateInFocusTabSettings<long>(setting, true, long.Parse(arg));
                }
            }
        }
        private void MarkupToInFocusSettingBoolean(MatchCollection matches, string markup, string setting)
        {
            IEnumerable<Match> markups = from match in matches where match.Value.Contains(markup) select match;
            if (markups.Any())
            {
                string[] marked = markups.First().Value.Split(' ');
                if (marked.Any())
                {
                    string arg = marked[1].Replace("}", "");
                    Type_3_UpdateInFocusTabSettings<bool>(setting, true, long.Parse(arg) == 1);
                }
            }
        }
        private void MarkupToInFocusSettingString(MatchCollection matches, string markup, string setting)
        {
            IEnumerable<Match> markups = from match in matches where match.Value.Contains(markup) select match;
            if (markups.Any())
            {
                string[] marked = markups.First().Value.Split(' ');
                if (marked.Any())
                {
                    string arg = marked[1].Replace("}", "");
                    Type_3_UpdateInFocusTabSettings<string>(setting, true, arg);
                }
            }
        }
        private (long id, string orientation) ConvertILangToInternalLanguageAndOrientation(long v)
        {
            foreach (string language in LanguageSettings.Keys)
            {
                Dictionary<string, string> global = LanguageSettings[language]["GLOBAL"];
                if (long.Parse(global["ID"]) == v)
                {
                    return (long.Parse(global["ID"]), global["TextOrientation"]);
                }
                else
                {
                    if (global["ilangAlso"].Split(',').Contains(v.ToString()))
                    {
                        return (long.Parse(global["ID"]), global["TextOrientation"]);
                    }
                }
            }
            return (long.Parse(LanguageSettings["English"]["GLOBAL"]["ID"]), LanguageSettings["English"]["GLOBAL"]["TextOrientation"]); // default
        }
        private static void HandlePossibleAmpersandInMenuItem(string name, MenuFlyoutItemBase mfib)
        {
            if (name.Contains('&'))
            {
                string accel = name.Substring(name.IndexOf("&") + 1, 1);
                mfib.KeyboardAccelerators.Add(new KeyboardAccelerator()
                {
                    Key = Enum.Parse<VirtualKey>(accel.ToUpperInvariant()),
                    Modifiers = VirtualKeyModifiers.Menu
                });
                name = name.Replace("&", "");
            }
            switch (mfib.GetType().Name)
            {
                case "MenuFlyoutSubItem":
                    ((MenuFlyoutSubItem)mfib).Text = name;
                    break;
                case "MenuFlyoutItem":
                    ((MenuFlyoutItem)mfib).Text = name;
                    break;
                default:
                    Debugger.Launch();
                    break;
            }
        }
        private static void HandlePossibleAmpersandInMenuItem(string name, MenuBarItem mbi)
        {
            if (name.Contains('&'))
            {
                string accel = name.Substring(name.IndexOf("&") + 1, 1);
                try
                {
                    mbi.KeyboardAccelerators.Add(new KeyboardAccelerator()
                    {
                        Key = Enum.Parse<VirtualKey>(accel.ToUpperInvariant()),
                        Modifiers = VirtualKeyModifiers.Menu
                    });
                }
                catch (Exception ex)
                {
                    Telemetry.EnableIfMethodNameInFactorySettingsTelemetry();
                    Telemetry.Transmit(ex.Message, accel);
                }
                name = name.Replace("&", "");
            }
            mbi.Title = name;
        }
        private static void HandlePossibleAmpersandInMenuItem(string name, MenuFlyoutItem mfi)
        {
            if (name.Contains('&'))
            {
                string accel = name.Substring(name.IndexOf("&") + 1, 1);
                mfi.KeyboardAccelerators.Add(new KeyboardAccelerator()
                {
                    Key = Enum.Parse<VirtualKey>(accel.ToUpperInvariant()),
                    Modifiers = VirtualKeyModifiers.Menu
                });
                name = name.Replace("&", "");
            }
            mfi.Text = name;
        }
        private string BuildTabCommandLine()
        {
            static List<string> BuildWith(InterpreterParametersStructure? interpreterParametersStructure)
            {
                List<string> paras = [];

                if (interpreterParametersStructure != null)
                {
                    foreach (string key in interpreterParametersStructure.Keys)
                    {
                        if ((bool)interpreterParametersStructure[key]["Defined"] && !(bool)interpreterParametersStructure[key]["Internal"])
                        {
                            string entry = $"/{interpreterParametersStructure[key]["Key"]}";
                            object value = interpreterParametersStructure[key]["Value"];
                            string type = value.GetType().Name;
                            switch (type)
                            {
                                case "Boolean":
                                    if ((bool)value) paras.Add(entry);
                                    break;
                                default:
                                    paras.Add($"{entry}:{value}");
                                    break;
                            }
                        }
                    }
                }
                return paras;
            }

            TabSettingJson? tsd = AnInFocusTabExists() ? InFocusTab().TabSettingsDict : PerTabInterpreterParameters;
            List<string> paras = [.. BuildWith(tsd)];

            return string.Join<string>(" ", [.. paras]);
        }
        private void FormatMenu_FontSize_Click(object sender, RoutedEventArgs e)
        {
            Telemetry.EnableIfMethodNameInFactorySettingsTelemetry();
            string interfaceLanguageName = Type_1_GetVirtualRegistry<string>("ideOps.InterfaceLanguageName");
            Dictionary<string, string> global = LanguageSettings[interfaceLanguageName]["GLOBAL"];
            Dictionary<string, string> frmMain = LanguageSettings[interfaceLanguageName]["frmMain"];
            var me = (MenuFlyoutItem)sender;
            Telemetry.Transmit(me.Name);

            CustomRichEditBox currentRichEditBox = _richEditBoxes[((CustomTabItem)tabControl.SelectedItem).Tag];
            double tag = double.Parse((string)me.Tag);
            currentRichEditBox.Document.Selection.CharacterFormat.Size = (float)tag;
            currentRichEditBox.Document.Selection.SelectOrDefault(x => x);

            Type_1_UpdateVirtualRegistry<double>("ideOps.FontSize", tag);
            Type_2_UpdatePerTabSettings<double>("ideOps.FontSize", true, tag);
            Type_3_UpdateInFocusTabSettings<double>("ideOps.FontSize", true, tag);
            //if (tag != Type_3_GetInFocusTab<double>("ideOps.FontSize"))
            //{
            //    _ = Type_3_UpdateInFocusTabSettingsIfPermittedAsync<double>("ideOps.FontSize", true, tag, $"{global["Document"]}: {frmMain["mnuFontSize"]} = '{tag}'?");
            //}
            UpdateMenus();
        }
        private void EnableAllOutputPanelTabsMatchingRendering()
        {
            Telemetry.EnableIfMethodNameInFactorySettingsTelemetry();
            if (!AnInFocusTabExists()) return;
            if (InFocusTab().TabSettingsDict == null) return;
            foreach (string key2 in Type_3_GetInFocusTab<string>("outputOps.ActiveRenderers").Split(",", StringSplitOptions.RemoveEmptyEntries))
            {
                foreach (object? item in outputPanelTabView.TabItems)
                {
                    var tvi = (TabViewItem)item;
                    if ((string)tvi.Tag == key2)
                    {
                        tvi.IsEnabled = true;
                        Telemetry.Transmit("tvi.Tag=", tvi.Tag, "tvi.IsEnabled=", tvi.IsEnabled);
                    }

                }
            }
        }
        private void DeselectAndDisableAllOutputPanelTabs()
        {
            Telemetry.EnableIfMethodNameInFactorySettingsTelemetry();
            outputPanelTabView.TabItems.ForEach(item =>
            {
                TabViewItem tvi = (TabViewItem)item;
                Telemetry.Transmit("tvi.Name=", tvi.Name, "tvi.Tag=", tvi.Tag, "IsSelected=", tvi.IsSelected, "IsEnabled", tvi.IsEnabled);
                tvi.IsSelected = false;
                tvi.IsEnabled = false;
                Telemetry.Transmit("tvi.Name=", tvi.Name, "tvi.Tag=", tvi.Tag, "IsSelected=", tvi.IsSelected, "IsEnabled", tvi.IsEnabled);
            });
        }
        private void InterpretMenu_Transput_Click(object sender, RoutedEventArgs e)
        {
            Telemetry.EnableIfMethodNameInFactorySettingsTelemetry();
            string interfaceLanguageName = Type_1_GetVirtualRegistry<string>("ideOps.InterfaceLanguageName");
            Dictionary<string, string> global = LanguageSettings[interfaceLanguageName]["GLOBAL"];
            Dictionary<string, string> frmMain = LanguageSettings[interfaceLanguageName]["frmMain"];

            Dictionary<string, string> DSS = [];

            MenuFlyoutItem me = (MenuFlyoutItem)sender;
            foreach (MenuFlyoutItem? mfi in from MenuFlyoutSubItem mfsi in mnuTransput.Items.Cast<MenuFlyoutSubItem>()
                                            where mfsi != null
                                            where mfsi.Items.Count > 0
                                            from MenuFlyoutItem mfi in mfsi.Items.Cast<MenuFlyoutItem>()
                                            select mfi)
            {
                DSS[(string)mfi.Tag] = mfi.Text; 
                MenuItemHighlightController((MenuFlyoutItem)mfi, false);
                if ((string)me.Tag == (string)mfi.Tag)
                {
                    MenuItemHighlightController((MenuFlyoutItem)mfi, true);
                }
            }
            long tag = long.Parse((string)me.Tag);
            Type_1_UpdateVirtualRegistry("pOps.Transput", tag);
            Type_2_UpdatePerTabSettings("pOps.Transput", true, tag);
            if (tag != Type_3_GetInFocusTab<long>("pOps.Transput"))
            {
                _ = Type_3_UpdateInFocusTabSettingsIfPermittedAsync<long>("pOps.Transput", true, tag, $"{global["Document"]}: {frmMain["mnuUpdate"]} {frmMain["mnuTransput"]} = '{DSS[tag.ToString()]}'?"); // mnuUpdate
            }

            UpdateStatusBar();
        }
        private void Help_Click(object sender, RoutedEventArgs e)
        {
            Telemetry.EnableIfMethodNameInFactorySettingsTelemetry();
            MenuFlyoutItem me = (MenuFlyoutItem)sender;
            Telemetry.Transmit(me.Name);

            ProcessStartInfo startInfo = new()
            {
                UseShellExecute = true,
                Verb = "open",
                FileName = @"c:\protium\bin\help\protium.chm",
                WindowStyle = ProcessWindowStyle.Normal
            };
            Process.Start(startInfo);
        }
        private void OutputPanelTabView_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            Telemetry.EnableIfMethodNameInFactorySettingsTelemetry();
            TabView me = (TabView)sender;
            Telemetry.Transmit(me.Name, "e.PreviousSize=", e.PreviousSize, "e.NewSize=", e.NewSize);
            string pos = Type_1_GetVirtualRegistry<string>("ideOps.OutputPanelPosition") ?? "Bottom";
            Type_1_UpdateVirtualRegistry<string>("OutputPanelTabView_Settings", string.Join("|", [pos, e.NewSize.Height, e.NewSize.Width]));
            //vHW.Text = $"OutputPanelTabView: {e.NewSize.Height}/{e.NewSize.Width}";
        }
        private void Page_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            Telemetry.EnableIfMethodNameInFactorySettingsTelemetry();
            Page me = (Page)sender;

            //pHW.Text = $"Page: {e.NewSize.Height}/{e.NewSize.Width}";

            if (Type_1_GetVirtualRegistry<string>("ideOps.OutputPanelPosition") == "Bottom")
            {
                if (!double.IsNaN(outputPanelTabView.Height))
                {

                    double winHeight = e.PreviousSize.Height;
                    double optvHeight = outputPanelTabView.Height;
                    Telemetry.Transmit("winHeight=", winHeight, "optvWidth=", optvHeight, "(winHeight - optvHeight)=", (winHeight - optvHeight), "(winHeight - optvHeight) / winHeight=", (winHeight - optvHeight) / winHeight);
                    if (((winHeight - optvHeight) / winHeight) <= 0.10)
                    {
                        return;
                    }
                    double winPanHeightRatio = optvHeight / winHeight;
                    double newHeight = Math.Floor(e.NewSize.Height * winPanHeightRatio);
                    outputPanel.Height = newHeight;
                }
            }
            else
            {
                if (!double.IsNaN(outputPanelTabView.Width))
                {
                    double winWidth = e.PreviousSize.Width;
                    double optvWidth = outputPanelTabView.Width;
                    Telemetry.Transmit("winWidth=", winWidth, "optvWidth=", optvWidth, "(winWidth - optvWidth)=", (winWidth - optvWidth), "(winWidth - optvWidth) / winWidth=", (winWidth - optvWidth) / winWidth);
                    if (((winWidth - optvWidth) / winWidth) <= 0.10)
                    {
                        return;
                    }
                    double winPanWidthRatio = optvWidth / winWidth;
                    double newWidth = Math.Floor(e.NewSize.Width * winPanWidthRatio);
                    outputPanel.Width = newWidth;
                }
            }
        }
        private void TabView_Rendering_TabItemsChanged(TabView sender, Windows.Foundation.Collections.IVectorChangedEventArgs args)
        {
            Telemetry.EnableIfMethodNameInFactorySettingsTelemetry();
            TabView me = (TabView)sender;
            //Telemetry.Transmit("me.Name=",me.Name, "me,Tag=",me.Tag, "args.Index=",args.Index, "args.CollectionChange=", args.CollectionChange, "Names=",string.Join(',', me.TabItems.Select(e => ((TabViewItem)e).Name)));
            //SerializeTabsToVirtualRegistry();
        }
        private void SettingsMenu_EditorConfiguration_Click(object sender, RoutedEventArgs e)
        {
            bool UsePerTabSettingsWhenCreatingTab = Type_1_GetVirtualRegistry<bool>("ideOps.UsePerTabSettingsWhenCreatingTab");
            MenuFlyoutItem me = (MenuFlyoutItem)sender;
            switch ((string)me.Tag)
            {
                case "PerTab":
                    MenuItemHighlightController(mnuPerTabSettings, true);
                    MenuItemHighlightController(mnuCurrentTabSettings, false);
                    UsePerTabSettingsWhenCreatingTab = true;
                    break
;
                case "CurrentTab":
                    MenuItemHighlightController(mnuPerTabSettings, false);
                    MenuItemHighlightController(mnuCurrentTabSettings, true);
                    UsePerTabSettingsWhenCreatingTab = false;
                    break;
            }
            Type_1_UpdateVirtualRegistry<bool>("ideOps.UsePerTabSettingsWhenCreatingTab", UsePerTabSettingsWhenCreatingTab);
        }
    }
}
