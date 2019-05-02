using GhostLineAPI;
using GhostLineAPI.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using ElkDogModel;

namespace ExternalApp
{
    class Program
    {

        static void Main(string[] args)
        {
            // To test, open a postman at http://127.0.0.1:19001/UntrainedElkDogs
            // and add Key "Authorization" with value of "27bc5f2c-bed5-41c7-8a5d-aec966212146"
            // for GET ... same thing for POST but add the body RAW for whatever you get back from the GET

            ElkDogManager manager = new ElkDogManager();
            manager.Manage();
        }
    }
}
