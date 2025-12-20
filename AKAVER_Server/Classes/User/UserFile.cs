using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AKAvR_IS.Classes.User
{
    [Table("User_Files")]
    public class UserFile
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required]
        [MaxLength(500)]
        public string FileName { get; set; } = string.Empty;

        [MaxLength(500)]
        public string OriginalFileName { get; set; } = string.Empty;

        [MaxLength(1000)]
        public string StoredFileName { get; set; } = string.Empty;

        [Required]
        [MaxLength(1000)]
        public string FilePath { get; set; } = string.Empty;

        [Required]
        public long FileSize { get; set; }

        [MaxLength(100)]
        public string ContentType { get; set; } = string.Empty;

        [MaxLength(1000)]
        public string Description { get; set; } = string.Empty;

        public DateTime UploadDate { get; set; } = DateTime.UtcNow;

        public DateTime? LastAccessed { get; set; }

        // Навигационное свойство
        [ForeignKey("UserId")]
        public virtual User? User { get; set; }
    }
}