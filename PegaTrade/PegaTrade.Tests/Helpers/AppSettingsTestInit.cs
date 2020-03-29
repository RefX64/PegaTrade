using System;
using System.Collections.Generic;
using System.Text;
using PegaTrade.Core.StaticLogic;

namespace PegaTrade.Tests.Helpers
{
    public static class AppSettingsTestInit
    {
        public static void Initialize()
        {
            AppSettingsProvider.PegasunDBConnectionString = "xxx";
            AppSettingsProvider.IsDevelopment = true;
            AppSettingsProvider.PegasunAPIUrl = "xxx";
        }
    }
}
