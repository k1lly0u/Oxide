using System;
using System.Text;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Oxide.Core;
using Oxide.Core.Configuration;
using Oxide.Game.Rust.Cui;
using Oxide.Core.Plugins;
using UnityEngine;
using System.Linq;
using System.Reflection;
using Oxide.Core.Libraries;
using Oxide.Plugins;
using System.Collections;
using System.Text.RegularExpressions;

namespace Oxide.Plugins
{
    [Info("Jail", "Reneb / k1lly0u", "3.1.0", ResourceId = 794)]
    class Jail : RustPlugin
    {
        #region Fields
        [PluginReference] Plugin ZoneManager;
        [PluginReference] Plugin Spawns;
        [PluginReference] Plugin Kits;

        PrisonData prisonData;
        PrisonerData prisonerData;
        private DynamicConfigFile prisondata;
        private DynamicConfigFile prisonerdata;

        private Dictionary<string, PrisonEntry> prisons = new Dictionary<string, PrisonEntry>();
        private Dictionary<ulong, PrisonerEntry> prisoners = new Dictionary<ulong, PrisonerEntry>();

        const string UIJailTimer = "JailUI_TimeRemaining";
        #endregion

        #region Oxide Hooks
        void Loaded()
        {
            prisondata = Interface.Oxide.DataFileSystem.GetFile("Jail/prison_data");
            prisonerdata = Interface.Oxide.DataFileSystem.GetFile("Jail/prisoner_data");

            lang.RegisterMessages(Messages, this);
            permission.RegisterPermission("jail.canenter", this);
        }
        void OnServerInitialized()
        {
            LoadVariables();
            LoadData();
        }
        #endregion

        #region Functions
        void SendToPrison(BasePlayer player, string prisonName, double time)
        {
            int cellNumber = GetEmptyCell(prisonName);            

            object spawnLocation = GetSpawnLocation(prisonName, cellNumber);
            if (spawnLocation is Vector3)
            {
                PrisonerEntry entry = new PrisonerEntry
                {
                    cellNumber = cellNumber,
                    prisonName = prisonName,
                    releaseDate = time + GrabCurrentTime(),
                    x = player.transform.position.x,
                    y = player.transform.position.y,
                    z = player.transform.position.z
                };
                prisoners[player.userID] = entry;
                if (SaveInventory(player))
                {
                    player.inventory.Strip();
                    MovePosition(player, (Vector3)spawnLocation);
                    CheckIn(player, prisonName);
                }
            }
            
        }
        int GetEmptyCell(string prisonName)
        {
            int cellNumber = prisons[prisonName].occupiedCells.Where(x => x.Value == false).ToList().GetRandom().Key;
            prisons[prisonName].occupiedCells[cellNumber] = true;
            return cellNumber;
        }
        private void CheckIn(BasePlayer player, string prisonName)
        {
            if (player.IsSleeping() || player.HasPlayerFlag(BasePlayer.PlayerFlags.ReceivingSnapshot))
            {
                timer.In(1, () => CheckIn(player, prisonName));
                return;
            }
            PrisonEntry entry = prisons[prisonName];
            if (configData.GiveInmateKits && !string.IsNullOrEmpty(entry.inmateKit))
                Kits.Call("GiveKit", entry.inmateKit);

            if (!configData.AllowBreakouts && !string.IsNullOrEmpty(entry.zoneId))
                ZoneManager.Call("AddPlayerToZoneKeepinlist", entry.zoneId, player);

            ShowJailTimer(player);
        }
        private void FreeFromJail(BasePlayer player)
        {
            PrisonerEntry entry = prisoners[player.userID];
            prisons[entry.prisonName].occupiedCells[entry.cellNumber] = false;
            MovePosition(player, configData.ReturnHomeAfterRelease ? new Vector3(entry.x, entry.y, entry.z) : CalculateFreePosition(entry.prisonName));
            RestoreAllInventory(player);
        }
        #endregion

        #region Teleportation
        private object GetSpawnLocation(string prisonName, int cellNumber)
        {
            var success = Spawns.Call("GetSpawn", new object[] { prisons[prisonName].spawnFile, cellNumber });
            if (success is string)
            {
                PrintError($"There was a error retrieving spawn location #{cellNumber} at prison {prisonName}");
                return null;
            }
            return (Vector3)success;
        }
        private Vector3 CalculateFreePosition(string prisonName)
        {
            PrisonEntry entry = prisons[prisonName];
            Vector2 onCircle = UnityEngine.Random.insideUnitCircle;
            onCircle.Normalize();
            onCircle *= entry.radius;
            RaycastHit hitInfo;
            Vector3 point = Vector3.zero;
            if (Physics.Raycast(new Vector3(onCircle.x, 0, onCircle.y), Vector3.down, out hitInfo, LayerMask.GetMask("Terrain", "World", "Construction")))
                point.y = hitInfo.point.y;
            point.y = Mathf.Max(point.y, TerrainMeta.HeightMap.GetHeight(point));            
            return point;
        } 

        private void MovePosition(BasePlayer player, Vector3 destination)
        {
            if (player.net?.connection != null)
                player.ClientRPCPlayer(null, player, "StartLoading", null, null, null, null, null);
            StartSleeping(player);
            player.MovePosition(destination);
            if (player.net?.connection != null)
                player.ClientRPCPlayer(null, player, "ForcePositionTo", destination);
            if (player.net?.connection != null)
                player.SetPlayerFlag(BasePlayer.PlayerFlags.ReceivingSnapshot, true);
            player.UpdateNetworkGroup();
            player.SendNetworkUpdateImmediate(false);
            if (player.net?.connection == null) return;
            try { player.ClearEntityQueue(null); } catch { }
            player.SendFullSnapshot();
        }
        private void StartSleeping(BasePlayer player)
        {
            if (player.IsSleeping())
                return;
            player.SetPlayerFlag(BasePlayer.PlayerFlags.Sleeping, true);
            if (!BasePlayer.sleepingPlayerList.Contains(player))
                BasePlayer.sleepingPlayerList.Add(player);
            player.CancelInvoke("InventoryUpdate");
        }
        #endregion

        #region UI
        class UI
        {

            static public CuiElementContainer CreateElementContainer(string panelName, string color, string aMin, string aMax)
            {
                var NewElement = new CuiElementContainer()
                {
                    {
                        new CuiPanel
                        {
                            Image = {Color = color},
                            RectTransform = {AnchorMin = aMin, AnchorMax = aMax},
                        },
                        new CuiElement().Parent = "Hud",
                        panelName
                    }
                };
                return NewElement;
            }
            static public void CreateLabel(ref CuiElementContainer container, string panel, string color, string text, int size, string aMin, string aMax, TextAnchor align = TextAnchor.MiddleCenter)
            {
                container.Add(new CuiLabel
                {
                    Text = { Color = color, FontSize = size, Align = align, Text = text },
                    RectTransform = { AnchorMin = aMin, AnchorMax = aMax }
                },
                panel);

            }
        }
        private void ShowJailTimer(BasePlayer player)
        {
            if (player != null)
            {
                CuiHelper.DestroyUi(player, UIJailTimer);
                PrisonerEntry entry;
                if (prisoners.TryGetValue(player.userID, out entry))
                {
                    var time = entry.releaseDate - GrabCurrentTime();
                    if (time > 0)
                    {
                        string clock = FormatTime(time);

                        var container = UI.CreateElementContainer(UIJailTimer, "0.3 0.3 0.3 0.6", "0.42 0.965", "0.58 0.995");
                        UI.CreateLabel(ref container, UIJailTimer, "", $"Remaining: {clock}", 18, "0 0", "1 1");
                        CuiHelper.AddUi(player, container);
                        timer.In(1, () => ShowJailTimer(player));
                    }
                    else
                    {
                        if (configData.AutoReleaseWhenExpired)
                            FreeFromJail(player);
                        else
                        {
                            var container = UI.CreateElementContainer(UIJailTimer, "0.3 0.3 0.3 0.6", "0.42 0.965", "0.58 0.995");
                            UI.CreateLabel(ref container, UIJailTimer, "", $"Free to leave!", 18, "0 0", "1 1");
                            CuiHelper.AddUi(player, container);
                        }
                    }
                    
                }
            }
        }
        private string FormatTime(double time)
        {
            TimeSpan dateDifference = TimeSpan.FromSeconds((float)time);
            var days = dateDifference.Days;
            var hours = dateDifference.Hours;
            hours += (days * 24);
            var mins = dateDifference.Minutes;
            var secs = dateDifference.Seconds;
            if (hours > 0)
                return string.Format("{0:00}:{1:00}:{2:00}", hours, mins, secs);
            else return string.Format("{0:00}:{1:00}", mins, secs);
        }
        #endregion

        #region Zone Management
        bool IsInZone(BasePlayer player, string zoneID)
        {
            if (ZoneManager == null) return false;
            return (bool)ZoneManager.Call("isPlayerInZone", zoneID, player);
        }
        void OnEnterZone(string zoneID, BasePlayer player)
        {
            if (prisons.ContainsKey(zoneID))
            {                
                string prisonName = prisons.FirstOrDefault(x => x.Value.zoneId == zoneID).Key;
                SendReply(player, string.Format("Welcome to {0} {1}", prisonName, player.displayName));
                if (configData.AllowBreakouts) return;
            }
        }
        void OnExitZone(string zoneID, BasePlayer player)
        {
            if (prisons.ContainsKey(zoneID))
            {
                if (IsPrisoner(player))
                {
                    if (configData.AllowBreakouts)
                    {
                        string prisonName = prisons.FirstOrDefault(x => x.Value.zoneId == zoneID).Key;
                        SendReply(player, string.Format("You have broken out of {0} and are now on the run!", prisonName));
                        return;
                    }
                    
                }                
            }
        }
        #endregion

        #region Inventory Saving and Restoration
        public bool SaveInventory(BasePlayer player)
        {
            Dictionary<string, List<InvItem>> kititems = new Dictionary<string, List<InvItem>>();
            kititems["belt"].AddRange(GetItems(player.inventory.containerBelt));
            kititems["main"].AddRange(GetItems(player.inventory.containerMain));
            kititems["wear"].AddRange(GetItems(player.inventory.containerWear));
            prisoners[player.userID].savedInventory = kititems;
            return true;
        }
        private IEnumerable<InvItem> GetItems(ItemContainer container)
        {
            return container.itemList.Select(item => new InvItem
            {
                itemid = item.info.itemid,
                amount = item.amount,
                ammo = (item.GetHeldEntity() as BaseProjectile)?.primaryMagazine.contents ?? 0,
                ammotype = (item.GetHeldEntity() as BaseProjectile)?.primaryMagazine.ammoType.shortname ?? null,
                skin = item.skin,
                condition = item.condition,
                instanceData = item.instanceData ?? null,
                contents = item.contents?.itemList.Select(item1 => new InvItem
                {
                    itemid = item1.info.itemid,
                    amount = item1.amount,
                    condition = item1.condition
                }).ToArray()
            });
        }
        private void RestoreAllInventory(BasePlayer player)
        {
            player.inventory.Strip();
            PrisonerEntry entry = prisoners[player.userID];
            timer.In(1, () =>
            {
                foreach (var container in entry.savedInventory)
                    RestoreItems(player, container.Value, container.Key);
                timer.In(1, () => prisoners.Remove(player.userID));
            });
        }
        private void RestoreItems(BasePlayer player, List<InvItem> items, string type)
        {
            ItemContainer container = type == "belt" ? player.inventory.containerBelt : type == "wear" ? player.inventory.containerWear : player.inventory.containerMain;

            for (int i = 0; i < container.capacity; i++)
            {
                var existingItem = container.GetSlot(i);
                if (existingItem != null)
                {
                    existingItem.RemoveFromContainer();
                    existingItem.Remove(0f);
                }
                if (items.Count > i)
                {
                    var itemData = items[i];
                    var item = ItemManager.CreateByItemID(itemData.itemid, itemData.amount, itemData.skin);
                    item.condition = itemData.condition;
                    if (itemData.instanceData != null)
                        item.instanceData = itemData.instanceData;

                    var weapon = item.GetHeldEntity() as BaseProjectile;
                    if (weapon != null)
                    {
                        if (!string.IsNullOrEmpty(itemData.ammotype))
                            weapon.primaryMagazine.ammoType = ItemManager.FindItemDefinition(itemData.ammotype);
                        weapon.primaryMagazine.contents = itemData.ammo;
                    }
                    if (itemData.contents != null)
                    {
                        foreach (var contentData in itemData.contents)
                        {
                            var newContent = ItemManager.CreateByItemID(contentData.itemid, contentData.amount);
                            if (newContent != null)
                            {
                                newContent.condition = contentData.condition;
                                newContent.MoveToContainer(item.contents);
                            }
                        }
                    }
                    item.position = i;
                    item.SetParent(container);
                }
            }
        }
        #endregion

        #region Helpers
        private double GrabCurrentTime() => DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1, 0, 0, 0)).TotalSeconds;        
        private bool HasPermission(ulong playerId, string perm) => permission.UserHasPermission(playerId.ToString(), perm);

        private bool IsPrisoner(BasePlayer player) => prisoners.ContainsKey(player.userID);
        private bool HasEmptyCells(string prisonName) => prisons[prisonName].occupiedCells.Where(x => x.Value == false).Count() > 0;
        #endregion

        #region Config        
        private ConfigData configData;
        class ConfigData
        {
            public bool ReturnHomeAfterRelease { get; set; }
            public bool AllowBreakouts { get; set; }
            public bool AutoReleaseWhenExpired { get; set; }
            public bool GiveInmateKits { get; set; }
            public bool DisablePrisonerDamage { get; set; }
            public bool BroadcastImprisonment { get; set; }
            public bool BlockPublicAccessToPrisons { get; set; }
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
                AllowBreakouts = false,
                AutoReleaseWhenExpired = true,
                BlockPublicAccessToPrisons = true,
                BroadcastImprisonment = true,
                DisablePrisonerDamage = true,
                GiveInmateKits = true,
                ReturnHomeAfterRelease = true
            };
            SaveConfig(config);
        }
        private void LoadConfigVariables() => configData = Config.ReadObject<ConfigData>();
        void SaveConfig(ConfigData config) => Config.WriteObject(config, true);
        #endregion

        #region Data Management
        void SavePrisonData()
        {
            prisonData.prisons = prisons;
            prisondata.WriteObject(prisonData);
        }
        void SavePrisonerData()
        {
            prisonerData.prisoners = prisoners;
            prisonerdata.WriteObject(prisonerData);
        }
        void LoadData()
        {
            try
            {
                prisonData = prisondata.ReadObject<PrisonData>();
                prisons = prisonData.prisons;
            }
            catch
            {
                prisonData = new PrisonData();
            }
            try
            {
                prisonerData = prisonerdata.ReadObject<PrisonerData>();
                prisoners = prisonerData.prisoners;
            }
            catch
            {
                prisonerData = new PrisonerData();
            }
        }
        class PrisonData
        {
            public Dictionary<string, PrisonEntry> prisons = new Dictionary<string, PrisonEntry>();
        }
        class PrisonEntry
        {
            public string zoneId, spawnFile, inmateKit;
            public float x, y, z, radius;
            public Dictionary<int, bool> occupiedCells = new Dictionary<int, bool>();
        }
        class PrisonerData
        {
            public Dictionary<ulong, PrisonerEntry> prisoners = new Dictionary<ulong, PrisonerEntry>();
        }
        class PrisonerEntry
        {
            public float x, y, z;
            public string prisonName;
            public int cellNumber;
            public double releaseDate;
            public Dictionary<string, List<InvItem>> savedInventory = new Dictionary<string, List<InvItem>>();
        }
        public class InvItem
        {
            public int itemid;
            public ulong skin;
            public int amount;
            public float condition;
            public int ammo;
            public string ammotype;
            public ProtoBuf.Item.InstanceData instanceData;
            public InvItem[] contents;
        }
        #endregion

        #region Localization
        string msg(string key, ulong playerId) => lang.GetMessage(key, this, playerId.ToString());
        Dictionary<string, string> Messages = new Dictionary<string, string>
        {

        };
        #endregion
    }
}
