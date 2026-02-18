using System.Text;
using TShockAPI;
using static FixTools.FixTools;
using static FixTools.PlayerState;
using static FixTools.Utils;

namespace FixTools;

internal class BakCmd
{
    #region 回档投票数据类
    public class VoteData
    {
        public int ApplyIdx { get; set; } = 0;           // 申请的备份索引
        public DateTime ApplyTime { get; set; }          // 申请时间
        public int VoteYes { get; set; } = 0;            // 同意票数
        public int VoteNo { get; set; } = 0;             // 反对票数
        public List<string> Voted { get; set; } = new(); // 已投票玩家
    }
    #endregion

    #region 投票回档管理方法
    public static VoteData? curVote = null;
    public static string? curName = null;
    public static readonly string ApplyDir = Path.Combine(MainPath, "临时申请备份");
    public static bool HasApply() => curVote != null;
    public static VoteData? GetApply(string name) => GetData(name).MyApply;
    public static List<(string name, VoteData apply)> GetAllApplies()
    {
        var list = new List<(string, VoteData)>();
        if (curVote != null && curName != null)
        {
            list.Add((curName, curVote));
        }
        return list;
    }

    public static void ClearApply()
    {
        curVote = null;
        curName = null;
        hasApply = false;
    }
    #endregion

    #region 投票回档指令 /bak
    public static void bakCmd(CommandArgs args)
    {
        var plr = args.Player;

        // 无参数：显示状态
        if (args.Parameters.Count == 0)
        {
            ShowBakStatus(plr);
            return;
        }

        // 配置项未开启
        if (!Config.ApplyVote)
        {
            plr.SendMessage($"[{PluginName}] 投票回档未开启", color);
            return;
        }

        var param = args.Parameters[0].ToLower();

        // 申请回档：/sbk 数字
        if (int.TryParse(param, out int idx))
        {
            if (!plr.RealPlayer)
            {
                plr.SendMessage($"[{PluginName}] 请进入游戏后再使用本指令", color);
                return;
            }

            ApplyBak(plr, idx);
            return;
        }

        switch (param)
        {
            case "y":
            case "yes":
            case "同意":
                DoVote(plr, true);
                break;

            case "n":
            case "no":
            case "反对":
            case "不同意":
                DoVote(plr, false);
                break;

            default:
                plr.SendMessage($"申请自己回档: /{bak} 数字", color);
                plr.SendMessage($"投票他人回档: /{bak} y | n ", color);
                break;
        }
    }
    #endregion

    #region 显示备份和投票状态
    private static void ShowBakStatus(TSPlayer plr)
    {
        var list = GetBakList();
        if (list.Count == 0)
        {
            plr.SendMessage("暂无自动备份", color);
            return;
        }

        var sb = new StringBuilder();
        sb.AppendLine("当前备份:");
        foreach (var item in list)
            sb.AppendLine(item);

        if (curVote != null && curName != null)
        {
            var remain = Config.ApplyTime - (DateTime.Now - curVote.ApplyTime).TotalSeconds;
            sb.AppendLine($"\n申请: {curName} - 备份索引{curVote.ApplyIdx}");
            sb.AppendLine($"剩余: {Math.Max(0, (int)remain)}秒");
            sb.AppendLine($"同意:{curVote.VoteYes} 反对:{curVote.VoteNo}");
            sb.AppendLine($"投票: /{bak} y 或 n");
        }
        else
        {
            sb.AppendLine($"\n申请自己回档: /{bak} 备份索引");
        }

        if (plr.HasPermission(Prem))
            sb.AppendLine($"拥有 {pt}.use 权限可决定结果");

        if (plr.RealPlayer)
            plr.SendMessage(TextGradient(sb.ToString()), color);
        else
            plr.SendMessage(sb.ToString(), color);
    }
    #endregion

    #region 申请回档
    private static void ApplyBak(TSPlayer plr, int idx)
    {
        var files = GetBakFiles();
        if (idx < 1 || idx > files.Length)
        {
            plr.SendMessage($"备份索引不存在: {idx}", color);
            return;
        }

        var count = TShock.Utils.GetActivePlayerCount() -1;
        if (count < Config.MinVotePlayers)
        {
            plr.SendMessage($"投票人数不足: {count} < {Config.MinVotePlayers}人", color);
            return;
        }

        if (curVote != null)
        {
            var remain = Config.ApplyTime - (DateTime.Now - curVote.ApplyTime).TotalSeconds;
            plr.SendMessage($"{curName}正在申请回档...还剩{remain}秒", color);
            return;
        }

        curName = plr.Name;
        curVote = new VoteData
        {
            ApplyIdx = idx,
            ApplyTime = DateTime.Now,
            VoteYes = 0,
            VoteNo = 0,
            Voted = new List<string>()
        };

        hasApply = true;

        TShock.Utils.Broadcast($"\n{plr.Name}正在申请回档", color2);
        TShock.Utils.Broadcast($"{Config.ApplyTime}秒投票: /{bak} yes | no", color);
    }
    #endregion

    #region 处理投票
    private static void DoVote(TSPlayer plr, bool isYes)
    {
        if (curVote == null || curName == null)
        {
            plr.SendMessage("当前没有回档申请", color);
            return;
        }

        if (plr.Name == curName)
        {
            plr.SendMessage("不能给自己投票", color);
            return;
        }

        if (curVote.Voted.Contains(plr.Name))
        {
            plr.SendMessage("你已经投过票了", color);
            return;
        }

        // 有权限直接决定
        if (plr.HasPermission(Prem))
        {
            if (isYes)
            {
                DoApprove(plr);
            }
            else
            {
                ClearApply();
                SendResult(plr, false);
            }
            return;
        }

        // 普通玩家投票
        curVote.Voted.Add(plr.Name);
        if (isYes)
            curVote.VoteYes++;
        else
            curVote.VoteNo++;

        // 显示详细投票统计
        var stats = GetVoteStats();
        var total = curVote.VoteYes + curVote.VoteNo;
        TShock.Utils.Broadcast($"\n投票回档得到1票:{(isYes ? "同意" : "反对")}", color2);
        TShock.Utils.Broadcast($"{stats}", color);

        if (CheckVote())
            DoApprove(TSPlayer.Server);
    }
    #endregion

    #region 批准回档
    public static void DoApprove(TSPlayer plr)
    {
        try
        {
            var files = GetBakFiles();
            if (curVote == null || curName == null) return;

            if (curVote.ApplyIdx > files.Length)
            {
                plr.SendErrorMessage($"备份索引无效");
                return;
            }

            var zipFile = files[curVote.ApplyIdx - 1];

            // 保存当前存档
            SaveOld(plr, curName);

            // 恢复备份
            ExtractAndImport(plr, zipFile, curName);

            // 清理申请
            ClearApply();

            // 通知
            SendResult(plr, true);
        }
        catch (Exception ex)
        {
            plr.SendErrorMessage($"批准失败: {ex.Message}");
            TShock.Log.ConsoleError($"批准失败: {ex}");
        }
    }

    private static void SendResult(TSPlayer plr, bool yes)
    {
        var result = yes ? "批准" : "拒绝";
        var mess = $"{plr.Name} {result} {curName} 的申请回档";

        if (plr == TSPlayer.Server)
            plr.SendMessage(mess, color);

        TSPlayer.All.SendMessage(TextGradient(mess), color);
    }
    #endregion

    #region 保存当前存档
    private static void SaveOld(TSPlayer plr, string name)
    {
        if (!Directory.Exists(ApplyDir))
            Directory.CreateDirectory(ApplyDir);

        var plr2 = TShock.Players.FirstOrDefault(p => p?.Name == name);
        if (plr2 != null)
        {
            var plrFile = Path.Combine(ApplyDir, $"{name}_{DateTime.Now:yyyyMMddHHmmss}.plr");
            var etDir = Path.GetDirectoryName(plrFile);

            if (!File.Exists(etDir)) return;

            if (WritePlayer.Export(plr.TPlayer, etDir))
            {
                var edFile = Directory.GetFiles(etDir, "*.plr")
                    .FirstOrDefault(f => Path.GetFileName(f).StartsWith(plr.Name));

                if (edFile != null && edFile != plrFile)
                {
                    File.Move(edFile, plrFile, true);
                }
            }
        }
    }
    #endregion

    #region 检查投票条件
    public static bool CheckVote()
    {
        if (curVote == null) return false;

        // 获取在线玩家总数(排除申请人自己)
        var count = TShock.Utils.GetActivePlayerCount() - 1;

        // 计算投票统计
        var total = curVote.VoteYes + curVote.VoteNo;
        if (total == 0) return false;

        var rate = (float)curVote.VoteYes / total;

        // 1. 在线人数达到最小投票人数要求
        bool min = count >= Config.MinVotePlayers;

        // 2. 同意率达到通过率
        bool PassRate = rate >= Config.VotePassRate;

        // 条件3：考虑在线玩家基数，防止1人申请1人投票就通过
        // 至少需要有一半以上的在线玩家参与投票，或者投票人数已达到最小要求
        bool hasPart = total >= Math.Max(Config.MinVotePlayers, total / 2);

        return min && PassRate && hasPart;
    }

    // 获取投票统计信息（用于显示）
    private static string GetVoteStats()
    {
        if (curVote == null) return "无数据";

        // 获取在线玩家总数(排除申请人自己)
        var count = TShock.Utils.GetActivePlayerCount() -1;
        var total = curVote.VoteYes + curVote.VoteNo;
        var Rate = total > 0 ? (float)curVote.VoteYes / total * 100 : 0;

        return $"可投:{count} 已投:{total} \n同意:{curVote.VoteYes} 反对:{curVote.VoteNo} \n通过率:{Rate:F1}%";
    }
    #endregion

}