using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Yarn
{
    public interface ISoftDelete
    {
        bool IsDeleted { get; set; }
        DateTime? UpdateDate { get; set; }
        DateTimeOffset? UpdateOffset { get; set; }
        string UpdatedBy { get; set; }
    }
}
