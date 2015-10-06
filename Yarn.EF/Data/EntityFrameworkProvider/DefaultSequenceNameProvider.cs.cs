using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Yarn.Data.EntityFrameworkProvider
{
    public class DefaultSequenceNameProvider : ISequenceNameProvider
    {
        public string GetSequenceName(Type entityType)
        {
            return entityType.Name + "Sequence";
        }
    }
}
