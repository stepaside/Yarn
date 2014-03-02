using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Yarn
{
    public interface IAuditable
    {
        DateTime CreateDate { get; set; }
        string CreatedBy { get; set; }

        DateTime? UpdateDate { get; set; }
        string UpdatedBy { get; set; }
    }
}
