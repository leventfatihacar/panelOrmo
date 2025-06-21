using System.Net;

namespace panelOrmo.Services
{
    public class FTPService
    {
        private readonly string _ftpHost;
        private readonly string _ftpUsername;
        private readonly string _ftpPassword;

        public FTPService(IConfiguration configuration)
        {
            _ftpHost = configuration["FTP:Host"];
            _ftpUsername = configuration["FTP:Username"];
            _ftpPassword = configuration["FTP:Password"];
        }

        public async Task<string> UploadFile(IFormFile file, string directory)
        {
            if (file == null || file.Length == 0)
                return null;

            var fileName = $"{Guid.NewGuid()}.jpg";
            var ftpPath = $"ftp://{_ftpHost}{directory}/{fileName}";

            try
            {
                var request = (FtpWebRequest)WebRequest.Create(ftpPath);
                request.Method = WebRequestMethods.Ftp.UploadFile;
                request.Credentials = new NetworkCredential(_ftpUsername, _ftpPassword);

                using var fileStream = file.OpenReadStream();
                using var ftpStream = request.GetRequestStream();
                await fileStream.CopyToAsync(ftpStream);

                return fileName;
            }
            catch (Exception ex)
            {
                // Log error
                return null;
            }
        }
        public async Task<byte[]?> DownloadFile(string filePath)
        {
            try
            {
                var ftpPath = $"ftp://{_ftpHost}{filePath}";

                var request = (FtpWebRequest)WebRequest.Create(ftpPath);
                request.Method = WebRequestMethods.Ftp.DownloadFile;
                request.Credentials = new NetworkCredential(_ftpUsername, _ftpPassword);

                using var response = (FtpWebResponse)await request.GetResponseAsync();
                using var stream = response.GetResponseStream();
                using var memoryStream = new MemoryStream();

                await stream.CopyToAsync(memoryStream);
                return memoryStream.ToArray();
            }
            catch (Exception)
            {
                return null;
            }
        }

        public async Task<bool> FileExists(string filePath)
        {
            try
            {
                var ftpPath = $"ftp://{_ftpHost}{filePath}";

                var request = (FtpWebRequest)WebRequest.Create(ftpPath);
                request.Method = WebRequestMethods.Ftp.GetFileSize;
                request.Credentials = new NetworkCredential(_ftpUsername, _ftpPassword);

                using var response = (FtpWebResponse)await request.GetResponseAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
