using Microsoft.Xna.Framework;
using TShockAPI;

namespace FixTools;

internal class PlayerState
{
    public class MyData
    {
        // 0=未发送公告，1=已发送公告但未显示更多信息，2=已显示更多信息
        public int Motd { get; set; } = 0;
        public bool Register { get; set; } = false;
        public PlayerData? NeedRestores { get; set; } = null;
        public List<Vector2> BagPos { get; set; } = new();
        public DateTime SendTime { get; set; } = DateTime.MinValue;
    }

    private static Dictionary<string, MyData> PData = new();
    public static MyData GetData(string name)
    {
        if (!PData.ContainsKey(name))
            PData[name] = new MyData();

        return PData[name];
    }
}
