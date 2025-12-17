using AKAvR_IS.Classes.FileInfo;
using AKAvR_IS.Interfaces.IFileInfo;

namespace AKAvR_IS.Interfaces.IFileService
{
    public interface IFileService
    {
        Task<IUploadResult> UploadCsvFileAsync(IFormFile file);
        Task<(byte[] fileBytes, string fileName, string contentType)> DownloadFileAsync(string fileName);
        Task<IEnumerable<IFileInfoDto>> GetUploadedFilesInfoAsync();
        Task<bool> DeleteFileAsync(string fileName);
    }
}
