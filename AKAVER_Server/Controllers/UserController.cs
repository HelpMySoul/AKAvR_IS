using AKAVER_Server.Classes.User;
using AKAVER_Server.Interfaces.IUser;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.ComponentModel.DataAnnotations;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace AKAVER_Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly IUserService   _userService;
        private readonly IConfiguration _configuration;

        public UserController(IUserService userService, IConfiguration configuration)
        {
            _userService   = userService;
            _configuration = configuration;
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

            var createdUser = await _userService.RegisterUserAsync(request);            

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

            var token = GenerateJwtToken(user);

            var loginResponse = new
            {
                user.Id,
                user.Username,
                user.Email,
                Token = token,
                message = "Вход выполнен успешно"
            };

            return Ok(loginResponse);
        }

        private string GenerateJwtToken(IUser user)
        {
            var jwtKey = _configuration["Jwt:Key"];

            if (string.IsNullOrEmpty(jwtKey))
            {
                throw new InvalidOperationException("JWT Key is not configured");
            }

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Email, user.Email)
            };

            var key   = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer:   _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.Now.AddHours(1),
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
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

        [HttpGet("userinfo")]
        [Authorize]
        public async Task<IActionResult> GetUserInfo()
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);

                if (userIdClaim == null)
                {
                    return Unauthorized(new { message = "Токен не содержит идентификатор пользователя" });
                }

                if (!int.TryParse(userIdClaim.Value, out int userId))
                {
                    return BadRequest(new { message = "Неверный формат идентификатора пользователя" });
                }

                var user = await _userService.GetUserAsync(userId);

                if (user == null || !user.IsActive)
                {
                    return NotFound(new { message = "Пользователь не найден" });
                }

                var userInfo = new
                {
                    user.Id,
                    user.Username,
                    user.Email,
                    user.CreatedAt,
                    user.UpdatedAt,
                    Claims = User.Claims.Select(c => new { c.Type, c.Value })
                };

                return Ok(userInfo);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Ошибка при получении информации о пользователе", error = ex.Message });
            }
        }
    }    
}