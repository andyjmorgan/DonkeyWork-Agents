using DonkeyWork.Agents.Persistence.Entities.Mcp;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DonkeyWork.Agents.Persistence.Configurations.Mcp;

/// <summary>
/// EF Core configuration for the MCP tool invocation log entity.
/// </summary>
public class McpToolInvocationLogConfiguration : IEntityTypeConfiguration<McpToolInvocationLogEntity>
{
    public void Configure(EntityTypeBuilder<McpToolInvocationLogEntity> builder)
    {
        builder.ToTable("tool_invocation_logs", "mcp");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(e => e.UserId)
            .HasColumnName("user_id");

        builder.Property(e => e.ToolName)
            .HasColumnName("tool_name")
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(e => e.ToolProvider)
            .HasColumnName("tool_provider")
            .HasMaxLength(200);

        builder.Property(e => e.RequestParams)
            .HasColumnName("request_params")
            .IsRequired()
            .HasColumnType("jsonb");

        builder.Property(e => e.ResponseContent)
            .HasColumnName("response_content")
            .HasColumnType("jsonb");

        builder.Property(e => e.IsSuccess)
            .HasColumnName("is_success")
            .IsRequired();

        builder.Property(e => e.ErrorMessage)
            .HasColumnName("error_message");

        builder.Property(e => e.InvokedAt)
            .HasColumnName("invoked_at")
            .IsRequired();

        builder.Property(e => e.CompletedAt)
            .HasColumnName("completed_at");

        builder.Property(e => e.DurationMs)
            .HasColumnName("duration_ms");

        builder.Property(e => e.ClientId)
            .HasColumnName("client_id")
            .HasMaxLength(500);

        builder.Property(e => e.ClientIpAddress)
            .HasColumnName("client_ip_address")
            .HasMaxLength(45); // IPv6 max length

        builder.Property(e => e.UserAgent)
            .HasColumnName("user_agent")
            .HasMaxLength(1000);

        builder.Property(e => e.SessionId)
            .HasColumnName("session_id")
            .HasMaxLength(500);

        builder.Property(e => e.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        // Indexes for common query patterns
        builder.HasIndex(e => e.UserId)
            .HasDatabaseName("ix_mcp_tool_invocation_logs_user_id");

        builder.HasIndex(e => e.ToolName)
            .HasDatabaseName("ix_mcp_tool_invocation_logs_tool_name");

        builder.HasIndex(e => e.InvokedAt)
            .HasDatabaseName("ix_mcp_tool_invocation_logs_invoked_at");

        builder.HasIndex(e => new { e.UserId, e.InvokedAt })
            .HasDatabaseName("ix_mcp_tool_invocation_logs_user_id_invoked_at");

        builder.HasIndex(e => new { e.ToolName, e.InvokedAt })
            .HasDatabaseName("ix_mcp_tool_invocation_logs_tool_name_invoked_at");
    }
}
