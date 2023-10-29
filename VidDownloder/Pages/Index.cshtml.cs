using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using YoutubeExplode;

namespace VidDownloder.Pages
{
    public class IndexModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;
        private readonly string videoLoc = Environment.CurrentDirectory + "\\wwwroot\\youtubevideos\\";
        [BindProperty]
        public string Texturl { get; set; }

        [BindProperty]
        public bool IsURLReady { get; set; }

        [BindProperty]
        public bool IsError { get; set; }

        public IndexModel(ILogger<IndexModel> logger)
        {
            _logger = logger;
        }


        public void OnGet(string texturl)
        {
            try
            {
                string embede = "https://www.youtube.com/embed/";
                if (!string.IsNullOrEmpty(texturl))
                {
                    IsURLReady = true;
                    if (texturl.Contains("="))
                    {
                        var arr = texturl.Split("=");
                        embede = embede + arr[arr.Length - 1];
                    }
                    else
                    {
                        var arr = texturl.Split("/");
                        embede = embede + arr[arr.Length - 1];
                    }
                    Texturl = embede;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                throw;
            }
        }

        public async Task<IActionResult> OnGetDownload(string texturl, int quality)
        {

            string result = await DownloadYouTubeVideo(texturl, quality);
            if (!string.IsNullOrEmpty(result))
            {
                return Redirect(result);
            }
            IsError = true;
          return BadRequest();  
        }

        private async Task<string> DownloadYouTubeVideo(string videoUrl,int quality)
        {
            var youtube = new YoutubeClient();
            var video = await youtube.Videos.GetAsync(videoUrl);



            // Sanitize the video title to remove invalid characters from the file name
            string sanitizedTitle = string.Join("_", video.Title.Split(Path.GetInvalidFileNameChars()));

            // Get all available muxed streams
            var streamManifest = await youtube.Videos.Streams.GetManifestAsync(video.Id);
            var muxedStreams = streamManifest.GetMuxedStreams().Where(q=>q.VideoQuality.MaxHeight == quality).OrderByDescending(s => s.VideoQuality).ToList();

            if (muxedStreams.Any())
            {
                var streamInfo = muxedStreams.First();
                using var httpClient = new HttpClient();
                var stream = await httpClient.GetStreamAsync(streamInfo.Url);
                var datetime = DateTime.Now;

                string outputFilePath = Path.Combine(videoLoc, $"{sanitizedTitle}.{streamInfo.Container}");
                using var outputStream = System.IO.File.Create(outputFilePath);
                await stream.CopyToAsync(outputStream);

                Console.WriteLine("Download completed!");
                Console.WriteLine($"Video saved as: {outputFilePath}{datetime}");
                return "/youtubevideos/" + $"{sanitizedTitle}.{streamInfo.Container}";
            }
            else
            {
                Console.WriteLine($"No suitable video stream found for {video.Title}.");
            }
            return "";
        }

    }
}