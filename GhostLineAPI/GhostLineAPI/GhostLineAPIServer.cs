using GhostLineAPI.Attributes;
using Newtonsoft.Json;
using System;
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
                    if (request.HttpMethod.ToLower() == "get")
                    {
                        // get the value
                        var name = request.Url;     // {http://127.0.0.1:19001/UntrainedElkDogs}
                        var tokens = request.Url.ToString().Split('/');
                        var lastToken = tokens[tokens.Length - 1];  // the last one should correspond to the property or object name

                        var serviceObj = _servableItems.Where(si => si.AccessName.Equals(lastToken, StringComparison.InvariantCultureIgnoreCase)).First();

                        if (serviceObj.CanRead)
                        {
                            responseString = JsonConvert.SerializeObject(serviceObj.Object);
                            response.StatusCode = (int)HttpStatusCode.OK;
                        } else
                        {
                            responseString = "This is not read enabled at this time. Add [GhostRead] attribute.";
                            response.StatusCode = (int)HttpStatusCode.Unauthorized;
                        }

                        if (After != null)
                            After();
                    }
                    else if (request.HttpMethod.ToLower() == "post")    // TODO: consider adding PUT for new stuff
                    {
                        // set the value
                        var name = request.Url;     // {http://127.0.0.1:19001/UntrainedElkDogs}
                        var tokens = request.Url.ToString().Split('/');
                        var lastToken = tokens[tokens.Length - 1];  // the last one should correspond to the property or object name

                        var serviceObj = _servableItems.Where(si => si.AccessName.Equals(lastToken, StringComparison.InvariantCultureIgnoreCase)).First();
                        Type serviceItemType = serviceObj.Type;

                        var thisObj = JsonConvert.DeserializeObject(payload, serviceItemType);

                        if (serviceObj.CanWrite)
                        {
                            if (serviceObj.PropertyInfo != null)
                            {
                                serviceObj.PropertyInfo.SetValue(serviceObj.Object, thisObj);
                                // OK ... it really is set now ... but it won't show up in the next GET unless we update the reference
                                serviceObj.Object = serviceObj.PropertyInfo.GetValue(_parentObj);
                            }
                            else
                            {
                                serviceObj.FieldInfo.SetValue(serviceObj.Object, thisObj);
                                // OK ... it really is set now ... but it won't show up in the next GET unless we update the reference
                                serviceObj.Object = serviceObj.FieldInfo.GetValue(_parentObj);
                            }
                            response.StatusCode = (int)HttpStatusCode.OK;
                        } else
                        {
                            responseString = "This is not write enabled at this time. Add [GhostWrite] attribute.";
                            response.StatusCode = (int)HttpStatusCode.Unauthorized;
                        }

                        if (After != null)
                            After();
                    }
                } else
                {
                    response.StatusCode = (int)HttpStatusCode.Forbidden;
                }

                //if (request.


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
