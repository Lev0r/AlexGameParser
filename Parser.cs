using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.Xml;
using CsvHelper;

namespace AlexGameParser
{
    public partial class Parser : Form
    {
        private readonly List<SourceTableRecord> _sourceTableRecords;
        private readonly List<ParsedGameFile> _parsedGameFiles;

        public Parser()
        {
            _sourceTableRecords = new List<SourceTableRecord>();
            _parsedGameFiles = new List<ParsedGameFile>();

            InitializeComponent();
            panel1.DragEnter += LeftPaneLDragEnter;
            panel1.DragDrop += LeftPanelDragDrop;

            panel2.DragEnter += RightPaneLDragEnter;
            panel2.DragDrop += RightPanelDragDrop;
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            base.OnFormClosing(e);

            panel1.DragEnter -= LeftPaneLDragEnter;
            panel1.DragDrop -= LeftPanelDragDrop;

            panel2.DragEnter -= RightPaneLDragEnter;
            panel2.DragDrop -= RightPanelDragDrop;
        }

        private void RightPanelDragDrop(object sender, DragEventArgs e)
        {
            var files = (string[])e.Data.GetData(DataFormats.FileDrop);
            if (files.Length != 1)
            {
                MessageBox.Show(@"Specify exactly 1 table file");
                return; ;
            }

            var filePath = files[0];

            if (Path.GetExtension(filePath) != ".csv")
            {
                MessageBox.Show(@"Support only csv files. Make sure file path have no russian letters");
                return;
            }

            ReadTableFile(filePath);
            PrintToPanel(panel2, _sourceTableRecords.Select(str => str.ToString()));
        }

        private void RightPaneLDragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop)) e.Effect = DragDropEffects.Copy;
        }

        private void LeftPanelDragDrop(object sender, DragEventArgs e)
        {
            var files = (string[])e.Data.GetData(DataFormats.FileDrop);

            _parsedGameFiles.Clear();
            PrintToPanel(panel1, files.Select(Path.GetFileName));
            foreach (var file in files)
            {
                _parsedGameFiles.Add(ReadGameFile(file));
            }
        }

        private void LeftPaneLDragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop)) e.Effect = DragDropEffects.Copy;
        }

        private static ParsedGameFile ReadGameFile(string filePath)
        {
            var blockBeginRegex = new Regex(@"^Poker Hand #(.*): .*? - (.*)");
            var collectedRegex = new Regex(@"(.*) collected \$([0-9]*\.?[0-9])");
            var showDownSectionRegex = new Regex(@"\*\*\* .*SHOWDOWN.*\*\*\*");
            var summarySectionRegex = new Regex(@"\*\*\* .*SUMMARY.*\*\*\*");


            var fileContent = new StringBuilder();
            var parseResult = new List<GameBlockRecord>();

            using (var fStream = new StreamReader(filePath))
            {
                string currentLine;

                var isInsideGameBlock = false;
                var isInShowDownSection = false;
                var isMultipleShowdowns = false;

                var currentGameBlock = new GameBlockRecord();

                while ((currentLine = fStream.ReadLine()) != null)
                {
                    fileContent.AppendLine(currentLine);
                    if (!isInsideGameBlock)
                    {
                        var match = blockBeginRegex.Match(currentLine);
                        if (match.Success)
                        {
                            isInsideGameBlock = true;
                            currentGameBlock.GameBlockId = match.Groups[1].Value;
                        }
                    }
                    else
                    {
                        if (isInShowDownSection)
                        {
                            if (showDownSectionRegex.IsMatch(currentLine))
                            {
                                isMultipleShowdowns = true;
                            }

                            if (summarySectionRegex.IsMatch(currentLine))
                            {
                                if (!isMultipleShowdowns)
                                    parseResult.Add(currentGameBlock);

                                isInsideGameBlock = false;
                                isInShowDownSection = false;
                                isMultipleShowdowns = false;
                                currentGameBlock = new GameBlockRecord();
                            }

                            var match = collectedRegex.Match(currentLine);
                            if (match.Success)
                            {
                                currentGameBlock.Hero = match.Groups[1].Value;
                            }
                        }
                        else if (showDownSectionRegex.IsMatch(currentLine))
                        {
                            isInShowDownSection = true;
                        }
                    }
                }
            }

            return new ParsedGameFile
            {
                FileContent = fileContent.ToString(),
                GameRecords = parseResult,
                FileName = filePath
            };
        }

        private void ReadTableFile(string filePath)
        {
            _sourceTableRecords.Clear();
            using (var fStream = new StreamReader(filePath))
            {
                using (var csv = new CsvReader(fStream, CultureInfo.InvariantCulture))
                {
                    while (csv.Read())
                    {
                        var record = new SourceTableRecord
                        {
                            RealNickname = csv.GetField(4),
                            GameBlockId = csv.GetField(1)
                        };
                        _sourceTableRecords.Add(record);
                    }
                }
            }
        }

        private static void PrintToPanel(Panel panel, IEnumerable<string> strings)
        {
            var prevY = 0;
            panel.Controls.Clear();
            foreach (var s in strings)
            {
                panel.Controls.Add(new Label
                {
                    Text = s,
                    Location = new Point(0, prevY),
                    AutoSize = true
                });
                prevY += 21;
            }
        }

        /// <summary>
        /// Process files button click
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button1_Click(object sender, EventArgs e)
        {
            foreach (var parsedGameFile in _parsedGameFiles)
            {
                foreach (var gameRecord in parsedGameFile.GameRecords)
                {
                    var realNicknames = _sourceTableRecords
                        .Where(str =>
                            str.GameBlockId == gameRecord.GameBlockId)
                        .Select(str => str.RealNickname)
                        .ToList();

                    if (!realNicknames.Any() || realNicknames.Count() > 1)
                        continue;

                    var realNickname = realNicknames.Single();

                    parsedGameFile.FileContent = parsedGameFile.FileContent.Replace(gameRecord.Hero, realNickname);
                }

                var newFileName = $"result_{Path.GetFileName(parsedGameFile.FileName)}";
                var dirName = Path.GetDirectoryName(parsedGameFile.FileName);
                File.WriteAllText($"{dirName}\\{newFileName}", parsedGameFile.FileContent);
            }
        }
    }
}
