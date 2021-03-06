﻿using System;
using System.Collections.Generic;
using System.Text;

namespace GhostLineAPI.Attributes
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, Inherited = false, AllowMultiple = true)]
    public class GhostReadAttribute : GhostAttribute
    {
        public GhostReadAttribute()
            : base()
        {
            canRead = true;
            canWrite = false;
        }

        public GhostReadAttribute(string version, bool disableVersioning, String overrideName)
            : base(version, disableVersioning, overrideName)
        {
            canRead = true;
            canWrite = false;
        }
    }
}
