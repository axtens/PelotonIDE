namespace PelotonIDE.Presentation
{
    internal class Metas
    {
        public string? Language { get; set; }
        public long LengthSyllable { get; set; }
        public bool Variable { get; set; }
        public long Locale { get; set; }
        public long OpCount { get; set; }
        public long KeyCount { get; set; }
        public long LanguageId { get; set; }
        public string? TextOrientation { get; set; }
    }
    internal class Plex
    {
        public Metas? Meta { get; set; }
        public Dictionary<string, long>? OpcodesByKey { get; set; }
        public Dictionary<long, string>? OpcodesByValue { get; set; }
        public Dictionary<string, long>? SyskeysByKey { get; set; }
        public Dictionary<long, string>? SyskeysByValue { get; set; }
        }
    internal class PlexBlock
    {
        public Plex? Plex { get; set; }
        public string? PlexFile { get; set; }

    }
}