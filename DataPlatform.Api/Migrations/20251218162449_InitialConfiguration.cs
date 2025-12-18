using System;
using System.Collections.Generic;
using DataPlatform.Api.Models;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataPlatform.Api.Migrations
{
    /// <inheritdoc />
    public partial class InitialConfiguration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DataSources",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    ConnectionString = table.Column<string>(type: "text", nullable: false),
                    Configuration = table.Column<Dictionary<string, string>>(type: "jsonb", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastTestedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DataSources", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Username = table.Column<string>(type: "text", nullable: false),
                    PasswordHash = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Datasets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DataSourceId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    TableName = table.Column<string>(type: "text", nullable: false),
                    Schema = table.Column<List<DatasetColumn>>(type: "jsonb", nullable: false),
                    RowCount = table.Column<long>(type: "bigint", nullable: true),
                    SizeBytes = table.Column<long>(type: "bigint", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastSyncedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    Layer = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    TableType = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    BusinessKey = table.Column<string>(type: "text", nullable: true),
                    GrainDescription = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Datasets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Datasets_DataSources_DataSourceId",
                        column: x => x.DataSourceId,
                        principalTable: "DataSources",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DataQualityRules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DatasetId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    ValidationQuery = table.Column<string>(type: "text", nullable: false),
                    Threshold = table.Column<string>(type: "text", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DataQualityRules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DataQualityRules_Datasets_DatasetId",
                        column: x => x.DatasetId,
                        principalTable: "Datasets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Pipelines",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    SourceQuery = table.Column<string>(type: "text", nullable: false),
                    SourceDatasetId = table.Column<Guid>(type: "uuid", nullable: false),
                    TargetDatasetId = table.Column<Guid>(type: "uuid", nullable: true),
                    Schedule = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastRunAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Pipelines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Pipelines_Datasets_SourceDatasetId",
                        column: x => x.SourceDatasetId,
                        principalTable: "Datasets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Pipelines_Datasets_TargetDatasetId",
                        column: x => x.TargetDatasetId,
                        principalTable: "Datasets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Queries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    SqlQuery = table.Column<string>(type: "text", nullable: false),
                    DatasetId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExecutedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RowsReturned = table.Column<long>(type: "bigint", nullable: true),
                    ExecutionTimeMs = table.Column<double>(type: "double precision", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Queries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Queries_Datasets_DatasetId",
                        column: x => x.DatasetId,
                        principalTable: "Datasets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DataQualityChecks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RuleId = table.Column<Guid>(type: "uuid", nullable: false),
                    ExecutedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Passed = table.Column<bool>(type: "boolean", nullable: false),
                    Score = table.Column<double>(type: "double precision", nullable: false),
                    RowsChecked = table.Column<long>(type: "bigint", nullable: false),
                    RowsFailed = table.Column<long>(type: "bigint", nullable: false),
                    Details = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DataQualityChecks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DataQualityChecks_DataQualityRules_RuleId",
                        column: x => x.RuleId,
                        principalTable: "DataQualityRules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DataLineages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceDatasetId = table.Column<Guid>(type: "uuid", nullable: false),
                    TargetDatasetId = table.Column<Guid>(type: "uuid", nullable: false),
                    PipelineId = table.Column<Guid>(type: "uuid", nullable: true),
                    TransformationDescription = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DataLineages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DataLineages_Datasets_SourceDatasetId",
                        column: x => x.SourceDatasetId,
                        principalTable: "Datasets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DataLineages_Datasets_TargetDatasetId",
                        column: x => x.TargetDatasetId,
                        principalTable: "Datasets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DataLineages_Pipelines_PipelineId",
                        column: x => x.PipelineId,
                        principalTable: "Pipelines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "PipelineRuns",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PipelineId = table.Column<Guid>(type: "uuid", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    RowsProcessed = table.Column<long>(type: "bigint", nullable: false),
                    RowsFailed = table.Column<long>(type: "bigint", nullable: false),
                    ErrorMessage = table.Column<string>(type: "text", nullable: true),
                    Metrics = table.Column<Dictionary<string, object>>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PipelineRuns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PipelineRuns_Pipelines_PipelineId",
                        column: x => x.PipelineId,
                        principalTable: "Pipelines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DataLineages_PipelineId",
                table: "DataLineages",
                column: "PipelineId");

            migrationBuilder.CreateIndex(
                name: "IX_DataLineages_SourceDatasetId",
                table: "DataLineages",
                column: "SourceDatasetId");

            migrationBuilder.CreateIndex(
                name: "IX_DataLineages_TargetDatasetId",
                table: "DataLineages",
                column: "TargetDatasetId");

            migrationBuilder.CreateIndex(
                name: "IX_DataQualityChecks_ExecutedAt",
                table: "DataQualityChecks",
                column: "ExecutedAt");

            migrationBuilder.CreateIndex(
                name: "IX_DataQualityChecks_RuleId",
                table: "DataQualityChecks",
                column: "RuleId");

            migrationBuilder.CreateIndex(
                name: "IX_DataQualityRules_DatasetId",
                table: "DataQualityRules",
                column: "DatasetId");

            migrationBuilder.CreateIndex(
                name: "IX_DataQualityRules_IsActive",
                table: "DataQualityRules",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Datasets_DataSourceId",
                table: "Datasets",
                column: "DataSourceId");

            migrationBuilder.CreateIndex(
                name: "IX_Datasets_Layer",
                table: "Datasets",
                column: "Layer");

            migrationBuilder.CreateIndex(
                name: "IX_Datasets_Name",
                table: "Datasets",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_Datasets_TableType",
                table: "Datasets",
                column: "TableType");

            migrationBuilder.CreateIndex(
                name: "IX_DataSources_Status",
                table: "DataSources",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_DataSources_Type",
                table: "DataSources",
                column: "Type");

            migrationBuilder.CreateIndex(
                name: "IX_PipelineRuns_PipelineId",
                table: "PipelineRuns",
                column: "PipelineId");

            migrationBuilder.CreateIndex(
                name: "IX_PipelineRuns_StartedAt",
                table: "PipelineRuns",
                column: "StartedAt");

            migrationBuilder.CreateIndex(
                name: "IX_PipelineRuns_Status",
                table: "PipelineRuns",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Pipelines_LastRunAt",
                table: "Pipelines",
                column: "LastRunAt");

            migrationBuilder.CreateIndex(
                name: "IX_Pipelines_SourceDatasetId",
                table: "Pipelines",
                column: "SourceDatasetId");

            migrationBuilder.CreateIndex(
                name: "IX_Pipelines_Status",
                table: "Pipelines",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Pipelines_TargetDatasetId",
                table: "Pipelines",
                column: "TargetDatasetId");

            migrationBuilder.CreateIndex(
                name: "IX_Queries_CreatedAt",
                table: "Queries",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Queries_CreatedBy",
                table: "Queries",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Queries_DatasetId",
                table: "Queries",
                column: "DatasetId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DataLineages");

            migrationBuilder.DropTable(
                name: "DataQualityChecks");

            migrationBuilder.DropTable(
                name: "PipelineRuns");

            migrationBuilder.DropTable(
                name: "Queries");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "DataQualityRules");

            migrationBuilder.DropTable(
                name: "Pipelines");

            migrationBuilder.DropTable(
                name: "Datasets");

            migrationBuilder.DropTable(
                name: "DataSources");
        }
    }
}
