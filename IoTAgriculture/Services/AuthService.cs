using System.Security.Cryptography;
using IoTAgriculture.Data;
using IoTAgriculture.DTOs.Auth;
using IoTAgriculture.Models;
using IoTAgriculture.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace IoTAgriculture.Services
{
    public class AuthService : IAuthService
    {
        private const int SessionDays = 30;
        private const int UserRole = 0;
        private const int AdminRole = 1;
        private readonly IoTDbContext _db;

        public AuthService(IoTDbContext db)
        {
            _db = db;
        }

        public async Task<AuthResponseDto> RegisterAsync(RegisterRequestDto dto)
        {
            var phone = NormalizePhone(dto.PhoneNumber);
            var exists = await _db.Users.AnyAsync(u => u.PhoneNumber == phone);
            if (exists)
            {
                throw new InvalidOperationException("Phone number already exists");
            }

            var password = HashPassword(dto.Password);
            var user = new AppUser
            {
                UserId = Guid.NewGuid(),
                FullName = dto.FullName.Trim(),
                PhoneNumber = phone,
                Address = dto.Address.Trim(),
                DateOfBirth = dto.DateOfBirth.Date,
                Role = UserRole,
                PasswordHash = password.Hash,
                PasswordSalt = password.Salt,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _db.Users.Add(user);
            await _db.SaveChangesAsync();
            await LogActivityAsync(user.UserId, "register", "Tạo tài khoản mới", "info");
            return await CreateSessionAsync(user);
        }

        public async Task<AuthResponseDto?> LoginAsync(LoginRequestDto dto)
        {
            var phone = NormalizePhone(dto.PhoneNumber);
            var user = await _db.Users.FirstOrDefaultAsync(u => u.PhoneNumber == phone);
            if (user == null || !VerifyPassword(dto.Password, user.PasswordHash, user.PasswordSalt))
            {
                return null;
            }

            var response = await CreateSessionAsync(user);
            await LogActivityAsync(user.UserId, "login", "Đăng nhập vào hệ thống", "info");
            return response;
        }

        public async Task<UserProfileDto?> GetProfileAsync(string token)
        {
            var session = await FindSessionAsync(token);
            return session?.User == null ? null : ToProfile(session.User);
        }

        public async Task<AccountSummaryDto?> GetAccountSummaryAsync(string token)
        {
            var session = await FindSessionAsync(token);
            if (session?.User == null)
            {
                return null;
            }

            var userId = session.UserId;
            var activeSessionCount = await _db.UserSessions
                .CountAsync(s => s.UserId == userId && s.ExpiresAt > DateTime.UtcNow);
            var assignedDeviceCount = await _db.UserDevices.CountAsync(x => x.UserId == userId);
            var permissions = session.User.Role == AdminRole
                ? new List<string>
                {
                    "Quản lý người dùng",
                    "Gán thiết bị",
                    "Xem toàn bộ thiết bị",
                    "Điều khiển thiết bị"
                }
                : new List<string>
                {
                    "Xem thiết bị được gán",
                    "Xem dữ liệu cảm biến",
                    "Điều khiển máy bơm được gán",
                    "Cập nhật thông tin cá nhân"
                };

            return new AccountSummaryDto
            {
                Profile = ToProfile(session.User),
                ActiveSessionCount = activeSessionCount,
                AssignedDeviceCount = assignedDeviceCount,
                Permissions = permissions
            };
        }

        public async Task<List<UserActivityDto>> GetActivitiesAsync(string token, int limit = 50)
        {
            var session = await FindSessionAsync(token);
            if (session?.User == null)
            {
                return [];
            }

            var safeLimit = Math.Clamp(limit, 1, 100);
            var items = await _db.UserActivities
                .Where(x => x.UserId == session.UserId)
                .OrderByDescending(x => x.CreatedAt)
                .Take(safeLimit)
                .ToListAsync();
            return items.Select(ToActivityDto).ToList();
        }

        public async Task<UserProfileDto?> UpdateProfileAsync(string token, UpdateProfileRequestDto dto)
        {
            var session = await FindSessionAsync(token);
            if (session?.User == null)
            {
                return null;
            }

            var fullName = dto.FullName.Trim();
            var phone = NormalizePhone(dto.PhoneNumber);
            var address = dto.Address.Trim();
            if (string.IsNullOrWhiteSpace(fullName) ||
                string.IsNullOrWhiteSpace(phone) ||
                string.IsNullOrWhiteSpace(address))
            {
                throw new InvalidOperationException("Profile fields are required");
            }

            var phoneExists = await _db.Users.AnyAsync(u =>
                u.UserId != session.UserId && u.PhoneNumber == phone);
            if (phoneExists)
            {
                throw new InvalidOperationException("Phone number already exists");
            }

            session.User.FullName = fullName;
            session.User.PhoneNumber = phone;
            session.User.Address = address;
            session.User.DateOfBirth = dto.DateOfBirth.Date;
            session.User.UpdatedAt = DateTime.UtcNow;
            await LogActivityAsync(session.UserId, "profile_update", "Cập nhật thông tin tài khoản", "info");
            await _db.SaveChangesAsync();
            return ToProfile(session.User);
        }

        public async Task<bool> ChangePasswordAsync(string token, ChangePasswordRequestDto dto)
        {
            var session = await FindSessionAsync(token);
            if (session?.User == null)
            {
                return false;
            }

            if (!VerifyPassword(dto.CurrentPassword, session.User.PasswordHash, session.User.PasswordSalt))
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(dto.NewPassword) || dto.NewPassword.Length < 6)
            {
                throw new InvalidOperationException("New password must have at least 6 characters");
            }

            var password = HashPassword(dto.NewPassword);
            session.User.PasswordHash = password.Hash;
            session.User.PasswordSalt = password.Salt;
            session.User.UpdatedAt = DateTime.UtcNow;
            await LogActivityAsync(session.UserId, "password_change", "Đổi mật khẩu đăng nhập", "warning");
            await _db.SaveChangesAsync();
            return true;
        }

        public async Task LogoutAsync(string token)
        {
            var session = await _db.UserSessions.FirstOrDefaultAsync(s => s.Token == token);
            if (session == null)
            {
                return;
            }

            await LogActivityAsync(session.UserId, "logout", "Đăng xuất khỏi hệ thống", "info");
            _db.UserSessions.Remove(session);
            await _db.SaveChangesAsync();
        }

        private async Task<AuthResponseDto> CreateSessionAsync(AppUser user)
        {
            var session = new UserSession
            {
                SessionId = Guid.NewGuid(),
                UserId = user.UserId,
                Token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48)),
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddDays(SessionDays)
            };

            _db.UserSessions.Add(session);
            await _db.SaveChangesAsync();

            return new AuthResponseDto
            {
                Token = session.Token,
                ExpiresAt = session.ExpiresAt,
                User = ToProfile(user)
            };
        }

        private async Task<UserSession?> FindSessionAsync(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                return null;
            }

            var session = await _db.UserSessions
                .Include(s => s.User)
                .FirstOrDefaultAsync(s => s.Token == token);

            if (session == null)
            {
                return null;
            }

            if (session.ExpiresAt <= DateTime.UtcNow)
            {
                _db.UserSessions.Remove(session);
                await _db.SaveChangesAsync();
                return null;
            }

            return session;
        }

        private static UserProfileDto ToProfile(AppUser user)
        {
            return new UserProfileDto
            {
                UserId = user.UserId,
                FullName = user.FullName,
                PhoneNumber = user.PhoneNumber,
                Address = user.Address,
                DateOfBirth = user.DateOfBirth,
                Role = user.Role == AdminRole ? "admin" : "user"
            };
        }

        private static UserActivityDto ToActivityDto(UserActivity activity)
        {
            var local = activity.CreatedAt.ToLocalTime();
            return new UserActivityDto
            {
                UserActivityId = activity.UserActivityId,
                Action = activity.Action,
                Description = activity.Description,
                Severity = activity.Severity,
                CreatedAt = activity.CreatedAt,
                CreatedLocal =
                    $"{local:yyyy-MM-dd HH:mm:ss}"
            };
        }

        private async Task LogActivityAsync(Guid userId, string action, string description, string severity)
        {
            _db.UserActivities.Add(new UserActivity
            {
                UserActivityId = Guid.NewGuid(),
                UserId = userId,
                Action = action,
                Description = description,
                Severity = severity,
                CreatedAt = DateTime.UtcNow
            });
            await _db.SaveChangesAsync();
        }

        private static string NormalizePhone(string phone)
        {
            return phone.Trim();
        }

        private static PasswordParts HashPassword(string password)
        {
            var salt = RandomNumberGenerator.GetBytes(16);
            using var pbkdf2 = new Rfc2898DeriveBytes(
                password,
                salt,
                100000,
                HashAlgorithmName.SHA256);
            var hash = pbkdf2.GetBytes(32);
            return new PasswordParts(Convert.ToBase64String(hash), Convert.ToBase64String(salt));
        }

        private static bool VerifyPassword(string password, string storedHash, string storedSalt)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(storedSalt))
                {
                    return VerifyPasswordParts(password, storedHash, storedSalt);
                }

                var parts = storedHash.Split('.');
                return parts.Length == 2 && VerifyPasswordParts(password, parts[1], parts[0]);
            }
            catch (FormatException)
            {
                return false;
            }
        }

        private static bool VerifyPasswordParts(string password, string storedHash, string storedSalt)
        {
            var salt = Convert.FromBase64String(storedSalt);
            var expectedHash = Convert.FromBase64String(storedHash);
            using var pbkdf2 = new Rfc2898DeriveBytes(
                password,
                salt,
                100000,
                HashAlgorithmName.SHA256);
            var actualHash = pbkdf2.GetBytes(32);
            return CryptographicOperations.FixedTimeEquals(actualHash, expectedHash);
        }

        private sealed record PasswordParts(string Hash, string Salt);
    }
}
