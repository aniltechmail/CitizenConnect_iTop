using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "departments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_departments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "districts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_districts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "internal_users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FullName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Email = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    PasswordHash = table.Column<string>(type: "text", nullable: false),
                    Role = table.Column<int>(type: "integer", nullable: false),
                    DepartmentId = table.Column<int>(type: "integer", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastLoginAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_internal_users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "complaint_categories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    DepartmentId = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_complaint_categories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_complaint_categories_departments_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "departments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "constituencies",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DistrictId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_constituencies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_constituencies_districts_DistrictId",
                        column: x => x.DistrictId,
                        principalTable: "districts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "sla_policies",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CategoryId = table.Column<int>(type: "integer", nullable: false),
                    ResponseHours = table.Column<int>(type: "integer", nullable: false),
                    ResolutionHours = table.Column<int>(type: "integer", nullable: false),
                    EscalationLevel1Hours = table.Column<int>(type: "integer", nullable: false),
                    EscalationLevel2Hours = table.Column<int>(type: "integer", nullable: false),
                    EscalationLevel3Hours = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sla_policies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_sla_policies_complaint_categories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "complaint_categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "areas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ConstituencyId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_areas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_areas_constituencies_ConstituencyId",
                        column: x => x.ConstituencyId,
                        principalTable: "constituencies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "blocks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AreaId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_blocks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_blocks_areas_AreaId",
                        column: x => x.AreaId,
                        principalTable: "areas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "citizens",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FullName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Phone = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: false),
                    Email = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    PasswordHash = table.Column<string>(type: "text", nullable: false),
                    IsVerified = table.Column<bool>(type: "boolean", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastLoginAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    BlockId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_citizens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_citizens_blocks_BlockId",
                        column: x => x.BlockId,
                        principalTable: "blocks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "complaints",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RefNumber = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Title = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Priority = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    SubmittedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    AssignedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ResolvedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ClosedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CitizenId = table.Column<Guid>(type: "uuid", nullable: false),
                    BlockId = table.Column<int>(type: "integer", nullable: false),
                    CategoryId = table.Column<int>(type: "integer", nullable: false),
                    AssignedDepartmentId = table.Column<int>(type: "integer", nullable: true),
                    AssignedAgentId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_complaints", x => x.Id);
                    table.ForeignKey(
                        name: "FK_complaints_blocks_BlockId",
                        column: x => x.BlockId,
                        principalTable: "blocks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_complaints_citizens_CitizenId",
                        column: x => x.CitizenId,
                        principalTable: "citizens",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_complaints_complaint_categories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "complaint_categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_complaints_departments_AssignedDepartmentId",
                        column: x => x.AssignedDepartmentId,
                        principalTable: "departments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_complaints_internal_users_AssignedAgentId",
                        column: x => x.AssignedAgentId,
                        principalTable: "internal_users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ComplaintFeedbacks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ComplaintId = table.Column<Guid>(type: "uuid", nullable: false),
                    CitizenId = table.Column<Guid>(type: "uuid", nullable: false),
                    Rating = table.Column<int>(type: "integer", nullable: false),
                    Comments = table.Column<string>(type: "text", nullable: true),
                    CollectedById = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ComplaintFeedbacks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ComplaintFeedbacks_citizens_CitizenId",
                        column: x => x.CitizenId,
                        principalTable: "citizens",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ComplaintFeedbacks_complaints_ComplaintId",
                        column: x => x.ComplaintId,
                        principalTable: "complaints",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ComplaintFeedbacks_internal_users_CollectedById",
                        column: x => x.CollectedById,
                        principalTable: "internal_users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ComplaintITopMappings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ComplaintId = table.Column<Guid>(type: "uuid", nullable: false),
                    ITopTicketRef = table.Column<string>(type: "text", nullable: false),
                    ITopTicketId = table.Column<string>(type: "text", nullable: false),
                    ITopClass = table.Column<string>(type: "text", nullable: false),
                    LastSyncedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    SyncStatus = table.Column<int>(type: "integer", nullable: false),
                    LastSyncError = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ComplaintITopMappings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ComplaintITopMappings_complaints_ComplaintId",
                        column: x => x.ComplaintId,
                        principalTable: "complaints",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ComplaintMedia",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ComplaintId = table.Column<Guid>(type: "uuid", nullable: false),
                    MediaType = table.Column<int>(type: "integer", nullable: false),
                    FileName = table.Column<string>(type: "text", nullable: false),
                    FilePath = table.Column<string>(type: "text", nullable: false),
                    FileSize = table.Column<long>(type: "bigint", nullable: false),
                    MimeType = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ComplaintMedia", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ComplaintMedia_complaints_ComplaintId",
                        column: x => x.ComplaintId,
                        principalTable: "complaints",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ComplaintMessages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ComplaintId = table.Column<Guid>(type: "uuid", nullable: false),
                    SenderType = table.Column<int>(type: "integer", nullable: false),
                    SenderId = table.Column<Guid>(type: "uuid", nullable: false),
                    Message = table.Column<string>(type: "text", nullable: false),
                    IsRead = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ComplaintMessages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ComplaintMessages_complaints_ComplaintId",
                        column: x => x.ComplaintId,
                        principalTable: "complaints",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "escalation_events",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ComplaintId = table.Column<Guid>(type: "uuid", nullable: false),
                    Level = table.Column<int>(type: "integer", nullable: false),
                    Reason = table.Column<string>(type: "text", nullable: false),
                    TriggeredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ResolvedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_escalation_events", x => x.Id);
                    table.ForeignKey(
                        name: "FK_escalation_events_complaints_ComplaintId",
                        column: x => x.ComplaintId,
                        principalTable: "complaints",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "notifications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserType = table.Column<int>(type: "integer", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ComplaintId = table.Column<Guid>(type: "uuid", nullable: true),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    Message = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    IsRead = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_notifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_notifications_complaints_ComplaintId",
                        column: x => x.ComplaintId,
                        principalTable: "complaints",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_areas_Code",
                table: "areas",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_areas_ConstituencyId",
                table: "areas",
                column: "ConstituencyId");

            migrationBuilder.CreateIndex(
                name: "IX_blocks_AreaId",
                table: "blocks",
                column: "AreaId");

            migrationBuilder.CreateIndex(
                name: "IX_blocks_Code",
                table: "blocks",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_citizens_BlockId",
                table: "citizens",
                column: "BlockId");

            migrationBuilder.CreateIndex(
                name: "IX_citizens_Email",
                table: "citizens",
                column: "Email",
                unique: true,
                filter: "\"Email\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_citizens_Phone",
                table: "citizens",
                column: "Phone",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_complaint_categories_DepartmentId",
                table: "complaint_categories",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_ComplaintFeedbacks_CitizenId",
                table: "ComplaintFeedbacks",
                column: "CitizenId");

            migrationBuilder.CreateIndex(
                name: "IX_ComplaintFeedbacks_CollectedById",
                table: "ComplaintFeedbacks",
                column: "CollectedById");

            migrationBuilder.CreateIndex(
                name: "IX_ComplaintFeedbacks_ComplaintId",
                table: "ComplaintFeedbacks",
                column: "ComplaintId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ComplaintITopMappings_ComplaintId",
                table: "ComplaintITopMappings",
                column: "ComplaintId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ComplaintMedia_ComplaintId",
                table: "ComplaintMedia",
                column: "ComplaintId");

            migrationBuilder.CreateIndex(
                name: "IX_ComplaintMessages_ComplaintId",
                table: "ComplaintMessages",
                column: "ComplaintId");

            migrationBuilder.CreateIndex(
                name: "IX_complaints_AssignedAgentId",
                table: "complaints",
                column: "AssignedAgentId");

            migrationBuilder.CreateIndex(
                name: "IX_complaints_AssignedDepartmentId",
                table: "complaints",
                column: "AssignedDepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_complaints_BlockId",
                table: "complaints",
                column: "BlockId");

            migrationBuilder.CreateIndex(
                name: "IX_complaints_CategoryId",
                table: "complaints",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_complaints_CitizenId",
                table: "complaints",
                column: "CitizenId");

            migrationBuilder.CreateIndex(
                name: "IX_complaints_RefNumber",
                table: "complaints",
                column: "RefNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_constituencies_Code",
                table: "constituencies",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_constituencies_DistrictId",
                table: "constituencies",
                column: "DistrictId");

            migrationBuilder.CreateIndex(
                name: "IX_departments_Code",
                table: "departments",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_districts_Code",
                table: "districts",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_escalation_events_ComplaintId",
                table: "escalation_events",
                column: "ComplaintId");

            migrationBuilder.CreateIndex(
                name: "IX_internal_users_Email",
                table: "internal_users",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_notifications_ComplaintId",
                table: "notifications",
                column: "ComplaintId");

            migrationBuilder.CreateIndex(
                name: "IX_sla_policies_CategoryId",
                table: "sla_policies",
                column: "CategoryId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ComplaintFeedbacks");

            migrationBuilder.DropTable(
                name: "ComplaintITopMappings");

            migrationBuilder.DropTable(
                name: "ComplaintMedia");

            migrationBuilder.DropTable(
                name: "ComplaintMessages");

            migrationBuilder.DropTable(
                name: "escalation_events");

            migrationBuilder.DropTable(
                name: "notifications");

            migrationBuilder.DropTable(
                name: "sla_policies");

            migrationBuilder.DropTable(
                name: "complaints");

            migrationBuilder.DropTable(
                name: "citizens");

            migrationBuilder.DropTable(
                name: "complaint_categories");

            migrationBuilder.DropTable(
                name: "internal_users");

            migrationBuilder.DropTable(
                name: "blocks");

            migrationBuilder.DropTable(
                name: "departments");

            migrationBuilder.DropTable(
                name: "areas");

            migrationBuilder.DropTable(
                name: "constituencies");

            migrationBuilder.DropTable(
                name: "districts");
        }
    }
}
