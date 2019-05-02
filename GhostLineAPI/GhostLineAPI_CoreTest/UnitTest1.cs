using Microsoft.VisualStudio.TestTools.UnitTesting;
using GhostLineAPI;
using System.Net;
using System;
using System.Reflection;
using System.Linq;
using System.Diagnostics;
using ElkDogModel;
using System.Collections.Generic;

namespace GhostLineAPI_CoreTest
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void ReflectionTest()
        {
            // we are using reflection to access a private method so we can test that reflection
            // is successfully pulling in the meta data
            //Class target = new Class();
            //GhostLineAPIServer server = new GhostLineAPIServer();
            //PrivateObject obj = new PrivateObject(target);

            // assembly won't get loaded unless something causes the CLR to load it
            ElkDogManager manager = new ElkDogManager();

            GhostLineAPIServer apiServer = new GhostLineAPIServer
            {
                ParentObj = manager,        // required if using properties
                Address = "127.0.0.1",
                Port = 19001
            };
            apiServer.Authenticator = delegate (HttpListenerRequest req)
            {
                if (String.IsNullOrEmpty(req.Headers["Authorization"]))
                    return false;

                if (req.Headers["Authorization"].Equals("27bc5f2c-bed5-41c7-8a5d-aec966212146"))
                    return true;
                return false;
            };

            // Can't access PrivateObject from .NET Core ?? Boo !!
            // http://anthonygiretti.com/2018/08/26/how-to-unit-test-private-methods-in-net-core-applications-even-if-its-bad/

            Type type = typeof(GhostLineAPIServer);
            MethodInfo method = type.GetMethods(BindingFlags.NonPublic | BindingFlags.Instance)
            .Where(x => x.Name == "ReflectServableItems" && x.IsPrivate)
            .First();

            //Act
            List<ServableItem> servableItems = (List<ServableItem>)method.Invoke(apiServer, null);

            Assert.IsTrue(servableItems.Count(si => si.PropertyName.Equals("TrainedElkDogs")) > 0);
            Assert.IsTrue(servableItems.Count(si => si.PropertyName.Equals("UntrainedElkDogs")) > 0);

            //var retVal = obj.Invoke("PrivateMethod");
        }

        [TestMethod]
        public void ReflectionTest_Negative()
        {
            // these objects exist in the ElkDogManager, but they aren't GhostLineAPI annotated

            // assembly won't get loaded unless something causes the CLR to load it
            ElkDogManager manager = new ElkDogManager();

            GhostLineAPIServer apiServer = new GhostLineAPIServer
            {
                ParentObj = manager,        // required if using properties
                Address = "127.0.0.1",
                Port = 19001
            };
            apiServer.Authenticator = delegate (HttpListenerRequest req)
            {
                if (String.IsNullOrEmpty(req.Headers["Authorization"]))
                    return false;

                if (req.Headers["Authorization"].Equals("27bc5f2c-bed5-41c7-8a5d-aec966212146"))
                    return true;
                return false;
            };

            // Can't access PrivateObject from .NET Core ?? Boo !!
            // http://anthonygiretti.com/2018/08/26/how-to-unit-test-private-methods-in-net-core-applications-even-if-its-bad/

            Type type = typeof(GhostLineAPIServer);
            MethodInfo method = type.GetMethods(BindingFlags.NonPublic | BindingFlags.Instance)
            .Where(x => x.Name == "ReflectServableItems" && x.IsPrivate)
            .First();

            //Act
            List<ServableItem> servableItems = (List<ServableItem>)method.Invoke(apiServer, null);

            Assert.IsFalse(servableItems.Count(si => si.PropertyName.Equals("SecretElkDogs")) > 0);

            //var retVal = obj.Invoke("PrivateMethod");
        }
    }
}
