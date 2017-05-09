using System.Collections.Generic;
using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Core.Configuration;
using Oxide.Core.Plugins;
using System.Linq;

namespace Oxide.Plugins
{
    [Info("VoteRewards", "k1lly0u", "2.0.1", ResourceId = 752)]
    class Voter : RustPlugin
    {
        #region Fields 
        [PluginReference] Plugin ServerRewards;
        [PluginReference] Plugin Economics;

        StoredData storedData;
        private DynamicConfigFile data;

        private Dictionary<string, ItemDefinition> itemDefs = new Dictionary<string, ItemDefinition>();

        private Timer broadcastTimer;

        const string tracker = "http://rust-servers.net/api/?action=custom&object=plugin&element=reward&key={KEY}&steamid=";
        private string col1;
        private string col2;
        #endregion

        #region Oxide Hooks
        void Loaded()
        {
            lang.RegisterMessages(Messages, this);
            data = Interface.Oxide.DataFileSystem.GetFile("voter_data");
        }
        void OnServerInitialized()
        {
            itemDefs = ItemManager.itemList.ToDictionary(i => i.shortname);
            LoadVariables();
            LoadData();
            col1 = $"<color={configData.Messaging.MainColor}>";
            col2 = $"<color={configData.Messaging.MSGColor}>";

            if (string.IsNullOrEmpty(configData.TrackerKey))
            {
                PrintError("Please enter your API key in the config!");
                Interface.Oxide.UnloadPlugin("Voter");
                return;
            }

            cmd.AddChatCommand(configData.VoteCommand, this, cmdVote);
            cmd.AddChatCommand(configData.RewardCommand, this, cmdRewards);

            if (configData.Broadcasting.Enabled)
                BroadcastLoop();
        }
        void Unload()
        {
            if (broadcastTimer != null)
                broadcastTimer.Destroy();
        }
        #endregion

        #region Functions
        private void BroadcastLoop()
        {
            PrintToChat(string.Format(msg("broadcastMessage"), configData.VoteCommand));
            broadcastTimer = timer.Once(configData.Broadcasting.Timer * 60, () => BroadcastLoop());
        }
        private void CheckForVotes(BasePlayer player) => GetWebRequest(player);
        

        private void GetWebRequest(BasePlayer player)
        {
            webrequest.EnqueueGet(tracker.Replace("{KEY}", configData.TrackerKey) + player.UserIDString, (code, response) =>
            {
                if (response == null || code != 200)
                {
                    PrintWarning($"Error: {code} - Couldn't get an answer from Rust-Servers.net for {player.displayName}");
                    SendReply(player, $"{col1}{msg("contactError", player.UserIDString)}</color>");
                }
                else
                {
                    int responeNum;
                    if (!int.TryParse(response, out responeNum))
                    {
                        PrintError("There was a error processing what was returned from rust-servers.net");
                        SendReply(player, $"{col1}{msg("voteError", player.UserIDString)}</color>");
                    }
                    else if (responeNum == 0)
                    {
                        SendReply(player, $"{col1}{msg("noVotes", player.UserIDString)}</color>");
                    }
                    else if (responeNum == 1)
                    {
                        storedData.userData[player.userID] += configData.PointsPerVote;
                        SaveData();
                        SendReply(player, $"{col2}{string.Format(msg("voteSuccess", player.UserIDString), $"</color>{col1}{storedData.userData[player.userID]}</color>{col2}", $"</color>{col1}/{configData.RewardCommand}</color>{col2}")}</color>");
                    }                   
                }
            }, this);
        }
        #endregion
       
        #region Rewards
        private bool GiveReward(BasePlayer player, Rewards reward)
        {
            if (reward.RPAmount > 0)
            {
                if (!ServerRewards)
                {
                    SendReply(player, $"{col1}{msg("noSR", player.UserIDString)}</color>");
                    return false;
                }
                GiveRP(player, reward.RPAmount);
            }
            if (reward.EcoAmount > 0)
            {
                if (!Economics)
                {
                    SendReply(player, $"{col1}{msg("noEco", player.UserIDString)}</color>");
                    return false;
                }
                GiveCoins(player, reward.EcoAmount);
            }            
            foreach(var rewardItem in reward.RewardItems)
            {                
                if (itemDefs.ContainsKey(rewardItem.Shortname))
                {
                    player.GiveItem(ItemManager.CreateByItemID(itemDefs[rewardItem.Shortname].itemid, rewardItem.Amount, rewardItem.SkinID), BaseEntity.GiveItemReason.PickedUp);
                }
                else
                {
                    SendReply(player, $"{col1}{msg("noItem", player.UserIDString)}</color>");
                    PrintError($"The reward {rewardItem.Shortname} does not exist. Check for the correct item shortname");
                    return false;
                }
            }
            SendReply(player, $"{col1}{msg("rewardSuccess", player.UserIDString)}</color>");
            return true;
        }
        private void GiveRP(BasePlayer player, int amount) => ServerRewards?.Call("AddPoints", player.UserIDString, amount);
        private void GiveCoins(BasePlayer player, int amount) => Economics?.Call("Deposit", player.UserIDString, amount);
        #endregion

        #region Chat Commands
        
        private void cmdVote(BasePlayer player, string command, string[] args)
        {
            if (!storedData.userData.ContainsKey(player.userID))
                storedData.userData.Add(player.userID, 0);            

            CheckForVotes(player);
        }
        
        private void cmdRewards(BasePlayer player, string command, string[] args)
        {
            if (!storedData.userData.ContainsKey(player.userID))
                storedData.userData.Add(player.userID, 0);
            if (args == null || args.Length == 0)
            {
                SendReply(player, $"{col1}Voter</color>  {col2}v </color>{col1}{Version}</color>");
                SendReply(player, $"{col2}{string.Format(msg("hasPoints", player.UserIDString), $"</color>{col1}{storedData.userData[player.userID]}</color>{col2}")}</color>");
                SendReply(player, $"{col2}{string.Format(msg("rewardHelp", player.UserIDString), $"</color>{col1}/{configData.RewardCommand}")}</color>");
                SendReply(player, $"{col2}{msg("available", player.UserIDString)}</color>");
                foreach (var reward in configData.Rewards)
                {
                    string rewardString = $"{col1}{msg("id", player.UserIDString)}</color> {col2}{reward.Key}</color>\n{col1}{msg("cost", player.UserIDString)}</color> {col2}{reward.Value.CostToBuy}</color>";
                    if (Economics && reward.Value.EcoAmount > 0)
                        rewardString += $"\n{col1}{msg("economics", player.UserIDString)}</color> {col2}{reward.Value.EcoAmount}</color>";
                    if (ServerRewards && reward.Value.RPAmount > 0)
                        rewardString += $"\n{col1}{msg("serverrewards", player.UserIDString)}</color> {col2}{reward.Value.RPAmount}</color>";

                    string rewardItems = string.Empty;
                    if (reward.Value.RewardItems.Count > 0)
                    {
                        rewardItems += $"\n{col1}{msg("rewardItems", player.UserIDString)}</color> {col2}";
                        for (int i = 0; i < reward.Value.RewardItems.Count; i++)
                        {
                            var item = reward.Value.RewardItems[i];
                            rewardItems += $"{item.Amount}x {itemDefs[item.Shortname].displayName.english}";
                            if (i < reward.Value.RewardItems.Count - 1)
                                rewardItems += ", ";
                            else rewardItems += "</color>";
                        } 
                    }
                    rewardString += rewardItems;
                    SendReply(player, rewardString);
                }
            }
            if (args.Length == 1)
            {
                int key;
                if (!int.TryParse(args[0], out key))
                {
                    SendReply(player, $"{col2}{msg("noId", player.UserIDString)}</color>");
                    return;
                }
                if (!configData.Rewards.ContainsKey(key))
                {
                    SendReply(player, $"{col2}{msg("notExist", player.UserIDString)} {key}</color>");
                    return;
                }
                var reward = configData.Rewards[key];
                if (storedData.userData[player.userID] < reward.CostToBuy)
                {
                    SendReply(player, $"{col2}{msg("noPoints", player.UserIDString)}</color>");
                    return;
                }
                else
                {
                    if (GiveReward(player, reward))
                    {
                        storedData.userData[player.userID] -= reward.CostToBuy;
                        SaveData();
                    }
                }
            }

        }
        #endregion

        #region HelpText
        private void SendHelpText(BasePlayer player)
        {
            SendReply(player, string.Format(msg("helptext1", player.UserIDString), configData.VoteCommand));
            SendReply(player, string.Format(msg("helptext2", player.UserIDString), configData.RewardCommand));
        }
        #endregion

        #region Config      
        class Rewards
        {
            public List<RewardItem> RewardItems { get; set; }           
            public int RPAmount { get; set; }
            public int EcoAmount { get; set; }
            public int CostToBuy { get; set; }
        }
        class RewardItem
        {
            public string Shortname { get; set; }
            public ulong SkinID { get; set; }
            public int Amount { get; set; }
        }
        class Messaging
        {
            public string MainColor { get; set; }
            public string MSGColor { get; set; }
        }
        class Broadcasting
        {
            public bool Enabled { get; set; }
            public int Timer { get; set; }
        }
        private ConfigData configData;
        class ConfigData
        {
            [JsonProperty(PropertyName = "Automated Broadcasting")]
            public Broadcasting Broadcasting { get; set; }
            [JsonProperty(PropertyName = "Message Colors")]
            public Messaging Messaging { get; set; }
            [JsonProperty(PropertyName = "Reward List")]
            public Dictionary<int, Rewards> Rewards { get; set; }
            [JsonProperty(PropertyName = "Chat Command - Reward Menu")]
            public string RewardCommand { get; set; }
            [JsonProperty(PropertyName = "Chat Command - Vote Checking")]
            public string VoteCommand { get; set; }
            [JsonProperty(PropertyName = "API Key for rust-servers.net")]
            public string TrackerKey { get; set; }
            [JsonProperty(PropertyName = "Points received per vote")]
            public int PointsPerVote { get; set; }
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
                Broadcasting = new Broadcasting
                {
                    Enabled = true,
                    Timer = 30
                },
                TrackerKey = "",
                Rewards = new Dictionary<int, Rewards>
                {
                    {0, new Rewards
                    {
                        CostToBuy = 1,
                        RPAmount = 0,
                        EcoAmount = 0,                      
                        RewardItems = new List<RewardItem>
                        {
                            new RewardItem
                            {
                                Amount = 1,
                                Shortname = "supply.signal",
                                SkinID = 0
                            }
                        }                       
                    } },
                    {1, new Rewards
                    {
                        CostToBuy = 2,
                        RPAmount = 0,
                        EcoAmount = 0,                       
                        RewardItems = new List<RewardItem>
                        {
                            new RewardItem
                            {
                                Amount = 100,
                                Shortname = "hq.metal.ore",
                                SkinID = 0
                            },
                            new RewardItem
                            {
                                Amount = 150,
                                Shortname = "sulfur.ore",
                                SkinID = 0
                            }
                        }
                    } },
                    {2, new Rewards
                    {
                        CostToBuy = 3,
                        RPAmount = 200,
                        EcoAmount = 0,                      
                        RewardItems = new List<RewardItem>()
                    } }
                },
                RewardCommand = "reward",
                VoteCommand = "vote",
                PointsPerVote = 1,
                Messaging = new Messaging
                {
                    MainColor = "#ce422b",
                    MSGColor = "#939393"
                }

            };
            SaveConfig(config);
        }
        private void LoadConfigVariables() => configData = Config.ReadObject<ConfigData>();
        void SaveConfig(ConfigData config) => Config.WriteObject(config, true);
        #endregion

        #region Data Management
        void SaveData() => data.WriteObject(storedData);
        void LoadData()
        {
            try
            {
                storedData = data.ReadObject<StoredData>();
            }
            catch
            {
                storedData = new StoredData();
            }
        }
        class StoredData
        {
            public Dictionary<ulong, int> userData = new Dictionary<ulong, int>();
        }       
        #endregion

        #region Localization
        string msg(string key, string playerId = null) => lang.GetMessage(key, this, playerId);

        Dictionary<string, string> Messages = new Dictionary<string, string>
        {
            {"contactError", "There was a error contacting rust-servers.net. Please try again later"},
            {"voteError", "There was a error collecting your votes. Please try again later" },
            {"noVotes", "Thank you for voting for us but you have not cast anymore votes since the last time you checked"},
            {"voteSuccess", "Thank you for voting for us! You now have {0} vote points available. You can spend these points by typing {1}"},
            {"noSR", "ServerRewards is not installed. Unable to issue points"},
            {"noEco", "Economics is not installed. Unable to issue coins"},
            {"noItem", "Unable to find the requested reward" },
            {"rewardSuccess", "Thank you for voting for us! Enjoy your reward." },
            {"hasPoints", "You currently have {0} vote points to spend"},
            {"rewardHelp", "You can claim any reward package you have enough vote points to buy by typing {0} <ID>" },
            {"available", "Available Rewards:" },
            {"economics", "Coins (Economics):"},
            {"serverrewards", "RP (ServerRewards):"},
            {"rewardItems", "Items:" },
            {"id", "ID:" },
            {"cost", "Cost:" },
            {"noId", "You need to enter a reward ID"},
            {"notExist", "There is no reward with the ID:" },
            {"noPoints", "You do not have enough vote points to purchase that reward"},
            {"broadcastMessage", "<color=#939393>Vote for us on </color><color=#ce422b>rust-servers.net</color><color=#939393> and receive rewards! Type </color><color=#ce422b>/{0}</color><color=#939393> after voting</color>"},
            {"helptext1", "<color=#ce422b>/{0}</color><color=#939393> - Checks rust-servers.net to see if you have voted for this server</color>" },
            {"helptext2", "<color=#ce422b>/{0}</color><color=#939393> - Display's available rewards and how many votepoints you have</color>" }
        };
        #endregion
    }
}
