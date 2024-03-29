﻿using DocumentFormat.OpenXml.Drawing;
using DocumentFormat.OpenXml.Office2016.Drawing.ChartDrawing;
using DocumentFormat.OpenXml.Wordprocessing;

using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml.Documents;

using System.Diagnostics;
using System.Text;

using Uno.Extensions;

using Windows.Devices.PointOfService;
using Windows.Storage;
using Windows.UI.Core;

using Paragraph = Microsoft.UI.Xaml.Documents.Paragraph;
using Run = Microsoft.UI.Xaml.Documents.Run;
using System.Linq;

namespace PelotonIDE.Presentation
{
    public sealed partial class MainPage : Page
    {
        private async void ExecuteInterpreter(string selectedText)
        {
            Telemetry t = new();
            t.SetEnabled(true);

            DispatcherQueue dispatcher = DispatcherQueue.GetForCurrentThread();

            if (Type_3_GetInFocusTab<long>("Quietude") == 0 && Type_3_GetInFocusTab<long>("Timeout") > 0)
            {
                // Yes, No, Cancel

                //Task<int> sure = AreYouSureYouWantToRunALongTimeSilently();
                //sure.ContinueWith(t => t);
                //if (sure.Result == 2) return;
                //if (sure.Result == 1)
                //    Type_3_UpdateInFocusTabSettings<long>("Quietude", true, 1);
                // Task<ContentDialogResult> task = AreYouSureYouWantToRunALongTimeSilently();
                // task.Wait();
                if (!await AreYouSureYouWantToRunALongTimeSilently())
                {
                    return;
                }
            }

            t.Transmit("selectedText=", selectedText);
            // load tab settings


            CustomTabItem navigationViewItem = (CustomTabItem)tabControl.SelectedItem;
            long quietude = (long)navigationViewItem.TabSettingsDict["Quietude"]["Value"];
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
            if (ApplicationData.Current.LocalSettings.Values["Engine"].ToString() == "Interpreter.P3")
            {
                (stdOut, stdErr) = RunPeloton2(engineArguments, selectedText, quietude, dispatcher);
            }
            else
            {
                (stdOut, stdErr) = RunProtium(engineArguments, selectedText, quietude);
            }

            t.Transmit("stdOut=", stdOut, "stdErr=", stdErr);

            IEnumerable<long> rendering = Type_3_GetInFocusTab<string>("Rendering").Split([',']).Select(e => long.Parse(e));

            IEnumerable<string> list = (from item in RenderingConstants["Rendering"]
                                        where rendering.Contains((long)item.Value)
                                        select item.Key);

            foreach (var item in list)
            {
                switch (item)
                {
                    case "Output":
                        if (!string.IsNullOrEmpty(stdOut))
                        {
                            AddInsertParagraph(outputText, stdOut, false);
                        }
                        break;
                    case "Error":
                        if (!string.IsNullOrEmpty(stdErr))
                        {
                            AddInsertParagraph(errorText, stdErr, false);
                        }
                        break;
                    case "Html":
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
                            //AddInsertParagraph(rtfText, stdOut, false);
                            rtfText.Document.SetText(Microsoft.UI.Text.TextSetOptions.FormatRtf, stdOut);
                        }
                        break;
                    case "Logo":
                        if (!string.IsNullOrEmpty(stdOut))
                        {
                            StorageFolder folder = ApplicationData.Current.LocalFolder;
                            StorageFile file = await folder.CreateFileAsync("temp.logo", CreationCollisionOption.ReplaceExisting);
                            List<string> lines = [.. stdOut.Split("\r\n", StringSplitOptions.RemoveEmptyEntries)];
                            await FileIO.WriteTextAsync(file, string.Join("\n", lines));
                            string jsBlock = ParseLogoIntoJavascript(await FileIO.ReadTextAsync(file));
                            file = await folder.CreateFileAsync("temp.html", CreationCollisionOption.ReplaceExisting);
                            await FileIO.WriteTextAsync(file, TurtleFrameworkPlus(jsBlock));
                            LogoText.Source = new Uri(file.Path);

                        }
                        break;
                }
            }

        }

        private string TurtleFrameworkPlus(string jsBlock)
        {
            return $@"<script type=""text/javascript"" src=""https://unpkg.com/real-turtle""></script>
<canvas id=""real-turtle""></canvas>
<script type=""text/javascript"" src=""https://unpkg.com/real-turtle/build/helpers/simple.js""></script>

<script type=""text/javascript"">
{jsBlock}
</script>";
        }

        private string ParseLogoIntoJavascript(string v)
        {
            Telemetry t = new();
            t.SetEnabled(true);
            
            List<string> result = [];
            var lines = v.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var line in lines)
            {
                var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length > 0)
                {
                    switch (parts[0].ToUpper())
                    {
                        case ";":
                            break;
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
            t.Transmit(result.JoinBy("\r\n"));
            return result.JoinBy("\n");
        }

        public void AddOutput(string text)
        {
            AddInsertParagraph(outputText, text, true, false);
        }

        public void AddError(string text)
        {
            AddInsertParagraph(errorText, text, true, false);
        }

        private static void AddInsertParagraph(RichEditBox reb, string text, bool addInsert = true, bool withPrefix = true)
        {
            Telemetry t = new();
            t.SetEnabled(true);
            if (string.IsNullOrEmpty(text))
            {
                return;
            }
            t.Transmit("text=", text, "addInsert=", addInsert, "withPrefix=", withPrefix);
            const string stamp = "> ";
            if (withPrefix)
                text = text.Insert(0, stamp);

            //reb.IsReadOnly = false;
            reb.Document.GetText(Microsoft.UI.Text.TextGetOptions.UseLf, out string? tx);
            if (addInsert)
            {
                reb.Document.SetText(Microsoft.UI.Text.TextSetOptions.None, tx + "\n" + text);
                //reb.Document.GetRange(t.Length, t.Length).Text = t;
            }
            else
            {
                //reb.Document.GetRange(0, 0).Text = t;
                reb.Document.SetText(Microsoft.UI.Text.TextSetOptions.None, text + "\n" + tx);
            }
            reb.Focus(FocusState.Programmatic);
            //reb.IsReadOnly = true;
        }

        public (string StdOut, string StdErr) RunProtium(string args, string buff, long quietude)
        {
            Telemetry t = new();
            t.SetEnabled(true);
            string? Exe = ApplicationData.Current.LocalSettings.Values["Interpreter.P2"].ToString();
            string temp = System.IO.Path.GetTempFileName();
            File.WriteAllText(temp, buff, Encoding.Unicode);

            args = args.Replace(":", "=");

            args += $" /F:\"{temp}\"";

            t.Transmit("Exe=", Exe, "Args:", args, "Buff=", buff, "Quietude=", quietude);

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

        public (string StdOut, string StdErr) RunPeloton(string args, string buff, long quietude)
        {
            Telemetry t = new();
            t.SetEnabled(true);

            string? Exe = ApplicationData.Current.LocalSettings.Values["Interpreter.P3"].ToString();

            t.Transmit("Exe=", Exe, "Args:", args, "Buff=", buff, "Quietude=", quietude);

            string t_in = System.IO.Path.GetTempFileName();
            string t_out = System.IO.Path.ChangeExtension(t_in, "out");
            string t_err = System.IO.Path.ChangeExtension(t_in, "err");

            File.WriteAllText(t_in, buff);

            //args = args.Replace(":", "=");

            args += $" /F:\"{t_in}\""; // 1>\"{t_out}\" 2>\"{t_err}\"";

            t.Transmit(args, buff);

            ProcessStartInfo info = new()
            {
                Arguments = $"{args}",
                FileName = Exe,
                UseShellExecute = false,
                CreateNoWindow = args.Contains("/Q:0"),
            };

            Process? proc = Process.Start(info);
            proc.WaitForExit();
            proc.Dispose();

            return (StdOut: File.Exists(t_out) ? File.ReadAllText(t_out) : string.Empty, StdErr: File.Exists(t_err) ? File.ReadAllText(t_err) : string.Empty);
        }

        public void Inject(string? arg)
        {
            //outputText.IsReadOnly = false;
            outputText.Document.GetText(Microsoft.UI.Text.TextGetOptions.AdjustCrlf, out string? value);
            outputText.Document.SetText(Microsoft.UI.Text.TextSetOptions.None, $"{value}{arg}");
            //outputText.IsReadOnly = true;
        }
        public (string StdOut, string StdErr) RunPeloton2(string args, string buff, long quietude, DispatcherQueue dispatcher)
        {
            Telemetry t = new();
            t.SetEnabled(true);

            string temp = System.IO.Path.GetTempFileName();
            File.WriteAllText(temp, buff, Encoding.Unicode);

            t.Transmit("temp=", temp);

            string? Exe = ApplicationData.Current.LocalSettings.Values["Interpreter.P3"].ToString();

            t.Transmit("Exe=", Exe, "Args:", args, "Buff=", buff, "Quietude=", quietude);

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
                //if (quietude == 0)
                //{
                //if (e.Data != null)
                //    stdout.AppendLine(e.Data);
                //}
                //else
                //{
                if (e.Data != null)
                {
                    //dispatcher.TryEnqueue(() =>
                    //{
                    //    //Inject($"{DateTime.Now:o}> {e.Data}\r");
                    //    Inject($"> {e.Data}\r");
                    //});
                    stdout.AppendLine(e.Data);
                }
                //}
            };
            proc.ErrorDataReceived += (object sender, DataReceivedEventArgs e) =>
            {
                Telemetry t = new();
                t.SetEnabled(true);
                //t.Transmit(e.Data);
                stderr.AppendLine(e.Data);
            };

            proc.BeginErrorReadLine();
            proc.BeginOutputReadLine();

            proc.WaitForExit(GetTimeoutInMilliseconds());
            proc.Dispose();

            //inject($"){DateTime.Now:o}\r");

            return (StdOut: stdout.ToString().Trim(), StdErr: stderr.ToString().Trim());
        }

        private int GetTimeoutInMilliseconds()
        {
            long timeout = Type_1_GetVirtualRegistry<long>("Timeout");
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
