using Core.Common;
using Core.DTOs.Auth;
using Core.DTOs.Mobile;
using Core.Entities.Mobile;
using Core.Interfaces.Services;
using Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace API.Controllers
{
    [ApiController]
    [Route("api/auth/citizen")]
    public class MobileAuthController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly IConfiguration _config;
        private readonly ISmsService _smsService;
        private readonly OtpSettings _otpSettings;

        public MobileAuthController(
            AppDbContext db,
            IConfiguration config,
            ISmsService smsService,
            IOptions<OtpSettings> otpSettings)
        {
            _db = db;
            _config = config;
            _smsService = smsService;
            _otpSettings = otpSettings.Value;
        }

        [HttpPost("refresh-token")]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequestDto request)
        {
            var refreshToken = await _db.CitizenRefreshTokens
                .FirstOrDefaultAsync(x => x.Token == request.RefreshToken && !x.IsRevoked);

            if (refreshToken == null || refreshToken.ExpiresAt <= DateTime.UtcNow)
                return Unauthorized(new { message = "Invalid or expired refresh token." });

            var citizen = await _db.Citizens.FirstOrDefaultAsync(x => x.Id == refreshToken.CitizenId && x.IsActive);
            if (citizen == null)
                return Unauthorized(new { message = "Citizen account is inactive or not found." });

            refreshToken.IsRevoked = true;
            var result = BuildCitizenAuthResponse(citizen);
            await _db.SaveChangesAsync();
            return Ok(result);
        }

        [HttpPost("register/send-otp")]
        [HttpPost("request-otp")]
        public async Task<IActionResult> SendRegistrationOtp([FromBody] SendOtpRequestDto request)
        {
            string normalized;
            try
            {
                normalized = MobileNumberNormalizer.Normalize(request.MobileNumber ?? request.Phone);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }

            var now = DateTime.UtcNow;
            var fixedOtpUser = _otpSettings.FixedOtpUsers?
                .FirstOrDefault(x => MobileNumberNormalizer.Normalize(x.MobileNumber) == normalized);
            if (fixedOtpUser != null)
                return Ok(await HandleFixedOtpUserAsync(normalized, fixedOtpUser, now));

            var resendCountToday = await _db.CitizenOtps
                .CountAsync(x => x.Phone == normalized && x.CreatedAt >= now.Date);
            if (resendCountToday >= _otpSettings.MaxResendsPerDay)
            {
                return Ok(new SendOtpResponseDto
                {
                    Phone = normalized,
                    MobileNumber = normalized,
                    IsNewOtp = false,
                    Message = "OTP resend limit exceeded for today"
                });
            }

            var lastOtp = await _db.CitizenOtps
                .Where(x => x.Phone == normalized && !x.IsUsed && !x.IsLocked)
                .OrderByDescending(x => x.CreatedAt)
                .FirstOrDefaultAsync();

            if (lastOtp != null && lastOtp.ExpiresAt >= now)
            {
                if (lastOtp.LastResendOn.HasValue &&
                    (now - lastOtp.LastResendOn.Value).TotalSeconds < _otpSettings.ResendCooldownSeconds)
                {
                    return Ok(new SendOtpResponseDto
                    {
                        Phone = normalized,
                        MobileNumber = normalized,
                        DevOtp = lastOtp.OtpCode,
                        Otp = lastOtp.OtpCode,
                        ExpiresAt = lastOtp.ExpiresAt,
                        ExpiresOn = lastOtp.ExpiresAt,
                        IsNewOtp = false,
                        Message = "OTP already sent. Please wait before requesting again."
                    });
                }

                lastOtp.LastResendOn = now;
                lastOtp.ResendCount++;
                await _db.SaveChangesAsync();
                await SendOtpSmsAsync(normalized, lastOtp.OtpCode);

                return Ok(new SendOtpResponseDto
                {
                    Phone = normalized,
                    MobileNumber = normalized,
                    DevOtp = lastOtp.OtpCode,
                    Otp = lastOtp.OtpCode,
                    ExpiresAt = lastOtp.ExpiresAt,
                    ExpiresOn = lastOtp.ExpiresAt,
                    IsNewOtp = false,
                    Message = "OTP resent successfully"
                });
            }

            var otp = Random.Shared.Next(100000, 999999).ToString();
            var entry = new CitizenOtp
            {
                Phone = normalized,
                OtpCode = otp,
                CreatedAt = now,
                ExpiresAt = now.AddMinutes(_otpSettings.ExpiryMinutes),
                LastResendOn = now,
                ResendCount = 1,
                IsUsed = false,
                IsLocked = false
            };

            _db.CitizenOtps.Add(entry);
            await _db.SaveChangesAsync();
            await SendOtpSmsAsync(normalized, otp);

            return Ok(new SendOtpResponseDto
            {
                Phone = normalized,
                MobileNumber = normalized,
                ExpiresAt = entry.ExpiresAt,
                ExpiresOn = entry.ExpiresAt,
                DevOtp = otp,
                Otp = otp,
                IsNewOtp = true,
                Message = "OTP sent successfully"
            });
        }

        [HttpPost("register/verify-otp")]
        [HttpPost("verify-otp")]
        public async Task<IActionResult> VerifyRegistrationOtp([FromBody] VerifyOtpRequestDto request)
        {
            string normalized;
            try
            {
                normalized = MobileNumberNormalizer.Normalize(request.MobileNumber ?? request.Phone);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }

            var otp = await _db.CitizenOtps
                .Where(x =>
                    x.Phone == normalized &&
                    !x.IsUsed &&
                    !x.IsLocked &&
                    x.ExpiresAt >= DateTime.UtcNow)
                .OrderByDescending(x => x.CreatedAt)
                .FirstOrDefaultAsync();

            if (otp == null)
                return BadRequest(new { message = "Invalid or expired OTP." });

            if (otp.OtpCode != request.Otp)
            {
                otp.VerifyAttempts++;
                if (otp.VerifyAttempts >= _otpSettings.MaxVerifyAttempts)
                    otp.IsLocked = true;
                await _db.SaveChangesAsync();
                return BadRequest(new { message = "Invalid or expired OTP." });
            }

            otp.IsUsed = true;
            var citizen = await _db.Citizens.FirstOrDefaultAsync(x => x.Phone == normalized || x.Phone == request.Phone || x.Phone == request.MobileNumber);
            if (citizen != null)
            {
                citizen.IsVerified = true;
                citizen.Phone = normalized;
            }

            await _db.SaveChangesAsync();
            return Ok(new { verified = true, phone = normalized });
        }

        private async Task<SendOtpResponseDto> HandleFixedOtpUserAsync(string normalized, FixedOtpUser fixedOtpUser, DateTime now)
        {
            var expiresAt = now.AddMinutes(10);
            var existingOtp = await _db.CitizenOtps
                .Where(x =>
                    x.Phone == normalized &&
                    !x.IsUsed &&
                    !x.IsLocked &&
                    x.OtpCode == fixedOtpUser.Otp)
                .OrderByDescending(x => x.CreatedAt)
                .FirstOrDefaultAsync();

            if (existingOtp != null && existingOtp.ExpiresAt >= now)
            {
                if (existingOtp.LastResendOn.HasValue &&
                    (now - existingOtp.LastResendOn.Value).TotalSeconds < _otpSettings.ResendCooldownSeconds)
                {
                    return new SendOtpResponseDto
                    {
                        Phone = normalized,
                        MobileNumber = normalized,
                        DevOtp = fixedOtpUser.Otp,
                        Otp = fixedOtpUser.Otp,
                        ExpiresAt = existingOtp.ExpiresAt,
                        ExpiresOn = existingOtp.ExpiresAt,
                        IsNewOtp = false,
                        Message = "OTP already sent. Please wait before requesting again."
                    };
                }

                existingOtp.LastResendOn = now;
                existingOtp.ResendCount++;
                existingOtp.ExpiresAt = expiresAt;
                await _db.SaveChangesAsync();
                await SendOtpSmsAsync(normalized, fixedOtpUser.Otp);

                return new SendOtpResponseDto
                {
                    Phone = normalized,
                    MobileNumber = normalized,
                    DevOtp = fixedOtpUser.Otp,
                    Otp = fixedOtpUser.Otp,
                    ExpiresAt = existingOtp.ExpiresAt,
                    ExpiresOn = existingOtp.ExpiresAt,
                    IsNewOtp = false,
                    Message = "OTP resent successfully"
                };
            }

            var newOtp = new CitizenOtp
            {
                Phone = normalized,
                OtpCode = fixedOtpUser.Otp,
                CreatedAt = now,
                ExpiresAt = expiresAt,
                IsUsed = false,
                IsLocked = false,
                ResendCount = 0,
                LastResendOn = now
            };

            _db.CitizenOtps.Add(newOtp);
            await _db.SaveChangesAsync();
            await SendOtpSmsAsync(normalized, fixedOtpUser.Otp);

            return new SendOtpResponseDto
            {
                Phone = normalized,
                MobileNumber = normalized,
                DevOtp = fixedOtpUser.Otp,
                Otp = fixedOtpUser.Otp,
                ExpiresAt = expiresAt,
                ExpiresOn = expiresAt,
                IsNewOtp = true,
                Message = "OTP sent successfully"
            };
        }

        private Task SendOtpSmsAsync(string normalized, string otp)
        {
            return _smsService.SendAsync(
                normalized,
                $"Your OTP to login to MLA Sampark is {otp}. Please enter this code to verify your identity. For security, do not share this code. https://mlasampark.com/");
        }

        private AuthResponseDto BuildCitizenAuthResponse(Core.Entities.Identity.Citizen citizen)
        {
            var expiry = DateTime.UtcNow.AddHours(int.Parse(_config["Jwt:ExpiryHours"] ?? "24"));
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, citizen.Id.ToString()),
                new Claim(ClaimTypes.Name, citizen.FullName),
                new Claim(ClaimTypes.MobilePhone, citizen.Phone),
                new Claim("user_type", "citizen"),
                new Claim("block_id", citizen.BlockId.ToString())
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                claims: claims,
                expires: expiry,
                signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));

            var refreshToken = new CitizenRefreshToken
            {
                CitizenId = citizen.Id,
                Token = Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N"),
                ExpiresAt = DateTime.UtcNow.AddDays(30)
            };
            _db.CitizenRefreshTokens.Add(refreshToken);

            return new AuthResponseDto
            {
                Token = new JwtSecurityTokenHandler().WriteToken(token),
                RefreshToken = refreshToken.Token,
                ExpiresAt = expiry,
                User = new UserInfoDto
                {
                    Id = citizen.Id.ToString(),
                    FullName = citizen.FullName,
                    Phone = citizen.Phone,
                    Email = citizen.Email,
                    UserType = "citizen"
                }
            };
        }
    }
}
