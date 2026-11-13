namespace SaveBite.Backend.Messaging.Events;

/// <summary>
/// The event requires re-synchronizing users' Trust Levels in accordance with the new Policy Version.
/// </summary>
public sealed record TrustLevelReconcileEvent(
    long PolicyVersion,
    string TriggerReason,
    DateTime RequestedAt);