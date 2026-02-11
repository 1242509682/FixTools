using Terraria;
using Terraria.ID;
using TShockAPI;
using static FixTools.Utils;

namespace FixTools;

internal class FixPlaceObject
{
    #region 修复天塔柱BUG
    public static void FixPlace(GetDataHandlers.PlaceObjectEventArgs e)
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
                ClearBugTiles(e.Player, e.X, e.Y, e.X, checkY);
            }
        }
    }
    #endregion

    #region 清理BUG图格（天塔柱+下面有2个锭）
    private static void ClearBugTiles(TSPlayer plr, int topX, int topY, int botX, int botY)
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
    #endregion

    #region 会产生类似BUG的图格ID表
    public static bool TargetItem(short type)
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
