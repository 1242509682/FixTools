using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using TShockAPI;
using static FixTools.FixTools;
using static FixTools.Utils;

namespace FixTools;

internal class FixStartInvasion
{
    #region 修复使用物品召唤事件
    private static readonly object InvtLock = new(); // 入侵事件锁，确保同一时间只有一个入侵被触发
    public static void StartInvasion(TSPlayer plr)
    {
        if (!plr.TPlayer.controlUseItem) return;

        // 检查玩家选中的物品是否为入侵召唤物
        var sel = plr.SelectedItem;
        HashSet<int> itemType = GetEventItemType();

        // 检查权限
        if (!plr.HasPermission("tshock.npc.startinvasion") && itemType.Contains(sel.type))
        {
            plr.SendMessage(TextGradient("[{插件名}] 你没有权限使用召唤入侵物品[c/FF514A:{物品名}]！", plr), color);
            plr.SendMessage(TextGradient("请通知管理给予权限:\n" +
                                         "/group addperm default tshock.npc.startinvasion\n", plr), color);
            return;
        }

        int Invtype = -1;

        // 只处理特定的入侵物品
        switch (sel.type)
        {
            case ItemID.GoblinBattleStandard: // 哥布林入侵召唤物
                Invtype = 1;
                break;
            case ItemID.SnowGlobe: // 雪人军团召唤物
                Invtype = 2;
                break;
            case ItemID.PirateMap: // 海盗入侵召唤物
                Invtype = 3;
                break;
            case ItemID.TempleKey: // 石后神庙钥匙召唤火星暴乱
                if (Main.hardMode && NPC.downedGolemBoss && Config.MartianEvent)
                    Invtype = 4;
                break;
            default:
                return;
        }

        // 使用锁确保同一时间只有一个入侵被触发
        lock (InvtLock)
        {
            // 检查是否已有入侵在进行
            if (Main.invasionType != 0)
            {
                plr.SendMessage(TextGradient($"\n[{PluginName}]\n" +
                                             $"已有1个入侵事件[c/FF5C57:({GetInvasionName(Main.invasionType)})]进行中！\n" +
                                             $"使用指令[c/FF5C57:结束]入侵:/worldevent invasion"), color);
                return;
            }

            // 消耗物品
            if (!UseEventItem(plr, itemType))
                return;

            // 根据超过200血的玩家数，计算入侵规模
            int MaxLife200_Player = TShock.Players.Count(p => p != null && p.Active && p.TPlayer.statLifeMax >= 200);

            // 设置入侵参数
            switch (Invtype)
            {
                case 1: // 哥布林入侵
                case 2: // 雪人军团入侵
                    Main.invasionSize = 80 + 40 * MaxLife200_Player;
                    break;
                case 3: // 海盗入侵
                    Main.invasionSize = 120 + 60 * MaxLife200_Player;
                    break;
                case 4: // 火星人入侵
                    Main.invasionSize = 160 + 40 * MaxLife200_Player;
                    break;
            }

            Main.invasionSizeStart = Main.invasionSize; // 设置入侵初始规模
            Main.invasionType = Invtype; // 设置入侵类型

            // 设置入侵起始位置
            if (Invtype == 4) // 火星人特殊处理
                Main.invasionX = Main.spawnTileX - 1;
            else
                Main.invasionX = (Main.rand.Next(2) == 0) ? 0 : Main.maxTilesX;

            // 设置警告状态
            Main.invasionWarn = (Invtype == 4) ? 2 : 0;

            try
            {
                // 开始入侵
                Main.StartInvasion(Invtype);

                // 发送网络同步
                NetMessage.SendData(MessageID.WorldData);
                NetMessage.SendData(MessageID.InvasionProgressReport);

                // 发送全局通知
                TShock.Utils.Broadcast(TextGradient($"{plr.Name} 召唤了{GetInvasionName(Invtype)}入侵！"), color);
            }
            catch (Exception ex)
            {
                TShock.Log.ConsoleError($"召唤入侵时发生错误: {ex}");
                plr.SendMessage("召唤入侵失败，请联系管理员！", Color.Red);

                // 重置入侵状态
                Main.invasionType = 0;
                Main.invasionSize = 0;
                Main.invasionDelay = 0;
            }
        }
    }
    #endregion
}
