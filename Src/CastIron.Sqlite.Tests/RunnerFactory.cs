﻿using System;
using System.IO;
using System.Runtime.InteropServices;
using CastIron.Sql;
using Microsoft.Extensions.Configuration;

namespace CastIron.Sqlite.Tests
{
    public static class RunnerFactory
    {
        private static readonly IConfigurationRoot _configuration;

        static RunnerFactory()
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory());

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                builder = builder.AddJsonFile("appsettings.linux.json");
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                builder = builder.AddJsonFile("appsettings.windows.json");

            _configuration = builder.Build();
        }

        public static ISqlRunner Create(Action<IContextBuilder> defaultBuilder = null)
        {
            return Sqlite.RunnerFactory.Create(_configuration["SQLITE"], null, null, defaultBuilder);
        }
    }
}
