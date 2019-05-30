using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Net;
using System.Text;

namespace GhostLineAPI.MethodHandlers
{
    public static class HandlerGenerator
    {
        public static MethodHandler GetHandler(HttpListenerRequest request, NameValueCollection nameValueCollection, ServableItem servableItem, String payload)
        {
            MethodHandler handler = null;
            switch(request.HttpMethod.ToLower())
            {
                case "get":
                    handler = new GetHandler();
                    break;
                case "post":
                    handler = new PostHandler();
                    break;
                case "put":
                    handler = new PutHandler();
                    break;
                case "delete":
                    handler = new DeleteHandler();
                    break;
                default:                    
                    throw new NotSupportedException("This Http verb Is Not Supported");
            }
            handler.Request = request;
            handler.NameValueCollection = nameValueCollection;
            handler.ServiceObj = servableItem;
            handler.Payload = payload;

            return handler;
        }
    }
}
