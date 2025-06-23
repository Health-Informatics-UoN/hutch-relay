using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hutch.Relay.Migrations
{
    /// <inheritdoc />
    public partial class RelaySubTask_ParentRequired : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RelaySubTasks_RelayTasks_RelayTaskId",
                table: "RelaySubTasks");

            migrationBuilder.AlterColumn<string>(
                name: "RelayTaskId",
                table: "RelaySubTasks",
                type: "character varying(255)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(255)",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_RelaySubTasks_RelayTasks_RelayTaskId",
                table: "RelaySubTasks",
                column: "RelayTaskId",
                principalTable: "RelayTasks",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RelaySubTasks_RelayTasks_RelayTaskId",
                table: "RelaySubTasks");

            migrationBuilder.AlterColumn<string>(
                name: "RelayTaskId",
                table: "RelaySubTasks",
                type: "character varying(255)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(255)");

            migrationBuilder.AddForeignKey(
                name: "FK_RelaySubTasks_RelayTasks_RelayTaskId",
                table: "RelaySubTasks",
                column: "RelayTaskId",
                principalTable: "RelayTasks",
                principalColumn: "Id");
        }
    }
}
