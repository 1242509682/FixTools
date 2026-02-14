using Microsoft.Xna.Framework;
using Terraria;
using TShockAPI;
using TShockAPI.DB;
using static FixTools.Utils;
using static FixTools.FixTools;

namespace FixTools;

internal class CanCraft
{
    #region 阻止合成区域内附近箱子方法（判断范围600像素 ≈ 37.5格）
    public static bool OnCanCraftFromChest(On.Terraria.GameContent.CraftingRequests.orig_CanCraftFromChest orig, Chest chest, int whoAmI)
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
    public static List<Region> GetChest(Chest chest)
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
    public static bool IsChestInRange(Chest chest, TSPlayer plr)
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
