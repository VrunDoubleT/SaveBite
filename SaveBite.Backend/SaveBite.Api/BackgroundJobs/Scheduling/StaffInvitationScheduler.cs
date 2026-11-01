using Hangfire;
using SaveBite.Backend.BackgroundJobs.Interfaces;
using SaveBite.Backend.BackgroundJobs.Jobs;

namespace SaveBite.Backend.BackgroundJobs.Scheduling;

public sealed class StaffInvitationScheduler(IBackgroundJobClient jobs) : IStaffInvitationScheduler
{
    public void ScheduleExpiration(Guid invitationId, DateTime expiresAt)
        => jobs.Schedule<StaffInvitationJob>(job => job.ExpireAsync(invitationId, CancellationToken.None),
            new DateTimeOffset(DateTime.SpecifyKind(expiresAt, DateTimeKind.Utc)));

}
