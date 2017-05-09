using System;
using Oxide.Core;

namespace Oxide.Plugins
{
    [Info("StashLogs", "k1lly0u", "0.1.0", ResourceId = 0)]
    class StashLogs : RustPlugin
    {
        #region Fields        
        private bool isInit = false;
        #endregion

        #region Oxide Hooks
        void OnServerInitialized() => isInit = true;        

        void OnLootEntity(BasePlayer player, BaseEntity entity)
        {
            if (isInit)
            {
                if (entity != null)
                {
                    if (entity is StashContainer)
                    {
                        Log(player.displayName, player.transform.position.ToString());
                    }
                }                                
            }
        }       
        #endregion

        #region Functions       
        void Log(string playername, string position)
        {
            if (!Interface.Oxide.DataFileSystem.ExistsDatafile($"StashLogs/foldercreator"))
                Interface.Oxide.DataFileSystem.SaveDatafile($"StashLogs/foldercreator");
            var date = DateTime.Now.ToString("yyyy-MM-dd");
            ConVar.Server.Log($"oxide/data/StashLogs/StashLog_{date}.txt", $"{playername} @ {position}");
        }
       
        #endregion
    }
}
