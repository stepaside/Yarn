using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Yarn
{
    public interface IFullTextRepository
    {
        IFullTextProvider FullText { get; }
    }
}
