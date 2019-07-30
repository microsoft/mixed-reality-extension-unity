using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Net;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using UnityEngine;
using System;
using System.Threading.Tasks;

namespace AvStreamPlugin
{
    public enum VideoSourceType
    {
        Raw,
        Mixer,
        Twitch,
        YouTube,
    }

    public enum VideoType
    {
        Unknown,
        LiveStream,
        Recording,
        Playlist,
    }

    [Serializable]
    public struct LiveStreamDetails
    {
        public int LiveViewers;
        public bool Online;
        public DateTime StartTime;
    }

    [Serializable]
    public struct RecordingDetails
    {
        public TimeSpan Duration;
        public long Views;
    }

    [Serializable]
    public struct PlaylistDetails
    {
        public int ItemCount;
        public string StartId;
    }

    [Serializable]
    public class VideoDetails
    {
        /// <summary>
        /// Check to see if the details are of the same video. This is NOT an equality operator.
        /// Changing descriptions and titles don't impact this and neither object can be null to
        /// return true (if we don't have videos, then they can't be the same).
        /// </summary>
        public static bool Same(VideoDetails a, VideoDetails b)
        {
            //if (a == null || b == null)
            //{
            //    return a == b;
            //}

            return a != null && b != null && a.Source == b.Source && a.Id == b.Id;
        }

        public VideoSourceType Source;

        public string Id;
        public string Title;
        public string Description;
        public string ChannelName;
        public string ThumbnailUrl;

        public VideoType Type;

        public LiveStreamDetails LiveStream;
        public RecordingDetails Recording;
        public PlaylistDetails Playlist;

	}

    public class VideoVariant
    {
        public string Name;
        public string Url;
        public int Width;
        public int Height;
    }

    public class VideoCategory
    {
        public VideoSourceType Source;

        public string Id;
        public string Title;
        public string ThumbnailUrl;
    }

    public struct PagedQueryResult
    {
        public List<VideoDetails> Videos;
        public string NextPageId;
        public string PrevPageId;
        public int Total;
    }

    public static class VideoSource
    {

        public static Task<List<VideoCategory>> GetCategoriesAsync(VideoSourceType source)
        {
            return Task.Run(() =>
            {
                return GetCategories(source);
            });
        }

        public static List<VideoCategory> GetCategories(VideoSourceType source)
        {
            try
            {
                switch (source)
                {
                    case VideoSourceType.Mixer:
                        return MixerVideoSource.GetCategoriesImpl();
                    case VideoSourceType.Twitch:
                        return TwitchVideoSource.GetCategoriesImpl();
                    case VideoSourceType.YouTube:
                        return YouTubeVideoSource.GetCategoriesImpl();
                }
            }
            catch (WebException e)
            {
                // Debug.LogWarningFormat("WebException: [{0}] [{1}]", e.Message, e.Status.ToString());
                Debug.LogException(e);
            }
            catch (System.Exception e)
            {
                // Debug.LogWarningFormat("Exception: [{0}]", e.Message);
                Debug.LogException(e);
            }

            return new List<VideoCategory>();
        }

        public static Task<PagedQueryResult> SearchVideosAsync(VideoCategory category, int count, string pageId = "")
        {
            return Task.Run(() =>
            {
                return SearchVideos(category, count, pageId);
            });
        }

        public static PagedQueryResult SearchVideos(VideoCategory category, int count, string pageId = "")
        {
            try
            {
                switch (category.Source)
                {
                    case VideoSourceType.Mixer:
                        return MixerVideoSource.SearchVideosImpl(category, count, pageId);
                    case VideoSourceType.Twitch:
                        return TwitchVideoSource.SearchVideosImpl(category, count, pageId);
                    case VideoSourceType.YouTube:
                        return YouTubeVideoSource.SearchVideosImpl(category, count, pageId);
                }
            }
            catch (WebException e)
            {
                // Debug.LogWarningFormat("WebException: [{0}] [{1}]", e.Message, e.Status.ToString());
                Debug.LogException(e);
            }
            catch (System.Exception e)
            {
                // Debug.LogWarningFormat("Exception: [{0}]", e.Message);
                Debug.LogException(e);
            }

            PagedQueryResult result = new PagedQueryResult();
            result.Videos = new List<VideoDetails>();
            return result;
        }

        public static Task<PagedQueryResult> SearchVideosAsync(VideoSourceType source, string query, int count, string pageId = "")
        {
            return Task.Run(() =>
            {
                return SearchVideos(source, query, count, pageId);
            });
        }

        public static PagedQueryResult SearchVideos(VideoSourceType source, string query, int count, string pageId = "")
        {
            try
            {
                switch (source)
                {
                    case VideoSourceType.Mixer:
                        return MixerVideoSource.SearchVideosImpl(query, count, pageId);
                    case VideoSourceType.Twitch:
                        return TwitchVideoSource.SearchVideosImpl(query, count, pageId);
                    case VideoSourceType.YouTube:
                        return YouTubeVideoSource.SearchVideosImpl(query, count, pageId);
                }
            }
            catch (WebException e)
            {
                // Debug.LogWarningFormat("WebException: [{0}] [{1}]", e.Message, e.Status.ToString());
                Debug.LogException(e);
            }
            catch (System.Exception e)
            {
                // Debug.LogWarningFormat("Exception: [{0}]", e.Message);
                Debug.LogException(e);
            }

            PagedQueryResult result = new PagedQueryResult();
            result.Videos = new List<VideoDetails>();
            return result;
        }

        public static Task<PagedQueryResult> QueryPlaylistVideosAsync(VideoDetails video, int count, string pageId = "")
        {
            return Task.Run(() =>
            {
                return QueryPlaylistVideos(video, count, pageId);
            });
        }

        public static PagedQueryResult QueryPlaylistVideos(VideoDetails video, int count, string pageId = "")
        {
            if (video != null && video.Source == VideoSourceType.YouTube && video.Type == VideoType.Playlist)
            {
                try
                {
                    return YouTubeVideoSource.QueryPlaylistVideosImpl(video, count, pageId);
                }
                catch (WebException e)
                {
                    // Debug.LogWarningFormat("WebException: [{0}] [{1}]", e.Message, e.Status.ToString());
                    Debug.LogException(e);
                }
                catch (System.Exception e)
                {
                    // Debug.LogWarningFormat("Exception: [{0}]", e.Message);
                    Debug.LogException(e);
                }
            }

            PagedQueryResult result = new PagedQueryResult();
            result.Videos = new List<VideoDetails>();
            return result;
        }

        public static VideoSourceType DetectVideoSourceType(string uriString)
        {
            // Assume https, if nothing is specified.
            if (!uriString.StartsWith("http"))
            {
                uriString = string.Format("https://{0}", uriString);
            }

            // for some reason, TryCreate was giving me a success and a null uri...
            if (Uri.IsWellFormedUriString(uriString, UriKind.Absolute))
            {
                Uri uri = new Uri(uriString);

                // We must have a query string of some sort. > 1 since '/' is the empty path.
                if (uri.PathAndQuery.Length > 1)
                {
                    // The string must match one of the known, supported providers.
                    if (Regex.IsMatch(uri.Host, @"youtube\.com", RegexOptions.IgnoreCase))
                    {
                        return VideoSourceType.YouTube;
                    }
                    else if (Regex.IsMatch(uri.Host, @"mixer\.com", RegexOptions.IgnoreCase))
                    {
                        return VideoSourceType.Mixer;
                    }
                    else if (Regex.IsMatch(uri.Host, @"twitch\.tv", RegexOptions.IgnoreCase))
                    {
                        return VideoSourceType.Twitch;
                    }
                }
            }

            return VideoSourceType.Raw;
        }

        public static Task<VideoDetails> GetVideoFromStringAsync(string url)
        {
            return GetVideoFromStringAsync(DetectVideoSourceType(url), url);
        }

        public static Task<VideoDetails> GetVideoFromStringAsync(VideoSourceType source, string url)
        {
            return Task.Run(() =>
            {
                return GetVideoFromString(source, url);
            });
        }

        public static VideoDetails GetVideoFromString(VideoSourceType source, string url)
        {
            if (!string.IsNullOrWhiteSpace(url))
            {
                try
                {
                    switch (source)
                    {
                        case VideoSourceType.Raw:
                            if (Uri.IsWellFormedUriString(url, UriKind.Absolute))
                            {
                                VideoDetails video = new VideoDetails();
                                video.Source = VideoSourceType.Raw;
                                video.Id = url;
                                if (Hls.LooksLikeIndexUrl(url))
                                {
                                    video.Type = VideoType.LiveStream;
                                    video.LiveStream = new LiveStreamDetails();
                                }
                                else
                                {
                                    video.Type = VideoType.Unknown;
                                }
                                return video;
                            }
                            break;
                        case VideoSourceType.Mixer:
                            return MixerVideoSource.GetVideoImpl(url);
                        case VideoSourceType.Twitch:
                            return TwitchVideoSource.GetVideoImpl(url);
                        case VideoSourceType.YouTube:
                            return YouTubeVideoSource.GetVideoImpl(url);
                    }
                }
                catch (WebException e)
                {
                    // Debug.LogWarningFormat("WebException: [{0}] [{1}]", e.Message, e.Status.ToString());
                    Debug.LogException(e);
                }
                catch (System.Exception e)
                {
                    // Debug.LogWarningFormat("Exception: [{0}]", e.Message);
                    Debug.LogException(e);
                }
            }

            return null;
        }

        public static Task<List<VideoVariant>> GetVideoVariantsAsync(VideoDetails video)
        {
            return Task.Run(() =>
            {
                return GetVideoVariants(video);
            });
        }

        public static List<VideoVariant> GetVideoVariants(VideoDetails video)
        {
            if (video != null && video.Type != VideoType.Playlist)
            {
                try
                {
                    switch (video.Source)
                    {
                        case VideoSourceType.Raw:
                            if (Uri.IsWellFormedUriString(video.Id, UriKind.Absolute))
                            {
                                if (Hls.LooksLikeIndexUrl(video.Id))
                                {
                                    return Hls.ParseVariantManifest(video.Id);
                                }
                                VideoVariant variant = new VideoVariant();
                                variant.Url = video.Id;
                                return new List<VideoVariant>() { variant };
                            }
                            break;

                        case VideoSourceType.Mixer:
                            return MixerVideoSource.GetVideoVariantsImpl(video);

                        case VideoSourceType.Twitch:
                            return TwitchVideoSource.GetVideoVariantsImpl(video);

                        case VideoSourceType.YouTube:
                            return YouTubeVideoSource.GetVideoVariantsImpl(video);
                    }
                }
                catch (WebException e)
                {
                    // Debug.LogWarningFormat("WebException: [{0}] [{1}]", e.Message, e.Status.ToString());
                    Debug.LogException(e);
                }
                catch (System.Exception e)
                {
                    // Debug.LogWarningFormat("Exception: [{0}]", e.Message);
                    Debug.LogException(e);
                }
            }

            return new List<VideoVariant>();
        }

        public static Task<VideoVariant> SelectVariantAsync(VideoDetails video, int verticalRes)
        {
            return Task.Run(() =>
            {
                return SelectVariant(video, verticalRes);
            });
        }

        public static VideoVariant SelectVariant(VideoDetails video, int verticalRes)
        {
            return SelectVariant(GetVideoVariants(video), verticalRes);
        }

        public static VideoVariant SelectVariant(List<VideoVariant> variants, int verticalRes)
        {
            if (variants == null || variants.Count == 0)
            {
                return null;
            }

            int variantIndex = 0;
            for (int i = 0; i < variants.Count; ++i)
            {
				// Prefer exact matches
				if (variants[i].Height == verticalRes)
				{
					variantIndex = i;
					break;
				}

				// Otherwise, choose the highest resolution that is still in the correct ballpark,
				// without going far over. For example, this would allow a 720 request to pick up a
				// 768 video, without falling into something like 1080.
				if (variants[i].Height < (verticalRes + 50) && variants[i].Height > variants[variantIndex].Height)
                {
                    variantIndex = i;
                }
            }
            if (variants[variantIndex] != null)
            {
                if (variants[variantIndex].Width == 0)
                {
                    variants[variantIndex].Width = 640;
                }
                if (variants[variantIndex].Height == 0)
                {
                    variants[variantIndex].Height = 480;
                }
            }

            return variants[variantIndex];
        }
    }
}
