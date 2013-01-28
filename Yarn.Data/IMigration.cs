using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Yarn
{
    public interface IMigration
    {
        void BuildSchema();
        void UpdateSchema();
    }
}
