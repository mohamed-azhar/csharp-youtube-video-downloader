using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using YoutubeExplode;
using YoutubeExplode.Videos;
using YoutubeExplode.Videos.Streams;
using YoutubeVideoDownloader.Extensions;
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
            // display intro text
            DisplayIntro();

            VideoId videoId;

            //get video id or url from user
            GetVideoInformationFromInput(ref videoId);

            //prepare youtube client
            var _youtubeClient = new YoutubeClient();

            Console.WriteLine("Fetching streams...\n");

            //get the manifest streams for the video 
            var streamManifest = await _youtubeClient.Videos.Streams.GetManifestAsync(videoId);

            //get the video
            var video = await _youtubeClient.Videos.GetAsync(videoId);

            //display the stream information in a table
            DisplayStreamInformation(streamManifest, video);

            int selectedStreamOption = 0;

            //get the stream number to be downloaded from user input
            GetDownloadStreamNumberFromInput(ref selectedStreamOption, streamManifest?.Streams?.Count ?? 0);

            //find selected stream information
            var selectedStreamInfomation = streamManifest.GetMuxed().ToArray()[selectedStreamOption - 1];

            //prepare filename
            string fileName = $"{video.Title}.{selectedStreamInfomation.Container.Name}";
            if (!ValidateFileName(video.Title)) fileName = $"{video.Title.GenerateSlug()}.{selectedStreamInfomation.Container.Name}";

            //print selected stream information
            PrintRow("#", "Size", "Format", "Bit Rate");
            PrintRow(selectedStreamOption.ToString(), selectedStreamInfomation.Size.ToString(), selectedStreamInfomation.Container.Name, selectedStreamInfomation.Bitrate.ToString());

            var saveDirectoryPath = string.Empty;

            //get the file save directory from user input
            GetSaveDirectoryFromInput(ref saveDirectoryPath, fileName);

            Console.WriteLine("Downloading File...");

            //download the file while showing the progress bar
            using var progress = new ProgressIndicator();
            await _youtubeClient.Videos.Streams.DownloadAsync(selectedStreamInfomation, saveDirectoryPath, progress);
        }

        #region Display Information
        /// <summary>
        /// Displays author information and links
        /// </summary>
        static void DisplayIntro()
        {
            Console.Title = "Youtube Video Downloader CLI v1 by mohamed-azhar";
            Console.WriteLine("=========================================");
            Console.WriteLine("=     Youtube Video Downloader CLI      =");
            Console.WriteLine("=             mohamed-azhar             =");
            Console.WriteLine("=    https://github.com/mohamed-azhar   =");
            Console.WriteLine("=========================================");
        }

        /// <summary>
        /// Display all the streams of a video id in a table fashion
        /// </summary>
        /// <param name="streamManifest"></param>
        /// <param name="video"></param>
        static void DisplayStreamInformation(StreamManifest streamManifest, Video video)
        {
            if (streamManifest != null && video != null && streamManifest.Streams.Count > 0)
            {
                Console.WriteLine($"{video.Title} by {video.Author} on {video.UploadDate.DateTime.ToLongDateString()}");
                PrintDivider();
                PrintRow("#", "Size", "Format", "Bit Rate");
                PrintDivider();
                PrintDivider();

                var toPrint = streamManifest.GetMuxed().ToArray();

                for (int i = 0; i < toPrint.Length; i++)
                {
                    var stream = toPrint[i];
                    PrintRow((i + 1).ToString(), stream.Size.ToString(), stream.Container.Name, stream.Bitrate.ToString());
                }
            }
            else
            {
                Console.WriteLine("\nNo streams found for the provided video link");
            }
        }
        #endregion

        #region User Inputs
        /// <summary>
        /// Asks the user to input the youtube video id or the youtube video url
        /// </summary>
        /// <param name="videoId">user input will be validated and binded to this</param>
        static void GetVideoInformationFromInput(ref VideoId videoId)
        {
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
        }

        /// <summary>
        /// Get the stream number which needs to be downloaded from the stream list of the youtube video id or youtube video url provided
        /// </summary>
        /// <param name="selectedStreamOption">a reference to the selected stream option</param>
        /// <param name="streamCount">the number of streams from the stream manifest</param>
        static void GetDownloadStreamNumberFromInput(ref int selectedStreamOption, int streamCount = 0)
        {
            do
            {
                Console.Write("\nSelect the # you want to download: ");
                int.TryParse(Console.ReadLine(), out int selectedOption);
                selectedStreamOption = selectedOption;

                if (selectedOption > streamCount || selectedOption < 1)
                {
                    _IsValidVideoIdSelected = false;
                    Console.WriteLine("Invalid # selected. Make sure you select a valid number from the # column of the above table\n");
                }
                else
                {
                    _IsValidVideoIdSelected = true;
                }

            } while (!_IsValidVideoIdSelected);
        }

        /// <summary>
        /// Gets the save directory for the download file from the user
        /// </summary>
        /// <param name="directoryPath">a reference to the directory path</param>
        /// <param name="fileName">file name of the download file</param>
        static void GetSaveDirectoryFromInput(ref string directoryPath, string fileName)
        {
            do
            {
                Console.Write("\nDirectory to save the downloaded file (leave blank if current directory): ");
                directoryPath = Console.ReadLine();

                var directoryValidationResult = ValidateSaveDirectory(directoryPath);

                if (directoryValidationResult.Item1)
                {
                    _IsValidDirectorySelected = true;
                    directoryPath = Path.Combine(directoryPath, fileName);
                }
                else
                {
                    _IsValidDirectorySelected = false;
                    Console.WriteLine(directoryValidationResult.Item2);
                }
            } while (!_IsValidDirectorySelected);
        }
        #endregion

        #region Validations
        /// <summary>
        /// Validates a given save directory path
        /// </summary>
        /// <param name="directory">name of the directory path which needs to be validated</param>
        /// <returns>returns a tuple countaining information about whether the directory is valid or not and error messages</returns>
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

        /// <summary>
        /// Validates a given file name. Currently validated against windows file system naming conventions
        /// </summary>
        /// <param name="name">name of the file which is going to be validated</param>
        /// <returns>returns if the validation succeeded or not</returns>
        static bool ValidateFileName(string name)
        {
            Regex containsABadCharacter = new Regex("[" + Regex.Escape(string.Join("", Path.GetInvalidFileNameChars())) + "]");
            if (containsABadCharacter.IsMatch(name)) return false;
            return true;
        }
        #endregion

        #region Table UI
        /// <summary>
        /// Draws a single table row 
        /// </summary>
        /// <param name="columns">columns to be drawn</param>
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

        /// <summary>
        /// Aligning text in a column
        /// </summary>
        /// <param name="text">text of the colum</param>
        /// <param name="width">width of the column</param>
        /// <returns></returns>
        static string AlignCentre(string text, int width)
        {
            text = text.Length > width ? text.Substring(0, width - 3) + "..." : text;

            if (string.IsNullOrEmpty(text)) return new string(' ', width);
            else return text.PadRight(width - (width - text.Length) / 2).PadLeft(width);
        }

        /// <summary>
        /// Prints a divider in the table
        /// </summary>
        /// <param name="drawCharacter">character representing the divider</param>
        static void PrintDivider(char drawCharacter = '-') => Console.WriteLine(new string(drawCharacter, _tableWidth));
        #endregion
    }
}
