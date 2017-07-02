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
using Rust.Xp;

namespace Oxide.Plugins
{
    [Info("XP Level Scaler", "k1lly0u", "0.1.0", ResourceId = 0)]
    class XPLevelScaler : RustPlugin
    {
        #region Fields
        StoredData storedData;
        private DynamicConfigFile data;

        static FieldInfo Levels = typeof(Rust.Xp.Config).GetField("Levels", (BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance | BindingFlags.NonPublic));
        #endregion

        #region Oxide Hooks
        void Loaded()
        {
            data = Interface.Oxide.DataFileSystem.GetFile("xplevel_data");
        }
        void OnServerInitialized()
        {
            LoadData();
            if (storedData.XPAmounts.Count < 50)
            {
                int i = 1;
                int[] xpLevels = Rust.Xp.Config.Levels;
                foreach(var lvl in xpLevels)
                {
                    Puts(i + ". " + lvl);
                    storedData.XPAmounts.Add(i, lvl);
                    i++;
                }
                SaveData();
            }
            else
            {
                var newLevels = storedData.XPAmounts.Values.ToArray();                
                Levels.SetValue("Levels", newLevels);
            }
        }
        
        #endregion

        #region Functions
       
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
            public Dictionary<int, int> XPAmounts = new Dictionary<int, int>();
        }
        #endregion
    }
}
