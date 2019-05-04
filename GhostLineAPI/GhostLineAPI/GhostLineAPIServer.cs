using GhostLineAPI.Attributes;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;

namespace GhostLineAPI
{
    public class GhostLineAPIServer
    {
        private List<Assembly> _monitoredAssemblies;
        private List<ServableItem> _servableItems;
        private object _parentObj;

        public object ParentObj {
            get
            { return _parentObj; }
            set
            {
                _parentObj = value;
            }
        }
        public String Address { get; set; }     // e.g. "127.0.0.1"
        public int Port { get; set; }

        public Func<HttpListenerRequest, bool> Authenticator { get; set; }
        public Action Before { get; set; }
        public Action After { get; set; }

        public GhostLineAPIServer()
        {
            // Set up
            _monitoredAssemblies = new List<Assembly>();
            _servableItems = new List<ServableItem>();
            Authenticator = null;

            if (String.IsNullOrEmpty(Address))
            {
                Address = "127.0.0.1";
            }
        }

        public bool IsList(object o)
        {
            return o is IList &&
               o.GetType().IsGenericType &&
               o.GetType().GetGenericTypeDefinition().IsAssignableFrom(typeof(List<>));
        }

        public void SetupAndStartServer()
        {
            ReflectServableItems();

            if (!HttpListener.IsSupported)
            {
                Console.WriteLine("Windows XP SP2 or Server 2003 is required to use the HttpListener class.");
                return;
            }
            HttpListener listener = new HttpListener();
            // Add the prefixes.
            var prefixes = _servableItems.Select(si =>
           {
               if (!String.IsNullOrEmpty(si.PropertyName))
               {
                   return $"http://{Address}:{Port}/{si.PropertyName}/";
               }
               else
               {
                   return $"http://{Address}:{Port}/{si.FieldName}/";
               }
           }).ToList();
            foreach (string s in prefixes)
            {
                listener.Prefixes.Add(s);
            }

            bool done = false;
            while(done == false)
            {
                listener.Start();
                Console.WriteLine("Listening...");
                // Note: The GetContext method blocks while waiting for a request. 

                HttpListenerContext context = listener.GetContext();
                HttpListenerRequest request = context.Request;

                string payload = string.Empty;
                using (var reader = new StreamReader(request.InputStream, Encoding.UTF8))
                {
                    payload = reader.ReadToEnd();
                    // Do something with the value
                }

                if (payload.Equals("DONE"))
                {
                    done = true;
                }

                string responseString = "No response. Unable to pair request to existing data.";

                // Obtain a response object.
                HttpListenerResponse response = context.Response;

                // TODO: add filters

                if (Authenticator == null || Authenticator(request))
                {
                    if (Before != null)
                        Before();

                    // get the value
                    var name = request.Url;     // {http://127.0.0.1:19001/UntrainedElkDogs}
                    var tokens = request.Url.ToString().Split('/');
                    var lastToken = tokens[tokens.Length - 1];  // the last one should correspond to the property or object name
                    if (lastToken.Contains('?'))
                    {
                        lastToken = lastToken.Split("?")[0];
                    }
                    var filterKeys = request.QueryString;
                    var serviceObj = _servableItems.Where(si => si.AccessName.Equals(lastToken, StringComparison.InvariantCultureIgnoreCase)).First();

                    if (request.HttpMethod.ToLower() == "get")
                    {
                        if (serviceObj.CanRead)
                        {
                            if (filterKeys.AllKeys.Count() == 0)
                            {
                                responseString = JsonConvert.SerializeObject(serviceObj.Object);
                                response.StatusCode = (int)HttpStatusCode.OK;
                            }
                            else
                            {
                                var innerType = serviceObj.Object.GetType().GetGenericArguments()[0];
                                var enumerables = (IEnumerable<object>)serviceObj.Object;
                                var results = new List<object>();
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
                                        results.Add(item);
                                    }
                                }
                                responseString = JsonConvert.SerializeObject(results);
                                response.StatusCode = (int)HttpStatusCode.OK;
                            }
                        }
                        else
                        {
                            responseString = "This is not read enabled at this time. Add [GhostRead] attribute.";
                            response.StatusCode = (int)HttpStatusCode.Unauthorized;
                        }
                    }
                    else if (request.HttpMethod.ToLower() == "post")    // TODO: consider adding PUT for new stuff
                    {
                        // set the value

                        Type serviceItemType = serviceObj.Type;

                        var thisObj = JsonConvert.DeserializeObject(payload, serviceItemType);
                        bool isList = IsList(thisObj);

                        if (serviceObj.CanWrite)
                        {
                            //if (filterKeys.AllKeys.Count() == 0)
                            if (isList)
                            {
                                if (serviceObj.PropertyInfo != null)    // property
                                {
                                    serviceObj.PropertyInfo.SetValue(serviceObj.Object, thisObj);
                                    // OK ... it really is set now ... but it won't show up in the next GET unless we update the reference
                                    serviceObj.Object = serviceObj.PropertyInfo.GetValue(_parentObj); // this ref now points to where it is in the parent
                                }
                                else    // field
                                {
                                    serviceObj.FieldInfo.SetValue(serviceObj.Object, thisObj);
                                    // OK ... it really is set now ... but it won't show up in the next GET unless we update the reference
                                    serviceObj.Object = serviceObj.FieldInfo.GetValue(_parentObj); // this ref now points to where it is in the parent
                                }
                            }
                            else
                            {
                                // not a list ... must be sending one item ... should be new since its a POST
                                var innerType = serviceObj.Object.GetType().GetGenericArguments()[0];
                                var enumerables = (IEnumerable<object>)serviceObj.Object;
                                var results = new List<object>();
                                results.AddRange(enumerables);
                                results.Add(thisObj);

                                //var target = results.ConvertAll(x => new TargetType { SomeValue = x.SomeValue });
                                //var target = re

                                Type targetType = typeof(List<>).MakeGenericType(innerType);
                                var outputList = (IList)Activator.CreateInstance(targetType);

                                foreach(var result in results)
                                {
                                    outputList.Add(result);
                                }

                                if (serviceObj.PropertyInfo != null)    // property
                                {
                                    serviceObj.PropertyInfo.SetValue(serviceObj.Object, outputList);
                                    // OK ... it really is set now ... but it won't show up in the next GET unless we update the reference
                                    serviceObj.Object = serviceObj.PropertyInfo.GetValue(_parentObj);
                                }
                                else    // field
                                {
                                    serviceObj.FieldInfo.SetValue(serviceObj.Object, outputList);
                                    // OK ... it really is set now ... but it won't show up in the next GET unless we update the reference
                                    serviceObj.Object = serviceObj.FieldInfo.GetValue(_parentObj);
                                }
                            }
                            responseString = "OK";
                            response.StatusCode = (int)HttpStatusCode.Created;
                        }
                        else
                        {
                            responseString = "This is not write enabled at this time. Add [GhostWrite] attribute.";
                            response.StatusCode = (int)HttpStatusCode.Unauthorized;
                        }
                    }
                    else if (request.HttpMethod.ToLower() == "put")
                    {
                        // set the value    (PUT supports NEW and UPDATE ... use query param for update)
                        Type serviceItemType = serviceObj.Type;

                        var thisObj = JsonConvert.DeserializeObject(payload, serviceItemType);
                        bool isList = IsList(thisObj);

                        if (serviceObj.CanWrite)
                        {
                            //if (filterKeys.AllKeys.Count() == 0)
                            if (!isList)
                            {
                                if (serviceObj.PropertyInfo != null)    // property
                                {
                                    serviceObj.PropertyInfo.SetValue(serviceObj.Object, thisObj);
                                    // OK ... it really is set now ... but it won't show up in the next GET unless we update the reference
                                    serviceObj.Object = serviceObj.PropertyInfo.GetValue(_parentObj); // this ref now points to where it is in the parent
                                }
                                else    // field
                                {
                                    serviceObj.FieldInfo.SetValue(serviceObj.Object, thisObj);
                                    // OK ... it really is set now ... but it won't show up in the next GET unless we update the reference
                                    serviceObj.Object = serviceObj.FieldInfo.GetValue(_parentObj); // this ref now points to where it is in the parent
                                }
                            }
                            else
                            {
                                // not a list ... must be sending one item ... COULD be new since its a POST
                                // if query parameter ... it must be an update TODO: add validation to make sure
                                var innerType = serviceObj.Object.GetType().GetGenericArguments()[0];
                                var enumerables = (IEnumerable<object>)serviceObj.Object;
                                var results = new List<object>();
                                int leftOutCounter = 0; // cancel the whole thing if more than one is left out according to query

                                if (filterKeys.AllKeys.Count() == 0)
                                {
                                    results.AddRange(enumerables);
                                    results.Add(thisObj);
                                } else
                                {
                                    // there's a query parameter, so treat this as an update for one item
                                    // only add if it is NOT matched ... and then add the inbound item separately
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
                                        if (!allMatched)
                                        {
                                            results.Add(item);
                                        } else
                                        {
                                            leftOutCounter++;
                                        }
                                    }
                                    // add the new item
                                    results.Add(thisObj);
                                }

                                Type targetType = typeof(List<>).MakeGenericType(innerType);
                                var outputList = (IList)Activator.CreateInstance(targetType);

                                foreach (var result in results)
                                {
                                    outputList.Add(result);
                                }

                                if (leftOutCounter <= 1)
                                {
                                    if (serviceObj.PropertyInfo != null)    // property
                                    {
                                        serviceObj.PropertyInfo.SetValue(serviceObj.Object, outputList);
                                        // OK ... it really is set now ... but it won't show up in the next GET unless we update the reference
                                        serviceObj.Object = serviceObj.PropertyInfo.GetValue(_parentObj);
                                    }
                                    else    // field
                                    {
                                        serviceObj.FieldInfo.SetValue(serviceObj.Object, outputList);
                                        // OK ... it really is set now ... but it won't show up in the next GET unless we update the reference
                                        serviceObj.Object = serviceObj.FieldInfo.GetValue(_parentObj);
                                    }

                                    responseString = "OK";
                                    response.StatusCode = (int)HttpStatusCode.Created;
                                } else
                                {
                                    responseString = "Looks like you sent a query parameter that matched multiple items";
                                    response.StatusCode = (int)HttpStatusCode.BadRequest;
                                }
                            }
                            response.StatusCode = (int)HttpStatusCode.Created;
                        }
                        else
                        {
                            responseString = "This is not write enabled at this time. Add [GhostWrite] attribute.";
                            response.StatusCode = (int)HttpStatusCode.Unauthorized;
                        }
                    }
                    else if (request.HttpMethod.ToLower() == "delete")
                    {
                        if (serviceObj.CanWrite)
                        {
                            if (filterKeys.AllKeys.Count() == 0)
                            {
                                // just obliterate it with the default
                                //default();
                                var thisObj = (serviceObj.Type.IsValueType? Activator.CreateInstance(serviceObj.Type) : null);
                                //var thisObj = default(serviceObj.);
                                if (serviceObj.PropertyInfo != null)    // property
                                {
                                    serviceObj.PropertyInfo.SetValue(serviceObj.Object, thisObj);
                                    // OK ... it really is set now ... but it won't show up in the next GET unless we update the reference
                                    serviceObj.Object = serviceObj.PropertyInfo.GetValue(_parentObj); // this ref now points to where it is in the parent
                                }
                                else    // field
                                {
                                    serviceObj.FieldInfo.SetValue(serviceObj.Object, thisObj);
                                    // OK ... it really is set now ... but it won't show up in the next GET unless we update the reference
                                    serviceObj.Object = serviceObj.FieldInfo.GetValue(_parentObj); // this ref now points to where it is in the parent
                                }
                            } else
                            {
                                // delete with query parameters
                                Type serviceItemType = serviceObj.Type;

                                //var thisObj = JsonConvert.DeserializeObject(payload, serviceItemType);
                                bool isList = IsList(serviceObj.Type);

                                // selective delete
                                var innerType = serviceObj.Object.GetType().GetGenericArguments()[0];
                                var enumerables = (IEnumerable<object>)serviceObj.Object;
                                var existingItems = new List<object>();
                                existingItems.AddRange(enumerables);

                                Type targetType = typeof(List<>).MakeGenericType(innerType);
                                var outputList = (IList)Activator.CreateInstance(targetType);

                                foreach (var existingItem in existingItems)
                                {
                                    // only leave it out it if it meets the query param criteria!

                                    bool allMatched = true;
                                    foreach (var attributeName in filterKeys.AllKeys)
                                    {
                                        var candidateValObject = existingItem.GetType().GetProperty(attributeName).GetValue(existingItem, null);
                                        var filterKeyVal = filterKeys[attributeName];
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
                            }
                            responseString = "Deleted";
                            response.StatusCode = (int)HttpStatusCode.Accepted;
                        }
                        else
                        {
                            responseString = "Cannot delete since not write enabled. Add [GhostWrite] attribute.";
                            response.StatusCode = (int)HttpStatusCode.Unauthorized;
                        }
                    }
                    else
                    {
                        responseString = "This http method is not enabled at this time.";
                        response.StatusCode = (int)HttpStatusCode.Unauthorized;
                    }
                } else
                {
                    response.StatusCode = (int)HttpStatusCode.Forbidden;
                }

                if (After != null)
                    After();

                // Construct a response.

                byte[] buffer = System.Text.Encoding.UTF8.GetBytes(responseString);
                // Get a response stream and write the response to it.
                response.ContentLength64 = buffer.Length;
                System.IO.Stream output = response.OutputStream;
                output.Write(buffer, 0, buffer.Length);
                // You must close the output stream.
                output.Close();
            }

            listener.Stop();
        }

        private List<ServableItem> ReflectServableItems()
        {
            // Find all GhostLine API attributes in other assemblies
            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();

            // check out the assemblies
            foreach (var assembly in assemblies)
            {
                Debug.WriteLine("Assembly found: " + assembly.FullName);
                //Console.WriteLine("Assembly found: " + assembly.FullName);
                if (!assembly.FullName.StartsWith("System.") 
                    && !assembly.FullName.Equals("GhostLineAPI")
                    && !assembly.FullName.StartsWith("Microsoft"))
                {
                    _monitoredAssemblies.Add(assembly);
                }
            }

            // look for the fields and properties
            foreach (var assembly in _monitoredAssemblies)
            {
                foreach (var typeObj in assembly.GetTypes())
                {
//                    PropertyInfo[] props = typeObj.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);   // this clips out everything .. ?
                    PropertyInfo[] props = typeObj.GetProperties();
                    foreach (PropertyInfo prop in props)
                    {
                        object[] attrs = prop.GetCustomAttributes(true);
                        foreach (object attr in attrs)
                        {
                            GhostReadAttribute gra = attr as GhostReadAttribute;
                            GhostWriteAttribute gwa = attr as GhostWriteAttribute;
                            if (gra != null || gwa != null)
                            {
                                var servableItem = new ServableItem
                                {
                                    AssemblyFullName = assembly.FullName,
                                    Type = prop.PropertyType,
                                    PropertyName = prop.Name
                                };
                                servableItem.GenerateId();

                                // check to see if this is already a servable item
                                //if (_servableItems.ContainsKey(servableItem.Id))
                                var checkItem = _servableItems.FirstOrDefault(si => si.Id == servableItem.Id);
                                //if (_servableItems.Any( si => si.Id == servableItem.Id))
                                if (checkItem != null)
                                {
                                    //servableItem = _servableItems[servableItem.Id];
                                    //servableItem = _servableItems.First(si => si.Id == servableItem.Id);
                                    servableItem = checkItem;
                                }
                                else
                                {
                                    // flesh out the rest
                                    if (_parentObj == null)
                                    {
                                        throw new ArgumentException("Must pass instance object (usually \"this\") as ParentObj when annotating properties due to .NET reflection access restrictions.");
                                    }
                                    servableItem.Object = prop.GetValue(_parentObj);
                                    servableItem.PropertyInfo = prop;
                                }

                                if (gra != null)
                                {
                                    servableItem.CanRead = true;
                                }
                                if (gwa != null)
                                {
                                    servableItem.CanWrite = true;
                                }
                                _servableItems.Add(servableItem);
                            }
                        }
                    }

                    FieldInfo[] fields = typeObj.GetFields();
                    foreach (FieldInfo field in fields)
                    {
                        object[] attrs = field.GetCustomAttributes(true);
                        foreach (object attr in attrs)
                        {
                            GhostReadAttribute gra = attr as GhostReadAttribute;
                            GhostWriteAttribute gwa = attr as GhostWriteAttribute;
                            if (gra != null || gwa != null)
                            {
                                var servableItem = new ServableItem
                                {
                                    AssemblyFullName = assembly.FullName,
                                    Type = field.FieldType, //.PropertyType,
                                    FieldName = field.Name//prop.Name
                                };
                                servableItem.GenerateId();

                                // check to see if this is already a servable item
                                var checkItem = _servableItems.FirstOrDefault(si => si.Id == servableItem.Id);
                                if (checkItem != null)
                                {
                                    servableItem = checkItem;
                                }
                                else
                                {
                                    // flesh out the rest
                                    if (_parentObj == null)
                                    {
                                        throw new ArgumentException("Must pass instance object (usually \"this\") when annotating properties due to .NET reflection access restrictions.");
                                    }
                                    servableItem.Object = field.GetValue(field.Name);
                                    servableItem.FieldInfo = field;
                                }

                                if (gra != null)
                                {
                                    servableItem.CanRead = true;
                                }
                                if (gwa != null)
                                {
                                    servableItem.CanWrite = true;
                                }
                                _servableItems.Add(servableItem);
                            }
                        }
                    }

                    // all properties and fields should now be in _servableItems
                }
            }
            return _servableItems;
        }
    }
}
