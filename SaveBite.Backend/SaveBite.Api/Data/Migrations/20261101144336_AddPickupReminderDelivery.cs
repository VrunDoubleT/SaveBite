using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable
namespace SaveBite.Backend.Data.Migrations;

public partial class AddPickupReminderDelivery : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
        => migrationBuilder.AddColumn<DateTime>(name: "pickup_reminder_email_sent_at", table: "orders",
            type: "timestamp with time zone", nullable: true);

    protected override void Down(MigrationBuilder migrationBuilder)
        => migrationBuilder.DropColumn(name: "pickup_reminder_email_sent_at", table: "orders");
}
