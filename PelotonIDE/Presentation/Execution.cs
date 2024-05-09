using Microsoft.UI.Dispatching;

using System.Diagnostics;
using System.Text;

using Windows.Storage;

namespace PelotonIDE.Presentation
{
    public sealed partial class MainPage : Page
    {
        private async void ExecuteInterpreter(string selectedText)
        {
            //Telemetry.EnableIfMethodNameInFactorySettingsTelemetry();
            Telemetry.EnableIfMethodNameInFactorySettingsTelemetry();

            DispatcherQueue dispatcher = DispatcherQueue.GetForCurrentThread();

            if (Type_3_GetInFocusTab<long>("pOps.Quietude") == 0 && Type_3_GetInFocusTab<long>("ideOps.Timeout") > 0)
            {
                if (!await AreYouSureYouWantToRunALongTimeSilently())
                {
                    return;
                }
            }

            Telemetry.Transmit("selectedText=", selectedText);
            // load tab settings


            long quietude = Type_3_GetInFocusTab<long>("pOps.Quietude");
            long interpreter = Type_3_GetInFocusTab<long>("ideOps.Engine");

            string engineArguments = BuildTabCommandLine();

            string output_Text = string.Empty,
                error_Text = string.Empty,
                RTF_Text = string.Empty,
                HTML_Text = string.Empty,
                Logo_Text = string.Empty;

            // override with matching tab settings
            // generate arguments string
            string stdOut;
            string stdErr;

            string interpKey = $"ideOps.Engine.{Type_3_GetInFocusTab<long>("ideOps.Engine")}";
            string? Exe = ApplicationData.Current.LocalSettings.Values[interpKey].ToString();

            if (!File.Exists(Exe))
            {
                if (!await FileNotFoundDialog(Exe)) return;
            }
            if (interpreter == 3)
            {

                (stdOut, stdErr) = RunPeloton2(Exe, engineArguments, selectedText, quietude, dispatcher);
            }
            else
            {
                (stdOut, stdErr) = RunProtium(Exe, engineArguments, selectedText, quietude);
            }

            Telemetry.Transmit("stdOut=", stdOut, "stdErr=", stdErr);

            IEnumerable<long> rendering = Type_3_GetInFocusTab<string>("outputOps.ActiveRenderers").Split(',', StringSplitOptions.RemoveEmptyEntries).Select(e => long.Parse(e)); // strip focuser

            IEnumerable<string> list = (from item in RenderingConstants["outputOps.ActiveRenderers"]
                                        where rendering.Contains((long)item.Value)
                                        select item.Key);

            foreach (string item in list)
            {
                switch (item)
                {
                    case "OUTPUT":
                        if (!string.IsNullOrEmpty(stdOut))
                        {
                            AddInsertParagraph(outputText, stdOut, false);
                        }
                        break;
                    case "ERROR":
                        if (!string.IsNullOrEmpty(stdErr))
                        {
                            AddInsertParagraph(errorText, stdErr, false);
                        }
                        break;
                    case "HTML":
                        if (!string.IsNullOrEmpty(stdOut))
                        {
                            if (stdOut.StartsWith("Status: 200 OK"))
                            {
                                StorageFolder folder = ApplicationData.Current.LocalFolder;
                                StorageFile file = await folder.CreateFileAsync("temp.html", CreationCollisionOption.ReplaceExisting);
                                List<string> lines = [.. stdOut.Split("\r\n", StringSplitOptions.RemoveEmptyEntries)];
                                lines.RemoveAt(0);
                                lines.RemoveAt(0);
                                await FileIO.WriteTextAsync(file, string.Join("\n", lines));
                                HtmlText.Source = new Uri(file.Path);// "file://c|/temp/temp.html");
                            }
                        }
                        break;
                    case "RTF":
                        if (!string.IsNullOrEmpty(stdOut))
                        {
                            rtfText.Document.SetText(Microsoft.UI.Text.TextSetOptions.FormatRtf, stdOut);
                        }
                        break;
                    case "LOGO":
                        if (!string.IsNullOrEmpty(stdOut))
                        {
                            StorageFolder folder = ApplicationData.Current.LocalFolder;
                            string guid = Guid.NewGuid().ToString();
                            StorageFile file = await folder.CreateFileAsync($"{guid}.logo", CreationCollisionOption.ReplaceExisting);
                            List<string> lines = [.. stdOut.Split("\r\n", StringSplitOptions.RemoveEmptyEntries)];
                            await FileIO.WriteTextAsync(file, string.Join("\n", lines));
                            string jsBlock = ParseLogoIntoJavascript(await FileIO.ReadTextAsync(file));
                            file = await folder.CreateFileAsync($"{guid}.html", CreationCollisionOption.ReplaceExisting);
                            await FileIO.WriteTextAsync(file, TurtleFrameworkPlus(jsBlock));
                            LogoText.Source = new Uri(file.Path);
                        }
                        break;
                }
            }

        }
        private string TurtleFrameworkPlus(string jsBlock)
        {
            return $@"<script type='text/javascript' src='https://unpkg.com/real-turtle'></script>" +
                    "<canvas id='real-turtle'></canvas>" +
                    "<script type='text/javascript' src='https://unpkg.com/real-turtle/build/helpers/simple.js'></script>" +
                    $"<script type='text/javascript'>{jsBlock}</script>";
        }
        private string ParseLogoIntoJavascript(string v)
        {
            Telemetry.EnableIfMethodNameInFactorySettingsTelemetry();

            List<string> result = [];
            string[] lines = v.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string line in lines)
            {
                if (line.StartsWith(';')) continue;
                var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length > 0)
                {
                    switch (parts[0].ToUpper())
                    {
                        case "CS":
                        case "CLEARSCREEN":
                            result.Add("turtle.clear()");
                            break;
                        case "PD":
                        case "PENDOWN":
                            result.Add("turtle.penDown()");
                            break;
                        case "PU":
                        case "PENUP":
                            result.Add("turtle.penDown()");
                            break;
                        case "FD":
                        case "FORWARD":
                            result.Add($"turtle.forward({parts[1]})");
                            break;
                        case "BK":
                        case "BACK":
                            result.Add($"turtle.back({parts[1]})");
                            break;
                        case "RT":
                        case "RIGHT":
                            result.Add($"turtle.right({parts[1]})");
                            break;
                        case "LT":
                        case "LEFT":
                            result.Add($"turtle.left({parts[1]})");
                            break;
                        case "SP":
                        case "SPEED":
                            result.Add($"turtle.setSpeed({parts[1]})");
                            break;
                        case "HT":
                        case "HIDETURTLE":
                            break;
                        case "SETXY":
                            result.Add($"turtle.setPosition({parts[1]},{parts[2]})");
                            break;
                        case "SETPENSIZE":
                            result.Add($"turtle.setLineWidth({parts[1]})");
                            break;
                        case "SETPENCOLOUR":
                        case "SETPENCOLOR":
                            result.Add($"turtle.setStrokeColorRGB({parts[1]},{parts[2]},{parts[3]})");
                            break;
                        case "SETFILLSTYLE":
                            result.Add($"turtle.setFillStyle({parts[1]})");
                            break;
                        case "FILL":
                            result.Add($"turtle.fill()");
                            break;
                        case "STROKE":
                            result.Add($"turtle.stroke()");
                            break;
                        case "BEGINPATH":
                            result.Add($"turtle.beginPath()");
                            break;
                        case "ENDPATH":
                            result.Add($"turtle.closePath()");
                            break;
                    }
                }
            }
            result.Add("turtle.start();");
            Telemetry.Transmit(result.JoinBy("\r\n"));
            return result.JoinBy("\n");
        }

        private static void AddInsertParagraph(RichEditBox reb, string text, bool addInsert = true, bool withPrefix = true)
        {
            Telemetry.EnableIfMethodNameInFactorySettingsTelemetry();
            if (string.IsNullOrEmpty(text))
            {
                return;
            }
            Telemetry.Transmit("text=", text, "addInsert=", addInsert, "withPrefix=", withPrefix);
            const string stamp = "> ";
            if (withPrefix)
                text = text.Insert(0, stamp);

            reb.Document.GetText(Microsoft.UI.Text.TextGetOptions.UseLf, out string? tx);
            if (addInsert)
            {
                reb.Document.SetText(Microsoft.UI.Text.TextSetOptions.None, tx + "\n" + text);
            }
            else
            {
                reb.Document.SetText(Microsoft.UI.Text.TextSetOptions.None, text + "\n" + tx);
            }
            reb.Focus(FocusState.Programmatic);
        }
        public (string StdOut, string StdErr) RunProtium(string? Exe, string args, string buff, long quietude)
        {
            Telemetry.EnableIfMethodNameInFactorySettingsTelemetry(2);

            string temp = System.IO.Path.GetTempFileName();
            File.WriteAllText(temp, buff, Encoding.Unicode);

            args = args.Replace(":", "=");

            args += $" /F:\"{temp}\"";

            Telemetry.Transmit("Exe=", Exe, "Args:", args);

            ProcessStartInfo info = new()
            {
                Arguments = $"{args}",
                FileName = Exe,
                UseShellExecute = false,
                CreateNoWindow = args.Contains("/Q=0"),
            };

            Process? proc = Process.Start(info);
            proc.WaitForExit(GetTimeoutInMilliseconds());
            proc.Dispose();

            return (StdOut: File.ReadAllText(System.IO.Path.ChangeExtension(temp, "out")), StdErr: string.Empty);
        }
        //public (string StdOut, string StdErr) RunPeloton(string args, string buff, long quietude)
        //{
        //    Telemetry.EnableIfMethodNameInFactorySettingsTelemetry();

        //    string interpKey = $"Engine.{Type_3_GetInFocusTab<long>("ideOps.Engine")}";
        //    string? Exe = ApplicationData.Current.LocalSettings.Values[interpKey].ToString();

        //    Telemetry.Transmit("Exe=", Exe, "Args:", args, "Buff=", buff, "Quietude=", quietude);

        //    string t_in = System.IO.Path.GetTempFileName();
        //    string t_out = System.IO.Path.ChangeExtension(t_in, "out");
        //    string t_err = System.IO.Path.ChangeExtension(t_in, "err");

        //    File.WriteAllText(t_in, buff);

        //    //args = args.Replace(":", "=");

        //    args += $" /F:\"{t_in}\""; // 1>\"{t_out}\" 2>\"{t_err}\"";

        //    Telemetry.Transmit(args, buff);

        //    ProcessStartInfo info = new()
        //    {
        //        Arguments = $"{args}",
        //        FileName = Exe,
        //        UseShellExecute = false,
        //        CreateNoWindow = args.Contains("/Q:0"),
        //    };

        //    Process? proc = Process.Start(info);
        //    proc.WaitForExit();
        //    proc.Dispose();

        //    return (StdOut: File.Exists(t_out) ? File.ReadAllText(t_out) : string.Empty, StdErr: File.Exists(t_err) ? File.ReadAllText(t_err) : string.Empty);
        //}

        public (string StdOut, string StdErr) RunPeloton2(string? Exe, string args, string buff, long quietude, DispatcherQueue dispatcher)
        {
            Telemetry.EnableIfMethodNameInFactorySettingsTelemetry(0);

            string temp = System.IO.Path.GetTempFileName();
            File.WriteAllText(temp, buff, Encoding.Unicode);

            Telemetry.Transmit("temp=", temp);

            Telemetry.Transmit("Exe=", Exe, "Args:", args, "Buff=", buff);

            ProcessStartInfo info = new()
            {
                Arguments = $"{args}",
                FileName = Exe,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                RedirectStandardInput = true,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden,
            };

            //inject($"{DateTime.Now:o}(\r");
            Process? proc = Process.Start(info);
            proc.EnableRaisingEvents = true;

            StringBuilder stdout = new();
            StringBuilder stderr = new();

            StreamWriter stream = proc.StandardInput;
            stream.Write(buff);
            stream.Close();

            proc.OutputDataReceived += (object sender, DataReceivedEventArgs e) =>
            {
                if (e.Data != null)
                {
                    stdout.AppendLine(e.Data);
                }
            };
            proc.ErrorDataReceived += (object sender, DataReceivedEventArgs e) =>
            {
                Telemetry.EnableIfMethodNameInFactorySettingsTelemetry();
                stderr.AppendLine(e.Data);
            };

            proc.BeginErrorReadLine();
            proc.BeginOutputReadLine();

            proc.WaitForExit(GetTimeoutInMilliseconds());
            proc.Dispose();

            return (StdOut: stdout.ToString().Trim(), StdErr: stderr.ToString().Trim());
        }
        private int GetTimeoutInMilliseconds()
        {
            long timeout = Type_1_GetVirtualRegistry<long>("ideOps.Timeout");
            int timeoutInMilliseconds = -1;
            switch (timeout)
            {
                case 0:
                    timeoutInMilliseconds = 20 * 1000;
                    break;
                case 1:
                    timeoutInMilliseconds = 100 * 1000;
                    break;
                case 2:
                    timeoutInMilliseconds = 200 * 1000;
                    break;
                case 3:
                    timeoutInMilliseconds = 1000 * 1000;
                    break;
                case 4:
                    timeoutInMilliseconds = -1;
                    break;
            }
            return timeoutInMilliseconds;
        }
    }
}
