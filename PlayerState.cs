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
