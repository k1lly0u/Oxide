using System.Collections.Generic;
using Oxide.Core;
using Oxide.Core.Configuration;

namespace Oxide.Plugins
{
    [Info("XPModifier", "k1lly0u", "0.1.1", ResourceId = 0)]
    class XPModifier : RustPlugin
    {
        #region Fields
        XPMData xpmData;
        private DynamicConfigFile data;

        #endregion

        #region Oxide Hooks
        void Loaded()
        {
            data = Interface.Oxide.DataFileSystem.GetFile("xpmod_data");
        }
        void OnServerInitialized()
        {
            LoadVariables();
            LoadData();
            foreach (var perm in xpmData.Permissions)
                permission.RegisterPermission(perm.Key, this);
        }
        object OnXpEarn(ulong id, float amount, string source)
        {
            var multiplier = GetMultiplier(id);
            amount = amount * multiplier;
            
            return amount;
        }
        #endregion

        #region Functions
        private float GetMultiplier(ulong playerid)
        {
            float percentage = configData.DefaultMultiplier;
            foreach (var entry in xpmData.Permissions)
            {
                if (permission.UserHasPermission(playerid.ToString(), entry.Key))
                {
                    percentage = entry.Value;
                    break;
                }
            }
            return percentage;
        }
        #endregion

        #region Chat Commands
        [ChatCommand("xpm")]
        private void cmdRod(BasePlayer player, string command, string[] args)
        {
            if (player.IsAdmin())
            {
                if (args == null || args.Length == 0)
                {
                    SendMSG(player, "/xpm add <permission> <multiplier> - Adds a new permission and multiplier");
                    SendMSG(player, "/xpm edit <permission> <multiplier> - Edits a existing permission and multiplier");
                    SendMSG(player, "/xpm remove <permission> - Remove a permission");
                    SendMSG(player, "/xpm list - Lists all permissions and assigned multiplier");
                    return;
                }
                if (args.Length >= 1)
                {
                    switch (args[0].ToLower())
                    {
                        case "add":
                            if (args.Length == 3)
                            {
                                string perm = args[1].ToLower();
                                if (!perm.StartsWith(Title.ToLower() + "."))
                                    perm = Title.ToLower() + "." + perm;
                                if (!permission.PermissionExists(perm) && !xpmData.Permissions.ContainsKey(perm))
                                {
                                    float percentage = 0;
                                    if (float.TryParse(args[2], out percentage))
                                    {
                                        xpmData.Permissions.Add(perm, percentage);
                                        permission.RegisterPermission(perm, this);
                                        SaveData();
                                        SendMSG(player, string.Format("You have successfully added the permission {0} with a multiplier of {1}", perm, percentage));
                                        return;
                                    }
                                    SendMSG(player, "You must enter a valid multiplier number");
                                    return;
                                }
                                SendMSG(player, "That permission already exists");
                                return;
                            }
                            SendMSG(player, "/xpm add <permission> <multiplier> - Adds a new permission and multiplier");
                            return;
                        case "edit":
                            if (args.Length == 3)
                            {
                                if (xpmData.Permissions.ContainsKey(args[1].ToLower()))
                                {
                                    float percentage = 0;
                                    if (float.TryParse(args[2], out percentage))
                                    {
                                        xpmData.Permissions[args[1].ToLower()] = percentage;
                                        SaveData();
                                        SendMSG(player, string.Format("You have successfully edited the permission {0} with a multiplier of {1}", args[1].ToLower(), percentage));
                                        return;
                                    }
                                    SendMSG(player, "You must enter a valid multiplier number");
                                    return;
                                }
                                SendMSG(player, string.Format("The permission {0} does not exist", args[1].ToLower()));
                                return;
                            }
                            SendMSG(player, "/xpm edit <permission> <multiplier> - Edits a existing permission and multiplier");
                            return;
                        case "remove":
                            if (args.Length >= 2)
                                if (xpmData.Permissions.ContainsKey(args[1].ToLower()))
                                {
                                    xpmData.Permissions.Remove(args[1].ToLower());
                                    SaveData();
                                    SendMSG(player, string.Format("You have successfully remove the permission {0}", args[1].ToLower()));
                                    return;
                                }
                            SendMSG(player, string.Format("The permission {0} does not exist", args[1].ToLower()));
                            return;
                        case "list":
                            if (xpmData.Permissions.Count > 0)
                            {
                                SendMSG(player, "Current permissions;");
                                foreach (var entry in xpmData.Permissions)
                                    SendMSG(player, $"{entry.Key} -- {entry.Value}x");
                                return;
                            }
                            SendMSG(player, "There are currently no permissions set up");
                            return;
                    }
                }
            }
        }
        private void SendMSG(BasePlayer player, string message) => SendReply(player, "<color=orange>" + message + "</color>");
        #endregion

        #region Config        
        private ConfigData configData;
        class ConfigData
        {
            public float DefaultMultiplier { get; set; }
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
                DefaultMultiplier = 1.0f
            };
            SaveConfig(config);
        }
        private void LoadConfigVariables() => configData = Config.ReadObject<ConfigData>();
        void SaveConfig(ConfigData config) => Config.WriteObject(config, true);
        #endregion

        #region Data Management
        void SaveData() => data.WriteObject(xpmData);
        void LoadData()
        {
            try
            {
                xpmData = data.ReadObject<XPMData>();
            }
            catch
            {
                xpmData = new XPMData();
            }
        }
        class XPMData
        {
            public Dictionary<string, float> Permissions = new Dictionary<string, float>();
        }
        #endregion
    }
}
