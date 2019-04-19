using GhostLineAPI;
using GhostLineAPI.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ExternalApp
{
    class Program
    {

        static void Main(string[] args)
        {
            ElkDogManager manager = new ElkDogManager();
            manager.Manage();
        }
    }
}
