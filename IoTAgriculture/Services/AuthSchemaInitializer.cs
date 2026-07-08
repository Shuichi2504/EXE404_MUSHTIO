using IoTAgriculture.Data;
using IoTAgriculture.Models;
using Microsoft.EntityFrameworkCore;

namespace IoTAgriculture.Services
{
    public static class AuthSchemaInitializer
    {
        public static async Task InitializeAsync(IServiceProvider services)
        {
            using var scope = services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<IoTDbContext>();

            await db.Database.ExecuteSqlRawAsync("""
                IF OBJECT_ID(N'[Users]', N'U') IS NULL
                BEGIN
                    CREATE TABLE [Users] (
                        [UserId] uniqueidentifier NOT NULL CONSTRAINT [PK_Users] PRIMARY KEY,
                        [PasswordHash] nvarchar(255) NOT NULL,
                        [FullName] nvarchar(255) NULL,
                        [PhoneNumber] nvarchar(20) NOT NULL,
                        [Address] nvarchar(255) NOT NULL,
                        [DateOfBirth] datetime2 NOT NULL,
                        [CreatedAt] datetime2 NOT NULL,
                        [UpdatedAt] datetime2 NOT NULL,
                        [Role] int NOT NULL,
                        [PasswordSalt] nvarchar(256) NOT NULL
                    );
                    CREATE UNIQUE INDEX [IX_Users_PhoneNumber] ON [Users] ([PhoneNumber]);
                END
                """);

            await db.Database.ExecuteSqlRawAsync("""
                IF COL_LENGTH(N'[Users]', N'DateOfBirth') IS NULL
                BEGIN
                    ALTER TABLE [Users] ADD [DateOfBirth] datetime2 NOT NULL
                        CONSTRAINT [DF_Users_DateOfBirth] DEFAULT ('2000-01-01');
                END
                """);

            await db.Database.ExecuteSqlRawAsync("""
                IF OBJECT_ID(N'[UserSessions]', N'U') IS NULL
                BEGIN
                    CREATE TABLE [UserSessions] (
                        [SessionId] uniqueidentifier NOT NULL CONSTRAINT [PK_UserSessions] PRIMARY KEY,
                        [UserId] uniqueidentifier NOT NULL,
                        [Token] nvarchar(128) NOT NULL,
                        [CreatedAt] datetime2 NOT NULL,
                        [ExpiresAt] datetime2 NOT NULL,
                        CONSTRAINT [FK_UserSessions_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([UserId]) ON DELETE CASCADE
                    );
                    CREATE UNIQUE INDEX [IX_UserSessions_Token] ON [UserSessions] ([Token]);
                    CREATE INDEX [IX_UserSessions_UserId] ON [UserSessions] ([UserId]);
                END
                """);

            await db.Database.ExecuteSqlRawAsync("""
                IF OBJECT_ID(N'[UserDevices]', N'U') IS NULL
                BEGIN
                    CREATE TABLE [UserDevices] (
                        [UserDeviceId] uniqueidentifier NOT NULL CONSTRAINT [PK_UserDevices] PRIMARY KEY,
                        [UserId] uniqueidentifier NOT NULL,
                        [DeviceKey] nvarchar(100) NOT NULL,
                        [DeviceName] nvarchar(255) NULL,
                        [AssignedAt] datetime2 NOT NULL
                    );
                    CREATE UNIQUE INDEX [IX_UserDevices_UserId_DeviceKey] ON [UserDevices] ([UserId], [DeviceKey]);
                END
                """);

            await db.Database.ExecuteSqlRawAsync("""
                IF OBJECT_ID(N'[UserActivities]', N'U') IS NULL
                BEGIN
                    CREATE TABLE [UserActivities] (
                        [UserActivityId] uniqueidentifier NOT NULL CONSTRAINT [PK_UserActivities] PRIMARY KEY,
                        [UserId] uniqueidentifier NOT NULL,
                        [Action] nvarchar(80) NOT NULL,
                        [Description] nvarchar(255) NOT NULL,
                        [Severity] nvarchar(64) NOT NULL,
                        [CreatedAt] datetime2 NOT NULL,
                        CONSTRAINT [FK_UserActivities_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([UserId]) ON DELETE CASCADE
                    );
                    CREATE INDEX [IX_UserActivities_UserId_CreatedAt] ON [UserActivities] ([UserId], [CreatedAt]);
                END
                """);
            await db.Database.ExecuteSqlRawAsync("""
                IF OBJECT_ID(N'[ChatMessages]', N'U') IS NULL
                BEGIN
                    CREATE TABLE [ChatMessages] (
                        [MessageId] uniqueidentifier NOT NULL CONSTRAINT [PK_ChatMessages] PRIMARY KEY,
                        [UserId] uniqueidentifier NOT NULL,
                        [Sender] nvarchar(50) NOT NULL,
                        [MessageText] nvarchar(max) NULL,
                        [ImageUrl] nvarchar(500) NULL,
                        [CreatedAt] datetime2 NOT NULL,
                        CONSTRAINT [FK_ChatMessages_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([UserId]) ON DELETE CASCADE
                    );
                    CREATE INDEX [IX_ChatMessages_UserId_CreatedAt] ON [ChatMessages] ([UserId], [CreatedAt]);
                END
                """);
            await SeedAdminAsync(db);
        }

        private static async Task SeedAdminAsync(IoTDbContext db)
        {
            const string adminPhone = "0900000000";
            if (await db.Users.AnyAsync(u => u.PhoneNumber == adminPhone))
            {
                return;
            }

            var password = AuthPasswordSeed.Hash("Admin@123456");
            db.Users.Add(new AppUser
            {
                UserId = Guid.NewGuid(),
                FullName = "Administrator",
                PhoneNumber = adminPhone,
                Address = "Admin",
                DateOfBirth = new DateTime(2000, 1, 1),
                Role = 1,
                PasswordHash = password.Hash,
                PasswordSalt = password.Salt,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });

            await db.SaveChangesAsync();
        }

        private static class AuthPasswordSeed
        {
            public static PasswordParts Hash(string password)
            {
                var salt = System.Security.Cryptography.RandomNumberGenerator.GetBytes(16);
                using var pbkdf2 = new System.Security.Cryptography.Rfc2898DeriveBytes(
                    password,
                    salt,
                    100000,
                    System.Security.Cryptography.HashAlgorithmName.SHA256);
                var hash = pbkdf2.GetBytes(32);
                return new PasswordParts(Convert.ToBase64String(hash), Convert.ToBase64String(salt));
            }

            public sealed record PasswordParts(string Hash, string Salt);
        }
    }
}
