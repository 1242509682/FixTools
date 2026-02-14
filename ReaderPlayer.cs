using System.Text;
using Terraria;
using Terraria.IO;
using TerrariaApi.Server;
using TShockAPI;
using TShockAPI.DB;
using static FixTools.FixTools;
using static FixTools.Utils;

namespace FixTools;

public class ReaderPlayer
{
    public static readonly string ReaderDir = Path.Combine(MainPath, "导入存档"); // 导入角色路径

    #region 通过plr的文件索引导入存档给对应玩家,如果指定名字则导入给指定玩家
    public static void ReadPlayerByIndex(TSPlayer plr, int idx, string? name = null)
    {
        try
        {
            // 获取所有.plr文件
            string[] files = Directory.GetFiles(ReaderDir, "*.plr");
            if (files.Length == 0)
            {
                plr.SendMessage("导入存档文件夹中没有.plr文件", color);
                return;
            }

            // 检查索引是否有效
            if (idx < 1 || idx > files.Length)
            {
                plr.SendMessage($"索引 {idx} 无效，有效范围: 1-{files.Length}", color);
                ShowPlrFile(plr);
                return;
            }

            // 获取选中的文件
            string file = files[idx - 1];
            if (string.IsNullOrEmpty(name))
                ReadPlayer(plr, file);
            else
                ReadPlayer(plr, name, file);
        }
        catch (Exception ex)
        {
            TShock.Log.ConsoleError($"按索引导入存档错误:{ex}");
            plr.SendErrorMessage("导入失败，请查看控制台错误");
        }
    }
    #endregion

    #region 显示导入存档文件夹里的文件（用于/pout plr r预览用）
    public static void ShowPlrFile(TSPlayer plr)
    {
        // 获取源文件，只显示.plr文件
        var srcFiles = Directory.GetFiles(ReaderDir, "*.plr");
        if (srcFiles.Length == 0)
        {
            plr.SendMessage($"导入存档文件夹中没有.plr文件", color);
            plr.SendMessage($"文件夹路径: {ReaderDir}", color);
            plr.SendMessage($"请将.plr放入此文件夹后重试", color);
            return;
        }

        // 构建文件列表
        var fileList = new StringBuilder();
        fileList.AppendLine($"{ReaderDir}:");
        fileList.AppendLine($"找到 {srcFiles.Length} 个存档文件:");

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

    #region 把存档导入同名的对应玩家
    public static void ReadPlayer(TSPlayer plr, string name, string file)
    {
        if (!File.Exists(file))
        {
            plr.SendMessage($"存档文件 {Path.GetFileName(file)} 不存在无法导入!", color);
            ShowPlrFile(plr);
            return;
        }

        try
        {
            var plr2 = TShock.Players.FirstOrDefault(p => p != null && p.Name == name);
            if (plr2 != null)
            {
                // 玩家在线，恢复物品
                ReadCopy(file, name, plr2);
                plr.SendSuccessMessage($"{Path.GetFileName(file)} 已成功导入给在线玩家 {name}，并立即恢复物品!");
            }
            else
            {
                // 玩家不在线，只保存到数据库
                ReadCopy(file, name);
                plr.SendSuccessMessage($"{Path.GetFileName(file)} 成功导入给指定玩家 {name}!");
            }
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
            var fName = Path.GetFileNameWithoutExtension(file);
            var plr2 = TShock.Players.FirstOrDefault(p => p != null && p.Name == fName);

            if (plr2 != null)
            {
                // 玩家在线，恢复物品
                ReadCopy(file, null, plr2);
                plr.SendMessage($"{Path.GetFileName(file)} 已成功导入给在线玩家 {fName}，并立即恢复物品!", color2);
            }
            else
            {
                // 玩家不在线，只保存到数据库
                ReadCopy(file);
                plr.SendMessage($"{Path.GetFileName(file)} 成功导入给对应玩家!", color2);
            }
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
        string[] files = Directory.GetFiles(ReaderDir);

        if (files.Count() == 0)
        {
            plr.SendMessage("导入存档文件夹为空", color2);
            plr.SendMessage($"请放入【.plr文件】再使用指令:\n{ReaderDir}", color);
            return;
        }

        var idx = 1;
        foreach (var file in files)
        {
            try
            {

                var fName = Path.GetFileNameWithoutExtension(file);
                var plr2 = TShock.Players.FirstOrDefault(p => p != null && p.Name == fName);

                if (plr2 != null)
                {
                    // 玩家在线，恢复物品
                    ReadCopy(file, null, plr2);
                    plr.SendSuccessMessage($"{idx++}.{Path.GetFileName(file)} 已成功导入给在线玩家 {fName}!");
                }
                else
                {
                    // 玩家不在线，只保存到数据库
                    ReadCopy(file);
                    plr.SendSuccessMessage($"{idx++}.{Path.GetFileName(file)} 已成功导入数据库!");
                }
            }
            catch (Exception ex)
            {
                TShock.Log.ConsoleError($"读取过程错误:{ex}");
            }
        }
    }
    #endregion

    #region 把存档复制到数据库（如果没有对应的数据则创建账号）
    private static void ReadCopy(string path, string? name = null, TSPlayer? plr2 = null)
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

        plr.Account = GetOrGenerateAccount(data.Player)!;
        plr.PlayerData = TShock.CharacterDB.GetPlayerData(plr, plr.Account.ID);
        plr.IsLoggedIn = true;
        plr.State = 10;

        //保存到数据库
        plr.PlayerData.CopyCharacter(plr);
        TShock.CharacterDB.InsertPlayerData(plr);

        // 如果玩家在线，恢复物品
        if (plr2 != null && plr2.Active && plr2.IsLoggedIn)
        {
            try
            {
                // 玩家死亡，将数据存入字典等待复活后恢复(离开服务器也能恢复)
                // 因为死亡时恢复会导致玩家数据错乱，直接变成四分五裂的拼小人
                if (plr2.Dead)
                {
                    var data2 = new PlayerData(false);
                    CopyData(data.Player, data2);
                    PlayerState.GetData(plr2.Name).NeedRestores = data2;
                    plr2.SendMessage($"检测到你已死亡，将在复活后恢复存档物品!", color);
                    return;
                }

                // 创建PlayerData对象并复制数据，玩家活着立即恢复存档
                var data3 = new PlayerData(false);
                CopyData(data.Player, data3);
                // 直接使用保存的PlayerData恢复
                data3.RestoreCharacter(plr2);

                // 发送消息
                plr2.SendMessage($"管理员已为你恢复存档物品!", color);
            }
            catch (Exception ex)
            {
                TShock.Log.ConsoleError($"恢复在线玩家物品失败: {ex}");
            }
        }
    }

    // 从Player复制到PlayerData
    private static void CopyData(Player d1plr, PlayerData d2)
    {
        var fake = new TSPlayer(byte.MaxValue - 1);
        typeof(TSPlayer).GetField("FakePlayer",
            System.Reflection.BindingFlags.NonPublic |
            System.Reflection.BindingFlags.Instance)?.
            SetValue(fake, d1plr);

        // 复制数据
        d2.CopyCharacter(fake);
    }
    #endregion

    #region 获取与创建账号(自动注册方法)
    public static string newUUID = string.Empty;
    private static UserAccount? GetOrGenerateAccount(Player plr)
    {
        var ac = TShock.UserAccounts.GetUserAccountByName(plr.name);
        if (ac != null)
        {
            return ac;
        }

        // 自动注册流程（适配Caibot）
        if (!ServerApi.Plugins.Any(p => p.Plugin.Name == "CaiBotLitePlugin"))
        {
            // uuid从发指令的人身上拿 反正登录后uuid也会更新
            var group = TShock.Config.Settings.DefaultRegistrationGroupName;
            var NewAcc = new UserAccount(plr.name, Config.DefPass, newUUID, group,
                                         DateTime.UtcNow.ToString("s"),
                                         DateTime.UtcNow.ToString("s"), "");
            try
            {
                // 密码上个哈希避免登录时改不了密码
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
}
