using ChaosBall.Utility;
using ChaosServer.Model;
using MySql.Data.MySqlClient;
using SocketProtocol;

namespace ChaosServer.Dao {
    public class UserService
    {
        private readonly Logger _mLogger;
        
        private readonly string _mConnectString = "Database=chaos_ball;Data Source=127.0.0.1;" +
            "Port=3306;User Id=root;Password=200110180919;Charset=utf8;";

        private MySqlConnection _mConnection;

        public UserService()
        {
            _mLogger = new Logger(GetType());
            ConnectMysql();
        }

        private void ConnectMysql() {
            try {
                _mConnection = new MySqlConnection(_mConnectString);
                _mConnection.Open();
                _mLogger.LogInfo("Database connected!");
            } catch (Exception) {
                _mLogger.LogWarning("Database connect failed!");
                throw;
            }
        }
        
        /// <summary>
        /// 登录验证
        /// </summary>
        public ReturnData<PlayerInfo> SignIn(SignInPack signInPack) {
            try {
                string username = signInPack.Username;
                string password = signInPack.Password;

                string sql = $"select * from `user_data` where user_name='{username}' and password='{password}'";
                MySqlCommand command = new MySqlCommand(sql, _mConnection);

                using MySqlDataReader reader = command.ExecuteReader();
                ReturnData<PlayerInfo> returnData;
                if (!reader.Read())
                {
                    returnData = new ReturnData<PlayerInfo>()
                    {
                        success = false,
                        errorMessage = CharsetUtil.DefaultToUTF8("用户名或密码错误!"),
                    };
                    
                    _mLogger.LogWarning($"用户名或密码错误");
                    return returnData;
                }

                string nickName = reader.GetString("nick_name");
                PlayerInfo playerInfo = new PlayerInfo
                {
                    id = reader.GetInt32("id"),
                    username = CharsetUtil.DefaultToUTF8(username),
                    nickname = CharsetUtil.DefaultToUTF8(nickName),
                };

                _mLogger.LogInfo("登录成功!");

                returnData = new ReturnData<PlayerInfo>
                {
                    success = true,
                    successMessage = CharsetUtil.DefaultToUTF8("登录成功!"),
                    data = playerInfo
                };

                return returnData;
                
            } catch (Exception ex) {
                _mLogger.LogWarning("登录校验错误：" + ex);
                ReturnData<PlayerInfo> returnData = new ReturnData<PlayerInfo>
                {
                    success = false,
                    errorMessage = CharsetUtil.DefaultToUTF8(ex.Message),
                };
                return returnData;
            }
        }

        /// <summary>
        /// 注册实现
        /// </summary>
        public ReturnData<PlayerInfo> SignUp(SignUpPack pack) {
            try {
                string username = pack.Username;
                string password = pack.Password;
                string nickName = pack.Nickname;

                string sql = $"select `user_name` from `user_data` where `user_name`='{username}'";
                MySqlCommand command = new MySqlCommand(sql, _mConnection);
                ReturnData<PlayerInfo> returnData;
                using (MySqlDataReader reader = command.ExecuteReader()) {
                    if (reader.Read()) {
                        _mLogger.LogWarning("注册失败已存在该用户名");
                        returnData = new ReturnData<PlayerInfo>
                        {
                            success = false,
                            errorMessage = CharsetUtil.DefaultToUTF8("存在该用户名!")
                        };
                        return returnData;
                    }
                }

                sql = $"insert into `user_data`(`user_name`, `nick_name`, `password`) values('{username}','{nickName}','{password}')";
                command = new MySqlCommand(sql, _mConnection);

                int effectedRows = command.ExecuteNonQuery();
                if (effectedRows > 0)
                {
                    // command.
                    sql = $"select `id` from `user_data` where `user_name`='{username}'";
                    command = new MySqlCommand(sql, _mConnection);
                    using MySqlDataReader reader = command.ExecuteReader();
                    if (reader.Read())
                    {
                        int id = reader.GetInt32("id");

                        PlayerInfo playerInfo = new PlayerInfo
                        {
                            id = id,
                            username = username,
                            nickname = nickName
                        };

                        returnData = new ReturnData<PlayerInfo>
                        {
                            success = true,
                            successMessage = CharsetUtil.DefaultToUTF8("注册成功!"),
                            data = playerInfo
                        };
                        return returnData;
                    } 
                }
                
                returnData = new ReturnData<PlayerInfo>
                {
                    success = false,
                    errorMessage = CharsetUtil.DefaultToUTF8("注册失败!"),
                };

                return returnData;
            } catch (Exception ex) {
                _mLogger.LogWarning("注册校验错误：" + ex);
                ReturnData<PlayerInfo> returnData = new ReturnData<PlayerInfo>
                {
                    success = false,
                    errorMessage = ex.Message,
                };
                return returnData;
            }
        }
    }
}
