using AKAvR_IS.Classes;
using AKAvR_IS.Contexts;
using AKAvR_IS.Interfaces.IUser;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore; // Если используете Entity Framework

internal class UserService : IUserService
{
    private readonly ApplicationDbContext _context;
    private readonly IPasswordHasher _passwordHasher;

    public UserService(ApplicationDbContext context, IPasswordHasher passwordHasher)
    {
        _context = context;
        _passwordHasher = passwordHasher;
    }

    public async Task<IUser> RegisterUserAsync(CreateUserRequest request)
    {
        // Проверяем, существует ли пользователь с таким email
        if (await _context.Users.AnyAsync(u => u.Email == request.Email && u.IsActive))
        {
            throw new InvalidOperationException("Пользователь с таким email уже существует");
        }

        // Проверяем, существует ли пользователь с таким именем
        if (await _context.Users.AnyAsync(u => u.Username == request.Username && u.IsActive))
        {
            throw new InvalidOperationException("Пользователь с таким именем уже существует");
        }

        // Хешируем пароль
        var passwordHash = _passwordHasher.HashPassword(request.Password);

        // Создаем нового пользователя
        var user = new User
        {
            Email = request.Email,
            Username = request.Username,
            Password = passwordHash,
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };

        // Сохраняем в базу данных
        _context.Users.Add(user);

        return user;
    }

    public async Task<IUser> LoginUserAsync(LoginRequest request)
    {
        // Ищем пользователя
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Email == request.Email && u.IsActive);

        if (user == null)
        {
            throw new UnauthorizedAccessException("Неверный email");
        }

        // Проверяем пароль
        var isPasswordValid = _passwordHasher.VerifyPassword(request.Password, user.Password);
        if (!isPasswordValid)
        {
            throw new UnauthorizedAccessException("Неверный пароль");
        }

        return user;
    }

    public async Task<IEnumerable<IUser>> GetAllUsersAsync()
    {
        return await _context.Users
            .Where(u => u.IsActive)
            .OrderBy(u => u.Username)
            .ToListAsync();
    }

    public async Task<IUser> GetUserAsync(int id)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == id && u.IsActive);

        if (user == null)
        {
            throw new KeyNotFoundException($"Пользователь с ID {id} не найден");
        }

        return user;
    }

    public async Task<IEnumerable<IUser>> SearchUsersAsync(string username)
    {
        return await _context.Users
            .Where(u => u.IsActive && u.Username.Contains(username))
            .OrderBy(u => u.Username)
            .ToListAsync();
    }

    public async Task<IUser> UpdateUserAsync(int id, UpdateUserRequest request)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == id && u.IsActive);

        if (user == null)
        {
            throw new KeyNotFoundException($"Пользователь с ID {id} не найден");
        }

        // Проверяем, не занят ли новый email другим пользователем
        if (!string.IsNullOrEmpty(request.Email) && request.Email != user.Email)
        {
            var emailExists = await _context.Users
                .AnyAsync(u => u.Email == request.Email && u.Id != id && u.IsActive);

            if (emailExists)
            {
                throw new InvalidOperationException("Пользователь с таким email уже существует");
            }

            user.Email = request.Email;
        }

        // Проверяем, не занято ли новое имя другим пользователем
        if (!string.IsNullOrEmpty(request.Username) && request.Username != user.Username)
        {
            var usernameExists = await _context.Users
                .AnyAsync(u => u.Username == request.Username && u.Id != id && u.IsActive);

            if (usernameExists)
            {
                throw new InvalidOperationException("Пользователь с таким именем уже существует");
            }

            user.Username = request.Username;
        }

        // Обновляем пароль, если предоставлен
        if (!string.IsNullOrEmpty(request.Password))
        {
            user.Password = _passwordHasher.HashPassword(request.Password);
        }

        user.UpdatedAt = DateTime.UtcNow;

        return user;
    }

    public async Task<bool> DeleteUserAsync(int id)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == id && u.IsActive);

        if (user == null)
        {
            return false;
        }

        // Мягкое удаление
        user.IsActive = false;
        user.UpdatedAt = DateTime.UtcNow;

        return true;
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}