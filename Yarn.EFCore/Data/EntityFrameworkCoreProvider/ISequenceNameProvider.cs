using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Yarn.Data.EntityFrameworkCoreProvider
{
    public interface ISequenceNameProvider
    {
        string GetSequenceName(Type entityType);
    }
}
