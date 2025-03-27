using Hutch.Relay.Constants;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hutch.Relay.Migrations
{
    /// <inheritdoc />
    public partial class RelayTask_Type : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Collection",
                table: "RelayTasks",
                type: "character varying(255)",
                maxLength: 255,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "Id",
                table: "RelayTasks",
                type: "character varying(255)",
                maxLength: 255,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<string>(
                name: "Type",
                table: "RelayTasks",
                type: "character varying(512)",
                maxLength: 512,
                nullable: false,
                // Here we default to availability so that new versions of Relay behave as it did prior to the Type column
                // The next migration removes the default value (which we don't really want, hence it not being on the entity itself)
                // but by that time this migration will have filled the value for existing records when the column is added
                // Making data migration safe.
                defaultValue: TaskTypes.TaskApi_Availability);

            migrationBuilder.AlterColumn<string>(
                name: "RelayTaskId",
                table: "RelaySubTasks",
                type: "character varying(255)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Type",
                table: "RelayTasks");

            migrationBuilder.AlterColumn<string>(
                name: "Collection",
                table: "RelayTasks",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(255)",
                oldMaxLength: 255);

            migrationBuilder.AlterColumn<string>(
                name: "Id",
                table: "RelayTasks",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(255)",
                oldMaxLength: 255);

            migrationBuilder.AlterColumn<string>(
                name: "RelayTaskId",
                table: "RelaySubTasks",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(255)",
                oldNullable: true);
        }
    }
}
