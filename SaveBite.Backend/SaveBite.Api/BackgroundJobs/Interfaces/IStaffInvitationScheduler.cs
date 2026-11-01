namespace SaveBite.Backend.BackgroundJobs.Interfaces;

public interface IStaffInvitationScheduler
{
    void ScheduleExpiration(Guid invitationId, DateTime expiresAt);
}
