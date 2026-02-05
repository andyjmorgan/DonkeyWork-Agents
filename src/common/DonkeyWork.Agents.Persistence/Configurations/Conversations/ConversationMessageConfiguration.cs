using System.Text.Json;
using DonkeyWork.Agents.Conversations.Contracts.Models;
using DonkeyWork.Agents.Persistence.Entities.Conversations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DonkeyWork.Agents.Persistence.Configurations.Conversations;

public class ConversationMessageConfiguration : IEntityTypeConfiguration<ConversationMessageEntity>
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        AllowOutOfOrderMetadataProperties = true
    };

    public void Configure(EntityTypeBuilder<ConversationMessageEntity> builder)
    {
        builder.ToTable("conversation_messages", "conversations");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(e => e.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        builder.Property(e => e.ConversationId)
            .HasColumnName("conversation_id")
            .IsRequired();

        builder.Property(e => e.Role)
            .HasColumnName("role")
            .IsRequired()
            .HasConversion<string>();

        builder.Property(e => e.Content)
            .HasColumnName("content")
            .IsRequired()
            .HasColumnType("jsonb")
            .HasConversion(
                v => JsonSerializer.Serialize(v, JsonOptions),
                v => JsonSerializer.Deserialize<List<ContentPart>>(v, JsonOptions) ?? new List<ContentPart>());

        builder.Property(e => e.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        // Messages are immutable, no UpdatedAt needed but BaseEntity has it
        builder.Property(e => e.UpdatedAt)
            .HasColumnName("updated_at");

        // Indexes
        builder.HasIndex(e => e.ConversationId)
            .HasDatabaseName("ix_conversation_messages_conversation_id");

        builder.HasIndex(e => e.CreatedAt)
            .HasDatabaseName("ix_conversation_messages_created_at");

        builder.HasIndex(e => e.UserId)
            .HasDatabaseName("ix_conversation_messages_user_id");
    }
}
