using System;
using System.Collections.Generic;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("CoptorTracker", "k1lly0u", "2.0.0", ResourceId = 0)]
    class CoptorTracker : RustPlugin
    {
        #region Fields
        private List<BaseHelicopter> activeHelis = new List<BaseHelicopter>();
        private DateTime TimerStart;
        private int ChopperSpawnTime;
        private float ChopperLifeTimeOriginal;
        private float ChopperLifeTimeCurrent;
        private DateTime TimerSpawn;
        private DateTime ChopperSpawned;
        private bool spawnedHeli;

        #endregion

        #region Oxide Hooks
        void Loaded()
        {
            permission.RegisterPermission("coptortracker.use", this);
            lang.RegisterMessages(Messages, this);
            spawnedHeli = false;
        }
        void OnServerInitialized()
        {
            LoadVariables();
            ConVar.PatrolHelicopter.lifetimeMinutes = configData.LifeTime;
        }
        void OnEntitySpawned(BaseNetworkable entity)
        {
            if (entity is BaseHelicopter)
            {
                if (spawnedHeli)
                {
                    activeHelis.Add(entity as BaseHelicopter);
                    PrintToChat($"<color=orange>{LA("heliSpawned")}</color>");
                    spawnedHeli = false;
                }
                else KillHeli(entity as BaseHelicopter);
            }
        }
        void OnEntityTakeDamage(BaseCombatEntity entity, HitInfo info)
        {
            if (entity is BaseHelicopter)
            {
                var TimeNow = DateTime.Now;
                var ChopLT = ChopperLifeTimeCurrent;
                DateTime Duration = ChopperSpawned.AddMinutes(ChopLT);
                TimeSpan l = Duration.Subtract(TimeNow);
                if (l.Minutes <= 2)
                {
                    ChopperLifeTimeCurrent = ChopperLifeTimeCurrent + 5;
                    ConsoleSystem.Run.Server.Normal("heli.lifetimeminutes", ChopperLifeTimeCurrent.ToString());
                    PrintToChat($"<color=orange>{LA("lifeExtended")}</color>");
                }
            }
        }
        void OnEntityDeath(BaseCombatEntity entity, HitInfo info)
        {
            try
            {
                if (entity is BaseHelicopter)
                {
                    if (activeHelis.Contains(entity as BaseHelicopter))
                    {
                        activeHelis.Remove(entity as BaseHelicopter);
                    }
                }
            }
            catch { }
        }
        #endregion

        #region Functions
        private void SetHeliLifetime() => ConsoleSystem.Run.Server.Normal("heli.lifetimeminutes", configData.LifeTime);
        private bool hasPerm(BasePlayer player) => permission.UserHasPermission(player.UserIDString, "coptortracker.use");
        private void SpawnHeli()
        {
            spawnedHeli = true;
            ChopperLifeTimeCurrent = ChopperLifeTimeOriginal;
            var entity = GameManager.server.CreateEntity("assets/prefabs/npc/patrol helicopter/patrolhelicopter.prefab", new Vector3(), new Quaternion(), true);
            var heli = entity.GetComponent<BaseHelicopter>();
            if (heli != null)
            {
                entity.Spawn();
                ChopperSpawned = DateTime.Now;
                StartChopperSpawnFreq();
            }            
        }
        private void StartChopperSpawnFreq()
        {
            TimerStart = DateTime.Now;
            TimerSpawn = TimerStart.AddSeconds(ChopperSpawnTime);
            timer.In(ChopperSpawnTime, () => SpawnHeli());
        }
        private void KillHeli(BaseHelicopter heli) { heli.maxCratesToSpawn = 0; heli.DieInstantly();}
        private void KillHelis()
        {
            int i = 0;
            foreach(var heli in activeHelis)
            {
                KillHeli(heli);
                i++;
            }
            activeHelis.Clear();
            PrintToChat($"{i} helicopters have been destroyed");
        }
        #endregion

        #region ChatCommands
        [ChatCommand("nextheli")]
        private void cmdNextCoptor(BasePlayer player, string command, string[] args)
        {
            var TimeNow = DateTime.Now;

            TimeSpan t = TimerSpawn.Subtract(TimeNow);

            string TimeLeft = string.Format(string.Format("{0:D2}h:{1:D2}m:{2:D2}s", t.Hours, t.Minutes, t.Seconds));

            MSG(player, string.Format(LA("nextSpawn", player.UserIDString), TimeLeft));
            if (activeHelis.Count > 0) 
            {
                MSG(player, LA("isSpawned", player.UserIDString));
                var ChopLT = -ChopperLifeTimeCurrent;
                DateTime Duration = ChopperSpawned.AddMinutes(-ChopLT);
                TimeSpan l = Duration.Subtract(TimeNow);
                string DurationLeft = string.Format(string.Format("{0:D2}h:{1:D2}m:{2:D2}s", l.Hours, l.Minutes, l.Seconds));
                MSG(player, string.Format(LA("heliLeave", player.UserIDString), DurationLeft));
            }
            else MSG(player, LA("notSpawned", player.UserIDString));
        }

        [ChatCommand("spawnheli")]
        private void cmdSpawnHeli(BasePlayer player, string command, string[] args)
        {            
            if (hasPerm(player))            
                SpawnHeli();            
            else
            {
                MSG(player, LA("noPerm", player.UserIDString));
                PrintWarning(string.Format(LA("spawnPerm"), player.displayName));
            }
        }

        [ChatCommand("killallhelis")]
        private void cmdKillHelis(BasePlayer player, string command, string[] args)
        {
            if (hasPerm(player))            
                KillHelis();
            else
            {
                MSG(player, LA("noPerm", player.UserIDString));
                PrintWarning(string.Format(LA("destroyPerm") ,player.displayName));
            }

        }
        #endregion

        #region Config        
        private ConfigData configData;
        class ConfigData
        {
            public int RespawnTimer { get; set; }
            public int LifeTime { get; set; }
        }
        private void LoadVariables()
        {
            LoadConfigVariables();
            SaveConfig();
        }
        protected override void LoadDefaultConfig()
        {
            var config = new ConfigData
            {
                RespawnTimer = 45,
                LifeTime = 15
            };
            SaveConfig(config);
        }
        private void LoadConfigVariables() => configData = Config.ReadObject<ConfigData>();
        void SaveConfig(ConfigData config) => Config.WriteObject(config, true);
        #endregion

        #region Messaging
        private void MSG(BasePlayer player, string message1, string message2 = "") => SendReply(player, $"<color=orange>{message1}</color><color=#939393>{message2}</color>");
        private string LA(string key, string ID = null) => lang.GetMessage(key, this, ID);
        Dictionary<string, string> Messages = new Dictionary<string, string>()
        {
            {"destroyPerm", "{0} has tried destroying all helicopters without permission" },
            {"spawnPerm", "{0} has tried spawning a helicopter without permission" },
            {"noPerm", "You do not have permission to use this command." },
            {"nextSpawn", "The next helicopter will spawn in {0}" },
            {"heliLeave", "It will leave in {0}" },
            {"lifeExtended", "The helicopter has been engaged and its lifetime has been extended" },
            {"heliSpawned", "A helicopter has spawned, watch out!" },
            {"isSpawned", "There is currently a helicopter out hunting" },
            {"notSpawned", "There are currently no helicopters spawned" }
        };
        #endregion

    }
}
