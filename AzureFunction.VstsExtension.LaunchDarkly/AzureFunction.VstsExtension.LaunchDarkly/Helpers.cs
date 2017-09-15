using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace AzureFunction.VstsExtension.LaunchDarkly
{
    public class Helpers
    {
        public static string GetEnvironmentVariable(string name)
        {
            return System.Environment.GetEnvironmentVariable(name, EnvironmentVariableTarget.Process);
        }
    }
}