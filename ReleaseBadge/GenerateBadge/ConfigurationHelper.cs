using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace ReleaseBadge
{
    /// <summary>
    /// Helper to get configuration value.
    /// 
    /// The value is fetched from HTTP headers and if not defined it is fetched from application settings.
    /// </summary>
    internal class ConfigurationHelper
    {
        private HttpRequestMessage req;

        public ConfigurationHelper(HttpRequestMessage req)
        {
            this.req = req;
        }

        /// <summary>
        /// Predicate to check if badges should be generated for any status.
        /// 
        /// If disable only sucessfull deploys generated a badge and other status are ignored.
        /// </summary>
        /// <returns></returns>
        internal bool EnabledAllStatus()
        {
            return GetConfigurationValue("EnableForAllStatus", "false").ToLower() == "true"; 
        }

        /// <summary>
        /// Get badge style
        /// </summary>
        /// <returns></returns>
        internal string GetStyle()
        {
            return GetConfigurationValue("Style", null);
        }

        /// <summary>
        /// Get badge file type (png,gif,svg,...)
        /// 
        /// Default value is PNG
        /// </summary>
        /// <returns></returns>
        internal string GetFileType()
        {
            return GetConfigurationValue("FileType", "png");
        }

        /// <summary>
        /// Gets release definition friendly name. Use an alternate name for the badge release definition name
         /// </summary>
        /// <returns></returns>
        internal string GetReleaseDefinitionFriendlyName()
        {
            if (req.Headers.Contains("X-ReleaseDefinitionFileFriendlyName"))
            {
                return req.Headers.GetValues("X-ReleaseDefinitionFileFriendlyName").FirstOrDefault();
            }

            return null;
        }

        /// <summary>
        /// Gets badge cache (seconds) duration
        /// </summary>
        /// <returns></returns>
        internal string GetMaxAge()
        {
            return GetConfigurationValue("MaxAge", "15");
        }

        /// <summary>
        /// Predicate to check if we should use release name to generate the badge filename
        /// 
        /// default value: true
        /// </summary>
        /// <returns></returns>
        internal bool UseReleaseName()
        {
            return GetConfigurationValue("UseReleaseName", "false").ToLower() == "true";
        }

        /// <summary>
        /// Gets a configuration value from the HTTP headers and if not defined fetched it
        /// from application settings.
        /// 
        /// On HTTP header the header should be X-{settingName}
        /// </summary>
        /// <param name="settingName">the name of the setting</param>
        /// <param name="defaultValue">The default value to return if no configuration value is found</param>
        /// <returns>configured value or default value if not defined</returns>
        internal string GetConfigurationValue(string settingName, string defaultValue)
        {
            var headerName = "X-" + settingName;

            if (req.Headers.Contains(headerName))
            {
                return req.Headers.GetValues(headerName).FirstOrDefault();
            }

            return GetApplicationSetting(settingName) ?? defaultValue;
        }

        /// <summary>
        /// Gets an azure function application setting value
        /// </summary>
        /// <param name="settingName"></param>
        /// <returns></returns>
        internal static string GetApplicationSetting(string settingName)
        {
            return System.Environment.GetEnvironmentVariable(settingName);
        }
    }
}
