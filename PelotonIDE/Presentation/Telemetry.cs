using System.Diagnostics;
using System.Text;

using Windows.Storage;

namespace PelotonIDE.Presentation
{
    public static class Telemetry
    {
        private static Dictionary<string, object>? factorySettings;
        private static string theYou = string.Empty;

        public static string GetTheYou()
        {
            return theYou;
        }

        private static void SetTheYou(string value)
        {
            theYou = value;
        }

        public static Dictionary<string, object>? GetFactorySettings()
        {
            return factorySettings;
        }

        public static void SetFactorySettings(Dictionary<string, object>? value)
        {
            factorySettings = value;
        }

        public static Dictionary<string, bool>? InModuleEnabled = [];
        private static bool firsted = false;

        public static bool GetEnabled()
        {
            System.Diagnostics.StackTrace trace = new System.Diagnostics.StackTrace();
            string you = Before(After(new StackFrame(1).GetMethod().DeclaringType.Name.ToString(), "<"), ">");
            if (InModuleEnabled.ContainsKey(you))
            {
                return InModuleEnabled[you];
            }
            return false;
        }
        public static void SetEnabled(bool value)
        {
            System.Diagnostics.StackTrace trace = new System.Diagnostics.StackTrace();
            string you = Before(After(new StackFrame(1).GetMethod().DeclaringType.Name.ToString(), "<"), ">");
            SetTheYou(you);
            InModuleEnabled[you] = value;
        }
        public static void Transmit(params object?[] args)
        {
            //System.Diagnostics.StackTrace trace = new System.Diagnostics.StackTrace();
            //string you = Before(After(new StackFrame(1).GetMethod().DeclaringType.Name.ToString(), "<"), ">");
            //SetTheYou(you);

            string you  = GetTheYou();
            InModuleEnabled.TryGetValue(you, out bool isEnabled);
            if (!isEnabled) { return; }

            StorageFolder folder = ApplicationData.Current.LocalFolder;
            string path = Path.Combine(folder.Path, $"{DateTime.Now:yyyy-MM-dd-HH}_pi.log");

            StringBuilder sb = new();
            if (!firsted)
            {
                sb.Append("---");
                firsted = true;
                File.AppendAllText(path, $"{DateTime.Now:o} > {sb}\r\n", Encoding.UTF8);
            }
            sb.Clear();
            sb.Append($"From {you}: ");
            for (int i = 0; i < args.Length; i++)
            {
                string item = $"{args[i]}";
                if (i == 0)
                {
                    sb.Append(item);
                }
                else
                {
                    string prev = $"{args[i - 1]}";
                    if (prev.EndsWith("="))
                    {
                        sb.Append(item);
                    }
                    else
                    {
                        sb.Append(' ').Append(item);
                    }
                }
            }
            File.AppendAllText(path, $"{DateTime.Now:o} > {sb}\r\n", Encoding.UTF8);
            return;
        }

        internal static void EnableIfMethodNameInFactorySettingsTelemetry(int depth = 1)
        {
            StackTrace stackTrace = new StackTrace();
            for ( int fc = 0; fc < stackTrace.FrameCount; fc++ )
            {
                StackFrame? frame = stackTrace.GetFrame(fc);
                string you = Before(After(frame.GetMethod().Name.ToString(), "<"), ">");
                //Debug.WriteLine($"{fc}: {you}");
                if (factorySettings != null && factorySettings["ideOps.Telemetry"].ToString().Contains(you))
                {
                    InModuleEnabled[you] = true;
                    SetTheYou(you);
                    //Debug.WriteLine($"{fc}: {you} found in {factorySettings["ideOps.Telemetry"]}");
                    break;
                }
            }
        }

        private static string Before(string name, string delim)
        {
            int b = name.IndexOf(delim);
            return b == -1 ? name : name.Substring(0, b);
        }

        private static string After(string name, string delim)
        {
            int a = name.LastIndexOf(delim);
            return a == -1 ? name : name.Substring(a + delim.Length);
        }
    }
}
