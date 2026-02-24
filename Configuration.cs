using Newtonsoft.Json;
using Terraria;
using Terraria.ID;
using static FixTools.FixTools;

namespace FixTools;

internal class Configuration
{
    [JsonProperty("管理进服无敌", Order = -202)]
    public bool AutoGod { get; set; } = true;
    [JsonProperty("自建GM权限组", Order = -201)]
    public bool AutoAddGM { get; set; } = true;
    [JsonProperty("启用自动注册", Order = -200)]
    public bool AutoRegister { get; set; } = true;
    [JsonProperty("随机默认密码", Order = -199)]
    public bool RandPass { get; set; } = true;
    [JsonProperty("注册默认密码", Order = -198)]
    public string DefPass { get; set; } = "123456";
    [JsonProperty("修复复活检查", Order = -197)]
    public bool FixSapwn { get; set; } = false;
    [JsonProperty("死亡出服禁止进服", Order = -196)]
    public bool DeathLimitForJoin { get; set; } = false;
    [JsonProperty("回到死亡地点", Order = -195)]
    public bool Back { get; set; } = false;

    [JsonProperty("自动备份存档", Order = -170)]
    public bool AutoSavePlayer { get; set; } = true;
    [JsonProperty("备份地图快照", Order = -169)]
    public bool SaveSnapshot { get; set; } = true;
    [JsonProperty("自动备份地图", Order = -168)]
    public bool AutoSaveWorld { get; set; } = false;
    [JsonProperty("自动备份数据库", Order = -167)]
    public bool AutoSaveSqlite { get; set; } = true;
    [JsonProperty("备份存档分钟数", Order = -166)]
    public int AutoSaveInterval { get; set; } = 30;
    [JsonProperty("自动清理备份", Order = -165)]
    public bool AutoClean { get; set; } = true;
    [JsonProperty("保留备份数量", Order = -164)]
    public int MaxBackup { get; set; } = 30;
    [JsonProperty("备份显示消息", Order = -163)]
    public bool ShowAutoSaveMsg { get; set; } = true;

    [JsonProperty("投票回档开关", Order = -100)]
    public bool ApplyVote { get; set; } = true;
    [JsonProperty("投票过期时间", Order = -99)]
    public int ApplyTime { get; set; } = 30;
    [JsonProperty("投票通过概率", Order = -98)]
    public float VotePassRate { get; set; } = 0.5f;
    [JsonProperty("投票参与人数", Order = -97)]
    public int MinVotePlayers { get; set; } = 2;

    [JsonProperty("导出存档的版本号", Order = -96)]
    public int GameVersion { get; set; } = 317;
    [JsonProperty("版本号对照参考表", Order = -95)]
    public HashSet<string> Example { get; set; } = [];

    [JsonProperty("跨版本进服", Order = -94)]
    public bool NoVisualLimit { get; set; } = true;
    [JsonProperty("宝藏袋传送", Order = -93)]
    public bool TpBagEnabled { get; set; } = true;
    [JsonProperty("显示伤害排行", Order = -92)]
    public bool NPCDamageTracker { get; set; } = true;
    [JsonProperty("宝藏袋传送关键词", Order = -91)]
    public List<string> AllowTpBagText { get; set; } = new();

    [JsonProperty("自动修复地图缺失", Order = -90)]
    public bool AutoFixWorld { get; set; } = true;
    [JsonProperty("修复物品召唤入侵事件", Order = -89)]
    public bool FixStartInvasion { get; set; } = true;
    [JsonProperty("修复天塔柱刷物品BUG", Order = -88)]
    public bool FixPlaceObject { get; set; } = true;
    [JsonProperty("石后神庙钥匙召唤火星暴乱", Order = -87)]
    public bool MartianEvent { get; set; } = true;
    [JsonProperty("禁用区域箱子材料", Order = -86)]
    public bool NoUseRgionCheat { get; set; } = true;
    [JsonProperty("禁用区域箱子范围", Order = -85)]
    public float NoUseCheatRange { get; set; } = 40;
    [JsonProperty("允许区域合成组", Order = -84)]
    public List<string> AllowRegionGroup { get; set; } = new();

    [JsonProperty("队伍模式开关", Order = -50)]
    public bool TeamMode { get; set; } = true;
    [JsonProperty("队伍出生点", Order = -49)]
    public bool TeamSpawn { get; set; } = true;
    [JsonProperty("队伍投票时间", Order = -48)]
    public int TeamVoteTime { get; set; } = 30;
    [JsonProperty("队伍切换冷却", Order = -47)]
    public int SwitchTeamCD { get; set; } = 30;
    [JsonProperty("队员死亡物品惩罚", Order = -46)]
    public bool TeamItemPun { get; set; } = false;
    [JsonProperty("队员死亡集体团灭", Order = -45)]
    public bool TeamDeathPun { get; set; } = false;

    [JsonProperty("重置清理数据表", Order = 1)]
    public HashSet<string> ClearSql { get; set; } = [];
    [JsonProperty("重置时删除文件", Order = 2)]
    public HashSet<string> DeleteFile { get; set; } = [];
    [JsonProperty("重置后执行指令", Order = 3)]
    public HashSet<string> AfterCMD = [];
    [JsonProperty("重置前执行指令", Order = 4)]
    public HashSet<string> BeforeCMD = [];

    [JsonProperty("开服后执行指令", Order = 5)]
    public HashSet<string> PostCMD = [];
    [JsonProperty("游戏时执行指令", Order = 6)]
    public HashSet<string> GameCMD = [];

    [JsonProperty("开服自动配权", Order = 20)]
    public bool AutoPerm { get; set; } = true;
    [JsonProperty("启用进服公告", Order = 21)]
    public bool MotdEnabled { get; set; } = true;
    [JsonProperty("进服公告1", Order = 22)]
    public string[] MotdMess { get; set; } = [];
    [JsonProperty("进服公告2", Order = 23)]
    public string[] MotdMess2 { get; set; } = [];
    [JsonProperty("进服公告3", Order = 24)]
    public string[] MotdMess3 { get; set; } = [];

    [JsonProperty("复制文件输出路径", Order = 40)]
    public List<string> CopyPaths = [];

    [JsonProperty("人数进度锁", Order = 50)]
    public bool ProgressLock { get; set; } = false;
    [JsonProperty("解锁人数", Order = 51)]
    public int UnLockCount { get; set; } = 3;
    [JsonProperty("已解锁怪物", Order = 52)]
    public HashSet<string> UnLockNpc = new HashSet<string>();
    [JsonProperty("进度锁怪物", Order = 53)]
    public HashSet<string> LockNpc = new HashSet<string>();

    [JsonProperty("批量改权限", Order = 71)]
    public Dictionary<string, HashSet<string>> Permission = [];

    #region 预设参数方法
    public void SetDefault()
    {
        Example =
        [
            "最新版  : -1",
            "1.4.5.5 : 318",
            "1.4.5.4 : 317",
            "1.4.5.3 : 316",
            "1.4.5.0 : 315",
            "1.4.4   : 279"
        ];

        LockNpc = SetNpcByID();

        MotdMess =
        [
            "\n欢迎 拿着{武器类型}{物品图标}的{玩家名} 来到 {服务器名}",
            "在线玩家 [c/FFFFFF:({在线人数}/{服务器上限})]: {在线玩家}",
            $"指令:/{pt} 权限:{Prem}",
            "配置路径: tshock/[c/FF6962:{插件名}]/配置文件.json",
            "TShock官方Q群:816771079",
            "所在队伍:{队伍} {同队人数}/{别队人数}",
            "同队玩家:{同队玩家}",
            "当前进度:{进度}",
            "---------",
            "发送[c/FF6962:任意消息]了解本插件相关功能\n",
        ];

        MotdMess2 =
        [
            "---------",
            $"《插件支持功能》适配版本:{TShockVS}",
            "[c/FFFFFF:1.]导入导出SSC存档、自动备份存档、禁用区域箱子材料",
            "[c/FFFFFF:2.]智能进服公告、跨版本进服、修复地图区块缺失、boss伤害排行",
            "[c/FFFFFF:3.]批量改权限、导出权限表、复制文件、宝藏袋传送、修复局部图格",
            "[c/FFFFFF:4.]自动注册、自动建GM组、自动配权、进度锁、重置服务器",
            "[c/FFFFFF:5.]修召唤入侵事件、修天塔柱刷物品BUG、个人投票回档",
            "[c/FFFFFF:6.]进服恢复队伍与出生点、切换队伍投票、队伍出生点修改投票",
            "---------",
            "发送[c/FF6962:任意消息]显示下条信息\n",
        ];

        MotdMess3 =
        [
            "---------",
            "《小提示》",
            "人数进度锁开关:/pout boss",
            "重置服务器流程:/pout reset",
            "控制台指定管理:/user group {玩家名} GM",
            "[c/56B7E0:加buff给npc]被踢的[c/FFA562:临时]解决方案",
            "分配到VIP组: /user group {玩家名} vip",

             $"\n祝您游戏愉快!! [i:3459][c/81C9E8:by] [c/00FFFF:羽学][i:3456]\n",
        ];

        ClearSql =
        [
             "DELETE FROM tsCharacter",
             "DELETE FROM Warps",
             "DELETE FROM Regions",
             "DELETE FROM Research",
             "DELETE FROM RememberedPos",
        ];

        DeleteFile =
        [
             "tshock/145修复小公举/权限表/*.txt",
             "tshock/145修复小公举/自动备份存档/*.zip",
             "tshock/145修复小公举/临时申请备份/*.plr",
             "tshock/145修复小公举/导入存档/*.plr",
             "tshock/145修复小公举/导入存档/*.tws",
             "tshock/backups/*.bak",
             "tshock/logs/*.log",
             "world/*.wld",
             "world/*.bak",
             "world/*.bak2",
        ];

        CopyPaths =
        [
            "world","tshock/backups/",
        ];

        PostCMD = ["/worldinfo"];

        AfterCMD = ["/off-nosave"];

        AllowTpBagText = 
        [
            "bag"
        ];

        AllowRegionGroup = 
        [
            "superadmin","GM","admin","owner",
            "newadmin","trustedadmin",
        ];

        BeforeCMD =
        [
            "/spi reset",
            "/det reset all",
            "/cb on",
            "/cb zip",
            "/gift rs",
            "/clall",
            "/mw reset",
            "/airreset",
            "/astreset",
            "/vel reset",
            "/wat clearall",
            "/pbreload",
            "/礼包 重置",
            "/礼包重置",
            "/pvp reset",
            "/hreset",
            "/gs r",
            "/rm reset",
            "/gn clear",
            "/kdm reset",
            "/派系 reset",
            "/bwl reload",
            "/task clear",
            "/task reset",
            "/rpg reset",
            "/bank reset",
            "/deal reset",
            "/skill reset",
            "/level reset",
            "/replenreload",
            "/重读多重限制",
            "/重读阶段库存",
            "/clearbuffs all",
            "/重读物品超数量封禁",
            "/重读自定义怪物血量",
            "/重读禁止召唤怪物表",
            "/zresetallplayers",
            "/clearallplayersplus",
        ];

        Permission["default"] =
        [
            "tshock.npc.startinvasion","tshock.npc.summonboss","tshock.spectating",
            "tshock.admin.seeplayerids","tshock.world.time.usemoondial","tshock.npc.startdd2",
            "tshock.tp.spawn","tshock.tp.self","tshock.tp.home","tshock.tp.npc","tshock.admin.house",
            "tshock.world.movenpc","tshock.admin.warp","tshock.npc.clearanglerquests",
            "zhipm.vi","zhipm.vs","zhipm.sort", "challenger.fun","challenger.tip","weaponplus.plus",
            "economics.deal","economics.skill","economics.skill.use",
            "economics.skillpro.use","economics.rpg","economics.task.use",
            "economics.rpg.rank","economics.currency.query","economics.regain",
            "economics.deal.use","economics.rpg.chat",
            "essentials.tp.back","essentials.home.tp","essentials.tp.eback",
            "essentials.tp.up","essentials.tp.down","essentials.tp.left",
            "essentials.tp.right","essentials.home.delete","essentials.lastcommand",
            "prot.manualprotect","prot.manualdeprotect","prot.chestshare",
            "prot.switchshare","prot.othershare","prot.setbankchest","prot.bankchestshare",
            "prot.settradechest","prot.freetradechests","prot.regionsonlyprotections",
            "servertool.query.wall","servertool.online.duration","servertool.user.kick",
            "servertool.user.kill","servertool.user.dead",
            "tprequest.tpat","tprequest.gettpr","tprequest.tpauto","autoteam.blue","autoteam.toggle",
            "bag.use","house.use","lookbag","user.add","bagger.getbags","pvper.use","signinsign.tp",
            "back","dujie.use","dj.use","room.use","det.use","scp.use","vel.use","role.use","ndt.use",
            "mw.use","zm.user","bm.user","AutoAir.use","AutoStore.use","mhookfish","dw.use",
            "ProgressQuery.use","bossinfo", "cleartomb","clearangler","moonstyle","relive","cbag",
            "terrajump","terrajump.use","rainbowchat.use","regionvision.regionview",
            "itemsearch.cmd","itemsearch.chest","AdditionalPylons","ListPlugin","sfactions.use",
            "veinminer","history.get","bridgebuilder.bridge","swapplugin.toggle","autofish","autofish.common",
            "chireiden.omni.whynot","permcontrol","RecipesBrowser","DataSync","itempreserver.receive",
            "EndureBoost","ExtraDamage.use","create.copy","treedrop.togglemsg","DonotFuck",
            "在线礼包","免拦截","死者复生",
        ];

        Permission["vip"] =
        [
            "tshock.ignore.npcbuff"
        ];
    }
    #endregion

    #region 设置锁定NPC方法
    public HashSet<string> SetNpcByID()
    {
        var NewNpc = new HashSet<string>();

        foreach (var item in SetLockNpc())
        {
            var name = Lang.GetNPCNameValue(item);
            if (NewNpc.Contains(name)) continue;
            NewNpc.Add(name);
        }

        return NewNpc;
    }

    public HashSet<int> SetLockNpc()
    {
        var NewNpc = new HashSet<int>()
        {
            NPCID.EyeofCthulhu, NPCID.EaterofWorldsHead,
            NPCID.EaterofWorldsBody,  NPCID.EaterofWorldsTail,
            NPCID.SkeletronHead, NPCID.SkeletronHand, NPCID.KingSlime,
            NPCID.WallofFlesh, NPCID.WallofFleshEye,
            NPCID.TheHungry,NPCID.TheHungryII,
            NPCID.Retinazer, NPCID.Spazmatism,
            NPCID.BloodNautilus,NPCID.GoblinShark,
            NPCID.BloodEelHead,NPCID.BloodEelBody,NPCID.BloodEelTail,
            NPCID.SkeletronPrime, NPCID.PrimeCannon,
            NPCID.PrimeSaw,NPCID.PrimeVice, NPCID.PrimeLaser,
            NPCID.TheDestroyer, NPCID.TheDestroyerBody,
            NPCID.TheDestroyerTail,NPCID.Probe,NPCID.PirateCaptain,
            NPCID.QueenBee, NPCID.Golem, NPCID.GolemHead,
            NPCID.GolemFistLeft, NPCID.GolemFistRight,
            NPCID.GolemHeadFree, NPCID.Plantera,
            NPCID.PlanterasTentacle,NPCID.BrainofCthulhu,
            NPCID.Creeper,NPCID.DukeFishron,NPCID.GoblinSummoner,
            NPCID.MartianSaucer,NPCID.MartianSaucerTurret,
            NPCID.MartianSaucerCannon,NPCID.MartianSaucerCore,
            NPCID.MoonLordHead, NPCID.MoonLordHand,
            NPCID.MoonLordCore, NPCID.MoonLordFreeEye,
            NPCID.MoonLordLeechBlob, NPCID.CultistBoss,
            NPCID.CultistBossClone, NPCID.LunarTowerVortex,
            NPCID.PirateShip,NPCID.PirateShipCannon,
            NPCID.LunarTowerStardust,NPCID.LunarTowerNebula,
            NPCID.DD2Betsy,NPCID.DD2DarkMageT1,NPCID.DD2DarkMageT3,
            NPCID.LunarTowerSolar,NPCID.HallowBoss,
            NPCID.DD2OgreT2,NPCID.DD2OgreT3,
            NPCID.QueenSlimeBoss, NPCID.Deerclops,

        };

        return NewNpc;
    }
    #endregion

    #region 读取与创建配置文件方法
    public void Write()
    {
        string json = JsonConvert.SerializeObject(this, Formatting.Indented);
        File.WriteAllText(ConfigPath, json);
    }
    public static Configuration Read()
    {
        if (!File.Exists(ConfigPath))
        {
            var NewConfig = new Configuration();
            NewConfig.SetDefault();
            NewConfig.Write();
            return NewConfig;
        }
        else
        {
            string jsonContent = File.ReadAllText(ConfigPath);
            var config = JsonConvert.DeserializeObject<Configuration>(jsonContent)!;
            return config;
        }
    }
    #endregion
}