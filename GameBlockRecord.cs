using System;

namespace AlexGameParser
{
    public class GameBlockRecord
    {
        public string GameBlockId { get; set; }

        public string Hero { get; set; }

        public decimal CollectedMoney { get; set; }

        public DateTime Time { get; set; }
    }
}