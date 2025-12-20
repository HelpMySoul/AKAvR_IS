using AKAvR_IS.Interfaces.IFileInfo;

namespace AKAvR_IS.Classes.FileInfo
{
    public class UploadResult : IUploadResult
    {
        public bool Success                  { get; set; }
        public required string Message       { get; set; }
        public FileInfoDto FileInfo          { get; set; }
    }
}
