using Newtonsoft.Json;

namespace ReleaseBadge
{
    /// <summary>
    /// Helper class that abstracts access to the Deployment completed event.
    /// </summary>
    internal class DeploymentCompletedEventHelper
    {
        private dynamic data;

        public string Id
        {
            get { return (string)data.id; }
        }

        public string Status
        {
            get { return (string)data.resource.environment.status; }
        }

        public string EnvironmentName
        {
            get { return (string)data.resource.environment.name; }
        }

        public string ReleaseDefinitionName
        {
            get { return (string)data.resource.environment.releaseDefinition.name; }
        }
        public string ReleaseName
        {
            get { return (string)data.resource.environment.release.name; }
        }

        /// <summary>
        /// ctor. Receives the JSON content of the event
        /// 
        /// Only works with deployment completed events
        /// </summary>
        /// <param name="jsonContent"></param>
        public DeploymentCompletedEventHelper(string jsonContent)
        {
            data = JsonConvert.DeserializeObject(jsonContent);
        }

        /// <summary>
        /// is the event valid?
        /// </summary>
        /// <returns>true if it is, false otherwise</returns>
        public bool IsValidEvent()
        {
            return data != null && data.id != null && data.eventType != "ms.vss-release.deployment-completed-event";
        }

        /// <summary>
        /// Gets the color based on the release status
        /// 
        /// Return either green, yellow or red
        /// </summary>
        /// <returns></returns>
        public string GetColor()
        {
            var status = (string)data.resource.environment.status;

            switch (status)
            {
                case "succeeded":
                    return "green";
                case "partiallySucceeded":
                    return "yellow";
                case "failed":
                default:
                    return "red";
            }
        }

        /// <summary>
        ///  The release identifier is composed of the them project guid plus the release definition id
        /// </summary>
        /// <returns></returns>
        internal string GetReleaseIdentifier()
        {
            return data.resource.environment.releaseDefinition.id;
        }

        internal string GetTeamProjectName()
        {
            return data.resource.project.id;
        }

        internal string GetReleaseName()
        {
            return data.resource.environment.releaseDefinition.name;
        }
    }
}
