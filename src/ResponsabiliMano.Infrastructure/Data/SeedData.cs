using Microsoft.EntityFrameworkCore;
using ResponsabiliMano.Core.Entities;
using ResponsabiliMano.Core.Enums;
using ResponsabiliMano.Infrastructure.Identity;

namespace ResponsabiliMano.Infrastructure.Data;

/// <summary>
/// Demo fixture: the duo, one project mid-flight with seven weeks of history, and one
/// still being negotiated. The history is what makes the dashboard charts, averages and
/// streak render with something worth looking at.
/// </summary>
public static class SeedData
{
    private const string DemoPassword = "Password123";

    public static async Task SeedAsync(AppDbContext context)
    {
        if (await context.Users.AnyAsync())
        {
            return;
        }

        var now = DateTime.UtcNow;

        var ana = new User
        {
            Id = Guid.NewGuid(),
            Name = "Ana Ribeiro",
            Email = "ana@email.com",
            PasswordHash = PasswordHasher.Hash(DemoPassword),
            CreatedAt = now
        };

        var bruno = new User
        {
            Id = Guid.NewGuid(),
            Name = "Bruno Tavares",
            Email = "bruno@email.com",
            PasswordHash = PasswordHasher.Hash(DemoPassword),
            CreatedAt = now
        };

        context.Users.AddRange(ana, bruno);

        context.Projects.Add(BuildSummerProject(ana, bruno, now));
        context.Projects.Add(BuildRunningProject(ana, bruno, now));

        await context.SaveChangesAsync();
    }

    /// <summary>Active project, week 7 of 15, both partners checked in every week.</summary>
    private static Project BuildSummerProject(User ana, User bruno, DateTime now)
    {
        var weight = NewGoalField("Peso", GoalDataType.Decimal, "kg", 40, 200);
        AddTarget(weight, ana.Id, ana.Id, 68.9m, 65.0m, GoalDirection.Decrease, GoalTargetStatus.Accepted, true, true);
        AddTarget(weight, bruno.Id, bruno.Id, 92.0m, 87.8m, GoalDirection.Decrease, GoalTargetStatus.Accepted, true, true);

        var workouts = NewGoalField("Adesão aos treinos", GoalDataType.Percent, "%", 0, 100);
        AddTarget(workouts, ana.Id, ana.Id, null, 90m, GoalDirection.Reach, GoalTargetStatus.Accepted, true, true);
        AddTarget(workouts, bruno.Id, bruno.Id, null, 90m, GoalDirection.Reach, GoalTargetStatus.Accepted, true, true);

        var diet = NewGoalField("Adesão à dieta", GoalDataType.Percent, "%", 0, 100);
        AddTarget(diet, ana.Id, ana.Id, null, 85m, GoalDirection.Reach, GoalTargetStatus.Accepted, true, true);
        AddTarget(diet, bruno.Id, bruno.Id, null, 85m, GoalDirection.Reach, GoalTargetStatus.Accepted, true, true);

        var water = NewGoalField("Água por dia", GoalDataType.Decimal, "L", 0, 6);
        AddTarget(water, ana.Id, ana.Id, null, 2.5m, GoalDirection.Reach, GoalTargetStatus.Accepted, true, true);
        AddTarget(water, bruno.Id, bruno.Id, null, 2.5m, GoalDirection.Reach, GoalTargetStatus.Accepted, true, true);

        var project = new Project
        {
            Id = Guid.NewGuid(),
            Name = "Projeto Verão",
            Icon = "🌊",
            CreatorId = ana.Id,
            PartnerId = bruno.Id,
            // Seven full weeks elapsed, so periods 1-7 are history and period 8 is open —
            // the check-in screen stays usable instead of reporting "already submitted".
            // The end date is inclusive and lands inside period 15, so the panel reads
            // "period 8 of 15" rather than opening a one-day sixteenth period.
            StartDate = now.AddDays(-49),
            EndDate = now.AddDays(55),
            Frequency = ProjectFrequency.Weekly,
            Status = ProjectStatus.Active,
            Goals = [weight, workouts, diet, water]
        };

        // Ana 68.9 → 65.0, Bruno 92.0 → 87.8: steady but not suspiciously linear.
        decimal[] anaWeight = [68.9m, 68.2m, 67.4m, 67.0m, 66.3m, 65.6m, 65.0m];
        decimal[] brunoWeight = [92.0m, 91.1m, 90.4m, 90.0m, 89.2m, 88.5m, 87.8m];
        decimal[] anaWorkouts = [80m, 100m, 90m, 100m, 80m, 100m, 100m];
        decimal[] brunoWorkouts = [60m, 80m, 90m, 80m, 100m, 90m, 90m];
        decimal[] anaDiet = [70m, 85m, 90m, 75m, 85m, 95m, 90m];
        decimal[] brunoDiet = [65m, 70m, 85m, 90m, 80m, 85m, 95m];
        decimal[] anaWater = [1.8m, 2.2m, 2.4m, 2.0m, 2.6m, 2.8m, 2.5m];
        decimal[] brunoWater = [1.5m, 1.9m, 2.1m, 2.3m, 2.0m, 2.6m, 2.7m];

        Feeling[] anaMood =
        [
            Feeling.Neutral, Feeling.Happy, Feeling.Happy, Feeling.Sad,
            Feeling.Happy, Feeling.VeryHappy, Feeling.VeryHappy
        ];
        Feeling[] brunoMood =
        [
            Feeling.Sad, Feeling.Neutral, Feeling.Happy, Feeling.Happy,
            Feeling.Neutral, Feeling.Happy, Feeling.VeryHappy
        ];

        for (var week = 0; week < 7; week++)
        {
            // Period numbers are 1-based; week 0 in these arrays is period 1. Anchoring
            // to "now" rather than to StartDate keeps every submission in the past.
            var period = week + 1;
            var submittedAt = now.AddDays(-((6 - week) * 7) - 2);

            project.CheckIns.Add(NewCheckIn(project, ana, period, submittedAt, anaMood[week],
            [
                (weight, anaWeight[week]),
                (workouts, anaWorkouts[week]),
                (diet, anaDiet[week]),
                (water, anaWater[week])
            ]));

            project.CheckIns.Add(NewCheckIn(project, bruno, period, submittedAt, brunoMood[week],
            [
                (weight, brunoWeight[week]),
                (workouts, brunoWorkouts[week]),
                (diet, brunoDiet[week]),
                (water, brunoWater[week])
            ]));
        }

        return project;
    }

    /// <summary>Still pending: exercises the "em negociação" state with no history.</summary>
    private static Project BuildRunningProject(User ana, User bruno, DateTime now) => new()
    {
        Id = Guid.NewGuid(),
        Name = "Rumo aos 10K",
        Icon = "🏃",
        CreatorId = bruno.Id,
        PartnerId = ana.Id,
        StartDate = now.AddDays(7),
        EndDate = now.AddDays(126),
        Frequency = ProjectFrequency.Weekly,
        Status = ProjectStatus.Pending,
        Goals =
        [
            NewGoalField("Distância na semana", GoalDataType.Decimal, "km", 0, 100, 24, bruno.Id, ana.Id),
            NewGoalField("Treinos concluídos", GoalDataType.Integer, "treinos", 0, 7, 4, bruno.Id, ana.Id),
            NewGoalField("Pace médio", GoalDataType.Decimal, "min/km", 3, 12, 6, bruno.Id, ana.Id)
        ]
    };

    private static GoalField NewGoalField(
        string label,
        GoalDataType dataType,
        string unit,
        decimal min,
        decimal max,
        decimal? target = null,
        Guid? creatorId = null,
        Guid? partnerId = null)
    {
        var goal = new GoalField
        {
            Id = Guid.NewGuid(),
            Label = label,
            DataType = dataType,
            Unit = unit,
            MinValue = min,
            MaxValue = max
        };

        if (creatorId is not null && target is not null)
            AddTarget(goal, creatorId, creatorId.Value, null, target, GoalDirection.Reach, GoalTargetStatus.PendingAcceptance, true, false);

        if (partnerId is not null && creatorId is not null && target is not null)
            AddTarget(goal, partnerId, creatorId.Value, null, target, GoalDirection.Reach, GoalTargetStatus.PendingAcceptance, true, false);

        return goal;
    }

    private static void AddTarget(
        GoalField goal,
        Guid? userId,
        Guid proposedByUserId,
        decimal? baseline,
        decimal? target,
        GoalDirection direction,
        GoalTargetStatus status,
        bool acceptedByCreator,
        bool acceptedByPartner)
    {
        goal.Targets.Add(new GoalTarget
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Baseline = baseline,
            TargetValue = target,
            Direction = direction,
            Status = status,
            AcceptedByCreator = acceptedByCreator,
            AcceptedByPartner = acceptedByPartner,
            LastProposedByUserId = proposedByUserId,
            LastProposedAt = DateTime.UtcNow
        });
    }

    private static CheckIn NewCheckIn(
        Project project,
        User user,
        int period,
        DateTime submittedAt,
        Feeling feeling,
        (GoalField Goal, decimal Value)[] metrics) => new()
        {
            Id = Guid.NewGuid(),
            ProjectId = project.Id,
            UserId = user.Id,
            PeriodNumber = period,
            SubmittedAt = submittedAt,
            Feeling = feeling,
            Metrics = [.. metrics.Select(m => new CheckInMetric
            {
                Id = Guid.NewGuid(),
                GoalFieldId = m.Goal.Id,
                Value = m.Value
            })]
        };
}
