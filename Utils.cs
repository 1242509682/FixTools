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

    #region 渐变着色方法 + 物品图标解析
    public static string TextGradient(string text, Color[]? colors = null)
    {
        // 处理空值或空字符串
        if (string.IsNullOrEmpty(text))
            return text;

        // 如果文本中已包含 [c/xxx:] 自定义颜色标签，则不做渐变，只替换图标
        if (text.Contains("[c/"))
        {
            return ReplaceIconsOnly(text);
        }

        var name = new StringBuilder();
        int length = text.Length;
        int Index = 0; // 渐变索引，排除换行符

        // 首先计算需要渐变的总字符数（排除换行符和图标标签）
        int CharCount = 0;
        for (int i = 0; i < length; i++)
        {
            // 检查是否是图标标签 [i:xxx] 或 [i/s数量:xxx]
            if (text[i] == '[' && i + 2 < length && text[i + 1] == 'i')
            {
                // 跳过整个图标标签
                int end = text.IndexOf(']', i);
                if (end != -1)
                {
                    i = end;
                    continue;
                }
            }
            else if (text[i] != '\n' && text[i] != '\r')
            {
                CharCount++;
            }
        }

        // 重置索引
        for (int i = 0; i < length; i++)
        {
            char c = text[i];

            // 处理换行符 - 直接保留
            if (c == '\n' || c == '\r')
            {
                name.Append(c);
                continue;
            }

            // 检查是否是图标标签 [i:xxx] 或 [i/s数量:xxx]
            if (c == '[' && i + 1 < length && text[i + 1] == 'i')
            {
                int end = text.IndexOf(']', i);
                if (end != -1)
                {
                    string tag = text.Substring(i, end - i + 1);

                    // 解析物品图标标签
                    if (TryParseItemTag(tag, out string iconTag))
                    {
                        name.Append(iconTag);
                    }
                    else
                    {
                        name.Append(tag); // 无效标签保留原样
                    }

                    i = end; // 跳过整个标签
                }
                else
                {
                    name.Append(c);
                    Index++;
                }
            }
            else
            {
                // 渐变颜色计算，排除换行符
                var start = colors is not null ? colors[0] : new Color(166, 213, 234);
                var endColor = colors is not null ? colors[1] : new Color(245, 247, 175);
                float ratio = CharCount <= 1 ? 0.5f : (float)Index / (CharCount - 1);
                var color = Color.Lerp(start, endColor, ratio);

                name.Append($"[c/{color.Hex3()}:{c}]");
                Index++;
            }
        }

        return name.ToString();
    }

    // 解析物品图标标签
    private static bool TryParseItemTag(string tag, out string result)
    {
        result = tag;

        // 匹配 [i:物品ID] 格式
        var match1 = Regex.Match(tag, @"^\[i:(\d+)\]$");
        if (match1.Success)
        {
            if (int.TryParse(match1.Groups[1].Value, out int itemID))
            {
                result = ItemIcon(itemID);
                return true;
            }
        }

        // 匹配 [i/s数量:物品ID] 格式
        var match2 = Regex.Match(tag, @"^\[i/s(\d+):(\d+)\]$");
        if (match2.Success)
        {
            if (int.TryParse(match2.Groups[2].Value, out int itemID))
            {
                int stack = int.Parse(match2.Groups[1].Value);
                result = ItemIcon(itemID, stack);
                return true;
            }
        }

        return false;
    }
    #endregion

    #region 最好的查找
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

    #region 将 string 转化为能直接作用于文件名的 string
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
            if (!Directory.Exists(CopyDir))
            {
                Directory.CreateDirectory(CopyDir);
                plr.SendMessage($"已创建复制源文件文件夹: {CopyDir}", color);
                plr.SendMessage($"请将文件放入此文件夹后重试", color);
                return;
            }

            // 获取源文件夹中的所有文件
            var srcFiles = Directory.GetFiles(CopyDir, "*", SearchOption.TopDirectoryOnly);

            if (srcFiles.Length == 0)
            {
                plr.SendMessage($"复制源文件文件夹中没有文件", color);
                plr.SendMessage($"文件夹路径: {CopyDir}", color);
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
            fileList.AppendLine($"{CopyDir}:");
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

    #region 只替换图标，不做渐变
    public static string ReplaceIconsOnly(string text)
    {
        if (string.IsNullOrEmpty(text))
            return text;

        var result = new StringBuilder();
        int index = 0;
        int length = text.Length;

        while (index < length)
        {
            char c = text[index];

            // 检查是否是图标标签 [i:xxx] 或 [i/s数量:xxx]
            if (c == '[' && index + 1 < length && text[index + 1] == 'i')
            {
                int end = text.IndexOf(']', index);
                if (end != -1)
                {
                    string tag = text.Substring(index, end - index + 1);

                    if (TryParseItemTag(tag, out string iconTag))
                    {
                        result.Append(iconTag);
                    }
                    else
                    {
                        result.Append(tag);
                    }

                    index = end + 1;
                }
                else
                {
                    result.Append(c);
                    index++;
                }
            }
            else
            {
                result.Append(c);
                index++;
            }
        }

        return result.ToString();
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
}
