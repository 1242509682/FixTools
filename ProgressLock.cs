using TerrariaApi.Server;
using TShockAPI;
using static FixTools.FixTools;
using static FixTools.Utils;

namespace FixTools;

internal class ProgressLock
{
    #region 人数进度锁
    public static void OnNpcAIUpdate(NpcAiUpdateEventArgs args)
    {
        var npc = args.Npc;
        if (!Config.ProgressLock || npc is null || !npc.active ||
            Config.UnLockNpc.Contains(npc.FullName))
            return;

        // 人数足够 不阻止
        int PlayerCount = TShock.Utils.GetActivePlayerCount();
        if (PlayerCount >= Config.UnLockCount)
            return;

        if (Config.LockNpc.Contains(npc.FullName))
        {
            npc.active = false;
            npc.type = 0;
            npc.netUpdate = true;
            TShock.Utils.Broadcast($"在线人数不足:{PlayerCount}/{Config.UnLockCount}人," +
                                   $"禁止召唤:{npc.FullName}", color);

            TSPlayer.All.SendData(PacketTypes.NpcUpdate, "", npc.whoAmI);
            args.Handled = true;
        }
    }

    public static void OnNPCStrike(NpcStrikeEventArgs args)
    {
        var npc = args.Npc;
        if (!Config.ProgressLock || npc is null || !npc.active ||
            Config.UnLockNpc.Contains(npc.FullName))
            return;

        // 人数足够 不阻止
        int PlayerCount = TShock.Utils.GetActivePlayerCount();
        if (PlayerCount >= Config.UnLockCount) return;

        if (Config.LockNpc.Contains(npc.FullName))
        {
            npc.active = false;
            npc.type = 0;
            npc.netUpdate = true;
            TShock.Utils.Broadcast($"在线人数不足:{PlayerCount}/{Config.UnLockCount}人," +
                                   $"禁止召唤:{npc.FullName}", color);

            TSPlayer.All.SendData(PacketTypes.NpcUpdate, "", npc.whoAmI);
            args.Handled = true;
        }
    }

    public static void OnNPCKilled(NpcKilledEventArgs args)
    {
        var npc = args.npc;
        if (!Config.ProgressLock || npc is null || !npc.active ||
            Config.UnLockNpc.Contains(npc.FullName))
            return;

        // 人数不够不解锁
        int PlayerCount = TShock.Utils.GetActivePlayerCount();
        if (PlayerCount < Config.UnLockCount) return;

        // 杀过一次就解锁
        if (Config.LockNpc.Contains(npc.FullName))
        {
            Config.UnLockNpc.Add(npc.FullName);
            Config.Write();
        }
    }
    #endregion
}
