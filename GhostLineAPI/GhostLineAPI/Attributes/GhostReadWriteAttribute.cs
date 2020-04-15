using System;
using System.Collections.Generic;
using System.Text;

namespace GhostLineAPI.Attributes
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, Inherited = false, AllowMultiple = true)]
    public class GhostReadWriteAttribute : GhostAttribute
    {
        public GhostReadWriteAttribute()
            : base()
        {
            canRead = true;
            canWrite = true;
        }

        public GhostReadWriteAttribute(string version, bool disableVersioning, String overrideName)
            : base(version, disableVersioning, overrideName)
        {
            canRead = true;
            canWrite = true;
        }
    }
}
