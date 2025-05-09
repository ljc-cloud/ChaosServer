using Google.Protobuf;
using SocketProtocol;

namespace ChaosServer.Net {
    public class Message
    {

        private readonly Logger _mLogger;
        
        /// <summary>
        /// 消息头长度
        /// </summary>
        public const int MESSAGE_HEADER_LEN = 4;

        private byte[] _mBuffer;
        private int _mMessageLen;

        public Message()
        {
            _mLogger = new Logger(GetType());
            _mBuffer = new byte[1024];
            _mMessageLen = 0;
        }

        public byte[] Buffer => _mBuffer;
        public int MessageLen => _mMessageLen;
        public int RemainSize => _mBuffer.Length - _mMessageLen;

        /// <summary>
        /// 解析从对端发送来的消息
        /// </summary>
        /// <param name="len">消息长度</param>
        public void ReadBuffer(int len, Action<MainPack> onMainPackDeserialize)
        {
            _mMessageLen += len;
            // 消息长度 <= 4，说明这个消息只有消息头
            if (len <= MESSAGE_HEADER_LEN)
            {
                return;
            }

            // 将字节数组中前4个字节（从startIndex开始,第二个参数）转换为 int，刚好是消息头大小，存储的是消息体长度
            int bodyLen = BitConverter.ToInt32(_mBuffer, 0);
            while (true)
            {
                if (_mMessageLen >= bodyLen + MESSAGE_HEADER_LEN)
                {
                    try
                    {
                        //MainPack pack = (MainPack)MainPack.Descriptor.Parser.ParseFrom(_mBuffer, MESSAGE_HEADER_LEN, bodyLen);
                        
                        MainPack pack = MainPack.Parser.ParseFrom(_mBuffer, MESSAGE_HEADER_LEN, bodyLen);
                        onMainPackDeserialize?.Invoke(pack);
                        System.Buffer.BlockCopy(_mBuffer, bodyLen + MESSAGE_HEADER_LEN, _mBuffer, 0,
                            _mMessageLen - (bodyLen + MESSAGE_HEADER_LEN));
                        _mMessageLen -= (bodyLen + MESSAGE_HEADER_LEN);
                    }
                    catch (Exception e)
                    {
                        _mLogger.LogError($"Tcp message serialize error: {e}");
                        throw;
                    }
                }
                else
                {
                    // 清空消息解析缓冲区
                    _mMessageLen = 0;
                    Array.Fill<byte>(_mBuffer, 0);
                    break;
                }
            }
        }

        public static byte[] GetPackData(MainPack pack)
        {
            byte[] body = pack.ToByteArray();
            byte[] head = BitConverter.GetBytes(body.Length);
            return head.Concat(body).ToArray();
        }
    }
}
