using System.ComponentModel.DataAnnotations;

namespace AKAvR_IS.Interfaces.IUser
{
    public interface IUser
    {
        int Id { get; set; }

        [Required]
        [EmailAddress]
        string Email { get; set; }

        [Required]
        [MinLength(3)]
        string Username { get; set; }

        [Required]
        [MinLength(6)]
        string Password { get; set; }

        DateTime CreatedAt { get; set; }
        DateTime? UpdatedAt { get; set; }
        bool IsActive { get; set; }
    }
}
