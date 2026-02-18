using Microsoft.Xna.Framework;
using TShockAPI;
using Terraria;
using Terraria.GameContent;

namespace FixTools;

internal class PlayerState
{
    #region 玩家数据状态类（内存）
    public class MyData
    {
        // 进服公告进程
        public int Motd { get; set; } = 0;
        // 发送公告时间，防止重复发送
        public DateTime SendTime { get; set; } = DateTime.MinValue;
        // 自动注册后发送反馈语标志
        public bool Register { get; set; } = false;
        // 随机密码
        public string? DefPass { get; set; } = string.Empty;
        // 导存档给死亡玩家，需复活后的恢复存档数据
        public PlayerData? NeedRestores { get; set; } = null;
        // BOSS宝藏袋掉落坐标表
        public List<Vector2> BagPos { get; set; } = new();
        // 投票回档数据
        public BakCmd.VoteData? MyApply { get; set; } = null;

        // 地图快照路径,用来修复局部图格
        public string rwSnap { get; set; } = string.Empty;
        // 标牌文件路径,用来修复标牌信息
        public string rwSign { get; set; } = string.Empty;
        // 获取到快照,开启精密线控仪检测
        public bool rwWire { get; set; } = false;
        // 撤销修复文件路径（后进先出栈）
        public List<string> rwUndoStack = new();


        // 玩家队伍
        public int Team { get; set; } = -1; // 缓存的队伍ID
        public bool Lock { get; set; } = false; // 是否被锁定切换队伍（阻止切换）
        public DateTime? SwitchTime { get; set; } = null; // 记录切换队伍时间
        public Point? NeedTp { get; set; } = null; // 记录是否需要传送
        public DateTime? JoinTime { get; set; } = null; // 记录可以传送的时间点

        // 玩家死亡状态下离开服务器时间
        public DateTime? DeadTime { get; set; } = null;
        // 回到死亡点
        public List<Vector2> BackPos { get; set; } = new();
    }
    #endregion

    #region 获取数据方法
    private static Dictionary<string, MyData> PData = new();
    public static MyData GetData(string name)
    {
        if (!PData.ContainsKey(name))
            PData[name] = new MyData();

        return PData[name];
    }
    #endregion
}

