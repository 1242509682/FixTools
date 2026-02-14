# FixTools 145修复小公举

- 作者: 羽学
- 出处: TShock官方群816771079
- 这是一个Tshock服务器插件，主要用于：
自动注册、宝藏袋传送、跨版本进服、导入与导出SSC玩家存档、
执行配置表指令、修改TShock数据表、禁用区域箱子合成材料、投票回档
批量修改多组权限、删除指定路径文件、互动入服公告、修复天塔柱刷物品
复制文件、重置服务器、开服控制台信息、修复物品召唤入侵事件、boss伤害排行
使用自动备份修复局部图格（支持修复145实体家具物品等）
- 本插件仅适配于[TShcock非官测试版1454](https://github.com/WindFrost-CSFT/TShock/)



## 指令

| 语法      |    权限     |        说明        |
| --------- | :---------: | :----------------: |
| /pout | pout.use |   指令菜单   |
| /pout plr | pout.use |   玩家存档管理菜单   |
| /pout save | pout.use |   自动备份存档菜单   |
| /pout vote | pout.use |   投票回档开关   |
| /bak | 无 |   投票回档功能   |
| /pout rw | pout.use |   使用自动备份,修复局部图格   |
| /pout vs | pout.use |   设置导出版本号   |
| /pout join | pout.use |   跨版本进服开关   |
| /pout inv | pout.use |   修复物品召唤入侵事件   |
| /pout bag | pout.use |   宝藏袋传送开关   |
| /pout chest | pout.use |   禁用区域箱子材料开关   |
| /pout ttz | pout.use |   修复天塔柱刷物品BUG开关   |
| /pout motd | pout.use |   进服公告开关,并在开启时广播一次跨版本进服开关   |
| /pout fix | pout.use |   显示修复地图区块缺失流程   |
| /pout copy | pout.use |   复制文件到配置指定路径   |
| /pout rm | pout.use |   删除配置指定路径文件   |
| /pout sql  | pout.use |   修改tshock.sqlite中的指定数据表   |
| /pout cmd | pout.use |   执行配置中游戏时指令   |
| /pout reg | pout.use |   自动注册开关(适配CaiBot插件)   |
| /pout add | pout.use |   批量加权限   |
| /pout del | pout.use |   批量删权限   |
| /pout lpm | pout.use |   导出权限表   |
| /pout boss | pout.use |   人数进度锁开关   |
| /pout reset | pout.use |   显示重置服务器流程   |
| /reload | tshock.cfg.reload | 重新加载插件配置 |

## 更新日志

```
v20260214 ——1.1.4 
加入了boss战后自动执行/bossDamage指令里的数据
并为其重新设计调色与信息排版,不会出现之前的排名

v20260213 ——1.1.3 适配Beta 1770f2d
依赖项改用UnrealMultiple.TSAPI-Beta
优化了性能,仅对修改的图格进行修复
减小了世界快照文件,使用GZIP二进制压缩
加入了/pt rw b 子命令,支持撤销本次修复
支持修复陷阱宝箱与梳妆台

v20260212 ——1.1.2 适配1.4.5.5
加入了修复局部图格功能,对应指令:/pt rw
可以从自动备份里选择出“地图快照”
使用/pt rw 索引 选择一个备份，
再用精密线控仪-红电线模式下画你需要修复的区域
建议画得区域别太大，避免加载太慢堵塞线程。
兼容1.4.5新家具,支持恢复实体物品

v20260211 ——1.1.1
加入了投票回档功能：
开关指令/pout vote
功能指令/bak(无权限)
/bak ——列出当前自动备份列表
/bak 1 ——选择第1个的备份作为申请回档
/bak y ——同意（申请人无法使用）
/bak n ——拒绝（申请人无法使用）
注意:拥有pout.use权限可直接决定结果
条件1.在线人数达到最小投票人数(排除申请人自己)
条件2.同意率达到通过率
条件3.至少半数玩家参与或达到最小投票人数
新增配置项：
【投票回档开关】【投票过期时间】
【投票通过概率】【投票最少人数】

v20260210 ——1.1.0
给改数据与删文件指令加入了yes确认参数,避免误操作
修复重置服务器时，出现的地图找不到元数据时空引用报错
现在pout主命令支持别名：pt
加入了/pout plr z指令
列出所有备份索引号:/pt p z
导入最新备份给玩家:/pt p z 玩家名
导入指定备份给玩家:/pt p z 索引 玩家名
导指定备份给所有人:/pt p z 索引 all
注意:索引为压缩包的序号,最新的是1，
每次执行前后都会清空《导入存档》文件夹

v20260209 ——1.0.9
修复天塔柱刷物品BUG，对应开关指令：/pout ttz

v20260208 ——1.0.8
1.加入了修复物品召唤入侵事件
2.加入了石后神庙钥匙召唤火星暴乱事件
3.重构/pout save指令
备份开关:/pout sv on|off
备份间隔:/pout sv min 分钟
备份清理:/pout sv cl
备份保留:/pout sv keep 数量
备份地图:/pout sv wld
备份数据:/pout sv sql
消息显示:/pout sv mag
加入配置项:
【自动清理备份】【保留备份数量】
【备份显示消息】【自动备份地图】
4.重构/pout plr指令
导出介绍:/pout p c
导出所有:/pout p c all
导出指定:/pout p c 玩家名
导入介绍:/pout p r
导入所有:/pout p r all
导入对应:/pout p r 索引
导入指定:/pout p r 索引 玩家名

v20260207 ——1.0.7
适配TShock测试版本号：c5a1747
1.加入了宝藏袋传送功能,对应开关指令:/pout bag
boss死亡后发送消息“宝藏袋”传送到该位置
如果有多个则优先传送到最近击败掉落的那个
2.加入了禁用区域箱子材料，对应开关指令:/pout cheat
用于禁止特性:在保护区域偷箱子材料来合成(区域所有者和管理排外)
3.优化了进服公告:
支持互动类型的分段发送（为了手游排版显示更整洁）
支持使用16进制单独给个别文字上色，并从上色位置重新渐变。
进服公告支持以下占位符：
插件名、玩家名、ip、uuid、组名、账号、武器类型、物品图标、物品名
进度、生命、生命上限、魔力、魔力上限、队伍、同队人数、同队玩家
别队人数、队伍统计、服务器名、在线人数、在线玩家、服务器上限

v202602051 ——1.0.6
适配TShock测试版本号：123a3bd
加入了人数进度锁，对应指令:/pout boss
新增配置项【人数进度锁】【解锁人数】【已解锁怪物】【进度锁怪物】
优化了导入存档功能,支持在线恢复存档物品
支持死亡重生或死亡时离服重进恢复(重启服务器后无效)
现在只需输入存档文件索引号,不需要输入文件名
示例:
1号存档给对应名字的玩家:/pout p 1 r 
1号存档给指定名字的玩家:/pout p 1 羽学 r
所有存档给对应玩家:/pout p all r
如果不存在这个玩家则新建账号(仅在没caibot插件情况下有效)
加入了新配置权限:
default组：tshock.spectating —占卜球观战权限

v202602042 ——1.0.5
新增文件夹《自动备份存档》
加入了自动备份功能，指令/pout save
新增配置项【自动备份存档】【自动备份数据库】【备份存档分钟数】
加入了开服自动修复地图区块缺失功能
加入了新配置权限
default组：
1.tshock.npc.startinvasion —召唤事件
2.tshock.npc.summonboss —召唤BOSS权限
vip组：tshock.ignore.npcbuff —忽略对NPC施加BUFF踢出

v20260204 ——1.0.4
重构了导出存档代码逻辑，加入了导入.plr存档功能
新增《导入存档》与《导出存档》2个文件夹
将导入与导出指令细分到/pout plr子命令内：
-----导出------
导出所有玩家：/pout plr all
导出指定玩家：/pout plr 玩家名
导出指定玩家：/pout plr 账号id
账号查询指令：/who -i
-----导入------
列出所有.plr存档：/pout plr r
对应存档给所有玩家：/pout plr all r
对应存档给对应玩家：/pout plr 存档名 r
指定存档给指定玩家：/pout plr 存档名 玩家名 r
玩家不存在则自动创建账号,密码为【自动注册默认密码】
注:存档名不需要写.plr,写文件名即可

v20260203 ——1.0.3
配置更名:配置文件.json
1.加入了复制文件指令：
新增《复制源文件》文件夹
把需要复制的文件放入《复制源文件》内,根据【复制文件输出路径】配置项
使用/pout copy 文件索引 路径索引 进行复制到指定路径
【输出路径】不存在则自动创建文件夹,
有相同文件则自动将原文件更名：文件名.bak_当前时间
2.将删除指定路径文件的file指令更名为rm
3.重构reset指令逻辑:
新增《重置时用的复制地图》文件夹
将多个地图放入该文件夹，
使用reset yes指令时会随机从中选择1个地图，
并更名为SFE4.wld复制到world启动路径
文件夹没有地图则根据server.properties参数表重建
4.给fix和reset指令加入yes确认参数,默认显示该指令具体工作流程,避免误操作
5.自动注册适配CaiBotLite插件,检测到该插件，默认禁用并禁止使用指令开启
6.加入了【开服自动配权】配置项：开服后自动根据【批量改权限】添加指定组没有的权限
7.考虑简幻欢面板服控制台信息显示问题,修改了默认配置的【进服公告】：
- 插件支持功能
- 适配TShock测试版本号
- 指令权限
- 配置路径
- 部分指令引导示意
等相关辅助开服信息
8.默认配置修改了开服后执行指令加入了“/worldinfo”查看当前地图信息

v202602023 ——1.0.2
给/pout reset加入了:
以及关服倒计时
踢出玩家功能确保SSC正确保存
修复重置时地图不会被删除的BUG
给/pout fix加入了关服倒计时

v20260202 ——1.0.1
加入了自动注册功能:
/pout reg 密码可修改默认密码,不带密码则切换自动注册开关
加入了导出组的权限列表生成txt文件
修改权限不限于default组，支持键值指定更多组。
加入了fix指令用于修复地图缺失BUG：
修改server.properties文件的autocreate值后自动关服重启
加入了更新配置项：
开服后执行指令、游戏时执行指令、重置后执行指令、重置前执行指令
细分了批量改权限表:
使default配置权限,不会拥有TShock原有数据库内的权限,
以方便批量移除配置权限
当前适配TShock测试版本号：e5299d7

v20260201 ——1.0.0
本插件整合了：
1.枳的pco计划书部分功能
2.Leader的NoVisualLimit无视版本验证
支持导出指定版本号导出玩家SSC存档，版本号为-1时候默认采用最新的版本导出
用于平替日常所需的服务器管理功能，仅在TShock临时版期间维护,后续将不再更新
玩家进服时会提示“《关于1.4.5地图缺失区块BUG》临时解决方案”
重置服务器时只需要输入/pout reset 然后重启服务器即可
```

## 配置
> 配置文件位置：tshock/145修复小公举/配置文件.json
```json
{
  "自建GM权限组": true,
  "启用自动注册": true,
  "注册默认密码": "123456",
  "自动备份存档": true,
  "备份地图快照": true,
  "自动备份地图": true,
  "自动备份数据库": true,
  "备份存档分钟数": 30,
  "自动清理备份": true,
  "保留备份数量": 30,
  "备份显示消息": true,
  "投票回档开关": true,
  "投票过期时间": 30,
  "投票通过概率": 0.5,
  "投票最少人数": 2,
  "导出存档的版本号": 317,
  "版本号对照参考表": [
    "最新版  : -1",
    "1.4.5.5 : 318",
    "1.4.5.4 : 317",
    "1.4.5.3 : 316",
    "1.4.5.0 : 315",
    "1.4.4   : 279"
  ],
  "跨版本进服": true,
  "宝藏袋传送": true,
  "宝藏袋传送关键词": [
    "宝藏袋",
    "bag"
  ],
  "自动修复地图缺失": true,
  "修复物品召唤入侵事件": true,
  "修复天塔柱刷物品BUG": true,
  "石后神庙钥匙召唤火星暴乱": true,
  "禁用区域箱子材料": true,
  "禁用区域箱子范围": 40.0,
  "允许区域合成组": [
    "superadmin",
    "GM",
    "admin",
    "owner",
    "newadmin",
    "trustedadmin"
  ],
  "重置清理数据表": [
    "DELETE FROM tsCharacter",
    "DELETE FROM Warps",
    "DELETE FROM Regions",
    "DELETE FROM Research",
    "DELETE FROM RememberedPos"
  ],
  "重置时删除文件": [
    "tshock/145修复小公举/权限表/*.txt",
    "tshock/145修复小公举/自动备份存档/*.zip",
    "tshock/145修复小公举/临时申请备份/*.plr",
    "tshock/backups/*.bak",
    "tshock/logs/*.log",
    "world/*.wld",
    "world/*.bak",
    "world/*.bak2"
  ],
  "重置后执行指令": [
    "/off-nosave"
  ],
  "重置前执行指令": [
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
    "/clearallplayersplus"
  ],
  "开服后执行指令": [
    "/worldinfo"
  ],
  "游戏时执行指令": [],
  "开服自动配权": true,
  "启用进服公告": true,
  "进服公告1": [
    "\n欢迎 拿着{武器类型}{物品图标}的{玩家名} 来到 {服务器名}",
    "在线玩家 [c/FFFFFF:({在线人数}/{服务器上限})]: {在线玩家}",
    "指令:/pout 权限:pout.use",
    "配置路径: tshock/[c/FF6962:{插件名}]/配置文件.json",
    "TShock官方Q群:816771079",
    "所在队伍:{队伍} {同队人数}/{别队人数}",
    "同队玩家:{同队玩家}",
    "当前进度:{进度}",
    "---------",
    "发送[c/FF6962:任意消息]了解本插件相关功能\n"
  ],
  "进服公告2": [
    "---------",
    "《插件支持功能》适配版本:c5a1747",
    "[c/FFFFFF:1.]导入导出SSC存档、自动备份存档、禁用区域箱子材料",
    "[c/FFFFFF:2.]智能进服公告、跨版本进服、修复地图区块缺失",
    "[c/FFFFFF:3.]批量改权限、导出权限表、复制文件、宝藏袋传送、修复局部图格",
    "[c/FFFFFF:4.]自动注册、自动建GM组、自动配权、进度锁、重置服务器",
    "[c/FFFFFF:5.]修复物品召唤入侵事件、修复天塔柱刷物品BUG、投票回档",
    "---------",
    "发送[c/FF6962:任意消息]显示下条信息\n"
  ],
  "进服公告3": [
    "---------",
    "《小提示》",
    "人数进度锁开关:/pout boss",
    "重置服务器流程:/pout reset",
    "控制台指定管理:/user group {玩家名} GM",
    "[c/56B7E0:加buff给npc]被踢的[c/FFA562:临时]解决方案",
    "分配到VIP组: /user group {玩家名} vip",
    "\n祝您游戏愉快!! [i:3459][c/81C9E8:by] [c/00FFFF:羽学][i:3456]\n"
  ],
  "复制文件输出路径": [
    "world",
    "tshock/backups/"
  ],
  "人数进度锁": false,
  "解锁人数": 3,
  "已解锁怪物": [],
  "进度锁怪物": [
    "克苏鲁之眼",
    "世界吞噬怪",
    "骷髅王",
    "史莱姆王",
    "血肉墙",
    "饿鬼",
    "激光眼",
    "魔焰眼",
    "恐惧鹦鹉螺",
    "血浆哥布林鲨鱼",
    "血鳗鱼",
    "机械骷髅王",
    "机械炮",
    "机械锯",
    "机械钳",
    "机械激光",
    "毁灭者",
    "探测怪",
    "海盗船长",
    "蜂王",
    "石巨人",
    "石巨人头",
    "石巨人之拳",
    "世纪之花",
    "世纪之花触手",
    "克苏鲁之脑",
    "飞眼怪",
    "猪龙鱼公爵",
    "哥布林术士",
    "火星飞碟",
    "火星飞碟炮塔",
    "火星飞碟炮",
    "月亮领主",
    "月亮领主手",
    "月亮领主心脏",
    "克苏鲁真眼",
    "月蛭凝块",
    "拜月教邪教徒",
    "星旋柱",
    "荷兰飞盗船",
    "荷兰大炮",
    "星尘柱",
    "星云柱",
    "双足翼龙",
    "黑暗魔法师",
    "日耀柱",
    "光之女皇",
    "食人魔",
    "史莱姆皇后",
    "独眼巨鹿"
  ],
  "批量改权限": {
    "default": [
      "tshock.npc.startinvasion",
      "tshock.npc.summonboss",
      "tshock.spectating",
      "tshock.admin.seeplayerids",
      "tshock.world.time.usemoondial",
      "tshock.npc.startdd2",
      "tshock.tp.spawn",
      "tshock.tp.self",
      "tshock.tp.home",
      "tshock.tp.npc",
      "tshock.admin.house",
      "tshock.world.movenpc",
      "tshock.admin.warp",
      "tshock.npc.clearanglerquests",
      "zhipm.vi",
      "zhipm.vs",
      "zhipm.sort",
      "challenger.fun",
      "challenger.tip",
      "weaponplus.plus",
      "economics.deal",
      "economics.skill",
      "economics.skill.use",
      "economics.skillpro.use",
      "economics.rpg",
      "economics.task.use",
      "economics.rpg.rank",
      "economics.currency.query",
      "economics.regain",
      "economics.deal.use",
      "economics.rpg.chat",
      "essentials.tp.back",
      "essentials.home.tp",
      "essentials.tp.eback",
      "essentials.tp.up",
      "essentials.tp.down",
      "essentials.tp.left",
      "essentials.tp.right",
      "essentials.home.delete",
      "essentials.lastcommand",
      "prot.manualprotect",
      "prot.manualdeprotect",
      "prot.chestshare",
      "prot.switchshare",
      "prot.othershare",
      "prot.setbankchest",
      "prot.bankchestshare",
      "prot.settradechest",
      "prot.freetradechests",
      "prot.regionsonlyprotections",
      "servertool.query.wall",
      "servertool.online.duration",
      "servertool.user.kick",
      "servertool.user.kill",
      "servertool.user.dead",
      "tprequest.tpat",
      "tprequest.gettpr",
      "tprequest.tpauto",
      "autoteam.blue",
      "autoteam.toggle",
      "bag.use",
      "house.use",
      "lookbag",
      "user.add",
      "bagger.getbags",
      "pvper.use",
      "signinsign.tp",
      "back",
      "dujie.use",
      "dj.use",
      "room.use",
      "det.use",
      "scp.use",
      "vel.use",
      "role.use",
      "ndt.use",
      "mw.use",
      "zm.user",
      "bm.user",
      "AutoAir.use",
      "AutoStore.use",
      "mhookfish",
      "dw.use",
      "ProgressQuery.use",
      "bossinfo",
      "cleartomb",
      "clearangler",
      "moonstyle",
      "relive",
      "cbag",
      "terrajump",
      "terrajump.use",
      "rainbowchat.use",
      "regionvision.regionview",
      "itemsearch.cmd",
      "itemsearch.chest",
      "AdditionalPylons",
      "ListPlugin",
      "sfactions.use",
      "veinminer",
      "history.get",
      "bridgebuilder.bridge",
      "swapplugin.toggle",
      "autofish",
      "autofish.common",
      "chireiden.omni.whynot",
      "permcontrol",
      "RecipesBrowser",
      "DataSync",
      "itempreserver.receive",
      "EndureBoost",
      "ExtraDamage.use",
      "create.copy",
      "treedrop.togglemsg",
      "DonotFuck",
      "在线礼包",
      "免拦截",
      "死者复生"
    ],
    "vip": [
      "tshock.ignore.npcbuff"
    ]
  }
}
```

## 反馈
- TShock官方QQ群：816771079