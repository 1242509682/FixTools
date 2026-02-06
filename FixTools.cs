using System.Collections.Concurrent;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.Net;
using TerrariaApi.Server;
using TShockAPI;
using TShockAPI.DB;
using TShockAPI.Hooks;
using static FixTools.Utils;
using On.Terraria.GameContent;
using NuGet.Packaging;
using System.ComponentModel;

namespace FixTools;

[ApiVersion(2, 1)]
public partial class FixTools : TerrariaPlugin
{
    #region 插件信息
    public override string Name => PluginName;
    public override string Author => "羽学";
    public override Version Version => new(2026, 2, 7);
    public override string Description => "本插件仅TShock测试版期间维护,指令/pout";
    #endregion

    #region 静态变量
    public static string PluginName => "145修复小公举"; // 插件名称
    public static string CmdName => "pout"; // 指令名称
    public static string TShockVS => "c5a1747"; // 适配版本号
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
    // 存储需要恢复存档的玩家数据,用于死亡复活后或死亡退出回服后恢复存档（键：玩家名，值：PlayerData）
    public static ConcurrentDictionary<string, PlayerData> NeedRestores = new();
    private static Dictionary<string, List<Vector2>> BagPos = new(); // 每个玩家的宝藏袋位置,使用后进先出
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
        ServerApi.Hooks.NpcAIUpdate.Register(this, OnNpcAIUpdate);
        ServerApi.Hooks.NpcStrike.Register(this, OnNPCStrike);
        ServerApi.Hooks.NpcKilled.Register(this, OnNPCKilled);
        ServerApi.Hooks.DropBossBag.Register(this, OnDropBossBag);
        ServerApi.Hooks.ServerChat.Register(this, this.OnChat);
        CraftingRequests.CanCraftFromChest += OnCanCraftFromChest;
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
            ServerApi.Hooks.NpcAIUpdate.Deregister(this, OnNpcAIUpdate);
            ServerApi.Hooks.NpcStrike.Deregister(this, OnNPCStrike);
            ServerApi.Hooks.NpcKilled.Deregister(this, OnNPCKilled);
            ServerApi.Hooks.DropBossBag.Deregister(this, OnDropBossBag);
            ServerApi.Hooks.ServerChat.Deregister(this, this.OnChat);
            CraftingRequests.CanCraftFromChest -= OnCanCraftFromChest;
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
        TShock.Log.ConsoleInfo($"2.进服公告、跨版本进服、自动修复地图区块缺失");
        TShock.Log.ConsoleInfo($"3.批量改权限、导出权限表、批量删文件、复制文件");
        TShock.Log.ConsoleInfo($"4.自动注册、自动建GM组、自动配权、进度锁、重置服务器");
        TShock.Log.ConsoleInfo($"指令/{CmdName} 权限:{CmdName}.use");
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

    #region 获取已经登录玩家事件，设置进服公告标记
    private void OnGreetPlayer(GreetPlayerEventArgs args)
    {
        var plr = TShock.Players[args.Who];
        if (plr is null || !plr.RealPlayer ||
            !plr.Active || !plr.IsLoggedIn)
        {
            return;
        }

        if (Config.MotdState)
            plr.SetData("motd", true);
    }
    #endregion

    #region 玩家离开服务器事件
    private void OnServerLeave(LeaveEventArgs args)
    {
        var plr = TShock.Players[args.Who];
        if (plr is null) return;

        if (plr.GetData<bool>("motd"))
            plr.RemoveData("motd");

        if (plr.GetData<bool>("motd2"))
            plr.RemoveData("motd2");

        if (plr.GetData<DateTime?>("motd3").HasValue)
            plr.RemoveData("motd3");

        if (plr.GetData<bool>("Register"))
            plr.RemoveData("Register");

        if (BagPos.ContainsKey(plr.Name))
            BagPos.Remove(plr.Name);
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
        if (plr.GetData<bool>("motd"))
        {
            if (Config.MotdMess.Any())
                plr.SendMessage($"{TextGradient(string.Join("\n", Config.MotdMess), plr: plr)}", color);

            plr.RemoveData("motd");
            plr.SetData("motd2", true); // 显示更多信息
        }

        // 注册成功提示
        if (plr.GetData<bool>("Register"))
        {
            var regText = $"\n[{PluginName}] 已为您自动注册，默认密码为: {Config.DefPass}\n" +
                            $"使用指令修改密码: /password {Config.DefPass} 新密码\n";

            plr.SendMessage(TextGradient(regText), color);
            TShock.Log.ConsoleInfo($"[{PluginName}]");
            TShock.Log.ConsoleInfo($"自动为玩家 {plr.Name} 注册账号,密码为 {Config.DefPass}");
            TShock.Log.ConsoleInfo($"帮玩家修改密码: /user password {plr.Name} 新密码");
            plr.RemoveData("Register");
        }

        // 检查是否需要恢复存档（玩家复活后）
        if (NeedRestores.TryGetValue(plr.Name, out var data) && !plr.Dead)
        {
            try
            {
                data?.RestoreCharacter(plr);
                plr.SendMessage($"已自动为你恢复存档物品!", color);
            }
            finally
            {
                NeedRestores.TryRemove(plr.Name, out _);
            }
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

    #region 人数进度锁
    private void OnNpcAIUpdate(NpcAiUpdateEventArgs args)
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

    private void OnNPCStrike(NpcStrikeEventArgs args)
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

    private void OnNPCKilled(NpcKilledEventArgs args)
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

    #region 宝藏袋掉落事件
    private void OnDropBossBag(DropBossBagEventArgs args)
    {
        if (!Config.TpBag) return;
        var npc = Main.npc[args.NpcArrayIndex];

        // 计算宝藏袋位置（Boss死亡位置）
        Vector2 bagPos = new Vector2(args.Position.X, args.Position.Y);
        var plrs = TShock.Players.Where(p => p != null && p.RealPlayer && p.Active && p.IsLoggedIn).ToList();
        foreach (var plr in plrs)
        {
            // 初始化玩家的位置列表
            if (!BagPos.ContainsKey(plr.Name))
            {
                BagPos[plr.Name] = new List<Vector2>();
            }

            // 将新的宝藏袋位置添加到列表末尾（最近的在最后）
            BagPos[plr.Name].Add(bagPos);

            // 限制列表大小，只保留最近10个
            if (BagPos[plr.Name].Count > 10)
            {
                // 移除最早的元素（索引0）
                BagPos[plr.Name].RemoveAt(0);
            }

            // 处理死亡玩家复活
            if (plr.Dead)
            {
                plr.RespawnTimer = 0; // 立即复活
                plr.Spawn(PlayerSpawnContext.ReviveFromDeath); // 触发复活
                plr.SendMessage(TextGradient($"[{PluginName}] 因击败[c/FF5149:{npc.FullName}]正为你自动复活!"), color);
            }
        }

        var itemID = args.ItemId;
        var ItemStack = args.Stack;
        var Icon = ItemIcon(itemID, ItemStack);


        // 发送击败消息
       string mess =$"\n[{PluginName}]\n" +
                    $"恭喜大家击败了[c/FF5149:{npc.FullName}]掉落了{Icon}\n" +
                    $"获取输出排名:[c/FFFFFF:/boss伤害]\n";

        if (Config.AllowTpBagText is not null && Config.AllowTpBagText.Count > 0)
        {
            mess += $"发送消息: [c/FF6962:{string.Join(" 或 ", Config.AllowTpBagText)}] 将传送到宝藏袋位置 ";
        }

        TShock.Utils.Broadcast(TextGradient(string.Join("\n", mess)), color);
    }

    // 处理宝藏袋传送
    private void TpBag(string text, TSPlayer plr)
    {
        if (!Config.TpBag)
        {
            plr.SendMessage(TextGradient("宝藏袋传送功能未启用!"), color);
            return;
        }

        // 检查玩家是否有位置列表
        if (!BagPos.ContainsKey(plr.Name) || BagPos[plr.Name].Count == 0)
        {
            plr.SendMessage(TextGradient("当前[c/FF5149:没有可用的]宝藏袋位置!"), color);
            return;
        }

        // 获取并移除列表的最后一个元素（最近的位置）
        int lastIndex = BagPos[plr.Name].Count - 1;
        Vector2 bagPos = BagPos[plr.Name][lastIndex];
        plr.Teleport(bagPos.X, bagPos.Y);
        BagPos[plr.Name].RemoveAt(lastIndex);

        // 显示剩余位置数量
        var mess = BagPos[plr.Name].Count > 0 ? $"[{PluginName}] 还有 [c/3FAEDB:{BagPos.Count}] 个宝藏袋位置可用" :
                                                $"[{PluginName}] 这是 [c/FF534A:最后一个] 宝藏袋位置";

        plr.SendMessage(TextGradient(mess), color);
    }
    #endregion

    #region 玩家聊天事件显示功能相关信息（交互型方法）
    private void OnChat(ServerChatEventArgs args)
    {
        var plr = TShock.Players[args.Who];
        if (plr is null || !plr.RealPlayer ||
           !plr.Active || !plr.IsLoggedIn)
            return;

        // 显示功能信息
        var Text = args.Text;

        // 检查是否为命令
        if (Text.StartsWith(TShock.Config.Settings.CommandSpecifier) ||
            Text.StartsWith(TShock.Config.Settings.CommandSilentSpecifier))
            return;

        // 检查是否输入"宝藏袋"相关传送
        if (Config.AllowTpBagText is not null && Config.AllowTpBagText.Count > 0)
        {
            // 检查是否包含任意一个关键词
            if (Config.AllowTpBagText.Any(word => Text.Contains(word)))
            {
                TpBag(Text, plr);
                return;
            }
        }

        // 原有的Motd显示逻辑
        if (!Config.MotdState)
            return;

        if (plr.GetData<bool>("motd2"))
        {
            if (Config.MotdMess2.Any())
                plr.SendMessage($"{TextGradient(string.Join("\n", Config.MotdMess2), plr: plr)}", color);
            plr.RemoveData("motd2");
            plr.SetData("motd3", DateTime.Now);
        }

        TimeSpan sendTime = DateTime.Now - plr.GetData<DateTime?>("motd3")!.Value;
        if (sendTime.TotalSeconds > 1)
        {
            if (Config.MotdMess3.Any())
                plr.SendMessage($"{TextGradient(string.Join("\n", Config.MotdMess3), plr: plr)}", color);
            plr.RemoveData("motd3");
        }
    }
    #endregion

    #region 阻止合成区域内附近箱子方法（判断范围600像素 ≈ 37.5格）
    private bool OnCanCraftFromChest(On.Terraria.GameContent.CraftingRequests.orig_CanCraftFromChest orig, Chest chest, int whoAmI)
    {
        // 首先调用原方法进行基础检查
        bool Result = orig(chest, whoAmI);

        // 配置项关闭 不拦截
        if (!Config.NoUseRgionCheat) return Result;

        // 如果原方法已经返回false，直接返回false
        if (!Result) return false;

        // 获取玩家对象
        TSPlayer plr = TShock.Players[whoAmI];
        if (plr == null || !plr.Active || !plr.RealPlayer) return Result;

        // 检查玩家是否有超级管理员权限
        if (Config.AllowRegionGroup.Contains(plr.Group.Name) ||
            plr.Group.HasPermission("*")) return Result;

        // 1. 检查箱子是否在任何区域内
        var ChestRegions = GetChest(chest);
        if (ChestRegions.Count == 0) return Result; // 箱子不在任何区域内，允许合成

        // 2. 检查玩家是否有权限访问箱子所在的任何一个区域
        bool hasPerm = false;
        Region? firstRegion = null;

        foreach (var region in ChestRegions)
        {
            firstRegion = region; // 记录第一个区域用于消息

            if (plr.Name == region.Owner ||
                region.AllowedGroups.Contains(plr.Group.Name) ||
                region.AllowedIDs.Contains(plr.Account.ID))
            {
                hasPerm = true;
                break;
            }
        }

        // 3. 如果玩家有权限，允许合成
        if (hasPerm) return Result;

        // 4. 玩家没有权限，检查箱子是否在玩家范围内
        if (!IsChestInRange(chest, plr)) return Result;

        // 5. 箱子在受保护区域内、玩家无权限、且在范围内，阻止合成
        plr.SendMessage(TextGradient($"[{PluginName}] 箱子在保护区域 [c/FF5149:{firstRegion?.Name}] 中，你无权合成！"), color);

        return false;
    }

    // 获取箱子所在的所有区域
    private List<Region> GetChest(Chest chest)
    {
        List<Region> regions = new List<Region>();

        // 箱子占据的图格范围（箱子是2x2的）
        Rectangle chestArea = new Rectangle(chest.x, chest.y, 2, 2);

        foreach (var region in TShock.Regions.Regions)
        {
            if (region.Area.Intersects(chestArea))
            {
                regions.Add(region);
            }
        }

        return regions;
    }

    // 检查箱子是否在玩家范围内
    private bool IsChestInRange(Chest chest, TSPlayer plr)
    {
        Vector2 pos = new Vector2(chest.x * 16 + 16, chest.y * 16 + 16);

        // 使用平方距离避免开方运算
        float distanceSq = Vector2.DistanceSquared(plr.TPlayer.Center, pos);

        // Terraria默认合成范围600像素（平方值：360000）
        // 详情看Terraria.GameContent.NearbyChests.GetChestsInRangeOf方法
        float maxDistanceSq = (Config.NoUseCheatRange * 16) * (Config.NoUseCheatRange * 16);

        return distanceSq <= maxDistanceSq;
    }
    #endregion
}