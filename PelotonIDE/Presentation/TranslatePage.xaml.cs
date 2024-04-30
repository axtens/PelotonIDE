using ClosedXML.Excel;

using System.Text.RegularExpressions;

using Group = System.Text.RegularExpressions.Group;
using LanguageConfigurationStructure = System.Collections.Generic.Dictionary<string, System.Collections.Generic.Dictionary<string, System.Collections.Generic.Dictionary<string, string>>>;
using LanguageConfigurationStructureSelection =
    System.Collections.Generic.Dictionary<string,
        System.Collections.Generic.Dictionary<string, string>>;
using TabSettingJson = System.Collections.Generic.Dictionary<string, System.Collections.Generic.Dictionary<string, object>>;

namespace PelotonIDE.Presentation
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class TranslatePage : Microsoft.UI.Xaml.Controls.Page
    {
        //List<PropertyBag>? OldPlexes;

        LanguageConfigurationStructure? Langs;
        string? SourcePath { get; set; }
        string? SourceSpec { get; set; }

        long Quietude { get; set; }
        internal static List<Plex>? Plexes { get; private set; }

        TabSettingJson? SourceInFocusTabSettings { get; set; }

        [GeneratedRegex(@"<(?:#|@) (.+?)>(.*?)</(?:#|@)>", RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.Compiled, "en-AU")]
        private static partial Regex PelotonFullPattern();

        [GeneratedRegex(@"<# (.+?)>", RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.Compiled, "en-AU")]
        private static partial Regex PelotonVariableSpacedPattern();

        [GeneratedRegex(@"<@ (...\s{0,1})+?>", RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.Compiled, "en-AU")]
        private static partial Regex PelotonFixedSpacedPattern();

        public TranslatePage()
        {
            this.InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            NavigationData parameters = (NavigationData)e.Parameter;

            if (parameters.Source == "MainPage")
            {
                SourceInFocusTabSettings = (TabSettingJson?)parameters.KVPs["InFocusTabSettingsDict"];
                Plexes = (List<Plex>?)parameters.KVPs["Plexes"];
                Langs = (LanguageConfigurationStructure)parameters.KVPs["Languages"];
                //string? tabLanguageName = parameters.KVPs["TabLanguageName"].ToString();
                int tabLanguageId = (int)(long)parameters.KVPs["TabLanguageID"];
                string? interfaceLanguageName = parameters.KVPs["ideOps.InterfaceLanguageName"].ToString();
                Quietude = (long)parameters.KVPs["pOps.Quietude"];
                //int interfaceLanguageID = (int)(long)parameters.KVPs["ideOps.InterfaceLanguageID"];
                SourcePath = parameters.KVPs["SourcePath"].ToString();
                SourceSpec = parameters.KVPs["SourceSpec"].ToString();

                FillLanguagesIntoList(Langs, interfaceLanguageName!, sourceLanguageList);
                FillLanguagesIntoList(Langs, interfaceLanguageName!, targetLanguageList);

                LanguageConfigurationStructureSelection language = Langs[interfaceLanguageName!];
                cmdCancel.Content = language["frmMain"]["cmdCancel"];
                cmdSaveMemory.Content = language["frmMain"]["cmdSaveMemory"];
                chkSpaceOut.Content = language["frmMain"]["chkSpaceOut"];
                chkVarLengthFrom.Content = language["frmMain"]["chkVarLengthFrom"];
                chkVarLengthTo.Content = language["frmMain"]["chkVarLengthTo"];

                CustomRichEditBox rtb = ((CustomRichEditBox)parameters.KVPs["RichEditBox"]);
                rtb.Document.GetText(Microsoft.UI.Text.TextGetOptions.None, out string selectedText);
                while (selectedText.EndsWith('\r')) selectedText = selectedText.Remove(selectedText.Length - 1);
                sourceText.Document.SetText(Microsoft.UI.Text.TextSetOptions.None, selectedText);

                if (selectedText.Contains("</#>"))
                {
                    chkVarLengthFrom.IsChecked = true;
                }

                if (ProbablySpacedInstructions(selectedText))
                {
                    chkSpaceIn.IsChecked = true;
                }

                sourceLanguageList.SelectedIndex = tabLanguageId;
                sourceLanguageList.ScrollIntoView(sourceLanguageList.SelectedItem);
                (sourceLanguageList.ItemContainerGenerator.ContainerFromIndex(tabLanguageId) as ListBoxItem)?.Focus(FocusState.Programmatic);

            }
        }

        private bool ProbablySpacedInstructions(string selectedText)
        {
            int result = 0;
            Regex pattern = PelotonFullPattern();
            MatchCollection matches = pattern.Matches(selectedText);
            for (int i = 0; i < matches.Count; i++)
            {
                Match match = matches[i];
                ReadOnlySpan<char> group = match.Groups[1].ValueSpan;
                var enu = group.EnumerateRunes();
                do
                {
                    if (enu.Current.Value == ' ') result++;
                } while (enu.MoveNext());
            }
            return result > 0;
        }

        private static void FillLanguagesIntoList(LanguageConfigurationStructure languages, string interfaceLanguageName, ListBox listBox)
        {
            if (languages is null)
            {
                throw new ArgumentNullException(nameof(languages));
            }
            // what is current language?
            Dictionary<string, string> globals = languages[interfaceLanguageName]["GLOBAL"];
            for (int i = 0; i < languages.Keys.Count; i++)
            {
                var names = from lang in languages.Keys
                            where languages.ContainsKey(lang) && languages[lang]["GLOBAL"]["ID"] == i.ToString()
                            let name = languages[lang]["GLOBAL"]["Name"]
                            select name;
                if (names.Any())
                {
                    string name = names.First();
                    bool present = LanguageIsPresentInPlexes(name);

                    ListBoxItem listBoxItem = new()
                    {
                        Content = globals[$"{100 + i + 1}"],
                        Name = name,
                        IsEnabled = present
                    };
                    listBox.Items.Add(listBoxItem);
                }
            }
        }

        private static bool LanguageIsPresentInPlexes(string name) => (from plex in Plexes where plex.Meta.Language == name.Replace(" ", "") select plex).Any();

        private string TranslateCode(string code, string sourceLanguageName, string targetLanguageName)
        {
            Telemetry.SetEnabled(false);

            Telemetry.Transmit("TranslateCode", "code=", code, "sourceLanguageName=", sourceLanguageName, "targetLanguageName=", targetLanguageName);

            bool variableTarget = chkVarLengthTo.IsChecked ?? false;
            bool variableSource = chkVarLengthFrom.IsChecked ?? false;
            bool fixedTarget = chkVarLengthTo.IsChecked == false;
            bool fixedSource = chkVarLengthFrom.IsChecked == false;
            bool spaced = chkSpaceOut.IsChecked ?? false;

            string? sourceName = (sourceLanguageName).Replace(" ", "").ToUpperInvariant();
            string? targetName = (targetLanguageName).Replace(" ", "").ToUpperInvariant();

            Plex englishFixed = (from plex in Plexes where plex.Meta.Language == "English" && !plex.Meta.Variable select plex).First();

            IEnumerable<Plex> sourcePlexVariable = from plex in Plexes where plex.Meta.Language == sourceLanguageName.Replace(" ", "") && plex.Meta.Variable select plex;
            IEnumerable<Plex> targetPlexVariable = from plex in Plexes where plex.Meta.Language == targetLanguageName.Replace(" ", "") && plex.Meta.Variable select plex;

            IEnumerable<Plex> sourcePlexFixed = from plex in Plexes where plex.Meta.Language == sourceLanguageName.Replace(" ", "") && !plex.Meta.Variable select plex;
            IEnumerable<Plex> targetPlexFixed = from plex in Plexes where plex.Meta.Language == targetLanguageName.Replace(" ", "") && !plex.Meta.Variable select plex;


            Telemetry.Transmit("TranslateCode", "variableTarget=", variableTarget, "variableSource=", variableSource, "fixedTarget=", fixedTarget, "fixedSource=", fixedSource, "spaced=", spaced);

            Plex source = new();
            Plex target = new();

            source = variableSource && sourcePlexVariable.Any() ? sourcePlexVariable.First() : sourcePlexFixed.First();
            target = variableTarget && targetPlexVariable.Any() ? targetPlexVariable.First() : targetPlexFixed.First();

            List<KeyValuePair<string, string>> kvpList =
            [
                .. from long key in englishFixed.OpcodesByValue.Keys
                                 select new KeyValuePair<string, string>($"{key:00000000}", target.OpcodesByValue[key]),
            ];

            string translatedCode = variableSource && sourcePlexVariable.Any()
                ? ProcessVariableToFixedOrVariable(code, source, target, spaced, variableTarget)
                : ProcessFixedToFixedOrVariableWithOrWithoutSpace(code, source, target, spaced, variableTarget);

            string? pathToSource = SourcePath; // Path.GetDirectoryName(SourceSpec);
            string? nameOfSource = Path.GetFileNameWithoutExtension(SourceSpec);
            string? xlsxPath = Path.Combine(pathToSource ?? ".", "p.xlsx");

            Telemetry.Transmit("TranslateCode", "pathToSource=", pathToSource, "nameOfSource=", nameOfSource, "xlsxPath=", xlsxPath);

            bool ok = false;

            (ok, XLWorkbook? workbook) = GetNamedExcelWorkbook(xlsxPath);
            if (!ok) return translatedCode;

            (ok, IXLWorksheet? worksheet) = GetNamedWorksheetInExcelWorkbook(workbook, nameOfSource);
            if (!ok)
            {
                (ok, worksheet) = GetNamedWorksheetInExcelWorkbook(workbook, "Document#");
                if (!ok) return translatedCode;
            }

            (ok, int sourceCol, int targetCol) = GetSourceAndTargetColumnsFromWorksheet(worksheet, source.Meta.LanguageId, target.Meta.LanguageId);
            if (!ok) return translatedCode;

            // iterate thru strings in source language, building dictionary of replacements ordered by length of sourceText
            SortedDictionary<string, (double _typeCode, string _text)> sortedDictionary = new(new LongestToShortestLengthComparer());
            (ok, SortedDictionary<string, (double _typeCode, string _text)> dict) = FillSortedDictionaryFromWorksheet(sortedDictionary, worksheet, sourceCol, targetCol);
            if (!ok) return translatedCode;

            long DEF_opcode = englishFixed.OpcodesByKey["DEF"];
            long KOP_opcode = englishFixed.OpcodesByKey["KOP"];
            long RST_opcode = englishFixed.OpcodesByKey["RST"];
            long SAY_opcode = englishFixed.OpcodesByKey["SAY"];
            long GET_opcode = englishFixed.OpcodesByKey["GET"];
            long UDR_opcode = englishFixed.OpcodesByKey["UDR"];
            long UDO_opcode = englishFixed.OpcodesByKey["UDO"];
            long KEY_opcode = englishFixed.OpcodesByKey["KEY"];

            foreach (string key in dict.Keys)
            {
                Telemetry.Transmit("TranslateCode", "key=", key, "dict[key]._typeCode=", dict[key]._typeCode, "dict[key]._text=", dict[key]._text);

                switch (dict[key]._typeCode)
                {
                    case 1: // undefined
                        break;
                    case 2: // KOP
                        string kopPattern = $"<{(source.Meta.Variable ? "#" : "@")} {target.OpcodesByValue[DEF_opcode]}{source.OpcodesByValue[KOP_opcode]}.*?>(.*?{Regex.Escape(key)}[^<]*)";
                        translatedCode = MorphTranslatedCodeUsingPattern(translatedCode, dict, key, kopPattern);

                        break;
                    case 3: // Code Block 
                        string defudrPattern = $"<{(source.Meta.Variable ? "#" : "@")} {target.OpcodesByValue[DEF_opcode]}{target.OpcodesByValue[UDR_opcode]}.*?>(.*?{Regex.Escape(key)}[^<]*)";
                        translatedCode = MorphTranslatedCodeUsingPattern(translatedCode, dict, key, defudrPattern);
                        string defudoPattern = $"<{(source.Meta.Variable ? "#" : "@")} {target.OpcodesByValue[DEF_opcode]}{target.OpcodesByValue[UDO_opcode]}.*?>(.*?{Regex.Escape(key)}[^<]*)";
                        translatedCode = MorphTranslatedCodeUsingPattern(translatedCode, dict, key, defudoPattern);
                        break;
                    case 4: // SQL
                        string rstPattern = $"<{(source.Meta.Variable ? "#" : "@")} {target.OpcodesByValue[RST_opcode]}.*?>(.*?{Regex.Escape(key)}[^<]*)";
                        translatedCode = MorphTranslatedCodeUsingPattern(translatedCode, dict, key, rstPattern);

                        break;
                    case 5: // undefind
                        break;
                    case 6: // file extension
                        break;
                    case 7: // Pattern
                        break;
                    case 8: // Syskey
                        string keyPattern = $"<{(source.Meta.Variable ? "#" : "@")} .*?{target.OpcodesByValue[KEY_opcode]}>(.*?{Regex.Escape(key)}[^<]*)";
                        translatedCode = MorphTranslatedCodeUsingPattern(translatedCode, dict, key, keyPattern);
                        break;
                    case 9: // Protium symbol
                        break;
                    case 10: // Wildcard
                        break;
                    case 11: // String Literal
                        string sayPattern = $"<{(source.Meta.Variable ? "#" : "@")} {target.OpcodesByValue[SAY_opcode]}.*?>(.*?{Regex.Escape(key)}[^<]*)";
                        translatedCode = MorphTranslatedCodeUsingPattern(translatedCode, dict, key, sayPattern);
                        string getPattern = $"<{(source.Meta.Variable ? "#" : "@")} {target.OpcodesByValue[GET_opcode]}.*?>(.*?{Regex.Escape(key)}[^<]*)";
                        translatedCode = MorphTranslatedCodeUsingPattern(translatedCode, dict, key, getPattern);
                        break;
                    default:
                        break;
                }
            }
            while (translatedCode.EndsWith('\r')) translatedCode = translatedCode.Remove(translatedCode.Length - 1);
            return translatedCode;

            static string MorphTranslatedCodeUsingPattern(string translatedCode, SortedDictionary<string, (double _typeCode, string _text)> dict, string key, string regexPattern)
            {
                Regex sayRegex = new(regexPattern, RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.Compiled | RegexOptions.RightToLeft);
                MatchCollection regexMatches = sayRegex.Matches(translatedCode);
                if (regexMatches != null)
                {
                    for (int matchNo = 0; matchNo < regexMatches.Count; matchNo++)
                    {
                        Match regexMatch = regexMatches[matchNo];
                        if (regexMatch.Groups.Count > 1)
                        {
                            Group secondGroup = regexMatch.Groups[1];
                            string value = secondGroup.Value;
                            if (value != null)
                            {
                                value = value.Replace(key, dict[key]._text, StringComparison.InvariantCultureIgnoreCase);
                                translatedCode = translatedCode.Remove(secondGroup.Index, secondGroup.Length);
                                translatedCode = translatedCode.Insert(secondGroup.Index, value);
                            }
                        }
                    }
                }

                return translatedCode;
            }
        }

        private string UpdateInLabelSpace(string result, string sourceText, string targetText)
        {
            var pattern = PelotonFullPattern();
            MatchCollection matches = pattern.Matches(result);
            for (int i = matches.Count - 1; i >= 0; i--)
            {
                Match match = matches[i];
                Group label = match.Groups[2];
                var index = label.Index;
                var length = label.Length;
                var value = label.Value;
                if (value.Contains(sourceText, StringComparison.CurrentCultureIgnoreCase))
                {
                    result = result.Remove(index, length);
                    value = value.Replace(sourceText, targetText, StringComparison.CurrentCultureIgnoreCase);
                    result = result.Insert(index, value);
                }
            }
            return result;
        }

        private static string ProcessVariableToFixedOrVariable(string code, Plex source, Plex target, bool spaced, bool variableTarget)
        {
            IOrderedEnumerable<string> variableLengthWords = from variableLengthWord in source.OpcodesByKey.Keys orderby -variableLengthWord.Length select variableLengthWord;

            Dictionary<string, string> fixedLengthEquivalents = (from word in variableLengthWords
                                          let sourceop = source.OpcodesByKey[word]
                                          let targetword = target.OpcodesByValue[sourceop]
                                          select (word, targetword)).ToDictionary(x => x.word, x => x.targetword);

            List<Capture> codeBlocks = GetCodeBlocks(code); // in reverse order

            foreach (Capture block in codeBlocks)
            {
                string codeChunk = block.Value;
                foreach (string? vlw in variableLengthWords)
                {
                    string spacedVlw = vlw + " ";

                    if (codeChunk.Contains(spacedVlw, StringComparison.CurrentCulture))
                    {
                        if (spaced)
                        {
                            codeChunk = codeChunk.Replace(spacedVlw, fixedLengthEquivalents[vlw] + " ").Trim();
                        }
                        else
                        {
                            codeChunk = codeChunk.Replace(spacedVlw, fixedLengthEquivalents[vlw]).Trim();
                        }
                        continue;
                    }
                    if (codeChunk.Contains(vlw, StringComparison.CurrentCulture))
                    {
                        if (spaced)
                        {
                            codeChunk = codeChunk.Replace(vlw, fixedLengthEquivalents[vlw] + " ").Trim();
                        }
                        else
                        {
                            codeChunk = codeChunk.Replace(vlw, fixedLengthEquivalents[vlw]).Trim();
                        }
                    }
                }
                code = code.Remove(block.Index, block.Length)
                    .Insert(block.Index, codeChunk);
            }
            return variableTarget ? code.Replace("<@", "<#").Replace("</@>", "</#>") : code.Replace("<#", "<@").Replace("</#>", "</@>");
        }

        private static List<Capture> GetCodeBlocks(string code)
        {
            List<Capture> codeBlocks = new List<Capture>();
            Regex pattern = PelotonVariableSpacedPattern();
            MatchCollection matches = pattern.Matches(code);
            for (int mi = matches.Count - 1; mi >= 0; mi--)
            {
                for (int i = matches[mi].Groups[1].Captures.Count - 1; i >= 0; i--)
                {
                    Capture cap = matches[mi].Groups[1].Captures[i];
                    if (cap == null) continue;
                    codeBlocks.Add(cap);
                }
            }
            return codeBlocks;
        }

        private static string ProcessFixedToFixedOrVariableWithOrWithoutSpace(string buff, Plex sourcePlex, Plex targetPlex, bool spaceOut, bool variableTarget)
        {
            var pattern = PelotonFixedSpacedPattern();
            MatchCollection matches = pattern.Matches(buff);
            for (int mi = matches.Count - 1; mi >= 0; mi--)
            {
                //var max = kopMatches[mi].Groups[2].Captures.Count - 1;
                for (int i = matches[mi].Groups[1].Captures.Count - 1; i >= 0; i--)
                {
                    Capture capture = matches[mi].Groups[1].Captures[i];
                    string key = capture.Value.ToUpper(System.Globalization.CultureInfo.InvariantCulture).Trim();
                    if (sourcePlex.OpcodesByKey.TryGetValue(key, out long opcode))
                    {
                        if (targetPlex.OpcodesByValue.TryGetValue(opcode, out string? value))
                        {
                            string newKey = value;
                            string next = buff.Substring(capture.Index + capture.Length, 1);
                            buff = buff.Remove(capture.Index, capture.Length)
                                .Insert(capture.Index, newKey + ((spaceOut && next != ">") ? " " : ""));
                        }
                    }
                }
                // var tag = kopMatches[mi].Groups[1].Captures[0];
            }
            return targetPlex.Meta.Variable ? buff.Replace("<@ ", "<# ").Replace("</@>", "</#>") : buff;
        }
    }
}
