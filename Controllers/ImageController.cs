using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace panelOrmo.Controllers
{
    public class ImageController : Controller
    {
        private readonly IConfiguration _configuration;

        public ImageController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [HttpGet]
        [Route("Image/{imageType}/{fileName}")]
        public async Task<IActionResult> GetImage(string imageType, string fileName)
        {
            try
            {
                // Debug logging
                Console.WriteLine($"Requesting image - Type: {imageType}, File: {fileName}");

                var ftpHost = _configuration["FTP:Host"];
                var ftpUsername = _configuration["FTP:Username"];
                var ftpPassword = _configuration["FTP:Password"];

                // Construct FTP path based on image type
                string ftpPath;
                switch (imageType.ToLower())
                {
                    case "small":
                        ftpPath = $"ftp://{ftpHost}/httpdocs/CMSFiles/ProductImages/SmallImage/{fileName}";
                        break;
                    case "medium":
                        ftpPath = $"ftp://{ftpHost}/httpdocs/CMSFiles/ProductImages/MediumImage/{fileName}";
                        break;
                    default:
                        // Log the imageType for debugging
                        Console.WriteLine($"Unknown image type: {imageType}");
                        return NotFound($"Unknown image type: {imageType}");
                }

                // Debug logging
                Console.WriteLine($"FTP Path: {ftpPath}");

                // Download image from FTP
                var request = (FtpWebRequest)WebRequest.Create(ftpPath);
                request.Method = WebRequestMethods.Ftp.DownloadFile;
                request.Credentials = new NetworkCredential(ftpUsername, ftpPassword);

                using var response = (FtpWebResponse)await request.GetResponseAsync();
                using var stream = response.GetResponseStream();
                using var memoryStream = new MemoryStream();

                await stream.CopyToAsync(memoryStream);
                var imageBytes = memoryStream.ToArray();

                // Return image with appropriate content type
                return File(imageBytes, "image/jpeg");
            }
            catch (Exception ex)
            {
                // Better error logging
                Console.WriteLine($"Error loading image {imageType}/{fileName}: {ex.Message}");
                // Return a placeholder image or 404
                return NotFound($"Image not found: {ex.Message}");
            }
        }
    }
}