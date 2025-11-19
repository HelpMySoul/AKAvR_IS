using AKAvR_IS.Classes;

namespace AKAvR_IS.Interfaces.IUser
{
    public interface IUserService
    {
        Task<IUser> RegisterUserAsync(CreateUserRequest request);
        Task<IUser> LoginUserAsync(LoginRequest request);
        Task<IUser> GetUserAsync(int id);
        Task<IUser> UpdateUserAsync(int id, UpdateUserRequest request);
        Task<bool> DeleteUserAsync(int id);
        Task<IEnumerable<IUser>> SearchUsersAsync(string username);
        Task<IEnumerable<IUser>> GetAllUsersAsync();
        Task SaveChangesAsync();
    }
}
