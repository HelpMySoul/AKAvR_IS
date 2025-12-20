using AKAVER_Server.Classes.FileInfo;
using AKAVER_Server.Interfaces.IFileInfo;

namespace AKAVER_Server.Interfaces.IFileService
{
    public interface IFileService
    {
        Task<IUploadResult> UploadCsvFileAsync(IFormFile file);
        Task<(byte[] fileBytes, string fileName, string contentType)> DownloadFileAsync(string fileName);
        Task<IEnumerable<IFileInfoDto>> GetUploadedFilesInfoAsync();
        Task<bool> DeleteFileAsync(string fileName);
    }
}
