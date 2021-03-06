﻿using GhostLineAPI;
using GhostLineAPI.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;

namespace ExternalApp
{
    class ElkDogManager
    {
        [GhostWrite,GhostRead]
        public static List<ElkDog> UntrainedElkDogs { get; set; }

        [GhostWrite, GhostRead]
        public static List<ElkDog> TrainedElkDogs { get; set; }

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
            apiServer.After = delegate ()
            {
                Console.WriteLine("Current: " + UntrainedElkDogs[0].Name + " can fly? " + UntrainedElkDogs[0].CanFly);
            };

            apiServer.SetupAndStartServer();

            Console.ReadLine();
        }
    }
}
