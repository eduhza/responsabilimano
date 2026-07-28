namespace ResponsabiliMano.Web;

/// <summary>
/// Named feature flags (spec R7). Deploy is not release: code ships behind a flag
/// that defaults to off and is flipped on only at Gate 3. Flag state lives in the
/// <c>FeatureManagement</c> section of configuration.
/// </summary>
public static class FeatureFlags
{
    /// <summary>Check-in capture and reminders (Sprint 3). Off until the feature is accepted.</summary>
    public const string CheckIns = "CheckIns";

    /// <summary>Dashboard data API (Sprint 4, spec S4.1). Off until the feature is accepted.</summary>
    public const string Dashboard = "Dashboard";
}
