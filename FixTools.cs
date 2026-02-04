using Terraria;
using Terraria.ID;
using Terraria.Net;
using TerrariaApi.Server;
using TShockAPI;
using TShockAPI.DB;
using TShockAPI.Hooks;
using static FixTools.Utils;

namespace FixTools;

[ApiVersion(2, 1)]
public partial class FixTools : TerrariaPlugin
{
    #region 插件信息
    public override string Name => PluginName;
    public override string Author => "羽学";
    public override Version Version => new(2026, 2, 4, 2);
    public override string Description => "本插件仅TShock测试版期间维护,指令/pout";
    #endregion

    #region 静态变量
    public static string PluginName => "145修复小公举"; // 插件名称
    public static string CmdName => "pout"; // 指令名称
    internal static Configuration Config = new(); // 配置文件实例
    public static readonly string MainPath = Path.Combine(TShock.SavePath, PluginName); // 主文件夹路径
    public static readonly string ConfigPath = Path.Combine(MainPath, "配置文件.json"); // 配置文件路径
    public static readonly string CopyDir = Path.Combine(MainPath, "复制源文件"); // 复制源文件路径
    public static readonly string AutoSaveDir = Path.Combine(MainPath, "自动备份存档"); // 自动备份角色路径
    public static readonly string SqlPath = Path.Combine(TShock.SavePath, "tshock.sqlite"); // 数据库路径
    public static readonly string WritePlrDir = Path.Combine(MainPath, "导出存档"); // 导出角色路径
    public static readonly string ReaderPlrDir = Path.Combine(MainPath, "导入存档"); // 导入角色路径
    public static readonly string MapDir = Path.Combine(MainPath, "重置时用的复制地图"); // 复制地图路径
    public static readonly string WldDir = Path.Combine(typeof(TShock).Assembly.Location, "world"); // 加载地图路径
    public static readonly string StartConfigPath = Path.Combine(typeof(TShock).Assembly.Location, "server.properties"); // 启动参数路径
    #endregion

    #region 注册与释放
    public FixTools(Main game) : base(game) { }
    public override void Initialize()
    {
        // 创建配置文件夹
        if (!Directory.Exists(MainPath))
            Directory.CreateDirectory(MainPath);
        // 创建复制源文件文件夹
        if (!Directory.Exists(CopyDir))
            Directory.CreateDirectory(CopyDir);
        // 创建复制地图文件夹
        if (!Directory.Exists(MapDir))
            Directory.CreateDirectory(MapDir);
        // 创建导出存档文件夹
        if (!Directory.Exists(WritePlrDir))
            Directory.CreateDirectory(WritePlrDir);
        // 创建导入存档文件夹
        if (!Directory.Exists(ReaderPlrDir))
            Directory.CreateDirectory(ReaderPlrDir);
        // 创建自动备份文件夹
        if (!Directory.Exists(AutoSaveDir))
            Directory.CreateDirectory(AutoSaveDir);

        LoadConfig(); // 加载配置文件
        GeneralHooks.ReloadEvent += ReloadConfig;
        ServerApi.Hooks.GamePostInitialize.Register(this, this.GamePost, 9999);
        ServerApi.Hooks.ServerJoin.Register(this, OnServerJoin);
        ServerApi.Hooks.NetGreetPlayer.Register(this, this.OnGreetPlayer);
        ServerApi.Hooks.ServerLeave.Register(this, this.OnServerLeave);
        GetDataHandlers.PlayerUpdate.Register(this.OnPlayerUpdate);
        ServerApi.Hooks.NetGetData.Register(this, OnNetGetData);
        ServerApi.Hooks.GameUpdate.Register(this, OnGameUpdate);
        TShockAPI.Commands.ChatCommands.Add(new Command($"{CmdName}.use", Commands.pout, CmdName));
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            GeneralHooks.ReloadEvent -= ReloadConfig;
            ServerApi.Hooks.GamePostInitialize.Deregister(this, this.GamePost);
            ServerApi.Hooks.ServerJoin.Deregister(this, OnServerJoin);
            ServerApi.Hooks.NetGreetPlayer.Deregister(this, this.OnGreetPlayer);
            ServerApi.Hooks.ServerLeave.Deregister(this, this.OnServerLeave);
            GetDataHandlers.PlayerUpdate.UnRegister(this.OnPlayerUpdate);
            ServerApi.Hooks.NetGetData.Deregister(this, OnNetGetData);
            ServerApi.Hooks.GameUpdate.Deregister(this, OnGameUpdate);
            TShockAPI.Commands.ChatCommands.RemoveAll(x => x.CommandDelegate == Commands.pout);
        }
        base.Dispose(disposing);
    }
    #endregion

    #region 配置重载读取与写入方法
    private static void ReloadConfig(ReloadEventArgs args = null!)
    {
        LoadConfig();
        args.Player.SendMessage($"[{PluginName}]重新加载配置完毕。", color);
    }
    private static void LoadConfig()
    {
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
        TShock.Log.ConsoleInfo($"1.导入或导出玩家强制开荒存档、自动备份存档");
        TShock.Log.ConsoleInfo($"2.自动注册、跨版本进服、修复地图区块缺失");
        TShock.Log.ConsoleInfo($"3.批量改权限、导出权限表、批量删文件、复制文件");
        TShock.Log.ConsoleInfo($"4.进服公告、自动建GM组、自动配权、重置服务器数据");
        TShock.Log.ConsoleInfo($"指令/{CmdName} 权限:{CmdName}.use");
        TShock.Log.ConsoleInfo($"显示重置服务器流程: /{CmdName} reset");
        TShock.Log.ConsoleInfo($"显示修复地图区块缺失流程: /{CmdName} fix");
        TShock.Log.ConsoleInfo($"《如出现因为施加buff给npc被踢出》");
        TShock.Log.ConsoleInfo($"可临时将玩家提升vip组 /user group 玩家名 vip");
        TShock.Log.ConsoleInfo($"请自行甄别玩家是否开挂,否则后果自负");
        TShock.Log.ConsoleInfo($"配置文件路径:{ConfigPath}");

        Console.WriteLine(string.Empty);

        if (Config.PostCMD.Any())
        {
            Console.WriteLine($"[开服执行指令]");
            Commands.DoCommand(TSPlayer.Server, Config.PostCMD);
            Console.WriteLine(string.Empty);
        }

        if (Config.AutoFixWorld)
        {
            Console.WriteLine($"[自动修复地图区块缺失]");
            Commands.ExecuteFix(TSPlayer.Server, false);
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

        if (Config.AutoPerm)
        {
            Console.WriteLine($"[自动配权提醒]");
            Commands.ManagePerm(TSPlayer.Server, true);
            TShock.Log.ConsoleInfo($"如果不需要可批量移除:/pout del");
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

    #region 加入服务器自动注册
    private void OnServerJoin(JoinEventArgs args)
    {
        if (!Config.AutoRegister) return;

        var plr = TShock.Players[args.Who];
        if (plr == null || plr == TSPlayer.Server) return;

        var user = TShock.UserAccounts.GetUserAccountByName(plr.Name);

        if (user is null)
        {
            var group = TShock.Config.Settings.DefaultRegistrationGroupName;
            var NewUser = new UserAccount(plr.Name, Config.DefPass, plr.UUID, group,
                                          DateTime.UtcNow.ToString("s"),
                                          DateTime.UtcNow.ToString("s"), "");

            try
            {
                // 给密码上个哈希，不然玩家改不了密码
                NewUser.CreateBCryptHash(Config.DefPass);
                TShock.UserAccounts.AddUserAccount(NewUser);
                plr.SetData("Register", true);
            }
            catch (Exception ex)
            {
                TShock.Log.ConsoleError($"[{PluginName}] 自动注册失败 [{plr.Name}]: {ex.Message}");
            }
        }
    }
    #endregion

    #region 创建玩家数据方法
    private void OnGreetPlayer(GreetPlayerEventArgs args)
    {
        var plr = TShock.Players[args.Who];
        if (plr is null || !plr.RealPlayer ||
            !plr.Active || !plr.IsLoggedIn)
        {
            return;
        }

        plr.SetData("Join", true);
    }
    #endregion

    #region 玩家离开服务器事件
    private void OnServerLeave(LeaveEventArgs args)
    {
        var plr = TShock.Players[args.Who];
        if (plr is null) return;

        if (plr.GetData<bool>("Join"))
            plr.RemoveData("Join");

        if (plr.GetData<bool>("Register"))
            plr.RemoveData("Register");
    }
    #endregion

    #region 玩家更新事件推送进服公告
    private void OnPlayerUpdate(object? sender, GetDataHandlers.PlayerUpdateEventArgs e)
    {
        var plr = e.Player;
        if (plr is null || !plr.RealPlayer ||
            !plr.Active || !plr.IsLoggedIn)
            return;

        // 进服公告
        if (plr.GetData<bool>("Join"))
        {
            if (Config.MotdState)
                plr.SendMessage($"{TextGradient(string.Join("\n", Config.MotdMess))}", color);

            plr.RemoveData("Join");
        }

        // 注册成功提示
        if (plr.GetData<bool>("Register"))
        {
            var regText = $"[{PluginName}] 已为您自动注册，默认密码为: {Config.DefPass}\n" +
                            $"使用指令修改密码: /password {Config.DefPass} 新密码\n" +
                            "[管]帮玩家修改密码: /user password 玩家名 新密码";

            plr.SendMessage(TextGradient(regText), color);
            plr.RemoveData("Register");
        }
    }
    #endregion

    #region 游戏更新事件，自动备份存档
    private long frame = 0;
    private void OnGameUpdate(EventArgs args)
    {
        if (!Config.AutoSavePlayer) return;

        frame++;

        // 每30分钟执行备份
        if (frame < Config.AutoSaveInterval * 60 * 60) return;

        // 执行备份
        WritePlayer.ExportAll(TSPlayer.Server, AutoSaveDir);

        frame = 0;

    }
    #endregion

    #region 跨版本进服方法
    private void OnNetGetData(GetDataEventArgs args)
    {
        if (args.MsgID != PacketTypes.ConnectRequest || !Config.NoVisualLimit) return;

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
    #endregion

}