using System;
using System.Collections.Generic;
using System.Collections.Specialized;

namespace GhostLineAPI
{

    public static class Utilities
    {
        public static List<object> GetMatchingItems(NameValueCollection filterKeys, IEnumerable<object> enumerables, Type innerType)
        {
            var returnableItems = new List<object>();

            foreach (var item in enumerables)
            {
                bool allMatched = true;
                foreach (var attributeName in filterKeys.AllKeys)
                {
                    var candidateValObject = item.GetType().GetProperty(attributeName).GetValue(item, null);
                    var filterKeyVal = filterKeys[attributeName];
                    if (!candidateValObject.ToString().Equals(filterKeyVal, StringComparison.InvariantCultureIgnoreCase))
                    {
                        allMatched = false;
                    }
                }
                if (allMatched)
                {
                    returnableItems.Add(item);
                }
            }

            return returnableItems;
        }

        public static void SetOrOverwriteValue(ServableItem serviceObj, object thisObj, object parentObj)
        {
            if (serviceObj.PropertyInfo != null)    // property
            {
                serviceObj.PropertyInfo.SetValue(serviceObj.Object, thisObj);
                // OK ... it really is set now ... but it won't show up in the next GET unless we update the reference
                serviceObj.Object = serviceObj.PropertyInfo.GetValue(parentObj); // this ref now points to where it is in the parent
            }
            else    // field
            {
                serviceObj.FieldInfo.SetValue(serviceObj.Object, thisObj);
                // OK ... it really is set now ... but it won't show up in the next GET unless we update the reference
                serviceObj.Object = serviceObj.FieldInfo.GetValue(parentObj); // this ref now points to where it is in the parent
            }
        }
    }

}
