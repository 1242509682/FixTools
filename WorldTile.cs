using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Utilities;
using TShockAPI;
using static FixTools.FixTools;
using static FixTools.Utils;

namespace FixTools;

public static class WorldTile
{
    #region 修复局部图格方法（从自动备份选择地图快照）
    public static void FixSnapshot(GetDataHandlers.MassWireOperationEventArgs e, TSPlayer plr)
    {
        // 如果没有输入/pt rw 指令选择备份，或者没有使用红电线模式则返回
        if (plr.GetData<bool?>("rwWire") != true || e.ToolMode != 1)
            return;
        
        // 触发精密线控仪绘画
        e.Handled = true;

        // 从/pt rw 指令获取临时快照文件的路径
        string wldPath = plr.GetData<string>("rwFile");
        if (string.IsNullOrEmpty(wldPath) || !File.Exists(wldPath))
        {
            plr.SendErrorMessage($"[{PluginName}] 快照文件已丢失，请重选备份");
            plr.RemoveData("rwWire");
            plr.RemoveData("rwFile");
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

        Task.Run(() =>
        {
            // 调用读取方法
            var tiles = ReadWorldTiles(wldPath, rect);
            if (tiles == null) return;

            // 覆盖当前世界的对应图格
            for (int x = 0; x < rect.Width; x++)
            {
                for (int y = 0; y < rect.Height; y++)
                {
                    int wx = rect.X + x, wy = rect.Y + y;
                    if (wx < 0 || wx >= Main.maxTilesX ||
                        wy < 0 || wy >= Main.maxTilesY)
                        continue;

                    Main.tile[wx, wy].CopyFrom(tiles[x, y]);
                }
            }

            // 批量发送更新
            NetMessage.SendTileSquare(-1, rect.X + rect.Width / 2, rect.Y + rect.Height / 2,
                                     (byte)Math.Max(rect.Width, rect.Height));

        }).ContinueWith(_ =>
        {
            // 强制刷新客户端地图
            for (int i = 0; i < TShock.Players.Length; i++)
                if (TShock.Players[i]?.Active == true)
                    for (int j = 0; j < Main.maxSectionsX; j++)
                        for (int k = 0; k < Main.maxSectionsY; k++)
                            Netplay.Clients[i].TileSections[j, k] = false;

            // 清理临时文件 & 状态
            File.Delete(wldPath);
            plr.RemoveData("rwWire");
            plr.RemoveData("rwFile");
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
    }
    #endregion

    #region 从快照读取图格方法
    public static Tile[,]? ReadWorldTiles(string path, Rectangle rect)
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

            if (rect.X < 0 || rect.Y < 0 || rect.Right > w || rect.Bottom > h)
            {
                TileSnapshot.Clear();
                return null;
            }

            // 提取矩形区域图格
            int tw = rect.Width;
            int th = rect.Height;
            Tile[,] res = new Tile[tw, th];

            for (int x = 0; x < tw; x++)
            {
                for (int y = 0; y < th; y++)
                {
                    int wx = rect.X + x;
                    int wy = rect.Y + y;
                    var ts = TileSnapshot._tiles[wx * h + wy];
                    res[x, y] = new Tile();
                    ts.Apply(res[x, y]);
                }
            }

            TileSnapshot.Clear();
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
}