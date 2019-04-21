using System;
using CitizenFX.Core;

namespace TruckingServer
{
    public class Server : BaseScript
    {
        public Server()
        {
            EventHandlers.Add("baseevents:enteredVehicle", new Action<Player, object, int, string>(OnPlayerEnteredVehicle));
            EventHandlers.Add("baseevents:leftVehicle", new Action<Player, object, int, string>(OnPlayerLeftVehicle));
            EventHandlers.Add("baseevents:enteringVehicle", new Action<Player, int, int, string>(OnPlayerEnteringVehicle));
        }

        public void OnPlayerEnteredVehicle([FromSource]Player player, object vehicle, int seat, string displayName)
        {
            TriggerClientEvent("TruckingClient:enteredTruck", vehicle, seat, displayName);
            Debug.WriteLine($"Player {player.Name} entered {displayName} {vehicle} to seat {seat}");
        }

        private void OnPlayerLeftVehicle([FromSource]Player player, object vehicle, int seat, string displayName)
        {
            Debug.WriteLine($"Player {player.Name} left {displayName} {vehicle} from seat {seat}");
        }

        private void OnPlayerEnteringVehicle([FromSource]Player player, int vehicleHash, int seat, string displayName)
        {
            Debug.WriteLine($"Player {player.Name} left {displayName} {vehicleHash} from seat {seat}");
        }
    }
}
