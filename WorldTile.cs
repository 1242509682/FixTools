using System.IO.Compression;
using System.Text;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;
using Terraria;
using Terraria.DataStructures;
using System.Diagnostics;
using Terraria.ID;
using Terraria.Utilities;
using TShockAPI;
using static FixTools.FixTools;
using static FixTools.PlayerState;
using static FixTools.Utils;

namespace FixTools;

/// <summary>
/// 世界图格操作类，提供地图快照修复、建筑复制粘贴、撤销等功能。
/// </summary>
public static class WorldTile
{
    #region 内部数据结构
    // 内部类 TileData：用于封装一个区域内的所有图格、箱子、实体和标牌数据
    public class TileData
    {
        public Tile[,]? Tiles; // 图格二维数组，[x,y] 对应区域内的图格
        public List<Chest>? Chests; // 箱子列表
        public List<EntityData>? EntData; // 实体列表（如训练假人、物品框等）
        public List<Sign>? Signs; // 标牌列表
    }

    // 内部类 EntityData：用于存储实体的必要信息
    public class EntityData
    {
        public byte Type; // 实体类型（例如 0=训练假人，1=物品框等）
        public short X, Y; // 实体在世界中的坐标
        public byte[]? ExtraData; // 实体的额外二进制数据（用于序列化/反序列化）
    }

    // 内部类 UndoOperation：撤销操作记录，保存操作前后的区域状态
    public class UndoOperation
    {
        public Rectangle Area { get; set; } // 操作影响的矩形区域
        public TileData BeforeState { get; set; } = new(); // 操作前的区域数据
        public DateTime Timestamp { get; set; } // 操作时间戳，用于排序或清理旧记录
    }
    #endregion

    #region 常量与辅助字段
    // 文件扩展名常量
    private static readonly string TwsExt = ".tws";      // 世界快照文件扩展名
    private static readonly string SgnExt = ".sgn";      // 标牌文件扩展名
    private static readonly string SnapPre = "snap_";    // 快照临时文件前缀
    private static readonly string SignPre = "sign_";    // 标牌临时文件前缀
    // 建筑存档目录
    private static readonly string ClipDir = Path.Combine(MainPath, "建筑存档");
    // 修复临时目录（存放加载的快照文件）
    private static readonly string RestoreDir = Path.Combine(MainPath, "建筑修复");
    #endregion

    #region 主指令处理 /pt rw
    /// <summary>
    /// 处理 /pt rw 主命令，根据子命令分发到不同功能
    /// </summary>
    public static void DoSnapshot(CommandArgs args, TSPlayer plr)
    {
        // 参数数量不足2（即只有 /pt rw 没有子命令）则显示帮助
        if (args.Parameters.Count < 2)
        {
            ShowHelp(plr);
            return;
        }

        string sub = args.Parameters[1].ToLower(); // 获取子命令（小写）
        switch (sub)
        {
            case "fix":
            case "修复": // 修复子命令
                HandleFix(args, plr);
                break;

            case "bk":
            case "撤销": // 撤销子命令
                UndoCmd(plr);
                break;

            case "add":
            case "sv":
            case "save":
            case "copy":
            case "复制": // 复制子命令
                HandleAdd(args, plr);
                break;

            case "pt":
            case "sp":
            case "spawn":
            case "create":
            case "paste":
            case "粘贴": // 粘贴子命令
                HandlePaste(args, plr);
                break;

            default: // 未知子命令，显示帮助
                ShowHelp(plr);
                break;
        }
    }

    /// <summary>
    /// 显示 /pt rw 的帮助信息
    /// </summary>
    private static void ShowHelp(TSPlayer plr)
    {
        var mess = new StringBuilder();
        mess.AppendLine($"/{pt} rw fix [索引] ——从备份修复区域");
        mess.AppendLine($"/{pt} rw sv <名称>   ——输入名称后，用蓝图复制建筑");
        mess.AppendLine($"/{pt} rw sp [名称/索引] ——粘贴建筑到头顶");
        mess.AppendLine($"/{pt} rw bk ——撤销上次操作\n");

        mess.AppendLine($"修复区域:无参数时列出自动备份");
        mess.AppendLine($"粘贴建筑:无参数时列出建筑");
        plr.SendMessage(TextGradient($"{mess.ToString()}"), color); // 发送消息
    }
    #endregion

    #region 修复子命令
    /// <summary>
    /// 处理 fix 子命令：加载备份文件，准备修复区域
    /// </summary>
    private static void HandleFix(CommandArgs args, TSPlayer plr)
    {
        // 如果参数不足3（即没有指定索引），则列出所有备份
        if (args.Parameters.Count < 3)
        {
            var list = GetBakList(); // 获取备份文件列表（仅文件名，格式 "索引. 文件名"）
            if (list.Count == 0)
            {
                plr.SendMessage("暂无自动备份", color);
                return;
            }
            var sb = new StringBuilder("当前备份:\n");
            foreach (var item in list) sb.AppendLine(item);
            plr.SendMessage(TextGradient(sb.ToString()), color); // 渐变颜色输出
            plr.SendMessage($"用法: /{pt} rw fix <索引>", color);
            plr.SendMessage($"注意:索引为文件名前面的序号", color);
            return;
        }

        // 尝试解析索引
        if (!int.TryParse(args.Parameters[2], out int idx) || idx < 1)
        {
            plr.SendMessage("索引必须是大于0的数字", color);
            return;
        }

        // 获取玩家数据（来自 PlayerState 类）
        var Mydata = PlayerState.GetData(plr.Name);
        if (Mydata == null) return;

        // 获取所有备份文件的完整路径数组
        var files = GetBakFiles();
        if (idx > files.Length)
        {
            plr.SendMessage($"索引超出范围，共有 {files.Length} 个备份", color);
            return;
        }

        // 确保修复临时目录存在
        if (!Directory.Exists(RestoreDir)) Directory.CreateDirectory(RestoreDir);

        string zipPath = files[idx - 1]; // 根据索引选取备份文件路径
        using (var zip = ZipFile.OpenRead(zipPath)) // 打开 ZIP 压缩包
        {
            // 查找 .tws 世界快照文件条目
            var snapEntry = zip.Entries.FirstOrDefault(e => e.Name.EndsWith(TwsExt));
            if (snapEntry == null)
            {
                plr.SendMessage($"备份文件 {Path.GetFileName(zipPath)} 中未找到世界快照", color);
                return;
            }

            // 将快照文件解压到临时目录，文件名包含时间戳
            string snapPath = Path.Combine(RestoreDir, $"{SnapPre}{DateTime.Now:HHmmss}{TwsExt}");
            snapEntry.Open().CopyTo(File.Create(snapPath));
            Mydata.rwSnap = snapPath; // 保存路径到玩家数据

            // 查找 .sgn 标牌文件条目（可选）
            var signEntry = zip.Entries.FirstOrDefault(e => e.Name.EndsWith(SgnExt, StringComparison.OrdinalIgnoreCase));
            if (signEntry != null)
            {
                string signPath = Path.Combine(RestoreDir, $"{SignPre}{DateTime.Now:HHmmss}{SgnExt}");
                signEntry.Open().CopyTo(File.Create(signPath));
                Mydata.rwSign = signPath; // 保存标牌文件路径
            }

            plr.SendMessage(TextGradient($"成功加载世界快照: {snapEntry.Name}"), color);
        }

        Mydata.rwFix = true; // 标记玩家进入修复模式
        plr.SendMessage("请使用 [i:3611] 红电线[i:530] 拉取需要恢复的区域", color2);
    }
    #endregion

    #region 撤销子命令
    /// <summary>
    /// 处理 bk 子命令：撤销上一次操作
    /// </summary>
    private static void UndoCmd(TSPlayer plr)
    {
        // 弹出该玩家的撤销操作栈顶元素
        var op = PopUndo(plr.Name);
        if (op == null)
        {
            plr.SendMessage("\n没有可撤销的操作记录", color);
            return;
        }

        // 修复图格：将区域还原为操作前的状态，count 接收修复的图格数量
        int count = FixTile(op.Area, op.BeforeState, 0);
        // 修复箱子/实体/标牌
        FixItem(op.BeforeState);
        plr.SendMessage(TextGradient($"\n撤销成功！恢复 {count} 个图格"), color);
    }
    #endregion

    #region 复制子命令（先输入名称）
    /// <summary>
    /// 处理 sv 等保存子命令：记录要保存的建筑名称，然后等待玩家用红电线框选区域
    /// </summary>
    private static void HandleAdd(CommandArgs args, TSPlayer plr)
    {
        if (args.Parameters.Count < 3)
        {
            plr.SendMessage("请指定建筑名称: /pt rw sv <名称>", color);
            return;
        }

        string name = args.Parameters[2];
        // 检查文件名非法字符
        if (name.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
        {
            plr.SendMessage("建筑名称包含非法字符", color);
            return;
        }

        if (!Directory.Exists(ClipDir))
            Directory.CreateDirectory(ClipDir);

        string path = GetClipPath(name); // 获取建筑文件完整路径
        if (File.Exists(path))
        {
            plr.SendMessage($"建筑 '{name}' 已存在，请使用其他名称",color);
            return;
        }

        // 将建筑名称存入玩家数据，等待红电线拉取区域时使用
        GetData(plr.Name).rwCopy = name;
        plr.SendMessage($"准备保存建筑 {name}\n" +
                        $"请使用 [i:3611] 红电线[i:530] 拉取需要复制的区域",color);
    }
    #endregion

    #region 粘贴子命令
    /// <summary>
    /// 处理 sp 等粘贴子命令：粘贴指定建筑到玩家头顶
    /// </summary>
    private static void HandlePaste(CommandArgs args, TSPlayer plr)
    {
        // 无参数时列出所有可用建筑
        if (args.Parameters.Count < 3)
        {
            var names = GetClipNames(); // 获取所有建筑名称（不带扩展名）
            if (names.Count == 0)
            {
                plr.SendMessage("暂无保存的建筑", color);
                return;
            }
            var sb = new StringBuilder("可用建筑:\n");
            for (int i = 0; i < names.Count; i++)
                sb.AppendLine($"{i + 1}. {names[i]}");
            plr.SendMessage(TextGradient(sb.ToString()), color);
            plr.SendMessage($"用法: /{pt} rw sp <名称/索引>", color);
            return;
        }

        string input = args.Parameters[2];
        TileData? clip = null;
        // 尝试按索引解析
        if (int.TryParse(input, out int idx))
        {
            var names = GetClipNames();
            if (idx < 1 || idx > names.Count)
            {
                plr.SendMessage($"索引 {idx} 无效，共 {names.Count} 个建筑", color);
                return;
            }
            clip = LoadClip(names[idx - 1]); // 根据名称加载建筑数据
        }
        else
        {
            clip = LoadClip(input); // 直接按名称加载
        }

        if (clip == null)
        {
            plr.SendMessage($"未找到建筑 '{input}'", color);
            return;
        }

        // 获取建筑尺寸
        int w = clip.Tiles?.GetLength(0) ?? 0;
        int h = clip.Tiles?.GetLength(1) ?? 0;
        if (w == 0 || h == 0)
        {
            plr.SendMessage("建筑数据无效", color);
            return;
        }

        // 计算粘贴位置：玩家头顶（y 偏移为 -h，x 居中）
        int px = plr.TileX;
        int py = plr.TileY;
        int startX = px - w / 2;
        int startY = py - h; // 头顶模式，建筑底部对齐玩家脚下？实际上是玩家坐标的 y 减去高度，即建筑顶部对齐玩家头顶？

        // 检查是否超出世界边界
        if (startX < 0 || startX + w >= Main.maxTilesX || startY < 0 || startY + h >= Main.maxTilesY)
        {
            plr.SendMessage("目标区域超出世界边界", color);
            return;
        }

        Rectangle rect = new Rectangle(startX, startY, w, h);
        // 保存粘贴前的区域状态以便撤销
        var before = GetTileDataFromWorld(rect);
        PushUndo(plr.Name, new UndoOperation { Area = rect, BeforeState = before, Timestamp = DateTime.Now });

        // 将建筑数据偏移到目标坐标
        TileData? data = CloneWithOffset(clip, startX, startY);

        int count = 0;
        var sw = System.Diagnostics.Stopwatch.StartNew();
        // 异步执行粘贴操作，避免阻塞主线程
        Task.Run(() =>
        {
            // 先清除目标区域内的所有箱子、实体、标牌
            KillAll(startX, startX + w - 1, startY, startY + h - 1);
            count = FixTile(rect, data, count); // 粘贴图格
        }).ContinueWith(_ =>
        {
            FixItem(data); // 粘贴箱子、实体、标牌
            sw.Stop();
            var mess = $"粘贴 {input} 完成！已创造: {count} 个图格, 用时 {sw.ElapsedMilliseconds} ms";

            if (GetData(plr.Name).rwUndoStack.Count > 0) 
                mess += $"\n撤销操作:/pt rw bk";

            plr.SendMessage(TextGradient(mess), color);
        });
    }

    /// <summary>
    /// 从建筑文件加载 TileData
    /// </summary>
    private static TileData? LoadClip(string name)
    {
        string path = GetClipPath(name);
        if (!File.Exists(path)) return null;
        using var baseStream = OpenGzip(path);
        using var reader = new BinaryReader(baseStream);
        return ReadTileData(reader);
    }

    /// <summary>
    /// 获取所有建筑名称列表（不带扩展名）
    /// </summary>
    private static List<string> GetClipNames()
    {
        if (!Directory.Exists(ClipDir)) return new List<string>();
        return Directory.GetFiles(ClipDir, "*.clip")
                        .Select(f => Path.GetFileNameWithoutExtension(f)).ToList();
    }

    /// <summary>
    /// 根据建筑名称获取完整文件路径
    /// </summary>
    private static string GetClipPath(string name) => Path.Combine(ClipDir, $"{name}.clip");
    #endregion

    #region 建筑数据克隆与偏移
    /// <summary>
    /// 克隆建筑数据，并将所有坐标偏移到目标位置
    /// </summary>
    private static TileData CloneWithOffset(TileData src, int offX, int offY)
    {
        var dst = new TileData();

        // 复制图格（图格本身不包含坐标，直接复制）
        if (src.Tiles != null)
        {
            int w = src.Tiles.GetLength(0);
            int h = src.Tiles.GetLength(1);
            dst.Tiles = new Tile[w, h];
            for (int x = 0; x < w; x++)
                for (int y = 0; y < h; y++)
                    dst.Tiles[x, y] = (Tile)src.Tiles[x, y].Clone();
        }

        // 复制箱子，并偏移坐标
        if (src.Chests != null)
        {
            dst.Chests = new List<Chest>();
            foreach (var chest in src.Chests)
            {
                var newChest = new Chest(0, chest.x + offX, chest.y + offY, false, chest.maxItems)
                {
                    name = chest.name ?? "",
                    item = new Item[chest.maxItems]
                };
                for (int i = 0; i < chest.maxItems; i++)
                    newChest.item[i] = chest.item[i]?.Clone() ?? new Item();
                dst.Chests.Add(newChest);
            }
        }

        // 复制实体，偏移坐标
        if (src.EntData != null)
        {
            dst.EntData = new List<EntityData>();
            foreach (var ent in src.EntData)
            {
                dst.EntData.Add(new EntityData
                {
                    Type = ent.Type,
                    X = (short)(ent.X + offX),
                    Y = (short)(ent.Y + offY),
                    ExtraData = ent.ExtraData?.ToArray()
                });
            }
        }

        // 复制标牌，偏移坐标
        if (src.Signs != null)
        {
            dst.Signs = new List<Sign>();
            foreach (var sign in src.Signs)
            {
                dst.Signs.Add(new Sign { x = sign.x + offX, y = sign.y + offY, text = sign.text });
            }
        }

        return dst;
    }
    #endregion

    #region 精密线控仪事件
    /// <summary>
    /// 处理 MassWireOperation 事件（玩家使用精密线控仪时触发）
    /// </summary>
    public static void FixSnapshot(GetDataHandlers.MassWireOperationEventArgs e, TSPlayer plr)
    {
        var Mydata = GetData(plr.Name);
        if (e.ToolMode != 1) return; // 只处理红电线（ToolMode 1 表示红电线）

        // 计算框选区域的边界（确保 x1<=x2, y1<=y2）
        int x1 = Math.Min(e.StartX, e.EndX);
        int y1 = Math.Min(e.StartY, e.EndY);
        int x2 = Math.Max(e.StartX, e.EndX);
        int y2 = Math.Max(e.StartY, e.EndY);
        x2++; y2++; // 包含终点图格（通常拉线时是点对点，这里扩展到包含整个矩形）
        Rectangle rect = new Rectangle(x1, y1, x2 - x1, y2 - y1);

        if (Mydata.rwFix) // 修复模式
        {
            string snapPath = Mydata.rwSnap;
            string signPath = Mydata.rwSign;
            if (string.IsNullOrEmpty(snapPath) || !File.Exists(snapPath))
            {
                plr.SendMessage($"[{PluginName}] 快照文件已丢失，请重新选择备份", color);
                Mydata.rwFix = false;
                Mydata.rwSnap = string.Empty;
                Mydata.rwSign = string.Empty;
                return;
            }

            plr.SendMessage(TextGradient($"\n正在从备份恢复 ({x1},{y1}) => ({x2},{y2})"), color);

            // 从快照文件中读取指定区域的数据
            var data = ReadWorldTiles(snapPath, rect, signPath);
            if (data == null) return;

            // 保存当前区域状态以便撤销
            var beforeState = GetTileDataFromWorld(rect);
            PushUndo(plr.Name, new UndoOperation { Area = rect, BeforeState = beforeState, Timestamp = DateTime.Now });

            var count = 0;
            var sw = Stopwatch.StartNew();

            Task.Run(() =>
            {
                // 清除区域内的箱子/实体/标牌
                KillAll(rect.Left, rect.Right, rect.Top, rect.Bottom);
                count = FixTile(rect, data, count); // 恢复图格

            }).ContinueWith(_ =>
            {
                FixItem(data); // 恢复箱子/实体/标牌
                // 删除临时快照文件
                if (File.Exists(snapPath)) File.Delete(snapPath);
                if (File.Exists(signPath)) File.Delete(signPath);

                // 清除玩家的修复状态
                Mydata.rwFix = false;
                Mydata.rwSnap = string.Empty;
                Mydata.rwSign = string.Empty;
                sw.Stop();
                var mess = $"已恢复区域: {count} 个图格, 用时 {sw.ElapsedMilliseconds} ms";
                if (Mydata.rwUndoStack.Count > 0)
                    mess += $"\n撤销操作:/pt rw bk";
                plr.SendMessage(TextGradient(mess), color);
            });

            e.Handled = true; // 标记事件已处理，阻止后续逻辑
        }
        else if (!string.IsNullOrEmpty(Mydata.rwCopy))
        {
            // 检查是否有待保存的建筑
            SaveBuilding(plr, Mydata.rwCopy, rect); // 保存建筑
            Mydata.rwCopy = string.Empty; // 清空状态
            e.Handled = true;
        }
    }
    #endregion

    #region 保存建筑到文件
    /// <summary>
    /// 将指定矩形区域的图格、箱子、实体、标牌保存为建筑文件（相对坐标）
    /// </summary>
    private static void SaveBuilding(TSPlayer plr, string name, Rectangle rect)
    {
        var clip = GetTileDataFromWorld(rect); // 获取世界区域数据

        // 转换为相对坐标（相对于矩形左上角）
        var newClip = new TileData
        {
            Tiles = clip.Tiles, // 图格直接复用，因为图格没有坐标字段
            Chests = clip.Chests?.Select(c => new Chest(0, c.x - rect.X, c.y - rect.Y, c.bankChest, c.maxItems)
            {
                name = c.name,
                item = c.item.Select(i => i?.Clone() ?? new Item()).ToArray()
            }).ToList(),

            EntData = clip.EntData?.Select(e => new EntityData
            {
                Type = e.Type,
                X = (short)(e.X - rect.X),
                Y = (short)(e.Y - rect.Y),
                ExtraData = e.ExtraData?.ToArray()
            }).ToList(),

            Signs = clip.Signs?.Select(s => new Sign
            {
                x = s.x - rect.X,
                y = s.y - rect.Y,
                text = s.text
            }).ToList()
        };

        string path = GetClipPath(name);
        using var fs = new FileStream(path, FileMode.Create);
        using var gz = new GZipStream(fs, CompressionLevel.Optimal);
        using var writer = new BinaryWriter(gz);
        WriteTileData(writer, newClip); // 序列化写入

        plr.SendMessage($"已保存建筑 '{name}' ({rect.Width}x{rect.Height})", color);
        // 清除玩家的区域点标记（来自 TShock 的 TempPoints）
        plr.TempPoints[0] = Point.Zero;
        plr.TempPoints[1] = Point.Zero;
    }
    #endregion

    #region 修复图格（核心）- 不分块，直接发送整个区域
    /// <summary>
    /// 将指定区域的图格替换为 TileData 中的数据，并返回修改的图格数量。
    /// 注意：直接发送整个矩形区域更新，若区域过大可能导致网络异常，请谨慎使用。
    /// </summary>
    private static int FixTile(Rectangle rect, TileData data, int count)
    {
        if (data.Tiles != null)
        {
            for (int x = 0; x < rect.Width; x++)
            {
                for (int y = 0; y < rect.Height; y++)
                {
                    int wx = rect.X + x, wy = rect.Y + y;
                    if (wx < 0 || wx >= Main.maxTilesX ||
                        wy < 0 || wy >= Main.maxTilesY) continue;

                    var backup = data.Tiles[x, y];      // 要恢复的图格
                    var current = Main.tile[wx, wy] ?? new Tile(); // 当前图格（若为 null 则新建）

                    // 使用 TileSnapshot.TileStruct 比较两个图格是否相同（避免不必要的网络发送）
                    var tsBackup = TileSnapshot.TileStruct.From(backup);
                    var tsCurrent = TileSnapshot.TileStruct.From(current);
                    if (!tsBackup.Equals(tsCurrent))
                    {
                        current.CopyFrom(backup); // 复制数据
                        count++;
                        NetMessage.SendTileSquare(-1, wx, wy); // 向所有客户端发送该图格的更新
                    }
                }
            }
        }

        return count;
    }
    #endregion

    #region 修复家具/实体/箱子/标牌
    /// <summary>
    /// 根据 TileData 中的数据，在世界上放置箱子、实体和标牌
    /// </summary>
    private static void FixItem(TileData data)
    {
        if (data.EntData != null)
        {
            foreach (var ed in data.EntData)
            {
                // 放置实体，返回实体 ID
                int id = TileEntity.Place(ed.X, ed.Y, ed.Type);
                if (id == -1 || ed.ExtraData == null) continue;
                if (!TileEntity.ByID.TryGetValue(id, out var ent)) continue;

                // 从 ExtraData 中读取实体的额外数据（需要模拟 BinaryReader）
                using var ms = new MemoryStream(ed.ExtraData);
                using var br = new BinaryReader(ms);

                // 跳过 TileEntity.Write 写入的前缀（type, id, X, Y）
                br.ReadByte(); // type
                br.ReadInt32(); // id
                br.ReadInt16(); // X
                br.ReadInt16(); // Y

                // 根据游戏版本读取剩余数据
                var GameVersion = Config.GameVersion == -1 ? GameVersionID.Latest : Config.GameVersion;
                ent.ReadExtraData(br, GameVersion, false);
            }
        }

        if (data.Chests != null)
        {
            foreach (var chest in data.Chests)
            {
                // 创建箱子，返回箱子索引
                int idx = Chest.CreateChest(chest.x, chest.y); 
                if (idx == -1) continue;
                var target = Main.chest[idx];
                target.name = chest.name ?? "";
                target.maxItems = chest.maxItems;
                for (int s = 0; s < chest.maxItems; s++)
                    target.item[s] = chest.item[s]?.Clone() ?? new Item();
            }
        }

        if (data.Signs != null)
        {
            foreach (var sign in data.Signs)
            {
                // 读取标牌（如果不存在则创建）
                int sid = Sign.ReadSign(sign.x, sign.y, true); 
                Main.sign[sid].text = sign.text; // 设置文本
            }
        }

        // 强制所有玩家重新加载图格区域（将他们的 TileSections 标记为未加载）
        for (int i = 0; i < TShock.Players.Length; i++)
            if (TShock.Players[i]?.Active == true)
                for (int j = 0; j < Main.maxSectionsX; j++)
                    for (int k = 0; k < Main.maxSectionsY; k++)
                        Netplay.Clients[i].TileSections[j, k] = false;
    }
    #endregion

    #region 保存世界快照（自动备份调用）
    /// <summary>
    /// 保存当前世界的快照（图格和标牌）到指定目录，用于自动备份
    /// </summary>
    public static void SaveSnapshot(TSPlayer plr, bool showMag, string worldName, string exportDir)
    {
        string tmpPath = Path.GetTempFileName(); // 临时文件
        TileSnapshot.Create();    // 创建世界快照
        TileSnapshot.Save(tmpPath); // 保存到临时文件
        TileSnapshot.Clear();      // 清理快照

        string snapPath = Path.Combine(exportDir, $"{worldName}{TwsExt}");
        using (var fs = new FileStream(snapPath, FileMode.Create))
        using (var gz = new GZipStream(fs, CompressionLevel.Optimal))
        using (var tmpFs = File.OpenRead(tmpPath))
        {
            tmpFs.CopyTo(gz); // 压缩临时文件到目标
        }
        File.Delete(tmpPath);
        if (showMag) plr.SendMessage($"已保存世界快照: {worldName}{TwsExt} (GZIP压缩)", color2);

        // 保存标牌
        string signPath = Path.Combine(exportDir, $"{worldName}{SgnExt}");
        var signs = new List<Sign>();
        for (int i = 0; i < Main.sign.Length; i++)
        {
            var s = Main.sign[i];
            if (s != null && !string.IsNullOrEmpty(s.text))
                signs.Add(new Sign { x = s.x, y = s.y, text = s.text });
        }
        SaveSigns(signPath, signs);
        if (showMag) plr.SendMessage($"已保存标牌: {worldName}{SgnExt} (GZIP压缩)", color2);
    }
    #endregion

    #region 读取世界快照
    /// <summary>
    /// 从世界快照文件中读取指定矩形区域的数据
    /// </summary>
    public static TileData? ReadWorldTiles(string path, Rectangle rect, string? signPath = null)
    {
        if (!File.Exists(path))
        {
            TShock.Log.ConsoleError($"[{PluginName}] 快照文件不存在: " + path);
            return null;
        }

        try
        {
            using var stream = OpenGzip(path);
            using var ms = new MemoryStream();
            stream.CopyTo(ms);
            ms.Position = 0;
            using var br = new BinaryReader(ms);
            TileSnapshot.Load(br); // 从流中加载快照到 TileSnapshot 静态类

            if (!TileSnapshot.IsCreated)
            {
                TShock.Log.ConsoleError($"[{PluginName}] 地图快照加载失败");
                return null;
            }

            int w = TileSnapshot._worldFile.WorldSizeX; // 快照世界的宽度
            int h = TileSnapshot._worldFile.WorldSizeY; // 快照世界的高度
            if (rect.X < 0 || rect.Y < 0 || rect.Right > w || rect.Bottom > h)
            {
                TileSnapshot.Clear();
                return null;
            }

            var res = new TileData
            {
                Tiles = new Tile[rect.Width, rect.Height],
                Chests = new List<Chest>(),
                EntData = new List<EntityData>(),
                Signs = new List<Sign>()
            };

            // 从快照中复制图格
            for (int x = 0; x < rect.Width; x++)
                for (int y = 0; y < rect.Height; y++)
                {
                    int wx = rect.X + x, wy = rect.Y + y;
                    var ts = TileSnapshot._tiles[wx * h + wy]; // 快照图格（一维数组，索引 = x * 高度 + y）
                    res.Tiles[x, y] = new Tile();
                    ts.Apply(res.Tiles[x, y]); // 将快照数据应用到新 Tile 对象
                }

            // 复制箱子（只要箱子覆盖的任何一个图格在区域内就包含）
            foreach (var c in TileSnapshot._chests)
            {
                if (c == null) continue;
                if (rect.Contains(c.x, c.y) || rect.Contains(c.x + 1, c.y) ||
                    rect.Contains(c.x, c.y + 1) || rect.Contains(c.x + 1, c.y + 1))
                    res.Chests.Add(c.CloneWithSeparateItems()); // 深拷贝箱子及其物品
            }

            // 复制实体
            foreach (var e in TileSnapshot._tileEntities)
            {
                if (e == null || !rect.Contains(e.Position.X, e.Position.Y)) continue;
                using var msEnt = new MemoryStream();
                using var bw = new BinaryWriter(msEnt);
                TileEntity.Write(bw, e); // 将实体序列化到流
                res.EntData.Add(new EntityData
                {
                    Type = e.type,
                    X = e.Position.X,
                    Y = e.Position.Y,
                    ExtraData = msEnt.ToArray()
                });
            }

            TileSnapshot.Clear(); // 清理快照

            if (signPath != null && File.Exists(signPath))
                res.Signs = LoadSigns(signPath); // 加载标牌

            return res;
        }
        catch (Exception ex)
        {
            TShock.Log.ConsoleError($"[{PluginName}] 快照读取失败: {ex.Message}");
            TileSnapshot.Clear();
            return null;
        }
    }
    #endregion

    #region 销毁区域实体
    /// <summary>
    /// 销毁指定矩形区域内的所有箱子、实体和标牌
    /// </summary>
    public static void KillAll(int startX, int endX, int startY, int endY)
    {
        Rectangle rect = new Rectangle(startX, startY, endX - startX + 1, endY - startY + 1);

        // 移除所有在区域内的实体
        var toRemove = TileEntity.ByPosition.Values
            .Where(te => rect.Contains(te.Position.X, te.Position.Y)).ToList();
        foreach (var te in toRemove) TileEntity.Remove(te);

        // 遍历区域内每个图格
        for (int x = startX; x <= endX; x++)
            for (int y = startY; y <= endY; y++)
            {
                var tile = Main.tile[x, y];
                if (tile == null || !tile.active()) continue;

                // 如果是箱子类图格，销毁箱子
                if (TileID.Sets.BasicChest[tile.type] ||
                    TileID.Sets.BasicChestFake[tile.type] ||
                    TileID.Sets.BasicDresser[tile.type])
                    Chest.DestroyChest(x, y);

                // 如果是标牌类图格，销毁标牌
                if (tile.type == TileID.Signs ||
                    tile.type == TileID.Tombstones ||
                    tile.type == TileID.AnnouncementBox)
                    Sign.KillSign(x, y);
            }
    }
    #endregion

    #region 撤销栈操作
    /// <summary>
    /// 将玩家的撤销栈保存到文件（GZip 压缩）
    /// </summary>
    private static void SaveUndoStack(string playerName, Stack<UndoOperation> stack)
    {
        if (!Directory.Exists(RestoreDir)) Directory.CreateDirectory(RestoreDir);
        string path = Path.Combine(RestoreDir, $"{playerName}_undo.bak");
        using var fs = new FileStream(path, FileMode.Create);
        using var gz = new GZipStream(fs, CompressionLevel.Optimal);
        using var writer = new BinaryWriter(gz);
        writer.Write(stack.Count); // 写入栈大小
        foreach (var op in stack)
        {
            writer.Write(op.Area.X);
            writer.Write(op.Area.Y);
            writer.Write(op.Area.Width);
            writer.Write(op.Area.Height);
            writer.Write(op.Timestamp.Ticks);
            WriteTileData(writer, op.BeforeState); // 写入操作前的数据
        }
    }

    /// <summary>
    /// 从文件加载玩家的撤销栈
    /// </summary>
    private static Stack<UndoOperation> LoadUndoStack(string playerName)
    {
        string path = Path.Combine(RestoreDir, $"{playerName}_undo.bak");
        if (!File.Exists(path)) return new Stack<UndoOperation>();
        using var stream = OpenGzip(path);
        using var reader = new BinaryReader(stream);
        int count = reader.ReadInt32();
        var list = new List<UndoOperation>(count);
        for (int i = 0; i < count; i++) list.Add(ReadUndoOperation(reader));
        list.Reverse(); // 因为栈是后进先出，从文件读出的顺序是栈底到栈顶，反转后便于 Push/Pop
        return new Stack<UndoOperation>(list);
    }

    /// <summary>
    /// 从 BinaryReader 读取一个 UndoOperation 对象
    /// </summary>
    private static UndoOperation ReadUndoOperation(BinaryReader reader)
    {
        return new UndoOperation
        {
            Area = new Rectangle(reader.ReadInt32(), reader.ReadInt32(), reader.ReadInt32(), reader.ReadInt32()),
            Timestamp = new DateTime(reader.ReadInt64()),
            BeforeState = ReadTileData(reader)
        };
    }

    /// <summary>
    /// 向玩家的撤销栈压入一个操作（保存到文件）
    /// </summary>
    public static void PushUndo(string playerName, UndoOperation op)
    {
        var stack = LoadUndoStack(playerName);
        stack.Push(op);
        SaveUndoStack(playerName, stack);
    }

    /// <summary>
    /// 从玩家的撤销栈弹出一个操作（从文件加载后删除）
    /// </summary>
    public static UndoOperation? PopUndo(string playerName)
    {
        var stack = LoadUndoStack(playerName);
        if (stack.Count == 0) return null;
        var op = stack.Pop();
        SaveUndoStack(playerName, stack);
        return op;
    }
    #endregion

    #region TileData 序列化辅助
    /// <summary>
    /// 将 TileData 对象写入 BinaryWriter（用于保存到文件）
    /// </summary>
    private static void WriteTileData(BinaryWriter writer, TileData data)
    {
        // 写入图格数组维度
        if (data.Tiles == null) { writer.Write(0); writer.Write(0); }
        else
        {
            int w = data.Tiles.GetLength(0);
            int h = data.Tiles.GetLength(1);
            writer.Write(w); writer.Write(h);
            for (int x = 0; x < w; x++)
                for (int y = 0; y < h; y++)
                    WriteTile(writer, data.Tiles[x, y]);
        }

        // 写入箱子
        writer.Write(data.Chests?.Count ?? 0);
        if (data.Chests != null)
        {
            foreach (var c in data.Chests)
            {
                writer.Write(c.x); writer.Write(c.y); writer.Write(c.name ?? ""); writer.Write(c.maxItems);
                for (int i = 0; i < c.maxItems; i++)
                {
                    var item = c.item[i];
                    writer.Write(item?.type ?? 0); writer.Write(item?.stack ?? 0); writer.Write(item?.prefix ?? (byte)0);
                }
            }
        }

        // 写入实体
        writer.Write(data.EntData?.Count ?? 0);
        if (data.EntData != null)
        {
            foreach (var e in data.EntData)
            {
                writer.Write(e.Type); writer.Write(e.X); writer.Write(e.Y);
                writer.Write(e.ExtraData?.Length ?? 0);
                if (e.ExtraData != null) writer.Write(e.ExtraData);
            }
        }

        // 写入标牌
        writer.Write(data.Signs?.Count ?? 0);
        if (data.Signs != null)
        {
            foreach (var s in data.Signs)
            {
                writer.Write(s.x); writer.Write(s.y); writer.Write(s.text ?? "");
            }
        }
    }

    /// <summary>
    /// 从 BinaryReader 读取一个 TileData 对象
    /// </summary>
    private static TileData ReadTileData(BinaryReader reader)
    {
        var data = new TileData();
        int w = reader.ReadInt32(); int h = reader.ReadInt32();
        if (w > 0 && h > 0)
        {
            data.Tiles = new Tile[w, h];
            for (int x = 0; x < w; x++)
                for (int y = 0; y < h; y++)
                    data.Tiles[x, y] = ReadTile(reader);
        }

        int chestCount = reader.ReadInt32();
        if (chestCount > 0)
        {
            data.Chests = new List<Chest>();
            for (int i = 0; i < chestCount; i++)
            {
                int cx = reader.ReadInt32();
                int cy = reader.ReadInt32();
                string cname = reader.ReadString();
                int max = reader.ReadInt32();
                var chest = new Chest(0, cx, cy, false, max)
                {
                    name = cname,
                    item = new Item[max]
                };

                for (int s = 0; s < max; s++)
                {
                    int type = reader.ReadInt32();
                    int stack = reader.ReadInt32();
                    byte prefix = reader.ReadByte();
                    var item = new Item();
                    item.SetDefaults(type);
                    item.stack = stack;
                    item.prefix = prefix;
                    chest.item[s] = item;
                }
                data.Chests.Add(chest);
            }
        }

        int entCount = reader.ReadInt32();
        if (entCount > 0)
        {
            data.EntData = new List<EntityData>();
            for (int i = 0; i < entCount; i++)
            {
                var ent = new EntityData
                {
                    Type = reader.ReadByte(),
                    X = reader.ReadInt16(),
                    Y = reader.ReadInt16()
                };
                int extraLen = reader.ReadInt32();
                ent.ExtraData = reader.ReadBytes(extraLen);
                data.EntData.Add(ent);
            }
        }

        int signCount = reader.ReadInt32();
        if (signCount > 0)
        {
            data.Signs = new List<Sign>();
            for (int i = 0; i < signCount; i++)
                data.Signs.Add(new Sign
                {
                    x = reader.ReadInt32(),
                    y = reader.ReadInt32(),
                    text = reader.ReadString()
                });
        }
        return data;
    }

    /// <summary>
    /// 将单个 Tile 写入 BinaryWriter（仅写入必要字段）
    /// </summary>
    private static void WriteTile(BinaryWriter writer, Tile tile)
    {
        writer.Write(tile.bTileHeader);
        writer.Write(tile.bTileHeader2);
        writer.Write(tile.bTileHeader3);
        writer.Write(tile.frameX);
        writer.Write(tile.frameY);
        writer.Write(tile.liquid);
        writer.Write(tile.sTileHeader);
        writer.Write(tile.type);
        writer.Write(tile.wall);
    }

    /// <summary>
    /// 从 BinaryReader 读取单个 Tile
    /// </summary>
    private static Tile ReadTile(BinaryReader reader)
    {
        return new Tile
        {
            bTileHeader = reader.ReadByte(),
            bTileHeader2 = reader.ReadByte(),
            bTileHeader3 = reader.ReadByte(),
            frameX = reader.ReadInt16(),
            frameY = reader.ReadInt16(),
            liquid = reader.ReadByte(),
            sTileHeader = reader.ReadUInt16(),
            type = reader.ReadUInt16(),
            wall = reader.ReadUInt16()
        };
    }
    #endregion

    #region 获取世界区域数据
    /// <summary>
    /// 从当前世界获取指定矩形区域的图格、箱子、实体、标牌数据
    /// </summary>
    private static TileData GetTileDataFromWorld(Rectangle rect)
    {
        var data = new TileData
        {
            Tiles = new Tile[rect.Width, rect.Height],
            Chests = new List<Chest>(),
            EntData = new List<EntityData>(),
            Signs = new List<Sign>()
        };

        // 复制图格
        for (int x = 0; x < rect.Width; x++)
            for (int y = 0; y < rect.Height; y++)
            {
                int wx = rect.X + x, wy = rect.Y + y;
                var tile = Main.tile[wx, wy];
                data.Tiles[x, y] = (Tile)(tile?.Clone() ?? new Tile());
            }

        // 复制箱子（只要箱子覆盖的区域与矩形有交集）
        foreach (var c in Main.chest)
        {
            if (c == null) continue;
            if (rect.Contains(c.x, c.y) || rect.Contains(c.x + 1, c.y) || rect.Contains(c.x, c.y + 1) || rect.Contains(c.x + 1, c.y + 1))
            {
                var copy = new Chest(index: 0, x: c.x, y: c.y, bank: c.bankChest, maxItems: c.maxItems)
                {
                    name = c.name,
                    item = new Item[c.maxItems]
                };
                for (int i = 0; i < c.maxItems; i++) copy.item[i] = c.item[i]?.Clone() ?? new Item();
                data.Chests.Add(copy);
            }
        }

        // 复制实体
        foreach (var kv in TileEntity.ByPosition)
        {
            var pos = kv.Key;
            if (rect.Contains(pos.X, pos.Y))
            {
                using var ms = new MemoryStream();
                using var bw = new BinaryWriter(ms);
                TileEntity.Write(bw, kv.Value);
                data.EntData.Add(new EntityData
                {
                    Type = kv.Value.type,
                    X = pos.X,
                    Y = pos.Y,
                    ExtraData = ms.ToArray()
                });
            }
        }

        // 复制标牌
        foreach (var s in Main.sign)
            if (s != null && rect.Contains(s.x, s.y))
                data.Signs.Add(new Sign { x = s.x, y = s.y, text = s.text });

        return data;
    }
    #endregion

    #region GZip 辅助方法
    /// <summary>
    /// 检查文件是否为 GZip 压缩格式（通过魔数 0x1F 0x8B）
    /// </summary>
    private static bool IsGzip(string path)
    {
        using var fs = new FileStream(path, FileMode.Open, FileAccess.Read);
        return fs.Length >= 2 && fs.ReadByte() == 0x1F && fs.ReadByte() == 0x8B;
    }

    /// <summary>
    /// 打开文件，如果是 GZip 则返回解压流，否则返回普通文件流
    /// </summary>
    private static Stream OpenGzip(string path)
    {
        if (IsGzip(path))
        {
            var fs = new FileStream(path, FileMode.Open);
            return new GZipStream(fs, CompressionMode.Decompress);
        }
        return new FileStream(path, FileMode.Open);
    }
    #endregion

    #region 标牌序列化辅助
    /// <summary>
    /// 将标牌列表保存为 GZip 压缩的 JSON 文件
    /// </summary>
    private static void SaveSigns(string path, List<Sign> signs)
    {
        string json = JsonConvert.SerializeObject(signs, Formatting.None);
        byte[] bytes = Encoding.UTF8.GetBytes(json);
        using var fs = new FileStream(path, FileMode.Create);
        using var gz = new GZipStream(fs, CompressionLevel.Optimal);
        gz.Write(bytes, 0, bytes.Length);
    }

    /// <summary>
    /// 从 GZip 压缩的 JSON 文件加载标牌列表
    /// </summary>
    private static List<Sign> LoadSigns(string path)
    {
        using var stream = OpenGzip(path);
        using var sr = new StreamReader(stream);
        string json = sr.ReadToEnd();
        return JsonConvert.DeserializeObject<List<Sign>>(json) ?? new List<Sign>();
    }
    #endregion
}