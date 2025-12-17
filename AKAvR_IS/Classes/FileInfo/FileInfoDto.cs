using AKAvR_IS.Interfaces.IFileInfo;

namespace AKAvR_IS.Classes.FileInfo
{
    public class FileInfoDto : IFileInfoDto
    {
        public required string FileName    { get; set; }
        public string ContentType          { get; set; }
        public long Size                   { get; set; }
        public DateTime UploadDate         { get; set; }
    }
}
