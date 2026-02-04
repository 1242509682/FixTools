using Terraria;
using TShockAPI;
using TShockAPI.DB;
using Terraria.IO;
using System.Text;
using TerrariaApi.Server;
using static FixTools.FixTools;
using static FixTools.Utils;

namespace FixTools;

public class ReaderPlayer
{
    #region 把存档导入同名的对应玩家
    public static void ReadPlayer(TSPlayer plr, string playerName, string file)
    {
        if (!File.Exists(file))
        {
            plr.SendMessage($"存档文件 {Path.GetFileName(file)} 不存在无法导入!", color);
            ShowPlrFile(plr);
            return;
        }

        try
        {
            ReadPlayerCopyCharacter(file, playerName);
            plr.SendSuccessMessage($"{Path.GetFileName(file)} 成功导入给指定玩家 {playerName}!");
        }
        catch (Exception ex)
        {
            TShock.Log.ConsoleError($"读取过程错误:{ex}");
        }
    }
    #endregion

    #region 把存档导入给指定玩家，如果没账号自动创建
    public static void ReadPlayer(TSPlayer plr, string file)
    {
        if (!File.Exists(file))
        {
            plr.SendMessage($"存档文件 {Path.GetFileName(file)} 不存在无法导入!", color2);
            ShowPlrFile(plr);
            return;
        }

        try
        {
            ReadPlayerCopyCharacter(file);
            plr.SendMessage($"{Path.GetFileName(file)} 成功导入给对应玩家!", color2);
        }
        catch (Exception ex)
        {
            TShock.Log.ConsoleError($"读取过程错误:{ex}");
        }
    }
    #endregion

    #region 导入所有存档给对应玩家,存在账号则覆盖，不在则创建账号
    public static void ReadPlayer(TSPlayer plr)
    {
        string[] files = Directory.GetFiles(ReaderPlrDir);

        if (files.Count() == 0)
        {
            plr.SendMessage("导入存档文件夹为空", color2);
            plr.SendMessage($"请放入【.plr文件】再使用指令:\n{ReaderPlrDir}", color);
            return;
        }

        var index = 1;
        foreach (var file in files)
        {
            try
            {
                ReadPlayerCopyCharacter(file);
                plr.SendSuccessMessage($"{index++}.{Path.GetFileName(file)} 已成功导入数据库!");
            }
            catch (Exception ex)
            {
                TShock.Log.ConsoleError($"读取过程错误:{ex}");
            }
        }
    }
    #endregion

    #region 把存档复制到数据库（如果没有对应的数据则创建账号）
    private static void ReadPlayerCopyCharacter(string path, string? name = null)
    {
        PlayerFileData data = Player.LoadPlayer(path, false);
        if (!string.IsNullOrEmpty(name))
        {
            data.Player.name = name;
        }

        TSPlayer plr = new TSPlayer(byte.MaxValue - 1);

        typeof(TSPlayer).GetField("FakePlayer", 
            System.Reflection.BindingFlags.NonPublic |
            System.Reflection.BindingFlags.Default | 
            System.Reflection.BindingFlags.Instance)?.
            SetValue(plr, data.Player);

        plr.Account = GetOrGenerateAccount(data.Player);
        plr.PlayerData = TShock.CharacterDB.GetPlayerData(plr, plr.Account.ID);
        plr.IsLoggedIn = true;
        plr.State = 10;

        plr.PlayerData.CopyCharacter(plr); //保存数据
        TShock.CharacterDB.InsertPlayerData(plr);

        try
        {
            plr.PlayerData.RestoreCharacter(plr);
        }
        catch { }
    }
    #endregion

    #region 获取与创建账号(说白了就是自动注册那套写法,uuid从发指令者里拿,密码上个哈希避免登录时改不了密码)
    public static string newUUID = string.Empty;
    private static UserAccount GetOrGenerateAccount(Player plr)
    {
        var ac = TShock.UserAccounts.GetUserAccountByName(plr.name);
        if (ac != null)
        {
            return ac;
        }

        // 自动注册流程（适配Caibot）
        if (!ServerApi.Plugins.Any(p => p.Plugin.Name == "CaiBotLitePlugin"))
        {
            var group = TShock.Config.Settings.DefaultRegistrationGroupName;
            var NewAcc = new UserAccount(plr.name, Config.DefPass, newUUID, group,
                                         DateTime.UtcNow.ToString("s"),
                                         DateTime.UtcNow.ToString("s"), "");
            try
            {
                NewAcc.CreateBCryptHash(Config.DefPass);
                TShock.UserAccounts.AddUserAccount(NewAcc);
                TShock.Log.ConsoleInfo($"已为{plr.name}创建账号,密码为:{Config.DefPass}");
            }
            finally
            {
                // 建完账号清空UUID缓存
                newUUID = string.Empty;
            }

            return TShock.UserAccounts.GetUserAccountByName(plr.name);
        }

        return null;
    }
    #endregion

    #region 显示导入存档文件夹里的文件（用于/pout plr r预览用）
    public static void ShowPlrFile(TSPlayer plr)
    {
        // 获取源文件
        var srcFiles = Directory.GetFiles(ReaderPlrDir, "*", SearchOption.TopDirectoryOnly);
        if (srcFiles.Length == 0)
        {
            plr.SendMessage($"导入存档文件夹中没有文件", color);
            plr.SendMessage($"文件夹路径: {ReaderPlrDir}", color);
            plr.SendMessage($"请将.plr放入此文件夹后重试", color);
            return;
        }

        // 构建文件列表
        var fileList = new StringBuilder();
        fileList.AppendLine($"{ReaderPlrDir}:");
        fileList.AppendLine($"找到 {srcFiles.Length} 个文件:");

        for (int i = 0; i < srcFiles.Length; i++)
        {
            string fileName = Path.GetFileName(srcFiles[i]);
            long fileSize = new FileInfo(srcFiles[i]).Length;
            string sizeText = fileSize < 1024 ? $"{fileSize} B" :
                            fileSize < 1024 * 1024 ? $"{fileSize / 1024} KB" :
                            $"{fileSize / (1024 * 1024)} MB";

            fileList.AppendLine($"{i + 1}. {fileName} ({sizeText})");
        }

        if (plr.RealPlayer)
            plr.SendMessage(TextGradient(fileList.ToString()), color);
        else
            TShock.Log.ConsoleInfo(fileList.ToString());
    }
    #endregion
}
