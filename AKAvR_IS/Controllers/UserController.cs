using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using AKAvR_IS.Interfaces.IUser;
using AKAvR_IS.Classes;

namespace AKAvR_IS.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly IUserService _userService;

        public UserController(IUserService userService)
        {
            _userService = userService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllUsers()
        {
            var activeUsers = await _userService.GetAllUsersAsync();
            activeUsers.Where(u => u.IsActive)
                .Select(u => new
                {
                    u.Id,
                    u.Username,
                    u.Email,
                    u.CreatedAt
                });

            return Ok(activeUsers);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetUser(int id)
        {
            var user = await _userService.GetUserAsync(id);
            
            if (user == null)
            {
                return NotFound(new { message = "Пользователь не найден" });
            }

            return Ok(user);
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] CreateUserRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var createdUser = _userService.RegisterUserAsync(request);            

            await _userService.SaveChangesAsync();

            return CreatedAtAction(nameof(GetUser), new { id = createdUser.Id }, createdUser);
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

           var user = await _userService.LoginUserAsync(request);            

            var loginResponse = new
            {
                user.Id,
                user.Username,
                user.Email,
                message = "Вход выполнен успешно"
            };

            return Ok(loginResponse);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateUser(int id, [FromBody] UpdateUserRequest request)
        {
            var updatedUser = await _userService.UpdateUserAsync(id, request);            

            await _userService.SaveChangesAsync();

            var response = new
            {
                updatedUser.Id,
                updatedUser.Username,
                updatedUser.Email,
                updatedUser.UpdatedAt,
                message = "Данные пользователя обновлены"
            };

            return Ok(response);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            var users = await _userService.GetAllUsersAsync();
            var user = users.FirstOrDefault(u => u.Id == id && u.IsActive);

            if (user == null)
            {
                return NotFound(new { message = "Пользователь не найден" });
            }

            user.IsActive = false;
            user.UpdatedAt = DateTime.UtcNow;

            await _userService.SaveChangesAsync();

            return Ok(new { message = "Пользователь удален" });
        }

        [HttpGet("search")]
        public async Task<IActionResult> SearchUsers([FromQuery] string username)
        {
            if (string.IsNullOrWhiteSpace(username) || username.Length < 2)
            {
                return BadRequest(new { message = "Поисковый запрос должен содержать минимум 2 символа" });
            }

            var users = await _userService.SearchUsersAsync(username);

            return Ok(users);
        }
    }    
}