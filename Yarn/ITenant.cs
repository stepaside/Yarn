using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Yarn
{
    public interface ITenant
    {
        long TenantId { get; set; }
        long OwnerId { get; set; }
    }
}
