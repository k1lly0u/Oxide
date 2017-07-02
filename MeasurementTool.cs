using System.Collections.Generic;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("MeasurementTool", "k1lly0u", "0.1.0", ResourceId = 0)]
    class MeasurementTool : RustPlugin
    {
        #region Fields

        Dictionary<ulong, Vector3> distanceCheck = new Dictionary<ulong, Vector3>();
        #endregion

        [ChatCommand("point")]
        void cmdPoint(BasePlayer player, string command, string[] args)
        {
            if (!distanceCheck.ContainsKey(player.userID))
            {
                distanceCheck.Add(player.userID, player.transform.position);
                SendReply(player, "Point A added");                
            }
            else
            {
                SendReply(player, $"Total Distance: {Vector3.Distance(distanceCheck[player.userID], player.transform.position)}M");
                distanceCheck.Remove(player.userID);
            }
        }
    }
}
