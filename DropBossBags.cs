using Microsoft.Xna.Framework;
using Terraria;
using TerrariaApi.Server;
using TShockAPI;
using static FixTools.FixTools;
using static FixTools.PlayerState;
using static FixTools.Utils;

namespace FixTools;

internal class DropBossBags
{
    #region 宝藏袋掉落事件
    public static void OnDropBossBag(DropBossBagEventArgs args)
    {
        if (!Config.TpBagEnabled) return;
        var npc = Main.npc[args.NpcArrayIndex];

        // 计算宝藏袋位置（Boss死亡位置）
        Vector2 bagPos = new Vector2(args.Position.X, args.Position.Y);
        var plrs = TShock.Players.Where(p => p != null && p.RealPlayer && p.Active && p.IsLoggedIn).ToList();
        foreach (var plr in plrs)
        {
            // 将新的宝藏袋位置添加到列表末尾（最近的在最后）
            var data = GetData(plr.Name);

            if (!data.BagPos.Contains(bagPos))
                data.BagPos.Add(bagPos);

            // 限制列表大小，只保留最近10个
            if (data.BagPos.Count > 10)
                data.BagPos.RemoveAt(0);// 移除最早的元素（索引0）

            // 处理死亡玩家复活
            if (plr.Dead)
            {
                plr.RespawnTimer = 0; // 立即复活
                plr.Spawn(PlayerSpawnContext.ReviveFromDeath); // 触发复活
                plr.SendMessage(TextGradient($"因击败[c/FF5149:{npc.FullName}]正为你自动复活!"), color);
            }
        }
    }

    // 处理宝藏袋传送
    public static void TpBag(string text, TSPlayer plr)
    {
        if (!Config.TpBagEnabled)
        {
            plr.SendMessage(TextGradient("宝藏袋传送功能未启用!"), color);
            return;
        }

        var data = GetData(plr.Name);

        // 检查玩家是否有位置列表
        if (data.BagPos.Count == 0)
        {
            plr.SendMessage(TextGradient("当前[c/FF5149:没有可用的]宝藏袋位置!"), color);
            return;
        }

        // 获取并移除列表的最后一个元素（最近的位置）
        int lastIndex = data.BagPos.Count - 1;
        Vector2 bagPos = data.BagPos[lastIndex];
        plr.Teleport(bagPos.X, bagPos.Y);
        data.BagPos.RemoveAt(lastIndex);

        // 显示剩余位置数量
        var mess = data.BagPos.Count > 0 ? $"[{PluginName}] 还有 [c/3FAEDB:{data.BagPos.Count}] 个宝藏袋位置可用" :
                                           $"[{PluginName}] 这是 [c/FF534A:最后一个] 宝藏袋位置";

        plr.SendMessage(TextGradient(mess), color);
    }
    #endregion
}
