using DocumentFormat.OpenXml.Drawing;
using DocumentFormat.OpenXml.Wordprocessing;

using Microsoft.UI.Text;
using Microsoft.UI.Xaml.Input;

using Newtonsoft.Json;

using System.Diagnostics;
using System.Numerics;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;

using Windows.Storage;
using Windows.System;
using Windows.UI.Notifications;
using Windows.UI.Text;

using Colors = Microsoft.UI.Colors;
using FactorySettingsStructure = System.Collections.Generic.Dictionary<string, object>;
using InterpreterParametersStructure = System.Collections.Generic.Dictionary<string,
    System.Collections.Generic.Dictionary<string, object>>;
using ITextSelection = Microsoft.UI.Text.ITextSelection;
using LanguageConfigurationStructure = System.Collections.Generic.Dictionary<string,
    System.Collections.Generic.Dictionary<string,
        System.Collections.Generic.Dictionary<string, string>>>;
using PointOptions = Microsoft.UI.Text.PointOptions;
using RenderingConstantsStructure = System.Collections.Generic.Dictionary<string,
        System.Collections.Generic.Dictionary<string, object>>;
using Style = Microsoft.UI.Xaml.Style;
using TabSettingJson = System.Collections.Generic.Dictionary<string, System.Collections.Generic.Dictionary<string, object>>;
using TextAlignment = Microsoft.UI.Xaml.TextAlignment;
using TextGetOptions = Microsoft.UI.Text.TextGetOptions;
using TextRangeUnit = Microsoft.UI.Text.TextRangeUnit;
using TextSetOptions = Microsoft.UI.Text.TextSetOptions;
using Thickness = Microsoft.UI.Xaml.Thickness;

namespace PelotonIDE.Presentation
{
    public sealed partial class MainPage : Microsoft.UI.Xaml.Controls.Page
    {
        [GeneratedRegex("\\{\\*?\\\\[^{}]+}|[{}]|\\\\\\n?[A-Za-z]+\\n?(?:-?\\d+)?[ ]?", RegexOptions.IgnoreCase | RegexOptions.Compiled, "en-AU")]
        private static partial Regex CustomRTFRegex();
        public readonly Dictionary<object, CustomRichEditBox> _richEditBoxes = [];
        // bool outputPanelShowing = true;
        enum OutputPanelPosition
        {
            Left,
            Bottom,
            Right
        }

        long Engine = 3;

        string? Codes = string.Empty;
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
        readonly List<PlexBlock>? PlexBlocks = GetAllPlexBlocks();

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
        public static async Task<InterpreterParametersStructure?> GetPerTabInterpreterParametersIncludingMatchingVirtualRegistry()
        {
            StorageFile tabSettingStorage = await StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///PelotonIDE\\Presentation\\PerTabInterpreterParameters.json"));
            string tabSettings = File.ReadAllText(tabSettingStorage.Path);
            var deserialisation = JsonConvert.DeserializeObject<InterpreterParametersStructure>(tabSettings);
            foreach (var key in deserialisation.Keys)
            {
                FactorySettingsStructure rec = deserialisation[key];
                if ((bool)rec["Internal"])
                {
                    var typ = rec["Value"].GetType().Name;
                    switch (typ)
                    {
                        case "String":
                            if (ApplicationData.Current.LocalSettings.Values[key] != null)
                                deserialisation[key]["Value"] = (string)ApplicationData.Current.LocalSettings.Values[key];// Type_1_GetVirtualRegistry<string>(key);
                            break;
                        case "Int64":
                            if (ApplicationData.Current.LocalSettings.Values[key] != null)
                                deserialisation[key]["Value"] = (long)ApplicationData.Current.LocalSettings.Values[key];
                            break;
                        case "Double":
                            if (ApplicationData.Current.LocalSettings.Values[key] != null)
                                deserialisation[key]["Value"] = (double)ApplicationData.Current.LocalSettings.Values[key];
                            break;
                        case "Boolean":
                            if (ApplicationData.Current.LocalSettings.Values[key] != null)
                                deserialisation[key]["Value"] = (bool)ApplicationData.Current.LocalSettings.Values[key];
                            break;
                    }
                }
            }
            return deserialisation;
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
            Telemetry.Disable();

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
            Telemetry.Disable();

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
        #region Event Handlers
        private InterpreterParametersStructure ShallowCopyPerTabSetting(InterpreterParametersStructure? perTabInterpreterParameters)
        {
            if (Type_1_GetVirtualRegistry<bool>("ideOps.UsePerTabSettingsWhenCreatingTab"))
            {
                return parameterBlock(perTabInterpreterParameters);
            }
            else
            {
                var ift = InFocusTab();
                if (ift != null && ift.TabSettingsDict != null)
                    return parameterBlock(ift.TabSettingsDict);
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
            Telemetry.Disable();

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

        private string? GetLanguageNameFromID(long interpreterLanguageID) => (from lang
                                                                              in LanguageSettings
                                                                              where long.Parse(lang.Value["GLOBAL"]["ID"]) == interpreterLanguageID
                                                                              select lang.Value["GLOBAL"]["Name"]).First();
        #endregion
        public void HandleCustomPropertySaving(string rtfContent, CustomTabItem navigationViewItem, string path)
        {
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
            info = ques.Replace(info, (bool)inFocusTab["pOps.Padding"]["Value"] ? "1" : "0", ONCE);

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

            File.WriteAllText(path, text, Encoding.ASCII);

        }


        public void HandleCustomPropertySaving(string file, CustomTabItem navigationViewItem)
        {
            string rtfContent = File.ReadAllText(file);
            HandleCustomPropertySaving(rtfContent, navigationViewItem, file);

        }
        public void HandleCustomPropertySaving(StorageFile file, CustomTabItem navigationViewItem)
        {
            Telemetry.Disable();

            string rtfContent = File.ReadAllText(file.Path);
            HandleCustomPropertySaving(rtfContent, navigationViewItem, file.Path);
        }
        public void HandleCustomPropertyLoading(string rtfContent, CustomRichEditBox customRichEditBox, int differentiator)
        {
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
        public void HandleCustomPropertyLoading(string file, CustomRichEditBox customRichEditBox)
        {
            string rtfContent = File.ReadAllText(file);
            HandleCustomPropertyLoading(rtfContent, customRichEditBox, 1);
        }
        public void HandleCustomPropertyLoading(StorageFile file, CustomRichEditBox customRichEditBox)
        {
            string rtfContent = File.ReadAllText(file.Path);
            HandleCustomPropertyLoading(rtfContent, customRichEditBox, 1);
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
                    Telemetry.Disable();
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
            Telemetry.Disable();
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
            Telemetry.Disable();
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
            Telemetry.Disable();
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
            Telemetry.Disable();
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
            Telemetry.Disable();
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
            Telemetry.Disable();
            TabView me = (TabView)sender;
            Telemetry.Transmit(me.Name, "e.PreviousSize=", e.PreviousSize, "e.NewSize=", e.NewSize);
            string pos = Type_1_GetVirtualRegistry<string>("ideOps.OutputPanelPosition") ?? "Bottom";
            Type_1_UpdateVirtualRegistry<string>("OutputPanelTabView_Settings", string.Join("|", [pos, e.NewSize.Height, e.NewSize.Width]));
            //vHW.Text = $"OutputPanelTabView: {e.NewSize.Height}/{e.NewSize.Width}";
        }
        private void Page_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            Telemetry.Disable();
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
            Telemetry.Disable();
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

        private void ContentControl_CodePath_RightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            // prompt for code directory
            // if ok
            //  update current tag's code directory

            var temp = FileFolderPicking.GetFolder(Type_3_GetInFocusTab<string>("ideOps.CodeFolder"));
            if (temp[0] == "OK")
            {
                Type_3_UpdateInFocusTabSettings<string>("ideOps.CodeFolder", true, temp[1]);
                UpdateStatusBar();
            }
        }

        private async void Format_TextColour_Click(object sender, RoutedEventArgs e)
        {
            Telemetry.Disable();
            ColorPicker cp = new()
            {
                Color = new Windows.UI.Color() { A = 255, R = 255, G = 255, B = 255 },
                ColorSpectrumShape = ColorSpectrumShape.Ring,
                IsColorPreviewVisible = true,
                IsColorChannelTextInputVisible = false,
                IsHexInputVisible = false,
            };

            GridLength OneSevenFive = new(175);

            Grid g = new() { Name = "Griddle", Width = 500, Height = 50 };

            RowDefinitionCollection rd = g.RowDefinitions;
            rd.Add(new RowDefinition());

            ColumnDefinitionCollection cd = g.ColumnDefinitions;
            cd.Add(new ColumnDefinition() { Width = OneSevenFive });
            cd.Add(new ColumnDefinition() { Width = OneSevenFive });

            StackPanel sp = new() { Name = "Panelled", Width = 350 };
            sp.Children.Add(cp);
            sp.Children.Add(g);

            ContentDialog dialog = new()
            {
                XamlRoot = this.XamlRoot,
                Style = Application.Current.Resources["DefaultContentDialogStyle"] as Style, // DefaultContentDialogStyle
                Title = "Select Colour",
                Content = sp,
                PrimaryButtonText = "Select",
                CloseButtonText = "Cancel"
            };

            var result = await dialog.ShowAsync();
            var selectedColor = cp.Color;

            CustomRichEditBox currentRichEditBox = _richEditBoxes[((CustomTabItem)tabControl.SelectedItem).Tag];
            currentRichEditBox.Document.Selection.CharacterFormat.ForegroundColor = selectedColor;
            currentRichEditBox.Document.Selection.SelectOrDefault(x => x);

            Telemetry.Disable();
        }

        private void Button_PreviousPanel_Click(object sender, RoutedEventArgs e)
        {
            Telemetry.Disable();
            var me = (Button)sender;

            Telemetry.Transmit("me=", me.Name);
            if (AnInFocusTabExists())
            {
                var tab = InFocusTab();
                var rex = new Regex(@"Tab(\d+)", RegexOptions.IgnoreCase);
                var match = rex.Match((string)tab.Tag);
                if (match.Success)
                {
                    var sdx = match.Groups[1].Value;
                    var idx = int.Parse(sdx) - 1;
                    Telemetry.Transmit("sdx=", sdx, "idx=", idx);

                    idx--;
                    if (idx < 0) idx = tabControl.MenuItems.Count - 1; // 0;
                    var newTag = $"Tab{idx}";
                    tabControl.SelectedItem = (CustomTabItem)tabControl.MenuItems[idx];
                    Telemetry.Transmit("SelectedItem.Tag=", ((CustomTabItem)tabControl.SelectedItem).Tag);
                }
            }
        }

        private void Button_NextPanel_Click(object sender, RoutedEventArgs e)
        {
            Telemetry.Enable();
            var me = (Button)sender;
            Telemetry.Transmit("me=", me.Name);
            if (AnInFocusTabExists())
            {
                var tab = InFocusTab();
                var rex = new Regex(@"Tab(\d+)", RegexOptions.IgnoreCase);
                var match = rex.Match((string)tab.Tag);
                if (match.Success)
                {
                    var sdx = match.Groups[1].Value;
                    var idx = int.Parse(sdx) - 1;
                    Telemetry.Transmit("sdx=", sdx, "idx=", idx);

                    idx++;
                    if (idx > tabControl.MenuItems.Count - 1)
                        idx = 0; // tabControl.MenuItems.Count - 1;
                    tabControl.SelectedItem = (CustomTabItem)tabControl.MenuItems[idx];
                    Telemetry.Transmit("SelectedItem.Tag=", ((CustomTabItem)tabControl.SelectedItem).Tag);
                }
            }
        }

        private async void Search_Find_Click(object sender, RoutedEventArgs e)
        {
            Telemetry.Disable();
            Grid g = new();

            GridLength gl = new(45);
            GridLength gh = new(30);

            RowDefinitionCollection rdc = g.RowDefinitions;
            rdc.Add(new RowDefinition() { Height = gh });
            rdc.Add(new RowDefinition() { Height = gh });
            rdc.Add(new RowDefinition() { Height = new GridLength(120) });
            rdc.Add(new RowDefinition() { Height = gh });
            rdc.Add(new RowDefinition() { });

            ColumnDefinitionCollection cdc = g.ColumnDefinitions;
            cdc.Add(new ColumnDefinition() { Width = gl });
            cdc.Add(new ColumnDefinition() { Width = gl });
            cdc.Add(new ColumnDefinition() { Width = gl });
            cdc.Add(new ColumnDefinition() { Width = gl });
            cdc.Add(new ColumnDefinition() { });

            TextBlock t = new()
            {
                TextAlignment = TextAlignment.Left,
                Text = "Find what?"
            };

            RichEditBox richEditBox = new RichEditBox()
            {
                TextAlignment = TextAlignment.DetectFromContent,
                FontSize = 12,
                PlaceholderText = "Find ...",
                Name = "findWhat",

            };

            Grid.SetRow(t, 0);
            Grid.SetColumn(t, 0);
            g.Children.Add(t);

            Grid.SetRow(richEditBox, 0);
            Grid.SetColumn(richEditBox, 1);
            Grid.SetColumnSpan(richEditBox, 4);
            g.Children.Add(richEditBox);

            DefineScopeElements(out RadioButton scopeAll,
                                out RadioButton scopeSelection,
                                out StackPanel searchAllSelectionSection,
                                out Microsoft.UI.Xaml.Controls.CheckBox wholeWordMatch,
                                out Microsoft.UI.Xaml.Controls.CheckBox caseMatch,
                                out Microsoft.UI.Xaml.Controls.CheckBox wrapAround,
                                out StackPanel wholeCaseWrapSection);

            Grid.SetRow(searchAllSelectionSection, 2);
            Grid.SetColumn(searchAllSelectionSection, 0);
            Grid.SetColumnSpan(searchAllSelectionSection, 3);
            g.Children.Add(searchAllSelectionSection);

            Grid.SetRow(wholeCaseWrapSection, 2);
            Grid.SetColumn(wholeCaseWrapSection, 4);
            Grid.SetColumnSpan(wholeCaseWrapSection, 5);
            g.Children.Add(wholeCaseWrapSection);


            StackPanel dialogStackPanel = new();
            dialogStackPanel.Children.Add(g);

            string? findText = Type_3_GetInFocusTab<string>("ideOps.findText");
            bool findWhole = Type_3_GetInFocusTab<bool>("ideOps.findWhole");
            bool findCase = Type_3_GetInFocusTab<bool>("ideOps.findCase");
            bool findSelection = Type_3_GetInFocusTab<bool>("ideOps.findSelection");
            bool findWrapped = Type_3_GetInFocusTab<bool>("ideOps.findWrapped");

            if (!string.IsNullOrEmpty(findText))
            {
                richEditBox.Document.SetText(TextSetOptions.None, findText);
            }

            wrapAround.IsChecked = findWrapped;
            wholeWordMatch.IsChecked = findWhole;
            caseMatch.IsChecked = findCase;
            if (findSelection)
            {
                scopeSelection.IsChecked = true;
            }
            else
            {
                scopeAll.IsChecked = true;
            }

            ContentDialog dialog = new()
            {
                XamlRoot = this.XamlRoot,
                Style = Application.Current.Resources["DefaultContentDialogStyle"] as Style,
                Title = "Find Text",
                Content = dialogStackPanel,
                PrimaryButtonText = "Find",
                CloseButtonText = "Cancel",
            };
            // dialog.PrimaryButtonClick += Search_Find_Dialog_Click;
            ContentDialogResult result = await dialog.ShowAsync();

            richEditBox.Document.GetText(TextGetOptions.AdjustCrlf, out var whatToFind);
            Telemetry.Transmit("whatToFind=", whatToFind);
            Telemetry.Transmit("scopeAll.IsChecked=", scopeAll.IsChecked, "scopeSelection.IsChecked=", scopeSelection.IsChecked);
            Telemetry.Transmit("wholeWordMatch.IsChecked=", wholeWordMatch.IsChecked, "caseMatch.IsChecked=", caseMatch.IsChecked);

            Type_3_UpdateInFocusTabSettings("ideOps.findText", true, whatToFind);
            Type_3_UpdateInFocusTabSettings("ideOps.findWhole", true, wholeWordMatch.IsChecked);
            Type_3_UpdateInFocusTabSettings("ideOps.findCase", true, caseMatch.IsChecked);
            Type_3_UpdateInFocusTabSettings("ideOps.findSelection", true, scopeSelection.IsChecked);
            Type_3_UpdateInFocusTabSettings("ideOps.findWrapped", true, wrapAround.IsChecked);

            string findString = (bool)wholeWordMatch.IsChecked! ? $@"\b{Regex.Escape(whatToFind)}\b" : $@"{Regex.Escape(whatToFind)}";
            RegexOptions findOptions = (bool)caseMatch.IsChecked! ? RegexOptions.Singleline : RegexOptions.IgnoreCase | RegexOptions.Singleline;

            CustomRichEditBox currentRichEditBox = _richEditBoxes[((CustomTabItem)tabControl.SelectedItem).Tag];
            //currentRichEditBox.Document.GetText(TextGetOptions.None, out string currentRichEditText);

            //if ((bool)scopeSelection.IsChecked)
            //{
            //    if (currentRichEditBox.Document.Selection.Length == 0)
            //    {
            //        await SomethingNotFoundDialog($"'{whatToFind}' not found.");
            //        return;
            //        // error
            //    }
            //    // find in selection
            //    ITextSelection sel = currentRichEditBox.Document.Selection;
            //    int sstart = sel.StartPosition;
            //    int sstop = sel.EndPosition;
            //    sel.ScrollIntoView(PointOptions.None);
            //    sel.GetText(TextGetOptions.None, out string stext);
            //    var smatch = Regex.Match(stext, findString, findOptions);
            //    if (smatch.Success)
            //    {
            //        Telemetry.Transmit("start=", sstart, "stop=", sstop, "match.Index=", smatch.Index);
            //        int there = smatch.Index + sstart;
            //        currentRichEditBox.Document.Selection.StartPosition = there;
            //        currentRichEditBox.Document.Selection.EndPosition = there + findText.Length;
            //        sel.ScrollIntoView(PointOptions.None);
            //        Type_3_UpdateInFocusTabSettings("ideOps.findLastFound", true, there);
            //    }
            //    else
            //    {
            //        await SomethingNotFoundDialog($"'{whatToFind}' not found in selection.");
            //    }
            //    return;
            //}
            // find in body 
            currentRichEditBox.Document.GetText(TextGetOptions.None, out string text);
            int start = 0;
            int stop = text.Length;
            Match match = Regex.Match(text, findString, findOptions);
            if (match.Success)
            {
                Telemetry.Transmit("start=", start, "stop=", stop, "match.Index=", match.Index);
                var there = match.Index + start;
                currentRichEditBox.Document.Selection.StartPosition = there;
                currentRichEditBox.Document.Selection.EndPosition = there + whatToFind.Length;
                currentRichEditBox.Document.Selection.ScrollIntoView(PointOptions.None);
                Type_3_UpdateInFocusTabSettings<long>("ideOps.findLastFound", true, there + whatToFind.Length);
            }
            else
            {
                await SomethingNotFoundDialog($"'{whatToFind}' not found.");
            }

            static void DefineScopeElements(out RadioButton scopeAll,
                                            out RadioButton scopeSelection,
                                            out StackPanel searchAllSelectionSection,
                                            out Microsoft.UI.Xaml.Controls.CheckBox wholeWordMatch,
                                            out Microsoft.UI.Xaml.Controls.CheckBox caseMatch,
                                            out Microsoft.UI.Xaml.Controls.CheckBox wrapAround,
                                            out StackPanel wholeCaseWrapSection)
            {
                TextBlock scopeLabel = new()
                {
                    Text = "Scope",
                    TextAlignment = TextAlignment.Center,
                };

                scopeAll = new()
                {
                    Name = "rbAll",
                    GroupName = "Search",
                    Content = "All",
                    IsChecked = true,
                    IsEnabled = true,

                };
                scopeSelection = new()
                {
                    Name = "rbSelection",
                    GroupName = "Search",
                    Content = "Selection",
                    IsChecked = false,
                    IsEnabled = false,
                };
                searchAllSelectionSection = new()
                {
                    BorderBrush = new SolidColorBrush(Colors.Blue),
                    CornerRadius = new CornerRadius(2),
                    BorderThickness = new Thickness(2)
                };
                searchAllSelectionSection.Children.Add(scopeLabel);
                searchAllSelectionSection.Children.Add(scopeAll);
                searchAllSelectionSection.Children.Add(scopeSelection);

                wholeWordMatch = new()
                {
                    Content = "Find whole words"
                };
                caseMatch = new()
                {
                    Content = "Match case"
                };
                wrapAround = new()
                {
                    Content = "Wrap around"
                };

                wholeCaseWrapSection = new();
                wholeCaseWrapSection.Children.Add(wholeWordMatch);
                wholeCaseWrapSection.Children.Add(caseMatch);
                wholeCaseWrapSection.Children.Add(wrapAround);
            }
        }

        private void Search_Replace_Click(object sender, RoutedEventArgs e)
        {

        }

        private async void Search_FindNext_Click(object sender, RoutedEventArgs e)
        {
            long findLastFound = Type_3_GetInFocusTab<long>("ideOps.findLastFound");
            string? findText = Type_3_GetInFocusTab<string>("ideOps.findText");
            bool findWhole = Type_3_GetInFocusTab<bool>("ideOps.findWhole");
            bool findCase = Type_3_GetInFocusTab<bool>("ideOps.findCase");
            bool findWrapped = Type_3_GetInFocusTab<bool>("ideOps.findWrapped");

            //bool findSelection = Type_3_GetInFocusTab<bool>("ideOps.findSelection");

            string findString = findWhole ? $@"\b{Regex.Escape(findText!)}\b" : $@"{Regex.Escape(findText!)}";
            RegexOptions findOptions = findCase! ? RegexOptions.Singleline : RegexOptions.IgnoreCase | RegexOptions.Singleline;

            CustomRichEditBox currentRichEditBox = _richEditBoxes[((CustomTabItem)tabControl.SelectedItem).Tag];

            // if findLastFound is -1 then the find failed. There's no point trying again.
            if (findLastFound == -1)
            {
                await SomethingNotFoundDialog($"No more instances of '{findText}' found.");
                return;
            }

            // if findLastFound != -1 then find found something and the user wants to find something more
            // if this next search doesn't find it,
            //  if findWrapped is false
            //   error
            //  else
            //   we set findLastFound to zero and find what we first found

            while (true)
            {
                currentRichEditBox.Document.GetText(TextGetOptions.None, out string text);
                int start = (int)findLastFound;
                text = text.Substring(start);
                var match = Regex.Match(text, findString, findOptions);
                if (match.Success)
                {
                    var there = match.Index + start;
                    currentRichEditBox.Document.Selection.StartPosition = there;
                    currentRichEditBox.Document.Selection.EndPosition = there + findText.Length;
                    currentRichEditBox.Document.Selection.ScrollIntoView(PointOptions.None);
                    Type_3_UpdateInFocusTabSettings<long>("ideOps.findLastFound", true, there + findText.Length);
                    break;
                }
                else
                {
                    if (findWrapped)
                    {
                        findLastFound = 0;
                        continue;
                    }
                    Type_3_UpdateInFocusTabSettings<long>("ideOps.findLastFound", true, -1);
                    await SomethingNotFoundDialog($"No more instances of '{findText}' found.");
                    return;
                }
            }
        }

        private async void ShowTabs_Click(object sender, RoutedEventArgs e)
        {
            Telemetry.Disable();

            List<string> lines = ["Tabs"];

            var ift = InFocusTab();

            int i = 0;
            foreach (CustomTabItem tab in tabControl.MenuItems)
            {
                lines.Add($"{i++,3}: Tag={tab.Tag,-10} Name={tab.Name,-10} Content={tab.Content,-20} InFocus={(ift.Tag.ToString() == tab.Tag.ToString() ? "True" : "False")}");
            }

            ContentDialog dialog = new()
            {
                XamlRoot = this.XamlRoot,
                Style = Application.Current.Resources["DefaultContentDialogStyle"] as Style, // DefaultContentDialogStyle
                Title = "Show Tabs",
                Content = lines.JoinBy("\n"),
                PrimaryButtonText = "OK",
                DefaultButton = ContentDialogButton.Primary,
                CanBeScrollAnchor = true,


            };
            _ = await dialog.ShowAsync();

        }
    }
}
