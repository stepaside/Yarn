using System;

namespace Yarn
{
    public interface IAuditable
    {
        DateTime CreateDate { get; set; }
        DateTimeOffset CreateOffset { get; set; }
        string CreatedBy { get; set; }

        DateTime? UpdateDate { get; set; }
        DateTimeOffset? UpdateOffset { get; set; }
        string UpdatedBy { get; set; }

        Guid? AuditId { get; set; }
    }
}
