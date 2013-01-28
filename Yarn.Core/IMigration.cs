using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Exsage.Core
{
    public interface IMigration
    {
        void BuildSchema();
        void UpdateSchema();
    }
}
