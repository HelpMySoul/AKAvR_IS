using AKAvR_IS.Classes.FileInfo;

namespace AKAvR_IS.Interfaces.IFileInfo
{
    public interface IUploadResult
    {
        bool Success         { get; set; }
        string Message       { get; set; }
        FileInfoDto FileInfo { get; set; }
    }
}