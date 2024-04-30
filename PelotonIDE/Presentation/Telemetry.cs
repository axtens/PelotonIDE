using System.Text;

using Windows.Storage;

namespace PelotonIDE.Presentation
{
    public static class Telemetry
    {
        private static Dictionary<string,bool> InModuleEnabled = new();
        private static bool firsted = false;
        //public static bool GetEnabled()
        //{
        //    System.Diagnostics.StackTrace trace = new System.Diagnostics.StackTrace();
        //    string you = trace.GetFrame(1).GetMethod().Name;
        //    if (InModuleEnabled.ContainsKey(you))
        //    {
        //        return InModuleEnabled[you];
        //    }
        //    return false;
        //}
        public static void SetEnabled(bool value)
        {
            System.Diagnostics.StackTrace trace = new System.Diagnostics.StackTrace();
            string you = trace.GetFrame(1).GetMethod().Name;
            InModuleEnabled[you] = value;
        }
        public static void Transmit(params object?[] args)
        {
            System.Diagnostics.StackTrace trace = new System.Diagnostics.StackTrace();
            string you = trace.GetFrame(1).GetMethod().Name;

            if (!InModuleEnabled.ContainsKey(you) || !InModuleEnabled[you]) return;
            StorageFolder folder = ApplicationData.Current.LocalFolder;
            string path = Path.Combine(folder.Path, $"{DateTime.Now:yyyy-MM-dd-HH}_pi.log");

            //List<string> frame = [];

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
    }
}
