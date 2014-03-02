using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using Yarn.Adapters;

namespace Yarn.Extensions
{
    public static class RepositoryExtensions
    {
        public static IRepository WithSoftDelete(this IRepository repository, IPrincipal principal = null)
        {
            return new SoftDeleteRepository(repository, principal ?? WindowsPrincipal.Current);
        }

        public static IRepository WithAudit(this IRepository repository, IPrincipal principal = null)
        {
            return new AuditableRepository(repository, principal ?? WindowsPrincipal.Current);
        }
    }
}
