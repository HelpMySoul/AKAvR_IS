

namespace AKAVER_Server.Interfaces.IUser
{
    public interface IUserRepository
    {
        Task<IUser> GetByIdAsync(int id);
        Task<IUser> GetByEmailAsync(string email);
        Task<IUser> GetByUsernameAsync(string username);
        Task<IEnumerable<IUser>> GetAllActiveAsync();
        Task<IUser> CreateAsync(IUser user);
        Task<IUser> UpdateAsync(IUser user);
        Task<bool> DeleteAsync(int id);
        Task<bool> EmailExistsAsync(string email);
        Task<bool> UsernameExistsAsync(string username);
        Task<IUser> ValidateCredentialsAsync(string login, string password);
    }
}
