using AKAVER_Server.Classes.FileInfo;
using AKAVER_Server.Classes.User;
using AKAVER_Server.Contexts;
using AKAVER_Server.Interfaces.IFileInfo;
using AKAVER_Server.Interfaces.IFileService;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Security.Claims;

namespace AKAVER_Server.Services
{
    public class FileService : IFileService
    {
        private readonly ApplicationDbContext _context;
        private readonly string               _uploadPath;
        private readonly string               _downloadPath;
        private readonly ILogger<FileService> _logger;
        private readonly IFileStorageConfig   _config;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public FileService(
            ApplicationDbContext context,
            IConfiguration configuration,
            ILogger<FileService> logger,
            IHttpContextAccessor httpContextAccessor,
            IOptions<FileStorageConfig> configOptions)
        {
            _context = context;
            _config = configOptions.Value;

            _logger = logger;
            _httpContextAccessor = httpContextAccessor;

            if (string.IsNullOrWhiteSpace(_config.BasePath))
            {
                _config.BasePath = configuration.GetSection("PythonExecutorConfig:WorkingDirectory")
                    .Value ?? Directory.GetCurrentDirectory();
            }

            _uploadPath   = Path.Combine(_config.BasePath, _config.InputFolder);
            _downloadPath = Path.Combine(_config.BasePath, _config.OutputFolder);

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

                var userId = GetCurrentUserId();
                if (userId == 0)
                {
                    return new UploadResult
                    {
                        Success = false,
                        Message = "User not authenticated"
                    };
                }

                // Проверяем существование пользователя
                var userExists = await _context.Users.AnyAsync(u => u.Id == userId);
                if (!userExists)
                {
                    return new UploadResult
                    {
                        Success = false,
                        Message = "User not found"
                    };
                }

                var fileName = $"{Guid.NewGuid()}_{DateTime.Now:yyyyMMddHHmmss}{fileExtension}";
                var filePath = Path.Combine(_uploadPath, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                // Создаем запись в базе данных
                var userFile = new UserFile
                {
                    UserId           = userId,
                    FileName         = Path.GetFileNameWithoutExtension(file.FileName),
                    FilePath         = filePath,
                    FileSize         = file.Length,
                    ContentType      = file.ContentType,
                    UploadDate       = DateTime.UtcNow,
                    OriginalFileName = file.FileName,
                    StoredFileName   = fileName
                };

                // Добавляем в DbSet и сохраняем
                await _context.UserFiles.AddAsync(userFile);
                await _context.SaveChangesAsync();

                // Создаем DTO для ответа
                var fileInfo = new FileInfoDto
                {
                    Id               = userFile.Id,
                    FileName         = userFile.StoredFileName,
                    OriginalFileName = userFile.OriginalFileName,
                    FilePath         = userFile.FilePath,
                    ContentType      = userFile.ContentType,
                    Size             = userFile.FileSize,
                    UploadDate       = userFile.UploadDate,
                    UserId           = userFile.UserId
                };

                _logger.LogInformation($"File {fileName} successfully uploaded by user {userId}. Size: {file.Length} bytes. DB ID: {userFile.Id}");

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
                var filePath = Path.Combine(_downloadPath, fileName);

                if (!File.Exists(filePath))
                {
                    throw new FileNotFoundException($"File {fileName} not found");
                }

                // Обновляем информацию о последнем доступе в базе данных
                var userFile = await _context.UserFiles
                    .FirstOrDefaultAsync(f => f.StoredFileName == fileName);

                if (userFile != null)
                {
                    userFile.LastAccessed = DateTime.UtcNow;
                    await _context.SaveChangesAsync();
                }

                var fileBytes  = await File.ReadAllBytesAsync(filePath);
                var contentType = GetContentType(fileName);

                return (fileBytes, fileName, contentType);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error downloading file {fileName}");
                throw;
            }
        }

        public async Task<IEnumerable<IFileInfoDto>> GetUploadedFilesInfoAsync()
        {
            try
            {
                // Получаем файлы из базы данных, а не из файловой системы
                var userId = GetCurrentUserId();

                var files = await _context.UserFiles
                    .Where(f => f.UserId == userId)
                    .OrderByDescending(f => f.UploadDate)
                    .Select(f => new FileInfoDto
                    {
                        Id               = f.Id,
                        FileName         = f.FileName,
                        OriginalFileName = f.OriginalFileName,
                        FilePath         = f.FilePath,
                        ContentType      = f.ContentType,
                        Size             = f.FileSize,
                        UploadDate       = f.UploadDate,
                        LastAccessed     = f.LastAccessed,
                        UserId           = f.UserId
                    })
                    .ToListAsync();

                return files;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting uploaded files info");
                return new List<FileInfoDto>();
            }
        }

        public async Task<IEnumerable<IFileInfoDto>> GetAllFilesInfoAsync()
        {
            try
            {
                var files = await _context.UserFiles
                    .Include(f => f.User)
                    .OrderByDescending(f => f.UploadDate)
                    .Select(f => new FileInfoDto
                    {
                        Id               = f.Id,
                        FileName         = f.FileName,
                        OriginalFileName = f.OriginalFileName,
                        FilePath         = f.FilePath,
                        ContentType      = f.ContentType,
                        Size             = f.FileSize,
                        UploadDate       = f.UploadDate,
                        LastAccessed     = f.LastAccessed,
                        UserId           = f.UserId
                    })
                    .ToListAsync();

                return files;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all files info");
                return new List<FileInfoDto>();
            }
        }

        public async Task<bool> DeleteFileAsync(string fileName)
        {
            try
            {
                var filePath = Path.Combine(_uploadPath, fileName);

                var userFile = await _context.UserFiles
                    .FirstOrDefaultAsync(f => f.StoredFileName == fileName);

                if (userFile != null)
                {
                    _context.UserFiles.Remove(userFile);
                    await _context.SaveChangesAsync();
                }

                if (!File.Exists(filePath))
                {
                    return false;
                }

                File.Delete(filePath);
                _logger.LogInformation($"File {fileName} deleted. DB record removed: {(userFile != null ? "yes" : "no")}");
                return await Task.FromResult(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting file {fileName}");
                return false;
            }
        }

        public async Task<IFileInfoDto> GetFileInfoByIdAsync(int fileId)
        {
            try
            {
                var userId = GetCurrentUserId();

                var file = await _context.UserFiles
                    .Where(f => f.Id == fileId && (f.UserId == userId || IsAdmin())) // Только свои файлы или админ
                    .Select(f => new FileInfoDto
                    {
                        Id               = f.Id,
                        FileName         = f.FileName,
                        OriginalFileName = f.OriginalFileName,
                        FilePath         = f.FilePath,
                        ContentType      = f.ContentType,
                        Size             = f.FileSize,
                        UploadDate       = f.UploadDate,
                        LastAccessed     = f.LastAccessed,
                        UserId           = f.UserId
                    })
                    .FirstOrDefaultAsync();

                return file;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting file info by ID {fileId}");
                return null;
            }
        }

        public async Task<bool> UpdateFileInfoAsync(int fileId, string description)
        {
            try
            {
                var userId = GetCurrentUserId();

                var userFile = await _context.UserFiles
                    .FirstOrDefaultAsync(f => f.Id == fileId && f.UserId == userId);

                if (userFile == null)
                {
                    return false;
                }

                userFile.Description = description;
                await _context.SaveChangesAsync();

                _logger.LogInformation($"File {fileId} description updated by user {userId}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating file info for ID {fileId}");
                return false;
            }
        }

        private int GetCurrentUserId()
        {
            try
            {
                var userIdClaim = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (int.TryParse(userIdClaim, out int userId))
                {
                    return userId;
                }
                return 0;
            }
            catch
            {
                return 0;
            }
        }

        private bool IsAdmin()
        {
            try
            {
                return _httpContextAccessor.HttpContext?.User?.IsInRole("Admin") ?? false;
            }
            catch
            {
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