using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace GhostLineAPI.MethodHandlers
{
    class GetHandler : MethodHandler
    {
        public override void Handle(ref HttpListenerResponse response)
        {
            //Response = new HttpListenerResponse();
            if (ServiceObj.CanRead)
            {
                if (FilterKeys == null || FilterKeys.AllKeys.Length == 0)
                {
                    ResponseString = JsonConvert.SerializeObject(ServiceObj.Object);
                    ResponseCode = (int)HttpStatusCode.OK;
                }
                else
                {
                    var innerType = ServiceObj.Object.GetType().GetGenericArguments()[0];
                    var enumerables = (IEnumerable<object>)ServiceObj.Object;
                    var results = Utilities.GetMatchingItems(FilterKeys, enumerables, innerType);

                    ResponseString = JsonConvert.SerializeObject(results);
                    ResponseCode = (int)HttpStatusCode.OK;
                }
            }
            else
            {
                ResponseString = "This is not read enabled at this time. Add [GhostRead] attribute.";
                response.StatusCode = (int)HttpStatusCode.Unauthorized;
            }
        }
    }
}
