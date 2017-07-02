using System.Collections.Generic;
using Rust.Xp;

namespace Oxide.Plugins
{
    [Info("XPReset", "k1lly0u", "0.1.0", ResourceId = 0)]
    class XPReset : RustPlugin
    {
        private BasePlayer FindPlayer(BasePlayer player, string arg)
        {
            var foundPlayers = new List<BasePlayer>();
            ulong steamid;
            ulong.TryParse(arg, out steamid);
            string lowerarg = arg.ToLower();

            foreach (var p in BasePlayer.activePlayerList)
            {
                if (p != null)
                {
                    if (steamid != 0L)
                        if (p.userID == steamid) return p;
                    string lowername = p.displayName.ToLower();
                    if (lowername.Contains(lowerarg))
                    {
                        foundPlayers.Add(p);
                    }
                }
            }
            if (foundPlayers.Count == 0)
            {
                foreach (var sleeper in BasePlayer.sleepingPlayerList)
                {
                    if (sleeper != null)
                    {
                        if (steamid != 0L)
                            if (sleeper.userID == steamid)
                            {
                                foundPlayers.Clear();
                                foundPlayers.Add(sleeper);
                                return foundPlayers[0];
                            }
                        string lowername = player.displayName.ToLower();
                        if (lowername.Contains(lowerarg))
                        {
                            foundPlayers.Add(sleeper);
                        }
                    }
                }
            }
            if (foundPlayers.Count == 0)
            {
                if (player != null)
                    SendReply(player, "No players found");
                return null;
            }
            if (foundPlayers.Count > 1)
            {
                if (player != null)
                    SendReply(player, "Multiple players found");
                return null;
            }
            return foundPlayers[0];
        }


        #region Chat Commands
        [ChatCommand("resetxp")]
        private void cmdXPE(BasePlayer player, string command, string[] args)
        {
            if (!player.IsAdmin()) return;
            if (args == null || args.Length == 0)
            {
                SendReply(player, "/resetxp <playername> - Reset <playernames> XP to 0");               
            }          
            if (args.Length == 1)
            {
                var target = FindPlayer(player, args[0]);
                if (target != null)
                {
                    target.xp.Reset();
                    SendReply(player, $"You have reset {target.displayName}'s XP");
                    SendReply(target, $"Your XP has been reset by a admin");
                }
            }
        }
        #endregion
    }
}
