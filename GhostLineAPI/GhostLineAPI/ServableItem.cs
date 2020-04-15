using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace GhostLineAPI
{
    public class ServableItem
    {
        public string AssemblyFullName { get; set; }
        public Type Type { get; set; }
        public Object Object { get; set; }
        public PropertyInfo PropertyInfo { get; set; }
        public bool CanRead { get; set; }
        public bool CanWrite { get; set; }
        public String PropertyName { get; set; }
        public String FieldName { get; set; }
        public FieldInfo FieldInfo { get; set; }
        public String AccessName { get; set; }
        public String Version { get; set; }
        public String OverriddenName { get; set; }

        public String Id { get; set; }

        public ServableItem()
        {
            CanRead = false;
            CanWrite = false;
        }

        public void GenerateId()
        {
            if (!String.IsNullOrEmpty(PropertyName))
            {
                AccessName = PropertyName;
                Id = AssemblyFullName + "_" + Type.ToString() + "_prop_" + PropertyName;
            }
            else
            {
                AccessName = FieldName;
                Id = AssemblyFullName + "_" + Type.ToString() + "_field_" + FieldName;
            }
        }
    }
}
