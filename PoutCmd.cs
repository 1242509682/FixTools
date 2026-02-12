using System.IO.Compression;
using System.Text;
using Terraria;
using Terraria.ID;
using TerrariaApi.Server;
using TShockAPI;
using TShockAPI.DB;
using static FixTools.FixTools;
using static FixTools.Utils;

namespace FixTools;

internal class PoutCmd
{
    #region 菜单方法
    private static void HelpCmd(TSPlayer plr)
    {
        // 先构建消息内容
        if (plr.RealPlayer)
        {
            plr.SendMessage("\n[i:3455][c/AD89D5:修][c/D68ACA:复][c/DF909A:公][c/E5A894:举][i:3454] " +
            "[i:3456][C/F2F2C7:开发] [C/BFDFEA:by] [c/00FFFF:羽学] [i:3459]", color);

            var mess = new StringBuilder();
            mess.AppendLine($"/{pt} plr ——玩家存档管理菜单");
            mess.AppendLine($"/{pt} save ——自动备份菜单");
            mess.AppendLine($"/{pt} vote ——投票回档开关");
            mess.AppendLine($"/{bak} ——投票回档功能");
            mess.AppendLine($"/{pt} rw ——修复局部图格");
            mess.AppendLine($"/{pt} vs ——设置导出版本号");
            mess.AppendLine($"/{pt} join ——跨版本进服开关");
            mess.AppendLine($"/{pt} bag ——宝藏袋传送开关");
            mess.AppendLine($"/{pt} chest ——禁用区域箱子材料");
            mess.AppendLine($"/{pt} motd ——进服公告开关");
            mess.AppendLine($"/{pt} fix ——修复地图区块缺失");
            mess.AppendLine($"/{pt} inv ——修复物品召唤入侵事件");
            mess.AppendLine($"/{pt} ttz ——修复天塔柱刷物品BUG");
            mess.AppendLine($"/{pt} cb ——复制文件");
            mess.AppendLine($"/{pt} rm ——删除文件");
            mess.AppendLine($"/{pt} sql ——改数据表");
            mess.AppendLine($"/{pt} cmd ——执行指令");
            mess.AppendLine($"/{pt} reg ——自动注册");
            mess.AppendLine($"/{pt} add ——批量加权限");
            mess.AppendLine($"/{pt} del ——批量删权限");
            mess.AppendLine($"/{pt} lp ——导出权限表");
            mess.AppendLine($"/{pt} boss ——人数进度锁");
            mess.AppendLine($"/{pt} reset ——重置服务器");
            GradMess(plr, mess);
        }
        else
        {
            plr.SendMessage($"《{PluginName}》\n" +
                            $"/{pt} plr ——玩家存档管理菜单\n" +
                            $"/{pt} save ——自动备份菜单\n" +
                            $"/{pt} vote ——投票回档开关\n" +
                            $"/{bak} ——投票回档功能\n" +
                            $"/{pt} rw ——修复局部图格\n" +
                            $"/{pt} vs ——设置导出版本号\n" +
                            $"/{pt} join ——跨版本进服开关\n" +
                            $"/{pt} bag ——宝藏袋传送开关\n" +
                            $"/{pt} chest ——禁用区域箱子材料\n" +
                            $"/{pt} motd ——进服公告开关\n" +
                            $"/{pt} fix ——修复地图区块缺失\n" +
                            $"/{pt} inv ——修复物品召唤入侵事件\n" +
                            $"/{pt} ttz ——修复天塔柱刷物品BUG\n" +
                            $"/{pt} copy ——复制文件\n" +
                            $"/{pt} rm ——删除文件\n" +
                            $"/{pt} sql ——改数据表\n" +
                            $"/{pt} cmd ——执行游戏时指令\n" +
                            $"/{pt} reg ——自动注册开关\n" +
                            $"/{pt} add ——批量加权限\n" +
                            $"/{pt} del ——批量删权限\n" +
                            $"/{pt} lp ——导出权限表\n" +
                            $"/{pt} boss ——人数进度锁\n" +
                            $"/{pt} reset ——重置服务器", color);
        }

        var AutoBoss = Config.ProgressLock ? "已开启" : "已禁用";
        var PlayerCount = TShock.Utils.GetActivePlayerCount();
        plr.SendMessage(TextGradient($"进度锁:{AutoBoss} 人数:{PlayerCount}/{Config.UnLockCount}人"), color);
        var GameVersion = Config.GameVersion == -1 ? GameVersionID.Latest : Config.GameVersion;
        var vsText = GetVSText(GameVersion);
        plr.SendMessage($"当前导出版本号：{vsText}", color);
        plr.SendMessage($"注:本插件仅ts临时版期间维护,后续将不再更新", color2);
    }
    #endregion

    #region 统一布尔配置项切换方法
    private static void SetBool(string desc, TSPlayer plr, Func<bool> getVal, Action<bool> setVal)
    {
        bool cur = getVal();
        bool newVal = !cur;
        setVal(newVal);
        Config.Write();
        var state = newVal ? "开启" : "关闭";
        plr.SendMessage($"{desc}已切换为 {state}", color);
    }
    #endregion

    #region 统一设置数值配置方法
    private static void SetNum(string desc, TSPlayer plr, Action<int> setVal, string num, string unit = "")
    {
        if (!int.TryParse(num, out int val))
        {
            plr.SendMessage($"请输入正确数字 如: {desc} {unit}", color);
            return;
        }

        setVal(val);
        Config.Write();
        plr.SendMessage($"{desc}已设置为 {val}{unit}", color);
    }

    private static void SetFloat(string desc, TSPlayer plr, Action<float> setVal, string num, string unit = "")
    {
        if (!float.TryParse(num, out float val))
        {
            plr.SendMessage($"请输入正确数字 如: {desc} {unit}", color);
            return;
        }

        setVal(val);
        Config.Write();
        plr.SendMessage($"{desc}已设置为 {val}{unit}", color);
    }
    #endregion

    #region 主指令方法
    internal static void Pouts(CommandArgs args)
    {
        var plr = args.Player;

        //子命令数量为0时
        if (args.Parameters.Count == 0)
        {
            HelpCmd(plr);
        }
        else if (args.Parameters.Count >= 1)
        {
            switch (args.Parameters[0].ToLower())
            {
                case "快照":
                case "rw":
                    {
                        WorldTile.DoSnapshot(args, plr);
                    }
                    break;

                case "p":
                case "plr":
                case "玩家存档":
                    {
                        if (args.Parameters.Count < 2)
                        {
                            ShowPlayerHelp(plr);
                            return;
                        }

                        // 根据第二个参数判断是导出(c)还是导入(r)
                        switch (args.Parameters[1].ToLower())
                        {
                            case "c": // 导出命令
                                ExportPlayer(args, plr);
                                break;

                            case "r": // 导入命令
                                ImportPlayer(args, plr);
                                break;

                            case "z": // 备份还原操作
                                ZipPlayer(args, plr);
                                break;

                            default:
                                ShowPlayerHelp(plr);
                                break;
                        }
                    }
                    break;

                case "j":
                case "join":
                case "跨版本":
                    SetBool("跨版本进服", plr, () => Config.NoVisualLimit, (val) => Config.NoVisualLimit = val);
                    break;

                case "版本":
                case "vs":
                case "version":
                    SetGameVersion(args, plr);
                    break;

                case "公告":
                case "motd":
                    SetBool("进服公告", plr, () => Config.MotdEnabled, (val) => Config.MotdEnabled = val);
                    break;

                case "bag":
                case "宝藏袋":
                    SwitchTPBag(args, plr);
                    break;

                case "修复区域箱":
                case "cheat":
                case "chest":
                    SwitchNoUseRgionCheat(args, plr);
                    break;

                case "自动注册":
                case "register":
                case "reg":
                case "ar":
                    AutoReg(args, plr);
                    break;

                case "addperm":
                case "add":
                case "加权":
                    ManagePerm(plr, true);
                    break;

                case "delperm":
                case "del":
                case "删权":
                    ManagePerm(plr, false);
                    break;

                case "listperm":
                case "lpm":
                case "lp":
                case "列出":
                    ListPerm(plr);
                    break;

                case "改数据":
                case "sql":
                    ClearSqlInfo(args,plr);
                    break;

                case "命令":
                case "cmd":
                    DoCommand(plr, Config.GameCMD);
                    break;

                case "复制":
                case "复制文件":
                case "cb":
                case "copy":
                    CopyFile(args, plr);
                    break;

                case "删文件":
                case "删除文件":
                case "rm":
                    DeleteFileInfo(args,plr);
                    break;

                case "修复入侵":
                case "入侵":
                case "inv":
                    SetBool("修复物品召唤入侵事件", plr, () => Config.FixStartInvasion, (val) => Config.FixStartInvasion = val);
                    break;

                case "天塔柱":
                case "ttz":
                    SetBool("修复天塔柱刷物品BUG", plr, () => Config.FixPlaceObject, (val) => Config.FixPlaceObject = val);
                    break;

                case "修复地图":
                case "fix":
                    FixWorld(args, plr);
                    break;

                case "自动备份":
                case "save":
                case "sv":
                    AutoSave(args, plr);
                    break;

                case "申请":
                case "回档":
                case "投票":
                case "vote":
                case "vt":
                    SetBool("申请回档投票", plr, () => Config.ApplyVote, (val) => Config.ApplyVote = val);
                    break;

                case "进度锁":
                case "pog":
                case "boss":
                    ProgressLock(args, plr);
                    break;

                case "重置":
                case "rs":
                case "reset":
                    Reset(args, plr);
                    break;

                default:
                    HelpCmd(plr);
                    break;
            };
        }
    }
    #endregion

    #region 自动备份指令
    private static void AutoSave(CommandArgs args, TSPlayer plr)
    {
        if (args.Parameters.Count < 2)
        {
            ShowSaveSet(plr);
            return;
        }

        switch (args.Parameters[1].ToLower())
        {
            case "on":
            case "off":
                SetBool("自动备份", plr, () => Config.AutoSavePlayer, (val) => Config.AutoSavePlayer = val);
                break;

            case "min":
            case "int":
            case "interval":
            case "分钟":
                if (args.Parameters.Count < 3)
                {
                    plr.SendMessage($"用法: /{pt} sv min <分钟数>", color);
                    plr.SendMessage($"当前间隔: {Config.AutoSaveInterval}分钟", color2);
                    return;
                }
                SetNum("备份间隔", plr, (val) => Config.AutoSaveInterval = val, args.Parameters[2], "分钟");
                break;

            case "cl":
            case "clear":
            case "clean":
            case "清理":
                SetBool("自动清理备份", plr, () => Config.AutoClean, (val) => Config.AutoClean = val);
                break;

            case "keep":
            case "保留":
                if (args.Parameters.Count < 3)
                {
                    plr.SendMessage($"用法: /{pt} save keep <数量>", color);
                    plr.SendMessage($"当前保留: {Config.MaxBackup}个", color2);
                    return;
                }
                SetNum("保留备份数量", plr, (val) => Config.MaxBackup = val, args.Parameters[2], "个");
                break;

            case "wld":
            case "world":
            case "地图":
                SetBool("自动备份地图", plr, () => Config.AutoSaveWorld, (val) => Config.AutoSaveWorld = val);
                break;

            case "sql":
            case "sqlite":
            case "数据库":
                SetBool("自动备份数据库", plr, () => Config.AutoSaveSqlite, (val) => Config.AutoSaveSqlite = val);
                break;

            case "mag":
            case "msg":
            case "mess":
            case "消息":
                SetBool("自动备份消息显示", plr, () => Config.ShowAutoSaveMsg, (val) => Config.ShowAutoSaveMsg = val);
                break;

            default:
                ShowSaveSet(plr);
                break;
        }
    }
    #endregion

    #region 显示备份设置
    private static void ShowSaveSet(TSPlayer plr)
    {
        var state1 = Config.AutoSavePlayer ? "开" : "关";
        var state2 = $"{Config.AutoSaveInterval}";
        var state3 = Config.AutoClean ? "开" : "关";
        var state4 = $"{Config.MaxBackup}";
        var state5 = Config.AutoSaveWorld ? "开" : "关";
        var state6 = Config.AutoSaveSqlite ? "开" : "关";
        var state7 = Config.ShowAutoSaveMsg ? "开" : "关";

        var mess = new StringBuilder();
        mess.AppendLine($"\n[c/AD89D5:自动备份设置]");
        mess.AppendLine($"[c/3FAEDB:备份][{state1}] /{pt} sv on");
        mess.AppendLine($"[c/3FAEDB:间隔][{state2}] /{pt} sv min <分钟>");
        mess.AppendLine($"[c/3FAEDB:清理][{state3}] /{pt} sv clear");
        mess.AppendLine($"[c/3FAEDB:保留][{state4}] /{pt} sv keep <数量>");
        mess.AppendLine($"[c/3FAEDB:地图][{state5}] /{pt} sv wld");
        mess.AppendLine($"[c/3FAEDB:数据][{state6}] /{pt} sv sql");
        mess.AppendLine($"[c/3FAEDB:消息][{state7}] /{pt} sv msg");

        if (plr.RealPlayer)
            plr.SendMessage(TextGradient(mess.ToString()), color);
        else
            plr.SendMessage(mess.ToString(), color);
    }
    #endregion

    #region 自动注册修改指令
    private static void AutoReg(CommandArgs args, TSPlayer plr)
    {
        var caibot = "CaiBotLitePlugin";
        var hasCaibot = ServerApi.Plugins.Any(p => p.Plugin.Name == caibot);
        if (hasCaibot)
        {
            plr.SendMessage($"检测到已安装 {caibot} 插件，本功能已禁用", color);
            return;
        }

        if (args.Parameters.Count < 2)
        {
            SetBool("自动注册", plr, () => Config.AutoRegister, (val) => Config.AutoRegister = val);
            plr.SendMessage($"改新玩家默认密码: /{pt} reg 新密码", color);
            return;
        }

        string NewPass = args.Parameters[1];
        int PassLength = TShock.Config.Settings.MinimumPasswordLength;

        // 检查密码长度是否少于4位
        if (NewPass.Length < PassLength)
        {
            plr.SendMessage($"密码长度不能少于{PassLength}位！当前长度：{NewPass.Length}", color);
            return;
        }

        var oldpass = Config.DefPass;
        Config.DefPass = NewPass;
        Config.Write();
        plr.SendMessage($"新玩家的默认密码已经从 {oldpass} => {NewPass}", color);
    }
    #endregion

    #region 人数进度锁指令
    private static void ProgressLock(CommandArgs args, TSPlayer plr)
    {
        if (args.Parameters.Count < 2)
        {
            SetBool("人数进度锁", plr, () => Config.ProgressLock, (val) => Config.ProgressLock = val);
            plr.SendMessage($"改解锁人数: /{pt} boss 人数", color);
            return;
        }

        int PlayerCount = TShock.Utils.GetActivePlayerCount();
        SetNum("解锁人数", plr, (val) => Config.UnLockCount = val, args.Parameters[1], "人");
    }
    #endregion

    #region 禁用区域箱子材料开关
    private static void SwitchNoUseRgionCheat(CommandArgs args, TSPlayer plr)
    {
        if (args.Parameters.Count < 2)
        {
            SetBool("禁用区域箱子材料", plr, () => Config.NoUseRgionCheat, (val) => Config.NoUseRgionCheat = val);
            plr.SendMessage($"改禁用范围: /{pt} chest 格数", color);
            return;
        }

        SetFloat("禁用范围", plr, (val) => Config.NoUseCheatRange = val, args.Parameters[1], "格");
    }
    #endregion

    #region 宝藏袋传送开关
    private static void SwitchTPBag(CommandArgs args, TSPlayer plr)
    {
        if (args.Parameters.Count < 2)
        {
            SetBool("宝藏袋传送", plr, () => Config.TpBagEnabled, (val) => Config.TpBagEnabled = val);
            plr.SendMessage($"修改关键词: /{pt} bag 关键词", color);
            plr.SendMessage($"存在则移除，不再则添加", color2);
            return;
        }

        string txt = args.Parameters[1];
        if (!string.IsNullOrEmpty(txt))
        {
            var state = Config.AllowTpBagText.Contains(txt) ? "[c/FF5149:移除]" : "[c/3FAEDB:添加]";
            if (Config.AllowTpBagText.Contains(txt))
            {
                Config.AllowTpBagText.Remove(txt);
            }
            else
            {
                Config.AllowTpBagText.Add(txt);
            }

            Config.Write();
            plr.SendMessage($"已{state}宝藏袋传送关键词: [c/F9FD49:{txt}]", color);
        }
    }
    #endregion

    #region 设置导出角色版本号
    private static void SetGameVersion(CommandArgs args, TSPlayer plr)
    {
        if (args.Parameters.Count < 2)
        {
            plr.SendErrorMessage("用法: /pout vs <版本号>");
            plr.SendMessage($"请输入正确的版本号数字，例如：\n{string.Join("\n", Config.Example)}", color);
            return;
        }

        SetNum("导出存档版本号", plr, (val) => Config.GameVersion = val, args.Parameters[1]);
    }
    #endregion

    #region 给配置组管理权限方法（统一方法）
    public static void ManagePerm(TSPlayer plr, bool isAdd)
    {
        if (Config.Permission is not null && Config.Permission.Count() > 0)
        {
            string action = isAdd ? "添加" : "删除";

            foreach (var kv in Config.Permission)
            {
                var name = kv.Key;
                var perm = kv.Value;
                var g = TShock.Groups.GetGroupByName(name);
                if (g == null)
                {
                    plr.SendMessage($"已跳过不存在的权限组 [{name}] ", color);
                    continue;
                }

                if (g.Name == "superadmin")
                {
                    plr.SendMessage($"跳过 superadmin 组(系统) 权限{action}", color);
                    continue;
                }

                // 当前权限
                var cur = new HashSet<string>(g.Permissions.Split(',').Select(p => p.Trim()).Where(p => !string.IsNullOrEmpty(p)));
                var changed = new List<string>();

                // 添加或删除权限
                foreach (var p in perm)
                {
                    var cp = p.Trim();
                    if (string.IsNullOrEmpty(cp)) continue;

                    if (isAdd)
                    {
                        if (!cur.Contains(cp))
                        {
                            g.AddPermission(cp);
                            cur.Add(cp);
                            changed.Add(cp);
                        }
                    }
                    else
                    {
                        if (cur.Contains(cp))
                        {
                            g.RemovePermission(cp);
                            cur.Remove(cp);
                            changed.Add(cp);
                        }
                    }
                }

                if (changed.Count > 0)
                {
                    // 更新组
                    TShock.Groups.UpdateGroup(g.Name, g.ParentName, string.Join(",", cur), g.ChatColor, g.Suffix, g.Prefix);
                    plr.SendMessage($"已为 {g.Name}组 {action} {changed.Count} 个权限:\n" +
                                    $"{string.Join(", \n", changed.Take(5))}" +
                                   (changed.Count > 5 ? $" 等 {changed.Count} 个权限\n" : ""), color);
                }
                else
                {
                    string status = isAdd ? "已添加" : "已删除";
                    plr.SendMessage($"{g.Name}组{status}配置文件中指定的所有权限", color);
                }
            }

            plr.SendMessage($"生成权限组文件查看当前权限: /pout lpm", color);
        }
        else
        {
            plr.SendMessage("配置文件中未设置任何权限", color);
        }
    }
    #endregion

    #region 列出权限表文件方法
    private static void ListPerm(TSPlayer plr)
    {
        try
        {
            string folderPath = Path.Combine(MainPath, "权限表");

            if (!Directory.Exists(folderPath))
                Directory.CreateDirectory(folderPath);

            string fileName = $"权限表_{DateTime.Now:yyyy-MM-dd_HH-mm}.txt";
            string filePath = Path.Combine(folderPath, fileName);

            var groups = TShock.Groups.groups;
            var content = new StringBuilder();

            content.AppendLine("《TShock服务器权限组列表》");
            content.AppendLine($"生成时间: {DateTime.Now:yyyy年MM月dd日 HH:mm:ss}");
            content.AppendLine($"服务器: {Main.worldName}");
            content.AppendLine("-".PadRight(50, '-') + "\n");

            foreach (var group in groups.OrderBy(g => g.Name))
            {
                if (group.Name == "superadmin") continue;

                content.AppendLine($"【{group.Name}】组");
                var parent = string.IsNullOrEmpty(group.ParentName) ? "无" : group.ParentName;
                content.AppendLine($"继承父级: {parent}");
                content.AppendLine($"聊天颜色: {group.ChatColor}");
                var prefix = string.IsNullOrEmpty(group.Prefix) ? "无" : group.Prefix;
                content.AppendLine($"聊天前缀: {prefix}");
                var suffix = string.IsNullOrEmpty(group.Suffix) ? "无" : group.Suffix;
                content.AppendLine($"聊天后缀: {suffix}");

                content.AppendLine("权限列表:");
                if (!string.IsNullOrEmpty(group.Permissions))
                {
                    var perms = group.Permissions.Split(',')
                        .Where(p => !string.IsNullOrWhiteSpace(p))
                        .OrderBy(p => p)
                        .ToList();

                    for (int i = 0; i < perms.Count; i++)
                    {
                        content.AppendLine($"{i + 1:D2}. {perms[i].Trim()}");
                    }
                }
                else
                {
                    content.AppendLine("暂无权限");
                }

                content.AppendLine();
                content.AppendLine("-".PadRight(50, '-'));
                content.AppendLine();
            }

            File.WriteAllText(filePath, content.ToString(), Encoding.UTF8);

            // 发送消息给玩家
            plr.SendMessage($"[{PluginName}] 权限表已生成:", color);
            plr.SendMessage($"文件路径: tshock/{PluginName}/权限表/{fileName}", color);

            // 添加到TShock日志
            TShock.Log.Info($"玩家【{plr.Name}】生成了权限表文件: {fileName}");
        }
        catch (Exception ex)
        {
            TShock.Log.ConsoleError($"[{PluginName}] 生成权限表失败: {ex}");
        }
    }
    #endregion

    #region 执行指令方法
    public static void DoCommand(TSPlayer plr, HashSet<string> cmds)
    {
        if (cmds is null || !cmds.Any()) return;

        var skipList = new List<string>();
        var execList = new List<string>();

        // 先分离存在的和不存在的指令
        foreach (var cmd in cmds)
        {
            if (IsCmdExist(cmd))
                execList.Add(cmd);
            else
                skipList.Add(cmd);
        }

        // 统一处理不存在的指令（显示全部）
        if (skipList.Count > 0)
        {
            string skipped = string.Join(", ", skipList);
            string msg = $"跳过 {skipList.Count} 个不存在的指令:\n {skipped}";

            if (plr != TSPlayer.Server)
                plr.SendMessage(msg, color);
            else
                TShock.Log.ConsoleWarn(msg);
        }

        // 执行存在的指令
        if (execList.Count > 0)
        {
            if (plr == TSPlayer.Server)
            {
                foreach (var cmd in execList)
                    TShockAPI.Commands.HandleCommand(plr, cmd);
            }
            else
            {
                Group group = plr.Group;
                try
                {
                    plr.Group = new SuperAdminGroup();
                    foreach (var cmd in execList)
                        TShockAPI.Commands.HandleCommand(plr, cmd);
                }
                finally
                {
                    plr.Group = group;
                }
            }
        }
    }
    #endregion

    #region 清理tshock.sqlite方法
    private static void ClearSqlInfo(CommandArgs args,TSPlayer plr)
    {
        // 检查是否有确认参数
        if (args.Parameters.Count < 2 || !args.Parameters[1].Equals("yes", StringComparison.OrdinalIgnoreCase))
        {
            plr.SendMessage(TextGradient($"此操作会清理tshock.sqlite数据库中的指定数据表，且不可逆转！"), color);
            plr.SendMessage($"确认操作请输入:/{pt} sql yes", color);
            return;
        }

        ClearSql(plr);
    }

    private static void ClearSql(TSPlayer plr)
    {
        var ok = 0;
        foreach (var sql in Config.ClearSql)
        {
            try
            {
                TShock.DB.Query(sql);
                ok++;
            }
            catch (Exception ex)
            {
                TShock.Log.ConsoleWarn($"[{PluginName}] 重置SQL({sql})执行失败: {ex.Message}");
            }
        }

        if (ok > 0)
            plr.SendMessage($"已重置数据表{ok}个", color);
    }
    #endregion

    #region 删除文件方法
    private static void DeleteFileInfo(CommandArgs args, TSPlayer plr)
    {
        // 检查是否有确认参数
        if (args.Parameters.Count < 2 || !args.Parameters[1].Equals("yes", StringComparison.OrdinalIgnoreCase))
        {
            plr.SendMessage(TextGradient($"此操作会清理当前地图与地图备份、自动备份存档、服务器日志，且不可逆转！"), color);
            plr.SendMessage($"确认操作请输入:/{pt} rm yes", color);
            return;
        }

        DeleteFile(plr);
    }

    private static void DeleteFile(TSPlayer plr)
    {
        try
        {
            var ok = 0;
            foreach (string f in Config.DeleteFile)
            {
                if (string.IsNullOrWhiteSpace(f)) continue;

                if (File.Exists(f))
                {
                    if (IsFileLocked(f))
                    {
                        TShock.Log.ConsoleWarn($"文件 {f} 正在被使用，跳过删除");
                        continue; // 跳过正在使用的文件
                    }
                    File.Delete(f);
                    ok++;
                }
                else if (Directory.Exists(f))
                {
                    // 删除文件夹及其内容
                    DeleteDirectory(f, plr);
                    ok++;
                }
                else
                {
                    // 分析删除后缀为 *.txt(举例) 的文件
                    string[] files = f.Split('/');
                    string theDirectory = f.Remove(f.Length - files[files.Length - 1].Length);
                    files[files.Length - 1] = files[files.Length - 1].Trim();
                    if (files[files.Length - 1].StartsWith("*.") && Directory.Exists(theDirectory))
                    {
                        var dir = new DirectoryInfo(theDirectory);
                        foreach (var info in dir.GetFileSystemInfos())
                        {
                            if (info.Extension.Equals(files[files.Length - 1].TrimStart('*'),
                                StringComparison.OrdinalIgnoreCase) && File.Exists(info.FullName))
                            {
                                if (IsFileLocked(info.FullName))
                                {
                                    TShock.Log.ConsoleWarn($"文件 {info.Name} 正在被使用，跳过删除");
                                    continue; // 跳过正在使用的文件
                                }

                                File.Delete(info.FullName);      //删除指定后缀名文件
                                TShock.Log.ConsoleInfo(info.Name + " 删除成功");
                                ok++;
                            }
                        }
                    }
                }
            }

            if (ok > 0)
                plr.SendMessage($"已删除文件{ok}个,如包含启动地图,请使用 /off-nosave 并重启", color);
        }
        catch (Exception ex)
        {
            TShock.Log.ConsoleError("删除文件时发生错误 ：" + ex.ToString());
        }
    }

    // 删除文件夹的辅助方法，排除正在使用的文件
    private static void DeleteDirectory(string directoryPath, TSPlayer plr)
    {
        try
        {
            if (!Directory.Exists(directoryPath))
                return;

            // 先删除文件夹内的所有文件
            string[] files = Directory.GetFiles(directoryPath);
            foreach (string file in files)
            {
                if (IsFileLocked(file))
                {
                    TShock.Log.ConsoleWarn($"文件 {file} 正在被使用，跳过删除");
                    continue; // 跳过正在使用的文件
                }

                try
                {
                    File.Delete(file);
                }
                catch (Exception ex)
                {
                    TShock.Log.ConsoleWarn($"删除文件 {file} 失败: {ex.Message}");
                }
            }

            // 递归删除子文件夹
            string[] directories = Directory.GetDirectories(directoryPath);
            foreach (string directory in directories)
            {
                DeleteDirectory(directory, plr);
            }

            // 如果文件夹为空，尝试删除
            if (Directory.GetFiles(directoryPath).Length == 0 &&
                Directory.GetDirectories(directoryPath).Length == 0)
            {
                try
                {
                    Directory.Delete(directoryPath);
                }
                catch (Exception ex)
                {
                    TShock.Log.ConsoleWarn($"删除文件夹 {directoryPath} 失败: {ex.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            TShock.Log.ConsoleError($"删除文件夹 {directoryPath} 时发生错误: {ex.Message}");
        }
    }
    #endregion

    #region 复制文件方法
    public static readonly string CopyDir = Path.Combine(MainPath, "复制源文件"); // 复制源文件路径
    private static void CopyFile(CommandArgs args, TSPlayer plr)
    {
        // 如果没有参数，显示文件列表
        if (args.Parameters.Count == 1)
        {
            ShowFileList(plr);
            return;
        }

        // 如果有参数，执行复制操作
        if (args.Parameters.Count < 3)
        {
            plr.SendErrorMessage($"用法: /{pt} copy <文件索引> <路径索引>");
            return;
        }

        if (!int.TryParse(args.Parameters[1], out int fileIdx) || fileIdx < 1)
        {
            plr.SendErrorMessage("文件索引必须是大于0的数字!");
            return;
        }

        if (!int.TryParse(args.Parameters[2], out int pathIdx) || pathIdx < 1)
        {
            plr.SendErrorMessage("路径索引必须是大于0的数字!");
            return;
        }

        try
        {
            // 获取源文件
            var srcFiles = Directory.GetFiles(CopyDir, "*", SearchOption.TopDirectoryOnly);
            if (fileIdx > srcFiles.Length)
            {
                plr.SendErrorMessage($"文件索引 {fileIdx} 无效，最大为 {srcFiles.Length}");
                return;
            }

            string srcFile = srcFiles[fileIdx - 1];
            string fileName = Path.GetFileName(srcFile);

            // 获取目标路径
            var destPaths = Config.CopyPaths;
            if (pathIdx > destPaths.Count)
            {
                plr.SendErrorMessage($"路径索引 {pathIdx} 无效，最大为 {destPaths.Count}");
                return;
            }

            string destPath = destPaths[pathIdx - 1];

            // 创建目标路径（如果不存在）
            if (!Directory.Exists(destPath))
            {
                try
                {
                    Directory.CreateDirectory(destPath);
                    plr.SendMessage($"已创建目标路径: \n{destPath}", color);
                }
                catch (Exception ex)
                {
                    plr.SendErrorMessage($"创建目标路径失败: {ex.Message}");
                    return;
                }
            }

            string destFile = Path.Combine(destPath, fileName);

            // 如果目标文件已存在，先备份
            if (File.Exists(destFile))
            {
                string backupFile = destFile + ".bak_" + DateTime.Now.ToString("yyyyMMddHHmmss");
                File.Copy(destFile, backupFile, true);
                plr.SendMessage($"检测到输出路径存在相同文件,已备份原文件: \n{Path.GetFileName(backupFile)}", color);
            }

            // 复制文件
            File.Copy(srcFile, destFile, true);

            long fileSize = new FileInfo(srcFile).Length;
            string sizeText = fileSize < 1024 ? $"{fileSize} B" :
                            fileSize < 1024 * 1024 ? $"{fileSize / 1024} KB" :
                            $"{fileSize / (1024 * 1024)} MB";

            if (plr.RealPlayer)
            {
                plr.SendMessage($"成功复制文件:", color);
                plr.SendMessage($"源文件: {fileName} ({sizeText})", color);
                plr.SendMessage($"目标位置: {destFile}", color);
            }

            // 记录到日志
            TShock.Log.ConsoleInfo($"[{PluginName}] 玩家【{plr.Name}】复制了文件:");
            TShock.Log.ConsoleInfo($"复制:{srcFile}");
            TShock.Log.ConsoleInfo($"输出:{destFile}");
        }
        catch (Exception ex)
        {
            plr.SendErrorMessage($"复制文件时出错: {ex.Message}");
            TShock.Log.ConsoleError($"[{PluginName}] DoCopyFile错误: {ex}");
        }
    }
    #endregion

    #region 修复地图区块缺失（自动修改server.properties文件）
    private static void FixWorld(CommandArgs args, TSPlayer plr)
    {
        // 检查是否有确认参数
        if (args.Parameters.Count < 2 || !args.Parameters[1].Equals("yes", StringComparison.OrdinalIgnoreCase))
        {
            ShowFixInfo(plr);
            return;
        }

        if (args.Parameters[1].Equals("auto", StringComparison.OrdinalIgnoreCase))
        {
            SetBool("自动修复地图区块缺失", plr, () => Config.AutoFixWorld, (val) => Config.AutoFixWorld = val);
            return;
        }

        // 执行修复
        ExecuteFix(plr);
    }
    #endregion

    #region 执行修复地图区块缺失操作
    public static readonly string PropPath = Path.Combine(typeof(TShock).Assembly.Location, "server.properties");
    public static void ExecuteFix(TSPlayer plr, bool isCmd = true)
    {
        try
        {
            int size = GetWorldSize();
            if (size == 0)
            {
                plr.SendMessage("无法获取地图尺寸，修复失败", color);
                return;
            }

            if (!File.Exists(PropPath))
            {
                plr.SendMessage($"找不到 server.properties 文件: {PropPath}", color);
                return;
            }

            string[] lines;
            bool found, flag;
            ModifyProperties(plr, size, out lines, out found, out flag);

            if (isCmd)
            {
                if (found)
                {
                    // 如果找到并修改，保存文件
                    SetAutoCreate(size, lines);
                    Notify_offServer(); // 通知并关闭服务器
                }
                else if (!flag)
                {
                    // 如果没有找到，在文件末尾添加
                    AddAutoCreate(size, lines);
                    Notify_offServer(); // 通知并关闭服务器
                }
            }
            else
            {
                if (found)
                {
                    // 如果找到并修改，保存文件
                    SetAutoCreate(size, lines);
                    offServer(); // 直接关服
                }
                else if (!flag)
                {
                    // 如果没有找到，在文件末尾添加
                    AddAutoCreate(size, lines);
                    offServer(); // 直接关服
                }
            }
        }
        catch (Exception ex)
        {
            TShock.Log.ConsoleError($"修改server.properties时发生错误: {ex}");
            plr.SendMessage($"修复失败: {ex.Message}", color);
        }
    }

    #region 修改Properties文件
    private static void ModifyProperties(TSPlayer plr, int size, out string[] lines, out bool found, out bool flag)
    {
        lines = File.ReadAllLines(PropPath);
        found = false;
        flag = false;
        for (int i = 0; i < lines.Length; i++)
        {
            string line = lines[i].Trim();

            // 跳过注释行和空行
            if (line.StartsWith("#") || string.IsNullOrEmpty(line))
                continue;

            // 找到autocreate配置行
            if (line.StartsWith("autocreate", StringComparison.OrdinalIgnoreCase))
            {
                int equalsIndex = line.IndexOf('=');
                if (equalsIndex > 0)
                {
                    string key = line.Substring(0, equalsIndex).Trim();
                    if (key.Equals("autocreate", StringComparison.OrdinalIgnoreCase))
                    {
                        string cur = line.Substring(equalsIndex + 1).Trim();

                        // 检查是否已经是目标值
                        if (cur == size.ToString())
                        {
                            flag = true;
                            plr.SendMessage($"此BUG已修复,当前autocreate为:{cur}", color);
                            return;
                        }

                        // 修改值
                        lines[i] = $"{key}={size}";
                        found = true;
                        break;
                    }
                }
            }
        }
    }
    #endregion

    #region 带倒计时通知并关服
    private static void Notify_offServer()
    {
        TShock.Utils.Broadcast($"[{PluginName}] 服务器即将[c/DC143C:开始修复地图区块缺失bug]...", color);
        for (var i = 10; i >= 0; i--)
        {
            TShock.Utils.Broadcast(string.Format($"[{PluginName}] {i}秒后关闭服务器..."), color2);
            Thread.Sleep(1000);
        }

        // 踢出玩家确保SSC角色保存
        TShock.Players.ForEach(delegate (TSPlayer? p)
        {
            p?.Kick($"{PluginName} 正在修复地图区块缺失bug,请10秒后重进", true, true);
        });

        TShockAPI.Commands.HandleCommand(TSPlayer.Server, "/off");
    }
    #endregion

    #region 关闭服务器方法
    private static void offServer()
    {
        try
        {
            TShock.RestApi.Dispose(); //关闭RestApi
        }
        catch { }

        Netplay.SaveOnServerExit = false; //不保存地图
        Netplay.Disconnect = true; //断开连接
        TShock.ShuttingDown = true; //关闭服务器
        Environment.Exit(0); //退出程序
    }
    #endregion

    #region 修改autocreate值
    private static void SetAutoCreate(int size, string[] lines)
    {
        File.WriteAllLines(PropPath, lines);
        TShock.Log.ConsoleInfo($"已根据地图尺寸修改 server.properties 中的 autocreate 值为{size}");
    }
    #endregion

    #region 添加autocreate并设置值
    private static void AddAutoCreate(int size, string[] lines)
    {
        var newLines = lines.ToList();
        newLines.Add($"autocreate={size}");
        File.WriteAllLines(PropPath, newLines);
        TShock.Log.ConsoleInfo($"已在 server.properties 中 添加 autocreate={size}");
    }
    #endregion

    #endregion

    #region 显示世界区块缺失的修复信息
    public static void ShowFixInfo(TSPlayer plr)
    {
        var info = new StringBuilder();
        info.AppendLine("修复地图区块缺失流程：");
        info.AppendLine("1.检测当前地图尺寸（小/中/大）");
        info.AppendLine("2.修改 server.properties 中的 autocreate 值");
        info.AppendLine("4.进入10秒倒计时");
        info.AppendLine("4.踢出在线玩家确保数据保存,关闭服务器");
        info.AppendLine("5.根据启动项,重启服务器后生效");

        info.AppendLine($"\n自动修复开关: /{pt} fix auto");
        info.AppendLine("注:自动修复会在刚开服后执行工作并立即重启");
        info.AppendLine($"\n确认修复请输入: /{pt} fix yes");
        info.AppendLine("注:此操作会重启服务器，请确保已通知在线玩家!");

        if (!plr.RealPlayer)
            plr.SendMessage(info.ToString(), color);
        else
            plr.SendMessage(TextGradient(info.ToString()), color);
    }
    #endregion

    #region 重置服务器方法
    private static void Reset(CommandArgs args, TSPlayer plr)
    {
        // 检查是否有确认参数
        if (args.Parameters.Count < 2 || !args.Parameters[1].Equals("yes", StringComparison.OrdinalIgnoreCase))
        {
            ShowResetInfo(plr);
            return;
        }

        TShock.Utils.Broadcast($"[{PluginName}] 服务器即将[c/DC143C:开始重置]...", color);
        for (var i = 10; i >= 0; i--)
        {
            TShock.Utils.Broadcast(string.Format($"[{PluginName}] {i}秒后关闭服务器..."), color2);
            Thread.Sleep(1000);
        }

        // 踢出玩家确保SSC角色保存
        TShock.Players.ForEach(delegate (TSPlayer? p)
        {
            p?.Kick($"{PluginName} 服务器已开始重置...", true, true);
        });

        WritePlayer.ExportAll(plr, WritePlayer.WriteDir);
        DoCommand(plr, Config.BeforeCMD);
        ClearSql(plr);

        // 重置进度锁
        Config.UnLockNpc.Clear();
        Config.Write();

        DeleteFile(plr); // 删除文件含地图
        RandomCopyMap(plr);
        DoCommand(plr, Config.AfterCMD); // 执行关服指令 让自动重启启动项创建新地图 重启也能清空TS程序内存
    }
    #endregion

    #region 随机复制新地图方法
    public static readonly string MapDir = Path.Combine(MainPath, "重置时用的复制地图"); // 复制地图路径
    public static readonly string WldDir = Path.Combine(typeof(TShock).Assembly.Location, "world"); // 加载地图路径
    private static void RandomCopyMap(TSPlayer plr)
    {
        try
        {
            // 获取所有wld文件
            string[] mapFiles = Directory.GetFiles(MapDir, "*.wld");

            if (mapFiles.Length == 0)
            {
                TShock.Log.ConsoleWarn($"[{PluginName}] 地图文件夹中没有.wld文件，跳过复制");
                return;
            }

            // 随机选择一个地图
            string source = mapFiles[rand.Next(mapFiles.Length)];
            string destFile = Path.Combine(WldDir, "SFE4.wld");

            // 复制地图
            File.Copy(source, destFile, true);
            TShock.Log.ConsoleInfo($"[{PluginName}] 已随机复制地图: {Path.GetFileName(source)} => SFE4.wld");
        }
        catch (Exception ex)
        {
            TShock.Log.ConsoleError($"[{PluginName}] 自动复制地图失败: {ex}");
        }
    }
    #endregion

    #region 显示重置信息
    private static void ShowResetInfo(TSPlayer plr)
    {
        var info = new StringBuilder();
        info.AppendLine("重置服务器流程：");
        info.AppendLine("1.自动导出所有SSC玩家存档并打包地图");
        info.AppendLine("2.重置前执行:其他插件的清理数据指令");
        info.AppendLine("3.清理tshock.sqlite数据库");
        info.AppendLine(" tsCharacter - 强制开荒背包");
        info.AppendLine(" Warps - 地标传送点");
        info.AppendLine(" Regions - 区域领地坐标");
        info.AppendLine(" Research - 旅途研究");
        info.AppendLine(" RememberedPos - 回服传送记录点");
        info.AppendLine("4.清理已解锁怪物表中的怪物名称");
        info.AppendLine("5.删除指定文件(当前地图备份、日志文件)");
        info.AppendLine($"6.检测【{Path.GetFileName(MapDir)}】是否有地图文件:");
        info.AppendLine("有: 随机选择地图并改名SFE4.wld复制world文件夹");
        info.AppendLine("没有: 根据server.properties内的参数创建新地图");
        info.AppendLine("7.重置后执行关服不保存,根据启动项自动重启");

        info.AppendLine($"\n确认重置请输入: /{pt} reset yes");
        info.AppendLine("警告: 此操作不可逆，请确保已备份重要数据!");

        if (!plr.RealPlayer)
            plr.SendMessage(info.ToString(), color);
        else
            plr.SendMessage(TextGradient(info.ToString()), color);
    }
    #endregion

    #region 处理导出命令 (/pout p c)
    private static void ExportPlayer(CommandArgs args, TSPlayer plr)
    {
        if (args.Parameters.Count < 3)
        {
            var mess = new StringBuilder();
            mess.Append("\n导出命令使用方法:");
            mess.Append($"\n导出所有玩家：/{pt} p c all");
            mess.Append($"\n导出指定玩家：/{pt} p c 玩家名");
            mess.Append($"\n导出指定玩家：/{pt} p c 账号id");
            mess.Append($"\n注:账号ID可用/who -i查询");
            GradMess(plr, mess);
            return;
        }

        string operation = args.Parameters[2].ToLower();

        if (operation == "all")
        {
            // 导出所有玩家
            WritePlayer.ExportAll(plr, WritePlayer.WriteDir);
        }
        else
        {
            // 导出指定玩家
            WritePlayer.ExportPlayer(operation, plr, WritePlayer.WriteDir);
        }
    }
    #endregion

    #region 处理导入命令 (/pout p r)
    private static void ImportPlayer(CommandArgs args, TSPlayer plr)
    {
        // 如果没有额外参数，显示存档列表
        if (args.Parameters.Count == 2)
        {
            ReaderPlayer.ShowPlrFile(plr);
            plr.SendMessage($"\n导入命令使用方法：", color);
            var mess = new StringBuilder();
            mess.Append($"存档导入给所有玩家：/{pt} p r all");
            mess.Append($"\n存档导入给对应玩家：/{pt} p r 存档索引");
            mess.Append($"\n存档导入给指定玩家：/{pt} p r 存档索引 玩家名\n");
            mess.Append($"\n【对应】和.plr同名玩家，【指定】不同名玩家");
            mess.Append($"\n玩家不存在:自动创建账号,密码为{Config.DefPass}");
            mess.Append($"\n注:存档索引是显示文件列表中的序号");
            GradMess(plr, mess);
            return;
        }

        // 获取发送指令者的UUID,给帮注册的人,避免空UUID无法登录
        ReaderPlayer.newUUID = plr.UUID;
        switch (args.Parameters.Count)
        {
            case 3:
                // 格式: /Pouts p r all 或 /Pouts p r 存档索引
                string param = args.Parameters[2].ToLower();
                if (param == "all")
                {
                    // 导入所有存档给所有玩家
                    ReaderPlayer.ReadPlayer(plr);
                }
                else if (int.TryParse(param, out int fileIdx))
                {
                    // 导入指定存档给当前玩家
                    ReaderPlayer.ReadPlayerByIndex(plr, fileIdx);
                }
                else
                {
                    plr.SendErrorMessage("错误：参数格式不正确！");
                    plr.SendMessage($"正确格式：/{pt} p r all", color);
                    plr.SendMessage($"正确格式：/{pt} p r 存档索引", color);
                }
                break;

            case 4:
                // 格式: /Pouts p r 存档索引 玩家名
                if (!int.TryParse(args.Parameters[2], out int fileIdx2))
                {
                    plr.SendErrorMessage("错误：存档索引必须是数字！");
                    plr.SendMessage($"正确格式：/{pt} p r 存档索引 玩家名", color);
                    return;
                }

                var plrName = args.Parameters[3];
                ReaderPlayer.ReadPlayerByIndex(plr, fileIdx2, plrName);
                break;

            default:
                plr.SendErrorMessage("参数错误！正确格式：");
                plr.SendMessage($"/{pt} p r all", color);
                plr.SendMessage($"/{pt} p r 存档索引", color);
                plr.SendMessage($"/{pt} p r 存档索引 玩家名", color);
                break;
        }
    }
    #endregion

    #region 从备份压缩包导入存档给玩家
    private static void ZipPlayer(CommandArgs args, TSPlayer plr)
    {
        try
        {
            // 获取自动备份文件夹中的所有zip文件
            var zipFiles = GetBakFiles();

            if (zipFiles.Length == 0)
            {
                plr.SendMessage("自动备份文件夹中没有备份文件", color);
                return;
            }

            // 情况1: /Pouts p z - 列出备份文件
            if (args.Parameters.Count == 2)
            {
                ListZips(plr, zipFiles);
                return;
            }

            // 情况2: /Pouts p z 玩家名 - 使用最新备份恢复指定玩家
            if (args.Parameters.Count == 3)
            {
                string param = args.Parameters[2];

                // 检查是否是数字（索引）
                if (int.TryParse(param, out int idx))
                {
                    plr.SendMessage($"用法: /{pt} p z {idx} all", color);
                    plr.SendMessage($"用法: /{pt} p z {idx} 玩家名", color);
                    return;
                }

                // 作为玩家名处理
                var name = param;
                var latestZip = zipFiles.First();
                ExtractAndImport(plr, latestZip, name);
                return;
            }

            // 情况3: /Pouts p z 索引 all - 恢复所有玩家
            // 情况4: /Pouts p z 索引 玩家名 - 恢复指定玩家
            if (args.Parameters.Count >= 4)
            {
                if (!int.TryParse(args.Parameters[2], out int idx) || idx < 1)
                {
                    plr.SendMessage("索引必须是大于0的数字", color);
                    return;
                }

                if (idx > zipFiles.Length)
                {
                    plr.SendMessage($"索引 {idx} 无效，最大为 {zipFiles.Length}", color);
                    ListZips(plr, zipFiles);
                    return;
                }

                var zipFile = zipFiles[idx - 1];
                var operation = args.Parameters[3].ToLower();

                if (operation == "all")
                {
                    ExtractAndImportAll(plr, zipFile);
                }
                else
                {
                    var name = operation;
                    ExtractAndImport(plr, zipFile, name);
                }
            }
        }
        catch (Exception ex)
        {
            plr.SendErrorMessage($"备份操作失败: {ex.Message}");
            TShock.Log.ConsoleError($"[{PluginName}] ZipPlayer错误: {ex}");
        }
    }
    #endregion

    #region 列出备份文件
    public static void ListZips(TSPlayer plr, string[] zipFiles)
    {
        var mess = new StringBuilder();
        mess.AppendLine("自动备份文件列表:");

        for (int i = 0; i < zipFiles.Length; i++)
        {
            mess.AppendLine(FormatBakInfo(zipFiles[i], i + 1));
        }

        mess.AppendLine($"\n使用最新备份恢复玩家: /{pt} p z 玩家名");
        mess.AppendLine($"恢复所有玩家: /{pt} p z 索引 all");
        mess.AppendLine($"恢复指定玩家: /{pt} p z 索引 玩家名");
        mess.AppendLine($"注意:索引为压缩包的序号,最新的是1,");
        mess.AppendLine($"每次执行前后都会清空《导入存档》文件夹");

        if (plr.RealPlayer)
            plr.SendMessage(TextGradient(mess.ToString()), color);
        else
            plr.SendMessage(mess.ToString(), color);
    }
    #endregion

    #region 显示玩家存档命令的帮助信息
    private static void ShowPlayerHelp(TSPlayer plr)
    {
        var mess = new StringBuilder();

        mess.AppendLine("\n[c/AD89D5:玩家存档管理指令]");
        mess.AppendLine($"[c/3FAEDB:导出] /{pt} p c");
        mess.AppendLine($"[c/3FAEDB:导入] /{pt} p r");
        mess.AppendLine($"[c/3FAEDB:从备份导入] /{pt} p z");

        // 获取版本显示字符串
        string vsText = GetVSText(Config.GameVersion);
        mess.AppendLine($"版本:[c/888888:{vsText}]");

        if (plr.RealPlayer)
            plr.SendMessage(TextGradient(mess.ToString()), color);
        else
            plr.SendMessage(mess.ToString(), color);
    }
    #endregion

    #region 获取版本对应文本
    public static string GetVSText(int vs)
    {
        if (vs == -1) return "最新版";

        // 从配置的示例中查找对应的版本名称
        foreach (var example in Config.Example)
        {
            if (example.Contains($": {vs}"))
            {
                // 格式如："1.4.5.0 : 315"
                var parts = example.Split(':');
                if (parts.Length >= 2)
                    return $"{parts[0].Trim()} ({vs})";
            }
        }

        // 如果找不到对应的版本名称，只显示数字
        return vs.ToString();
    }
    #endregion

}