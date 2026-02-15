using Terraria;
using TShockAPI;
using static FixTools.FixTools;
using static FixTools.Utils;

namespace DeathEvent;

internal static class Rewards
{
    // 槽位信息结构（私有类，仅在此模块内部使用）
    private class ItemSlotInfo
    {
        public int PlayerIndex;   // 玩家索引
        public int SlotIndex;     // 物品槽位索引
        public int ItemId;        // 物品ID
        public int ItemStack;     // 物品堆叠数量
    }

    #region 执行激励功能 - 当队伍中有玩家死亡时，从死亡队伍每个玩家随机抽取一件物品奖励给其他队伍
    public static void TeamItemPun(TSPlayer plr, HashSet<int> other, List<TSPlayer> teamPly)
    {
        // 检查是否启用队伍模式、死亡物品惩罚，以及是否有其他队伍玩家
        if (!Config.TeamMode || !Config.TeamItemPun || other.Count == 0) return;

        int team = plr.Team;  // 获取当前队伍ID

        // 检查同队伍玩家列表是否为空
        if (teamPly.Count == 0) return;

        // 筛选有效其他队伍玩家（过滤掉无效、离线或不活动的玩家）
        var validOth = other.Where(idx =>
        {
            var p = TShock.Players[idx];
            return p != null && p.RealPlayer && p.Active;
        }).ToList();

        // 如果没有有效的其他队伍玩家，直接返回
        if (validOth.Count == 0) return;

        // Terraria的随机数生成器
        var rand = Main.rand;

        // 遍历死亡同队伍所有玩家
        foreach (var p in teamPly)
        {
            // 跳过管理（不会被抽取物品）
            if (p.HasPermission($"{pt}.use")) continue;

            // 收集玩家所有物品槽位
            var plySlots = CollectSlots(p);
            if (plySlots.Count == 0) continue;  // 玩家没有物品可抽取

            // 随机抽取一个物品槽位
            int rndSlot = rand.Next(plySlots.Count);
            var selSlot = plySlots[rndSlot];

            // 从玩家槽位移除物品，获取物品数量
            int stack = RemoveSlot(p, selSlot.SlotIndex);
            if (stack <= 0) continue;  // 移除失败或物品数量为0

            // 随机选择一个目标玩家（从其他队伍中）
            int rndPlr = validOth[rand.Next(validOth.Count)];
            var toPlr = TShock.Players[rndPlr];

            // 验证目标玩家是否有效
            if (toPlr == null || !toPlr.RealPlayer || !toPlr.Active) continue;

            // 给予物品给目标玩家
            GiveItems(toPlr, selSlot.ItemId, stack);

            // 发送消息通知源玩家和目标玩家
            SendRewMsg(p, toPlr, selSlot.ItemId, stack);
        }
    }
    #endregion

    #region 发送激励消息给物品来源玩家和目标玩家
    private static void SendRewMsg(TSPlayer from, TSPlayer to, int id, int stack)
    {
        // 获取物品图标和数量的格式化字符串
        string itemIcon = ItemIcon(id, stack);

        // 获取双方队伍名称
        var fromTeam = GetTeamCName(from.Team);
        var toTeam = GetTeamCName(to.Team);

        // 给源玩家：提示物品被奖励给谁
        from.SendMessage($"您的{itemIcon}已奖励给{toTeam}的[c/42D394:{to.Name}]", color);
        // 给目标玩家：提示从谁那里获得奖励
        to.SendMessage($"恭喜您从{fromTeam}的[c/42D394:{from.Name}]获得奖励{itemIcon}", color);

        // 只发送给除发送方和接收方以外的其他玩家
        var other = TShock.Players.FirstOrDefault(p => p != null && p != from && p != to);
        other?.SendMessage($"{toTeam}的[c/508DC8:{to.Name}]从{fromTeam}的[c/508DC8:{from.Name}]获得了:{itemIcon}", color);
    }
    #endregion

    #region 给予物品给玩家，处理堆叠限制
    private static void GiveItems(TSPlayer plr, int id, int stack)
    {
        if (stack <= 0) return;  // 无效数量检查

        // 创建物品实例以获取最大堆叠数
        var item = new Item();
        item.SetDefaults(id);
        int maxStack = item.maxStack;

        // 计算完整堆叠数和剩余数量
        int fullStacks = stack / maxStack;
        int rem = stack % maxStack;

        // 给予完整堆叠的物品
        for (int i = 0; i < fullStacks; i++)
            plr.GiveItem(id, maxStack);

        // 给予剩余数量的物品
        if (rem > 0)
            plr.GiveItem(id, rem);
    }
    #endregion

    #region 收集玩家所有物品槽位信息
    private static List<ItemSlotInfo> CollectSlots(TSPlayer plr)
    {
        var slotList = new List<ItemSlotInfo>();
        var t = plr.TPlayer;  // 获取Terraria玩家对象
        int idx = plr.Index;

        // 定义所有物品存储区域及其元数据
        var slots = new[]
        {
            // 背包
            (t.inventory, 0, NetItem.InventorySlots),
            // 装备+饰品栏
            (t.armor, NetItem.ArmorIndex.Item1, NetItem.ArmorSlots),
            // 染料栏
            (t.dye, NetItem.DyeIndex.Item1, NetItem.DyeSlots),
            // 工具栏
            (t.miscEquips, NetItem.MiscEquipIndex.Item1, NetItem.MiscEquipSlots),
            // 饰品染料栏
            (t.miscDyes, NetItem.MiscDyeIndex.Item1, NetItem.MiscDyeSlots),
            // 猪猪储钱罐
            (t.bank.item, NetItem.PiggyIndex.Item1, NetItem.PiggySlots),
            // 保险箱
            (t.bank2.item, NetItem.SafeIndex.Item1, NetItem.SafeSlots),
            // 护卫熔炉
            (t.bank3.item, NetItem.ForgeIndex.Item1, NetItem.ForgeSlots),
            // 虚空保险库
            (t.bank4.item, NetItem.VoidIndex.Item1, NetItem.VoidSlots)
        };

        // 遍历所有槽位区域
        foreach (var (items, start, cnt) in slots)
        {
            for (int i = 0; i < cnt; i++)
            {
                var item = items[i];
                // 只收集有物品的槽位（type > 0 表示有物品）
                if (item != null && item.type > 0 && item.stack > 0)
                {
                    AddSlot(slotList, plr, start + i, item.type, item.stack);
                }
            }
        }

        // 收集垃圾桶物品
        if (t.trashItem != null && t.trashItem.type > 0 && t.trashItem.stack > 0)
        {
            AddSlot(slotList, plr, NetItem.TrashIndex.Item1, t.trashItem.type, t.trashItem.stack);
        }

        // 收集三个套装栏的物品
        for (int lod = 0; lod < 3; lod++)
        {
            // 计算套装装备栏的起始索引
            var armorStart = lod == 0 ? NetItem.Loadout1Armor.Item1 :
                            lod == 1 ? NetItem.Loadout2Armor.Item1 :
                            NetItem.Loadout3Armor.Item1;

            // 计算套装染料栏的起始索引
            var dyeStart = lod == 0 ? NetItem.Loadout1Dye.Item1 :
                          lod == 1 ? NetItem.Loadout2Dye.Item1 :
                          NetItem.Loadout3Dye.Item1;

            // 收集套装装备栏
            for (int i = 0; i < NetItem.LoadoutArmorSlots; i++)
            {
                var item = t.Loadouts[lod].Armor[i];
                if (item != null && item.type > 0 && item.stack > 0)
                {
                    AddSlot(slotList, plr, armorStart + i, item.type, item.stack);
                }
            }

            // 收集套装染料栏
            for (int i = 0; i < NetItem.LoadoutDyeSlots; i++)
            {
                var item = t.Loadouts[lod].Dye[i];
                if (item != null && item.type > 0 && item.stack > 0)
                {
                    AddSlot(slotList, plr, dyeStart + i, item.type, item.stack);
                }
            }
        }

        return slotList;
    }
    #endregion

    #region 添加槽位信息到列表的辅助方法
    private static void AddSlot(List<ItemSlotInfo> list, TSPlayer plr, int slotIdx, int itemId, int stack)
    {
        list.Add(new ItemSlotInfo
        {
            PlayerIndex = plr.Index,
            SlotIndex = slotIdx,
            ItemId = itemId,
            ItemStack = stack
        });
    }
    #endregion

    #region 从指定槽位移除物品
    private static int RemoveSlot(TSPlayer plr, int slotIdx)
    {
        var t = plr.TPlayer;
        Item? item = null;
        int stack = 0;

        // 根据槽位索引确定物品所在的位置
        if (slotIdx >= 0 && slotIdx < NetItem.InventorySlots)
        {
            // 背包槽位
            item = t.inventory[slotIdx];
        }
        else if (slotIdx >= NetItem.ArmorIndex.Item1 && slotIdx < NetItem.ArmorIndex.Item1 + NetItem.ArmorSlots)
        {
            // 装备栏槽位
            item = t.armor[slotIdx - NetItem.ArmorIndex.Item1];
        }
        else if (slotIdx >= NetItem.DyeIndex.Item1 && slotIdx < NetItem.DyeIndex.Item1 + NetItem.DyeSlots)
        {
            // 染料栏槽位
            item = t.dye[slotIdx - NetItem.DyeIndex.Item1];
        }
        else if (slotIdx >= NetItem.MiscEquipIndex.Item1 && slotIdx < NetItem.MiscEquipIndex.Item1 + NetItem.MiscEquipSlots)
        {
            // 饰品栏槽位
            item = t.miscEquips[slotIdx - NetItem.MiscEquipIndex.Item1];
        }
        else if (slotIdx >= NetItem.MiscDyeIndex.Item1 && slotIdx < NetItem.MiscDyeIndex.Item1 + NetItem.MiscDyeSlots)
        {
            // 饰品染料栏槽位
            item = t.miscDyes[slotIdx - NetItem.MiscDyeIndex.Item1];
        }
        else if (slotIdx >= NetItem.PiggyIndex.Item1 && slotIdx < NetItem.PiggyIndex.Item1 + NetItem.PiggySlots)
        {
            // 猪猪储钱罐槽位
            item = t.bank.item[slotIdx - NetItem.PiggyIndex.Item1];
        }
        else if (slotIdx >= NetItem.SafeIndex.Item1 && slotIdx < NetItem.SafeIndex.Item1 + NetItem.SafeSlots)
        {
            // 保险箱槽位
            item = t.bank2.item[slotIdx - NetItem.SafeIndex.Item1];
        }
        else if (slotIdx >= NetItem.ForgeIndex.Item1 && slotIdx < NetItem.ForgeIndex.Item1 + NetItem.ForgeSlots)
        {
            // 护卫熔炉槽位
            item = t.bank3.item[slotIdx - NetItem.ForgeIndex.Item1];
        }
        else if (slotIdx >= NetItem.VoidIndex.Item1 && slotIdx < NetItem.VoidIndex.Item1 + NetItem.VoidSlots)
        {
            // 虚空袋槽位
            item = t.bank4.item[slotIdx - NetItem.VoidIndex.Item1];
        }
        else if (slotIdx == NetItem.TrashIndex.Item1)
        {
            // 垃圾桶槽位
            item = t.trashItem;
        }
        // 以下处理三个套装栏的槽位
        else if (slotIdx >= NetItem.Loadout1Armor.Item1 && slotIdx < NetItem.Loadout1Armor.Item1 + NetItem.LoadoutArmorSlots)
        {
            // 套装1装备栏
            item = t.Loadouts[0].Armor[slotIdx - NetItem.Loadout1Armor.Item1];
        }
        else if (slotIdx >= NetItem.Loadout1Dye.Item1 && slotIdx < NetItem.Loadout1Dye.Item1 + NetItem.LoadoutDyeSlots)
        {
            // 套装1染料栏
            item = t.Loadouts[0].Dye[slotIdx - NetItem.Loadout1Dye.Item1];
        }
        else if (slotIdx >= NetItem.Loadout2Armor.Item1 && slotIdx < NetItem.Loadout2Armor.Item1 + NetItem.LoadoutArmorSlots)
        {
            // 套装2装备栏
            item = t.Loadouts[1].Armor[slotIdx - NetItem.Loadout2Armor.Item1];
        }
        else if (slotIdx >= NetItem.Loadout2Dye.Item1 && slotIdx < NetItem.Loadout2Dye.Item1 + NetItem.LoadoutDyeSlots)
        {
            // 套装2染料栏
            item = t.Loadouts[1].Dye[slotIdx - NetItem.Loadout2Dye.Item1];
        }
        else if (slotIdx >= NetItem.Loadout3Armor.Item1 && slotIdx < NetItem.Loadout3Armor.Item1 + NetItem.LoadoutArmorSlots)
        {
            // 套装3装备栏
            item = t.Loadouts[2].Armor[slotIdx - NetItem.Loadout3Armor.Item1];
        }
        else if (slotIdx >= NetItem.Loadout3Dye.Item1 && slotIdx < NetItem.Loadout3Dye.Item1 + NetItem.LoadoutDyeSlots)
        {
            // 套装3染料栏
            item = t.Loadouts[2].Dye[slotIdx - NetItem.Loadout3Dye.Item1];
        }

        // 如果找到有效物品，将其清空并返回堆叠数
        if (item != null && item.type > 0)
        {
            stack = item.stack; // 保存物品数量
            item.TurnToAir(); // 将槽位物品清空
            plr.SendData(PacketTypes.PlayerSlot, "", plr.Index, slotIdx); // 发送物品槽位更新包
        }

        return stack;
    }
    #endregion
}