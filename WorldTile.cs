using System.IO.Compression;
using System.Text;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.Utilities;
using TShockAPI;
using static FixTools.FixTools;
using static FixTools.Utils;

namespace FixTools;

public static class WorldTile
{
    public class TileData
    {
        public Tile[,]? Tiles;      // 图格
        public List<Chest>? Chests; // 箱子
        public List<EntityData>? EntData;
        public List<Sign>? Signs;   // 标牌（需额外备份）
    }

    public class EntityData
    {
        public byte Type;          // 实体类型
        public short X, Y;         // 位置
        public byte[]? ExtraData;   // 额外数据（WriteExtraData 的输出）
    }

    #region 地图快照修复图格指令
    public static void DoSnapshot(CommandArgs args, TSPlayer plr)
    {
        // 参数不足1列出备份列表
        if (args.Parameters.Count < 2)
        {
            ListBak(plr);
            return;
        }

        // 获取玩家数据
        var Mydata = PlayerState.GetData(plr.Name);
        if (Mydata is null) return;

        #region 撤销指令
        if (args.Parameters.Count >= 2 && 
            args.Parameters[1].ToLower() == "b" ||
            args.Parameters[1].ToLower() == "bk")
        {
            if (Mydata.rwUndoStack.Count == 0)
            {
                plr.SendMessage("\n没有可撤销的操作记录", color);
                return;
            }

            // 弹出最新撤销快照（尾出）
            string undoPath = Mydata.rwUndoStack[^1];
            Mydata.rwUndoStack.RemoveAt(Mydata.rwUndoStack.Count - 1);

            if (!File.Exists(undoPath))
            {
                plr.SendErrorMessage("\n撤销快照文件已丢失，已自动移除");
                return;
            }

            // 从文件名解析矩形坐标
            string fileName = Path.GetFileNameWithoutExtension(undoPath);
            var parts = fileName.Split('_');
            if (parts.Length >= 7 &&
                int.TryParse(parts[2], out int x1) && int.TryParse(parts[3], out int y1) &&
                int.TryParse(parts[4], out int x2) && int.TryParse(parts[5], out int y2))
            {
                Rectangle rect = new Rectangle(x1, y1, x2 - x1, y2 - y1);
                var data = ReadWorldTiles(undoPath, rect, null);
                if (data != null)
                {
                    int count = FixTile(rect, data, 0);
                    FixItem(data);
                    plr.SendMessage(TextGradient($"\n撤销成功！恢复 {count} 个图格"), color);
                }
            }

            // 删除撤销快照文件
            File.Delete(undoPath);
            return;
        } 
        #endregion

        // 检查指令第一个参数，是否为整数 或 小于1
        if (!int.TryParse(args.Parameters[1], out int idx) || idx < 1)
        {
            ListBak(plr);
            plr.SendMessage($"索引是备份的序号,如:/{pt} rw 1", color);
            return;
        }

        // 检查第一个参数,是否超出备份总数上限
        var files = GetBakFiles();
        if (idx > files.Length)
        {
            ListBak(plr);
            plr.SendMessage($"索引超出范围，共有 {files.Length} 个备份", color);
            return;
        }

        // 获取压缩包的路径,并从1开始计为第一个
        string zipPath = files[idx - 1];
        using (var zip = ZipFile.OpenRead(zipPath))
        {
            // 获取以.tws结尾的快照文件
            var snapEntry = zip.Entries.FirstOrDefault(e => e.Name.EndsWith(".tws"));
            if (snapEntry == null)
            {
                plr.SendErrorMessage($"备份文件 {Path.GetFileName(zipPath)} 中未找到世界快照 (.tws)，请重新备份");
                return;
            }

            // 保存快照（可能为压缩格式，直接原样复制）
            string snapPath = Path.Combine(ReaderPlayer.ReaderDir, $"snap_{DateTime.Now:HHmmss}.tws");
            snapEntry.Open().CopyTo(File.Create(snapPath));
            Mydata.rwSnap = snapPath; // 给指令执行者设置个快照路径

            // 尝试保存标牌文件（可能不存在）
            var signEntry = zip.Entries.FirstOrDefault(e => e.Name.EndsWith(".sgn", StringComparison.OrdinalIgnoreCase));
            if (signEntry != null)
            {
                string signPath = Path.Combine(ReaderPlayer.ReaderDir, $"sign_{DateTime.Now:HHmmss}.sgn");
                signEntry.Open().CopyTo(File.Create(signPath));
                Mydata.rwSign = signPath; // 给指令执行者设置个标牌文件路径
            }

            plr.SendMessage(TextGradient($"成功加载世界快照: {snapEntry.Name}"), color);
        }

        Mydata.rwWire = true;
        plr.SendMessage("请使用 [i:3611] 红电线[i:530] 拉取需要恢复的区域", color2);
    }
    #endregion

    #region 精密线控仪 修复局部图格方法（从自动备份选择地图快照）
    public static void FixSnapshot(GetDataHandlers.MassWireOperationEventArgs e, TSPlayer plr)
    {
        var Mydata = PlayerState.GetData(plr.Name);

        // 如果没有输入/pt rw 指令选择备份，或者没有使用红电线模式则返回
        if (!Mydata.rwWire || e.ToolMode != 1)
            return;

        // 触发精密线控仪绘画
        e.Handled = true;

        // 从/pt rw 指令获取临时快照文件的路径
        string snapPath = Mydata.rwSnap;
        string signPath = Mydata.rwSign;
        if (string.IsNullOrEmpty(snapPath) || !File.Exists(snapPath))
        {
            plr.SendErrorMessage($"[{PluginName}] 快照文件已丢失，请重新选择备份");
            Mydata.rwWire = false;
            Mydata.rwSnap = string.Empty;
            Mydata.rwSign = string.Empty;
            return;
        }

        // 计算精密线控仪画出来的选区边界
        int x1 = Math.Min(e.StartX, e.EndX);
        int y1 = Math.Min(e.StartY, e.EndY);
        int x2 = Math.Max(e.StartX, e.EndX);
        int y2 = Math.Max(e.StartY, e.EndY);
        x2++; y2++;
        Rectangle rect = new Rectangle(x1, y1, x2 - x1, y2 - y1);
        plr.SendMessage(TextGradient($"\n正在从备份恢复 ({x1},{y1}) => ({x2},{y2})"), color);

        // 调用读取快照方法
        var data = ReadWorldTiles(snapPath, rect, signPath);
        if (data is null) return;

        #region 保存撤销快照
        // 生成撤销快照（文件名含矩形坐标+时间戳）
        string tmpUndo = Path.Combine(ReaderPlayer.ReaderDir,
            $"undo_{plr.Name}_{rect.X}_{rect.Y}_{rect.Right}_{rect.Bottom}_{DateTime.Now:HHmmss}.tws.tmp");
        TileSnapshot.Create();
        TileSnapshot.Save(tmpUndo);
        TileSnapshot.Clear();
        #endregion

        // 记录修复图格数量
        var count = 0;
        // 开始计时
        var sw = System.Diagnostics.Stopwatch.StartNew();

        // 异步执行避免堵塞主线程
        Task.Run(() =>
        {
            // 1. 销毁区域内原有实体
            KillAll(rect.Left, rect.Right, rect.Top, rect.Bottom);

            // 2. 恢复图格（仅当发生变化时）
            count = FixTile(rect, data, count);

        }).ContinueWith(_ =>
        {
            // 3. 根据是否实际修改决定保留/删除 撤销快照
            if (count > 0)
            {
                string finalUndo = tmpUndo[..^4]; // 去掉 .tmp
                File.Move(tmpUndo, finalUndo);
                Mydata.rwUndoStack.Add(finalUndo);
                plr.SendMessage(TextGradient($"区域有 {count} 个图格变化，已保存本次撤销操作"), color);
            }
            else
            {
                // 无变化：直接删除临时文件，不压栈
                File.Delete(tmpUndo);
                plr.SendMessage(TextGradient("区域无变化，不保存本次撤销操作"),color);
            }

            // 4. 恢复实体、箱子、标牌等
            FixItem(data);

            // 5. 清理临时文件(世界快照与标牌信息)
            if (File.Exists(snapPath))
                File.Delete(snapPath);
            if (File.Exists(signPath))
                File.Delete(signPath);

            // 6. 清理玩家状态
            Mydata.rwWire = false;
            Mydata.rwSnap = string.Empty;
            Mydata.rwSign = string.Empty;

            // 7. 停止计时，发送统计消息
            sw.Stop();
            var mess = $"已恢复区域: {count} 个图格, 用时 {sw.ElapsedMilliseconds} ms";
            if (Mydata.rwUndoStack.Count > 0) mess += $"\n撤销操作:/pt rw b";
            plr.SendMessage(TextGradient(mess), color);

        });
    }
    #endregion

    #region 修复图格方法
    private static int FixTile(Rectangle rect, TileData data, int count)
    {
        // 记录需要发送网络更新的区块（50x50）
        const int BLOCK_SIZE = 50;
        var dirtyBlocks = new HashSet<(int bx, int by)>();

        // 恢复图格
        if (data.Tiles != null)
        {
            for (int x = 0; x < rect.Width; x++)
            {
                for (int y = 0; y < rect.Height; y++)
                {
                    int wx = rect.X + x, wy = rect.Y + y;
                    if (wx < 0 || wx >= Main.maxTilesX || wy < 0 || wy >= Main.maxTilesY)
                        continue;

                    var backup = data.Tiles[x, y];
                    var current = Main.tile[wx, wy] ?? new Tile();
                    var tsBackup = TileSnapshot.TileStruct.From(backup);
                    var tsCurrent = TileSnapshot.TileStruct.From(current);

                    // 使用官方 TileStruct 快速比较（仅比较 3 个整数）
                    if (!tsBackup.Equals(tsCurrent))
                    {
                        current.CopyFrom(backup);
                        count++;
                        dirtyBlocks.Add((wx / BLOCK_SIZE, wy / BLOCK_SIZE));
                    }
                }
            }
        }

        // 发送所有脏区块的图格更新（分块发送，避免大包）
        foreach (var (bx, by) in dirtyBlocks)
        {
            int worldX = bx * BLOCK_SIZE;
            int worldY = by * BLOCK_SIZE;
            int bw = Math.Min(BLOCK_SIZE, Main.maxTilesX - worldX);
            int bh = Math.Min(BLOCK_SIZE, Main.maxTilesY - worldY);
            int cx = worldX + bw / 2;
            int cy = worldY + bh / 2;
            int size = (int)Math.Ceiling(Math.Max(bw, bh) / 2.0);
            NetMessage.SendTileSquare(-1, cx, cy, size);
        }

        return count;
    }
    #endregion

    #region 修复家具、实体、物品、标牌等
    private static void FixItem(TileData data)
    {
        // 恢复 TileEntity（物品框、武器架、人偶、盘子、逻辑感应器等）
        if (data.EntData != null)
            foreach (var ed in data.EntData)
            {
                // 1. 使用原版 Place 创建新实体（自动分配ID、检查图格、调用 OnPlaced）
                int id = TileEntity.Place(ed.X, ed.Y, ed.Type);
                if (id == -1 || ed.ExtraData == null) continue;

                // 2. 获取新创建的实体
                if (!TileEntity.ByID.TryGetValue(id, out var ent)) continue;

                // 3. 反序列化原始数据，覆盖当前状态
                using var ms = new MemoryStream(ed.ExtraData);
                using var br = new BinaryReader(ms);

                br.ReadByte();  // 跳过 实体type
                br.ReadInt32(); // 跳过 实体ID
                br.ReadInt16(); br.ReadInt16(); // 跳过位置

                // 4. 调用 ReadExtraData 恢复物品、逻辑类型等
                var GameVersion = Config.GameVersion == -1 ? GameVersionID.Latest : Config.GameVersion;
                ent.ReadExtraData(br, GameVersion, false);
            }

        // 恢复箱子
        if (data.Chests != null)
            foreach (var chest in data.Chests)
            {
                // 确保该位置没有箱子（KillAll 已做），创建新箱子
                int idx = Chest.CreateChest(chest.x, chest.y);
                if (idx == -1) continue;

                var target = Main.chest[idx];
                target.name = chest.name ?? ""; // 复制箱子名称
                target.maxItems = chest.maxItems; // 复制容量（通常是40）

                // 复制每个槽位的物品（深拷贝）
                for (int s = 0; s < chest.maxItems; s++)
                    target.item[s] = chest.item[s]?.Clone() ?? new Item();
            }

        // 恢复标牌
        if (data.Signs != null)
            foreach (var sign in data.Signs)
            {
                int sid = Sign.ReadSign(sign.x, sign.y, true);
                Main.sign[sid].text = sign.text;
            }

        // 强制刷新客户端地图
        for (int i = 0; i < TShock.Players.Length; i++)
            if (TShock.Players[i]?.Active == true)
                for (int j = 0; j < Main.maxSectionsX; j++)
                    for (int k = 0; k < Main.maxSectionsY; k++)
                        Netplay.Clients[i].TileSections[j, k] = false;
    }
    #endregion

    #region 保存Gzip版的快照方法（自动备份时调用 => WritePlayer.ExportAll）
    public static void SaveSnapshot(TSPlayer plr, bool showMag, string worldName, string exportDir)
    {
        // 1. 生成 TileSnapshot 保存到内存流
        string tmpPath = Path.GetTempFileName();
        TileSnapshot.Create();
        TileSnapshot.Save(tmpPath);
        TileSnapshot.Clear();

        // 2. GZIP 压缩并写入 .tws 文件
        string snapPath = Path.Combine(exportDir, $"{worldName}.tws");
        using (var fs = new FileStream(snapPath, FileMode.Create))
        using (var gz = new GZipStream(fs, CompressionLevel.Optimal))
        using (var tmpFs = File.OpenRead(tmpPath))
        {
            tmpFs.CopyTo(gz);
        }
        File.Delete(tmpPath);

        if (showMag)
            plr.SendMessage($"已保存世界快照: {worldName}.tws (GZIP压缩)", color2);

        // 3. 标牌序列化 + GZIP压缩
        string signPath = Path.Combine(exportDir, $"{worldName}.sgn");
        var signs = new List<Sign>();
        for (int i = 0; i < Main.sign.Length; i++)
        {
            var s = Main.sign[i];
            if (s != null && !string.IsNullOrEmpty(s.text))
                signs.Add(new Sign { x = s.x, y = s.y, text = s.text });
        }

        // 使用Newtonsoft.Json序列化
        string json = JsonConvert.SerializeObject(signs, Formatting.None);
        byte[] jsonBytes = Encoding.UTF8.GetBytes(json);

        // GZIP 压缩并写入 .sgn 文件
        using (var fs = new FileStream(signPath, FileMode.Create))
        using (var gz = new GZipStream(fs, CompressionLevel.Optimal))
        {
            gz.Write(jsonBytes, 0, jsonBytes.Length);
        }

        if (showMag)
            plr.SendMessage($"已保存标牌: {worldName}.sgn (GZIP压缩)", color2);
    }
    #endregion

    #region 从快照读取图格方法（自动解压GZIP，兼容旧版）
    public static TileData? ReadWorldTiles(string path, Rectangle rect, string? signPath = null)
    {
        if (!File.Exists(path))
        {
            TShock.Log.ConsoleError($"[{PluginName}] 快照文件不存在: " + path);
            return null;
        }

        try
        {
            // 检测是否为 GZIP 压缩文件（前两个字节 0x1F 0x8B）
            bool isGzip = false;
            using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read))
            {
                if (fs.Length >= 2)
                {
                    int b1 = fs.ReadByte();
                    int b2 = fs.ReadByte();
                    isGzip = (b1 == 0x1F && b2 == 0x8B);
                }
            }

            // 加载 TileSnapshot 快照（自动解压）
            if (isGzip)
            {
                using var fs = new FileStream(path, FileMode.Open);
                using var gz = new GZipStream(fs, CompressionMode.Decompress);
                using var ms = new MemoryStream();
                gz.CopyTo(ms);
                ms.Position = 0;
                using var br = new BinaryReader(ms);
                TileSnapshot.Load(br);  // 使用 BinaryReader 重载
            }
            else
            {
                TileSnapshot.Load(path); // 兼容旧版未压缩
            }

            // 如果没有加载到快照则返回
            if (!TileSnapshot.IsCreated)
            {
                TShock.Log.ConsoleError($"[{PluginName}] 地图快照加载失败");
                return null;
            }


            // 从快照获取世界边界(就是 Main.maxTilesX 和 Main.maxTilesY)
            int w = TileSnapshot._worldFile.WorldSizeX;
            int h = TileSnapshot._worldFile.WorldSizeY;

            // 检查边界(如果越界则清理快照)
            if (rect.X < 0 || rect.Y < 0 || rect.Right > w || rect.Bottom > h)
            {
                TileSnapshot.Clear();
                return null;
            }

            // 新建数据
            var res = new TileData
            {
                Tiles = new Tile[rect.Width, rect.Height],
                Chests = new List<Chest>(),
                EntData = new List<EntityData>(),
                Signs = new List<Sign>()
            };

            // 图格
            for (int x = 0; x < rect.Width; x++)
                for (int y = 0; y < rect.Height; y++)
                {
                    int wx = rect.X + x, wy = rect.Y + y;
                    var ts = TileSnapshot._tiles[wx * h + wy];
                    res.Tiles[x, y] = new Tile();
                    ts.Apply(res.Tiles[x, y]);
                }

            // 箱子
            foreach (var c in TileSnapshot._chests)
            {
                if (c == null) continue;
                // 箱子占据2x2，只要任意一格在矩形内就保留
                if (rect.Contains(c.x, c.y) || rect.Contains(c.x + 1, c.y) ||
                    rect.Contains(c.x, c.y + 1) || rect.Contains(c.x + 1, c.y + 1))
                    res.Chests.Add(c.CloneWithSeparateItems());  // 深拷贝
            }

            // 实体 (所有可互动放置家具、晶塔等)
            foreach (var e in TileSnapshot._tileEntities)
            {
                if (e == null || !rect.Contains(e.Position.X, e.Position.Y))
                    continue;

                using var ms = new MemoryStream();
                using var bw = new BinaryWriter(ms);
                TileEntity.Write(bw, e);  // 写入完整实体数据
                res.EntData.Add(new EntityData
                {
                    Type = e.type,
                    X = e.Position.X,
                    Y = e.Position.Y,
                    ExtraData = ms.ToArray()
                });
            }

            TileSnapshot.Clear(); // 清理快照内存

            // 标牌（自动解压GZIP，兼容旧版）
            if (signPath != null && File.Exists(signPath))
            {
                bool signGzip = false;
                using (var fs = new FileStream(signPath, FileMode.Open, FileAccess.Read))
                {
                    if (fs.Length >= 2)
                    {
                        int b1 = fs.ReadByte();
                        int b2 = fs.ReadByte();
                        signGzip = (b1 == 0x1F && b2 == 0x8B);
                    }
                }

                string json;
                if (signGzip)
                {
                    using var fs = File.OpenRead(signPath);
                    using var gz = new GZipStream(fs, CompressionMode.Decompress);
                    using var sr = new StreamReader(gz);
                    json = sr.ReadToEnd();
                }
                else
                {
                    json = File.ReadAllText(signPath);
                }

                var signs = JsonConvert.DeserializeObject<List<Sign>>(json);
                if (signs != null)
                {
                    foreach (var s in signs)
                        if (rect.Contains(s.x, s.y))
                            res.Signs.Add(new Sign { x = s.x, y = s.y, text = s.text });
                }
            }

            TShock.Log.ConsoleInfo($"[{PluginName}] 提取 {rect.Width}x{rect.Height} |\n" +
                                   $" 箱子:{res.Chests.Count} 实体:{res.EntData.Count} 标牌:{res.Signs.Count}");
            return res;
        }
        catch (Exception ex)
        {
            TShock.Log.ConsoleError($"[{PluginName}] 快照读取失败: {ex.Message}\n{ex.StackTrace}");
            TileSnapshot.Clear(); // 清理快照
            return null;
        }
    }
    #endregion

    #region 销毁所有互动家具实体
    public static void KillAll(int startX, int endX, int startY, int endY)
    {
        Rectangle rect = new Rectangle(startX, startY, endX - startX + 1, endY - startY + 1);

        // 删除所有 TileEntity（物品框、武器架、人偶、盘子、逻辑感应器、晶塔、训练假人、衣帽架等）
        var toRemove = TileEntity.ByPosition.Values
            .Where(te => rect.Contains(te.Position.X, te.Position.Y))
            .ToList();

        foreach (var te in toRemove)
            TileEntity.Remove(te);

        // 删除箱子、标牌 墓碑 广播盒
        for (int x = startX; x <= endX; x++)
            for (int y = startY; y <= endY; y++)
            {
                var tile = Main.tile[x, y];
                if (tile == null || !tile.active()) continue;

                // 箱子、野外宝箱、梳妆台
                if (TileID.Sets.BasicChest[tile.type] || TileID.Sets.BasicChestFake[tile.type] || TileID.Sets.BasicDresser[tile.type])
                    Chest.DestroyChest(x, y);

                // 标牌 墓碑 广播盒
                if (tile.type == TileID.Signs || tile.type == TileID.Tombstones || tile.type == TileID.AnnouncementBox)
                    Sign.KillSign(x, y);
            }
    }
    #endregion

    #region 列出自动备份列表
    private static void ListBak(TSPlayer plr)
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

        if (!plr.RealPlayer)
        {
            plr.SendMessage(sb.ToString(), color);
            plr.SendMessage($"请进入游戏后,再用: /{pt} rw 索引", color);
            return;
        }

        plr.SendMessage(TextGradient(sb.ToString()), color);
        plr.SendMessage($"选择: /{pt} rw 索引", color);
        plr.SendMessage($"撤销: /{pt} rw bk", color);
        return;
    }
    #endregion
}