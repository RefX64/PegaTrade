using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Text;

namespace PegaTrade.Core.StaticLogic
{
    public static class AppSettingsProvider
    {
        public static string PegasunDBConnectionString { get; set; }
        public static bool IsDevelopment { get; set; }
        public static string PegasunAPIUrl { get; set; }
    }
}
