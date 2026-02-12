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
        if (args.Parameters.Count < 2)
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

            if (plr.RealPlayer)
                plr.SendMessage(TextGradient(sb.ToString()), color);
            else
                plr.SendMessage(sb.ToString(), color);

            plr.SendMessage($"用法: /{pt} rw 索引", color);
            return;
        }

        if (!int.TryParse(args.Parameters[1], out int idx) || idx < 1)
        {
            plr.SendErrorMessage("索引必须是正整数");
            return;
        }

        var files = GetBakFiles();
        if (idx > files.Length)
        {
            plr.SendErrorMessage($"索引超出范围，共有 {files.Length} 个备份");
            return;
        }

        string zipPath = files[idx - 1];

        using (var zip = ZipFile.OpenRead(zipPath))
        {
            var snapEntry = zip.Entries.FirstOrDefault(e =>
                e.Name.EndsWith(".tws", StringComparison.OrdinalIgnoreCase));

            if (snapEntry == null)
            {
                plr.SendErrorMessage($"备份文件 {Path.GetFileName(zipPath)} 中未找到世界快照 (.tws)，请重新备份");
                return;
            }

            // 保存快照
            string snapPath = Path.Combine(ReaderPlayer.ReaderDir, $"snap_{DateTime.Now:HHmmss}.tws");
            snapEntry.Open().CopyTo(File.Create(snapPath));
            plr.SetData("rwSnap", snapPath); // 快照路径

            // 尝试保存标牌文件（可能不存在）
            var signEntry = zip.Entries.FirstOrDefault(e => e.Name.EndsWith(".sgn", StringComparison.OrdinalIgnoreCase));
            if (signEntry != null)
            {
                string signPath = Path.Combine(ReaderPlayer.ReaderDir, $"sign_{DateTime.Now:HHmmss}.sgn");
                signEntry.Open().CopyTo(File.Create(signPath));
                plr.SetData("rwSign", signPath);
            }

            plr.SendSuccessMessage($"已加载世界快照: {snapEntry.Name}");
        }

        plr.SetData("rwWire", true);
        plr.SendInfoMessage("请使用 [i:3611] 红电线 拉取需要恢复的区域");
    }
    #endregion

    #region 修复局部图格方法（从自动备份选择地图快照）
    public static void FixSnapshot(GetDataHandlers.MassWireOperationEventArgs e, TSPlayer plr)
    {
        // 如果没有输入/pt rw 指令选择备份，或者没有使用红电线模式则返回
        if (plr.GetData<bool?>("rwWire") != true || e.ToolMode != 1)
            return;

        // 触发精密线控仪绘画
        e.Handled = true;

        // 从/pt rw 指令获取临时快照文件的路径
        string snapPath = plr.GetData<string>("rwSnap");
        string signPath = plr.GetData<string>("rwSign");
        if (string.IsNullOrEmpty(snapPath) || !File.Exists(snapPath))
        {
            plr.SendErrorMessage($"[{PluginName}] 快照文件已丢失，请重新选择备份");
            plr.RemoveData("rwWire");
            plr.RemoveData("rwSnap");
            plr.RemoveData("rwSign");
            return;
        }

        // 计算选区
        int x1 = Math.Min(e.StartX, e.EndX);
        int y1 = Math.Min(e.StartY, e.EndY);
        int x2 = Math.Max(e.StartX, e.EndX);
        int y2 = Math.Max(e.StartY, e.EndY);
        x2++; y2++;
        Rectangle rect = new Rectangle(x1, y1, x2 - x1, y2 - y1);
        plr.SendMessage(TextGradient($"正在从备份恢复区域: ({x1},{y1}) → ({x2},{y2})"), color);

        // 调用读取方法
        var data = ReadWorldTiles(snapPath, rect, signPath);
        if (data is null) return;

        Task.Run(() =>
        {
            // 1. 销毁区域内原有实体
            KillAll(rect.Left, rect.Right, rect.Top, rect.Bottom);

            // 2. 恢复图格
            if (data.Tiles != null)
                for (int x = 0; x < rect.Width; x++)
                    for (int y = 0; y < rect.Height; y++)
                    {
                        int wx = rect.X + x, wy = rect.Y + y;
                        if (wx < 0 || wx >= Main.maxTilesX ||
                            wy < 0 || wy >= Main.maxTilesY)
                            continue;

                        Main.tile[wx, wy].CopyFrom(data.Tiles[x, y]);
                    }

            // 批量发送图格更新
            NetMessage.SendTileSquare(-1, rect.X + rect.Width / 2, rect.Y + rect.Height / 2,
                                     (byte)Math.Max(rect.Width, rect.Height));

        }).ContinueWith(_ =>
        {
            // 3. 恢复 TileEntity（物品框、武器架、人偶、盘子、逻辑感应器等）
            if (data.EntData != null)
                foreach (var ed in data.EntData)
                {
                    // 1. 使用原版 Place 创建新实体（自动分配ID、检查图格、调用 OnPlaced）
                    int id = TileEntity.Place(ed.X, ed.Y, ed.Type);
                    if (id == -1) continue;

                    // 2. 获取新创建的实体
                    if (!TileEntity.ByID.TryGetValue(id, out var ent)) continue;

                    // 3. 反序列化原始数据，覆盖当前状态
                    if (ed.ExtraData is null) continue;
                    using var ms = new MemoryStream(ed.ExtraData);
                    using var br = new BinaryReader(ms);

                    // 跳过实体类型（已读取）
                    br.ReadByte();  // 跳过 type（已读）
                    br.ReadInt32(); // 读取 ID、位置（但 Place 已设置，忽略）
                    br.ReadInt16(); br.ReadInt16(); // 位置

                    // 4. 调用 ReadExtraData 恢复物品、逻辑类型等
                    ent.ReadExtraData(br, Config.GameVersion == -1 ? GameVersionID.Latest : Config.GameVersion, false);
                }

            // 4. 恢复箱子
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
                    {
                        if (chest.item[s] != null && !chest.item[s].IsAir)
                            target.item[s] = chest.item[s].Clone();
                        else
                            target.item[s] = new Item(); // 置空
                    }
                }

            // 5.恢复标牌
            if (data.Signs != null)
                foreach (var sign in data.Signs)
                {
                    int sid = Sign.ReadSign(sign.x, sign.y, true);
                    Main.sign[sid].text = sign.text;
                }

            // 6. 强制刷新客户端地图
            for (int i = 0; i < TShock.Players.Length; i++)
                if (TShock.Players[i]?.Active == true)
                    for (int j = 0; j < Main.maxSectionsX; j++)
                        for (int k = 0; k < Main.maxSectionsY; k++)
                            Netplay.Clients[i].TileSections[j, k] = false;

            // 清理临时文件 & 状态
            File.Delete(snapPath);
            if (signPath != null)
                File.Delete(signPath);
            plr.RemoveData("rwSnap");
            plr.RemoveData("rwSign");
            plr.RemoveData("rwWire");
        });
    }
    #endregion

    #region 保存快照方法（自动备份时调用：WritePlayer类-ExportAll方法中使用）
    public static void SaveSnapshot(TSPlayer plr, bool showMag, string worldName, string exportDir)
    {
        string snapPath = Path.Combine(exportDir, $"{worldName}.tws");
        TileSnapshot.Create();  // 创建当前世界快照
        TileSnapshot.Save(snapPath); // 保存为 .tws 文件
        TileSnapshot.Clear(); // 清理内存

        if (showMag)
            plr.SendMessage($"已保存世界快照: {worldName}.tws", color2);

        // 使用 Newtonsoft.Json 序列化标牌
        string signPath = Path.Combine(exportDir, $"{worldName}.sgn");
        var signs = new List<Sign>();
        for (int i = 0; i < Main.sign.Length; i++)
        {
            var s = Main.sign[i];
            if (s != null && !string.IsNullOrEmpty(s.text))
                signs.Add(new Sign { x = s.x, y = s.y, text = s.text });
        }

        string json = JsonConvert.SerializeObject(signs, Formatting.Indented);
        File.WriteAllText(signPath, json);

        if (showMag)
            plr.SendMessage($"已保存标牌: {worldName}.sgn", color2);
    }
    #endregion

    #region 从快照读取图格方法
    public static TileData? ReadWorldTiles(string path, Rectangle rect, string? signPath = null)
    {
        if (!File.Exists(path))
        {
            TShock.Log.ConsoleError($"[{PluginName}] 快照文件不存在: " + path);
            return null;
        }

        try
        {
            // 加载 TileSnapshot 快照文件
            TileSnapshot.Load(path);
            if (!TileSnapshot.IsCreated)
            {
                TShock.Log.ConsoleError($"[{PluginName}] 地图快照加载失败");
                return null;
            }

            int w = TileSnapshot._worldFile.WorldSizeX;
            int h = TileSnapshot._worldFile.WorldSizeY;

            // 检查边界
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
                {
                    var copy = c.CloneWithSeparateItems(); // 深拷贝
                    res.Chests.Add(copy);
                }
            }

            // 实体
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

            // 标牌（从额外文件加载）
            if (signPath != null && File.Exists(signPath))
            {
                string json = File.ReadAllText(signPath);
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

                // 箱子
                if (TileID.Sets.BasicChest[tile.type])
                    Chest.DestroyChest(x, y);

                // 标牌 墓碑 广播盒
                if (tile.type == TileID.Signs || tile.type == TileID.Tombstones || tile.type == TileID.AnnouncementBox)
                    Sign.KillSign(x, y);
            }
    }
    #endregion
}