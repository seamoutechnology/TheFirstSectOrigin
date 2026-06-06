using System;
using System.Collections.Generic;
using GameClient.Network.Pb;

namespace GameClient
{
    public partial class GameManager
    {
        public List<Zone> AvailableZones { get; private set; } = new();

        public void SetZones(IEnumerable<Zone> zones)
        {
            AvailableZones = new List<Zone>(zones);
        }
    }
}
