using Newtonsoft.Json;
using Relay.BulkSenderService.Classes;
using Relay.BulkSenderService.Configuration;
using System;
using System.Collections.Generic;
using System.IO;

namespace Relay.BulkSenderService.Processors
{
    public abstract class BaseWorker
    {
        protected List<IUserConfiguration> _users;
        protected readonly ILog _logger;
        protected readonly IConfiguration _configuration;
        protected readonly IWatcher _watcher;
        private object _lockConfigChanges = new object();
        private bool _configChanges = false;

        public BaseWorker(ILog logger, IConfiguration configuration, IWatcher watcher)
        {
            _logger = logger;
            _configuration = configuration;
            _watcher = watcher;
            _users = LoadUsers();

            ((FileCommandsWatcher)watcher).ChangeConfigurationEvent += BaseWorker_ChangeConfigurationEvent;
        }

        private void BaseWorker_ChangeConfigurationEvent(object sender, EventArgs e)
        {
            _logger.Debug($"There are changes on configuration.");
            lock (_lockConfigChanges)
            {
                _configChanges = true;
            }
        }

        protected bool CheckConfigChanges()
        {
            lock (_lockConfigChanges)
            {
                if (_configChanges)
                {
                    _configChanges = false;
                    _users = LoadUsers();
                    return true;
                }

                return false;
            }
        }

        protected List<IUserConfiguration> LoadUsers()
        {
            var userList = new List<IUserConfiguration>();
            var jsonSerializerSettings = new JsonSerializerSettings()
            {
                TypeNameHandling = TypeNameHandling.Auto
            };

            string configFilePath = $"{AppDomain.CurrentDomain.BaseDirectory}configs";

            //TO LOCAL TEST
            //string configFilePath = $"{AppDomain.CurrentDomain.BaseDirectory}configs\\test";

            try
            {
                string[] configFiles = Directory.GetFiles(configFilePath);

                foreach (string configFile in configFiles)
                {
                    string fileName = Path.GetFileName(configFile);

                    if (fileName.EndsWith("config.json"))
                    {
                        string fileNamePath = $@"{configFilePath}\{fileName}";

                        string jsonString = File.ReadAllText(fileNamePath);

                        IUserConfiguration userConfiguration = JsonConvert.DeserializeObject<IUserConfiguration>(jsonString, jsonSerializerSettings);

                        userList.Add(userConfiguration);
                    }
                }
            }
            catch (Exception e)
            {
                _logger.Error($"Error trying to load json configuration {e}");
            }

            return userList;
        }
    }
}
