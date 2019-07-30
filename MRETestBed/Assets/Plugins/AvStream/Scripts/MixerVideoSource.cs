using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Net;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using UnityEngine;
using System;

namespace AvStreamPlugin
{
    //--------------------------------------------------------------------------------------------------
    // Details: https://dev.mixer.com/rest.html
    //--------------------------------------------------------------------------------------------------
    internal static class MixerVideoSource
    {
        /// <summary>
        /// We only return the top games as categories. Pagination is an option, but we don't
        /// bother, for simplicity. Youtube doesn't have enough categories to make it matter and it
        /// drives most of our feature set.
        /// </summary>
        internal static List<VideoCategory> GetCategoriesImpl()
        {
            var json = Http.DownloadString("https://mixer.com/api/v1/types?order=viewersCurrent:DESC");
            JArray items = JToken.Parse(json).Value<JArray>();

            List<VideoCategory> categories = new List<VideoCategory>();
            for (int i = 0; i < items.Count; ++i)
            {
                VideoCategory category = new VideoCategory();
                category.Source = VideoSourceType.Mixer;
                category.Id = items[i]["id"].Value<string>();
                category.ThumbnailUrl = items[i]["coverUrl"].Value<string>();
                category.Title = items[i]["name"].Value<string>();
                categories.Add(category);

                // items[i]["description"]      // Description of this type.
                // items[i]["viewersCurrent"]   // Number of views active on streams for this type.
                // items[i]["online"]           // Number of online channels for this type.
            }

            return categories;
        }

        internal static VideoDetails GetVideoImpl(string connectionString)
        {
            Regex mixerRegex = new Regex(@"(.*?mixer.com/)(api/v1/channels)?(.+)", RegexOptions.IgnoreCase);

            Match match = mixerRegex.Match(connectionString);
            if (!match.Success)
            {
                throw new System.Exception("URI does not appear to be a mixer URI.");
            }

            string convertedUri;
            if (!match.Groups[2].Success)
            {
                convertedUri = match.Groups[1].Value + "api/v1/channels/" + match.Groups[3].Value;
            }
            else
            {
                convertedUri = connectionString;
            }

            var json = Http.DownloadString(convertedUri);

            JToken jt = JToken.Parse(json);
            if (jt.Type == JTokenType.Array)
            {
                jt = jt[0];
            }

            bool online = jt["online"].Value<bool>();
            JToken hosteeId = jt["hosteeId"];
            if (!online && hosteeId.Type == JTokenType.Integer)
            {
                json = Http.DownloadString("https://mixer.com/api/v1/channels/" + hosteeId.Value<int>().ToString());
                jt = JToken.Parse(json);
                online = jt["online"].Value<bool>();
            }

            return CreateChannel(jt);
        }

        internal static PagedQueryResult SearchVideosImpl(VideoCategory category, int count, string pageId)
        {
            var json = Http.DownloadString(string.Format("http://mixer.com/api/v1/channels?limit={1}&page={2}&scope=all&order=viewersCurrent:DESC&where=typeId:eq:{0},online:eq:true", category.Id, count, pageId)); // ,thumbnailId:ne:null
            return CreatePagedQueryResult(json, pageId);
        }

        internal static PagedQueryResult SearchVideosImpl(string query, int count, string pageId)
        {
            // Note: Channel search on mixer is very literal. It only searches channel names, so a
            // 'fortnite' search won't actually show the most popular fortnight streams.
            var json = Http.DownloadString(string.Format("http://mixer.com/api/v1/channels?q={0}&limit={1}&page={2}&scope=all&order=viewersCurrent:DESC&where=online:eq:true", System.Uri.EscapeUriString(query), count, pageId)); // ,thumbnailId:ne:null
            return CreatePagedQueryResult(json, pageId);
        }

        private static PagedQueryResult CreatePagedQueryResult(string json, string pageId)
        {
            // We expect an array of results. Pull the array out.
            JToken jt = JToken.Parse(json);
            if (jt.Type != JTokenType.Array)
            {
                throw new System.Exception("Unexpected response type");
            }
            JArray jtArray = jt.Value<JArray>();

            // Grab the StreamChannel for each item in the array.
            List<VideoDetails> videos = new List<VideoDetails>();
            for (int i = 0; i < jtArray.Count; ++i)
            {
                VideoDetails channel = CreateChannel(jtArray[i]);
                if (channel != null)
                {
                    videos.Add(channel);
                }
            }

            PagedQueryResult result = new PagedQueryResult();
            result.Videos = videos;

            // Mixer uses actual page numbers.
            int page = string.IsNullOrWhiteSpace(pageId) ? 0 : System.Convert.ToInt32(pageId);
            result.NextPageId = (page + 1).ToString();
            result.PrevPageId = page > 0 ? (page - 1).ToString() : "";
            result.Total = -1; // Unknown.

            return result;
        }

        internal static List<VideoVariant> GetVideoVariantsImpl(VideoDetails video)
        {
            string hlsUrl = "https://mixer.com/api/v1/channels/" + video.Id.ToString() + "/manifest.m3u8";
            return Hls.ParseVariantManifest(hlsUrl);
        }

        private static VideoDetails CreateChannel(JToken jt)
        {
            VideoDetails video = new VideoDetails();
            try
            {
                video.Source = VideoSourceType.Mixer;

                video.Id = jt["id"].Value<string>();
                video.Title = jt["name"].Value<string>();
                video.ChannelName = jt["user"]["username"].Value<string>();

                // Fallback to the game cover if the user doesn't have their own thumbnail. This is allowed to be empty.
                video.ThumbnailUrl = LookupOptionalString(jt, "thumbnail", "url");
                if (string.IsNullOrWhiteSpace(video.ThumbnailUrl))
                {
                    video.ThumbnailUrl = LookupOptionalString(jt, "type", "coverUrl");
                }

                // Description is also allowed to be empty.
                video.Description = LookupOptionalString(jt, "description");

                // We only support live streams from Mixer, for now.
                video.Type = VideoType.LiveStream;
                video.LiveStream.Online = jt["online"].Value<bool>();
                video.LiveStream.LiveViewers = jt["viewersCurrent"].Value<int>();
                video.LiveStream.StartTime = DateTime.MinValue;
                // video.Followers = jt["numFollowers"].Value<uint>();
            }
            catch (System.Exception e)
            {
                Debug.LogException(e);
                video = null;
            }

            return video;
        }

        static string LookupOptionalString(JToken t, string a)
        {
            JToken child = t[a];
            return !IsTokenNull(child) ? child.Value<string>() : "";
        }

        static string LookupOptionalString(JToken parent, string a, string b)
        {
            JToken child = parent[a];
            return !IsTokenNull(child) ? LookupOptionalString(child, b) : "";
        }

        static bool IsTokenNull(JToken token)
        {
            return token == null || token.Type == JTokenType.Null || token.Type == JTokenType.None;
        }
    }
}
