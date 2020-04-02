using log4net.Config;
using System;
using System.Collections.Generic;
using System.IO;

namespace Relay.BulkSenderService.Classes
{
    public class LogManager : ILogManager
    {
        /// <summary>
        /// Allows to override configuration file path in order to work fine with ASP.NET 5
        /// </summary>
        public static string ConfigurationPath { get; set; }

        static bool _configured = false;
        static object _threadSafeObject = new object();
        static Dictionary<string, Logger> _loggers = new Dictionary<string, Logger>();
        public void Configure()
        {
            if (!_configured)
            {
                Configure(ConfigurationPath ?? AppDomain.CurrentDomain.GetData("APP_CONFIG_FILE").ToString());
                _configured = true;
            }
        }

        public static void Configure(string configFile)
        {
            Stream stream = new FileStream(configFile, FileMode.Open, FileAccess.Read, FileShare.Read);
            XmlConfigurator.Configure(stream);
            stream.Close();
        }

        public static ILog GetLoggerStatic(string name)
        {
            Logger logger = null;
            lock (_threadSafeObject)
            {
                if (!_configured)
                {
                    XmlConfigurator.Configure();
                    _configured = true;
                }

                if (_loggers.ContainsKey(name))
                    logger = _loggers[name];
                else
                {
                    logger = new Logger(name);
                    _loggers.Add(name, logger);
                }
            }
            return logger;
        }

        public ILog GetLogger(string name)
        {
            Configure();
            return GetLoggerStatic(name);
        }
    }

    public class Logger : ILog
    {
        log4net.ILog _log = null;

        public bool IsDebugEnabled => _log.IsDebugEnabled;

        public bool IsInfoEnabled => _log.IsInfoEnabled;

        public bool IsWarnEnabled => _log.IsWarnEnabled;

        public bool IsErrorEnabled => _log.IsErrorEnabled;

        public bool IsFatalEnabled => _log.IsFatalEnabled;

        public Logger(string name)
        {
            _log = log4net.LogManager.GetLogger(name);
        }
        public void Debug(string message)
        {
            _log.Debug(message);
        }

        public void Debug(string message, Exception exception)
        {
            _log.Debug(message, exception);
        }

        public void Debug(string message, params object[] args)
        {
            _log.DebugFormat(message, args);
        }

        public void Warn(string message)
        {
            _log.Warn(message);
        }

        public void Warn(string message, Exception exception)
        {
            _log.Warn(message, exception);
        }

        public void Warn(string message, params object[] args)
        {
            _log.WarnFormat(message, args);
        }

        public void Error(string message)
        {
            _log.Error(message);
        }

        public void Error(string message, Exception exception)
        {
            _log.Error(message, exception);
        }

        public void Error(string message, params object[] args)
        {
            _log.ErrorFormat(message, args);
        }

        public void Fatal(string message)
        {
            _log.Fatal(message);
        }

        public void Fatal(string message, Exception exception)
        {
            _log.Fatal(message, exception);
        }

        public void Fatal(string message, params object[] args)
        {
            _log.FatalFormat(message, args);
        }

        public void Info(string message)
        {
            _log.Info(message);
        }

        public void Info(string message, Exception exception)
        {
            _log.Info(message, exception);
        }

        public void Info(string message, params object[] args)
        {
            _log.InfoFormat(message, args);
        }
    }
}
