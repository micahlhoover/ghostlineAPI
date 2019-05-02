using System;
using System.Collections.Generic;
using System.Text;

namespace ElkDogModel
{
    public class ElkDog
    {
        public enum ElkDogRole
        {
            None,
            HumanTransportation,
            GroundDelivery,
            SeaDelivery,
            AirDelivery
        }

        public bool CanFly { get; set; }
        public String Name { get; set; }
        public decimal MarketValue { get; set; }
        public ElkDogRole Role { get; set; }
        public double Longitude { get; set; }
        public double Latitude { get; set; }
        public Guid Id { get; set; }

        public ElkDog()
        {
            MarketValue = 0.0M;
            Role = ElkDogRole.None;
            Name = "Cignus";
            Latitude = 35.905570;
            Longitude = -78.661110;
            Id = Guid.NewGuid();
        }

    }
}
