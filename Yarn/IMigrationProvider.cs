using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Yarn
{
    public interface IMigrationProvider
    {
        Stream BuildSchema();
        Stream UpdateSchema();
    }
}
