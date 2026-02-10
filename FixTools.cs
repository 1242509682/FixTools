using Microsoft.Xna.Framework;
using On.Terraria.GameContent;
using Terraria;
using Terraria.ID;
using Terraria.Net;
using TerrariaApi.Server;
using TShockAPI;
using TShockAPI.DB;
using TShockAPI.Hooks;
using static FixTools.PlayerState;
using static FixTools.Utils;

namespace FixTools;

[ApiVersion(2, 1)]
public partial class FixTools : TerrariaPlugin
{
    #region 插件信息
    public override string Name => PluginName;
    public override string Author => "羽学";
    public override Version Version => new(2026, 2, 11);
    public override string Description => "本插件仅TShock测试版期间维护,指令/pout";
    #endregion

    #region 静态变量
    public static string PluginName => "145修复小公举"; // 插件名称
    public static string pt => "pout"; // 主指令名称
    public static string bak => "bak"; // 投票指令名
    public static string TShockVS => "c5a1747"; // 适配版本号
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
        ServerApi.Hooks.ServerLeave.Register(this, this.OnServerLeave);
        GetDataHandlers.PlayerUpdate.Register(this.OnPlayerUpdate);
        GetDataHandlers.PlaceObject.Register(this.OnPlaceObject);
        ServerApi.Hooks.NetGetData.Register(this, OnNetGetData);
        ServerApi.Hooks.GameUpdate.Register(this, OnGameUpdate);
        ServerApi.Hooks.NpcAIUpdate.Register(this, OnNpcAIUpdate);
        ServerApi.Hooks.NpcStrike.Register(this, OnNPCStrike);
        ServerApi.Hooks.NpcKilled.Register(this, OnNPCKilled);
        ServerApi.Hooks.DropBossBag.Register(this, OnDropBossBag);
        ServerApi.Hooks.ServerChat.Register(this, this.OnChat);
        CraftingRequests.CanCraftFromChest += OnCanCraftFromChest;

        TShockAPI.Commands.ChatCommands.Add(new Command($"{pt}.use", PoutCmd.Pouts, pt, "pt"));
        TShockAPI.Commands.ChatCommands.Add(new Command(string.Empty, BakCmd.bakCmd, bak));
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            GeneralHooks.ReloadEvent -= ReloadConfig;
            ServerApi.Hooks.GamePostInitialize.Deregister(this, this.GamePost);
            ServerApi.Hooks.ServerJoin.Deregister(this, OnServerJoin);
            ServerApi.Hooks.ServerLeave.Deregister(this, this.OnServerLeave);
            GetDataHandlers.PlayerUpdate.UnRegister(this.OnPlayerUpdate);
            GetDataHandlers.PlaceObject.UnRegister(this.OnPlaceObject);
            ServerApi.Hooks.NetGetData.Deregister(this, OnNetGetData);
            ServerApi.Hooks.GameUpdate.Deregister(this, OnGameUpdate);
            ServerApi.Hooks.NpcAIUpdate.Deregister(this, OnNpcAIUpdate);
            ServerApi.Hooks.NpcStrike.Deregister(this, OnNPCStrike);
            ServerApi.Hooks.NpcKilled.Deregister(this, OnNPCKilled);
            ServerApi.Hooks.DropBossBag.Deregister(this, OnDropBossBag);
            ServerApi.Hooks.ServerChat.Deregister(this, this.OnChat);
            CraftingRequests.CanCraftFromChest -= OnCanCraftFromChest;

            TShockAPI.Commands.ChatCommands.RemoveAll(x =>
            x.CommandDelegate == PoutCmd.Pouts ||
            x.CommandDelegate == BakCmd.bakCmd);
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
        if (!Directory.Exists(WritePlayer.WritePlrDir))
            Directory.CreateDirectory(WritePlayer.WritePlrDir);
        // 创建导入存档文件夹
        if (!Directory.Exists(ReaderPlayer.ReaderPlrDir))
            Directory.CreateDirectory(ReaderPlayer.ReaderPlrDir);
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
        TShock.Log.ConsoleInfo($"1.导入或导出玩家强制开荒存档、自动备份存档");
        TShock.Log.ConsoleInfo($"2.进服公告、跨版本进服、自动修复地图区块缺失");
        TShock.Log.ConsoleInfo($"3.批量改权限、导出权限表、批量删文件、复制文件");
        TShock.Log.ConsoleInfo($"4.自动注册、自动建GM组、自动配权、进度锁、重置服务器");
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

    #region 加入服务器自动注册
    private void OnServerJoin(JoinEventArgs args)
    {
        var hasCaibot = ServerApi.Plugins.Any(p => p.Plugin.Name == "CaiBotLitePlugin");
        if (!Config.AutoRegister || hasCaibot) return;

        var plr = TShock.Players[args.Who];
        if (plr == null || plr == TSPlayer.Server) return;

        var user = TShock.UserAccounts.GetUserAccountByName(plr.Name);

        var motd = GetData(plr.Name);
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

                motd.Register = true;
            }
            catch (Exception ex)
            {
                TShock.Log.ConsoleError($"[{PluginName}] 自动注册失败 [{plr.Name}]: {ex.Message}");
            }
        }
        else
        {
            motd.Motd = 1;
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

            // 如果离开的玩家是申请人，取消申请
            if (BakCmd.curName == plr.Name)
            {
                TSPlayer.All.SendMessage(TextGradient($"[{PluginName}] {plr.Name} 离开,申请已取消"), color);
                BakCmd.ClearApply();
            }
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
            var regText = $"\n[{PluginName}] 已为您自动注册，默认密码为: {Config.DefPass}\n" +
                            $"使用指令修改密码: /password {Config.DefPass} 新密码\n";

            plr.SendMessage(TextGradient(regText), color);
            TShock.Log.ConsoleInfo($"[{PluginName}]");
            TShock.Log.ConsoleInfo($"自动为玩家 {plr.Name} 注册账号,密码为 {Config.DefPass}");
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
        {
            StartInvasion(plr);
        }
    }
    #endregion

    #region 游戏更新事件，自动备份存档
    private static long frame = 0;  // 自动备份计时器
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
                plr.SendMessage(TextGradient($"[{PluginName}] 因击败[c/FF5149:{npc.FullName}]正为你自动复活!"), color);
            }
        }

        var itemID = args.ItemId;
        var ItemStack = args.Stack;
        var Icon = ItemIcon(itemID, ItemStack);

        // 发送击败消息
        string mess = $"\n[{PluginName}]\n" +
                     $"恭喜大家击败了[c/FF5149:{npc.FullName}]掉落了{Icon}\n" +
                     $"获取输出排名:[c/FFFFFF:/boss伤害]\n";

        if (Config.AllowTpBagText is not null && Config.AllowTpBagText.Count > 0)
        {
            mess += $"发送消息: [c/FF6962:{string.Join(" 或 ", Config.AllowTpBagText)}] 将传送到宝藏袋位置 ";
        }

        TSPlayer.All.SendMessage(TextGradient(string.Join("\n", mess)), color);
    }

    // 处理宝藏袋传送
    private void TpBag(string text, TSPlayer plr)
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
                TpBag(Text, plr);
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

    #region 修复使用物品召唤事件
    private static readonly object InvtLock = new(); // 入侵事件锁，确保同一时间只有一个入侵被触发
    private static void StartInvasion(TSPlayer plr)
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
                case 2: // 霜月入侵
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

        // FixNpcBuffKick(args);
    }
    #endregion

    #region 修复天塔柱刷物品BUG（放置BUG流程为：4个金属锭、1个金箱、挖掉底下2个金属锭、底部放工作台、挖掉金箱、放置天塔柱）
    private void OnPlaceObject(object? sender, GetDataHandlers.PlaceObjectEventArgs e)
    {
        var plr = e.Player;
        if (plr is null || !plr.RealPlayer || !plr.Active || !plr.IsLoggedIn || !Config.FixPlaceObject)
            return;

        // 检查是否为天塔柱等目标物品
        if (TargetItem(e.Type))
        {
            // 检查下方一格
            int checkY = e.Y + 1;
            if (checkY < Main.maxTilesY && Main.tile[e.X, checkY].active())
            {
                // 获取下方的图格对象
                var tile = Main.tile[e.X, checkY];

                // 如果下方是金属锭或传送机
                if (tile.type == TileID.MetalBars || tile.type == TileID.Teleporter)
                {
                    // 清理金属锭和上面的天塔柱
                    ClearBugTiles(plr, e.X, e.Y, e.X, checkY);
                }
            }
        }
    }

    private void ClearBugTiles(TSPlayer plr, int topX, int topY, int botX, int botY)
    {
        // 清理天塔柱
        Main.tile[topX, topY].ClearEverything();

        // 清理金属锭
        Main.tile[botX, botY].ClearEverything();
        Main.tile[botX - 1, botY].ClearEverything();

        // 发送更新给所有玩家
        TSPlayer.All.SendTileSquareCentered(topX, topY, 2);

        for (int i = 0; i < TShock.Players.Length; i++)
        {
            for (int j = 0; j < Main.maxSectionsX; j++)
            {
                for (int k = 0; k < Main.maxSectionsY; k++)
                {
                    Netplay.Clients[i].TileSections[j, k] = false;
                }
            }
        }

        plr.SendMessage(TextGradient("[{插件名}] 检测到刷物品BUG！正在清理..."), color);
    }

    private bool TargetItem(short type)
    {
        // 目标物品列表
        int[] targetList =
        {

            TileID.LunarMonolith,      // 天塔柱
            TileID.WaterFountain,      // 喷泉
            TileID.Cannon,             // 各种大炮
            TileID.SnowballLauncher,   // 雪球发射器
            TileID.MusicBoxes,         // 八音盒
            TileID.BloodMoonMonolith,  // 血月天塔柱
            TileID.ShimmerMonolith,    // 以太天塔柱
            TileID.ShadowCandle,       // 暗影蜡烛
            TileID.PeaceCandle,        // 和平蜡烛
            TileID.Candelabras,        // 烛台
            TileID.PlatinumCandelabra, // 铂金烛台
            TileID.PlatinumCandelabra, // 铂金蜡烛
            TileID.Lamps,              // 柱式灯
            TileID.Lever,              // 遥控杆
        };

        return Array.IndexOf(targetList, type) >= 0;
    }
    #endregion

}