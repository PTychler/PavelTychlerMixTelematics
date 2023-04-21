using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Text;

namespace MixTelematics
{
    internal class Program
    {
        public const int GRID_SCALE = 3;
        static void Main(string[] args)
        {

            var watch = System.Diagnostics.Stopwatch.StartNew();
            List<Vehicle>[,]? vehiclesFromData = null;

            try
            {
                //Read VehiclePositions.dat
                vehiclesFromData = ReadUsingBRToGrid();
                //vehiclesFromDataBruteForce = ReadUsingBR();

                watch.Stop();
                var readMs = watch.ElapsedMilliseconds;
                watch.Restart();

                List<Vehicle> testVehicles = InitialiseFromVehicles();
                List<Vehicle> closestVehicles = new List<Vehicle>();

                //Brute Force Test
                //List<Vehicle> tempclosestVehicles = new List<Vehicle>();

                //foreach (var vehicle in testVehicles)
                //{
                //    Vehicle closestVehicleFound = FindClosestVehicleBruteForce(vehicle, vehiclesFromDataBruteForce);
                //    tempclosestVehicles.Add(closestVehicleFound);
                //    closestVehicleFound.PrintVehicle();
                //}

                //Grid
                foreach (var vehicle in testVehicles)
                {
                    Vehicle closestVehicleFound = FindClosestVehicleGrid(vehicle, vehiclesFromData);
                    closestVehicles.Add(closestVehicleFound);
                }

                //Test for validity

                //for (int i = 0; i < tempclosestVehicles.Count; i++)
                //{
                //    if (tempclosestVehicles[i].PositionID == closestVehicles[i].PositionID)
                //        Console.WriteLine("true");
                //    else
                //        Console.WriteLine("false");
                //}

                watch.Stop();
                var findMs = watch.ElapsedMilliseconds;
                var totalMs = readMs + findMs;

                //Console.WriteLine($"{i} outside of america");

                Console.WriteLine($"{readMs} to read the file");
                Console.WriteLine($"{findMs} to find the closest vehicles");
                Console.WriteLine($"{totalMs} total");

                //Console.ReadKey();
            }
            catch
            (IOException ioex)
            {
                Console.WriteLine(ioex.Message);
            }

            Console.ReadKey();

        }

        static Vehicle FindClosestVehicleBruteForce(Vehicle fromVehicle, List<Vehicle> ToVehicles)
        {
            Vehicle? closestVehicle = null;
            double closestDistance = double.MaxValue;

            foreach(var vehicle in ToVehicles)
            {
                var resultDistance = CalculateHaversineDistanceBetweenTwoPoints(fromVehicle, vehicle);
                if (resultDistance < closestDistance)
                {
                    closestDistance = resultDistance;
                    closestVehicle = vehicle;
                }
            }
            
            return closestVehicle;
        }

        static Vehicle FindClosestVehicleGrid(Vehicle fromVehicle, List<Vehicle>[,] ToVehicles, int radius = 0, double closestDistance = double.MaxValue, Vehicle? closestVehicle = null, int maxDepth = 0)
        {
            closestDistance = double.MaxValue;
            Tuple<int, int> indexVehicle = GetGridIndex(fromVehicle.Latitude, fromVehicle.Longitude);

            //Radius edge search case break. Applicable for Edge distance check
            if (maxDepth > 3)
            {
                return closestVehicle;
            }

            //First iternation check of recursive function
            if (radius == 0)
            {
                //Find closest distance within fromVehicle index
                if (ToVehicles[indexVehicle.Item1, indexVehicle.Item2].Any())
                    foreach (var vehicle in ToVehicles[indexVehicle.Item1, indexVehicle.Item2])
                    {
                        var resultDistance = CalculateHaversineDistanceBetweenTwoPoints(fromVehicle, vehicle);
                        if (resultDistance < closestDistance)
                        {
                            closestDistance = resultDistance;
                            closestVehicle = vehicle;
                        }
                    }
            }
            else
            {
                //Search surrounding radius for closest vehicle
                for (int i = indexVehicle.Item1 - radius; i < indexVehicle.Item1 + radius; i++)
                {
                    //Find closest distance vehicle in top and bottom row of non max left and max right of radius
                    if (i != indexVehicle.Item1 - radius && i != indexVehicle.Item1 + radius)
                    {
                        Tuple<int, int> validIndex1 = EnsureValidIndex(i, indexVehicle.Item2 - radius);
                        Tuple<int, int> validIndex2 = EnsureValidIndex(i, indexVehicle.Item2 + radius);

                        if (ToVehicles[validIndex1.Item1, validIndex1.Item2].Any())
                            foreach (var vehicle in ToVehicles[validIndex1.Item1, validIndex1.Item2])
                            {
                                var resultDistance = CalculateHaversineDistanceBetweenTwoPoints(fromVehicle, vehicle);
                                if (resultDistance < closestDistance)
                                {
                                    closestDistance = resultDistance;
                                    closestVehicle = vehicle;
                                }
                            }
                        if (ToVehicles[validIndex2.Item1, validIndex2.Item2].Any())
                            foreach (var vehicle in ToVehicles[validIndex2.Item1, validIndex2.Item2])
                            {
                                var resultDistance = CalculateHaversineDistanceBetweenTwoPoints(fromVehicle, vehicle);
                                if (resultDistance < closestDistance)
                                {
                                    closestDistance = resultDistance;
                                    closestVehicle = vehicle;
                                }
                            }
                    }

                    //Else Find closest distance vehicle in left and right column of radius
                    if (i == indexVehicle.Item1 - radius || i == indexVehicle.Item1 + radius)
                    {
                        for (int j = indexVehicle.Item2 - radius; j < indexVehicle.Item2 + radius; j++)
                        {
                            Tuple<int, int> validIndex = EnsureValidIndex(i, j);

                            if(ToVehicles[validIndex.Item1, validIndex.Item2].Any())
                                foreach (var vehicle in ToVehicles[validIndex.Item1, validIndex.Item2])
                                {
                                    var resultDistance = CalculateHaversineDistanceBetweenTwoPoints(fromVehicle, vehicle);
                                    if (resultDistance < closestDistance)
                                    {
                                        closestDistance = resultDistance;
                                        closestVehicle = vehicle;
                                    }
                                }
                        }

                    }
                }
            }

            //If not vehicle is found search radius around 
            if (closestVehicle is null)
            {
                closestVehicle = FindClosestVehicleGrid(fromVehicle, ToVehicles, ++radius, closestDistance);
            }
            //Check if the distance between the vehicles is shorter than the distance of the input vehicle to the edges of the grid block
            else
            {
                //get index of edges of best result to far
                var edgeTestIndex = GetGridIndex(fromVehicle.Latitude, fromVehicle.Longitude);
                Tuple<int, int> validIndexEdgeTest = EnsureValidIndex(edgeTestIndex.Item1, edgeTestIndex.Item2);

                if (IsDistanceToEdgeSmallerThan(closestVehicle,
                ((validIndexEdgeTest.Item1 - radius) * 3) - 90,
                ((validIndexEdgeTest.Item2 + 1 + radius) * 3) - 90,
                ((validIndexEdgeTest.Item1 - radius) * 3) - 180,
                ((validIndexEdgeTest.Item2 + 1 + radius) * 3) - 180,
                closestDistance))
                {
                    closestVehicle = FindClosestVehicleGrid(fromVehicle, ToVehicles, ++radius, closestDistance, closestVehicle, ++maxDepth);

                }
            }

            return closestVehicle;
        }


        //Reading from data file 
        static List<Vehicle>[,] ReadUsingBRToGrid()
        {


            var dir = Directory.GetCurrentDirectory();
            var file = Path.Combine(dir, "VehiclePositions.dat");

            List<Vehicle>[,] gloablGrid = new List<Vehicle>[(180 / GRID_SCALE), (360 / GRID_SCALE)];

            for (int i = 0; i < (180 / GRID_SCALE); i++)
                for (int j = 0; j < (360 / GRID_SCALE); j++)
                    gloablGrid[i, j] = new List<Vehicle>();

            using (FileStream fs = new FileStream(file, FileMode.Open, FileAccess.Read))
            {
                using (BinaryReader r = new BinaryReader(fs))
                {
                    while (r.BaseStream.Position != r.BaseStream.Length)
                    {
                        var insertionVehicle = new Vehicle
                        {
                            PositionID = r.ReadInt32(),
                            VehicleRegistration = ReadSZString(r),
                            Latitude = r.ReadSingle(),
                            Longitude = r.ReadSingle(),
                            RecordedTimeUTC = r.ReadUInt64()
                        };

                        Tuple<int, int> index = GetGridIndex(insertionVehicle.Latitude,insertionVehicle.Longitude);

                        if (gloablGrid[index.Item1,index.Item2] is null)
                        {
                            gloablGrid[index.Item1, index.Item2] = new List<Vehicle> {insertionVehicle};
                        }
                        else
                        {
                            gloablGrid[index.Item1, index.Item2].Add(insertionVehicle);
                        }
                    }
                }
            }

            return gloablGrid;
        }

        static List<Vehicle> ReadUsingBR()
        {
            var dir = Directory.GetCurrentDirectory();
            var file = Path.Combine(dir, "VehiclePositions.dat");

            List<Vehicle> vehicles = new List<Vehicle>();

            using (FileStream fs = new FileStream(file, FileMode.Open, FileAccess.Read))
            {
                using (BinaryReader r = new BinaryReader(fs))
                {
                    while (r.BaseStream.Position != r.BaseStream.Length)
                    {
                        vehicles.Add(new Vehicle
                        {
                            PositionID = r.ReadInt32(),
                            VehicleRegistration = ReadSZString(r),
                            Latitude = r.ReadSingle(),
                            Longitude = r.ReadSingle(),
                            RecordedTimeUTC = r.ReadUInt64()
                        });
                    }
                }
            }

            return vehicles;
        }




        //Index Functions
        public static Tuple<int, int> EnsureValidIndex(int x, int y)
        {
            var tempx = x % ((90 + 90) / GRID_SCALE);
            var tempy =  y % ((180 + 180) / GRID_SCALE);

            if (tempx >= (90 + 90) / GRID_SCALE || tempx < 0)
            {
                tempx = 0;
            }
            if (tempy >= (180 + 180) / GRID_SCALE || tempy < 0)
            {
                tempy = 0;
            }

            
            return new Tuple<int, int>(tempx, tempy);
        }

        public static Tuple<int,int> GetGridIndex(float lat, float lon)
        {
            var tempLat = lat; 
            var tempLon = lon;
            int latRound = (int)Math.Round(tempLat, 0);
            int lonRound = (int)Math.Round(tempLon, 0);

            return new Tuple<int, int>((latRound + 89) / GRID_SCALE, (lonRound + 179) / GRID_SCALE);
        }



        

        //Distance Calculations
        static double CalculateHaversineDistanceBetweenTwoPoints(float lat1, float lon1, float lat2, float lon2)
        {
            var d1 = lat1 * (Math.PI / 180.0);
            var num1 = lon1 * (Math.PI / 180.0);
            var d2 = lat2 * (Math.PI / 180.0);
            var num2 = lon2 * (Math.PI / 180.0) - num1;
            var d3 = Math.Pow(Math.Sin((d2 - d1) / 2.0), 2.0) + Math.Cos(d1) * Math.Cos(d2) * Math.Pow(Math.Sin(num2 / 2.0), 2.0);

            return 6376500.0 * (2.0 * Math.Atan2(Math.Sqrt(d3), Math.Sqrt(1.0 - d3)));

        }

        static double CalculateHaversineDistanceBetweenTwoPoints(Vehicle vehicle1, Vehicle vehicle2)
        {
            var d1 = vehicle1.Latitude * (Math.PI / 180.0);
            var num1 = vehicle1.Longitude * (Math.PI / 180.0);
            var d2 = vehicle2.Latitude * (Math.PI / 180.0);
            var num2 = vehicle2.Longitude * (Math.PI / 180.0) - num1;
            var d3 = Math.Pow(Math.Sin((d2 - d1) / 2.0), 2.0) + Math.Cos(d1) * Math.Cos(d2) * Math.Pow(Math.Sin(num2 / 2.0), 2.0);

            return 6376500.0 * (2.0 * Math.Atan2(Math.Sqrt(d3), Math.Sqrt(1.0 - d3)));
        }

        
        static bool IsDistanceToEdgeSmallerThan(Vehicle vehicle, double latMin, double latMax, double lonMin, double lonMax, double closestDistance)
        {
            //Vehicle Position
            var d1 = vehicle.Latitude * (Math.PI / 180.0);
            var num1 = vehicle.Longitude * (Math.PI / 180.0);

            //Left Edge
            var d2 = latMin * (Math.PI / 180.0);
            var num2 = 0;
            var d3 = Math.Pow(Math.Sin((d2 - d1) / 2.0), 2.0) + Math.Cos(d1) * Math.Cos(d2) * Math.Pow(Math.Sin(num2 / 2.0), 2.0);
            var lr = 6376500.0 * (2.0 * Math.Atan2(Math.Sqrt(d3), Math.Sqrt(1.0 - d3)));

            //Right Edge
            var d4 = latMax * (Math.PI / 180.0);
            var num3 = 0;
            var d5 = Math.Pow(Math.Sin((d4 - d1) / 2.0), 2.0) + Math.Cos(d1) * Math.Cos(d4) * Math.Pow(Math.Sin(num3 / 2.0), 2.0);
            var rr = 6376500.0 * (2.0 * Math.Atan2(Math.Sqrt(d5), Math.Sqrt(1.0 - d5)));

            //Top Edge
            var d6 = 0;
            var num4 = lonMin * (Math.PI / 180.0) - num1;
            var d7 = Math.Pow(Math.Sin((d6 - d1) / 2.0), 2.0) + Math.Cos(d1) * Math.Cos(d6) * Math.Pow(Math.Sin(num4 / 2.0), 2.0);
            var rt = 6376500.0 * (2.0 * Math.Atan2(Math.Sqrt(d3), Math.Sqrt(1.0 - d7)));

            //Bottom Edge
            var d8 = 0;
            var num5 = lonMax * (Math.PI / 180.0) - num1;
            var d9 = Math.Pow(Math.Sin((d8 - d1) / 2.0), 2.0) + Math.Cos(d1) * Math.Cos(d8) * Math.Pow(Math.Sin(num5 / 2.0), 2.0);
            var rb = 6376500.0 * (2.0 * Math.Atan2(Math.Sqrt(d3), Math.Sqrt(1.0 - d9)));

            if (lr < closestDistance)
            {
                return true;
            }    
                
            if (rr < closestDistance)
            {
                return true;
            }
                
            if (rt < closestDistance)
            {
                return true;
            }
                
            if (rb < closestDistance)
            {
                return true;
            }
                

            return false;
        }

        static double CalculateEuclideanDistanceBetweenTwoPoints(float x, float y, float cx, float cy)
        {
            var dx = cx - x;
            var dy = cy - y;
            var d = Math.Sqrt(dx * dx + dy * dy);

            return d;
        }




        //BinaryReader Extension Method for Null Terminated ASCII Strings
        static string ReadSZString(BinaryReader reader)
        {
            var result = new StringBuilder();
            while (true)
            {
                byte b = reader.ReadByte();
                if (0 == b)
                    break;
                result.Append((char)b);
            }
            return result.ToString();
        }

        //Initialization of Vehicles to Test
        static List<Vehicle> InitialiseFromVehicles()
        {
            List<Vehicle> result = new List<Vehicle>
            {
                new Vehicle
                {
                    PositionID = 1,
                    VehicleRegistration = "",
                    Latitude = 34.544909F,
                    Longitude = -102.100843F,
                    RecordedTimeUTC = 0
                },
                new Vehicle
                {
                    PositionID = 2,
                    VehicleRegistration = "",
                    Latitude = 32.345544F,
                    Longitude = -99.123124F,
                    RecordedTimeUTC = 0
                },
                new Vehicle
                {
                    PositionID = 3,
                    VehicleRegistration = "",
                    Latitude = 33.234235F,
                    Longitude = -100.214124F,
                    RecordedTimeUTC = 0
                },
                new Vehicle
                {
                    PositionID = 4,
                    VehicleRegistration = "",
                    Latitude = 35.195739F,
                    Longitude = -95.348899F,
                    RecordedTimeUTC = 0
                },
                new Vehicle
                {
                    PositionID = 5,
                    VehicleRegistration = "",
                    Latitude = 31.895839F,
                    Longitude = -97.789573F,
                    RecordedTimeUTC = 0
                },
                new Vehicle
                {
                    PositionID = 6,
                    VehicleRegistration = "",
                    Latitude = 32.895839F,
                    Longitude = -101.789573F,
                    RecordedTimeUTC = 0
                },
                new Vehicle
                {
                    PositionID = 7,
                    VehicleRegistration = "",
                    Latitude = 34.115839F,
                    Longitude = -100.225732F,
                    RecordedTimeUTC = 0
                },
                new Vehicle
                {
                    PositionID = 8,
                    VehicleRegistration = "",
                    Latitude = 32.335839F,
                    Longitude = -99.992232F,
                    RecordedTimeUTC = 0
                },
                new Vehicle
                {
                    PositionID = 9,
                    VehicleRegistration = "",
                    Latitude = 33.535339F,
                    Longitude = -94.792232F,
                    RecordedTimeUTC = 0
                },
                new Vehicle
                {
                    PositionID = 10,
                    VehicleRegistration = "",
                    Latitude = 32.234235F,
                    Longitude = -100.222222F,
                    RecordedTimeUTC = 0
                }

            };

            return result;
        }


        //Check for if it test data is all in America
        static int OutofAmerica(List<Vehicle> ToVehicles)
        {
            int number = 0;

            foreach (var vehicle in ToVehicles)
            {

                if (vehicle.Latitude > 49.4 || vehicle.Latitude < 24.5 || vehicle.Longitude < -124.8 || vehicle.Longitude > -66.9)
                {
                    number++;
                }
            }

            return number;
        }
    }
}