using Menlo.Lib.Auth.Entities;
using Menlo.Lib.Onboarding;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.Text.Json;

namespace Menlo.Application.Onboarding.EntityConfigurations;

public sealed class OnboardingStateConfiguration : IEntityTypeConfiguration<OnboardingState>
{
    private static readonly JsonSerializerOptions JsonSerializerOptions = new(JsonSerializerDefaults.Web);

    public void Configure(EntityTypeBuilder<OnboardingState> builder)
    {
        builder.ToTable("onboarding_states", "shared");

        builder.HasKey(state => state.Id);

        builder.Property(state => state.Id)
            .ValueGeneratedNever();

        builder.Property(state => state.UserId)
            .IsRequired();

        builder.HasIndex(state => state.UserId)
            .IsUnique();

        builder.HasOne<User>()
            .WithOne()
            .HasForeignKey<OnboardingState>(state => state.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        ValueComparer<IReadOnlyCollection<OnboardingTask>> completedTasksComparer = new(
            (left, right) => SerializeTasks(left) == SerializeTasks(right),
            tasks => SerializeTasks(tasks).GetHashCode(),
            tasks => CloneTasks(tasks));

        builder.Property(state => state.CompletedTasks)
            .HasColumnType("jsonb")
            .HasDefaultValueSql("'[]'::jsonb")
            .HasConversion(
                tasks => SerializeTasks(tasks),
                json => DeserializeTasks(json))
            .Metadata.SetValueComparer(completedTasksComparer);

        builder.Property(state => state.CreatedAt)
            .HasColumnType("timestamp with time zone");

        builder.Property(state => state.UpdatedAt)
            .HasColumnType("timestamp with time zone");

        builder.Ignore(state => state.DomainEvents);
    }

    private static IReadOnlyCollection<OnboardingTask> CloneTasks(IReadOnlyCollection<OnboardingTask>? tasks)
    {
        if (tasks is null)
        {
            return [];
        }

        return tasks
            .Select(task => task with
            {
                Metadata = task.Metadata is null ? null : new Dictionary<string, object>(task.Metadata)
            })
            .ToList();
    }

    private static IReadOnlyCollection<OnboardingTask> DeserializeTasks(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return [];
        }

        return JsonSerializer.Deserialize<List<OnboardingTask>>(json, JsonSerializerOptions) ?? [];
    }

    private static string SerializeTasks(IReadOnlyCollection<OnboardingTask>? tasks) =>
        JsonSerializer.Serialize(tasks ?? [], JsonSerializerOptions);
}
