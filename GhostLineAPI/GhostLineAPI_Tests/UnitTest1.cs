//using System;
//using Microsoft.VisualStudio.TestTools.UnitTesting;
//using GhostLineAPI;

//namespace GhostLineAPI_Tests
//{
//    [TestClass]
//    public class UnitTest1
//    {
//        [TestMethod]
//        public void ReflectionTest()
//        {
//            // we are using reflection to access a private method so we can test that reflection
//            // is successfully pulling in the meta data
//            //Class target = new Class();
//            //GhostLineAPIServer server = new GhostLineAPIServer();
//            //PrivateObject obj = new PrivateObject(target);

//            GhostLineAPIServer apiServer = new GhostLineAPIServer
//            {
//                ParentObj = (object)this,
//                Address = "127.0.0.1",
//                Port = 19001
//            };
//            apiServer.Authenticator = delegate (HttpListenerRequest req)
//            {
//                if (String.IsNullOrEmpty(req.Headers["Authorization"]))
//                    return false;

//                if (req.Headers["Authorization"].Equals("27bc5f2c-bed5-41c7-8a5d-aec966212146"))
//                    return true;
//                return false;
//            };

//            // Can't access PrivateObject from .NET Core ?? Boo !!
//            // http://anthonygiretti.com/2018/08/26/how-to-unit-test-private-methods-in-net-core-applications-even-if-its-bad/

//            Type type = typeof(GhostLineAPIServer);
//            //var hello = Activator.CreateInstance(type, firstName, lastName);
//            var server = Activator.CreateInstance(type);
//            MethodInfo method = type.GetMethods(BindingFlags.NonPublic | BindingFlags.Instance)
//            .Where(x => x.Name == "ReflectServableItems" && x.IsPrivate)
//            .First();

//            //Act
//            // var helloMan = (string)method.Invoke(hello, new object[] { firstName, lastName });
//            var servableItems = (string)method.Invoke(server, null);

//            Console.Out.WriteLine("Test finished. Delete this line soon.");

//            //var retVal = obj.Invoke("PrivateMethod");
//            //Assert.AreEqual(expectedVal, retVal);
//        }
//    }
//}
