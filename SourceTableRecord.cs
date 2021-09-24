using System;

namespace AlexGameParser
{
    public class SourceTableRecord
    {
        private const string PanelTimeFormat = "MM/dd, HH:mm";
        public DateTime Time { get; set; }

        public string GameBlockId { get; set; }

        public string RealNickname { get; set; }

        public override string ToString()
        {
            return $"{Time.ToString(PanelTimeFormat)} {RealNickname} {GameBlockId}";
        }
    }
}
