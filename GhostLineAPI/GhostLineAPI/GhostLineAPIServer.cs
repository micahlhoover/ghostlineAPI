﻿using GhostLineAPI.Attributes;
using GhostLineAPI.MethodHandlers;
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
    public enum LogType
    {
        None = 0,
        Trace = 1,
        Console = 2,
        Debug = 3
    }

    public enum LogLevel
    {
        None = 0,
        Critical = 1,
        Error = 2,
        Info = 3,
        Verbose = 4
    }

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
        public Func<HttpListenerRequest, ValidationResponse> Validator { get; set; }
        public LogType LogType { get; set; }
        public LogLevel LogLevel { get; set; }

        public GhostLineAPIServer()
        {
            // Set up
            _monitoredAssemblies = new List<Assembly>();
            _servableItems = new List<ServableItem>();
            Authenticator = null;

            Address = "127.0.0.1";
            Port = 19001;

            LogType = LogType.Console;
            LogLevel = LogLevel.Error;
        }

        public void SetupAndStartServer()
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();
            ReflectServableItems();
            sw.Stop();
            Log($"Reflection set up: {sw.ElapsedMilliseconds} milliseconds", LogLevel.Verbose);

            if (!HttpListener.IsSupported)
            {
                Log("Windows XP SP2 or Server 2003 is required to use the HttpListener class.", LogLevel.Error);
                return;
            }
            HttpListener listener = new HttpListener();
            // Add the prefixes.
            var prefixes = _servableItems.Select(si =>
           {
               if (!String.IsNullOrEmpty(si.PropertyName))
               {
                   String name = !String.IsNullOrWhiteSpace(si.OverriddenName) ? si.OverriddenName : si.PropertyName;
                   if (String.IsNullOrWhiteSpace(si.Version))
                       return $"http://{Address}:{Port}/{si.PropertyName}/";
                   else
                       return $"http://{Address}:{Port}/{si.Version}/{name}/";
               }
               else
               {
                   String name = !String.IsNullOrWhiteSpace(si.OverriddenName) ? si.OverriddenName : si.FieldName;
                   if (String.IsNullOrWhiteSpace(si.Version))
                       return $"http://{Address}:{Port}/{si.FieldName}/";
                   else
                       return $"http://{Address}:{Port}/{si.Version}/{name}/";
               }
           }).ToList();
            foreach (string s in prefixes)
            {
                Log($"Endpoint: {s}", LogLevel.Info);
                listener.Prefixes.Add(s);
            }

            Log($"Listening on http://{Address}:{Port}", LogLevel.Info);

            bool done = false;
            while (done == false)
            {
                listener.Start();
                HttpListenerContext context = listener.GetContext();
                HttpListenerRequest request = context.Request;
                HttpListenerResponse response = context.Response;

                Log($"Received request: {request.Url}", LogLevel.Info);

                try
                {
                    done = HandleOneRequest(listener, context, request, response);
                } catch(Exception ex)
                {
                    Log("Got exception: " + ex.Message, LogLevel.Error);
                    
                    String responseString = "The request was not valid.";
                    byte[] buffer = System.Text.Encoding.UTF8.GetBytes(responseString);
                    response.StatusCode = (int)HttpStatusCode.BadRequest;
                    response.ContentLength64 = buffer.Length;
                    System.IO.Stream output = response.OutputStream;
                    output.Write(buffer, 0, buffer.Length);
                    output.Close();
                    Log($"Wrote: {responseString} with Http Code: {response.StatusCode}", LogLevel.Verbose);
                }
            }

            listener.Stop();
        }

        private bool HandleOneRequest(HttpListener listener, HttpListenerContext context, HttpListenerRequest request, HttpListenerResponse response)
        {
            bool done = false;
            // Note: The GetContext method blocks while waiting for a request. 

            Stopwatch sw2 = new Stopwatch();
            sw2.Start();

            string payload = string.Empty;
            using (var reader = new StreamReader(request.InputStream, Encoding.UTF8))
            {
                payload = reader.ReadToEnd();
            }

            if (payload.Equals("DONE"))
            {
                done = true;
            }

            string responseString = "No response. Unable to pair request to existing data.";

            if (Authenticator == null || Authenticator(request))
            {
                if (Before != null)
                    Before();

                if (Validator != null)
                {
                    var validationResponse = Validator(request);
                    if (!validationResponse.Success ||
                        validationResponse.Messages.Any(m => m.ValidationType == ValidationMessageType.Failure))
                    {
                        response.StatusCode = (int)HttpStatusCode.BadRequest;
                        StringBuilder responseInfo = new StringBuilder();
                        foreach (var message in validationResponse.Messages)
                        {
                            responseInfo.Append($"{Enum.GetName(typeof(ValidationMessageType), message.ValidationType)} {message.Message} ");
                        }
                        responseString = responseInfo.ToString();
                    }
                }

                if (response.StatusCode != (int)HttpStatusCode.BadRequest)
                {
                    // get the value
                    var name = request.Url.ToString();     // {http://127.0.0.1:19001/UntrainedElkDogs} or {http://127.0.0.1:19001/v1/UntrainedElkDogs}
                    name = name.TrimEnd('/');
                    var tokens = name.Split('/');

                    // TODO: the version number isn't always a version number
                    var versionNumber = tokens[tokens.Length - 2];
                    var objectName = tokens[tokens.Length - 1];  // the last one should correspond to the property or object name
                    if (objectName.Contains('?'))
                    {
                        objectName = objectName.Split("?")[0];
                    }

                    String fail = String.Empty;
                    if (!_servableItems.Any(si => si.AccessName.Equals(objectName)))
                    {
                        // try checking the next to last one (i.e. if there is a version number)
                        objectName = tokens[tokens.Length - 2];
                        if (!_servableItems.Any(si => si.AccessName.Equals(objectName)))
                        {
                            versionNumber = "v1";   // default
                        } else
                        {
                            // no recognizable object name from the URL in servable itmes
                            fail = "No recognizable object name from the URL in servable itmes";

                        }
                    }

                    if (String.IsNullOrEmpty(fail))
                    {
                        var filterKeys = request.QueryString;
                        // get original name matches or overridden name matches
                        var check = _servableItems.Where(si => si.AccessName.Equals(objectName, StringComparison.InvariantCultureIgnoreCase));

                        var directmatches = _servableItems.Where(si => si.AccessName.Equals(objectName, StringComparison.InvariantCultureIgnoreCase) ).ToList();
                        var overrideMatches = _servableItems.Where(si =>
                               (!String.IsNullOrWhiteSpace(si.OverriddenName) && si.OverriddenName.Equals(objectName, StringComparison.InvariantCultureIgnoreCase))).ToList();
                        var serviceObjs = directmatches.Union(overrideMatches).ToList();

                        ServableItem serviceObj = null;
                        if (serviceObjs.Count == 1)
                        {
                            // no ambiguity ... go with it
                            serviceObj = serviceObjs.First();
                        } else
                        {
                            // use version number
                            serviceObj = serviceObjs.First(so => so.Version.Equals(versionNumber, StringComparison.InvariantCultureIgnoreCase));
                        }

                        var methodHandler = HandlerGenerator.GetHandler(request, filterKeys, serviceObj, payload);
                        methodHandler.Handle(ref response);
                        responseString = methodHandler.ResponseString;
                    } else
                    {
                        responseString = fail;
                        response.StatusCode = (int)HttpStatusCode.BadRequest;
                    }
                }
            }
            else
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
            Log($"Wrote: {responseString} with Http Code: {response.StatusCode}", LogLevel.Verbose);

            sw2.Stop();
            Log($"Response process time: {sw2.ElapsedMilliseconds} milliseconds", LogLevel.Info);

            return done;
        }

        public void Refresh()
        {
            ReflectServableItems();
        }

        private List<ServableItem> ReflectServableItems()
        {
            // Find all GhostLine API attributes in other assemblies
            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();

            // check out the assemblies
            foreach (var assembly in assemblies)
            {
                Log("Assembly found: " + assembly.FullName, LogLevel.Verbose);
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
                    PropertyInfo[] props = typeObj.GetProperties();
                    foreach (PropertyInfo prop in props)
                    {
                        object[] attrs = prop.GetCustomAttributes(true);
                        foreach (object attr in attrs)
                        {
                            GhostReadAttribute gra = attr as GhostReadAttribute;
                            GhostWriteAttribute gwa = attr as GhostWriteAttribute;
                            GhostReadWriteAttribute grwa = attr as GhostReadWriteAttribute;
                            GhostAttribute ga = attr as GhostAttribute;
                            if (gra != null || gwa != null || grwa != null)
                            {
                                var servableItem = new ServableItem
                                {
                                    AssemblyFullName = assembly.FullName,
                                    Type = prop.PropertyType,
                                    PropertyName = prop.Name
                                };
                                servableItem.GenerateId();


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
                                        String message = "Must pass instance object (usually \"this\") when annotating properties due to .NET reflection access restrictions.";
                                        Log(message, LogLevel.Critical);
                                        throw new ArgumentException(message);
                                    }
                                    servableItem.Object = prop.GetValue(_parentObj);
                                    servableItem.PropertyInfo = prop;
                                }

                                if (gra != null || grwa != null)
                                {
                                    servableItem.CanRead = true;
                                    servableItem.Version = ga.Version;
                                    servableItem.OverriddenName = ga.OverrideName;
                                }
                                if (gwa != null || grwa != null)
                                {
                                    servableItem.CanWrite = true;
                                    servableItem.Version = ga.Version;
                                    servableItem.OverriddenName = ga.OverrideName;
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
                            GhostReadWriteAttribute grwa = attr as GhostReadWriteAttribute;
                            GhostAttribute ga = attr as GhostAttribute;
                            if (gra != null || gwa != null || grwa != null)
                            {
                                var servableItem = new ServableItem
                                {
                                    AssemblyFullName = assembly.FullName,
                                    Type = field.FieldType,
                                    FieldName = field.Name,
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
                                        String message = "Must pass instance object (usually \"this\") when annotating properties due to .NET reflection access restrictions.";
                                        Log(message, LogLevel.Critical);
                                        throw new ArgumentException(message);
                                    }
                                    servableItem.Object = field.GetValue(field.Name);
                                    servableItem.FieldInfo = field;
                                }

                                if (gra != null || grwa != null)
                                {
                                    servableItem.CanRead = true;
                                    servableItem.Version = ga.Version;
                                    servableItem.OverriddenName = ga.OverrideName;
                                }
                                if (gwa != null || grwa != null)
                                {
                                    servableItem.CanWrite = true;
                                    servableItem.Version = ga.Version;
                                    servableItem.OverriddenName = ga.OverrideName;
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

        private void Log(String message, LogLevel logLevel = LogLevel.Info)
        {
            if (logLevel > LogLevel)
                return;

            String timedMessage = (logLevel < LogLevel.Info) ? message : $"{DateTime.Now.ToString()}: {message}";
            if (LogType == LogType.Console)
            {
                Console.WriteLine(message);
            } else if (LogType == LogType.Trace)
            {
                Trace.WriteLine(message);
            }
            else if (LogType == LogType.Debug)
            {
                Debug.WriteLine(message);
            }
        }
    }
}
