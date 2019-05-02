using System;
using System.Collections.Generic;
using System.Text;

namespace GhostLineAPI.Attributes
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, Inherited = false, AllowMultiple = true)]
    public class GhostWriteAttribute : Attribute
    {
        public GhostWriteAttribute()
        {

        }
    }
}
