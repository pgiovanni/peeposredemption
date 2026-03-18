using peeposredemption.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace peeposredemption.Domain.Interfaces.Repositories
{
    public interface IUserRepository
    {
        Task<User?> GetByIdAsync(Guid id);
        Task<User?> GetByEmailAsync(string email);
        Task<bool> EmailExistsAsync(string email);
        Task<bool> UsernameExistsAsync(string username);
        Task AddAsync(User user);
        Task<User?> GetByConfirmationTokenAsync(string token);
        Task<User?> GetByUsernameAsync(string username);
        Task<List<User>> GetAllAsync();
        Task<User?> GetByPasswordResetTokenAsync(string token);
    }
}
