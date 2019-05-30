using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;

namespace GhostLineAPI.MethodHandlers
{
    class PutHandler : MethodHandler
    {
        public override void Handle(ref HttpListenerResponse response)
        {
            // set the value    (PUT supports NEW and UPDATE ... use query param for update)
            Type serviceItemType = ServiceObj.Type;

            var thisObj = JsonConvert.DeserializeObject(Payload, serviceItemType);
            bool listWasSent = IsList(thisObj);
            bool currentlyList = IsList(serviceItemType);

            if (ServiceObj.CanWrite)
            {
                //if (filterKeys.AllKeys.Count() == 0)
                if (!listWasSent)
                {
                    Utilities.SetOrOverwriteValue(ServiceObj, thisObj, ParentObj);
                }
                else
                {
                    // not a list ... 
                    if (currentlyList)
                    {
                        // if query parameter ... it must be an update TODO: add validation to make sure
                        var innerType = ServiceObj.Object.GetType().GetGenericArguments()[0];
                        var enumerables = (IEnumerable<object>)ServiceObj.Object;
                        List<object> results = null;
                        int leftOutCounter = 0; // cancel the whole thing if more than one is left out according to query

                        if (FilterKeys.AllKeys.Length == 0)
                        {
                            results.AddRange(enumerables);
                            results.Add(thisObj);
                        }
                        else
                        {
                            results = Utilities.GetMatchingItems(FilterKeys, enumerables, innerType);
                            leftOutCounter = enumerables.Count() - results.Count();
                        }

                        Type targetType = typeof(List<>).MakeGenericType(innerType);
                        var outputList = (IList)Activator.CreateInstance(targetType);

                        foreach (var result in results)
                        {
                            outputList.Add(result);
                        }

                        if (leftOutCounter <= 1)
                        {
                            Utilities.SetOrOverwriteValue(ServiceObj, outputList, ParentObj);

                            ResponseString = "OK";
                            response.StatusCode = (int)HttpStatusCode.Created;
                        }
                        else
                        {
                            ResponseString = "Looks like you sent a query parameter that matched multiple items";
                            response.StatusCode = (int)HttpStatusCode.BadRequest;
                        }
                    }
                    else
                    {
                        // they sent a non-list and a non-list is there
                        Utilities.SetOrOverwriteValue(ServiceObj, thisObj, ParentObj);
                    }
                }
                response.StatusCode = (int)HttpStatusCode.Created;
            }
            else
            {
                ResponseString = "This is not write enabled at this time. Add [GhostWrite] attribute.";
                response.StatusCode = (int)HttpStatusCode.Unauthorized;
            }
        }
    }
}
