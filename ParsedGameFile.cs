using System.Collections.Generic;

namespace AlexGameParser
{
    public class ParsedGameFile
    {
        public string FileName { get; set; }

        public string FileContent { get; set; }

        public List<GameBlockRecord> GameRecords { get; set; }
    }
}
