using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using CitizenFX.Core;
using CitizenFX.Core.Native;

namespace TruckingClient
{
    public class Client : BaseScript
    {
        // TODO: Weight reduces speed of truck, health reduces salary, distance increases salary, fuel efficiency
        // Random events that can force you to check quality of tire, check load, engine, etc.
        // Work office, choose what type of trucking: construction, regular, vehicle transportation, etc.
        // Configuration file

        // Initial data
        private int _truckId = 0;
        private int _trailerId = 0;
        private int _loadId = 0;
        private bool _working = false;
        private bool _hasLoaded = false;
        private float _salary = 0f;

        // Key locations 
        private Vector3 _officeLocation = new Vector3(787.407f, -2991.458f, 6.021f);
        private Vector3 _loadLocation = new Vector3(972.593f, -2911.816f, 5.984f);

        // Delivery locations
        private readonly Dictionary<string, Vector3> _deliveryLocations = new Dictionary<string, Vector3>()
        {
            { "Warehouse A", new Vector3(0f, 5f, 0f) },
            { "Dock A, shipment", new Vector3(0f, 5f, 0f) },
        };

        public Client()
        {
            Tick += OnTick;
            Initialize();

            // Event Handlers
            EventHandlers.Add("TruckingClient:enteredTruck", new Action<object, int, string>(EnteredTruck));
        }
        
        // Player has entered truck,
        private void EnteredTruck(object vehicle, int seat, string name)
        {
            if (!_working) return;

            if (Game.PlayerPed.LastVehicle.NetworkId == _truckId)
            {
                SendChatMessage(
                    Game.PlayerPed.LastVehicle.IsAttachedTo(Entity.FromNetworkId(_trailerId))
                        ? "Welcome back to your truck."
                        : "Please ~r~attach your trailer^r to your truck marked on the map.", 255, 0, 0
                );
            }

            else SendChatMessage("You are still working, please go back to your truck and finish your mission", 255, 0, 0);

        }

        private void Initialize()
        {
            var workOffice = World.CreateBlip(_officeLocation);
            workOffice.Sprite = BlipSprite.GarbageTruck;
            workOffice.Name = "Trucker Office";

            Commands();
        }

        private void Commands() 
        {
            API.RegisterCommand("trucker", new Action<int, List<object>, string>(async (src, args, raw) =>
            {
                var argList = args.Select(o => o.ToString()).ToList();

                switch (argList[0])
                {
                    case "start":
                        {
                            var playerPos = Game.PlayerPed.Position;

                            if (_working)
                            {
                                SendChatMessage("You are already working, cancel mission to start a new one.", 255, 0, 0);
                                CreateMission();
                            }

                            else if (API.GetDistanceBetweenCoords(playerPos.X, playerPos.Y, playerPos.Z, _officeLocation.X, _officeLocation.Y, _officeLocation.Z, false) > 5f)
                            {
                                SendChatMessage("You are not at the trucker office, and therefore can't be given a mission.", 255, 0, 0);
                            }

                            else
                            {
                                SendChatMessage("You have started a new trucker mission.", 255, 0, 0);
                                CreateMission();
                            }

                            break;
                        }

                    case "cancel":
                        {
                            if (_working)
                            {
                                if (API.DoesEntityExist(Entity.FromNetworkId(_loadId).Handle)) Entity.FromNetworkId(_loadId).Delete();
                                if (API.DoesEntityExist(Entity.FromNetworkId(_trailerId).Handle)) Entity.FromNetworkId(_trailerId).Delete();
                                if (API.DoesEntityExist(Entity.FromNetworkId(_truckId).Handle)) Entity.FromNetworkId(_truckId).Delete();

                                SendChatMessage("You have cancelled your trucker mission.", 255, 0, 0);

                                // Restoring default values
                                _working = false;
                                _hasLoaded = false;
                                _truckId = 0;
                                _trailerId = 0;
                                _loadId = 0;
                            }

                            else SendChatMessage("You can't stop working when you aren't working, right?", 255, 0, 0);

                            break;
                        }

                     case "salary":
                        {
                            SendChatMessage($"Your total current salary is ${_salary}", 255, 0, 0);
                            break;
                        }

                    case "load":
                        {
                            if (_hasLoaded) SendChatMessage("You have already loaded, proceed to the delivery location", 255, 0, 0);

                            else
                            {
                                var playerPos = Game.PlayerPed.Position;

                                if (API.GetDistanceBetweenCoords(playerPos.X, playerPos.Y, playerPos.Z, _loadLocation.X, _loadLocation.Y, _loadLocation.Z, false) < 10f)
                                {
                                    SendChatMessage("Your trailer is currently being loaded.", 255, 0, 0);
                                    await Delay(1000);
                                    LoadTruck();
                                }
                                else
                                {
                                    SendChatMessage("You're not at the loading location, please go there.", 255, 0, 0);
                                }
                            }

                            break;
                        }

                    default:
                        {
                            SendChatMessage("Usage: /trucker [start/cancel/load/salary]", 255, 0, 0);
                            break;
                        }
                }
            }), false);
        }

        private async Task OnTick()
        {
            World.DrawMarker(MarkerType.TruckSymbol, _officeLocation, Vector3.Zero, new Vector3(180f, 0, 120f), 1.0f * Vector3.One, Color.FromArgb(255, 255, 0, 0), true);

            if (_working)
            {
                World.DrawMarker(MarkerType.ChevronUpx1, _loadLocation, Vector3.Zero, new Vector3(180f, 0, 120f), 1.0f * Vector3.One, Color.FromArgb(255, 255, 0, 0), true);
            }
        }

        public static void SendChatMessage(string message, int r, int g, int b)
        {
            var msg = new Dictionary<string, object>
            {
                ["color"] = new[] { r, g, b },
                ["args"] = new[] { "[TRUCKER]", message }
            };

            TriggerEvent("chat:addMessage", msg);
        }

        public async void CreateMission()
        {
            // Spawning truck and trailer
            var truck = await World.CreateVehicle(VehicleHash.Packer, new Vector3(815.287f, -3034.411f, 5.135f), 45);
            var trailer = await World.CreateVehicle(VehicleHash.TRFlat, new Vector3(815.287f, -3042.411f, 5.135f), 45);

            // Attaching trailer to truck
            API.AttachVehicleToTrailer(truck.Handle, trailer.Handle, 30f);

            // Placing player inside truck
            Game.PlayerPed.SetIntoVehicle(truck, VehicleSeat.Driver);

            // Player is now working
            _working = true;
            truck.NeedsToBeHotwired = false;
            _truckId = truck.NetworkId;
            _trailerId = trailer.NetworkId;

            // Blip on truck and trailer for the player to keep track of them
            truck.AttachBlip();
            trailer.AttachBlip();

            // Placing waypoint to get load
            Function.Call(Hash.SET_NEW_WAYPOINT, _loadLocation.X, _loadLocation.Y, _loadLocation.Z);
            SendChatMessage("Follow the GPS to get load. Once arrived, type /trucker load", 255, 0, 0);
        }

        public async void LoadTruck()
        {
            // Create a container prop and attaches it to trailer
            var trailerLoad = await World.CreateProp("prop_container_01a", Entity.FromNetworkId(_truckId).Position, true, false);
            trailerLoad.AttachTo(Entity.FromNetworkId(_trailerId));

            // Keeping track of load
            _loadId = trailerLoad.NetworkId;
            _hasLoaded = true;

            SendChatMessage("Your trailer has been loaded, go to next waypoint for delivery.", 255, 0, 0);
            Function.Call(Hash.SET_NEW_WAYPOINT, 0f, 0f, 0f);
        }
    }
}