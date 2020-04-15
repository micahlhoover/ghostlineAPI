using System;
using System.Collections.Generic;
using System.Text;

namespace GhostLineAPI.Attributes
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, Inherited = false, AllowMultiple = true)]
    public class GhostWriteAttribute : GhostAttribute
    {
        public GhostWriteAttribute()
            : base()
        {
            canRead = false;
            canWrite = true;
        }

        public GhostWriteAttribute(string version, bool disableVersioning, String overrideName)
            : base(version, disableVersioning, overrideName)
        {
            canRead = false;
            canWrite = true;
        }
    }
}
