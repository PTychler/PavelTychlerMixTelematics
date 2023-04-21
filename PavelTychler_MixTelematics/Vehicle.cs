using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MixTelematics
{
    public class Vehicle
    {
        public Int32 PositionID { get; set; }
        public String? VehicleRegistration { get; set; }
        public float Latitude { get; set; }
        public float Longitude { get; set; }
        public UInt64 RecordedTimeUTC { get; set; }


        public void PrintVehicle()
        {
            Console.WriteLine($"PositionID {PositionID}");
            Console.WriteLine($"VehicleRegistration {VehicleRegistration}");
            Console.WriteLine($"Latitude {Latitude}");
            Console.WriteLine($"Longitude {Longitude}");
            Console.WriteLine($"RecordedTimeUTC {RecordedTimeUTC}");
            Console.WriteLine();
        }

        
    }
}
