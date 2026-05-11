using Core.DTOs.Auth;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Core.Interfaces.Services
{
    public interface IAuthService
    {
        Task<AuthResponseDto> RegisterCitizenAsync(CitizenRegisterDto dto);
        Task<AuthResponseDto> LoginCitizenAsync(LoginDto dto);
        Task<AuthResponseDto> LoginInternalUserAsync(LoginDto dto);
    }
}
