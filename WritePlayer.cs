using System.IO.Compression;
using System.Reflection;
using System.Security.Cryptography;
using Terraria;
using Terraria.ID;
using Terraria.IO;
using TShockAPI;
using TShockAPI.DB;
using static FixTools.FixTools;
using static FixTools.Utils;

namespace FixTools;

internal class WritePlayer
{
    private static string now = string.Empty;  // 当前操作玩家时间

    // 嘿嘿 从恋恋那抄来的就是好用，比枳的一堆if整洁多了
    private class MyPlayer : TSPlayer
    {
        public MyPlayer() : base(string.Empty)
        {
            this.Account = new UserAccount();
        }

        public Player Player
        {
            get => this.TPlayer;
            set => typeof(TSPlayer).GetField("FakePlayer",
                                    BindingFlags.NonPublic |
                                    BindingFlags.Instance)!.
                                    SetValue(this, value);
        }
    }

    #region 导出所有玩家方法
    public static void ExportAll(TSPlayer plr, string Dir)
    {
        //对每个导出的文件夹做时间名称后缀
        now = "_" + FormatFileName(DateTime.Now.ToString());
        var autoSave = Dir == AutoSaveDir ? true : false;
        var state = autoSave ? "备份" : "导出";

        try
        {
            var all = new List<UserAccount>();
            using (QueryResult queryResult = TShock.DB.QueryReader("SELECT * FROM tsCharacter"))
            {
                while (queryResult.Read())
                {
                    int num = queryResult.Get<int>("Account");
                    UserAccount user = TShock.UserAccounts.GetUserAccountByID(num);
                    all.Add(user);
                }
            }

            plr.SendMessage($"\n预计{state}数量：" + all.Count, color);
            if (all.Count < 1) return;

            string worldName = FormatFileName(Main.worldName);
            string exportDir = $"{Dir}/{worldName + now}";

            // 创建导出目录
            if (!Directory.Exists(exportDir))
                Directory.CreateDirectory(exportDir);

            List<string> yes = new();
            List<string> no = new();

            // 批量导出方法
            foreach (var one in all)
            {
                Player player = NewPlayer(one);

                if (Export(player, exportDir))
                {
                    yes.Add(player.name);
                }
                else
                {
                    no.Add(player.name);
                }
            }

            if (no.Count > 0)
            {
                plr.SendMessage($"{state}失败:\n{string.Join(",", no)}", color);
                plr.SendMessage($"请通知以上玩家重进服务器,退出一次确保数据正常", color2);
            }

            if (yes.Count > 0)
            {
                CopyWorld(exportDir); // 复制世界文件

                // 备份数据库文件
                if (autoSave && Config.AutoSaveSqlite)
                {
                    // 复制tshock.sqlite文件到备份目录的子目录中
                    string sqlName = Path.GetFileName(SqlPath);
                    string destSqlPath = Path.Combine(exportDir, sqlName);
                    File.Copy(SqlPath, destSqlPath, true);
                    plr.SendMessage($"已{state}数据库文件:{sqlName}", color2);
                }

                plr.SendMessage($"{state}成功:\n{string.Join(",", yes)}", color);
            }

            string sourcePath = $"{Dir}/{worldName + now}";
            string destPath = $"{Dir}/{worldName + now}.zip";
            ZipFile.CreateFromDirectory(sourcePath, destPath, CompressionLevel.SmallestSize, false); //压缩打包
            Directory.Delete(sourcePath, true); // 删除文件夹
            plr.SendMessage($"已全部打包到:\n{Dir}", color);
            plr.SendMessage($"压缩包名称:{worldName + now}.zip", color2);

        }
        catch (Exception ex)
        {
            plr.SendErrorMessage($"{state}存档错误");
            TShock.Log.ConsoleError(ex.ToString());
        }
    }
    #endregion

    #region 导出单个玩家存档
    public static void ExportPlayer(string name, TSPlayer plr, string Dir)
    {
        //对每个导出的文件夹做时间名称后缀
        now = "_" + FormatFileName(DateTime.Now.ToString());
        string worldName = FormatFileName(Main.worldName);
        string exportDir = $"{Dir}/{worldName + now}";

        //只导出一人或搜到的多人
        List<TSPlayer> list = FindPlayer(name);
        if (list.Count == 0)//查不到，开始模糊搜索
        {
            plr.SendInfoMessage("未找到在线玩家，尝试离线玩家数据");
            var users = TShock.UserAccounts.GetUserAccountsByName(name, true);
            if (users.Count == 1 || users.Count > 1 && users.Exists(x => x.Name == name))
            {
                if (users.Count > 1)
                {
                    users[0] = users.Find(x => x.Name == name)!;
                }

                Player? player = NewPlayer(users[0]);

                if (!Export(player, exportDir)) return;

                CopyWorld(exportDir); // 复制世界文件

                plr.SendMessage($"导出成功！目录:\n{exportDir}/{name}.plr", color);
            }
            else if (users.Count == 0)
            {
                plr.SendInfoMessage("未找到该玩家的备份数据，可能该玩家从未登录过本服务器");
                return;
            }
            else if (users.Count > 1)
            {
                plr.SendInfoMessage("找到多个同名玩家，请输入更精确的名字或accID");
                return;
            }
        }
        else if (list.Count > 1)
        {
            plr.SendInfoMessage("找到多个同名玩家，请输入更精确的名字或accID");
        }
        else if (Export(list[0].TPlayer, exportDir))
        {
            CopyWorld(exportDir); // 复制世界文件
            plr.SendMessage($"导出成功！目录:\n{exportDir}/{list[0].Name}.plr", color);
        }
        else
        {
            plr.SendErrorMessage("导出失败，因输入错误");
        }
    }
    #endregion

    #region 创建新玩家
    private static Player NewPlayer(UserAccount one)
    {
        var p = new MyPlayer();
        p.Account.ID = one.ID;
        p.Player = new Player
        {
            name = one.Name
        };

        var data = TShock.CharacterDB.GetPlayerData(p, one.ID);

        try
        {
            data.RestoreCharacter(p);
        }
        catch { }

        Player? player = p.Player;
        return player;
    }
    #endregion

    #region 导出plr文件
    public static bool Export(Player? player, string exportDir)
    {
        if (player is null) return false;

        var playerNama = player.name;

        //移除不合法的字符
        playerNama = FormatFileName(playerNama);
        string worldname = new string(Main.worldName);

        //移除不合法的字符
        worldname = FormatFileName(worldname);

        // 从PlayerFileData.CreateAndSave方法抄来的
        PlayerFileData data = new PlayerFileData();
        data.Metadata = FileMetadata.FromCurrentSettings(FileType.Player);
        data.Player = player;
        data._isCloudSave = false;
        FileData fileData = data;
        fileData._path = $"{exportDir}/{playerNama}.plr";
        data.SetPlayTime(new TimeSpan(0)); // 不设置游玩时间,还得遍历所有玩家消耗服务器性能
        Main.LocalFavoriteData.ClearEntry(data);

        try
        {
            string path = data.Path;

            if (string.IsNullOrEmpty(path))
            {
                return false;
            }
            else
            {
                // 导出文件夹不存在,自动创建文件夹
                if (!Directory.Exists(exportDir))
                    Directory.CreateDirectory(exportDir);

                // 如果配置了版本号，则使用配置的版本号，否则使用最新版本号
                var GameVersion = Config.GameVersion == -1 ? GameVersionID.Latest : Config.GameVersion;
                if (GameVersion == -1)
                {
                    // 原版的保存角色存档方法
                    // 但会根据OTAPI写入最新版本号
                    Player.InternalSavePlayerFile(data);
                    return true;
                }
                else
                {
                    // 把InternalSavePlayerFile方法手抄出来,为了指定导出版本
                    using RijndaelManaged rijndaelManaged = new();
                    using Stream stream = new FileStream(path, FileMode.Create);
                    using CryptoStream cryptoStream = new CryptoStream(stream, rijndaelManaged.CreateEncryptor(Player.ENCRYPTION_KEY, Player.ENCRYPTION_KEY), CryptoStreamMode.Write);
                    using BinaryWriter binaryWriter = new BinaryWriter(cryptoStream);
                    binaryWriter.Write(GameVersion); // 修正：使用配置的版本号
                    data.Metadata.Write(binaryWriter);
                    Player.Serialize(data, player, binaryWriter);
                    binaryWriter.Flush();
                    cryptoStream.FlushFinalBlock();
                    stream.Flush();

                    return true;
                }
            }
        }
        catch (Exception ex)
        {
            TShock.Log.ConsoleError("导出plr文件错误:\n" + ex.ToString());
            TShock.Log.ConsoleError($"名字：{playerNama},路径：\n{data.Path}");
            return false;
        }
    }
    #endregion

    #region 复制世界文件方法
    private static void CopyWorld(string exportDir)
    {
        // 复制世界文件
        string worldPath = Path.Combine(typeof(TShock).Assembly.Location, "world");
        if (Directory.Exists(worldPath))
        {
            string worldName = FormatFileName(Main.worldName);
            string[] worldFiles = Directory.GetFiles(worldPath, $"*.wld");
            foreach (string worldFile in worldFiles)
            {
                // 只复制第一个，或者使用特定逻辑确定要复制哪个
                string destFile = Path.Combine(exportDir, $"{Main.worldName}.wld");
                File.Copy(worldFile, destFile, true);
                break; // 只处理第一个文件
            }
        }
    }
    #endregion
}
