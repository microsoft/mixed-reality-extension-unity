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
    internal static class TwitchVideoSource
    {
        /// <summary>
        /// We only return the top games as categories. Pagination is an option, but we don't
        /// bother, for simplicity. Youtube doesn't have enough categories to make it matter and it
        /// drives most of our feature set.
        /// </summary>
        internal static List<VideoCategory> GetCategoriesImpl()
        {
            var json = WebRequest("https://api.twitch.tv/helix/games/top");
            JArray items = JToken.Parse(json)["data"].Value<JArray>();

            List<VideoCategory> categories = new List<VideoCategory>();
            for (int i = 0; i < items.Count; ++i)
            {
                VideoCategory category = new VideoCategory();
                category.Source = VideoSourceType.Twitch;
                category.Id = items[i]["id"].Value<string>();
                category.ThumbnailUrl = items[i]["box_art_url"].Value<string>();
                category.Title = items[i]["name"].Value<string>();
                categories.Add(category);
            }

            return categories;
        }

        internal static PagedQueryResult SearchVideosImpl(VideoCategory category, int count, string pageId)
        {
            string gameComponent = string.Format("&game_id={0}", category.Id);
            return CreatePagedQueryResult(gameComponent, count, pageId);
        }

        internal static PagedQueryResult SearchVideosImpl(string query, int count, string pageId)
        {
            //TODO: Implement query. The new APIs don't seem to support it?
            return CreatePagedQueryResult("", count, pageId);
        }

        internal static PagedQueryResult CreatePagedQueryResult(string queryComponent, int count, string pageId)
        {
            string pageComponent = "";
            if (!string.IsNullOrWhiteSpace(pageId))
            {
                // This is not an efficient way to do this, but given that youtube works with
                // specific IDs for next/previous, it slots in reasonably well.
                if (pageId.StartsWith("b"))
                {
                    pageComponent = string.Format("&before={0}", pageId.Substring(1));
                }
                else if (pageId.StartsWith("a"))
                {
                    pageComponent = string.Format("&after={0}", pageId.Substring(1));
                }
            }

            string streamQueryResult = WebRequest(string.Format("https://api.twitch.tv/helix/streams?first={0}{1}{2}", count, pageComponent, queryComponent));

            JToken response = JToken.Parse(streamQueryResult);
            JArray jtStreamArray = response["data"].Value<JArray>();

            // Look up the users associated with the streams we got back. Do a single call to reduce the request count.
            List<string> userIds = new List<string>();
            for (int i = 0; i < jtStreamArray.Count; ++i)
            {
                userIds.Add("id=" + jtStreamArray[i]["user_id"].Value<uint>().ToString());
            }

            var userJson = WebRequest(string.Format("https://api.twitch.tv/helix/users?{0}", string.Join("&", userIds)));
            JArray jtUserArray = JToken.Parse(userJson)["data"].Value<JArray>();

            // Grab the StreamChannel for each item in the array.
            List <VideoDetails> videos = new List<VideoDetails>();
            for (int s = 0, u = 0; s < jtStreamArray.Count && u < jtUserArray.Count;)
            {
                if (jtStreamArray[s]["user_id"].Value<string>() == jtUserArray[u]["id"].Value<string>())
                {
                    VideoDetails channel = CreateChannel(jtUserArray[u], jtStreamArray[s]);
                    if (channel != null)
                    {
                        videos.Add(channel);
                    }

                    ++s;
                    ++u;
                }
                else
                {
                    // Sometimes not all users come back (but we should never get extra users). If
                    // we don't see a match, only advance the stream counter.
                    ++s;
                }
            }

            PagedQueryResult result = new PagedQueryResult();
            result.Videos = videos;

            // This is not an efficient way to do this, but given that youtube works with
            // specific IDs for next/previous, it slots in reasonably well.
            string currentPageId = response["pagination"]["cursor"].Value<string>();
            result.NextPageId = "a" + currentPageId;
            result.PrevPageId = "b" + currentPageId;
            result.Total = -1; // Unknown.

            return result;
        }

        internal static List<VideoVariant> GetVideoVariantsImpl(VideoDetails video)
        {
            // Generate the Hls URL. For twitch, you have to request a token, then put together the URL by hand.
            var tokenJson = WebRequest(string.Format("https://api.twitch.tv/api/channels/{0}/access_token", video.ChannelName));
            JToken tokenJt = JToken.Parse(tokenJson);

            string hlsUrl = string.Format(
                "http://usher.twitch.tv/api/channel/hls/{0}.m3u8?player=twitchweb&token={1}&sig={2}&allow_audio_only=true&allow_source=true&type=any&p={3}",
                video.ChannelName,
                System.Uri.EscapeDataString(tokenJt["token"].Value<string>()),
                tokenJt["sig"].Value<string>(),
                GetRandomNumber(1, 999999));

            return Hls.ParseVariantManifest(hlsUrl);
        }
        
        internal static VideoDetails GetVideoImpl(string connectionString)
        {
            Regex twitchRegex = new Regex(@"(.*?twitch\.tv/)(.+)", RegexOptions.IgnoreCase);
            Match match = twitchRegex.Match(connectionString);
            if (!match.Success)
            {
                throw new System.Exception("URI does not appear to be a Twitch URI.");
            }

            string loginName = match.Groups[2].Value;

            string usersJson = WebRequest(string.Format("https://api.twitch.tv/helix/users?login={0}", loginName));
            JArray userAry = JToken.Parse(usersJson)["data"].Value<JArray>();
            if (userAry.Count == 0)
            {
                throw new System.Exception(string.Format("Could not find a user names {0}.", loginName));
            }

            string userJson = WebRequest(string.Format("https://api.twitch.tv/helix/streams?user_id={0}", userAry[0]["id"].Value<uint>()));
            JArray streamAry = JToken.Parse(userJson)["data"].Value<JArray>();

            return CreateChannel(userAry[0], streamAry.Count > 0 ? streamAry[0] : null);
        }

        private static VideoDetails CreateChannel(JToken userJt, JToken streamJt)
        {
            VideoDetails video = new VideoDetails();
            try
            {
                video.Source = VideoSourceType.Twitch;

                // Twitch separates its info into user and stream data. If there is no stream, we
                // assume the stream is currently offline and report a slightly different data set.
                // TODO: Should we always report user description for the description?
                // TODO: Does reporting the user ID make... any kind of sense here? Maybe we should
                //       report something empty...
                // TODO: Is it possible to get a stream object but still have it be offline?
                video.Id = streamJt != null ? streamJt["id"].Value<string>() : userJt["id"].Value<string>();
                video.Title = streamJt != null ? streamJt["title"].Value<string>() : userJt["display_name"].Value<string>();
                video.Description = userJt["description"].Value<string>();
                video.ChannelName = userJt["login"].Value<string>();
                video.ThumbnailUrl = userJt["profile_image_url"].Value<string>();

                // We only support live streams from Twitch, for now.
                video.Type = VideoType.LiveStream;
                video.LiveStream.Online = streamJt != null ? streamJt["type"].Value<string>() == "live" : false;
                video.LiveStream.LiveViewers = streamJt != null ? streamJt["viewer_count"].Value<int>() : 0;
                video.LiveStream.StartTime = DateTime.MinValue;

                // Lookup the follower count.
                // TODO: To reduce lookups, request the entire array at once for CreateChannel calls from GetTopChannels.
                //var followJson = WebRequest(string.Format("https://api.twitch.tv/helix/users/follows?first=1&to_id={0}", channel.Id));
                //JToken followJt = JToken.Parse(followJson);
                //video.Followers = followJt["total"].Value<uint>();
            }
            catch (System.Exception e)
            {
                Debug.LogException(e);
                video = null;
            }

            return video;
        }

        private static string WebRequest(string request)
        {
            return Http.DownloadString(request, "Client-ID", "twr8be3vw90p02u3r5s96obxd59nh8");
        }

        private static readonly System.Random getrandom = new System.Random();
        static private int GetRandomNumber(int min, int max)
        {
            lock (getrandom) // synchronize
            {
                return getrandom.Next(min, max);
            }
        }
    }
}
