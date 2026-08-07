# ADR 0007: Per-participant goal targets

## Status

Accepted

## Context

Each `GoalField` historically stored a single `TargetValue` shared between the creator and the partner. This made it impossible for one participant to aim for a different outcome — e.g. one person losing 10 kg while the other maintains a different weight — even though the goal definition (label, data type, unit, validation bounds) is the same for both.

Spec S7.2 requires per-participant target values, optional baselines, and an explicit direction so the dashboard can compare relative progress instead of absolute values.

## Decision

Split the concept:

- `GoalField` keeps the shared definition: label, data type, unit, min/max bounds.
- A new `GoalTarget` entity stores the per-user values: `Baseline`, `TargetValue`, `Direction`, and a nullable `UserId` for the suggested partner target before the invitation is accepted.
- `GoalDirection` is an explicit enum (`Decrease`, `Increase`, `Reach`, `Maintain`) instead of being inferred from baseline and target.
- `GoalProgress.Percent` is a pure function in `Core.Common` that computes 0..100 progress for any combination of baseline, current, target, and direction, with `null` returned only when the target or a divisor is zero/missing.

This is a breaking API change: the project response no longer returns `goal.targetValue`; it returns `goal.targets[]`.

## Consequences

- Project creation now requires a `creatorTarget` and an optional `suggestedPartnerTarget` per goal.
- `AcceptInvitationAsync` assigns the pre-created `GoalTarget` with `UserId == null` to the accepted partner.
- Dashboard, project detail, check-in editor, and invitation pages all read the logged-in user's target and may show the partner's target as reference.
- Change requests for goals now only modify the shared definition; target negotiation is deferred to a future spec.
- A migration `AddGoalTargets` back-fills the existing `goal_fields.target_value` into one `GoalTarget` for the creator and, when a partner exists, one for the partner.
