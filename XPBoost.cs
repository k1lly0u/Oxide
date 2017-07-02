using System.Collections.Generic;
using Oxide.Core;
using Oxide.Core.Configuration;

namespace Oxide.Plugins
{
    [Info("XPBoost", "k1lly0u", "0.1.1", ResourceId = 0)]
    class XPBoost : RustPlugin
    {
        #region Fields
        StoredData storedData;
        private DynamicConfigFile data;

        private Dictionary<ulong, float> cachedLevels = new Dictionary<ulong, float>();
        private bool activeDebug = false;
        private Timer updateTimer;
        private float LevelAverage = 0;
        #endregion

        #region Oxide Hooks
        void Loaded()
        {
            data = Interface.Oxide.DataFileSystem.GetFile("xpboost_data");
        }
        void OnServerInitialized()
        {
            LoadVariables();
            LoadData();
            UpdateAverageLevel();
            UpdateLoop();
            foreach (var player in BasePlayer.activePlayerList)
                OnPlayerInit(player);
        }
        void OnPlayerInit(BasePlayer player)
        {
            var level = GetPlayerLevel(player.userID);
            if (level != null)
            {
                if ((float)level >= configData.MinimumLevel)
                    AddUpdatePlayer(player.userID, (float)level);
            }
        }
        void OnXpLevelUp(ulong id, int level)
        {
            var player = BasePlayer.FindByID(id);
            if (player != null)
            {
                if (level >= configData.MinimumLevel)
                    AddUpdatePlayer(player.userID, level);
            }
        }
        object OnXpEarn(ulong id, float amount, string source)
        {
            var currentLevel = GetPlayerLevel(id);
            if (currentLevel == null)
            {
                if (cachedLevels.ContainsKey(id))
                    currentLevel = cachedLevels[id];
                else return amount;
            }                

            if ((float)currentLevel >= LevelAverage)
            {
                Debug($"--- Player {id} is earning xp ---\n Players level ({currentLevel}) is greater than the average level.\n Giving default boost rate {configData.DefaultBoostRate}");
                return amount * configData.DefaultBoostRate;
            }
            else
            {
                var modifier = CalculateModifier((float)currentLevel);                
                Debug($"--- Player {id} is earning xp ---\n Players level ({currentLevel}) is less than the average level.\n Giving modified boost rate of {modifier + configData.DefaultBoostRate}");
                return amount * (modifier + configData.DefaultBoostRate);
            }                    
        }   
        void Unload()
        {
            SaveData();
            if (updateTimer != null)
                updateTimer.Destroy();
        }
        #endregion

        #region Functions
        private void AddUpdatePlayer(ulong id, float level)
        {            
            if (!cachedLevels.ContainsKey(id))
                cachedLevels.Add(id, level);
            else cachedLevels[id] = level;
        }
        private object GetPlayerLevel(ulong ID)
        {
            var agent = BasePlayer.FindXpAgent(ID);
            if (agent != null)
            {
                return agent.CurrentLevel;
            }
            return null;
        }
        private void UpdateAverageLevel()
        {
            Debug($"--- Updating average level ---");
            float level = 0;
            int count = 0;
            foreach(var entry in cachedLevels)
            {
                level += entry.Value;
                count++;
            }
            if (level == 0 || count == 0) LevelAverage = 1;
            else LevelAverage = level / count;
            Debug($"New average level is {LevelAverage}");
        }
        private float CalculateModifier(float playerLevel)
        {
            Debug($"--- Calculating modifier ---");
            float peakLevel = LevelAverage * configData.PeakBoostPercentage;
            Debug($"XP boost peak level is {peakLevel}");
            if (playerLevel < peakLevel)
            {                
                float percentage = playerLevel / peakLevel;
                Debug($"Players level is less than peak level. Fraction multiplier is {percentage}. Boost rate is {configData.PeakBoostRate * percentage}");
                return configData.PeakBoostRate * percentage;                
            }
            else
            {
                float max = LevelAverage - peakLevel;
                float min = playerLevel - peakLevel;
                float percentage = min / max;
                float modifier = 1 - percentage;
                Debug($"Players level ({playerLevel}) is greater than the peak level. Fraction multiplier is {modifier}. Boost rate is {configData.PeakBoostRate * (1-percentage)}");
                return configData.PeakBoostRate * modifier;
            }
        }
        private void UpdateLoop()
        {
            if (configData.AvgLevelUpdateTimer <= 0) return;
            updateTimer = timer.Once(configData.AvgLevelUpdateTimer * 60, () => {  UpdateAverageLevel(); SaveData(); UpdateLoop(); });
        }
        #endregion

        #region Debug
        private void Debug(string message)
        {
            if (activeDebug)
            {
                Puts(message);
            }
        }
        [ChatCommand("xpdebug")]
        private void cmdDebug(BasePlayer player, string command, string[] args)
        {
            if (!player.IsAdmin()) return;
            if (activeDebug)
            {
                activeDebug = false;
                SendReply(player, "Debug has been deactivated");
            }
            else
            {
                activeDebug = true;
                SendReply(player, "Debug has been activated");
            }
        }
        [ConsoleCommand("xpdebug")]
        private void ccmdDebug(ConsoleSystem.Arg arg)
        {
            if (arg.Connection?.authLevel < 1) return;
            if (activeDebug)
            {
                activeDebug = false;
                SendReply(arg, "Debug has been deactivated");
            }
            else
            {
                activeDebug = true;
                SendReply(arg, "Debug has been activated");
            }
        }
        [ConsoleCommand("setaverage")]
        private void ccmdSetAverage(ConsoleSystem.Arg arg)
        {
            if (arg.Connection?.authLevel < 1) return;
            if (arg.Args == null || arg.Args.Length == 0)
            {
                SendReply(arg, "Current average level: " + LevelAverage);
                SendReply(arg, "Change it by typing 'setaverage <amount>'");
            }
            else if (arg.Args.Length >= 1)
            {
                float newAvg;
                if (!float.TryParse(arg.Args[0], out newAvg))
                {
                    SendReply(arg, "You must enter a amount");
                    return;
                }
                else LevelAverage = newAvg;
                SendReply(arg, $"Average level has been changed to {LevelAverage}");
            }
        }
        [ConsoleCommand("xpboost.save")]
        private void ccmdSaveAverage(ConsoleSystem.Arg arg)
        {
            if (arg.Connection?.authLevel < 1) return;
            UpdateAverageLevel();
            SaveData();
        }
        #endregion

        #region Chat Commands
        [ChatCommand("xpboost")]
        private void cmdXPBoost(BasePlayer player, string command, string[] args)
        {
            SendReply(player, $"<color=#939393>---</color> <color=#C4FF00>{Title}   v{Version}</color> <color=#939393>---</color>");
            SendReply(player, $"<color=#939393>The current average player level is </color><color=#C4FF00>{LevelAverage}</color>");
            SendReply(player, $"<color=#939393>The current peak boost level is </color><color=#C4FF00>{LevelAverage * configData.PeakBoostPercentage}</color>");
            var currentLevel = GetPlayerLevel(player.userID);
            if (currentLevel == null)
            {
                if (cachedLevels.ContainsKey(player.userID))
                    currentLevel = cachedLevels[player.userID];
                else
                {
                    SendReply(player, "<color=#939393>Unable to get your data. Please try again later</color>");
                    return;
                }
            }
            if ((float)currentLevel >= LevelAverage)
            {
                SendReply(player, $"<color=#939393>Your current boost rate is </color><color=#C4FF00>{configData.DefaultBoostRate}</color>");
                return;
            }
            else
            {
                var modifier = CalculateModifier((float)currentLevel);
                SendReply(player, $"<color=#939393>Your current boost rate is </color><color=#C4FF00>{modifier + configData.DefaultBoostRate}</color>");
                return;
            }
        }
        #endregion

        #region Config        
        private ConfigData configData;
        class ConfigData
        {
            public int LevelCap { get; set; }
            public int MinimumLevel { get; set; }
            public float PeakBoostRate { get; set; }
            public float DefaultBoostRate { get; set; }
            public float PeakBoostPercentage { get; set; }
            public int AvgLevelUpdateTimer { get; set; }
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
                AvgLevelUpdateTimer = 15,
                LevelCap = 60,
                DefaultBoostRate = 2,
                MinimumLevel = 5,
                PeakBoostPercentage = 0.4f,
                PeakBoostRate = 5
            };
            SaveConfig(config);
        }
        private void LoadConfigVariables() => configData = Config.ReadObject<ConfigData>();
        void SaveConfig(ConfigData config) => Config.WriteObject(config, true);
        #endregion

        #region Data Management
        void SaveData()
        {
            storedData.playerLevels = cachedLevels;
            data.WriteObject(storedData);
        }
        void LoadData()
        {
            try
            {
                storedData = data.ReadObject<StoredData>();
                cachedLevels = storedData.playerLevels;
            }
            catch
            {
                storedData = new StoredData();
            }
        }
        class StoredData
        {
            public Dictionary<ulong, float> playerLevels = new Dictionary<ulong, float>();
        }
        #endregion
    }
}
