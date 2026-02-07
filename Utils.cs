using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.Utilities;
using TShockAPI;
using static FixTools.FixTools;

namespace FixTools;

internal class Utils
{
    #region 异步执行
    public static void Tack()
    {
        var task = Task.Run(delegate
        {
            // 写你的执行方法
        });

        task.ContinueWith(delegate
        {
            // 执行完毕后回到主线程
        });
    }
    #endregion

    #region 单色与随机色
    public static UnifiedRandom rand = Main.rand; // 随机器
    public static Color color => new(240, 250, 150); // 单行色
    public static Color color2 => new(rand.Next(180, 250), // 单行随机色
                                  rand.Next(180, 250),
                                  rand.Next(180, 250));
    public static Color RandomColors()
    {
        var r = rand.Next(200, 255);
        var g = rand.Next(200, 255);
        var b = rand.Next(180, 255);
        var color = new Color(r, g, b);
        return color;
    }
    #endregion

    #region 逐行渐变色
    public static void GradMess(TSPlayer plr, StringBuilder mess)
    {
        var Text = mess.ToString();
        var lines = Text.Split('\n');

        var GradMess = new StringBuilder();
        var start = new Color(166, 213, 234);
        var end = new Color(245, 247, 175);
        for (int i = 0; i < lines.Length; i++)
        {
            if (!string.IsNullOrEmpty(lines[i]))
            {
                float ratio = (float)i / (lines.Length - 1);
                var gradColor = Color.Lerp(start, end, ratio);

                // 将颜色转换为十六进制格式
                string colorHex = $"{gradColor.R:X2}{gradColor.G:X2}{gradColor.B:X2}";

                // 使用颜色标签包装每一行
                GradMess.AppendLine($"[c/{colorHex}:{lines[i]}]");
            }
        }

        plr.SendMessage(GradMess.ToString(), 240, 250, 150);
    }
    #endregion

    #region 渐变着色方法
    public static string TextGradient(string text, TSPlayer? plr = null)
    {
        if (string.IsNullOrEmpty(text))
            return text;

        text = placeholder(text, plr);

        // 检查是否已包含颜色标签
        if (text.Contains("[c/"))
        {
            // 如果有颜色标签，保留它们并处理其他部分
            return MixedText(text);
        }
        else
        {
            // 如果没有颜色标签，直接应用渐变
            return ApplyGrad(text);
        }
    }
    #endregion

    #region 占位符替换方法(忽略大小写)
    private static string placeholder(string text, TSPlayer? plr)
    {
        if (plr != null)
        {
            text = Regex.Replace(text, @"\{玩家名\}", plr.Name, RegexOptions.IgnoreCase);
            text = Regex.Replace(text, @"\{ip\}", plr.IP, RegexOptions.IgnoreCase);
            text = Regex.Replace(text, @"\{uuid\}", plr.UUID, RegexOptions.IgnoreCase);
            text = Regex.Replace(text, @"\{组名\}", plr.Account.Group, RegexOptions.IgnoreCase);
            text = Regex.Replace(text, @"\{账号\}", plr.Account.ID.ToString(), RegexOptions.IgnoreCase);
            text = Regex.Replace(text, @"\{武器类型\}", GetWeapon(plr.SelectedItem), RegexOptions.IgnoreCase);
            text = Regex.Replace(text, @"\{物品图标\}", ItemIcon(plr.SelectedItem.type), RegexOptions.IgnoreCase);
            text = Regex.Replace(text, @"\{物品名\}", Lang.GetItemNameValue(plr.SelectedItem.type), RegexOptions.IgnoreCase);
            text = Regex.Replace(text, @"\{生命\}", plr.TPlayer.statLife.ToString(), RegexOptions.IgnoreCase);
            text = Regex.Replace(text, @"\{生命上限\}", plr.TPlayer.statLifeMax.ToString(), RegexOptions.IgnoreCase);
            text = Regex.Replace(text, @"\{魔力\}", plr.TPlayer.statMana.ToString(), RegexOptions.IgnoreCase);
            text = Regex.Replace(text, @"\{魔力上限\}", plr.TPlayer.statManaMax.ToString(), RegexOptions.IgnoreCase);
            text = Regex.Replace(text, @"\{队伍\}", GetTeamCName(plr.Team), RegexOptions.IgnoreCase);

            // 同队人数
            if (Regex.IsMatch(text, @"\{同队人数\}", RegexOptions.IgnoreCase))
            {
                int teamCount = TShock.Players.Count(p => p != null && p.Active && p.Team == plr.Team);
                text = Regex.Replace(text, @"\{同队人数\}", teamCount.ToString(), RegexOptions.IgnoreCase);
            }

            // 同队玩家名称
            if (Regex.IsMatch(text, @"\{同队玩家\}", RegexOptions.IgnoreCase))
            {
                var TeamPlayer = TShock.Players
                    .Where(p => p != null && p.Active && p.Team == plr.Team)
                    .Select(p => p.Name);
                text = Regex.Replace(text, @"\{同队玩家\}",
                    string.Join(", ", TeamPlayer), RegexOptions.IgnoreCase);
            }

            // 别队人数
            if (Regex.IsMatch(text, @"\{别队人数\}", RegexOptions.IgnoreCase))
            {
                int otherTeamCount = TShock.Players
                    .Count(p => p != null && p.Active && p.Team != plr.Team);
                text = Regex.Replace(text, @"\{别队人数\}",
                    otherTeamCount.ToString(), RegexOptions.IgnoreCase);
            }
        }

        // 队伍统计
        if (Regex.IsMatch(text, @"\{队伍统计\}", RegexOptions.IgnoreCase))
        {
            var teamStats = new StringBuilder();
            for (int i = 0; i <= 5; i++)
            {
                int count = TShock.Players.Count(p => p != null && p.Active && p.Team == i);
                if (count > 0)
                {
                    teamStats.Append($"{GetTeamCName(i)}-{count}人 ");
                }
            }
            text = Regex.Replace(text, @"\{队伍统计\}", teamStats.ToString(), RegexOptions.IgnoreCase);
        }

        // 服务器名
        text = Regex.Replace(text, @"\{服务器名\}",
            TShock.Config.Settings.UseServerName ? TShock.Config.Settings.ServerName : Main.worldName,
            RegexOptions.IgnoreCase);

        // 在线人数
        text = Regex.Replace(text, @"\{在线人数\}",
            TShock.Utils.GetActivePlayerCount().ToString(),
            RegexOptions.IgnoreCase);

        // 服务器上限
        text = Regex.Replace(text, @"\{服务器上限\}",
            TShock.Config.Settings.MaxSlots.ToString(),
            RegexOptions.IgnoreCase);

        text = Regex.Replace(text, @"\{插件名\}",
            PluginName,
            RegexOptions.IgnoreCase);

        // 在线玩家
        if (Regex.IsMatch(text, @"\{在线玩家\}", RegexOptions.IgnoreCase))
        {
            var plrs = TShock.Players.Where(p => p != null && p.Active).Select(p => p.Name);
            string allPlayers = string.Join(", ", plrs);
            text = Regex.Replace(text, @"\{在线玩家\}", allPlayers, RegexOptions.IgnoreCase);
        }

        // 进度
        if (GetProgress().Count > 0)
            text = Regex.Replace(text, @"\{进度\}", string.Join(",", GetProgress()), RegexOptions.IgnoreCase);
        else
            text = Regex.Replace(text, @"\{进度\}", "无", RegexOptions.IgnoreCase);

        return text;
    }
    #endregion

    #region 混合文本（包含颜色标签、物品图标标签和普通文本）
    private static string MixedText(string text)
    {
        var res = new StringBuilder();

        // 匹配颜色标签 [c/颜色:文本] 或 物品图标标签 [i:物品ID] 或 [i/s数量:物品ID]
        var regex = new Regex(@"(\[c/([0-9a-fA-F]+):([^\]]+)\]|\[i(?:/s\d+)?:\d+\])");
        var matches = regex.Matches(text);

        if (matches.Count == 0)
            return ApplyGrad(text);

        int idx = 0;
        foreach (Match match in matches.Cast<Match>())
        {
            // 添加标签前的普通文本（应用渐变）
            if (match.Index > idx)
            {
                string plainText = text.Substring(idx, match.Index - idx);
                res.Append(ApplyGrad(plainText));
            }

            // 添加标签本身（保持不变）
            res.Append(match.Value);
            idx = match.Index + match.Length;
        }

        // 添加最后一个标签后的普通文本
        if (idx < text.Length)
        {
            string plainText = text.Substring(idx);
            res.Append(ApplyGrad(plainText));
        }

        return res.ToString();
    }
    #endregion

    #region 应用文本渐变方法
    private static string ApplyGrad(string text)
    {
        if (string.IsNullOrEmpty(text))
            return text;

        var res = new StringBuilder();
        var start = new Color(166, 213, 234);
        var end = new Color(245, 247, 175);

        // 计算有效字符数（排除换行符）
        int cnt = 0;
        foreach (char c in text)
        {
            if (c != '\n' && c != '\r')
                cnt++;
        }

        // 如果没有有效字符，直接返回
        if (cnt == 0)
            return text;

        int idx = 0;

        foreach (char c in text)
        {
            if (c == '\n' || c == '\r')
            {
                res.Append(c);
                continue;
            }

            // 计算渐变比例
            float ratio = (float)idx / (cnt - 1);
            var clr = Color.Lerp(start, end, ratio);

            // 添加到结果
            res.Append($"[c/{clr.Hex3()}:{c}]");
            idx++;
        }

        return res.ToString();
    }
    #endregion

    #region 最好的查找（从枳那抄来的代码）
    public static List<TSPlayer> FindPlayer(string name)
    {
        if (int.TryParse(name, out var num))
        {
            foreach (var plr in TShock.Players)
            {
                if (plr is not null && plr.Index == num)
                {
                    return new List<TSPlayer> { plr };
                }
            }
        }

        foreach (var plr in TShock.Players)
        {
            if (plr is not null && plr.Name.Equals(name))
            {
                return new List<TSPlayer> { plr };
            }
        }

        var list = new List<TSPlayer>();
        foreach (var plr in TShock.Players)
        {
            if (plr is not null && plr.Name.Contains(name, StringComparison.OrdinalIgnoreCase))
            {
                list.Add(plr);
            }
        }
        return list;
    }
    #endregion

    #region 检查指令是否存在
    public static bool IsCmdExist(string cmd)
    {
        if (string.IsNullOrWhiteSpace(cmd))
            return false;

        // 移除开头的命令符号(如 / 或 .)
        string text = cmd.Trim();
        if (text.Length < 2)
            return false;

        // 检查第一个字符是否是命令符号
        char firstChar = text[0];
        if (firstChar != '/' && firstChar != '.')
            return false;

        // 提取命令名（第一个单词）
        string text2 = text.Substring(1);
        int spaceIndex = text2.IndexOf(' ');
        string cmdName = spaceIndex > 0 ? text2.Substring(0, spaceIndex).ToLower() : text2.ToLower();

        // 在聊天命令中查找是否有这个命令
        return TShockAPI.Commands.ChatCommands.Any(c => c.HasAlias(cmdName));
    }
    #endregion

    #region 获取地图大小
    public static int GetWorldSize()
    {
        switch (Main.maxTilesX)
        {
            case 4200 when Main.maxTilesY == 1200:
                return 1;
            case 6400 when Main.maxTilesY == 1800:
                return 2;
            case 8400 when Main.maxTilesY == 2400:
                return 3;
            default:
                return 0;
        }

    }
    #endregion

    #region 将 string 转化为能直接作用于文件名的 string （从枳那抄来的代码）
    public static string FormatFileName(string text)
    {
        //移除不合法的字符
        for (int i = 0; i < text.Length; ++i)
        {
            bool flag = text[i] == '\\' || text[i] == '/' || text[i] == ':' || text[i] == '*' || text[i] == '?' || text[i] == '"' || text[i] == '<' || text[i] == '>' || text[i] == '|';
            if (flag)
            {
                text = text.Replace(text[i], '-');
            }
        }
        return text;
    }
    #endregion

    #region 检查文件是否被占用的方法
    public static bool IsFileLocked(string filePath)
    {
        try
        {
            using (FileStream stream = new FileStream(filePath, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
            {
                // 如果能成功打开，说明文件未被占用
                stream.Close();
                return false;
            }
        }
        catch (IOException)
        {
            // 文件被占用或无权限
            return true;
        }
        catch (UnauthorizedAccessException)
        {
            // 无权限访问
            return true;
        }
        catch (Exception)
        {
            // 其他异常
            return true;
        }
    }
    #endregion

    #region 显示复制源文件与输出路径方法
    public static void ShowFileList(TSPlayer plr)
    {
        try
        {
            // 扫描复制源文件文件夹
            if (!Directory.Exists(Commands.CopyDir))
            {
                Directory.CreateDirectory(Commands.CopyDir);
                plr.SendMessage($"已创建复制源文件文件夹: {Commands.CopyDir}", color);
                plr.SendMessage($"请将文件放入此文件夹后重试", color);
                return;
            }

            // 获取源文件夹中的所有文件
            var srcFiles = Directory.GetFiles(Commands.CopyDir, "*", SearchOption.TopDirectoryOnly);

            if (srcFiles.Length == 0)
            {
                plr.SendMessage($"复制源文件文件夹中没有文件", color);
                plr.SendMessage($"文件夹路径: {Commands.CopyDir}", color);
                plr.SendMessage($"请将文件放入此文件夹后重试", color);
                return;
            }

            // 获取目标路径列表
            var destPaths = Config.CopyPaths;

            if (destPaths.Count == 0)
            {
                plr.SendMessage($"配置文件中未设置任何复制输出路径", color);
                plr.SendMessage($"请在配置文件的【复制文件输出路径】中添加路径", color);
                return;
            }

            // 构建文件列表
            var fileList = new StringBuilder();
            fileList.AppendLine($"{Commands.CopyDir}:");
            fileList.AppendLine($"找到 {srcFiles.Length} 个文件:");

            for (int i = 0; i < srcFiles.Length; i++)
            {
                string fileName = Path.GetFileName(srcFiles[i]);
                long fileSize = new FileInfo(srcFiles[i]).Length;
                string sizeText = fileSize < 1024 ? $"{fileSize} B" :
                                fileSize < 1024 * 1024 ? $"{fileSize / 1024} KB" :
                                $"{fileSize / (1024 * 1024)} MB";

                fileList.AppendLine($"{i + 1}. {fileName} ({sizeText})");
            }

            // 构建目标路径列表
            fileList.AppendLine($"\n复制输出路径 ({destPaths.Count} 个):");
            for (int i = 0; i < destPaths.Count; i++)
            {
                string path = destPaths[i];
                bool exists = Directory.Exists(path);
                fileList.AppendLine($"{i + 1}. {path} {(exists ? "" : "(路径不存在)")}");
            }

            fileList.AppendLine($"\n使用: /{CmdName} copy <文件索引> <路径索引>");
            fileList.AppendLine("示例: /pout copy 1 2   (复制第1个文件到第2个路径)");

            if (plr.RealPlayer)
                plr.SendMessage(TextGradient(fileList.ToString()), color);
            else
                TShock.Log.ConsoleInfo(fileList.ToString());
        }
        catch (Exception ex)
        {
            plr.SendErrorMessage($"扫描文件时出错: {ex.Message}");
            TShock.Log.ConsoleError($"[{PluginName}] CopyFile错误: {ex}");
        }
    }
    #endregion

    #region 返回物品图标方法
    // 根据物品ID返回物品图标
    public static string ItemIcon(ItemID itemID) => ItemIcon(itemID);
    // 根据物品ID返回物品图标
    public static string ItemIcon(int itemID) => $"[i:{itemID}]";
    // 根据物品对象返回物品图标
    public static string ItemIcon(Item item) => ItemIcon(item.type, item.stack);
    // 返回带数量的物品图标
    public static string ItemIcon(int itemID, int stack = 1) => $"[i/s{stack}:{itemID}]";
    #endregion

    #region 队伍名称映射
    public static string GetTeamCName(int teamId) => TeamColorMap.TryGetValue(teamId, out var name) ? name : "全体";
    private static readonly Dictionary<int, string> TeamColorMap = new()
    {
        { 0, "[c/5ADECE:白队]" },{ 1, "[c/F56470:红队]" },
        { 2, "[c/74E25C:绿队]" },{ 3, "[c/5A9DDE:蓝队]" },
        { 4, "[c/FCF466:黄队]" },{ 5, "[c/E15BC2:粉队]" }
    };
    #endregion

    #region 获取武器类型
    public static string GetWeapon(Item item)
    {
        var Held = item;
        if (Held == null || Held.type == 0) return "无";

        if (Held.melee && Held.damage > 0 && Held.ammo == 0 &&
            Held.pick < 1 && Held.hammer < 1 && Held.axe < 1) return "近战";

        if (Held.ranged && Held.damage > 0 && Held.ammo == 0 && !Held.consumable) return "远程";

        if (Held.magic && Held.damage > 0 && Held.ammo == 0) return "魔法";

        if (ItemID.Sets.SummonerWeaponThatScalesWithAttackSpeed[Held.type]) return "召唤";

        if (Held.maxStack == 9999 && Held.damage > 0 &&
            Held.ammo == 0 && Held.ranged && Held.consumable ||
            ItemID.Sets.ItemsThatCountAsBombsForDemolitionistToSpawn[Held.type]) return "投掷物";

        return "未知";
    }
    #endregion

    #region 获取进度
    public static List<string> GetProgress()
    {
        var prog = new List<string>();

        // 按照从高到低的进度检查
        if (NPC.downedMoonlord)
            prog.Add("月总");

        if (NPC.downedTowerNebula && NPC.downedTowerSolar && NPC.downedTowerStardust && NPC.downedTowerVortex)
        {
            prog.Add("四柱");
            prog.Remove("日耀");
            prog.Remove("星旋");
            prog.Remove("星尘");
            prog.Remove("星云");
        }
        else
        {
            if (NPC.downedTowerSolar)
                prog.Add("日耀");
            if (NPC.downedTowerVortex)
                prog.Add("星旋");
            if (NPC.downedTowerStardust)
                prog.Add("星尘");
            if (NPC.downedTowerNebula)
                prog.Add("星云");
        }

        if (NPC.downedAncientCultist)
            prog.Add("拜月");

        if (Terraria.GameContent.Events.DD2Event._spawnedBetsyT3)
            prog.Add("双足翼龙");

        if (NPC.downedMartians)
            prog.Add("火星");

        if (NPC.downedGolemBoss)
            prog.Add("石巨人");

        if (NPC.downedEmpressOfLight)
            prog.Add("光女");

        if (NPC.downedChristmasTree ||
            NPC.downedChristmasIceQueen ||
            NPC.downedChristmasSantank)
            prog.Add("霜月");

        if (NPC.downedHalloweenTree || NPC.downedHalloweenKing)
            prog.Add("南瓜月");

        if (NPC.downedPlantBoss)
            prog.Add("世花");

        if (NPC.downedFishron)
            prog.Add("猪鲨");

        // 机械三王判断
        if (NPC.downedMechBoss1 && NPC.downedMechBoss2 && NPC.downedMechBoss3)
        {
            if (!Main.zenithWorld)
                prog.Add("三王");
            else
                prog.Add("美杜莎");

            prog.Remove("毁灭者");
            prog.Remove("机械骷髅王");
            prog.Remove("双子眼");
        }
        else
        {
            if (NPC.downedMechBoss2)
                prog.Add("双子眼");
            if (NPC.downedMechBoss3)
                prog.Add("机械骷髅王");
            if (NPC.downedMechBoss1)
                prog.Add("毁灭者");
        }

        if (NPC.downedQueenSlime)
            prog.Add("史后");

        if (NPC.downedPirates)
            prog.Add("海盗");

        if (Main.hardMode)
            prog.Add("肉山");

        if (NPC.downedBoss3)
            prog.Add("骷髅王");

        if (NPC.downedQueenBee)
            prog.Add("蜂王");

        if (NPC.downedBoss2)
            prog.Add("世吞克脑");

        if (NPC.downedDeerclops)
            prog.Add("鹿角怪");

        if (NPC.downedSlimeKing)
            prog.Add("史王");

        if (NPC.downedBoss1)
            prog.Add("克眼");

        if (NPC.downedGoblins)
            prog.Add("哥布林");

        return prog;
    }
    #endregion

    #region 获取入侵事件名称
    public static string GetInvasionName(int type)
    {
        return type switch
        {
            1 => "哥布林",
            2 => "雪人军团",
            3 => "海盗",
            4 => "火星暴乱",
            _ => "未知"
        };
    }
    #endregion

    #region 正在使用入侵事件的召唤物品
    public static bool UseEventItem(TSPlayer plr, HashSet<int> itemType)
    {
        var sel = plr.SelectedItem;

        if (sel == null || sel.IsAir) return false;

        if (itemType.Contains(sel.type))
        {
            sel.stack--;

            if (sel.stack == 0)
                sel.TurnToAir(true);

            // 移除玩家物品
            plr.SendData(PacketTypes.PlayerSlot, "", plr.Index, plr.TPlayer.selectedItem);

            // 重置现有入侵状态
            Main.invasionType = 0;
            Main.invasionSize = 0;
            Main.invasionDelay = 0;

            return true;
        }

        return false;
    }
    #endregion

    #region 获取入侵事件的召唤物品
    public static HashSet<int> GetEventItemType()
    {
        if (Config.MartianEvent)
        {
            return new HashSet<int>
            {
                ItemID.GoblinBattleStandard,
                ItemID.SnowGlobe,
                ItemID.PirateMap,
                ItemID.TempleKey
            };
        }

        return [ItemID.GoblinBattleStandard, ItemID.SnowGlobe, ItemID.PirateMap];
    }
    #endregion
}
