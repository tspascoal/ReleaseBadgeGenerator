using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace ReleaseBadge.ShieldIO
{
    /// <summary>
    /// Fetches a badge image (svg,png,etc) from http://shields.io service
    /// </summary>
    internal class ShieldsIOBadgeGenerator
    {
        private const string BaseUrl = "https://img.shields.io/badge/";

        /// <summary>
        /// Fetches the badge from shields.io service.
        /// 
        /// See https://shields.io for all parameter values
        /// </summary>
        /// <param name="subject">The subject (left part of the badge)</param>
        /// <param name="status">Status (right part of the badge)</param>
        /// <param name="color">The color of the badge. Any supported by shields.io
        /// like for example
        /// <list type="">
        /// <item>brightgreen</item>
        /// <item>green</item>
        /// <item>yellowgreen</item>
        /// <item>yellow</item>
        /// <item>orange</item>
        /// <item>red</item>
        /// <item>lightgrey</item>
        /// <item>blue</item>
        /// </list>
        /// </param>
        /// <param name="fileType">The type of file (as supported by shields.io
        /// <list type="">
        /// <item>svg</item>
        /// <item>png</item>
        /// <item>json</item>
        /// </param>
        /// <param name="style">Badge style
        /// <list type="">
        /// <item>plastic</item></list>
        /// <item>flat</item></list>
        /// <item>flat-squared</item></list>
        /// </param>
        /// <returns>The content of the badge</returns>
        public async Task<byte[]> GenerateBadge(string subject, string status, string color, string fileType, string style)
        {
            // ShieldsIO doesn't handle spaces encoded as + , so we replace it with %20
            subject = WebUtility.UrlEncode(EncodeSpecharChar(subject)).Replace("+", "%20");
            status = WebUtility.UrlEncode(EncodeSpecharChar(status)).Replace("+", "%20");

            var url = String.Format("{0}{1}-{2}-{3}.{4}", BaseUrl, subject, status, color, fileType);
            
            if (style != null)
            {
                url += $"?style={style}";
            }

            return await DownloadContent(url);
        }

        private static string EncodeSpecharChar(string text)
        {
            return text.Replace("-", "--").Replace("_", "__");
        }

        private static async Task<byte[]> DownloadContent(string url)
        {
            using (var httpClient = new HttpClient())
            {
                using (var reader = await httpClient.GetAsync(new Uri(url)))
                {
                    byte[] content = await reader.Content.ReadAsByteArrayAsync();


                    return content;
                }
            }
        }
    }
}
