using TShockAPI;
using Terraria;
using Terraria.DataStructures;
using Microsoft.Xna.Framework;
using static FixTools.FixTools;
using static FixTools.PlayerState;
using static FixTools.Utils;

namespace FixTools;

internal class DeadLimit
{
    #region 死亡事件触发，修复重生时间
    public static HashSet<TSPlayer> DeadPlayer = new(); // 缓存死亡玩家
    public static void FixRespawnTimer(TSPlayer plr, MyData data)
    {
        // 如果玩家已经在死亡倒计时中，忽略本次死亡（避免重置计时器）
        if (data.DeadTime != null) return;

        // 先更新TShock自己的配置文件,确保复活时间不为0
        FixTShockConfig();

        var RespawnTimer = Main.npc.Any(npc => npc.boss && npc.active)
            ? TShock.Config.Settings.RespawnBossSeconds
            : TShock.Config.Settings.RespawnSeconds;

        // 设置复活时间
        plr.RespawnTimer = RespawnTimer;

        // 保存当前死亡坐标
        if (Config.Back)
        {
            data.BackPos.Add(new Vector2(plr.X, plr.Y));
            plr.SendMessage(TextGradient($"发送指令 [c/FF6962:/back] 返回死亡地点(已有{data.BackPos.Count}个)"), color);
        }

        // 只在服务器复活时间，超过原版复活时间时触发
        if (plr.RespawnTimer >= 15)
        {
            // 如果开启限制死亡进服，玩家处于死亡状态,设置离开时间:为现在+剩余复活秒数
            data.DeadTime = DateTime.Now.AddSeconds(plr.RespawnTimer);
            // 加入重生读秒遍历检查
            DeadPlayer.Add(plr);
        }
    }
    #endregion

    #region 先更新TShock自己的配置文件,确保复活时间不为0
    private static void FixTShockConfig()
    {
        var write = false;
        if (TShock.Config.Settings.RespawnBossSeconds == 0)
        {
            TShock.Config.Settings.RespawnBossSeconds = 30;
            write = true;
        }

        if (TShock.Config.Settings.RespawnSeconds == 0)
        {
            TShock.Config.Settings.RespawnSeconds = 15;
            write = true;
        }

        if (write)
        {
            Task.Run(() =>
            {
                TShock.Config.Write(Path.Combine(TShock.SavePath, "config.json"));
            });
        }
    }
    #endregion

    #region 玩家进服事件，限制进服
    public static void LimitJoin(TSPlayer plr, MyData data)
    {
        if (!data.DeadTime.HasValue) return;

        DeadPlayer.Remove(plr);

        if (!Config.DeathLimitForJoin) return;

        var respawn = (data.DeadTime.Value - DateTime.Now).TotalSeconds;
        if (respawn > 0)
        {
            plr.Disconnect($"您在死亡时退出服务器！进服需等待{respawn:0}秒");
            TShock.Log.ConsoleInfo($"[{PluginName}] {plr.Name}:在死亡时退出服务器！进服需等待{respawn:0}秒");
        }
        else
        {
            // 清除过期死亡标记
            data.DeadTime = null;
        }
    }
    #endregion

    #region 游戏更新事件触发，定时杀死玩家
    private static long Frame = 0;  // 杀死玩家计时器
    public static void DeadTimeUpdate()
    {
        if (DeadPlayer.Count < 0 || ++Frame % 60 != 0) return;
        Frame = 0;

        foreach (var plr in DeadPlayer)
        {
            var data = GetData(plr.Name);
            if (data.DeadTime == null)
            {
                DeadPlayer.Remove(plr);
                continue;
            }

            double remaining = (data.DeadTime.Value - DateTime.Now).TotalSeconds;
            if (remaining > 0)
            {
                // 每14秒发包一次杀死玩家
                if (Frame % 14 == 0)
                {
                    var deathReason = PlayerDeathReason.ByOther(255);
                    NetMessage.SendPlayerDeath(plr.Index, deathReason, 99999, 0, false, -1, -1);
                }
                plr.SendMessage(TextGradient($"您复活需等待 {remaining:0} 秒"), color);
            }
            else
            {
                // 触发复活
                plr.RespawnTimer = 0;
                plr.Spawn(PlayerSpawnContext.ReviveFromDeath);
                data.DeadTime = null;
                DeadPlayer.Remove(plr);
            }
        }
    }
    #endregion

    #region 发送消息事件,回到死亡位置
    public static void Back(CommandArgs args)
    {
        var plr = args.Player;

        if (!Config.Back)
        {
            var state = Config.Back ? "开启" : "关闭";
            plr.SendMessage(TextGradient($"回到死亡地点功能已:{state}"), color);
            return;
        }

        if (!plr.RealPlayer)
        {
            plr.SendMessage("请进入游戏再使用本指令", color);
            return;
        }

        var data = GetData(plr.Name);
        if (plr.Dead && data.DeadTime.HasValue)
        {
            plr.SendMessage(TextGradient("您正处于重生读秒,无法使用[c/FF5149:死亡位置传送]"), color);
            return;
        }

        // 检查玩家是否有位置列表
        if (data.BackPos.Count == 0)
        {
            plr.SendMessage(TextGradient("当前[c/FF5149:没有可用的]死亡位置!"), color);
            return;
        }

        // 获取并移除列表的最后一个元素（最近的位置）
        int lastIndex = data.BackPos.Count - 1;
        Vector2 backPos = data.BackPos[lastIndex];
        plr.Teleport(backPos.X, backPos.Y);
        data.BackPos.RemoveAt(lastIndex);

        // 显示剩余位置数量
        var mess = data.BackPos.Count > 0 ? $"还有 [c/3FAEDB:{data.BackPos.Count}] 个死亡位置可用" :
                                            $"这是 [c/FF534A:最后一个] 死亡位置";

        plr.SendMessage(TextGradient(mess), color);
    }
    #endregion
}
