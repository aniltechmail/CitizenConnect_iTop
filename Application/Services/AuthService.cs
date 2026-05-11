using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Core.DTOs.Auth;
using Core.Entities.Identity;
using Core.Interfaces.Repositories;
using Core.Interfaces.Services;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace Application.Services
{
    public class AuthService : IAuthService
    {
        private readonly ICitizenRepository _citizenRepo;
        private readonly AppDbContext _db;
        private readonly IConfiguration _config;

        public AuthService(ICitizenRepository citizenRepo, AppDbContext db, IConfiguration config)
        {
            _citizenRepo = citizenRepo;
            _db = db;
            _config = config;
        }

        public async Task<AuthResponseDto> RegisterCitizenAsync(CitizenRegisterDto dto)
        {
            if (await _citizenRepo.PhoneExistsAsync(dto.Phone))
                throw new InvalidOperationException("Phone number already registered.");

            var citizen = new Citizen
            {
                FullName = dto.FullName,
                Phone = dto.Phone,
                Email = dto.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                BlockId = dto.BlockId,
                IsVerified = false
            };

            await _citizenRepo.CreateAsync(citizen);
            return GenerateCitizenToken(citizen);
        }

        public async Task<AuthResponseDto> LoginCitizenAsync(LoginDto dto)
        {
            var citizen = await _citizenRepo.GetByPhoneAsync(dto.Identifier);
            if (citizen == null || !BCrypt.Net.BCrypt.Verify(dto.Password, citizen.PasswordHash))
                throw new UnauthorizedAccessException("Invalid credentials.");

            if (!citizen.IsActive)
                throw new UnauthorizedAccessException("Account is deactivated.");

            citizen.LastLoginAt = DateTime.UtcNow;
            await _citizenRepo.UpdateAsync(citizen);

            return GenerateCitizenToken(citizen);
        }

        public async Task<AuthResponseDto> LoginInternalUserAsync(LoginDto dto)
        {
            var user = await _db.InternalUsers
                .FirstOrDefaultAsync(x => x.Email == dto.Identifier && x.IsActive);

            if (user == null || !BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
                throw new UnauthorizedAccessException("Invalid credentials.");

            user.LastLoginAt = DateTime.UtcNow;
            _db.InternalUsers.Update(user);
            await _db.SaveChangesAsync();

            return GenerateInternalUserToken(user);
        }

        private AuthResponseDto GenerateCitizenToken(Citizen citizen)
        {
            var claims = new[]
            {
            new Claim(ClaimTypes.NameIdentifier, citizen.Id.ToString()),
            new Claim(ClaimTypes.Name, citizen.FullName),
            new Claim(ClaimTypes.MobilePhone, citizen.Phone),
            new Claim("user_type", "citizen"),
            new Claim("block_id", citizen.BlockId.ToString())
        };
            return BuildAuthResponse(claims, new UserInfoDto
            {
                Id = citizen.Id.ToString(),
                FullName = citizen.FullName,
                Phone = citizen.Phone,
                Email = citizen.Email,
                UserType = "citizen"
            });
        }

        private AuthResponseDto GenerateInternalUserToken(InternalUser user)
        {
            var claims = new[]
            {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.FullName),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role.ToString()),
            new Claim("user_type", "internal"),
        };
            return BuildAuthResponse(claims, new UserInfoDto
            {
                Id = user.Id.ToString(),
                FullName = user.FullName,
                Email = user.Email,
                UserType = "internal",
                Role = user.Role.ToString()
            });
        }

        private AuthResponseDto BuildAuthResponse(Claim[] claims, UserInfoDto userInfo)
        {
            var key = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var expiry = DateTime.UtcNow.AddHours(
                int.Parse(_config["Jwt:ExpiryHours"] ?? "24"));

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                claims: claims,
                expires: expiry,
                signingCredentials: creds
            );

            return new AuthResponseDto
            {
                Token = new JwtSecurityTokenHandler().WriteToken(token),
                RefreshToken = Guid.NewGuid().ToString(),
                ExpiresAt = expiry,
                User = userInfo
            };
        }
    }
}
