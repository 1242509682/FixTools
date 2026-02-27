using DeathEvent;
using Terraria;
using Terraria.ID;
using Terraria.Net;
using TerrariaApi.Server;
using TShockAPI;
using TShockAPI.DB;
using TShockAPI.Hooks;
using Terraria.GameContent.Creative;
using static FixTools.PlayerState;
using static FixTools.Utils;

namespace FixTools;

[ApiVersion(2, 1)]
public partial class FixTools : TerrariaPlugin
{
    #region 插件信息
    public override string Name => PluginName;
    public override string Author => "羽学";
    public override Version Version => new(2026, 2, 25);
    public override string Description => "本插件仅TShock测试版期间维护,指令/pout";
    #endregion

    #region 静态变量
    public static string PluginName => "145修复小公举"; // 插件名称
    public static string pt => "pout"; // 主指令名称
    public static string Prem => $"{pt}.use"; // 管理权限
    public static string bak => "bak"; // 投票指令名
    public static string TShockVS => "1770f2d"; // 适配版本号
    public static readonly string MainPath = Path.Combine(TShock.SavePath, PluginName); // 主文件夹路径
    public static readonly string ConfigPath = Path.Combine(MainPath, "配置文件.json"); // 配置文件路径
    #endregion

    #region 注册与释放
    public FixTools(Main game) : base(game) { }
    public override void Initialize()
    {
        LoadConfig(); // 加载配置文件
        GeneralHooks.ReloadEvent += ReloadConfig;
        ServerApi.Hooks.GamePostInitialize.Register(this, this.GamePost, 9999);
        ServerApi.Hooks.ServerJoin.Register(this, OnServerJoin);
        GetDataHandlers.PlayerSpawn.Register(this.OnPlayerSpawn);
        GetDataHandlers.PlayerUpdate.Register(this.OnPlayerUpdate);
        GetDataHandlers.KillMe.Register(OnKillMe);
        GetDataHandlers.PlayerTeam.Register(TeamData.OnPlayerTeam);
        ServerApi.Hooks.ServerLeave.Register(this, this.OnServerLeave);
        GetDataHandlers.PlaceObject.Register(this.OnPlaceObject);
        GetDataHandlers.MassWireOperation.Register(this.OnWire);
        ServerApi.Hooks.NetGetData.Register(this, OnNetGetData);
        ServerApi.Hooks.GameUpdate.Register(this, OnGameUpdate);
        ServerApi.Hooks.NpcAIUpdate.Register(this, ProgressLock.OnNpcAIUpdate);
        ServerApi.Hooks.NpcStrike.Register(this, ProgressLock.OnNPCStrike);
        ServerApi.Hooks.NpcKilled.Register(this, ProgressLock.OnNPCKilled);
        ServerApi.Hooks.DropBossBag.Register(this, DropBossBags.OnDropBossBag);
        ServerApi.Hooks.ServerChat.Register(this, this.OnChat);
        On.Terraria.GameContent.CraftingRequests.CanCraftFromChest += CanCraft.OnCanCraftFromChest;
        On.Terraria.GameContent.BossDamageTracker.OnBossKilled += DamageTrackers.OnBossKilled;
        TShockAPI.Commands.ChatCommands.Add(new Command(Prem, PoutCmd.Pouts, pt, "pt"));
        TShockAPI.Commands.ChatCommands.Add(new Command(string.Empty, BakCmd.bakCmd, bak));
        TShockAPI.Commands.ChatCommands.Add(new Command(string.Empty, TeamData.tvCmd, "tv"));
        TShockAPI.Commands.ChatCommands.Add(new Command(string.Empty, DeadLimit.Back, "back"));
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            GeneralHooks.ReloadEvent -= ReloadConfig;
            ServerApi.Hooks.GamePostInitialize.Deregister(this, this.GamePost);
            ServerApi.Hooks.ServerJoin.Deregister(this, OnServerJoin);
            GetDataHandlers.PlayerSpawn.UnRegister(this.OnPlayerSpawn);
            GetDataHandlers.PlayerUpdate.UnRegister(this.OnPlayerUpdate);
            GetDataHandlers.KillMe.UnRegister(OnKillMe);
            GetDataHandlers.PlayerTeam.UnRegister(TeamData.OnPlayerTeam);
            ServerApi.Hooks.ServerLeave.Deregister(this, this.OnServerLeave);
            GetDataHandlers.PlaceObject.UnRegister(this.OnPlaceObject);
            GetDataHandlers.MassWireOperation.UnRegister(this.OnWire);
            ServerApi.Hooks.NetGetData.Deregister(this, OnNetGetData);
            ServerApi.Hooks.GameUpdate.Deregister(this, OnGameUpdate);
            ServerApi.Hooks.NpcAIUpdate.Deregister(this, ProgressLock.OnNpcAIUpdate);
            ServerApi.Hooks.NpcStrike.Deregister(this, ProgressLock.OnNPCStrike);
            ServerApi.Hooks.NpcKilled.Deregister(this, ProgressLock.OnNPCKilled);
            ServerApi.Hooks.DropBossBag.Deregister(this, DropBossBags.OnDropBossBag);
            ServerApi.Hooks.ServerChat.Deregister(this, this.OnChat);
            On.Terraria.GameContent.CraftingRequests.CanCraftFromChest -= CanCraft.OnCanCraftFromChest;
            On.Terraria.GameContent.BossDamageTracker.OnBossKilled -= DamageTrackers.OnBossKilled;
            TShockAPI.Commands.ChatCommands.RemoveAll(x =>
            x.CommandDelegate == PoutCmd.Pouts ||
            x.CommandDelegate == BakCmd.bakCmd ||
            x.CommandDelegate == DeadLimit.Back ||
            x.CommandDelegate == TeamData.tvCmd);
        }
        base.Dispose(disposing);
    }
    #endregion

    #region 配置重载读取与写入方法
    internal static Configuration Config = new(); // 配置文件实例
    private static void ReloadConfig(ReloadEventArgs args = null!)
    {
        LoadConfig();
        args.Player.SendMessage($"[{PluginName}]重新加载配置完毕。", color);
    }
    private static void LoadConfig()
    {
        // 创建配置文件夹
        if (!Directory.Exists(MainPath))
            Directory.CreateDirectory(MainPath);
        // 创建复制源文件文件夹
        if (!Directory.Exists(PoutCmd.CopyDir))
            Directory.CreateDirectory(PoutCmd.CopyDir);
        // 创建复制地图文件夹
        if (!Directory.Exists(PoutCmd.MapDir))
            Directory.CreateDirectory(PoutCmd.MapDir);
        // 创建导出存档文件夹
        if (!Directory.Exists(WritePlayer.WriteDir))
            Directory.CreateDirectory(WritePlayer.WriteDir);
        // 创建导入存档文件夹
        if (!Directory.Exists(ReaderPlayer.ReaderDir))
            Directory.CreateDirectory(ReaderPlayer.ReaderDir);
        // 创建自动备份文件夹
        if (!Directory.Exists(WritePlayer.AutoSaveDir))
            Directory.CreateDirectory(WritePlayer.AutoSaveDir);

        Config = Configuration.Read();
        Config.Write();
    }
    #endregion

    #region 世界加载完结束事件
    private void GamePost(EventArgs args)
    {
        Console.WriteLine($"\n----------{PluginName} v{Version}----------");

        Console.WriteLine(string.Empty);
        Console.WriteLine($"[本插件支持]");
        TShock.Log.ConsoleInfo($"1.导入导出SSC存档、自动备份存档、禁用区域箱子材料");
        TShock.Log.ConsoleInfo($"2.智能进服公告、跨版本进服、修复地图区块缺失、boss伤害排行");
        TShock.Log.ConsoleInfo($"3.批量改权限、导出权限表、复制文件、宝藏袋传送、修复局部图格");
        TShock.Log.ConsoleInfo($"4.自动注册、自动建GM组、自动配权、进度锁、重置服务器");
        TShock.Log.ConsoleInfo($"5.修复召唤入侵事件、修复天塔柱刷物品BUG、投票回档");
        TShock.Log.ConsoleInfo($"6.进服恢复队伍与出生点、投票切换队伍、投票修改队伍出生点");
        TShock.Log.ConsoleInfo($"指令/{pt} 权限:{pt}.use");
        TShock.Log.ConsoleInfo($"配置文件路径:{ConfigPath}");
        Console.WriteLine(string.Empty);

        if (Config.PostCMD.Any())
        {
            Console.WriteLine($"[开服执行指令]");
            PoutCmd.DoCommand(TSPlayer.Server, Config.PostCMD);
            Console.WriteLine(string.Empty);
        }

        if (Config.AutoFixWorld)
        {
            PoutCmd.ExecuteFix(TSPlayer.Server, false);
            Console.WriteLine(string.Empty);
        }

        if (Config.AutoPerm)
        {
            Console.WriteLine($"[自动配权提醒]");
            PoutCmd.ManagePerm(TSPlayer.Server, true);
            TShock.Log.ConsoleInfo($"如果不需要可批量移除:/pout del");
            Console.WriteLine(string.Empty);
        }

        if (Config.AutoAddGM && !TShock.Groups.GroupExists("GM"))
        {
            Console.WriteLine($"[自动建GM组]");
            TShock.Groups.AddGroup("GM", string.Empty, "*,!tshock.ignore.ssc", "193,223,186");
            TShock.Log.ConsoleInfo($"已创建GM权限组，请使用角色进入游戏后:");
            TShock.Log.ConsoleInfo($"在本控制台指定管理:/user group 玩家名 GM");
            Console.WriteLine(string.Empty);
        }

        if (Config.AutoRegister)
        {
            Console.WriteLine($"[自动注册功能]");
            var caibot = "CaiBotLitePlugin";
            var hasCaibot = ServerApi.Plugins.Any(p => p.Plugin.Name == caibot);
            if (!hasCaibot)
            {
                TShock.Log.ConsoleInfo($"自动注册功能已启用，检测到新玩家将自动注册账号，");
                TShock.Log.ConsoleInfo($"默认注册密码为: {Config.DefPass}");
                TShock.Log.ConsoleInfo($"玩家自己改密码: /password {Config.DefPass} 新密码");
                TShock.Log.ConsoleInfo($"帮玩家修改密码: /user password 玩家名 新密码");
            }
            else
            {
                Config.AutoRegister = false;
                Config.Write();
                TShock.Log.ConsoleInfo($"检测到已存在【{caibot}】插件，自动注册功能已禁用");
            }
            Console.WriteLine(string.Empty);
        }

        Console.WriteLine("----------------------------------------------\n");
    }
    #endregion

    #region 加入服务器事件
    private void OnServerJoin(JoinEventArgs args)
    {
        var plr = TShock.Players[args.Who];
        if (plr is null) return;

        var user = TShock.UserAccounts.GetUserAccountByName(plr.Name);
        var data = GetData(plr.Name);
        if (user is null)
        {
            // 如果有Caibot则返回
            var hasCaibot = ServerApi.Plugins.Any(p => p.Plugin.Name == "CaiBotLitePlugin");
            if (!Config.AutoRegister || hasCaibot) return;

            // 如果开启随机密码则应用随机,否则使用默认密码
            string RandPass = GetRandPass();
            var pass = Config.RandPass ? RandPass : Config.DefPass;
            var group = TShock.Config.Settings.DefaultRegistrationGroupName;
            var NewUser = new UserAccount(plr.Name, pass, plr.UUID, group,
                                          DateTime.UtcNow.ToString("s"),
                                          DateTime.UtcNow.ToString("s"), "");
            try
            {
                // 缓存一下随机密码，用于播报
                data.DefPass = pass;

                // 给密码上个哈希，不然玩家改不了密码
                NewUser.CreateBCryptHash(pass);
                TShock.UserAccounts.AddUserAccount(NewUser);

                data.Motd = 1;
                data.Register = true;
            }
            catch (Exception ex)
            {
                TShock.Log.ConsoleError($"[{PluginName}] 自动注册失败 [{plr.Name}]: \n{ex.Message}");
            }
        }
        else
        {
            data.Motd = 1;

            // 修复复活检查,限制死亡状态进服
            if (Config.FixSapwn)
            {
                DeadLimit.LimitJoin(plr, data);
            }
        }
    }
    #endregion

    #region 玩家离开服务器事件
    private void OnServerLeave(LeaveEventArgs args)
    {
        var plr = TShock.Players[args.Who];
        if (plr is null) return;

        var data = GetData(plr.Name);
        if (data != null)
        {
            data.Motd = 0;
            data.Register = false;

            // 清理该玩家残留的临时文件
            data.rwFix = false;
            data.rwSnap = string.Empty;
            data.rwSign = string.Empty;
        }

        if (Config.TeamMode)
            TeamData.ClearApply(plr.Name);

        // 如果离开的玩家是申请人，取消申请
        if (BakCmd.curName == plr.Name)
        {
            TSPlayer.All.SendMessage(TextGradient($"{plr.Name} 已离开,他的个人回档申请已取消"), color);
            BakCmd.ClearApply();
        }
    }
    #endregion

    #region 玩家生成事件
    private void OnPlayerSpawn(object? sender, GetDataHandlers.SpawnEventArgs e)
    {
        var plr = e.Player;
        if (plr == null || !plr.IsLoggedIn) return;

        var data = GetData(plr.Name);
        if (data is null) return;

        // 管理进服无敌
        if (Config.AutoGod && plr.HasPermission(Prem) && e.SpawnContext == PlayerSpawnContext.SpawningIntoWorld)
        {
            var Power = CreativePowerManager.Instance.GetPower<CreativePowers.GodmodePower>();
            Power.SetEnabledState(plr.Index, true);

            // 下面2条消息管理自己看不见,其他玩家可以看见(因为事件本身比玩家完全进来触发要早)
            TSPlayer.All.SendMessage(TextGradient("已为[c/4CB5DE:管理员] {玩家名} 开启无敌模式", plr), color);
            TSPlayer.All.SendMessage(TextGradient("当前进度:{进度}"), color);
        }

        if (Config.TeamMode)
        {
            TeamData.TeamSpawn(e, plr, data);
        }
    }
    #endregion

    #region 玩家更新事件推送进服公告+自动注册反馈+恢复存档+修复物品召唤入侵
    private void OnPlayerUpdate(object? sender, GetDataHandlers.PlayerUpdateEventArgs e)
    {
        var plr = e.Player;
        if (plr is null || !plr.RealPlayer ||
           !plr.Active || !plr.IsLoggedIn)
            return;

        var data = GetData(plr.Name);

        // 如果开启队伍申请模式,刚进服就恢复队伍回到此队出生点
        if (Config.TeamMode)
            TeamData.IsJoinBackTeam(plr, data);

        // 进服公告
        if (Config.MotdEnabled && data.Motd == 1)
        {
            if (Config.MotdMess.Any())
                plr.SendMessage($"{TextGradient(string.Join("\n", Config.MotdMess), plr: plr)}", color);

            data.Motd = 2;
        }

        // 注册成功提示
        if (data.Register)
        {
            var regText = $"[{PluginName}] 已为您自动注册，默认密码为: {data.DefPass}\n" +
                          $"使用指令修改密码: /password {data.DefPass} 新密码\n";

            plr.SendMessage(TextGradient(regText), color);
            TShock.Log.ConsoleInfo($"[{PluginName}]");
            TShock.Log.ConsoleInfo($"自动为玩家 {plr.Name} 注册账号,密码为 {data.DefPass}");
            TShock.Log.ConsoleInfo($"帮玩家修改密码: /user password {plr.Name} 新密码");
            data.Register = false;
        }

        // 检查是否需要恢复存档（玩家复活后）
        if (data.NeedRestores != null && !plr.Dead)
        {
            try
            {
                data.NeedRestores.RestoreCharacter(plr);
                plr.SendMessage($"已自动为你恢复存档物品!", color);
            }
            finally
            {
                data.NeedRestores = null;
            }
        }

        // 修复玩家使用物品召唤入侵后未正确触发入侵事件的情况
        if (Config.FixStartInvasion)
            FixStartInvasion.StartInvasion(plr);
    }
    #endregion

    #region 玩家死亡事件
    public static void OnKillMe(object? sender, GetDataHandlers.KillMeEventArgs e)
    {
        var plr = TShock.Players[e.PlayerId];
        if (plr is null || !plr.RealPlayer) return;

        var data = GetData(plr.Name);
        if (data is null) return;

        // 队伍模式惩罚
        if (Config.TeamMode)
            TeamData.TeamKillMe(e, plr, data);

        // 修复复活检查
        if (Config.FixSapwn)
        {
            DeadLimit.FixRespawnTimer(plr, data);
        }
    }
    #endregion

    #region 游戏更新事件
    private static long frame = 0;  // 自动备份计时器
    private static long Teamframe = 0;  // 队伍投票计时器
    private static long checkFrame = 0; // 申请检查计时器
    public static bool hasApply = false; // 是否有申请存在
    private void OnGameUpdate(EventArgs args)
    {
        // 自动备份逻辑
        if (Config.AutoSavePlayer)
        {
            frame++;
            if (frame >= Config.AutoSaveInterval * 60 * 60)
            {
                WritePlayer.ExportAll(TSPlayer.Server, WritePlayer.AutoSaveDir);
                frame = 0;
            }
        }

        // 未满足复活时间,定时杀死玩家
        if (Config.FixSapwn)
        {
            DeadLimit.DeadTimeUpdate();
        }

        // 队伍投票时间检查
        if (Config.TeamMode)
        {
            // 当有人刚进服或者回到出生点时
            // 每帧检查是否需要传送会队伍出生点
            if (TeamData.needTP.Count > 0)
                TeamData.BackTeamSpawn();

            // 当有投票时，每秒检查投票是否过期
            if (TeamData.VoteData.Count > 0 && ++Teamframe >= 60)
            {
                TeamData.CheckTimeout();
                Teamframe = 0;
            }
        }

        // 有投票时每秒检查1次过期时间,没有投票则返回
        if (Config.ApplyVote && hasApply && ++checkFrame >= 60)
        {
            // 检查当前申请
            if (BakCmd.curVote != null && BakCmd.curName != null)
            {
                // 检查申请是否超时
                var remain = DateTime.Now - BakCmd.curVote.ApplyTime;
                if (remain.TotalSeconds > Config.ApplyTime)
                {
                    // 发送超时消息
                    TSPlayer.All.SendMessage(TextGradient($"[{PluginName}] {BakCmd.curName}的回档申请[c/FF5E57:已超时]自动关闭"), color);

                    // 清理申请状态
                    BakCmd.ClearApply();
                    hasApply = false;
                }
                else
                {
                    // 检查投票条件
                    if (BakCmd.CheckVote())
                        BakCmd.DoApprove(TSPlayer.Server);
                }
            }

            checkFrame = 0;
        }
    }
    #endregion

    #region 玩家聊天事件显示功能相关信息（交互型方法）
    private void OnChat(ServerChatEventArgs args)
    {
        var plr = TShock.Players[args.Who];
        if (plr is null || !plr.RealPlayer ||
           !plr.Active || !plr.IsLoggedIn)
            return;

        // 原始消息
        var Text = args.Text;

        // 检查是否为命令
        if (Text.StartsWith(TShock.Config.Settings.CommandSpecifier) ||
            Text.StartsWith(TShock.Config.Settings.CommandSilentSpecifier))
            return;

        // 检查是否输入"宝藏袋"相关传送
        if (Config.AllowTpBagText is not null && Config.AllowTpBagText.Count > 0)
        {
            // 检查是否包含任意一个关键词
            if (Config.AllowTpBagText.Any(Text.Contains))
            {
                DropBossBags.TpBag(Text, plr);
                return;
            }
        }

        // 原有的Motd显示逻辑
        if (Config.MotdEnabled)
        {
            var data = GetData(plr.Name);

            if (data.Motd == 2)
            {
                if (Config.MotdMess2.Any())
                    plr.SendMessage($"{TextGradient(string.Join("\n", Config.MotdMess2), plr: plr)}", color);
                data.Motd = 3;
                data.SendTime = DateTime.Now;
                return; // 设置完 motd3 后直接返回，避免后续代码执行
            }

            if (data.Motd == 3)
            {
                TimeSpan sendTime = DateTime.Now - data.SendTime;
                if (sendTime.TotalSeconds > 1)
                {
                    if (Config.MotdMess3.Any())
                        plr.SendMessage($"{TextGradient(string.Join("\n", Config.MotdMess3), plr: plr)}", color);

                    data.Motd = 0;
                    data.SendTime = DateTime.MinValue;
                }
            }
        }
    }
    #endregion

    #region 跨版本进服方法
    private void OnNetGetData(GetDataEventArgs args)
    {
        if (args.MsgID == PacketTypes.ConnectRequest && Config.NoVisualLimit)
        {
            args.Handled = true;

            if (Main.netMode is not 2) return;

            RemoteClient client = Netplay.Clients[args.Msg.whoAmI];
            RemoteAddress ip = client.Socket.GetRemoteAddress();

            if (Main.dedServ && Netplay.IsBanned(ip))
            {
                // 因封禁,禁止玩家连接
                NetMessage.TrySendData(MessageID.Kick, args.Msg.whoAmI, -1, Lang.mp[3].ToNetworkText());
            }
            else if (client.State == 0)
            {
                if (string.IsNullOrEmpty(Netplay.ServerPassword))
                {
                    // 无密码服务器，直接通过
                    client.State = 1;
                    NetMessage.TrySendData(MessageID.PlayerInfo, args.Msg.whoAmI);
                }
                else
                {
                    // 需要密码验证
                    client.State = -1;
                    NetMessage.TrySendData(MessageID.RequestPassword, args.Msg.whoAmI);
                }
            }
        }
    }
    #endregion

    #region 修复天塔柱刷物品BUG
    private void OnPlaceObject(object? sender, GetDataHandlers.PlaceObjectEventArgs e)
    {
        var plr = e.Player;
        if (plr is null || !plr.RealPlayer ||
            !plr.Active || !plr.IsLoggedIn ||
            !Config.FixPlaceObject)
            return;

        // 修复天塔柱刷物品BUG
        if (FixPlaceObject.TargetItem(e.Type))
        {
            FixPlaceObject.FixPlace(e);
        }
    }
    #endregion

    #region 精密线控仪事件
    private void OnWire(object? sender, GetDataHandlers.MassWireOperationEventArgs e)
    {
        var plr = e.Player;
        if (plr is null || !plr.RealPlayer ||
           !plr.Active || !plr.IsLoggedIn)
            return;

        WorldTile.FixSnap(e, plr);

    }
    #endregion

}