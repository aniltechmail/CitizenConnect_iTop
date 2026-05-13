using Core.Entities.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Interfaces.Repositories
{
    public interface IInternalUserRepository
    {
        Task<InternalUser?> GetByIdAsync(Guid id);
        Task<InternalUser?> GetByEmailAsync(string email);
        Task<IEnumerable<InternalUser>> GetByRoleAsync(Enums.UserRole role);
        Task UpdateAsync(InternalUser user);
    }
}
