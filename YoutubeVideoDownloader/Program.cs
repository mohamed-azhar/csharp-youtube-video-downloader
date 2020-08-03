using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using YoutubeExplode;
using YoutubeExplode.Videos;
using YoutubeExplode.Videos.Streams;
using YoutubeVideoDownloader.Utilities;

namespace YoutubeVideoDownloader
{
    class Program
    {
        static int _tableWidth = 70;
        static bool _IsVideoIdValid = false;
        static bool _IsValidVideoIdSelected = false;
        static bool _IsValidDirectorySelected = false;

        public static async Task Main(string[] args)
        {
            DisplayIntro();

            var _youtubeClient = new YoutubeClient();

            VideoId videoId; 
            do
            {
                try
                {
                    Console.Write("\nEnter the Video URL/ID: ");
                    videoId = new VideoId(Console.ReadLine());
                    _IsVideoIdValid = true;
                }
                catch (ArgumentException ex)
                {
                    _IsVideoIdValid = false;
                    Console.WriteLine(ex.Message);
                }
            } while (!_IsVideoIdValid);

            

            Console.WriteLine("Fetching streams...\n");

            var streams = await _youtubeClient.Videos.Streams.GetManifestAsync(videoId);
            var video = await _youtubeClient.Videos.GetAsync(videoId);
            DisplayStreamInformation(streams, video);

            Console.WriteLine();
            int selectedVideoOption = 0;
            do
            {
                Console.Write("Select the # you want to download: ");
                int.TryParse(Console.ReadLine(), out int selectedOption);
                selectedVideoOption = selectedOption;

                if (selectedOption > streams.Streams.Count || selectedOption < 1)
                {
                    _IsValidVideoIdSelected = false;
                    Console.WriteLine("Invalid # selected. Make sure you select a valid number from the # column of the above table\n");
                }
                else
                {
                    _IsValidVideoIdSelected = true;
                }

            } while (!_IsValidVideoIdSelected);

            var selectedStreamInfomation = streams.Streams.ToArray()[selectedVideoOption - 1];
            var fileName = $"{video.Title}.{selectedStreamInfomation.Container.Name}";
            var saveDirectoryPath = string.Empty;


            PrintRow("#", "Size", "Format", "Bit Rate");
            PrintRow(selectedVideoOption.ToString(), selectedStreamInfomation.Size.ToString(), selectedStreamInfomation.Container.Name, selectedStreamInfomation.Bitrate.ToString());

            do
            {
                Console.Write("\nDirectory to save the downloaded file (leave blank if current directory): ");
                saveDirectoryPath = Console.ReadLine();

                var directoryValidationResult = ValidateSaveDirectory(saveDirectoryPath);

                if (directoryValidationResult.Item1)
                {
                    _IsValidDirectorySelected = true;
                    saveDirectoryPath = Path.Combine(saveDirectoryPath, fileName);
                }
                else
                {
                    _IsValidDirectorySelected = false;
                    Console.WriteLine(directoryValidationResult.Item2);
                }
            } while (!_IsValidDirectorySelected);

            Console.WriteLine("Downloading File...");

            using var progress = new ProgressIndicator();
 
            await _youtubeClient.Videos.Streams.DownloadAsync(selectedStreamInfomation, saveDirectoryPath, progress);
        }

        static void DisplayIntro()
        {
            Console.Title = "Youtube Video Downloader CLI v1 by mohamed-azhar";
            Console.WriteLine("======================================");
            Console.WriteLine("=    Youtube Video Downloader CLI    =");
            Console.WriteLine("=            mohamed-azhar           =");
            Console.WriteLine("=  https://github.com/mohamed-azhar  =");
            Console.WriteLine("======================================");
        }

        static void DisplayStreamInformation(StreamManifest streamManifest, Video video)
        {
            if (streamManifest != null && video != null && streamManifest.Streams.Count  > 0)
            {
                Console.WriteLine($"{video.Title} by {video.Author} on {video.UploadDate.DateTime.ToLongDateString()}");
                PrintDivider();
                PrintRow("#", "Size", "Format", "Bit Rate");
                PrintDivider();
                PrintDivider();

                for (int i = 0; i < streamManifest.Streams.Count; i++)
                {
                    var stream = streamManifest.Streams[i];
                    PrintRow((i+1).ToString(), stream.Size.ToString(), stream.Container.Name, stream.Bitrate.ToString());
                }
            }
            else
            {
                Console.WriteLine("\nNo streams found for the provided video link");
            }
        }

        static Tuple<bool, string> ValidateSaveDirectory(string directory)
        {
            if (string.IsNullOrWhiteSpace(directory)) return Tuple.Create(true, string.Empty);

            if (!Directory.Exists(directory))
            {
                try
                {
                    Directory.CreateDirectory(directory);
                }
                catch (Exception ex)
                {
                    return Tuple.Create(false, ex.Message);
                }
            }
            return Tuple.Create(true, string.Empty);
        }

        #region Table UI
        static void PrintRow(params string[] columns)
        {
            int width = (_tableWidth - columns.Length) / columns.Length;
            string row = "|";

            foreach (string column in columns)
            {
                row += AlignCentre(column, width) + "|";
            }

            Console.WriteLine(row);
        }

        static string AlignCentre(string text, int width)
        {
            text = text.Length > width ? text.Substring(0, width - 3) + "..." : text;

            if (string.IsNullOrEmpty(text))
            {
                return new string(' ', width);
            }
            else
            {
                return text.PadRight(width - (width - text.Length) / 2).PadLeft(width);
            }
        }

        static void PrintDivider(char drawCharacter = '-')
        {
            Console.WriteLine(new string(drawCharacter, _tableWidth));
        }
        #endregion
    }
}
