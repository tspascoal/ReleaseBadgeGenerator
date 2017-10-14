using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.WindowsAzure.Storage.Blob;

using ReleaseBadge.ShieldIO;

namespace ReleaseBadge
{
    public static class GetBadge
    {
        /// <summary>
        /// Generates a badge for a given release.
        /// 
        /// The badge is generated when the completed deployment event is received.
        /// 
        /// It generates a badge with the name of the environment and the name of the current release.
        /// 
        /// It has a different color based on the status of the deploy.
        /// 
        /// The badge is stored in a azure blob so we can be easily (and cheaply) accessed from anywhere.
        /// 
        /// By default it only created a badge for successfull deploys. Pass the paramter X-EnableForAllStatus with value true to generate
        /// a badge for any status.
        /// </summary>
        /// <param name="req"></param>
        /// <param name="binder"></param>
        /// <param name="log"></param>
        /// <returns></returns>
        [FunctionName("GenerateBadge")]
        public static async Task<object> Run([HttpTrigger(WebHookType = "genericJson")]HttpRequestMessage req, Binder binder, TraceWriter log)
        {
            string badgeFileName = null;

            log.Info($"Webhook was triggered!");

            var eventHelper = new DeploymentCompletedEventHelper(await req.Content.ReadAsStringAsync());
            var parameterHelper = new ConfigurationHelper(req);

            if (eventHelper.IsValidEvent())
            {
                return req.CreateResponse(HttpStatusCode.BadRequest, new { error = "invalid event" });
            }

            log.Info($"{eventHelper.Id} with status {eventHelper.Status} for environment {eventHelper.EnvironmentName}");

            if (eventHelper.Status != "succeeded" && parameterHelper.EnabledAllStatus() == false)
            {
                return req.CreateResponse(HttpStatusCode.OK, new { result = $"status {eventHelper.Status} ignored for for {eventHelper.Id}" });
            }

            string releaseIdentifier = GetReleaseIdentifier(parameterHelper, eventHelper);

            badgeFileName = string.Format("{0}/{1}-{2}.{3}", eventHelper.GetTeamProjectName(), releaseIdentifier, eventHelper.EnvironmentName, parameterHelper.GetFileType());

            log.Info($"going to generate badge with name {badgeFileName}");

            var blobUri = await WriteBadgeToStorage(eventHelper, parameterHelper, binder, badgeFileName);

            log.Info($"badge stored on {blobUri}");

            return req.CreateResponse(HttpStatusCode.OK, new { result = $"Generated {badgeFileName} for {eventHelper.Id} on {blobUri}" });
        }

        private static string GetReleaseIdentifier(ConfigurationHelper parameterHelper, DeploymentCompletedEventHelper eventHelper)
        {
            var releaseFriendlyName = parameterHelper.GetReleaseDefinitionFriendlyName();

            if (releaseFriendlyName != null)
            {
                return releaseFriendlyName;
            }

            if (parameterHelper.UseReleaseName())
            {
                return eventHelper.GetReleaseName();
            }

            return eventHelper.GetReleaseIdentifier();
        }

        /// <summary>
        /// Generates the badge and stores is on the BadgesBlob blob storage.
        /// 
        /// The BadgesBlob needs to be configured in the config files
        /// </summary>
        /// <param name="helper"></param>
        /// <param name="configurationHelper"></param>
        /// <param name="binder"></param>
        /// <param name="badgeFileName"></param>
        /// <returns>the uri of the blob the badge was written to</returns>
        private static async Task<string> WriteBadgeToStorage(DeploymentCompletedEventHelper helper, ConfigurationHelper configurationHelper, Binder binder, string badgeFileName)
        {
            var generator = new ShieldsIOBadgeGenerator();

            var badgeContent = await generator.GenerateBadge(
                helper.ReleaseDefinitionName,
                helper.ReleaseName,
                helper.GetColor(),
                configurationHelper.GetFileType(),
                configurationHelper.GetStyle()
                );

            var attributes = new Attribute[]
            {
                new BlobAttribute($"badges/{badgeFileName}", FileAccess.ReadWrite),
                new StorageAccountAttribute("BadgesBlob")
            };

            var maxAge = configurationHelper.GetMaxAge();

            CloudBlockBlob cloudBlob = await binder.BindAsync<CloudBlockBlob>(attributes);

            cloudBlob.Properties.CacheControl = $"public, max-age={maxAge}";

            await cloudBlob.UploadFromByteArrayAsync(badgeContent, 0, badgeContent.Length);

            return cloudBlob.Uri.AbsoluteUri;
        }
    }
}
