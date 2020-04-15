using GhostLineAPI;
using GhostLineAPI.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using ElkDogModel;

namespace ElkDogModel
{
    public class ElkDogManager
    {
        [GhostReadWrite]
        public static List<ElkDog> UntrainedElkDogs { get; set; }

        [GhostRead(DisableVersioning = false, OverrideName = "TrainedElkDogs", Version = "V1")]
        public static List<ElkDog> TrainedElkDogs { get; set; }

        [GhostRead(DisableVersioning = false, OverrideName = "TrainedElkDogs", Version = "V2")]
        public static List<ElkDog> TrainedElkDogsV2 { get; set; }

        public static List<ElkDog> SecretElkDogs { get; set; }

        [GhostWrite, GhostRead]
        public static String _locationCity;

        public ElkDogManager()
        {
        }

        public void Manage()
        {
            Console.WriteLine("Staging Elk Dogs ....");
            var namesList = new List<String> { "Travis", "Nermal", "Benji", "Shiloh", "Rod", "Hickhack", "Sojo", "Riffy", "HodgePodge", "Milo", "Shannon", "Spinzin", "Furtile", "Miningine", "Sniffy", "Doorbell", "Ralco", "Jeb", "Nono", "Oscar", "Dingo", "Halepale", "Ralphie", "Pinkpod", "Flip" };

            Random rnd = new Random();

            UntrainedElkDogs = new List<ElkDog>();
            foreach (var index in Enumerable.Range(0, 3))
            {
                ElkDog elkDog = new ElkDog
                {
                    Name = namesList.ElementAt(rnd.Next(0, namesList.Count)),
                    MarketValue = 300,
                    CanFly = false,
                    Role = ElkDog.ElkDogRole.None
                };
                UntrainedElkDogs.Add(elkDog);
            }

            TrainedElkDogs = new List<ElkDog>();
            foreach (var index in Enumerable.Range(0, 2))
            {
                ElkDog elkDog = new ElkDog
                {
                    Name = namesList.ElementAt(rnd.Next(0, namesList.Count)),
                    MarketValue = 300,
                    CanFly = false,
                    Role = ElkDog.ElkDogRole.None
                };
                ElkDog.ElkDogRole role = ElkDog.ElkDogRole.GroundDelivery;
                switch (rnd.Next(0, 3))
                {
                    case 0:
                        role = ElkDog.ElkDogRole.AirDelivery;
                        elkDog.CanFly = true;
                        break;
                    case 1:
                        role = ElkDog.ElkDogRole.SeaDelivery;
                        break;
                    case 2:
                        role = ElkDog.ElkDogRole.HumanTransportation;
                        break;
                    default:
                    case 3:
                        role = ElkDog.ElkDogRole.GroundDelivery;
                        break;
                }
                TrainedElkDogs.Add(elkDog);
            }

            TrainedElkDogsV2 = new List<ElkDog>();
            foreach (var index in Enumerable.Range(0, 3))
            {
                ElkDog elkDog = new ElkDog
                {
                    Name = namesList.ElementAt(rnd.Next(0, namesList.Count)),
                    MarketValue = 300,
                    CanFly = false,
                    Role = ElkDog.ElkDogRole.None
                };
                ElkDog.ElkDogRole role = ElkDog.ElkDogRole.GroundDelivery;
                switch (rnd.Next(0, 3))
                {
                    case 0:
                        role = ElkDog.ElkDogRole.AirDelivery;
                        elkDog.CanFly = true;
                        break;
                    case 1:
                        role = ElkDog.ElkDogRole.SeaDelivery;
                        break;
                    case 2:
                        role = ElkDog.ElkDogRole.HumanTransportation;
                        break;
                    default:
                    case 3:
                        role = ElkDog.ElkDogRole.GroundDelivery;
                        break;
                }
                TrainedElkDogs.Add(elkDog);
            }

            SecretElkDogs = new List<ElkDog>();
            foreach (var index in Enumerable.Range(0, 2))
            {
                ElkDog elkDog = new ElkDog
                {
                    Name = namesList.ElementAt(rnd.Next(0, namesList.Count)),
                    MarketValue = 300,
                    CanFly = false,
                    Role = ElkDog.ElkDogRole.None
                };
                SecretElkDogs.Add(elkDog);
            }

            // Display inventory
            foreach (var elkDog in UntrainedElkDogs.Union(TrainedElkDogs))
            {
                Console.WriteLine($"Elkdog: {elkDog.Name}\t Id: {elkDog.Id}\t Role: {elkDog.Role.ToString()}");
            }

            // Kick the tires and light the fires
            GhostLineAPIServer apiServer = new GhostLineAPIServer
            {
                ParentObj = (object)this,
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
            //apiServer.Validator = delegate (HttpListenerRequest req)
            //{
            //    var result = new ValidationResponse();
            //    result.Success = false;
            //    result.Messages = new List<ValidationMessage> {
            //        new ValidationMessage {
            //            Code = "F128",
            //            Message = "Request contains insufficent number of query parameters.",
            //            ValidationType = ValidationMessageType.Failure
            //        }
            //    };
            //    return result;
            //};
            apiServer.After = delegate ()
            {
                Console.WriteLine("Current: " + UntrainedElkDogs[0].Name + " can fly? " + UntrainedElkDogs[0].CanFly);
            };

            _locationCity = "Raleigh";

            apiServer.SetupAndStartServer();

            Console.ReadLine();
        }
    }
}
