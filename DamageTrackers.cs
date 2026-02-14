using System.Text;
using Terraria;
using TShockAPI;
using Terraria.GameContent;
using static FixTools.FixTools;
using static FixTools.Utils;

namespace FixTools;

internal class DamageTrackers
{
    #region Boss伤害排行事件
    public static void OnBossKilled(On.Terraria.GameContent.BossDamageTracker.orig_OnBossKilled orig, BossDamageTracker self, NPC npc)
    {
        orig(self, npc); // 调用原版方法

        // 没开启伤害排名则不播报
        if (!Config.NPCDamageTracker) return;

        // 获取CreditList列表
        var CreditList = self._list;
        if (CreditList == null || CreditList.Count == 0)
            return;

        // 提取玩家条目
        var pList = new List<NPCDamageTracker.PlayerCreditEntry>();
        foreach (var entry in CreditList)
        {
            if (entry is not NPCDamageTracker.PlayerCreditEntry p) continue;
            pList.Add(p);
        }

        // 直接获取世界伤害和名称
        int worldDmg = self._worldCredit?.Damage ?? 0;
        string? worldName = self._worldCredit?.Name.ToString();

        // 遍历所有在线玩家
        var onPlrs = TShock.Players.Where(p => p != null && p.Active && p.RealPlayer).ToList();

        // 参战玩家名集合
        var fightSet = new HashSet<string>(pList.Select(p => p.PlayerName), StringComparer.OrdinalIgnoreCase);
        int plrDmg = pList.Sum(p => p.Damage);  // 统计所有玩家伤害
        int totalDmg = plrDmg + worldDmg; // 总伤害 = 玩家 + 环境伤害

        // 未参战玩家列表
        var idlePlrs = onPlrs.Where(p => !fightSet.Contains(p.Name)).ToList();
        string idleCnt2 = idlePlrs.Count > 0 ? $"/[c/61BFE2:{onPlrs.Count}]" : "";
        string idleStr = idlePlrs.Count > 0 ? string.Join(", ", idlePlrs.Select(p => p.Name)) : "";

        // 战斗用时（秒）
        double secs = self.Duration / 60.0;
        int totalSec = (int)secs;
        string timeStr = GetTimeString(totalSec);

        // 计算每个玩家的 DPS，并找出 MVP（DPS 最高者）
        var mvpPlayer = pList.OrderByDescending(p => secs > 0 ? p.Damage / secs : 0).FirstOrDefault();

        // 获取最后一击信息
        string? lastName = null;
        bool lastIsW = false;
        if (self._lastAttacker is NPCDamageTracker.PlayerCreditEntry lastP)
        {
            lastName = lastP.PlayerName;
        }
        else if (self._lastAttacker is NPCDamageTracker.WorldCreditEntry)
        {
            lastIsW = true;
            lastName = self._worldCredit?.Name.ToString();
        }

        // 构建排名列表（玩家 + 减益）
        var rank = new List<(string name, int dmg, bool isP)>();
        foreach (var p in pList)
            rank.Add((p.PlayerName, p.Damage, true));
        if (worldDmg > 0 && worldName != null)
            rank.Add((worldName, worldDmg, false));
        rank = rank.OrderByDescending(x => x.dmg).ToList();

        // 使用原版方法计算百分比
        int[] dmgArray = rank.Select(x => x.dmg).ToArray();
        int[] percents = NPCDamageTracker.CalculatePercentages(dmgArray); // 顺序与 rank 一致

        // 获取队伍颜色值给有队伍的玩家名称单独上色
        var teamMap = onPlrs.ToDictionary(
            p => p.Name,
            p => GetTeamColor(p.Team),
            StringComparer.OrdinalIgnoreCase);

        var lifeMax = npc.lifeMax; // 最大生命
        var damage = npc.damage; // 伤害
        var defense = npc.defense; // 防御
        var hatred = pList.Count > 1 ? "仇恨: " + Main.player[npc.target].name : string.Empty;

        // 构建消息
        var sb = new StringBuilder();
        sb.AppendLine("      [i:3455][c/AD89D5:伤][c/D68ACA:害][c/DF909A:排][c/E5A894:行][c/E5BE94:榜][i:3454]");
        sb.AppendLine($"{self.Name}" + $" 参战 [c/FF726E:{pList.Count}]{idleCnt2}位 " + $"{timeStr} " + hatred);
        sb.AppendLine($"生命: [c/FFA96D:{lifeMax}] 攻击: [c/FFE36D:{damage}] 防御: [c/EA64AC:{defense}]");
        sb.AppendLine($"总伤: [c/FF726E:{totalDmg}] 玩家: [c/FCFE6D:{plrDmg}] 减益: [c/61BFE2:{worldDmg}]");

        // 输出排名（玩家和减益混合排行）
        int idx = 1;
        for (int i = 0; i < rank.Count; i++)
        {
            var r = rank[i];
            int perc = percents[i]; // 使用预计算的百分比
            string line = GetLine(pList, secs, mvpPlayer, lastName, lastIsW, teamMap, idx, r, perc);
            sb.AppendLine(line);
            idx++;
        }

        // 未参战玩家
        if (!string.IsNullOrEmpty(idleStr))
            sb.AppendLine($"未参战: {idleStr}");

        // 发送给所有在线玩家
        foreach (var plr in onPlrs)
        {
            plr.SendMessage(TextGradient(sb.ToString()), color);
        }


        // 宝藏袋提示
        if (Config.TpBagEnabled &&
            Config.AllowTpBagText != null &&
            Config.AllowTpBagText.Count > 0)
            TSPlayer.All.SendMessage(TextGradient($"发送消息 [c/FF6962:{string.Join(" 或 ", Config.AllowTpBagText)}] 将传送到宝藏袋位置 "), color);
    }
    #endregion

    #region 队伍颜色映射
    // 根据队伍ID获取颜色代码，若无则返回null
    public static string GetTeamColor(int teamId) => TeamColorMap2.TryGetValue(teamId, out var color) ? color : null!;
    // 队伍ID到颜色代码的映射（仅颜色值，不含格式）
    public static readonly Dictionary<int, string> TeamColorMap2 = new()
    {
        { 0, "5ADECE"},  // 白队
        { 1, "FF716D" }, // 红队
        { 2, "61E26B" }, // 绿队
        { 3, "61BFE2" }, // 蓝队
        { 4, "FCFE6D" }, // 黄队
        { 5, "E15BC2" }  // 粉队
    };
    #endregion

    #region 获取格式化的战斗时间
    private static string GetTimeString(int totalSec)
    {
        if (totalSec < 1) return "1秒";
        if (totalSec < 60) return $"{totalSec}秒";
        if (totalSec < 3600) return $"{totalSec / 60}分{totalSec % 60:D2}秒";
        return $"{totalSec / 3600}时{(totalSec % 3600) / 60}分{totalSec % 60:D2}秒";
    }
    #endregion

    #region 获取排名方法
    private static string GetLine(List<NPCDamageTracker.PlayerCreditEntry> pList, double secs, NPCDamageTracker.PlayerCreditEntry? mvpPlayer, string? lastName, bool lastIsW, Dictionary<string, string> teamMap, int idx, (string name, int dmg, bool isP) r, int perc)
    {
        string line;
        if (r.isP)
        {
            // 玩家行：计算 DPS，并判断 MVP 和最后一击
            int dps = secs > 0 ? (int)(r.dmg / secs) : 0;

            // 玩家名字对应队伍颜色
            string displayName = r.name;
            if (teamMap.TryGetValue(r.name, out string? color))
                displayName = $"[c/{color}:{r.name}]";

            // 玩家
            line = $"{idx}.{displayName} {perc}% 伤害{r.dmg} 秒伤[c/FF706D:{dps}]";

            // 只在超过1人时才添加标记
            if (pList.Count >= 2)
            {
                // MVP 标记（基于 DPS）
                if (mvpPlayer != null && r.name.Equals(mvpPlayer.PlayerName, StringComparison.OrdinalIgnoreCase))
                {
                    line += " [c/FCFE6D:<mvp>]";
                }
                // 如果不是MVP，才考虑最后一击标记
                else if (!lastIsW && lastName != null && r.name.Equals(lastName, StringComparison.OrdinalIgnoreCase))
                {
                    line += " [c/FF726E:<end>]";
                }
            }
        }
        else
        {
            // 陷阱与减益
            line = $"{idx}.{r.name} {perc}% 伤害{r.dmg}";

            // 环境最后一击标记
            if (lastIsW && lastName != null && r.name == lastName)
            {
                line += " [c/FF726E:<end>]";
            }
        }

        return line;
    }
    #endregion
}