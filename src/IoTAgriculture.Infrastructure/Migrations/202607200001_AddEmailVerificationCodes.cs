using IoTAgriculture.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IoTAgriculture.Infrastructure.Migrations
{
    [DbContext(typeof(IoTDbContext))]
    [Migration("202607200001_AddEmailVerificationCodes")]
    public partial class AddEmailVerificationCodes : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF OBJECT_ID(N'[EmailVerificationCodes]', N'U') IS NULL
                BEGIN
                    CREATE TABLE [EmailVerificationCodes] (
                        [VerificationId] uniqueidentifier NOT NULL CONSTRAINT [PK_EmailVerificationCodes] PRIMARY KEY,
                        [Email] nvarchar(120) NOT NULL,
                        [Code] nvarchar(6) NOT NULL,
                        [Purpose] nvarchar(40) NOT NULL,
                        [UserId] uniqueidentifier NULL,
                        [CreatedAt] datetime2 NOT NULL,
                        [ExpiresAt] datetime2 NOT NULL,
                        [VerifiedAt] datetime2 NULL,
                        [UsedAt] datetime2 NULL
                    );

                    CREATE INDEX [IX_EmailVerificationCodes_Email_Purpose_Code]
                        ON [EmailVerificationCodes] ([Email], [Purpose], [Code]);
                END
                """);

            migrationBuilder.Sql("""
                IF COL_LENGTH(N'[EmailVerificationCodes]', N'VerifiedAt') IS NULL
                BEGIN
                    ALTER TABLE [EmailVerificationCodes] ADD [VerifiedAt] datetime2 NULL;
                END
                """);

            migrationBuilder.Sql("""
                IF COL_LENGTH(N'[EmailVerificationCodes]', N'UsedAt') IS NULL
                BEGIN
                    ALTER TABLE [EmailVerificationCodes] ADD [UsedAt] datetime2 NULL;
                END
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF OBJECT_ID(N'[EmailVerificationCodes]', N'U') IS NOT NULL
                BEGIN
                    DROP TABLE [EmailVerificationCodes];
                END
                """);
        }
    }
}
