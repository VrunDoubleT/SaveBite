namespace SaveBite.Backend.Services.Interfaces;

public interface ITrustReconciliationService
{
    /// <summary>
    /// Resynchronize the Trust Level for all users according to the specified Policy Version.
    /// Resolving Race Conditions using the "Latest Configuration Wins" principle:
    /// If the current version in the DB is higher than the provided `policyVersion` -> Fast Abort.
    /// </summary>
    Task ReconcileAsync(long policyVersion, CancellationToken cancellationToken = default);
}