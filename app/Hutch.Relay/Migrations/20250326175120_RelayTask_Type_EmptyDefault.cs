using Hutch.Relay.Constants;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hutch.Relay.Migrations
{
    /// <inheritdoc />
    public partial class RelayTask_Type_EmptyDefault : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
          migrationBuilder.AlterColumn<string>(
            name: "Type",
            table: "RelayTasks",
            type: "character varying(512)",
            maxLength: 512,
            nullable: false,
            oldMaxLength: 512,
            oldClrType: typeof(string),
            oldType: "text",
            oldNullable: false,
            defaultValue: "",
            oldDefaultValue: TaskTypes.TaskApi_Availability);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
          migrationBuilder.AlterColumn<string>(
            name: "Type",
            table: "RelayTasks",
            type: "character varying(512)",
            maxLength: 512,
            nullable: false,
            oldMaxLength: 512,
            oldClrType: typeof(string),
            oldType: "text",
            oldNullable: false,
            defaultValue: TaskTypes.TaskApi_Availability,
            oldDefaultValue: "");
        }
    }
}
