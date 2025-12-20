using AKAVER_Server.Interfaces.IFileInfo;

namespace AKAVER_Server.Classes.FileInfo
{
    public class UploadResult : IUploadResult
    {
        public bool Success                  { get; set; }
        public required string Message       { get; set; }
        public FileInfoDto FileInfo          { get; set; }
    }
}
