﻿using System;
using EthereumSamurai.Core.Services;
using EthereumSamurai.Core.Settings;
using EthereumSamurai.MongoDb;
using EthereumSamurai.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Common.Log;

namespace EthereumSamurai.Common
{
    public static class Configuration
    {
        public static IServiceCollection ConfigureRepositories(this IServiceCollection collection)
        {
            collection.RegisterRepositories();

            return collection;
        }

        public static IServiceCollection ConfigureServices(
            this IServiceCollection collection,
                 IConfigurationRoot configuration)
        {
            collection.ConfigureLogging();
            collection.GetSettings(configuration);

            collection.ConfigureRepositories();
            collection.ConfigureServices();

            return collection;
        }

        public static IServiceCollection ConfigureServices(this IServiceCollection collection)
        {
            collection.RegisterServices();
            return collection;
        }

        public static IServiceCollection GetSettings(
            this IServiceCollection collection,
                 IConfigurationRoot configuration)
        {
            var connectionString = configuration.GetConnectionString("ConnectionString");

            SettingsWrapper settings;
            if (!string.IsNullOrEmpty(connectionString))
            {
                settings = GeneralSettingsReader.ReadGeneralSettings<SettingsWrapper>(connectionString);
            }
            else
            {
                IConfigurationSection indexerSettingsSection = configuration.GetSection("SettingsWrapper");
                settings = indexerSettingsSection.Get<SettingsWrapper>();
            }

            if (settings == null)
            {
                throw new Exception("Please, provide connection string or env settings");
            }
            
            collection.AddSingleton<IBaseSettings>(settings.EthereumIndexer);

            return collection;
        }

        private static IServiceCollection ConfigureLogging(this IServiceCollection collection)
        {
            collection.AddSingleton<ILog, LogToConsole>();

            return collection;
        }
    }
}