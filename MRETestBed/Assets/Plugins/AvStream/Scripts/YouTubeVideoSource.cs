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
	// TODO: Cache regex compilations and web service results.

	//--------------------------------------------------------------------------------------------------
	// Docs: https://developers.google.com/youtube/v3/
	// Run practice queries: https://developers.google.com/apis-explorer/#p/youtube/v3/
	// Example Queries:
	//  List Video Categories: GET https://www.googleapis.com/youtube/v3/videoCategories?part=snippet&regionCode=US&fields=items(id%2Csnippet(assignable%2Ctitle))&key={YOUR_API_KEY}
	//  Get top Gaming Videos: GET https://www.googleapis.com/youtube/v3/search?part=snippet&maxResults=20&regionCode=US&type=video&videoCategoryId=20&fields=items(id%2FvideoId%2Csnippet(description%2Cthumbnails%2Fdefault%2Ctitle))%2CnextPageToken%2CprevPageToken&key={YOUR_API_KEY}
	//  Top live streams: GET https://www.googleapis.com/youtube/v3/search?part=snippet&eventType=live&maxResults=20&regionCode=US&type=video&videoCategoryId=20&fields=items(id%2FvideoId%2Csnippet(description%2Cthumbnails%2Fdefault%2Ctitle))%2CnextPageToken%2CprevPageToken&key={YOUR_API_KEY}
	//
	internal static class YouTubeVideoSource
    {
        private const string apiKey = "AIzaSyA-wN8lmcSJ0krClEpw8JdlCN7Mc8WMCgM";

		// Query Builder: https://developers.google.com/apis-explorer/#p/youtube/v3/youtube.videoCategories.list?part=snippet&regionCode=US&fields=items(id%252Csnippet(assignable%252Ctitle))&_h=2&
		private const string getCategoriesUri = "https://www.googleapis.com/youtube/v3/videoCategories?part=snippet&regionCode=US&fields=items(id%2Csnippet(assignable%2Ctitle))&key=" + apiKey;

        // Query Builder: https://developers.google.com/apis-explorer/#p/youtube/v3/youtube.search.list?part=snippet&maxResults=20&pageToken=CBQQAA&q=Music&type=video%252Cplaylist&fields=items(id(playlistId%252CvideoId))%252CpageInfo%252FtotalResults%252CnextPageToken%252CprevPageToken&_h=4&
        private const string searchUri = "https://www.googleapis.com/youtube/v3/search?part=snippet&maxResults={1}&pageToken={2}&q={0}&type=video%2Cplaylist&fields=items(id(playlistId%2CvideoId))%2CpageInfo%2FtotalResults%2CnextPageToken%2CprevPageToken&key=" + apiKey;

        // Query Builder: https://developers.google.com/apis-explorer/#p/youtube/v3/youtube.search.list?part=snippet&maxResults=20&pageToken=CBQQAA&type=video&videoCategoryId=20&fields=items(id(playlistId%252CvideoId))%252CnextPageToken%252CpageInfo%252FtotalResults%252CprevPageToken&_h=11&
        private const string categorySearchUri = "https://www.googleapis.com/youtube/v3/search?part=snippet&maxResults={1}&pageToken={2}&type=video&videoCategoryId={0}&fields=items(id(playlistId%2CvideoId))%2CnextPageToken%2CpageInfo%2FtotalResults%2CprevPageToken&key=" + apiKey;

        // Query Builder: https://developers.google.com/apis-explorer/#p/youtube/v3/youtube.videos.list?part=snippet%252C+statistics%252C+liveStreamingDetails%252C+contentDetails&id=Lvp-lSqHVKc%252CpD6S69pCj28&fields=items(contentDetails%252Fduration%252Cid%252CliveStreamingDetails(concurrentViewers%252CscheduledStartTime)%252Csnippet(channelTitle%252Cdescription%252CliveBroadcastContent%252Cthumbnails%252Fmedium%252Furl%252Ctitle)%252Cstatistics%252FviewCount)&_h=7&
        private const string getVideoUri = "https://www.googleapis.com/youtube/v3/videos?part=snippet%2C+statistics%2C+liveStreamingDetails%2C+contentDetails&id={0}&fields=items(contentDetails%2Fduration%2Cid%2CliveStreamingDetails(concurrentViewers%2CscheduledStartTime)%2Csnippet(channelTitle%2Cdescription%2CliveBroadcastContent%2Cthumbnails%2Fmedium%2Furl%2Ctitle)%2Cstatistics%2FviewCount)&key=" + apiKey;

        // Query Builder: https://developers.google.com/apis-explorer/#p/youtube/v3/youtube.playlists.list?part=snippet%252CcontentDetails&id=PLFgquLnL59alCl_2TQvOiD5Vgm1hCaGSI&fields=items(contentDetails%252Cid%252Csnippet(channelTitle%252Cdescription%252Cthumbnails%252Fmedium%252Furl%252Ctitle))&_h=5&
        private const string getPlaylistUri = "https://www.googleapis.com/youtube/v3/playlists?part=snippet%2CcontentDetails&id={0}&fields=items(contentDetails%2Cid%2Csnippet(channelTitle%2Cdescription%2Cthumbnails%2Fmedium%2Furl%2Ctitle))&key=" + apiKey;

        // Query Builder: https://developers.google.com/apis-explorer/#p/youtube/v3/youtube.playlistItems.list?part=snippet&maxResults=20&playlistId=RDQMsHhx03c4Dwk&fields=items%252Fsnippet%252FresourceId%252FvideoId%252CpageInfo%252FtotalResults%252CnextPageToken%252CprevPageToken&_h=5&
        private const string getPlaylistItemsUri = "https://www.googleapis.com/youtube/v3/playlistItems?part=snippet&maxResults={1}&playlistId={0}&pageToken={2}&fields=items%2Fsnippet%2FresourceId%2FvideoId%2CpageInfo%2FtotalResults%2CnextPageToken%2CprevPageToken&key=" + apiKey;

        internal static List<VideoCategory> GetCategoriesImpl()
        {
            string categoriesJson = Http.DownloadString(getCategoriesUri);
            JArray items = JToken.Parse(categoriesJson)["items"].Value<JArray>();

            var result = new List<VideoCategory>();
            for (int i = 0; i < items.Count; ++i)
            {
				// For some reason, youtube has a bunch of categories that are marked as
				// non -assignable (or "can't be used"). No idea why, but here we are. Cull these
				// out, since they'll never have content.
				JToken jt = items[i]["snippet"]["assignable"];
				if (jt.Value<bool>())
				{
					VideoCategory category = new VideoCategory();
					category.Source = VideoSourceType.YouTube;
					category.Id = items[i]["id"].Value<string>();
					category.ThumbnailUrl = "";
					category.Title = items[i]["snippet"]["title"].Value<string>();
					result.Add(category);
				}
            }

            return result;
        }

        internal static VideoDetails GetVideoImpl(string connectionString)
        {
            string json = "";
            string optionalVideoId = "";

            // Check to see if we have a URL.
            if (Regex.IsMatch(connectionString, @".*?youtube\.com/watch\?", RegexOptions.IgnoreCase) ||
                Regex.IsMatch(connectionString, @".*?youtube\.com/playlist\?", RegexOptions.IgnoreCase))
            {
                Match videoMatch = Regex.Match(connectionString, @"[?&]v=([\w\d-_]{11})(&.*)?", RegexOptions.IgnoreCase);
                Match listMatch = Regex.Match(connectionString, @"[?&]list=([^&]+)&?", RegexOptions.IgnoreCase);
                if (listMatch.Success)
                {
                    json = Http.DownloadString(string.Format(getPlaylistUri, listMatch.Groups[1].Value));
                    if (videoMatch.Success)
                    {
                        optionalVideoId = videoMatch.Groups[1].Value;
                    }
                }
                else if (videoMatch.Success)
                {
                    json = Http.DownloadString(string.Format(getVideoUri, videoMatch.Groups[1].Value));
                }
            }
            else
            {
                // It's not a YouTube URL, but may be a video or playlist ID. For youtube, these are
                // always separate from each other, so we can try to look for one, then the other
                // and see if we get anything.
                if (Regex.IsMatch(connectionString, @"^[\w\d-_]+$"))
                {
                    if (connectionString.Length == 11)
                    {
                        json = Http.DownloadString(string.Format(getVideoUri, connectionString));
                    }
                    else if (connectionString.Length > 11)
                    {
                        json = Http.DownloadString(string.Format(getPlaylistUri, connectionString));
                    }
                }
            }

            if (!string.IsNullOrWhiteSpace(json))
            {
                JArray items = JToken.Parse(json)["items"].Value<JArray>();
                if (items != null && items.Count > 0)
                {
                    return BuildVideo(items[0], optionalVideoId);
                }
            }

            throw new System.Exception(string.Format("Could not find a video or playlist from '{0}'.", connectionString));
        }

        internal static PagedQueryResult SearchVideosImpl(VideoCategory category, int count, string pageId)
        {
            // Submit our query to get video ids.
            string searchJson = Http.DownloadString(string.Format(categorySearchUri, category.Id, count, pageId));
            return CreatePagedQueryResult(searchJson, "id");
        }

        internal static PagedQueryResult SearchVideosImpl(string query, int count, string pageId)
        {
            // Submit our query to get video ids.
            string searchJson = Http.DownloadString(string.Format(searchUri, Uri.EscapeDataString(query), count, pageId));
            return CreatePagedQueryResult(searchJson, "id");
        }

        internal static PagedQueryResult QueryPlaylistVideosImpl(VideoDetails playlist, int count, string pageId = "")
        {
            string searchJson = Http.DownloadString(string.Format(getPlaylistItemsUri, playlist.Id, count, pageId));
            return CreatePagedQueryResult(searchJson, "snippet.resourceId");
        }

        internal static List<VideoVariant> GetVideoVariantsImpl(VideoDetails video)
        {
            List<VideoVariant> results = null;

            // I don't see a way to get this in the V3 APIs.
            if (video.Type == VideoType.LiveStream)
            {
                string videoInfoUrl = string.Format("https://www.youtube.com/get_video_info?&video_id={0}&asv=3&el=detailpage&hl=en_US", video.Id);
                string videoInfoParameters = Http.DownloadString(videoInfoUrl);

                videoInfoParameters = WWW.UnEscapeURL(videoInfoParameters);

                Regex hlsRegex = new Regex(@"hlsvp=(.*?index.m3u8)", RegexOptions.IgnoreCase);
                Match match = hlsRegex.Match(videoInfoParameters);
                if (match.Success)
                {
                    results = Hls.ParseVariantManifest(WWW.UnEscapeURL(match.Groups[1].Value));
                }
                else
                {
                    hlsRegex = new Regex(@"""hlsManifestUrl"":""(.*?index.m3u8)", RegexOptions.IgnoreCase);
                    match = hlsRegex.Match(videoInfoParameters);
                    if (match.Success)
                    {
                        results = Hls.ParseVariantManifest(WWW.UnEscapeURL(match.Groups[1].Value));
                    }
                    else
                    {
                        Debug.LogWarningFormat("Failed to get HLS url from {0}", videoInfoUrl);
                    }
                }
            }
            else
            {
                // string videoInfoUrl = string.Format("https://www.youtube.com/get_video_info?&video_id={0}&asv=3&el=detailpage&hl=en_US", channel.Id); // 
                string videoInfoUrl = string.Format("https://www.youtube.com/get_video_info?&video_id={0}&el=embedded&ps=default&eurl=&gl=US&hl=en", video.Id);
                string videoInfoParameters = Http.DownloadString(videoInfoUrl);

                // TODO: Look at supporting adaptive_fmts. They're high quality, though the audi and
                // video streams are split, so some significant support would need to be plumbed to
                // make it happen.

                // The regex is about 4 times as fast as trying to be clever with split.
                Regex hlsRegex = new Regex(@"url_encoded_fmt_stream_map=([^&]+)", RegexOptions.IgnoreCase);
                Match match = hlsRegex.Match(videoInfoParameters);

                // Rather than checking for 'errorcode' specifically, rely on the fact that
                // url_encoded_fmt_stream_map is empty and the regex will fail on errors.
                // Retry with the details page. Note that we can't always use the details page,
                // since some that succeed on embedded fail on details. Details is also more data.
                if (!match.Success)
                {
                    videoInfoUrl = string.Format("https://www.youtube.com/get_video_info?&video_id={0}&el=detailpage&ps=default&eurl=&gl=US&hl=en", video.Id);
                    videoInfoParameters = Http.DownloadString(videoInfoUrl);
                    match = hlsRegex.Match(videoInfoParameters);

                    // TODO: For some reason, I'm still getting 403 errors when I lookup the actual video files...
                }

                if (match.Success)
                {
                    string[] videoDetails = WWW.UnEscapeURL(match.Groups[1].Value).Split(',');

                    results = new List<VideoVariant>(videoDetails.Length);

                    for (int i = 0; i < videoDetails.Length; ++i)
                    {
                        try
                        {
                            VideoVariant desc = new VideoVariant();
                            string decipheredSig = null;
                            string[] parameterPairs = videoDetails[i].Split('&');
                            for (int j = 0; j < parameterPairs.Length; ++j)
                            {
                                string[] splitPair = parameterPairs[j].Split('=');
                                switch (splitPair[0])
                                {
                                    case "quality":
                                        desc.Name = splitPair[1];
                                        switch (splitPair[1])
                                        {
                                            case "hd2160":
                                                desc.Width = 3840;
                                                desc.Height = 2160;
                                                break;
                                            case "hd1440":
                                                desc.Width = 2560;
                                                desc.Height = 1440;
                                                break;
                                            case "hd1080":
                                                desc.Width = 1920;
                                                desc.Height = 1080;
                                                break;
                                            case "hd720":
                                                desc.Width = 1280;
                                                desc.Height = 720;
                                                break;
                                            case "large":
                                                desc.Width = 854;
                                                desc.Height = 480;
                                                break;
                                            case "medium":
                                                desc.Width = 640;
                                                desc.Height = 360;
                                                break;
                                            case "small":
                                                desc.Width = 426;
                                                desc.Height = 240;
                                                break;
                                        }
                                        break;

                                    case "url":
                                        desc.Url = WWW.UnEscapeURL(splitPair[1]);
                                        break;

                                    case "s":
                                        // TODO: Either hide these entirely or support the cipher. The cipher changes, so people are actually parsing the player's javascript code...
                                        // Info: https://tyrrrz.me/Blog/Reverse-engineering-YouTube
                                        // An API: https://api.gitnol.com/

                                        string cipherSig = splitPair[1];

                                        string embedPage = Http.DownloadString("https://www.youtube.com/embed/" + video.Id);
                                        Regex playerRegex = new Regex(@"src=""([^\""]+/base.js)\""");
                                        Match playerMatch = playerRegex.Match(embedPage);
                                        if (!playerMatch.Success)
                                        {
                                            throw new Exception("Failed to find player URL");
                                        }

                                        string playerUrl = "https://www.youtube.com" + playerMatch.Groups[1].Value;

                                        try
                                        {
                                            decipheredSig = Decipherer.Decipher(playerUrl, cipherSig);
                                        }
                                        catch (Exception e)
                                        {
                                            throw new Exception(string.Format("Could not decipher signature for video. [https://www.youtube.com/watch?v={0}] [{1}] [{2}]", video.Id, playerUrl, cipherSig), e);
                                        }
                                        

                                        break;
                                }
                            }

                            if (string.IsNullOrWhiteSpace(desc.Url))
                            {
                                throw new Exception("Did no find URL in video parameters");
                            }

                            if (!string.IsNullOrWhiteSpace(decipheredSig))
                            {
                                desc.Url = string.Format("{0}&signature={1}", desc.Url, decipheredSig);
                            }

                            results.Add(desc);
                        }
                        catch (Exception e)
                        {
                            Debug.LogException(e);
                        }
                    }
                }
                else
                {
                    Debug.LogWarningFormat("Failed to get video url from {0}", videoInfoUrl);
                }
            }

            if (results == null)
            {
                results = new List<VideoVariant>();
            }

            return results;
        }

        private static PagedQueryResult CreatePagedQueryResult(string json, string path)
        {
            JToken searchToken = JToken.Parse(json);
            JArray searchItems = searchToken["items"].Value<JArray>();

            List<string> searchVideoIds = new List<string>(searchItems.Count);
            List<string> searchPlaylistIds = new List<string>(searchItems.Count);
            for (int i = 0; i < searchItems.Count; ++i)
            {
                JToken idInfo = searchItems[i].SelectToken(path);
                if (idInfo["videoId"] != null)
                {
                    searchVideoIds.Add(idInfo["videoId"].Value<string>());
                }
                else if (idInfo["playlistId"] != null)
                {
                    searchPlaylistIds.Add(idInfo["playlistId"].Value<string>());
                }
            }

            // Query for more details on the videos themselves.
            JArray videoItems = null;
            if (searchVideoIds.Count > 0)
            {
                string videoJson = Http.DownloadString(string.Format(getVideoUri, string.Join("%2C", searchVideoIds)));
                videoItems = JToken.Parse(videoJson)["items"].Value<JArray>();
            }

            JArray playlistItems = null;
            if (searchPlaylistIds.Count > 0)
            {
                string playlistJson = Http.DownloadString(string.Format(getPlaylistUri, string.Join("%2C", searchPlaylistIds)));
                playlistItems = JToken.Parse(playlistJson)["items"].Value<JArray>();
            }

            List<VideoDetails> videos = new List<VideoDetails>(searchItems.Count);
            for (int i = 0, p = 0, v = 0; i < searchItems.Count; ++i)
            {
                VideoDetails video = null;
                JToken idInfo = searchItems[i].SelectToken(path);
                if ((idInfo["videoId"] != null) && (v < videoItems.Count))
                {
                    video = BuildVideo(videoItems[v]);
                    ++v;
                }
                else if ((idInfo["playlistId"] != null) && (p < playlistItems.Count))
                {
                    video = BuildVideo(playlistItems[p]);
                    ++p;
                }

                if (video != null)
                {
                    videos.Add(video);
                }
            }

            PagedQueryResult result = new PagedQueryResult();
            result.Videos = videos;
            result.NextPageId = searchToken["nextPageToken"] != null ? searchToken["nextPageToken"].Value<string>() : "";
            result.PrevPageId = searchToken["prevPageToken"] != null ? searchToken["prevPageToken"].Value<string>() : "";
            result.Total = searchToken["pageInfo"]["totalResults"].Value<int>();

            return result;
        }

        private static VideoDetails BuildVideo(JToken item, string optionalVideoId = "")
        {
            VideoDetails video = new VideoDetails();
            try
            {
                video.Source = VideoSourceType.YouTube;

                video.Id = item["id"].Value<string>();

                JToken snippet = item["snippet"];
                video.Title = snippet["title"].Value<string>();
                video.Description = snippet["description"].Value<string>();
                video.ThumbnailUrl = snippet["thumbnails"]["medium"]["url"].Value<string>();
                video.ChannelName = snippet["channelTitle"].Value<string>();

                if (snippet["liveBroadcastContent"] != null)
                {
                    string liveBroadcastContent = snippet["liveBroadcastContent"].Value<string>();
                    if (liveBroadcastContent != "none")
                    {
                        video.Type = VideoType.LiveStream;

                        video.LiveStream.Online = (liveBroadcastContent == "live");

                        JToken cv = item["liveStreamingDetails"]["concurrentViewers"];
                        video.LiveStream.LiveViewers = (cv != null) ? cv.Value<int>() : 0;

                        JToken sst = item["liveStreamingDetails"]["scheduledStartTime"];
                        video.LiveStream.StartTime = sst != null ? DateTime.Parse(sst.Value<string>()) : DateTime.MinValue;
                    }
                    else
                    {
                        video.Type = VideoType.Recording;

                        // Some videos (videos by Google itself) sometimes don't actually have views
                        // or event statistics on them at all (sketchy sketchy)
                        if (item["statistics"] != null && item["statistics"]["viewCount"] != null)
                        {
                            video.Recording.Views = item["statistics"]["viewCount"].Value<long>();
                        }

                        string duration = item["contentDetails"]["duration"].Value<string>();
                        Regex durationRegex = new Regex(@"PT((\d+)H)?((\d+)M)?((\d+)S)?");
                        Match match = durationRegex.Match(duration);
                        if (match.Success)
                        {
                            int hours = match.Groups[2].Success ? Convert.ToInt32(match.Groups[2].Value) : 0;
                            int minutes = match.Groups[4].Success ? Convert.ToInt32(match.Groups[4].Value) : 0;
                            int seconds = match.Groups[5].Success ? Convert.ToInt32(match.Groups[6].Value) : 0;
                            video.Recording.Duration = new System.TimeSpan(hours, minutes, seconds);
                        }
                    }
                }
                else
                {
                    video.Type = VideoType.Playlist;
                    video.Playlist.ItemCount = item["contentDetails"]["itemCount"].Value<int>();
                    video.Playlist.StartId = optionalVideoId;
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                video = null;
            }

            return video;
        }
    }
}
