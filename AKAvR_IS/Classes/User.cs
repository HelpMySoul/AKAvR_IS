using AKAvR_IS.Interfaces.IUser;
using System.ComponentModel.DataAnnotations;

namespace AKAvR_IS.Classes
{
    // Модели данных
    public class User : IUser
    {
        public int Id { get; set; }

        [Required]
        [EmailAddress]
        public required string Email { get; set; }

        [Required]
        [MinLength(3)]
        public required string Username { get; set; }

        [Required]
        [MinLength(6)]
        public required string Password { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public bool IsActive { get; set; }
    }

    public class CreateUserRequest
    {
        [Required]
        [EmailAddress]
        public required string Email { get; set; }

        [Required]
        [MinLength(3)]
        public required string Username { get; set; }

        [Required]
        [MinLength(6)]
        public required string Password { get; set; }
    }

    public class UpdateUserRequest
    {
        [EmailAddress]
        public string? Email { get; set; }

        [MinLength(3)]
        public string? Username { get; set; }

        [MinLength(6)]
        public string? Password { get; set; }
    }

    public class LoginRequest
    {
        [Required]
        public required string Email { get; set; }

        [Required]
        public required string Password { get; set; }
    }
}
