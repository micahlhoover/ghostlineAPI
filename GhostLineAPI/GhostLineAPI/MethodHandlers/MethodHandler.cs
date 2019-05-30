using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Net;
using System.Text;

namespace GhostLineAPI.MethodHandlers
{
    /// <summary>
    /// Base class handler for all HTTP method handlers
    /// </summary>
    public class MethodHandler
    {
        protected NameValueCollection FilterKeys { get; set; }
        public ServableItem ServiceObj { get; set; }
        public String ResponseString { get; set; }
        protected int ResponseCode { get; set; }
        //protected HttpListenerResponse Response { get; set;}
        
        protected Object ParentObj { get; set; }

        public HttpListenerRequest Request { get; set; }
        public NameValueCollection NameValueCollection { get; set; }
        public String Payload { get; set; }
        //public ServableItem ServableItem { get; set; }

        virtual public void Handle(ref HttpListenerResponse response)
        {
        }

        public bool IsList(object o)
        {
            return o is IList &&
               o.GetType().IsGenericType &&
               o.GetType().GetGenericTypeDefinition().IsAssignableFrom(typeof(List<>));
        }
    }
}
