using System.Collections.Concurrent;
using System.Text;
using Microsoft.Xna.Framework;
using TShockAPI;
using static FixTools.FixTools;
using static FixTools.PlayerState;
using static FixTools.Utils;
using static FixTools.PoutCmd;
using FixTools;

namespace DeathEvent;

internal static class TeamData
{
    #region 投票数据结构
    public static ConcurrentDictionary<string, TeamVote> VoteData = new ConcurrentDictionary<string, TeamVote>();
    public static ConcurrentDictionary<int, Point> SpawnPoint = new ConcurrentDictionary<int, Point>();
    public static ConcurrentQueue<TSPlayer> needTP = new();
    public enum VoteType
    {
        Join,      // 加入队伍
        SetSpawn   // 设置出生点
    }
    public class TeamVote
    {
        public string AppName { get; set; } = "";      // 申请人
        public int Team { get; set; } = -1;            // 目标队伍
        public DateTime Start { get; set; }            // 开始时间
        public int Time { get; set; } = 30;           // 投票时间(秒)
        public HashSet<string> Agree { get; set; } = new();   // 同意名单
        public HashSet<string> Against { get; set; } = new(); // 拒绝名单
        public VoteType Type { get; set; } = VoteType.Join;  // 投票类型

        // 队伍成员总数(排除申请人) - 实时计算在线玩家
        public int Total => GetPlayers().Count;
        public int Remain => Time - (int)(DateTime.Now - Start).TotalSeconds;   // 剩余时间
        public bool IsEnd => Remain <= 0; // 是否结束
        public string Key => $"{AppName}|{Team}";   // 获取投票键

        // 获取队伍玩家列表（排除申请人）
        public List<TSPlayer> GetPlayers()
        {
            var plrs = new List<TSPlayer>();
            for (int i = 0; i < TShock.Players.Length; i++)
            {
                var p = TShock.Players[i];
                if (p != null && p.Active && p.Team == Team && p.Name != AppName)
                {
                    plrs.Add(p);
                }
            }
            return plrs;
        }

        // 获取投票统计信息
        public VoteStats GetStats()
        {
            var players = GetPlayers();
            int total = players.Count;
            int agree = Agree.Count;
            int against = Against.Count;
            double agreeRate = total > 0 ? (double)agree / total * 100 : 0;

            return new VoteStats
            {
                Total = total,
                Agree = agree,
                Against = against,
                AgreeRate = agreeRate,
                Players = players
            };
        }
    }

    public class VoteStats
    {
        public int Total { get; set; }
        public int Agree { get; set; }
        public int Against { get; set; }
        public double AgreeRate { get; set; }
        public List<TSPlayer> Players { get; set; } = new();
    }
    #endregion

    #region 数据管理方法
    // 获取投票数据
    public static TeamVote? Get(string appName, int team) => VoteData.TryGetValue($"{appName}|{team}", out var vote) ? vote : null;
    // 检查玩家是否有未结束的投票申请
    public static bool HasPending(string appName) => VoteData.Values.Any(v => v.AppName == appName && !v.IsEnd);
    // 检查队伍是否有未结束的投票
    public static bool HasTeamVote(int team) => VoteData.Values.Any(v => v.Team == team && !v.IsEnd);
    // 添加投票
    public static bool Add(TeamVote vote) => VoteData.TryAdd(vote.Key, vote);
    // 移除投票
    public static void Remove(string appName, int team) => VoteData.TryRemove($"{appName}|{team}", out _);
    // 从队伍名字获取队伍id
    public static int GetTeamId(string teamName) => TeamColorMap.FirstOrDefault(x => x.Value == teamName).Key;
    #endregion

    #region 检查并处理超时投票
    public static void CheckTimeout()
    {
        // 使用列表记录需要处理的超时投票
        var ToProcess = new List<TeamVote>();

        // 一次遍历收集超时投票
        foreach (var vote in VoteData.Values)
        {
            if (!vote.IsEnd) continue;
            ToProcess.Add(vote);
        }

        // 批量处理超时投票
        foreach (var vote in ToProcess)
        {
            ProcessResult(vote);
        }
    }
    #endregion

    #region 玩家生成事件 恢复玩家队伍
    public static void TeamSpawn(GetDataHandlers.SpawnEventArgs e, TSPlayer plr, MyData data)
    {
        // 如果是刚进世界
        if (e.SpawnContext == Terraria.PlayerSpawnContext.SpawningIntoWorld)
        {
            // 如果开启队伍申请模式,队伍不为白队,当前队伍和数据内不符
            if (Config.TeamMode && data.Team > 0 && plr.Team != data.Team)
                // 避免和TShock的传送冲突,不能现在就传
                // 更新玩家加入时间,传给"玩家更新事件"恢复队伍和传回出生点
                data.JoinTime = DateTime.Now;
        }
        else if (Config.TeamSpawn && SpawnPoint.TryGetValue(plr.Team, out Point spawn))
        {
            data.NeedTp = spawn; // 不是刚进服,则把队伍出生点传到玩家数据里
            needTP.Enqueue(plr); // 把玩家加入传送队列,在游戏更新事件里执行传送
        }
    }

    // 玩家更新事件，进服恢复队伍,如果队伍有出生点1秒后传回
    public static void IsJoinBackTeam(TSPlayer plr, MyData data)
    {
        // 如果不是刚加入则返回
        if (!data.JoinTime.HasValue) return;

        // 还原队伍
        plr.SetTeam(data.Team);

        // 如果队伍出生点功能关闭，直接清除标记并返回
        // 如果玩家队伍没有设置出生点,也返回
        if (!Config.TeamSpawn || !SpawnPoint.ContainsKey(plr.Team))
        {
            data.JoinTime = null;
            return;
        }

        // 有出生点,则播报剩余多久传回
        var isJoin = DateTime.Now - data.JoinTime.Value;
        var rening = 1 + isJoin.TotalSeconds;
        plr.SendMessage(TextGradient($"回到队伍出生点剩余:{rening:F1}秒"), color);

        // 加入到现在超过1秒 立即传回
        if (isJoin.TotalSeconds >= 1)
        {
            if (SpawnPoint.TryGetValue(plr.Team, out Point spawn))
            {
                // 如果玩家队伍设置有出生点 则加入传送队列
                data.NeedTp = spawn;
                needTP.Enqueue(plr);
            }
        }
    }

    // 队伍出生点传送 游戏更新事件触发
    public static void BackTeamSpawn()
    {
        while (needTP.TryDequeue(out var plr))
        {
            // 跳过无效的玩家
            if (plr is null || !plr.Active) continue;

            var data = GetData(plr.Name);
            if (data is null || !data.NeedTp.HasValue) continue;

            // 检查是否刚加入服务器
            if (data.JoinTime.HasValue && DateTime.Now < data.JoinTime.Value)
            {
                // 还没到时间，重新入队稍后再试
                needTP.Enqueue(plr);
                continue;
            }

            // 传送回队伍出生点
            var spawn = data.NeedTp.Value;
            plr.Teleport(spawn.X * 16, spawn.Y * 16);
            plr.SendMessage(TextGradient($"已传回{GetTeamCName(plr.Team)}出生点"), color);

            data.NeedTp = null;
            data.JoinTime = null;
        }
    }
    #endregion

    #region 玩家死亡事件,队伍物品惩罚
    public static void TeamKillMe(GetDataHandlers.KillMeEventArgs e, TSPlayer plr, MyData data)
    {
        int team = plr.Team;
        if (team <= 0) return;
        var teamName = GetTeamCName(plr.Team);

        var other = new HashSet<int>(); // 记录其他队伍玩家以便激励
        var MyTeam = new List<TSPlayer>(); // 死亡同队伍玩家

        // 遍历所有在线玩家
        for (int i = 0; i < TShock.Players.Length; i++)
        {
            var p = TShock.Players[i];
            if (p == null || !p.RealPlayer || !p.Active) continue;

            // 处理同队伍玩家
            if (p.Team == team)
            {
                // 记录同队伍玩家,作为物品惩罚
                if (Config.TeamItemPun)
                    MyTeam.Add(p);

                // 只在 队员死亡集体团灭 开启时杀死同队伍玩家
                // 排除自己与管理员
                if (Config.TeamDeathPun && p != plr && !p.Dead && !p.HasPermission(Prem))
                {
                    p.KillPlayer();
                    TSPlayer.All.SendData(PacketTypes.DeadPlayer, "", p.Index);
                }
            }
            else if (Config.TeamItemPun && !other.Contains(p.Index))
            {
                other.Add(p.Index);  // 添加其他队伍玩家作为随机奖励对象
            }
        }

        if (Config.TeamDeathPun && MyTeam.Count > 0)
        {
            TSPlayer.All.SendMessage(TextGradient($"正在执行{teamName}团灭惩罚!"), color);
        }

        // 从本队成员扣除一件物品送给其他队伍的惩罚功能
        if (Config.TeamItemPun && other.Count > 0 && MyTeam.Contains(plr))
        {
            Rewards.TeamItemPun(plr, other, MyTeam);
        }
    }
    #endregion

    #region 队伍更新事件
    public static void OnPlayerTeam(object? sender, GetDataHandlers.PlayerTeamEventArgs e)
    {
        var plr = e.Player;
        if (plr == null || !plr.RealPlayer || !Config.TeamMode) return;

        int oldTeam = plr.Team;
        int newTeam = e.Team;

        if (oldTeam == newTeam) return;

        // 获取玩家缓存数据
        var data = GetData(plr.Name);

        // 检查队伍锁定
        if (data.Lock)
        {
            plr.SendMessage(TextGradient($"你的队伍已锁定为{GetTeamCName(data.Team)},无法切换"), Color.Red);
            e.Handled = true;
            plr.SetTeam(oldTeam);
            return;
        }

        // 检查切换队伍冷却
        if (CheckSwitchCD(plr, data, newTeam))
        {
            TimeSpan timeSpan = DateTime.Now - data.SwitchTime!.Value;
            double remain = Config.SwitchTeamCD - timeSpan.TotalSeconds;
            plr.SendMessage(TextGradient($"队伍切换冷却中，请等待:[c/508DC8:{remain:f2}]秒"), color);
            e.Handled = true;
            plr.SetTeam(oldTeam); // 强制还原队伍
            return;
        }

        // 如果队伍申请功能已经处理了，就直接返回，不执行后续的缓存更新
        if (TeamApply(e, plr, oldTeam, newTeam)) return;

        // 切换队伍
        SwitchTeam(plr, newTeam);
    }
    #endregion

    #region 检查切换队伍冷却
    private static bool CheckSwitchCD(TSPlayer plr, MyData data, int newTeam)
    {
        if (plr.HasPermission(Prem) ||
            !data.SwitchTime.HasValue)
            return false;

        TimeSpan timeSpan = DateTime.Now - data.SwitchTime.Value;
        if (timeSpan.TotalSeconds >= Config.SwitchTeamCD) return false;

        return true;
    }
    #endregion

    #region 检查投票结果
    private static void CheckResult(TeamVote vote)
    {
        var stats = vote.GetStats();
        int voted = stats.Agree + stats.Against;
        string typeDesc = vote.Type == VoteType.Join ? "申请加入" : "申请修改出生点";

        // 如果已经投票的人数达到当前在线成员数，立即结束投票
        if (voted >= stats.Total && stats.Total > 0)
        {
            ProcessResult(vote); // 处理投票结果
        }
        // 仍有未投票成员
        else if (voted < stats.Total)
        {
            var teamName = GetTeamCName(vote.Team);
            string msg = $"\n{teamName}投票: [c/508DC8:{vote.AppName}]{typeDesc}\n" +
                         $"同意:[c/32CD32:{stats.Agree}/{stats.Total}], 拒绝:[c/FF4500:{stats.Against}/{stats.Total}]\n" +
                         $"同意率:[c/FFD700:{stats.AgreeRate:F1}%], 剩余:[c/00CED1:{vote.Remain}秒]\n" +
                         "使用 /tv [c/5A9CDE:y]同意, [c/F4636F:n]拒绝, [c/5ADED3:v]查看详情\n";

            int remaining = stats.Total - voted;
            if (remaining > 0)
                msg += $"还需投票: [c/FCF567:{remaining}]人";

            // 发送给目标队伍成员
            foreach (var p in stats.Players)
            {
                if (p != null && p.Active)
                {
                    p.SendMessage(TextGradient(msg), color);
                }
            }
        }
    }
    #endregion

    #region 处理投票结果（核心逻辑）
    private static void ProcessResult(TeamVote vote)
    {
        var stats = vote.GetStats();
        var teamName = GetTeamCName(vote.Team);
        string typeDesc = vote.Type == VoteType.Join ? "加入申请" : "设置出生点申请";
        string msg = $"\n投票结束！{teamName}{typeDesc}申请结果:\n" +
                     $"同意:[c/32CD32:{stats.Agree}/{stats.Total}] ({stats.AgreeRate:F1}%)\n";

        TSPlayer? app = TShock.Players.FirstOrDefault(p => p?.Name == vote.AppName);

        if (stats.AgreeRate > 50)
        {
            if (vote.Type == VoteType.Join)
            {
                // 加入队伍
                if (app != null)
                    SwitchTeam(app, vote.Team, false);
                msg += "结果: [c/32CD32:通过] 已允许加入";
            }
            else // SetSpawn
            {
                // 设置出生点
                if (app != null && app.Active)
                {
                    var pos = new Point((int)app.X / 16, (int)app.Y / 16);
                    SpawnPoint[vote.Team] = pos;
                    msg += "结果: [c/32CD32:通过] 已设置新出生点";
                }
                else
                {
                    msg += "结果: [c/FF4500:申请人不在线，无法设置]";
                }
            }
        }
        else
        {
            msg += "结果: [c/FF4500:不通过]";
            if (app != null)
            {
                string rejectMsg = vote.Type == VoteType.Join
                    ? $"加入 {teamName} 的申请被拒绝"
                    : $"设置 {teamName} 出生点的申请被拒绝";
                app.SendMessage(TextGradient(rejectMsg), color);
            }
        }

        // 通知所有相关玩家
        for (int i = 0; i < TShock.Players.Length; i++)
        {
            var p = TShock.Players[i];
            if (p != null && p.Active && (p.Name == vote.AppName || p.Team == vote.Team))
            {
                p.SendMessage(TextGradient(msg), color);
            }
        }

        // 移除投票
        Remove(vote.AppName, vote.Team);
    }
    #endregion

    #region 显示投票状态（用于/tv v指令）
    public static void ShowStatus(TSPlayer plr, TeamVote vote)
    {
        var stats = vote.GetStats();
        var teamName = GetTeamCName(vote.Team);
        string typeStr = vote.Type == VoteType.Join ? "加入申请" : "设置出生点";

        // 检查投票状态
        string info = plr.Name == vote.AppName ? "（您是申请人）"
            : vote.Agree.Contains(plr.Name) ? "（您已投：同意）"
            : vote.Against.Contains(plr.Name) ? "（您已投：反对）"
            : "（您未投票）";

        string msg = $"\n{teamName}{typeStr} {info}\n" +
                     $"申请人: [c/508DC8:{vote.AppName}]\n" +
                     $"同意: [c/32CD32:{stats.Agree}/{stats.Total}] 反对: [c/FF4500:{stats.Against}/{stats.Total}]\n" +
                     $"同意率: [c/FFD700:{stats.AgreeRate:F1}%] 剩余: [c/00CED1:{vote.Remain}秒]\n";

        // 显示投票情况
        var voted = new List<string>();
        var notVoted = new List<string>();

        foreach (var p in stats.Players)
        {
            if (vote.Agree.Contains(p.Name))
                voted.Add($"[c/32CD32:{p.Name}]");
            else if (vote.Against.Contains(p.Name))
                voted.Add($"[c/FF4500:{p.Name}]");
            else
                notVoted.Add(p.Name);
        }

        if (voted.Count > 0)
            msg += $"已投票: {string.Join("、", voted)}\n";

        if (notVoted.Count > 0)
            msg += $"未投票: [c/888888:{string.Join("、", notVoted)}]";

        plr.SendMessage(TextGradient(msg), color);
    }
    #endregion

    #region 清理玩家投票数据（用于玩家离开服务器）
    public static void ClearApply(string plrName)
    {
        foreach (var kv in VoteData)
        {
            var vote = kv.Value;

            // 1. 检查是否是申请人
            if (vote.AppName == plrName)
            {
                if (!vote.IsEnd)
                {
                    NotifyCancel(vote, $"申请人 {plrName} 已离开");
                }

                Remove(vote.AppName, vote.Team); // 先通知再移除
                continue; // 已移除，不需要检查投票记录
            }

            // 2. 检查是否是投票者
            if (vote.Agree.Remove(plrName) || vote.Against.Remove(plrName))
            {
                if (!vote.IsEnd)
                {
                    CheckResult(vote);
                }
            }
        }
    }
    #endregion

    #region 通知投票取消
    private static void NotifyCancel(TeamVote vote, string reason)
    {
        var teamName = GetTeamCName(vote.Team);
        string typeDesc = vote.Type == VoteType.Join ? "加入申请" : "设置出生点申请";
        string msg = $"{teamName}{typeDesc}投票已取消: {reason}";

        // 通知申请者（如果在线）
        var app = TShock.Players.FirstOrDefault(p => p?.Name == vote.AppName);
        if (app != null) app.SendMessage(msg, color);

        // 通知目标队伍成员
        var stats = vote.GetStats();
        foreach (var p in stats.Players)
        {
            if (p == null || !p.Active) continue;
            p.SendMessage(TextGradient(msg), color);
        }
    }
    #endregion

    #region 切换队伍时的申请功能
    private static bool TeamApply(GetDataHandlers.PlayerTeamEventArgs e, TSPlayer plr, int oldTeam, int newTeam)
    {
        // 检查玩家是否拥有直接切换权限
        if (plr.HasPermission(Prem)) return false; // 直接允许切换队伍

        // newTeam > 0 表示不是无队伍
        if (Config.TeamMode && newTeam > 0)
        {
            // 检查玩家是否已有未结束的申请
            if (HasPending(plr.Name))
            {
                plr.SendMessage(TextGradient("您已有未完成的队伍申请，请等待投票结束"), Color.Yellow);
                e.Handled = true;
                plr.SetTeam(oldTeam);
                return true;
            }

            // 检查目标队伍是否已有未结束的投票
            if (HasTeamVote(newTeam))
            {
                plr.SendMessage(TextGradient("该队伍正在进行投票，请稍后再试"), Color.Yellow);
                e.Handled = true;
                plr.SetTeam(oldTeam);
                return true;
            }

            var teamName = GetTeamCName(newTeam); // 带颜色的队伍名称
            var target = new List<TSPlayer>();

            // 一次性收集目标队伍成员
            for (int i = 0; i < TShock.Players.Length; i++)
            {
                var p = TShock.Players[i];
                if (p != null && p.Active && p.Team == newTeam && p.Index != plr.Index)
                {
                    target.Add(p);
                }
            }

            // 如果目标队伍没有其他成员，直接允许加入
            if (target.Count == 0)
            {
                plr.SendMessage(TextGradient($"已加入{teamName}（队伍内无其他成员）"), color);
                return false; // 返回false，让后续逻辑处理正常的队伍切换
            }

            // 还原队伍
            e.Handled = true;
            plr.SetTeam(oldTeam);

            // 创建投票
            var vote = new TeamVote
            {
                AppName = plr.Name,
                Team = newTeam,
                Start = DateTime.Now,
                Time = Config.TeamVoteTime,
                Type = VoteType.Join   // 加入类型
            };

            if (Add(vote))
            {
                // 通知目标队伍成员
                string applyMsg = $"[c/508DC8:{plr.Name}]申请加入{teamName},\n" +
                                  $"请使用 /tv [c/5A9CDE:y] 或 /tv [c/F4636F:n] 投票({Config.TeamVoteTime}秒)";

                foreach (var p in target)
                {
                    p.SendMessage(TextGradient(applyMsg), color);
                }

                plr.SendMessage(TextGradient($"已向{teamName}发送申请，等待投票结果"), color);
            }
            else
            {
                plr.SendMessage(TextGradient("申请创建失败，请稍后再试"), color);
            }
            return true;
        }

        return false;
    }
    #endregion

    #region 队伍切换方法
    private static void SwitchTeam(TSPlayer plr, int newTeam, bool fromEvent = true)
    {
        int oldTeam = plr.Team;

        // 如果不是来自事件调用，手动设置队伍(事件自己会设置队伍)
        if (!fromEvent)
            plr.SetTeam(newTeam);

        // 获取队伍名称
        var oldName = GetTeamCName(oldTeam);
        var newName = GetTeamCName(newTeam);

        // 发送消息
        string mess = $"[c/508DC8:{plr.Name}] 已从{oldName}加入到{newName}";
        TSPlayer.All.SendMessage(mess, color);

        // 更新缓存
        var data = GetData(plr.Name);
        data.SwitchTime = DateTime.Now;
        data.Team = newTeam;
    }
    #endregion

    #region 玩家指令菜单方法
    private static void HelpCmd(TSPlayer plr)
    {
        var mess = new StringBuilder();

        // 构建消息内容
        if (!plr.RealPlayer)
        {
            // 控制台版本
            plr.SendMessage("《队伍投票》指令:", color);
            plr.SendMessage("/tv fp - 分配玩家队伍", color);
        }
        else
        {
            plr.SendMessage("\n[i:3455][c/AD89D5:队][c/D68ACA:伍][c/DF909A:投][c/E5A894:票][i:3454] " +
            "[i:3456][C/F2F2C7:开发] [C/BFDFEA:by] [c/00FFFF:羽学] [i:3459]", color);

            mess.Append("/tv y|n - 投票同意/拒绝\n");
            mess.Append("/tv v - 查看投票详情\n");
            mess.Append("/tv s - 申请修改本队出生点\n");

            // 管理专用指令
            if (plr.HasPermission(Prem))
            {
                mess.Append("/tv fp - 分配玩家队伍\n");
            }

            var data = GetData(plr.Name);

            // 队伍申请投票状态
            var vote = VoteData.Values.FirstOrDefault(v => v.Team == plr.Team && !v.IsEnd);
            if (vote != null)
            {
                // 有投票时显示投票信息
                var stats = vote.GetStats();
                bool hasVoted = vote.Agree.Contains(plr.Name) || vote.Against.Contains(plr.Name);
                string typeStr = vote.Type == VoteType.Join ? "申请入队" : "申请改出生点";
                mess.Append($"\n{typeStr}: [c/508DC8:{vote.AppName}]\n");
                mess.Append($"投票状态: {(hasVoted ? "[c/32CD32:已投票]" : "[c/FF4500:未投票]")}\n");
                mess.Append($"同意率: [c/32CD32:{stats.AgreeRate:F1}%]\n");
                mess.Append($"剩余时间: [c/00CED1:{vote.Remain}]秒");
            }

            plr.SendMessage(TextGradient(mess.ToString()), color);
        }
    }
    #endregion

    #region 玩家指令方法
    internal static void tvCmd(CommandArgs args)
    {
        var plr = args.Player;

        if (!Config.ApplyVote)
        {
            plr.SendMessage($"队伍申请投票功能未开启", color);
            return;
        }

        //子命令数量为0时显示帮助
        if (args.Parameters.Count == 0)
        {
            HelpCmd(plr);
            return;
        }

        // 处理子命令
        switch (args.Parameters[0].ToLower())
        {
            case "y":
            case "yes":
            case "是":
            case "允许":
            case "同意":
                Action(plr, true);
                break;

            case "n":
            case "no":
            case "不":
            case "拒绝":
            case "不同意":
                Action(plr, false);
                break;

            case "vote":
            case "v":
                ShowVoteCmd(plr);
                break;

            case "fp":
            case "分配":
                ForceTeam(plr, args);
                break;

            case "s":
            case "set":
            case "spawn":
            case "出生点":
                SetSpawnVote(plr);
                break;

            default:
                HelpCmd(plr);
                break;
        }
    }
    #endregion

    #region 投票操作(指令操作：/tv y与n)
    private static void Action(TSPlayer plr, bool isAgree)
    {
        if (!plr.RealPlayer)
        {
            plr.SendMessage("请进游戏加入对应队伍再使用该指令", color);
            return;
        }

        // 查找玩家当前队伍的投票
        var vote = VoteData.Values.FirstOrDefault(v => v.Team == plr.Team && !v.IsEnd && plr.Name != v.AppName);

        if (vote == null)
        {
            plr.SendMessage("当前没有可投票的申请", color);
            return;
        }

        // 检查是否已投票
        if (vote.Agree.Contains(plr.Name) || vote.Against.Contains(plr.Name))
        {
            ShowStatus(plr, vote);
            return;
        }

        // 执行投票
        if (isAgree)
        {
            vote.Agree.Add(plr.Name);
        }
        else
        {
            vote.Against.Add(plr.Name);
        }

        // 检查投票结果
        CheckResult(vote);
    }
    #endregion

    #region 查看投票状态的方法
    private static void ShowVoteCmd(TSPlayer plr)
    {
        if (!Config.TeamMode)
        {
            plr.SendMessage("队伍申请功能未开启", color);
            return;
        }

        // 查找玩家当前队伍的投票
        var vote = VoteData.Values.FirstOrDefault(v =>
            v.Team == plr.Team && !v.IsEnd && plr.Name != v.AppName);

        if (vote == null)
        {
            plr.SendMessage("当前没有进行中的投票", color);
            return;
        }

        ShowStatus(plr, vote);
    }
    #endregion

    #region 强制分配队伍方法
    private static void ForceTeam(TSPlayer plr, CommandArgs args)
    {
        if (!plr.HasPermission(Prem)) return;

        if (args.Parameters.Count < 3)
        {
            plr.SendMessage(TextGradient("用法: /tv fp <玩家> <队伍id> [-L]"), color);
            plr.SendMessage(TextGradient("玩家: 玩家名或玩家索引(1-255)"), color);
            plr.SendMessage("队伍id: \n" +
                            "[c/5ADECE:白队](0),[c/F56470:红队](1)," +
                            "[c/74E25C:绿队](2),[c/5A9DDE:蓝队](3)," +
                            "[c/FCF466:黄队](4),[c/E15BC2:粉队](5)", color);
            plr.SendMessage(TextGradient("-L: 可选,锁定队伍"), color);
            plr.SendMessage(TextGradient("【[c/E24763:注]】: 使用/who -i 可查看玩家索引"), color);

            return;
        }

        // 获取目标玩家 - 支持索引和名字
        TSPlayer? target = null;

        // 先尝试解析在线索引
        string input = args.Parameters[1];
        if (int.TryParse(input, out int pIndex))
        {
            // 检查索引范围
            if (pIndex >= 0 && pIndex < TShock.Players.Length)
            {
                target = TShock.Players[pIndex];
                if (target == null || !target.RealPlayer)
                {
                    plr.SendMessage($"索引 {pIndex} 处无在线玩家", Color.Yellow);
                    return;
                }
            }
            else
            {
                plr.SendMessage($"索引 {pIndex} 超出范围(0-{TShock.Players.Length - 1})", Color.Yellow);
                return;
            }
        }
        else
        {
            // 作为玩家名处理
            target = TShock.Players.FirstOrDefault(p => p != null && p.Name.Equals(input, StringComparison.OrdinalIgnoreCase));
        }

        // 解析队伍id
        string team = args.Parameters[2];
        int teamId = -1;
        if (!int.TryParse(team, out int id) || id < 1 || id > 5)
        {
            plr.SendMessage(TextGradient("队伍id: \n" +
                            "[c/5ADECE:白队](0),[c/F56470:红队](1)," +
                            "[c/74E25C:绿队](2),[c/5A9DDE:蓝队](3)," +
                            "[c/FCF466:黄队](4),[c/E15BC2:粉队](5)"), color);

            plr.SendMessage(TextGradient($"请输入队伍对应的数字id,禁止设置白队"), color);
            return;
        }
        teamId = id;

        // 如果玩家不在线，检查数据库并创建缓存
        bool lockTeam = args.Parameters.Count > 3 && args.Parameters[3].ToLower() == "-l";
        if (target == null)
        {
            // 从数据库查找用户账户
            var acc = TShock.UserAccounts.GetUserAccountByName(input);
            if (acc == null)
            {
                plr.SendMessage($"玩家 {input} 不存在", color);
                return;
            }

            // 创建玩家缓存数据
            var data = GetData(acc.Name);
            data.Team = teamId;
            data.Lock = lockTeam;

            // 发送消息
            plr.SendMessage(TextGradient($"已为离线玩家 {acc.Name} 设置队伍 {GetTeamCName(teamId)}" +
                           (lockTeam ? " [c/FF5555:(已锁定)]" : "")), color);
            plr.SendMessage(TextGradient("该玩家下次进入服务器时将自动分配到指定队伍"), color);
            return;
        }

        // 玩家在线的情况
        var data2 = GetData(target.Name);

        // 设置队伍
        target.SetTeam(teamId);

        // 更新缓存
        data2.Team = teamId;
        data2.Lock = lockTeam;
        data2.SwitchTime = DateTime.Now;

        // 发送消息
        plr.SendMessage(TextGradient($"已将 {target.Name} (索引:{target.Index}) 分配到 {GetTeamCName(data2.Team)}" +
                       (lockTeam ? " [c/FF5555:(已锁定)]" : "")), color);

        target.SendMessage(TextGradient($"你已被分配到 {GetTeamCName(data2.Team)}" +
                          (lockTeam ? " [c/FF5555:(队伍已锁定)]" : "")), color);
    }
    #endregion

    #region 发起设置出生点投票
    private static void SetSpawnVote(TSPlayer plr)
    {
        // 检查队伍出生点功能是否开启
        if (!Config.TeamSpawn)
        {
            plr.SendMessage(TextGradient("队伍出生点功能未开启，无法使用"), color);
            return;
        }

        // 检查是否是控制台在使用指令
        if (!plr.RealPlayer)
        {
            plr.SendMessage("你必须进入游戏且处于队伍中才能使用该指令", color);
            return;
        }

        // 获取玩家所在队伍
        int team = plr.Team;

        // 不准设置白队出生点
        if (team < 1)
        {
            plr.SendMessage(TextGradient($"禁止设置{GetTeamCName(0)}出生点"), color);
            return;
        }

        // 有管理权，直接设置，无需投票
        if (plr.HasPermission(Prem))
        {
            SpawnPoint[team] = new Point((int)plr.X / 16, (int)(plr.Y / 16));
            TSPlayer.All.SendMessage(TextGradient($"管理 {plr.Name} 已将 {(int)plr.X / 16},{(int)plr.Y / 16} 设为" +
                                                  $"{GetTeamCName(plr.Team)}出生点"), color);
            return;
        }

        // 检查该队伍是否已有投票（无论类型）
        if (HasTeamVote(team))
        {
            plr.SendMessage(TextGradient("该队伍已有投票正在进行，请稍后再试"), Color.Yellow);
            return;
        }

        // 获取队伍其他成员
        var members = new List<TSPlayer>();
        for (int i = 0; i < TShock.Players.Length; i++)
        {
            var p = TShock.Players[i];
            if (p != null && p.Active && p.Team == team && p.Index != plr.Index)
                members.Add(p);
        }

        // 无其他成员，返回
        if (members.Count == 0)
        {
            plr.SendMessage(TextGradient($"申请修改{GetTeamCName(plr.Team)}出生点最少需2人"), color);
            return;
        }

        var vote = new TeamVote
        {
            AppName = plr.Name,
            Team = team,
            Start = DateTime.Now,
            Time = Config.TeamVoteTime,
            Type = VoteType.SetSpawn   // 设置出生点类型
        };

        // 加入投票
        if (Add(vote))
        {
            string teamName = GetTeamCName(team);
            string msg = $"[c/508DC8:{plr.Name}] 申请设置{teamName}出生点，\n" +
                         $"同意: /tv [c/5A9CDE:y] 或拒绝: /tv [c/F4636F:n]（{Config.TeamVoteTime}秒）";

            foreach (var m in members)
                m.SendMessage(TextGradient(msg), color);

            plr.SendMessage(TextGradient($"已向{teamName}其他成员申请 设置出生点 "), color);
        }
        else
        {
            plr.SendMessage(TextGradient("发起申请失败，请稍后再试"), color);
        }
    }
    #endregion

    #region 队伍模式管理指令
    public static void SwitchTeam(CommandArgs args, TSPlayer plr)
    {
        if (args.Parameters.Count < 2)
        {
            ShowTeamMenu(plr);
            return;
        }

        switch (args.Parameters[1].ToLower())
        {
            case "on":
            case "off":
                SetBool("队伍模式总开关", plr, () => Config.TeamMode, (val) => Config.TeamMode = val);
                break;

            case "s":
            case "spawn":
            case "出生点":
                SetBool("队伍出生点", plr, () => Config.TeamSpawn, (val) => Config.TeamSpawn = val);
                break;

            case "t":
            case "time":
            case "投票时间":
                if (args.Parameters.Count < 3)
                {
                    plr.SendMessage($"用法: /{pt} tm t <秒数>", color);
                    plr.SendMessage($"当前投票时间: {Config.TeamVoteTime}秒", color2);
                    return;
                }
                SetNum("队伍投票时间", plr, (val) => Config.TeamVoteTime = val, args.Parameters[2], "秒");
                break;

            case "cd":
            case "冷却":
                if (args.Parameters.Count < 3)
                {
                    plr.SendMessage($"用法: /{pt} tm cd <秒数>", color);
                    plr.SendMessage($"当前切换冷却: {Config.SwitchTeamCD}秒", color2);
                    return;
                }
                SetNum("队伍切换冷却", plr, (val) => Config.SwitchTeamCD = val, args.Parameters[2], "秒");
                break;

            case "i":
            case "item":
            case "物品惩罚":
                SetBool("队员死亡物品惩罚", plr, () => Config.TeamItemPun, (val) => Config.TeamItemPun = val);
                break;

            case "d":
            case "dead":
            case "death":
            case "团灭":
                SetBool("队员死亡集体团灭", plr, () => Config.TeamDeathPun, (val) => Config.TeamDeathPun = val);
                break;

            default:
                ShowTeamMenu(plr);
                break;
        }
    }

    private static void ShowTeamMenu(TSPlayer plr)
    {
        var state1 = Config.TeamMode ? "开" : "关";
        var state2 = $"{Config.TeamVoteTime}";
        var state3 = $"{Config.SwitchTeamCD}";
        var state4 = Config.TeamItemPun ? "开" : "关";
        var state5 = Config.TeamDeathPun ? "开" : "关";
        var state6 = Config.TeamSpawn ? "开" : "关";

        var mess = new StringBuilder();
        mess.AppendLine($"\n[c/AD89D5:队伍模式设置]");
        mess.AppendLine($"[c/3FAEDB:总开关][{state1}] /{pt} tm on/off");
        mess.AppendLine($"[c/3FAEDB:出生点][{state6}] /{pt} tm s");
        mess.AppendLine($"[c/3FAEDB:投票时间][{state2}] /{pt} tm t <秒>");
        mess.AppendLine($"[c/3FAEDB:切换冷却][{state3}] /{pt} tm cd <秒>");
        mess.AppendLine($"[c/3FAEDB:物品惩罚][{state4}] /{pt} tm i");
        mess.AppendLine($"[c/3FAEDB:集体团灭][{state5}] /{pt} tm d");

        if (plr.RealPlayer)
            plr.SendMessage(TextGradient(mess.ToString()), color);
        else
            plr.SendMessage(mess.ToString(), color);
    }
    #endregion

}