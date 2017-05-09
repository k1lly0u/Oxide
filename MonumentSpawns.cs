using System;
using System.Collections.Generic;
using UnityEngine;
using System.Reflection;

namespace Oxide.Plugins
{
    [Info("MonumentSpawns", "k1lly0u", "0.1.0", ResourceId = 0)]
    class MonumentSpawns : RustPlugin
    {
        #region Fields
        private static readonly Collider[] colBuffer = (Collider[])typeof(Vis).GetField("colBuffer", (BindingFlags.Static | BindingFlags.NonPublic))?.GetValue(null);
        System.Random random = new System.Random();
        private List<Vector3> availableSpawnpoints;
        private bool Disabled;
        LayerMask layerMask;
        #endregion

        #region Oxide Hooks
        void Loaded()
        {
            availableSpawnpoints = new List<Vector3>();
        }
        void OnServerInitialized()
        {
            layerMask = (1 << 29);
            layerMask |= (1 << 18);
            layerMask |= (1 << 3);
            layerMask = ~layerMask;
            Disabled = true;
            LoadVariables();
            FindMonuments();
        }
        BasePlayer.SpawnPoint OnFindSpawnPoint()
        {
            if (Disabled) return null;
            var targetpos = GetSpawnPoint();
            BasePlayer.SpawnPoint spawnPoint1 = new BasePlayer.SpawnPoint();
            spawnPoint1.pos = targetpos;
            spawnPoint1.rot = new Quaternion(0f, 0f, 0f, 1f);            
            return spawnPoint1;
        }
        #endregion

        #region Functions        
        [ChatCommand("ms")]
        void cmdShowSpawns(BasePlayer player, string command, string[] args)
        {
            if (!player.IsAdmin()) return;
            SendReply(player, "Total spawn count: " + availableSpawnpoints.Count);
            foreach(var position in availableSpawnpoints)            
                player.SendConsoleCommand("ddraw.box", 30f, Color.magenta, position, 1f);
        }        
        private Vector3 GetSpawnPoint()
        {
            var targetpos = availableSpawnpoints[random.Next(0, availableSpawnpoints.Count - 1)];
            var entities = Physics.OverlapSphereNonAlloc(targetpos, configData.aBuildingDetectionRadius, colBuffer, LayerMask.GetMask("Construction"));
            if (entities > 0)
            {
                availableSpawnpoints.Remove(targetpos);
                if (availableSpawnpoints.Count < 10)
                {
                    PrintWarning("All current spawnpoints have been overrun by buildings and such. Disabling custom spawnpoints");
                    Disabled = true;
                }
                return GetSpawnPoint();
            }
            return targetpos;
        }
        private void FindMonuments()
        {
            PrintWarning("Finding All Monuments...");
            var allobjects = UnityEngine.Object.FindObjectsOfType<MonumentInfo>();
            foreach (var gobject in allobjects)
            {
                if (gobject.name.Contains("autospawn/monument"))
                {
                    var pos = gobject.transform.position;

                    if (gobject.name.Contains("lighthouse"))
                    {
                        if (configData.Lighthouse.Activated)
                        {
                            PrintWarning("Generating spawnpoints for Lighthouse");
                            GenerateSpawnpoints(pos, configData.Lighthouse.MaximumDistance, configData.Lighthouse.SpawnCount);
                        }
                        continue;
                    }

                    if (gobject.name.Contains("powerplant_1"))
                    {
                        if (configData.Powerplant.Activated)
                        {
                            PrintWarning("Generating spawnpoints for Powerplant");
                            GenerateSpawnpoints(pos, configData.Powerplant.MaximumDistance, configData.Powerplant.SpawnCount);
                        }
                        continue;
                    }

                    if (gobject.name.Contains("military_tunnel_1"))
                    {
                        if (configData.Tunnels.Activated)
                        {
                            PrintWarning("Generating spawnpoints for Military Tunnels");
                            GenerateSpawnpoints(pos, configData.Tunnels.MaximumDistance, configData.Tunnels.SpawnCount);
                        }
                        continue;
                    }

                    if (gobject.name.Contains("airfield_1"))
                    {
                        if (configData.Airfield.Activated)
                        {
                            PrintWarning("Generating spawnpoints for Airfield");
                            GenerateSpawnpoints(pos, configData.Airfield.MaximumDistance, configData.Airfield.SpawnCount);
                        }
                        continue;
                    }

                    if (gobject.name.Contains("trainyard_1"))
                    {
                        if (configData.Trainyard.Activated)
                        {
                            PrintWarning("Generating spawnpoints for Trainyard");
                            GenerateSpawnpoints(pos, configData.Trainyard.MaximumDistance, configData.Trainyard.SpawnCount);
                        }
                        continue;
                    }

                    if (gobject.name.Contains("water_treatment_plant_1"))
                    {
                        if (configData.WaterTreatment.Activated)
                        {
                            PrintWarning("Generating spawnpoints for Water Treatment");
                            GenerateSpawnpoints(pos, configData.WaterTreatment.MaximumDistance, configData.WaterTreatment.SpawnCount);
                        }
                        continue;
                    }

                    if (gobject.name.Contains("warehouse"))
                    {
                        if (configData.Warehouse.Activated)
                        {
                            PrintWarning("Generating spawnpoints for Warehouse");
                            GenerateSpawnpoints(pos, configData.Warehouse.MaximumDistance, configData.Warehouse.SpawnCount);
                        }
                        continue;
                    }

                    if (gobject.name.Contains("satellite_dish"))
                    {

                        if (configData.Satellite.Activated)
                        {
                            PrintWarning("Generating spawnpoints for Satellite");
                            GenerateSpawnpoints(pos, configData.Satellite.MaximumDistance, configData.Satellite.SpawnCount);
                        }
                        continue;
                    }

                    if (gobject.name.Contains("sphere_tank"))
                    {
                        if (configData.Dome.Activated)
                        {
                            PrintWarning("Generating spawnpoints for Dome");
                            GenerateSpawnpoints(pos, configData.Dome.MaximumDistance, configData.Dome.SpawnCount);
                        }
                        continue;
                    }
                    if (gobject.name.Contains("harbor_1"))
                    {
                        if (configData.Harbor_Large.Activated)
                        {
                            PrintWarning("Generating spawnpoints for Harbour (Large)");
                            GenerateSpawnpoints(pos, configData.Harbor_Large.MaximumDistance, configData.Harbor_Large.SpawnCount);
                        }
                        continue;
                    }
                    if (gobject.name.Contains("harbor_2"))
                    {
                        if (configData.Harbor_Small.Activated)
                        {
                            PrintWarning("Generating spawnpoints for Harbour (Small)");
                            GenerateSpawnpoints(pos, configData.Harbor_Small.MaximumDistance, configData.Harbor_Small.SpawnCount);
                        }
                        continue;
                    }
                    if (gobject.name.Contains("radtown_small_3"))
                    {
                        if (configData.Radtown.Activated)
                        {
                            PrintWarning("Generating spawnpoints for Radtown");
                            GenerateSpawnpoints(pos, configData.Radtown.MaximumDistance, configData.Radtown.SpawnCount);
                        }
                        continue;
                    }
                }
            }
            if (availableSpawnpoints.Count > 5) Disabled = false;
        }
        private void GenerateSpawnpoints(Vector3 position, float max, int count)
        {
            for (int i = 0; i < count; i++)
            {
                var spawnPoint = FindNewPosition(position, max);
                if (spawnPoint is Vector3)
                    availableSpawnpoints.Add((Vector3)spawnPoint);
            }          
        }        
        Vector3 CalculatePoint(Vector3 position, float max)
        {
            var angle = Math.PI * 2.0f * random.NextDouble();
            var radius = Math.Sqrt(random.NextDouble()) * max;
            var x = position.x + radius * Math.Cos(angle);
            var y = position.y + radius * Math.Sin(angle);
            return new Vector3((float)x, 300, (float)y);
        }         
        object FindNewPosition(Vector3 position, float max, bool failed = false) 
        {
            var targetPos = UnityEngine.Random.insideUnitCircle * max;
            var sourcePos = new Vector3(position.x + targetPos.x, 300, position.z + targetPos.y);
            var success = CastRay(sourcePos);
            if (success == null)
            {
                if (failed) return null;
                else return FindNewPosition(position, max, true);
            }
            else if (success is Vector3)
            {
                if (failed) return null;
                else return FindNewPosition(new Vector3(sourcePos.x, ((Vector3)success).y, sourcePos.y), max, true);
            }
            else
            {
                sourcePos.y = Mathf.Max((float)success, TerrainMeta.HeightMap.GetHeight(sourcePos));
                return sourcePos;
            }
        }
        private object CastRay(Vector3 sourcePos)
        {
            RaycastHit hitInfo;
            if (Physics.Raycast(new Ray(sourcePos, Vector3.down), out hitInfo, 300, layerMask))
            {
                if (hitInfo.collider?.gameObject.layer == UnityEngine.LayerMask.NameToLayer("Water"))
                    return null;
                if (hitInfo.collider?.gameObject.name == "Zone Manager")                
                    return hitInfo.collider.transform.position + new Vector3(0, -1, 0);                
                return hitInfo.point.y;
            }
            return null;
        }
        #endregion

        #region Helpers
        int GetRandom(int max, int min = 0) => UnityEngine.Random.Range(min, max);
        #endregion

        #region Config        
        private ConfigData configData;
        class MonumentSettings
        {
            public bool Activated;
            public float MaximumDistance;
            public int SpawnCount;
        }
        class ConfigData
        {
            public float aBuildingDetectionRadius { get; set; }
            public MonumentSettings Airfield { get; set; }
            public MonumentSettings Dome { get; set; }
            public MonumentSettings Lighthouse { get; set; }
            public MonumentSettings Powerplant { get; set; }
            public MonumentSettings Radtown { get; set; }
            public MonumentSettings Satellite { get; set; }
            public MonumentSettings Harbor_Small { get; set; }
            public MonumentSettings Harbor_Large { get; set; }
            public MonumentSettings Trainyard { get; set; }
            public MonumentSettings Tunnels { get; set; }
            public MonumentSettings Warehouse { get; set; }
            public MonumentSettings WaterTreatment { get; set; }
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
                aBuildingDetectionRadius = 10f,
                Airfield = new MonumentSettings
                {
                    Activated = true,
                    MaximumDistance = 250,
                    SpawnCount = 15
                },
                Dome = new MonumentSettings
                {
                    Activated = true,
                    MaximumDistance = 180,
                    SpawnCount = 10
                },
                Lighthouse = new MonumentSettings
                {
                    Activated = true,
                    MaximumDistance = 120,
                    SpawnCount = 10
                },
                Powerplant = new MonumentSettings
                {
                    Activated = true,
                    MaximumDistance = 250,
                    SpawnCount = 15
                },
                Radtown = new MonumentSettings
                {
                    Activated = true,
                    MaximumDistance = 180,
                    SpawnCount = 10
                },
                Satellite = new MonumentSettings
                {
                    Activated = true,
                    MaximumDistance = 180,
                    SpawnCount = 15
                },
                Harbor_Small = new MonumentSettings
                {
                    Activated = true,
                    MaximumDistance = 150,
                    SpawnCount = 15
                },
                Harbor_Large = new MonumentSettings
                {
                    Activated = true,
                    MaximumDistance = 200,
                    SpawnCount = 20
                },
                Trainyard = new MonumentSettings
                {
                    Activated = true,
                    MaximumDistance = 200,
                    SpawnCount = 15
                },
                Tunnels = new MonumentSettings
                {
                    Activated = true,
                    MaximumDistance = 200,
                    SpawnCount = 15
                },
                Warehouse = new MonumentSettings
                {
                    Activated = true,
                    MaximumDistance = 120,
                    SpawnCount = 10
                },
                WaterTreatment = new MonumentSettings
                {
                    Activated = true,
                    MaximumDistance = 200,
                    SpawnCount = 15
                }
            };
            SaveConfig(config);
        }
        private void LoadConfigVariables() => configData = Config.ReadObject<ConfigData>();
        void SaveConfig(ConfigData config) => Config.WriteObject(config, true);
        #endregion
    }
}
