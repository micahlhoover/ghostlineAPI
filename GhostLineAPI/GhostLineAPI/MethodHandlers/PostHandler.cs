using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace GhostLineAPI.MethodHandlers
{
    class PostHandler : MethodHandler
    {
        public override void Handle(ref HttpListenerResponse response)
        {
            // set the value
            Type serviceItemType = ServiceObj.Type;

            var thisObj = JsonConvert.DeserializeObject(Payload, serviceItemType);
            bool listWasSent = IsList(thisObj);
            bool currentlyList = IsList(serviceItemType);

            if (ServiceObj.CanWrite)
            {
                if (listWasSent)
                {
                    Utilities.SetOrOverwriteValue(ServiceObj, thisObj, ParentObj);
                }
                else
                {
                    // list was not sent
                    if (currentlyList)
                    {
                        // they didn't send a list, but the reflected element is a list ... append it
                        var innerType = ServiceObj.Object.GetType().GetGenericArguments()[0];
                        var enumerables = (IEnumerable<object>)ServiceObj.Object;
                        var results = new List<object>();
                        results.AddRange(enumerables);
                        results.Add(thisObj);

                        Type targetType = typeof(List<>).MakeGenericType(innerType);
                        var outputList = (IList)Activator.CreateInstance(targetType);

                        foreach (var result in results)
                        {
                            outputList.Add(result);
                        }

                        Utilities.SetOrOverwriteValue(ServiceObj, outputList, ParentObj);
                    }
                    else
                    {
                        Utilities.SetOrOverwriteValue(ServiceObj, thisObj, ParentObj);
                    }
                }
                ResponseString = "OK";
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
