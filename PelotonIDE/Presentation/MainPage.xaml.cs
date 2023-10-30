using Microsoft.UI;
using Microsoft.UI.Input;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Input;

using Newtonsoft.Json;

using System.Diagnostics;
using System.Text;

using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Storage.Provider;
using Windows.System;
using Windows.UI;
using Windows.UI.Core;

using InterpreterParameterStructure = System.Collections.Generic.Dictionary<string, object>;
using LanguageConfigurationStructure = System.Collections.Generic.Dictionary<string, System.Collections.Generic.Dictionary<string, System.Collections.Generic.Dictionary<string, string>>>;
using InterpreterParametersStructure = System.Collections.Generic.Dictionary<string, System.Collections.Generic.Dictionary<string, object>>;
using FactorySettingsStructure = System.Collections.Generic.Dictionary<string, object>;

using System.Text.RegularExpressions;
using Windows.Storage.Streams;
using Windows.Foundation;
using System.Timers;
using Newtonsoft.Json.Linq;


namespace PelotonIDE.Presentation
{
    public sealed partial class MainPage : Page
    {
        readonly Dictionary<object, CustomRichEditBox> _richEditBoxes = new();
        bool outputPanelShowing = true;
        enum OutputPanelPosition
        {
            Left,
            Bottom,
            Right
        }
        string? InterfaceLanguageName = "English";
        long InterfaceLanguageID = 0;
        
        string? LastSelectedInterpreterLanguageName;
        long LastSelectedInterpreterLanguageID;

        long LastSelectedVariableLength = 0;
        long LastSelectedSpaced = 0;


        OutputPanelPosition outputPanelPosition = OutputPanelPosition.Bottom;
        string pelotonEXE = string.Empty;
        //string pelotonARG = string.Empty;

        InterpreterParametersStructure? GlobalInterpreterParameters = new();
        InterpreterParametersStructure? PerTabInterpreterParameters = new();
        LanguageConfigurationStructure? LanguageSettings = new();

        public MainPage()
        {

            this.InitializeComponent();

            //System.Timers.Timer t = new(1000)
            //{
            //    AutoReset = true,
            //    Enabled = true,

            //    //Elapsed += new ElapsedEventHandler(OnTimedEvent_Tick)
            //};
            //t.Elapsed += TimerTick;
            //t.Start();

            CAPS.Foreground = Console.CapsLock ? new SolidColorBrush(Colors.Black) : new SolidColorBrush(Colors.LightGray);
            NUM.Foreground = Console.NumberLock ? new SolidColorBrush(Colors.Black) : new SolidColorBrush(Colors.LightGray);


            // GetGlobals();
            CustomRichEditBox customREBox = new()
            {
                Tag = "Tab1",
                //Background = new SolidColorBrush(new Color() { A = 0xFF, R = 0xf9, G = 0xf8, B = 0xbd }),
                //Foreground = new SolidColorBrush(new Color() { A = 0xFF, R = 0xf9, G = 0xf8, B = 0xbd })
            };
            customREBox.KeyDown += RichEditBox_KeyDown;
            customREBox.AcceptsReturn = true;

            // richEditBox.Background = 
            tabControl.Content = customREBox;
            _richEditBoxes[customREBox.Tag] = customREBox;
            tab1.TabSettingsDict = null;
            tabControl.SelectedItem = tab1;
            App._window.Closed += MainWindow_Closed;

            FillLanguagesIntoMenu(mnuSettings, "mnuSelectLanguage", Internationalization_Click);
            FillLanguagesIntoMenu(mnuRun, "mnuLanguage", MnuLanguage_Click);

            UpdateTabCommandLine();
        }

        private InterpreterParametersStructure CopyFromGlobalCodeRunCargo()
        {
            InterpreterParametersStructure tsj = new();
            foreach (var key in GlobalInterpreterParameters.Keys)
            {
                var kvp = GlobalInterpreterParameters[key];
                tsj.Add(key, kvp);
            }
            return tsj;
        }

        private Dictionary<string, object> GetTabSettingsFromRegistry()
        {
            ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
            Dictionary<string, object> dict = new();
            foreach (var value in localSettings.Values)
            {
                if (value.Key.StartsWith("tab_"))
                    dict.Add(value.Key, value.Value);
            }
            return dict;
        }

        public static async Task<InterpreterParametersStructure?> GetGlobalInterpreterParameters()
        {
            var tabSettingStorage = await StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///PelotonIDE\\Presentation\\GlobalInterpreterParameters.json"));
            string tabSettings = File.ReadAllText(tabSettingStorage.Path);
            return JsonConvert.DeserializeObject<InterpreterParametersStructure>(tabSettings);
        }

        public static async Task<InterpreterParametersStructure?> GetPerTabInterpreterParameters()
        {
            var tabSettingStorage = await StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///PelotonIDE\\Presentation\\PerTabInterpreterParameters.json"));
            string tabSettings = File.ReadAllText(tabSettingStorage.Path);
            return JsonConvert.DeserializeObject<InterpreterParametersStructure>(tabSettings);
        }

        private static async Task<FactorySettingsStructure?> GetFactorySettings()
        {
            var globalSettings = await StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///PelotonIDE\\Presentation\\FactorySettings.json"));
            string globalSettingsString = File.ReadAllText(globalSettings.Path);
            return JsonConvert.DeserializeObject<FactorySettingsStructure>(globalSettingsString);
        }

        private static async Task<LanguageConfigurationStructure?> GetLanguageConfiguration()
        {
            var languageConfig = await StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///PelotonIDE\\Presentation\\LanguageConfiguration.json"));
            string languageConfigString = File.ReadAllText(languageConfig.Path);
            return JsonConvert.DeserializeObject<LanguageConfigurationStructure>(languageConfigString);
        }
        private async void FillLanguagesIntoMenu(MenuBarItem menuBarItem, string menuLabel, RoutedEventHandler routedEventHandler)
        {
            var tabset = await GetGlobalInterpreterParameters();

            LanguageSettings = await GetLanguageConfiguration();

            if (InterfaceLanguageName == null || !LanguageSettings.ContainsKey(InterfaceLanguageName))
            {
                return;
            }

            var sub = new MenuFlyoutSubItem
            {
                // <!--<MenuFlyoutSubItem Text="Choose interface language" BorderBrush="LightGray" BorderThickness="1" names:Name="SettingsBar_InterfaceLanguage" />-->
                Text = LanguageSettings[InterfaceLanguageName]["frmMain"][menuLabel],
                BorderThickness = new Thickness(1, 1, 1, 1),
                BorderBrush = new SolidColorBrush() { Color = Colors.LightGray },
                Name = menuLabel
            };


            //var items = new List<MenuFlyoutItem>();

            // what is current language?
            var globals = LanguageSettings[InterfaceLanguageName]["GLOBAL"];
            var count = LanguageSettings.Keys.Count;
            for (var i = 0; i < count; i++)
            {
                var names = from lang in LanguageSettings.Keys
                        where LanguageSettings.ContainsKey(lang) && LanguageSettings[lang]["GLOBAL"]["ID"] == i.ToString()
                        let name = LanguageSettings[lang]["GLOBAL"]["Name"]
                        select name;
                if (names.Any())
                {
                    MenuFlyoutItem menuFlyoutItem = new()
                    {
                        Text = globals[$"{100 + i + 1}"],
                        Name = names.First()  //languageJson[key]["GLOBAL"]["ID"]
                    };
                    menuFlyoutItem.Click += routedEventHandler; //  Internationalization_Click;
                    sub.Items.Add(menuFlyoutItem);
                }
            }
            menuBarItem.Items.Add(sub);
        }

        /// <summary>
        /// Load previous editor settings
        /// </summary>
        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;

            GlobalInterpreterParameters = await MainPage.GetGlobalInterpreterParameters();
            PerTabInterpreterParameters = await MainPage.GetPerTabInterpreterParameters();
            
            var FactorySettings = await GetFactorySettings();

            outputPanelShowing = GetFactorySettingsWithLocalSettingsOverrideOrDefault<bool>("OutputPanelShowing", true, FactorySettings, localSettings);

            var outputPanelPosition = GetFactorySettingsWithLocalSettingsOverrideOrDefault("OutputPanelPosition", (OutputPanelPosition)Enum.Parse(typeof(OutputPanelPosition), "Bottom"), FactorySettings, localSettings);

            HandleOutputPanelChange(outputPanelPosition);
            
            outputPanel.Height = GetFactorySettingsWithLocalSettingsOverrideOrDefault<double>("OutputPanelHeight",200, FactorySettings, localSettings);
            InterfaceLanguageName = GetFactorySettingsWithLocalSettingsOverrideOrDefault<string>("InterfaceLanguageName", "English", FactorySettings, localSettings);
            InterfaceLanguageID = GetFactorySettingsWithLocalSettingsOverrideOrDefault<long>("InterfaceLanguageID",0, FactorySettings, localSettings);

            pelotonEXE = GetFactorySettingsWithLocalSettingsOverrideOrDefault("PelotonEXE", @"C:\protium\bin\pdb.exe", FactorySettings, localSettings) ?? @"C:\protium\bin\pdb.exe";

            LastSelectedInterpreterLanguageName = GetFactorySettingsWithLocalSettingsOverrideOrDefault<string>("LastSelectedInterpreterLanguageName", "English", FactorySettings, localSettings);
            LastSelectedInterpreterLanguageID = GetFactorySettingsWithLocalSettingsOverrideOrDefault<long>("LastSelectedInterpreterLanguageID", 0, FactorySettings, localSettings);

            LastSelectedVariableLength = GetFactorySettingsWithLocalSettingsOverrideOrDefault<long>("LastSelectedVariableLength", 0, FactorySettings, localSettings);
            LastSelectedSpaced = GetFactorySettingsWithLocalSettingsOverrideOrDefault<long>("LastSelectedSpaced", 0, FactorySettings, localSettings);

            if (tab1.TabSettingsDict == null)
                tab1.TabSettingsDict = Clone(PerTabInterpreterParameters);

            tab1.TabSettingsDict["Language"]["Defined"] = true;
            tab1.TabSettingsDict["Language"]["Value"] = LastSelectedInterpreterLanguageID;

            UpdateMenuRunningMode(GlobalInterpreterParameters["Quietude"]);
            UpdateVariableLengthMode(tab1.TabSettingsDict["VariableLength"]);
            UpdateSpacedMode(tab1.TabSettingsDict["Spaced"]);
            UpdateLanguageName(tab1.TabSettingsDict);
            UpdateTabCommandLine();

        }

        private T? GetFactorySettingsWithLocalSettingsOverrideOrDefault<T>(string name, T value, FactorySettingsStructure? factory, ApplicationDataContainer? container)
        {
            T? result = default;
            bool noFactory = false;
            bool noContainer = false;
            if (factory.ContainsKey(name))
            {
                result = (T)factory[name];
            }
            else
            {
                noFactory = true;
            }
            if (container.Values.ContainsKey(name))
            {
                result = (T)container.Values[name];
            }
            else
            {
                noContainer = true;
            }
            if (noFactory && noContainer)
            {
                result = value;
            }
            return result;
        }

        private int GetFactorySettingsWithLocalSettingsOverrideOrDefault(string name, int value, FactorySettingsStructure? factory, ApplicationDataContainer? container)
        {
            int result = default;
            bool noFactory = false;
            bool noContainer = false;
            if (factory.ContainsKey(name))
            {
                result = (int)factory[name];
            } else
            {
                noFactory = true;
            }
            if (container.Values.ContainsKey(name))
            {
                result = (int)container.Values[name];
            } else
            {
                noContainer = true;
            }
            if (noFactory && noContainer)
            {
                result = value;
            }
            return result;
        }
        private long GetFactorySettingsWithLocalSettingsOverrideOrDefault(string name, long value, FactorySettingsStructure? factory, ApplicationDataContainer? container)
        {
            long result = default;
            bool noFactory = false;
            bool noContainer = false;
            if (factory.ContainsKey(name))
            {
                result = (long)factory[name];
            }
            else
            {
                noFactory = true;
            }
            if (container.Values.ContainsKey(name))
            {
                result = (long)container.Values[name];
            }
            else
            {
                noContainer = true;
            }
            if (noFactory && noContainer)
            {
                result = value;
            }
            return result;
        }
        private bool GetFactorySettingsWithLocalSettingsOverrideOrDefault(string name, bool value, FactorySettingsStructure? factory, ApplicationDataContainer? container)
        {
            bool result = default;
            bool noFactory = false;
            bool noContainer = false;
            if (factory.ContainsKey(name))
            {
                result = (bool)factory[name];
            }
            else
            {
                noFactory = true;
            }
            if (container.Values.ContainsKey(name))
            {
                result = (bool)container.Values[name];
            }
            else
            {
                noContainer = true;
            }
            if (noFactory && noContainer)
            {
                result = value;
            }
            return result;
        }

        private OutputPanelPosition GetFactorySettingsWithLocalSettingsOverrideOrDefault(string name, OutputPanelPosition value, FactorySettingsStructure? factory, ApplicationDataContainer? container)
        {
            OutputPanelPosition result = default;
            bool noFactory = false;
            bool noContainer = false;
            if (factory.ContainsKey(name))
            {
                result = (OutputPanelPosition)Enum.Parse(typeof(OutputPanelPosition), (string)factory[name]);
            }
            else
            {
                noFactory = true;
            }
            if (container.Values.ContainsKey(name))
            {
                result = (OutputPanelPosition)Enum.Parse(typeof(OutputPanelPosition), (string)container.Values[name]);
            }
            else
            {
                noContainer = true;
            }
            if (noFactory && noContainer)
            {
                result = value;
            }
            return result;
        }

        private string? GetFactorySettingsWithLocalSettingsOverrideOrDefault(string name, string value, FactorySettingsStructure? factory, ApplicationDataContainer? container)
        {
            string? result = default;
            bool noFactory = false;
            bool noContainer = false;
            if (factory.ContainsKey(name))
            {
                result = (string)factory[name];
            }
            else
            {
                noFactory = true;
            }
            if (container.Values.ContainsKey(name))
            {
                result = (string)container.Values[name];
            }
            else
            {
                noContainer = true;
            }
            if (noFactory && noContainer)
            {
                result = value;
            }
            return result;
        }

        private void UpdateVariableLengthMode(InterpreterParameterStructure variableLength)
        {
            FontIcon tickIcon = new FontIcon()
            {
                FontFamily = new FontFamily("Segoe MDL2 Assets"),
                Glyph = "\uF0B7"
            };

            mnuVariableLength.Icon = ((bool)variableLength["Defined"]) ? tickIcon : null;
        }
        private void UpdateSpacedMode(InterpreterParameterStructure spaced)
        {
            FontIcon tickIcon = new FontIcon()
            {
                FontFamily = new FontFamily("Segoe MDL2 Assets"),
                Glyph = "\uF0B7"
            };

            mnuSpaced.Icon = ((bool)spaced["Defined"]) ? tickIcon : null;
        }
        private void UpdateMenuRunningMode(InterpreterParameterStructure quietude)
        {
            FontIcon tickIcon = new FontIcon()
            {
                FontFamily = new FontFamily("Segoe MDL2 Assets"),
                Glyph = "\uF0B7"
            };

            if ((bool)quietude["Defined"] == true)
            {
                foreach (var item in from key in new string[] { "mnuQuiet", "mnuVerbose", "mnuVerbosePauseOnExit" }
                                     let items = from item in mnuRunningMode.Items where item.Name == key select item
                                     from item in items
                                     select item)
                {
                    (item as MenuFlyoutItem).Icon = null;
                }

                switch ((long)quietude["Value"])
                {
                    case 0:
                        mnuQuiet.Icon = tickIcon;
                        break;
                    case 1:
                        mnuVerbose.Icon = tickIcon;
                        break;
                    case 2:
                        mnuVerbosePauseOnExit.Icon = tickIcon;
                        break;
                }
            }
        }

        /// <summary>
        /// Save current editor settings
        /// </summary>
        private void MainWindow_Closed(object sender, object e) //FIXME How do I save to JSON?
        {
            ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
            localSettings.Values["OutputPanelPosition"] = outputPanelPosition.ToString();
            localSettings.Values["OutputHeight"] = outputPanel.Height;
            localSettings.Values["OutputWidth"] = outputPanel.Width;
            localSettings.Values["InterfaceLanguageName"] = InterfaceLanguageName;
            localSettings.Values["InterfaceLanguageID"] = InterfaceLanguageID;
            localSettings.Values["LastSelectedInterpreterLanguageName"] = LastSelectedInterpreterLanguageName;
            localSettings.Values["LastSelectedInterpreterLanguageID"] = LastSelectedInterpreterLanguageID;
            localSettings.Values["LastSelectedVariableLength"] = LastSelectedVariableLength;
            localSettings.Values["LastSelectedSpaced"] = LastSelectedSpaced;
            localSettings.Values["PelotonEXE"] = pelotonEXE;
        }

        #region Event Handlers


        #region Script Runner

        private void CreateNewRichEditBox()
        {
            CustomRichEditBox richEditBox = new()
            {
                isDirty = false,
            };
            richEditBox.KeyDown += RichEditBox_KeyDown;
            richEditBox.AcceptsReturn = true;

            CustomTabItem navigationViewItem = new()
            {
                Content = "Tab " + (tabControl.MenuItems.Count + 1),
                Tag = "Tab" + (tabControl.MenuItems.Count + 1),
                IsNewFile = true,
                TabSettingsDict = Clone(PerTabInterpreterParameters),
                Height = 30
            };
            richEditBox.Tag = navigationViewItem.Tag;
            tabControl.Content = richEditBox;
            _richEditBoxes[richEditBox.Tag] = richEditBox;
            tabControl.MenuItems.Add(navigationViewItem);
            tabControl.SelectedItem = navigationViewItem; // in focus?
            richEditBox.Focus(FocusState.Keyboard);
            //navigationViewItem.tabSettingJson["Language"]["Defined"] = true;
            //navigationViewItem.tabSettingJson["Language"]["Value"] = InterfaceLanguageID;
            //languageName.Text = LanguageSettings[InterfaceLanguageName]["GLOBAL"][$"{101 + InterfaceLanguageID}"];
            UpdateLanguageName(navigationViewItem.TabSettingsDict);
            UpdateTabCommandLine();
        }

        private InterpreterParametersStructure Clone(InterpreterParametersStructure? perTabInterpreterParameters)
        {
            var clone = new InterpreterParametersStructure();
            foreach (var okey in perTabInterpreterParameters.Keys)
            {
                var inner = new Dictionary<string, object>();
                foreach (var ikey in perTabInterpreterParameters[okey].Keys)
                {
                    inner[ikey] = perTabInterpreterParameters[okey][ikey];
                }
                clone[okey] = inner;
            }
            return clone;
        }

        public string GetTabsLanguageName(InterpreterParametersStructure? tabSettingJson)
        {
            var langValue = InterfaceLanguageID;
            var langName = string.Empty;
            // select from LanguageSettings the record where GLOBAL.ID matches langValue
            var languages = from lang in LanguageSettings.Keys
                            where long.Parse(LanguageSettings[lang]["GLOBAL"]["ID"]) == langValue
                            select LanguageSettings[lang];
            if (languages.Count() > 0)
            {
                var first = languages.First();
                var value = tabSettingJson["Language"]["Value"];
                var type = value.GetType().Name;
                long i = 0;
                if (type == "Int32")
                {
                    i = (int)value;
                }

                if (type =="Int64")
                {
                    i = (long)value;
                }

                var nameName = $"{101 + i}";
                langName = first["GLOBAL"][nameName];
            }
            return langName;
        }

        private void UpdateLanguageName(InterpreterParametersStructure? tabSettingJson)
        {
            languageName.Text = GetTabsLanguageName(tabSettingJson);
        }

        #endregion

        public static string Reverse(string s)
        {
            char[] charArray = s.ToCharArray();
            Array.Reverse(charArray);
            return new string(charArray);
        }

        public static void HandleCustomPropertySaving(StorageFile file, CustomRichEditBox customRichEditBox, CustomTabItem navigationViewItem)
        { 

            string rtfContent = File.ReadAllText(file.Path);
            StringBuilder rtfBuilder = new(rtfContent);

            var ques = new Regex(Regex.Escape("?"));
            string info = @"\info {\*\ilang ?} {\*\ilength ?} {\*\ipadout ?}} ";
            info = ques.Replace(info, $"{navigationViewItem.TabSettingsDict["Language"]["Value"]}", 1);
            info = ques.Replace(info, (bool)navigationViewItem.TabSettingsDict["VariableLength"]["Value"] ? "1" : "0", 1);
            info = ques.Replace(info, (bool)navigationViewItem.TabSettingsDict["Spaced"]["Value"] ? "1" : "0", 1);

            var regex = new Regex(@"\{\*?\\[^{}]+}|[{}]|\\\n?[A-Za-z]+\n?(?:-?\d+)?[ ]?", RegexOptions.IgnoreCase | RegexOptions.Compiled);

            var matches = regex.Matches(rtfContent);

            var infos = from match in matches where match.Value == @"\info" select match;

            if (infos.Any())
            {
                var fullBlock = rtfContent.Substring(infos.First().Index, infos.First().Length);
                var blockMatches = regex.Matches(fullBlock);

            }
            else
            {
                var start = @"{\rtf1";
                var i = rtfContent.IndexOf(start);
                var j = i + start.Length;
                rtfBuilder.Insert(j, info);
            }
            File.WriteAllText(file.Path, rtfBuilder.ToString());
        }

        public static void HandleCustomPropertyLoading(StorageFile file, CustomRichEditBox customRichEditBox, CustomTabItem navigationViewItem)
        {
            string rtfContent = File.ReadAllText(file.Path);
            var regex = new Regex(@"\{\*?\\[^{}]+}|[{}]|\\\n?[A-Za-z]+\n?(?:-?\d+)?[ ]?", RegexOptions.IgnoreCase | RegexOptions.Compiled);

            var matches = regex.Matches(rtfContent);

            var infos = from match in matches where match.Value.StartsWith(@"\info") select match;
            if (infos.Any())
            {
                var ilang = from match in matches where match.Value.Contains(@"\ilang") select match;
                if (ilang.Any())
                {
                    var items = ilang.First().Value.Split(' ');
                    if (items.Any())
                    {
                        navigationViewItem.TabSettingsDict["Language"]["Defined"] = true;
                        navigationViewItem.TabSettingsDict["Language"]["Value"] = long.Parse(items[1].Replace("}", ""));
                    }
                }
                var ilength = from match in matches where match.Value.Contains(@"\ilength") select match;
                if (ilength.Any())
                {
                    var items = ilength.First().Value.Split(' ');
                    if (items.Any())
                    {
                        var flag = items[1].Replace("}", "");
                        if (flag == "0")
                        {
                            navigationViewItem.TabSettingsDict["VariableLength"]["Defined"] = false;
                            navigationViewItem.TabSettingsDict["VariableLength"]["Value"] = flag == "1";
                        }
                        else
                        {
                            navigationViewItem.TabSettingsDict["VariableLength"]["Defined"] = true;
                            navigationViewItem.TabSettingsDict["VariableLength"]["Value"] = flag == "1";
                        }
                    }

                }
                var ipadout = from match in matches where match.Value.Contains(@"\ipadout") select match;
                if (ipadout.Any())
                {
                    var items = ipadout.First().Value.Split(' ');
                    var flag = items[1].Replace("}", "");
                    if (flag == "0")
                    {
                        navigationViewItem.TabSettingsDict["Spaced"]["Defined"] = false;
                        navigationViewItem.TabSettingsDict["Spaced"]["Value"] = flag == "1";
                    }
                    else
                    {
                        navigationViewItem.TabSettingsDict["Spaced"]["Defined"] = true;
                        navigationViewItem.TabSettingsDict["Spaced"]["Value"] = flag == "1";
                    }
                }

            }
            else
            {
                navigationViewItem.TabSettingsDict["Language"]["Defined"] = true;
                navigationViewItem.TabSettingsDict["Language"]["Value"] = 0;
                if (rtfContent.Contains("<# "))
                {
                    navigationViewItem.TabSettingsDict["VariableLength"]["Defined"] = true;
                    navigationViewItem.TabSettingsDict["VariableLength"]["Value"] = rtfContent.Contains("<# ");
                }
            }
        }

        private static void HandlePossibleAmpersand(string name, MenuFlyoutItemBase mfib)
        {
            if (name.Contains('&'))
            {
                string accel = name.Substring(name.IndexOf("&") + 1, 1);
                mfib.KeyboardAccelerators.Add(new KeyboardAccelerator() { Key = Enum.Parse<VirtualKey>(accel.ToUpperInvariant()), Modifiers = VirtualKeyModifiers.Menu });
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

        private static void HandlePossibleAmpersand(string name, MenuBarItem mbi)
        {
            if (name.Contains('&'))
            {
                string accel = name.Substring(name.IndexOf("&") + 1, 1);
                mbi.KeyboardAccelerators.Add(new KeyboardAccelerator() { Key = Enum.Parse<VirtualKey>(accel.ToUpperInvariant()), Modifiers = VirtualKeyModifiers.Menu });
                name = name.Replace("&", "");
            }
            mbi.Title = name;
        }

        private static void HandlePossibleAmpersand(string name, MenuFlyoutItem mfi)
        {
            if (name.Contains('&'))
            {
                string accel = name.Substring(name.IndexOf("&") + 1, 1);
                mfi.KeyboardAccelerators.Add(new KeyboardAccelerator() { Key = Enum.Parse<VirtualKey>(accel.ToUpperInvariant()), Modifiers = VirtualKeyModifiers.Menu });
                name = name.Replace("&", "");
            }
            mfi.Text = name;
        }

        private string BuildTabCommandLine()
        {
            CustomTabItem navigationViewItem = (CustomTabItem)tabControl.SelectedItem;
            CustomRichEditBox currentRichEditBox = _richEditBoxes[navigationViewItem.Tag];

            List<string> paras = new();
            foreach (var key in GlobalInterpreterParameters.Keys)
            {
                if ((bool)GlobalInterpreterParameters[key]["Defined"])
                {
                    var entry = $"/{GlobalInterpreterParameters[key]["Key"]}";
                    var value = GlobalInterpreterParameters[key]["Value"];
                    var type = value.GetType().Name;
                    switch (type)
                    {
                        case "Boolean":
                            paras.Add(entry);
                            break;
                        default:
                            paras.Add($"{entry}:{value}");
                            break;
                    }
                }
            }

            var inTab = navigationViewItem.TabSettingsDict;

            if (inTab != null)
            {
                foreach (var key in inTab.Keys)
                {
                    if ((bool)inTab[key]["Defined"])
                    {
                        var entry = $"/{inTab[key]["Key"]}";
                        var value = inTab[key]["Value"];
                        var type = value.GetType().Name;
                        switch (type)
                        {
                            case "Boolean":
                                paras.Add(entry);
                                break;
                            default:
                                paras.Add($"{entry}:{value}");
                                break;
                        }
                    }
                }
            }
            return string.Join(" ", paras.ToArray());
        }

        private void UpdateTabCommandLine()
        {
            tabCommandLine.Text = BuildTabCommandLine();
        }

        private void ExecuteInterpreter(string selectedText)
        {
            // load tab settings
            ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
            //foreach (var setting in localSettings.Values)
            //{
            //    if (setting.Key.StartsWith("tab_"))
            //        Debug.WriteLine("{0} => {1}", setting.Key, setting.Value);
            //}

            string pelotonARG = BuildTabCommandLine();

            // override with matching tab settings
            // generate arguments string
            (string stdOut, string stdErr) = RunPeloton(pelotonEXE, pelotonARG, selectedText);

            Run run = new();
            Paragraph paragraph = new();
            if (!string.IsNullOrEmpty(stdOut))
            {
                run.Text = stdOut;
                paragraph.Inlines.Add(run);
                outputText.Blocks.Add(paragraph);
            }

            run = new();
            paragraph = new();
            if (!string.IsNullOrEmpty(stdErr))
            {
                run.Text = stdErr;
                paragraph.Inlines.Add(run);
                errorText.Blocks.Add(paragraph);
            }
        }


        public static (string StdOut, string StdErr) RunPeloton(string exe, string args, string buff)
        {
            //var temp = Path.GetTempFileName();
            //File.WriteAllText(temp, buff);
            ProcessStartInfo info = new()
            {
                Arguments = $"{args}",
                FileName = exe,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                RedirectStandardInput = true,
                // CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden
            };

            var proc = Process.Start(info);
            StringBuilder stdout = new();
            StringBuilder stderr = new();

            StreamWriter stream = proc.StandardInput;
            stream.Write(buff);
            stream.Close();

            proc.OutputDataReceived += (object sender, DataReceivedEventArgs e) => stdout.Append(e.Data);
            proc.ErrorDataReceived += (object sender, DataReceivedEventArgs e) => stderr.Append(e.Data);

            proc.BeginErrorReadLine();
            proc.BeginOutputReadLine();

            proc.WaitForExit();
            proc.Dispose();

            return (StdOut: stdout.ToString(), StdErr: stderr.ToString());
        }
        #endregion

        private void mnuIDEConfiguration_Click(object sender, RoutedEventArgs e)
        {
            //var g = new Grid();
            //g.ColumnDefinitions.Add(new ColumnDefinition());
            //g.ColumnDefinitions.Add(new ColumnDefinition());
            //g.RowDefinitions.Add(new RowDefinition());
            //g.RowDefinitions.Add(new RowDefinition());
            //g.RowDefinitions.Add(new RowDefinition());

            //var cd = new ContentDialog();
            //var tb = new TextBlock();
            //tb.Text = "Path to Interpreter";
            //tb.HorizontalAlignment = HorizontalAlignment.Right;
            //tb.VerticalAlignment = VerticalAlignment.Center;
            //tb.FontWeight = FontWeights.Bold;
            //tb.Margin = new Thickness(5);
            
            
            //cd.


        }

        private void ContentControl_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {

        }
    }
}
