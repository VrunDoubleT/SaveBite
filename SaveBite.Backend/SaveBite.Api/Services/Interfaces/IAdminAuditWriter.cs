using SaveBite.Backend.Models.Entities;
namespace SaveBite.Backend.Services.Interfaces;

public interface IAdminAuditWriter
{
    // Track only. The caller saves/commits this entry together with its business changes.
    AuditLog Record(AuditLog entry);
}
