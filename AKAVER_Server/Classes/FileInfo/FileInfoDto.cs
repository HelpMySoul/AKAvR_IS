using AKAVER_Server.Interfaces.IFileInfo;

namespace AKAVER_Server.Classes.FileInfo
{
    public class FileInfoDto : IFileInfoDto
    {
        public int Id { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string OriginalFileName { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty;
        public long Size { get; set; }
        public DateTime UploadDate { get; set; }
        public DateTime? LastAccessed { get; set; }
        public int DownloadCount { get; set; }
        public int UserId { get; set; }
        public string? Username { get; set; }
        public string? Description { get; set; }
    }
}