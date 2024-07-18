using Microsoft.UI;
using Microsoft.UI.Text;

using Newtonsoft.Json;

using System.Diagnostics;
using System.IO.Compression;

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
                    Type_1_UpdateVirtualRegistry("ideOps.CodeFolder", parameters.KVPs["ideOps.CodeFolder"].ToString());
                    Type_1_UpdateVirtualRegistry("ideOps.DataFolder", parameters.KVPs["ideOps.DataFolder"].ToString());
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

                    UpdateStatusBar();
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
                    if (key.StartsWith("pOps.") || key.StartsWith("ideOps.") || key.StartsWith("outputOps."))
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
            Telemetry.Disable();

            LanguageSettings ??= await GetLanguageConfiguration();
            RenderingConstants ??= await GetRenderingConstants();

            if (LangLangs.Count == 0)
                LangLangs = GetLangLangs(LanguageSettings);

            if (!IsPowerShellInstalled())
            {
                await PowerShellNeedDialog();
            }

            bool result = await TestPresenceOfAllPlexes();

            SetKeyboardFlags();

            FactorySettings ??= await GetFactorySettings();
            Telemetry.SetFactorySettings(FactorySettings);

            // #MainPage-LoadingVirtReg
            IfNotInVirtualRegistryUpdateItFromFactorySettingsOrDefaultTo<string>("outputOps.AvailableRenderers", FactorySettings, "3,0,21,42,31");
            IfNotInVirtualRegistryUpdateItFromFactorySettingsOrDefaultTo<string>("outputOps.ActiveRenderers", FactorySettings, "3,0");
            IfNotInVirtualRegistryUpdateItFromFactorySettingsOrDefaultTo<long>("outputOps.TappedRenderer", FactorySettings, -1);
            IfNotInVirtualRegistryUpdateItFromFactorySettingsOrDefaultTo<double>("ideOps.FontSize", FactorySettings, (double)12.0);
            IfNotInVirtualRegistryUpdateItFromFactorySettingsOrDefaultTo<bool>("ideOps.UsePerTabSettingsWhenCreatingTab", FactorySettings, true);
            //IfNotInVirtualRegistryUpdateItFromFactorySettingsOrDefaultTo<string>("ideOps.DataFolder", FactorySettings, @"C:\Peloton\Data");

            IfNotInVirtualRegistryUpdateItFromFactorySettingsOrDefaultTo<long>("ideOps.Engine", FactorySettings, 3L);
            Engine = Type_1_GetVirtualRegistry<long>("ideOps.Engine");

            IfNotInVirtualRegistryUpdateItFromFactorySettingsOrDefaultTo<string>("ideOps.CodeFolder", FactorySettings, @"C:\peloton\code");
            Codes = Type_1_GetVirtualRegistry<string>("ideOps.CodeFolder");

            IfNotInVirtualRegistryUpdateItFromFactorySettingsOrDefaultTo<string>("ideOps.DataFolder", FactorySettings, @"C:\peloton\data");
            Datas = Type_1_GetVirtualRegistry<string>("ideOps.DataFolder");

            IfNotInVirtualRegistryUpdateItFromFactorySettingsOrDefaultTo<string>("ideOps.Engine.2", FactorySettings, @"c:\protium\bin\pdb.exe");
            InterpreterP2 = Type_1_GetVirtualRegistry<string>("ideOps.Engine.2");

            IfNotInVirtualRegistryUpdateItFromFactorySettingsOrDefaultTo<string>("ideOps.Engine.3", FactorySettings, @"C:\peloton\bin\p3.exe");
            InterpreterP3 = Type_1_GetVirtualRegistry<string>("ideOps.Engine.3");

            IfNotInVirtualRegistryUpdateItFromFactorySettingsOrDefaultTo<long>("pOps.Transput", FactorySettings, 2);

            IfNotInVirtualRegistryUpdateItFromFactorySettingsOrDefaultTo<long>("ideOps.Timeout", FactorySettings, 1);
            // UpdateTimeoutInMenu(); // BOOM
            //UpdateRenderingInMenu();

            // UpdateTransputInMenu();

            IfNotInVirtualRegistryUpdateItFromFactorySettingsOrDefaultTo<string>("ideOps.OutputPanelSettings", FactorySettings, "True|Bottom|200|400");

            var position = FromBarredString_GetString(Type_1_GetVirtualRegistry<string>("ideOps.OutputPanelSettings"), 1);
            Type_1_UpdateVirtualRegistry<string>("ideOps.OutputPanelPosition", position);

            Type_1_UpdateVirtualRegistry<bool>("ideOps.OutputPanelShowing", FromBarredString_GetBoolean(Type_1_GetVirtualRegistry<string>("ideOps.OutputPanelSettings"), 0));
            Type_1_UpdateVirtualRegistry<double>("ideOps.OutputPanelHeight", (double)FromBarredString_GetDouble(Type_1_GetVirtualRegistry<string>("ideOps.OutputPanelSettings"), 2));
            Type_1_UpdateVirtualRegistry<double>("ideOps.OutputPanelWidth", (double)FromBarredString_GetDouble(Type_1_GetVirtualRegistry<string>("ideOps.OutputPanelSettings"), 3));

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

            //SetEngine();
            //SetScriptsAndData();
            //SetInterpreterNew();
            //SetInterpreterOld();

            PerTabInterpreterParameters ??= await MainPage.GetPerTabInterpreterParametersIncludingMatchingVirtualRegistry();

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


                UpdateStatusBar();
                DeserializeTabsFromVirtualRegistry();
            }
            InterfaceLanguageSelectionBuilder(mnuSelectLanguage, Internationalization_Click);
            InterpreterLanguageSelectionBuilder(mnuRun, "mnuLanguage", MnuLanguage_Click);
            // UpdateEngineSelectionFromFactorySettingsInMenu();

            //if (!AfterTranslation)
            //{
            //    UpdateMenuRunningModeInMenu(PerTabInterpreterParameters["pOps.Quietude"]);
            //}

            if (AfterTranslation)
            {
                await HtmlText.EnsureCoreWebView2Async();
                HtmlText.NavigateToString("<body style='background-color: papayawhip;'></body>");

                await LogoText.EnsureCoreWebView2Async();
                LogoText.NavigateToString("<body style='background-color: #ffdad5;'></body>");
            }


            AfterTranslation = false;

            // SetVariableLengthModeInMenu(mnuVariableLength, Type_1_GetVirtualRegistry<bool>("pOps.VariableLength"));

            // UpdateLanguageNameInStatusBar(navigationViewItem.TabSettingsDict);

            // UpdateStatusBarFromVirtualRegistry();

            UpdateStatusBar();
            spOutput.Visibility = Visibility.Visible;

            // outputPanel.Width = relativePanel.ActualSize.X;            

            (tabControl.Content as CustomRichEditBox).Focus(FocusState.Keyboard);

            string currentLanguageName = GetLanguageNameOfCurrentTab(navigationViewItem.TabSettingsDict);
            if (sbLanguageName.Text != currentLanguageName)
            {
                sbLanguageName.Text = currentLanguageName;
            }

            // UpdateTopMostRendererInCurrentTab();
            UpdateOutputTabs();
            // UpdateTabCreationMethodInMenu();
            UpdateMenus();

            return;

            void SetKeyboardFlags()
            {
                var lightGrey = new SolidColorBrush(Colors.LightGray);
                var black = new SolidColorBrush(Colors.Black);
                CAPS.Foreground = Console.CapsLock ? black : lightGrey;
                NUM.Foreground = Console.NumberLock ? black : lightGrey;
                //INS.Foreground = InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Insert).HasFlag(CoreVirtualKeyStates.Locked) ? black : lightGrey;
            }

            //void SetEngine()
            //{
            //    if (LocalSettings.Values.TryGetValue("ideOps.Engine", out object? value))
            //    {
            //        Engine = (long)value;
            //    }
            //    else
            //    {
            //        Engine = (long)FactorySettings["ideOps.Engine"];
            //    }
            //    Type_1_UpdateVirtualRegistry("ideOps.Engine", Engine);
            //}
            //void SetScriptsAndData()
            //{
            //    if (LocalSettings.Values.TryGetValue("ideOps.CodeFolder", out object? value))
            //    {
            //        Codes = value.ToString();
            //    }
            //    else
            //    {
            //        Codes = FactorySettings["ideOps.CodeFolder"].ToString();
            //    }
            //    Codes ??= @"C:\peloton\code";
            //    Type_1_UpdateVirtualRegistry("ideOps.CodeFolder", Codes);            

            //    if (LocalSettings.Values.TryGetValue("ideOps.DataFolder", out object? dvalue))
            //    {
            //        Datas = dvalue.ToString();
            //    }
            //    else
            //    {
            //        Datas = FactorySettings["ideOps.DataFolder"].ToString();
            //    }
            //    Datas ??= @"C:\peloton\data";
            //    Type_1_UpdateVirtualRegistry("ideOps.DataFolder", Datas);
            //}
            //void SetInterpreterOld()
            //{
            //    if (LocalSettings.Values.TryGetValue("ideOps.Engine.2", out object? value))
            //    {
            //        InterpreterP2 = value.ToString();
            //    }
            //    else
            //    {
            //        InterpreterP2 = FactorySettings["ideOps.Engine.2"].ToString();
            //    }
            //    InterpreterP2 ??= @"c:\protium\bin\pdb.exe";
            //    Type_1_UpdateVirtualRegistry("ideOps.Engine.2", InterpreterP2);
            //}
            //void SetInterpreterNew()
            //{
            //    if (LocalSettings.Values.TryGetValue("ideOps.Engine.3", out object? value))
            //    {
            //        InterpreterP3 = value.ToString();
            //    }
            //    else
            //    {
            //        InterpreterP3 = FactorySettings["ideOps.Engine.3"].ToString();
            //    }
            //    InterpreterP3 ??= @"c:\peloton\bin\p3.exe";
            //    Type_1_UpdateVirtualRegistry("ideOps.Engine.3", InterpreterP3);
            //}
        }

        public async Task PowerShellNeedDialog()
        {
            Grid g = new() { 
                Name = "PowerShellNeeded", 
                Width = 600, 
                Height = 100 
            };

            RowDefinitionCollection rd = g.RowDefinitions;
            rd.Add(new RowDefinition());

            ColumnDefinitionCollection cd = g.ColumnDefinitions;
            cd.Add(new ColumnDefinition() {  });

            WebView2 wv = new WebView2();
            await wv.EnsureCoreWebView2Async();
            wv.NavigateToString(
            @"<HTML>
                <BODY>
                    <p>We have noticed that PowerShell is not installed</p>
                    <ol>
                        <li>Please visit the webpage below, download PowerShell and install it.<br />
                        <a target='_blank' href='https://pelotonprogramming.org/download_peloton'>https://pelotonprogramming.org/download_peloton</a></li>
                        <li>Close the IDE and re-launch it.</li>
                    </ol>
                </BODY>
            </HTML>");
            //wv.AddHandler(TappedEvent, () => {
            //    Process.Start("https://pelotonprogramming.org/download_peloton/");
            ////}, true);
            wv.CoreWebView2.NewWindowRequested += CoreWebView2_NewWindowRequested;
            g.Children.Add(wv);

            StackPanel sp = new() { 
                Name = "Panelled", 
                Width = 600
            };

            sp.Children.Add(g);

            ContentDialog dialog = new()
            {
                XamlRoot = this.XamlRoot,
                Style = Application.Current.Resources["DefaultContentDialogStyle"] as Style, // DefaultContentDialogStyle
                Title = "PowerShell Needed",
                Content = sp,
                PrimaryButtonText = "Close"
            };

            var result = await dialog.ShowAsync();
        }

        private void CoreWebView2_NewWindowRequested(Microsoft.Web.WebView2.Core.CoreWebView2 sender, Microsoft.Web.WebView2.Core.CoreWebView2NewWindowRequestedEventArgs args)
        {
            var ps = new ProcessStartInfo("https://pelotonprogramming.org/download_peloton")
            {
                UseShellExecute = true,
                Verb = "open"
            };
            Process.Start(ps);
            args.Handled = true;
        }

        private static bool CreateAndFillProtiumFolderIfMissing()
        {
            bool brokenInstallation = false;
            foreach (var folder in new string[] {
                @"C:\protium",
                @"C:\protium\bin",
                @"C:\protium\Code",
                @"C:\protium\Data",
                @"C:\protium\docs",
                @"C:\protium\temp",
                @"C:\protium\bin\.vs",
                @"C:\protium\bin\data",
                @"C:\protium\bin\Exe exchange",
                @"C:\protium\bin\Help",
                @"C:\protium\bin\icons",
                @"C:\protium\bin\Lexers",
                @"C:\protium\bin\lng",
                @"C:\protium\bin\plugins",
                @"C:\protium\bin\resources",
                @"C:\protium\bin\Exe exchange\aa Operating before replacement",
                @"C:\protium\bin\Exe exchange\NewInterpreters20July2015bin",
                @"C:\protium\bin\Help\decompiled",
                @"C:\protium\bin\Help\decompiled\html",
                @"C:\protium\bin\Help\decompiled\images",
                @"C:\protium\bin\plugins\images",
                @"C:\protium\Code\dt",
                @"C:\protium\Code\lib",
                @"C:\protium\Code\p",
                @"C:\protium\Code\pr",
                @"C:\protium\Code\prx",
                @"C:\protium\Code\pr\advanced",
                @"C:\protium\Code\pr\data",
                @"C:\protium\Code\pr\international",
                @"C:\protium\Code\pr\plugins",
                @"C:\protium\Code\pr\Simple",
                @"C:\protium\Code\pr\standard",
                @"C:\protium\Code\pr\structures",
                @"C:\protium\Code\pr\yb",
                @"C:\protium\Code\prx\China",
                @"C:\protium\Code\prx\library",
                @"C:\protium\Code\prx\plugins",
                @"C:\protium\Code\prx\library\css",
                @"C:\protium\Code\prx\library\data",
                @"C:\protium\Code\prx\library\images",
                @"C:\protium\Code\prx\library\projects",
                @"C:\protium\Code\prx\library\scripts",
                @"C:\protium\Code\prx\library\data\temp",
                @"C:\protium\Code\prx\plugins\cheetah",
                @"C:\protium\Code\prx\plugins\common",
                @"C:\protium\Code\prx\plugins\isis",
                @"C:\protium\Code\prx\plugins\sqlite",
                @"C:\protium\Code\prx\plugins\tsunami",
                @"C:\protium\Code\prx\plugins\zoom",
                @"C:\protium\Code\prx\plugins\cheetah\projects",
                @"C:\protium\Code\prx\plugins\cheetah\scripts",
                @"C:\protium\Code\prx\plugins\isis\data",
                @"C:\protium\Code\prx\plugins\isis\projects",
                @"C:\protium\Code\prx\plugins\isis\scripts",
                @"C:\protium\Code\prx\plugins\sqlite\projects",
                @"C:\protium\Code\prx\plugins\sqlite\scripts",
                @"C:\protium\Code\prx\plugins\tsunami\projects",
                @"C:\protium\Code\prx\plugins\tsunami\scripts",
                @"C:\protium\Code\prx\plugins\zoom\projects",
                @"C:\protium\Code\prx\plugins\zoom\scripts",
                @"C:\protium\Data\dbf",
                @"C:\protium\Data\excel",
                @"C:\protium\Data\isis",
                @"C:\protium\Data\msaccess",
                @"C:\protium\Data\mysql",
                @"C:\protium\Data\sqlite",
                @"C:\protium\Data\thes",
                @"C:\protium\Data\tinydb",
                @"C:\protium\Data\tsunami",
                @"C:\protium\Data\ZOOM",
                @"C:\protium\Data\mysql\cars",
                @"C:\protium\Data\mysql\northwind"})
            {
                if (!Directory.Exists(folder))
                {
                    brokenInstallation = true;
                    break;
                }
            }
            if (brokenInstallation)
            {
                ExtractProtiumAssets();
            }
            return true;
        }

        private static void ExtractProtiumAssets(string tag = "")
        {
            string root = Windows.ApplicationModel.Package.Current.InstalledLocation.Path;
            string zipPath = root + @"\Assets\InstallationItems\ProtiumAssets.zip";
            if (!File.Exists(zipPath)) { return; }
            Directory.CreateDirectory(@"C:\protium");
            foreach (ZipArchiveEntry entry in ZipFile.OpenRead(zipPath).Entries)
            {
                string target;
                if (tag == "")
                {
                    target = $"C:/protium/{entry.FullName}";
                    Directory.CreateDirectory(Path.GetDirectoryName(target)!);
                    if (entry.Length > 0)
                        entry.ExtractToFile(target, true);
                }
                else
                {
                    if (entry.FullName.StartsWith(tag + "/"))
                    {
                        target = $"C:/protium/{entry.FullName}";
                        Directory.CreateDirectory(Path.GetDirectoryName(target)!);
                        if (entry.Length > 0)
                            entry.ExtractToFile(target, true);
                    }
                }
            }

        }

        private static bool CreateAndFillPelotonFolderIfMissing()
        {
            bool brokenInstallation = false;
            foreach (var folder in new string[] {
                @"C:\Peloton",
                @"C:\Peloton\bin",
                @"C:\Peloton\code",
                @"C:\Peloton\data",
                @"C:\Peloton\bin\lexers",
                @"C:\Peloton\code\dt",
                @"C:\Peloton\code\learning Docs",
                @"C:\Peloton\code\lib",
                @"C:\Peloton\code\p",
                @"C:\Peloton\code\pr",
                @"C:\Peloton\code\prx",
                @"C:\Peloton\code\learning Docs\Day 1",
                @"C:\Peloton\code\learning Docs\Day 2",
                @"C:\Peloton\code\learning Docs\Day 3",
                @"C:\Peloton\code\learning Docs\Day 4",
                @"C:\Peloton\code\learning Docs\Day 5",
                @"C:\Peloton\code\learning Docs\Fishing1",
                @"C:\Peloton\code\learning Docs\Fishing4",
                @"C:\Peloton\code\learning Docs\PPA",
                @"C:\Peloton\code\learning Docs\Fishing1\buttons",
                @"C:\Peloton\code\learning Docs\Fishing1\images",
                @"C:\Peloton\code\learning Docs\Fishing4\Css",
                @"C:\Peloton\code\learning Docs\Fishing4\Data",
                @"C:\Peloton\code\learning Docs\Fishing4\Javascripts",
                @"C:\Peloton\code\learning Docs\Fishing4\Projects",
                @"C:\Peloton\code\learning Docs\Fishing4\Scripts",
                @"C:\Peloton\code\learning Docs\Fishing4\Siteimages",
                @"C:\Peloton\code\learning Docs\Fishing4\zSetupInstructions",
                @"C:\Peloton\code\learning Docs\Fishing4\Siteimages\buttons",
                @"C:\Peloton\code\learning Docs\Fishing4\Siteimages\images",
                @"C:\Peloton\code\pr\advanced",
                @"C:\Peloton\code\pr\data",
                @"C:\Peloton\code\pr\international",
                @"C:\Peloton\code\pr\plugins",
                @"C:\Peloton\code\pr\Simple",
                @"C:\Peloton\code\pr\standard",
                @"C:\Peloton\code\pr\structures",
                @"C:\Peloton\code\pr\yb",
                @"C:\Peloton\code\prx\China",
                @"C:\Peloton\code\prx\library",
                @"C:\Peloton\code\prx\plugins",
                @"C:\Peloton\code\prx\library\css",
                @"C:\Peloton\code\prx\library\data",
                @"C:\Peloton\code\prx\library\images",
                @"C:\Peloton\code\prx\library\projects",
                @"C:\Peloton\code\prx\library\scripts",
                @"C:\Peloton\code\prx\library\data\temp",
                @"C:\Peloton\code\prx\plugins\cheetah",
                @"C:\Peloton\code\prx\plugins\common",
                @"C:\Peloton\code\prx\plugins\isis",
                @"C:\Peloton\code\prx\plugins\sqlite",
                @"C:\Peloton\code\prx\plugins\tsunami",
                @"C:\Peloton\code\prx\plugins\zoom",
                @"C:\Peloton\code\prx\plugins\cheetah\projects",
                @"C:\Peloton\code\prx\plugins\cheetah\scripts",
                @"C:\Peloton\code\prx\plugins\isis\data",
                @"C:\Peloton\code\prx\plugins\isis\projects",
                @"C:\Peloton\code\prx\plugins\isis\scripts",
                @"C:\Peloton\code\prx\plugins\sqlite\projects",
                @"C:\Peloton\code\prx\plugins\sqlite\scripts",
                @"C:\Peloton\code\prx\plugins\tsunami\projects",
                @"C:\Peloton\code\prx\plugins\tsunami\scripts",
                @"C:\Peloton\code\prx\plugins\zoom\projects",
                @"C:\Peloton\code\prx\plugins\zoom\scripts",
                @"C:\Peloton\data\dbf",
                @"C:\Peloton\data\excel",
                @"C:\Peloton\data\isis",
                @"C:\Peloton\data\msaccess",
                @"C:\Peloton\data\mysql",
                @"C:\Peloton\data\sqlite",
                @"C:\Peloton\data\thes",
                @"C:\Peloton\data\tinydb",
                @"C:\Peloton\data\tsunami",
                @"C:\Peloton\data\ZOOM",
                @"C:\Peloton\data\mysql\cars",
                @"C:\Peloton\data\mysql\northwind" })
            {
                if (!Directory.Exists(folder))
                {
                    brokenInstallation = true;
                    break;
                }
            }
            if (brokenInstallation)
            {
                ExtractPelotonAssets();
            }
            return true;
        }

        //private void UpdateTabCreationMethodInMenu()
        //{
        //    MenuItemHighlightController(mnuPerTabSettings, UsePerTabSettingsWhenCreatingTab);
        //    MenuItemHighlightController(mnuCurrentTabSettings, !UsePerTabSettingsWhenCreatingTab);
        //}

        //private void UpdateTransputInMenu()
        //{
        //    Telemetry.Disable();

        //    string transput = Type_1_GetVirtualRegistry<long>("pOps.Transput").ToString();
        //    foreach (var mfi in from MenuFlyoutSubItem mfsi in mnuTransput.Items.Cast<MenuFlyoutSubItem>()
        //                        where mfsi != null
        //                        where mfsi.Items.Count > 0
        //                        from MenuFlyoutItem mfi in mfsi.Items.Cast<MenuFlyoutItem>()
        //                        select mfi)
        //    {
        //        MenuItemHighlightController((MenuFlyoutItem)mfi, false);
        //        if (transput == (string)mfi.Tag)
        //        {
        //            MenuItemHighlightController((MenuFlyoutItem)mfi, true);
        //        }
        //    }
        //}
        //private void UpdateRenderingInMenu()
        //{
        //    List<string> renderers = Type_1_GetVirtualRegistry<string>("outputOps.ActiveRenderers").Split(',').Select(x => x.Trim()).ToList();

        //    mnuRendering.Items.ForEach(item =>
        //    {
        //        MenuItemHighlightController((MenuFlyoutItem)item, false);
        //        if (renderers.Contains((string)item.Tag))
        //        {
        //            MenuItemHighlightController((MenuFlyoutItem)item, true);
        //        }

        //    });
        //}
        private Dictionary<string, List<string>> GetLangLangs(Dictionary<string, Dictionary<string, Dictionary<string, string>>>? languageSettings)
        {
            Telemetry.Disable();
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
                //Telemetry.Transmit("key=", key, "dict[key]=", strings.JoinBy("\n"));
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
        //private void UpdateStatusBarFromVirtualRegistry()
        //{
        //    string interfaceLanguageName = Type_1_GetVirtualRegistry<string>("ideOps.InterfaceLanguageName");

        //    bool isVariableLength = Type_1_GetVirtualRegistry<bool>("pOps.VariableLength");
        //    sbFixedVariable.Text = (isVariableLength ? "#" : "@") + LanguageSettings[interfaceLanguageName]["GLOBAL"][isVariableLength ? "variableLength" : "fixedLength"];

        //    string[] quietudes = ["mnuQuiet", "mnuVerbose", "mnuVerbosePauseOnExit"];
        //    long quietude = Type_1_GetVirtualRegistry<long>("pOps.Quietude");
        //    sbQuietude.Text = LanguageSettings[interfaceLanguageName]["frmMain"][quietudes.ElementAt((int)quietude)];

        //    string[] timeouts = ["mnu20Seconds", "mnu100Seconds", "mnu200Seconds", "mnu1000Seconds", "mnuInfinite"];
        //    long timeout = Type_1_GetVirtualRegistry<long>("ideOps.Timeout");
        //    sbTimeout.Text = $"{LanguageSettings[interfaceLanguageName]["frmMain"]["mnuTimeout"]}: {LanguageSettings[interfaceLanguageName]["frmMain"][timeouts.ElementAt((int)timeout)]}";
        //}
        //private void UpdateStatusBarFromInFocusTab()
        //{
        //    string interfaceLanguageName = Type_1_GetVirtualRegistry<string>("ideOps.InterfaceLanguageName");

        //    bool isVariableLength = Type_3_GetInFocusTab<bool>("pOps.VariableLength");
        //    sbFixedVariable.Text = (isVariableLength ? "#" : "@") + LanguageSettings[interfaceLanguageName]["GLOBAL"][isVariableLength ? "variableLength" : "fixedLength"];

        //    string[] quietudes = ["mnuQuiet", "mnuVerbose", "mnuVerbosePauseOnExit"];
        //    long quietude = Type_3_GetInFocusTab<long>("pOps.Quietude");
        //    sbQuietude.Text = LanguageSettings[interfaceLanguageName]["frmMain"][quietudes.ElementAt((int)quietude)];

        //    string[] timeouts = ["mnu20Seconds", "mnu100Seconds", "mnu200Seconds", "mnu1000Seconds", "mnuInfinite"];
        //    long timeout = Type_3_GetInFocusTab<long>("ideOps.Timeout");
        //    sbTimeout.Text = $"{LanguageSettings[interfaceLanguageName]["frmMain"]["mnuTimeout"]}: {LanguageSettings[interfaceLanguageName]["frmMain"][timeouts.ElementAt((int)timeout)]}";
        //}
        private void UpdateTabDocumentNameIfOnlyOneAndFirst(NavigationView tabControl, string? interfaceLanguageName)
        {
            if (tabControl.MenuItems.Count == 1 && interfaceLanguageName != null && interfaceLanguageName != "English")
            {
                string? content = (string?)((CustomTabItem)tabControl.SelectedItem).Content;
                content = content.Replace(LanguageSettings["English"]["GLOBAL"]["Document"], LanguageSettings[interfaceLanguageName]["GLOBAL"]["Document"]);
                ((CustomTabItem)tabControl.SelectedItem).Content = content;
            }
        }
        //private void UpdateEngineSelectionFromFactorySettingsInMenu()
        //{
        //    if (LocalSettings.Values["ideOps.Engine"].ToString() == "ideOps.Engine.2")
        //    {
        //        MenuItemHighlightController(mnuNewEngine, false);
        //        MenuItemHighlightController(mnuOldEngine, true);
        //        sbEngine.Text = "P2";
        //    }
        //    else
        //    {
        //        MenuItemHighlightController(mnuNewEngine, true);
        //        MenuItemHighlightController(mnuOldEngine, false);
        //        sbEngine.Text = "P3";
        //    }
        //}
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
            Telemetry.Disable();
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
