using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;

namespace GhostLineAPI.MethodHandlers
{
    class DeleteHandler : MethodHandler
    {
        public override void Handle(ref HttpListenerResponse response)
        {
            if (ServiceObj.CanWrite)
            {
                if (FilterKeys.AllKeys.Count() == 0)
                {
                    // just obliterate it with the default
                    //default();
                    var thisObj = (ServiceObj.Type.IsValueType ? Activator.CreateInstance(ServiceObj.Type) : null);
                    //var thisObj = default(ServiceObj.);
                    Utilities.SetOrOverwriteValue(ServiceObj, thisObj, ParentObj);
                }
                else
                {
                    // delete with query parameters
                    Type serviceItemType = ServiceObj.Type;

                    //var thisObj = JsonConvert.DeserializeObject(payload, serviceItemType);
                    bool isList = IsList(ServiceObj.Type);

                    // selective delete
                    var innerType = ServiceObj.Object.GetType().GetGenericArguments()[0];
                    var enumerables = (IEnumerable<object>)ServiceObj.Object;
                    var existingItems = new List<object>();
                    existingItems.AddRange(enumerables);

                    Type targetType = typeof(List<>).MakeGenericType(innerType);
                    var outputList = (IList)Activator.CreateInstance(targetType);

                    foreach (var existingItem in existingItems)
                    {
                        // only leave it out it if it meets the query param criteria!
                        bool allMatched = true;
                        foreach (var attributeName in FilterKeys.AllKeys)
                        {
                            var candidateValObject = existingItem.GetType().GetProperty(attributeName).GetValue(existingItem, null);
                            var filterKeyVal = FilterKeys[attributeName];
                            if (!candidateValObject.ToString().Equals(filterKeyVal, StringComparison.InvariantCultureIgnoreCase))
                            {
                                allMatched = false;
                            }
                        }
                        if (!allMatched)
                        {
                            outputList.Add(existingItem);
                        }
                    }
                    Utilities.SetOrOverwriteValue(ServiceObj, outputList, ParentObj);
                }
                ResponseString = "Deleted";
                response.StatusCode = (int)HttpStatusCode.Accepted;
            }
            else
            {
                ResponseString = "Cannot delete since not write enabled. Add [GhostWrite] attribute.";
                response.StatusCode = (int)HttpStatusCode.Unauthorized;
            }
        }
    }
}
