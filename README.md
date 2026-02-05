# FixTools 145修复小公举

- 作者: 羽学
- 出处: TShock官方群816771079
- 这是一个Tshock服务器插件，主要用于：
自动注册、入服公告、跨版本进服、导入与导出SSC玩家存档、
执行配置表指令、修改TShock数据表、自动备份存档
批量修改多组权限、删除指定路径文件、
复制文件、重置服务器、开服控制台信息等功能
- 本插件仅适配于[TShcock非官测试版1453](https://github.com/WindFrost-CSFT/TShock/)



## 指令

| 语法      |    权限     |        说明        |
| --------- | :---------: | :----------------: |
| /pout | pout.use |   指令菜单   |
| /pout plr | pout.use |   玩家存档管理指令菜单   |
| /pout plr 玩家名 | pout.use |   导出指定玩家存档,并打包地图   |
| /pout plr all | pout.use |   导出所有玩家存档,并打包地图,压缩为zip   |
| /pout plr r | pout.use |   列出《导入存档》文件夹里所有.plr存档   |
| /pout plr 存档索引 r | pout.use |   将指定存档导入对应玩家   |
| /pout plr 存档索引 玩家名 r | pout.use |   将指定存档导入给指定玩家   |
| /pout plr all r | pout.use |   导入所有存档给对应玩家,不存在则创建账号   |
| /pout save | pout.use |   自动备份存档开关   |
| /pout save 分钟数 | pout.use |   修改备份存档间隔   |
| /pout vs | pout.use |   设置导出版本号   |
| /pout join | pout.use |   跨版本进服开关   |
| /pout motd | pout.use |   进服公告开关,并在开启时广播一次跨版本进服开关   |
| /pout fix | pout.use |   显示修复地图区块缺失流程   |
| /pout fix auto | pout.use |   切换自动修复地图开关   |
| /pout fix yes | pout.use |   确认修复地图区块缺失BUG   |
| /pout copy | pout.use |   复制文件到配置指定路径   |
| /pout rm | pout.use |   删除配置指定路径文件   |
| /pout sql  | pout.use |   修改tshock.sqlite中的指定数据表   |
| /pout cmd | pout.use |   执行配置中游戏时指令   |
| /pout reg | pout.use |   自动注册开关(适配CaiBot插件)   |
| /pout reg 密码 | pout.use |   修改自动注册的默认密码   |
| /pout add | pout.use |   批量加权限   |
| /pout del | pout.use |   批量删权限   |
| /pout lpm | pout.use |   导出权限表   |
| /pout boss | pout.use |   人数进度锁开关   |
| /pout boss 人数 | pout.use |   设置解锁人数   |
| /pout reset | pout.use |   显示重置服务器流程   |
| /pout reset yes | pout.use |   确认重置服务器   |
| /reload | tshock.cfg.reload | 重新加载插件配置 |

## 更新日志

```
v202602051 ——1.0.6
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
加入了新配置权限,适配TShock测试版本号：ab1208c
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
  "跨版本进服": true,
  "自动修复地图缺失": true,
  "自动备份存档": true,
  "自动备份数据库": true,
  "备份存档分钟数": 30,
  "导出存档的版本号": 315,
  "版本号对照参考表": [
    "最新版  : -1",
    "1.4.5.3 : 316",
    "1.4.5.0 : 315",
    "1.4.4   : 279"
  ],
  "清理数据表": [
    "DELETE FROM tsCharacter",
    "DELETE FROM Warps",
    "DELETE FROM Regions",
    "DELETE FROM Research",
    "DELETE FROM RememberedPos"
  ],
  "删除文件": [
    "tshock/145修复小公举/权限表/*.txt",
    "tshock/145修复小公举/自动备份存档/*.zip",
    "tshock/backups/*.bak",
    "tshock/logs/*.log",
    "world/*.wld",
    "world/*.bak",
    "world/*.bak2"
  ],
  "复制文件输出路径": [
    "world",
    "tshock/backups/"
  ],
  "启用自动注册": true,
  "注册默认密码": "123456",
  "启用进服公告": true,
  "进服公告": [
    "\n[本插件支持以下功能] 适配版本号:ab1208c",
    "1.导入或导出玩家强制开荒存档、自动备份存档",
    "2.自动注册、跨版本进服、修复地图区块缺失",
    "3.批量改权限、导出权限表、批量删文件、复制文件",
    "4.进服公告、自动建GM组、自动配权、进度锁、重置服务器",
    "\n《如出现因为施加buff给npc被踢出》",
    "可将玩家提升到vip组 /user group 玩家名 vip",
    "请自行甄别玩家是否开挂,否则后果自负",
    "\n欢迎来到泰拉瑞亚 1.4.5.3 服务器",
    "TShock官方Q群:816771079",
    "指令/pout 权限:pout.use",
    "配置文件路径: tshock/145修复小公举/配置文件.json",
    "\n已根据配置自动批量添加组权限",
    "不需可批量移除:/pout del",
    "人数进度锁开关:/pout boss",
    "重置服务器流程:/pout reset",
    "控制台指定管理:/user group 玩家名 GM",
    "\n[145修复小公举] 祝您游戏愉快!!  by羽学\n"
  ],
  "开服后执行指令": [
    "/worldinfo"
  ],
  "游戏时执行指令": [],
  "重置后执行指令": [
    "/off"
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
  "人数进度锁": true,
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
  "开服自动配权": true,
  "批量改权限": {
    "default": [
      "tshock.npc.startinvasion",
      "tshock.npc.summonboss",
      "tshock.spectating",
      "tshock.admin.seeplayerids",
      "tshock.world.time.usemoondial",
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
- 优先发issued -> 共同维护的插件库：https://github.com/UnrealMultiple/TShockPlugin
- 次优先：TShock官方群：816771079
- 大概率看不到但是也可以：国内社区trhub.cn ，bbstr.net , tr.monika.love