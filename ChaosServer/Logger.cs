using Microsoft.Extensions.Logging;

namespace ChaosServer;

enum LogTarget
{
    Console,
    File,
    Both
}
public class Logger
{
    private readonly ILogger _mLogger;
    private readonly LogTarget _mLogTarget = LogTarget.Both;
    private readonly Type _mType;
    
    private readonly StreamWriter _mStreamWriter;
    private static readonly FileStream _mFileStream = new FileStream(ServerConstant.APP_PATH + "\\server_log.txt", 
        FileMode.Append, FileAccess.Write, FileShare.Write);

    public Logger(Type type)
    {
        _mType = type;
        
        ILoggerFactory factory = LoggerFactory.Create(builder => builder.AddConsole());
        _mLogger = factory.CreateLogger(type);

        // string path = ServerConstant.APP_PATH + "\\server_log.txt";
        // FileStream fileStream = new FileStream(path, FileMode.OpenOrCreate, FileAccess.Write, FileShare.Write);
        _mStreamWriter = new StreamWriter(_mFileStream);
        _mStreamWriter.AutoFlush = true;
    }

    public void LogInfo(string message)
    {
        _mLogger.LogInformation(message);
        if (_mLogTarget is LogTarget.File or LogTarget.Both)
        {
            string msg = $"{DateTime.Now} [Info]: {_mType.Name}: {message}";
            _mStreamWriter.WriteLine(msg);
        }
    }

    public void LogWarning(string message)
    {
        _mLogger.LogWarning(message);
        if (_mLogTarget is LogTarget.File or LogTarget.Both)
        {
            string msg = $"{DateTime.Now} [Warning]: {_mType.Name}: {message}";
            _mStreamWriter.WriteLine(msg);
        }
    }

    public void LogError(string message)
    {
        _mLogger.LogError(message);
        if (_mLogTarget is LogTarget.File or LogTarget.Both)
        {
            string msg = $"{DateTime.Now} [Error]: {_mType.Name}: {message}";
            _mStreamWriter.WriteLine(msg);
        }
    }

    public void LogCritical(string message)
    {
        _mLogger.LogCritical(message);
        if (_mLogTarget is LogTarget.File or LogTarget.Both)
        {
            string msg = $"{DateTime.Now} [Critical]: {_mType.Name}: {message}";
            _mStreamWriter.WriteLine(msg);
        }
    }

    public void LogDebug(string message)
    {
        _mLogger.LogDebug(message);
        if (_mLogTarget is LogTarget.File or LogTarget.Both)
        {
            string msg = $"{DateTime.Now} [Debug]: {_mType.Name}: {message}";
            _mStreamWriter.WriteLine(msg);
        }
    }
}