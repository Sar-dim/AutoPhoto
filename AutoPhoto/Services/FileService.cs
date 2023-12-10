using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Shapes;

using static System.Net.Mime.MediaTypeNames;

namespace AutoPhoto.Services
{
    public static class FileService
    {
        private const string DataFile = "data.txt";

        public static void SaveToFile(DataForFile dataForFile)
        {
            string json = JsonSerializer.Serialize(dataForFile);
            using (StreamWriter writer = new StreamWriter(DataFile, false))
            {
                writer.WriteLine(json);
            }
        }

        public static DataForFile ReadFromFile()
        {
            using (StreamReader reader = new StreamReader(DataFile))
            {
                string json = reader.ReadToEnd();
                return JsonSerializer.Deserialize<DataForFile>(json);
            }
        }
    }

    public class DataForFile
    {
        public string HPPotionPixelX { get; set; }
        public string PotionCountX { get; set; }
        public string PotionCountY { get; set; }
        public string HPPotionPixelY { get; set; }
        public string PotionDelay { get; set; }
        public string TeleportPixelX { get; set; }
        public string TeleportPixelY { get; set; }
        public string TeleportDelay { get; set; }
        public bool IsSwitchToR2 { get; set; }
        public bool IsDuplicateSounds { get; set; }
        public string GamePath { get; set; }
    }
}
