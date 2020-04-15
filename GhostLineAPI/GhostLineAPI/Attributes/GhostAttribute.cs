using System;
using System.Collections.Generic;
using System.Text;

namespace GhostLineAPI.Attributes
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, Inherited = false, AllowMultiple = true)]
    public abstract class GhostAttribute : Attribute
    {
        protected bool canRead = false;
        protected bool canWrite = false;
        public String Version;
        public bool DisableVersioning;
        // for the case where you have two versions and the property has the same name
        public String OverrideName;

        public GhostAttribute()
        {
            this.Version = String.Empty;
            this.DisableVersioning = false;
            this.OverrideName = String.Empty;
        }

        public GhostAttribute(string version, bool disableVersioning, String overrideName)
        {
            this.Version = version;
            this.DisableVersioning = disableVersioning;
            this.OverrideName = overrideName;
        }
    }
}
