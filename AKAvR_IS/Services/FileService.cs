using AKAvR_IS.Classes.FileInfo;
using AKAvR_IS.Interfaces.IFileInfo;
using AKAvR_IS.Interfaces.IFileService;

namespace AKAvR_IS.Services
{
    public class FileService : IFileService
    {
        private readonly string _uploadPath;
        private readonly ILogger<FileService> _logger;
        private readonly IFileStorageConfig _config;

        public FileService(IConfiguration configuration, ILogger<FileService> logger)
        {
            _config = configuration.GetSection("FileStorage")
                .Get<IFileStorageConfig>() ?? new FileStorageConfig();
            
            _logger = logger;

            if (string.IsNullOrWhiteSpace(_config.BasePath))
            {
                _config.BasePath = configuration.GetSection("PythonExecutorConfig:WorkingDirectory")
                    .Value ?? Directory.GetCurrentDirectory();
            }

            _uploadPath = Path.Combine(_config.BasePath, _config.ExamplesFolder);

            if (!Directory.Exists(_uploadPath))
            {
                Directory.CreateDirectory(_uploadPath);
            }
        }

        public async Task<IUploadResult> UploadCsvFileAsync(IFormFile file)
        {
            try
            {
                if (file == null || file.Length == 0)
                {
                    return new UploadResult
                    {
                        Success = false,
                        Message = "The file is not provided or is empty"
                    };
                }

                var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
                if (fileExtension != ".csv")
                {
                    return new UploadResult
                    {
                        Success = false,
                        Message = "Only CSV files are allowed"
                    };
                }

                var fileName = $"{Guid.NewGuid()}_{DateTime.Now:yyyyMMddHHmmss}{fileExtension}";
                var filePath = Path.Combine(_uploadPath, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                var fileInfo = new FileInfoDto
                {
                    FileName    = fileName,
                    ContentType = file.ContentType,
                    Size        = file.Length,
                    UploadDate  = DateTime.UtcNow
                };

                _logger.LogInformation($"File {fileName} successfully uploaded. Size: {file.Length} bytes");

                return new UploadResult
                {
                    Success  = true,
                    Message  = "File uploaded successfully",
                    FileInfo = fileInfo
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error uploading file {file?.FileName}");
                return new UploadResult
                {
                    Success = false,
                    Message = $"Error uploading file: {ex.Message}"
                };
            }
        }

        public async Task<(byte[] fileBytes, string fileName, string contentType)> DownloadFileAsync(string fileName)
        {
            try
            {
                var filePath = Path.Combine(_uploadPath, fileName);

                if (!File.Exists(filePath))
                {
                    throw new FileNotFoundException($"File {fileName} not found");
                }

                var fileBytes = await File.ReadAllBytesAsync(filePath);
                var contentType = GetContentType(fileName);

                return (fileBytes, fileName, contentType);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error uploading file {fileName}");
                throw;
            }
        }

        public async Task<IEnumerable<IFileInfoDto>> GetUploadedFilesInfoAsync()
        {
            var files = Directory.GetFiles(_uploadPath, "*.csv");
            var fileInfos = new List<FileInfoDto>();

            foreach (var filePath in files)
            {
                var fileInfo = new FileInfo(filePath);
                fileInfos.Add(new FileInfoDto
                {
                    FileName   = Path.GetFileName(filePath),
                    Size       = fileInfo.Length,
                    UploadDate = fileInfo.CreationTimeUtc
                });
            }

            return await Task.FromResult(fileInfos);
        }

        public async Task<bool> DeleteFileAsync(string fileName)
        {
            try
            {
                var filePath = Path.Combine(_uploadPath, fileName);

                if (!File.Exists(filePath))
                {
                    return false;
                }

                File.Delete(filePath);
                _logger.LogInformation($"File {fileName} deleted");
                return await Task.FromResult(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting file {fileName}");
                return false;
            }
        }

        private string GetContentType(string fileName)
        {
            var extension = Path.GetExtension(fileName).ToLowerInvariant();
            return extension switch
            {
                ".csv" => "text/csv",
                _ => "application/octet-stream"
            };
        }
    }
}
