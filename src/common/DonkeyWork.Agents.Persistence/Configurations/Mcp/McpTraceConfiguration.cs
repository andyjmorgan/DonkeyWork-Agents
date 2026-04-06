using DonkeyWork.Agents.Persistence.Entities.Mcp;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DonkeyWork.Agents.Persistence.Configurations.Mcp;

public class McpTraceConfiguration : IEntityTypeConfiguration<McpTraceEntity>
{
    public void Configure(EntityTypeBuilder<McpTraceEntity> builder)
    {
        builder.ToTable("traces", "mcp");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(e => e.UserId)
            .HasColumnName("user_id");

        builder.Property(e => e.Method)
            .HasColumnName("method")
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(e => e.JsonRpcId)
            .HasColumnName("jsonrpc_id")
            .HasMaxLength(200);

        builder.Property(e => e.RequestBody)
            .HasColumnName("request_body")
            .IsRequired()
            .HasColumnType("text");

        builder.Property(e => e.ResponseBody)
            .HasColumnName("response_body")
            .HasColumnType("text");

        builder.Property(e => e.HttpStatusCode)
            .HasColumnName("http_status_code")
            .IsRequired();

        builder.Property(e => e.IsSuccess)
            .HasColumnName("is_success")
            .IsRequired();

        builder.Property(e => e.ErrorMessage)
            .HasColumnName("error_message");

        builder.Property(e => e.ClientIpAddress)
            .HasColumnName("client_ip_address")
            .HasMaxLength(45);

        builder.Property(e => e.UserAgent)
            .HasColumnName("user_agent")
            .HasMaxLength(1000);

        builder.Property(e => e.StartedAt)
            .HasColumnName("started_at")
            .IsRequired();

        builder.Property(e => e.CompletedAt)
            .HasColumnName("completed_at");

        builder.Property(e => e.DurationMs)
            .HasColumnName("duration_ms");

        builder.Property(e => e.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.HasIndex(e => e.UserId)
            .HasDatabaseName("ix_mcp_traces_user_id");

        builder.HasIndex(e => e.Method)
            .HasDatabaseName("ix_mcp_traces_method");

        builder.HasIndex(e => e.StartedAt)
            .HasDatabaseName("ix_mcp_traces_started_at");

        builder.HasIndex(e => new { e.UserId, e.StartedAt })
            .HasDatabaseName("ix_mcp_traces_user_id_started_at");

        builder.HasIndex(e => new { e.Method, e.StartedAt })
            .HasDatabaseName("ix_mcp_traces_method_started_at");
    }
}
