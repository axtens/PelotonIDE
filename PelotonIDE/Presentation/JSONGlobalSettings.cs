namespace PelotonIDE.Presentation
{
    public class JSONGlobalSettings
    {
        public string? Language { get; set; }
        public long LanguageID { get; set; }
        public string? OutputPanelPosition { get; set; }
        public string? DefaultTabBackground { get; set; }
        public string? Engine { get; set; }
        public string? PelotonCMDLine { get; set; }
        public int OutputHeight { get; set; }
        public int OutputWidth { get; set; }
        public bool OutputPanelShowing { get; set; }
        public string? Scripts {  get; set; }
    }

    public class NavigationData
    {
        public string? Source { get; set; }
        public Dictionary<string, object>? KVPs { get; set; }
    }
}
