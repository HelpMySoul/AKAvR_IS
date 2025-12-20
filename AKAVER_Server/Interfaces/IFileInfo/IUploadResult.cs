using AKAVER_Server.Classes.FileInfo;

namespace AKAVER_Server.Interfaces.IFileInfo
{
    public interface IUploadResult
    {
        bool Success         { get; set; }
        string Message       { get; set; }
        FileInfoDto FileInfo { get; set; }
    }
}