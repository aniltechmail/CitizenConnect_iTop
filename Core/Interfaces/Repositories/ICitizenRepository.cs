using Core.Entities.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Interfaces.Repositories
{
    public interface ICitizenRepository
    {
        Task<Citizen?> GetByPhoneAsync(string phone);
        Task<Citizen?> GetByIdAsync(Guid id);
        Task<bool> PhoneExistsAsync(string phone);
        Task<Citizen> CreateAsync(Citizen citizen);
        Task UpdateAsync(Citizen citizen);
    }
}
