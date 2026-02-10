using Microsoft.Xna.Framework;
using TShockAPI;

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
        // 导存档给死亡玩家，需复活后的恢复存档数据
        public PlayerData? NeedRestores { get; set; } = null;
        // BOSS宝藏袋掉落坐标表
        public List<Vector2> BagPos { get; set; } = new();
        // 投票回档数据
        public VoteData? MyApply { get; set; } = null;
    }
    #endregion

    #region 回档投票数据类
    public class VoteData
    {
        public int ApplyIdx { get; set; } = 0;           // 申请的备份索引
        public DateTime ApplyTime { get; set; }          // 申请时间
        public int VoteYes { get; set; } = 0;            // 同意票数
        public int VoteNo { get; set; } = 0;             // 反对票数
        public List<string> Voted { get; set; } = new(); // 已投票玩家
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
