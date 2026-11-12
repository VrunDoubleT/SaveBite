using SaveBite.Backend.Exceptions;
using SaveBite.Backend.Models.Entities;
namespace SaveBite.Backend.Helpers;

public static class PenaltyScheduleRules
{
    // Half-open intervals: a shared end/start is contiguous, not overlapping.
    public static void Validate(IEnumerable<RecoveryRequirementRule> source, DateTime now)
    {
        var all = source.ToList();
        foreach (var group in all.GroupBy(r => r.MinimumOccurrence))
        {
            var ordered = group.OrderBy(r => r.EffectiveFrom).ToList();
            for (var i = 1; i < ordered.Count; i++)
                if (ordered[i - 1].EffectiveTo == null || ordered[i - 1].EffectiveTo > ordered[i].EffectiveFrom)
                    throw AppException.Conflict($"Schedules for no-show #{group.Key} overlap. Choose a period outside the existing schedule.");
        }
        var active = all.Where(r => r.IsActive && (r.EffectiveTo == null || r.EffectiveTo > now)).ToList();
        foreach (var higher in active.Where(r => r.MinimumOccurrence > 1))
        {
            for (var occurrence = 1; occurrence < higher.MinimumOccurrence; occurrence++)
            {
                var cursor = higher.EffectiveFrom > now ? higher.EffectiveFrom : now;
                var end = higher.EffectiveTo ?? DateTime.MaxValue;
                foreach (var lower in active.Where(r => r.MinimumOccurrence == occurrence).OrderBy(r => r.EffectiveFrom))
                {
                    if (lower.EffectiveFrom > cursor) break;
                    var lowerEnd = lower.EffectiveTo ?? DateTime.MaxValue;
                    if (lowerEnd > cursor) cursor = lowerEnd;
                    if (cursor >= end) break;
                }
                if (cursor < end)
                    throw AppException.BadRequest($"No-show #{higher.MinimumOccurrence} requires no-show #{occurrence} coverage throughout its effective period. Missing coverage from {cursor:dd/MM/yyyy HH:mm} UTC. Gaps are allowed only when no higher penalty is effective.");
            }
        }
    }
}
