using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Net;

using UnityEngine;
using System.Threading.Tasks;

namespace AvStreamPlugin
{
    internal class Hls
    {
        static Regex AmsHlsRegex = new Regex(@"Manifest\(format=m3u8-aapl-v3\)", RegexOptions.IgnoreCase);
        static Regex HlsRegex = new Regex(@"\.m3u8(\?.*)?$", RegexOptions.IgnoreCase);

        static private Match RequireMatch(string str, string regex)
        {
            Match result = Regex.Match(str, regex);
            if (!result.Success)
            {
                throw new System.Exception();
            }

            return result;
        }

        static public bool LooksLikeIndexUrl(string url)
        {
            return (AmsHlsRegex.Match(url).Success || HlsRegex.Match(url).Success);
        }

        static public Task<List<VideoVariant>> ParseVatiantManifestAsync(string sourceString)
        {
            return Task.Run(() =>
            {
                return ParseVariantManifest(sourceString);
            });
        }

        static public List<VideoVariant> ParseVariantManifest(string sourceString)
        {
            List<VideoVariant> variants = new List<VideoVariant>();

            try
            {
                string manifestData = Http.DownloadString(sourceString);

                System.IO.StringReader reader = new System.IO.StringReader(manifestData);

                // Make sure the file is setup like we expect.
                string manifestLine = reader.ReadLine();
                RequireMatch(manifestLine, "^#EXTM3U\\s*");

                // Start scanning for actual variants.
                string variantIdentifier = "#EXT-X-STREAM-INF:";

                Match match;
                while (true)
                {
                    manifestLine = reader.ReadLine();
                    if (manifestLine == null)
                    {
                        break;
                    }

                    // We only only care about one thing, at this point. Variant streams. Ignore anything else.
                    // This includes things like empty lines and other tags we don't support, yet, like
                    // #EXT-X-I-FRAME-STREAM-IN and #EXT-X-MEDIA.
                    if (!manifestLine.StartsWith(variantIdentifier))
                    {
                        continue;
                    }

                    VideoVariant variant = new VideoVariant();

                    // Fill in the info with details from the current line.
                    //match = RequireMatch(manifestLine, "BANDWIDTH=(\\d+)");
                    //variant.Bandwidth = uint.Parse(match.Groups[1].Value);

                    // No resolution indicates an audio only stream?
                    match = Regex.Match(manifestLine, "RESOLUTION=(\\d+)x(\\d+)");
                    if (match.Success)
                    {
                        variant.Width = int.Parse((match.Groups[1].Value));
                        variant.Height = int.Parse((match.Groups[2].Value));
                    }

                    match = Regex.Match(manifestLine, "NAME=([^,]+)");
                    if (match.Success)
                    {
                        variant.Name = match.Groups[1].Value;
                    }

                    // The next line must be a valid URI. Overly specific, but I'm being paranoid for now.
                    manifestLine = reader.ReadLine();
                    match = RequireMatch(manifestLine, "^\\s*(.+?)\\s*$");

                    variant.Url = match.Groups[1].Value;
                    if (!System.Uri.IsWellFormedUriString(variant.Url, System.UriKind.RelativeOrAbsolute))
                    {
                        throw new System.Exception();
                    }

                    // Handle relative URIs.
                    System.Uri baseUri = new System.Uri(variant.Url, System.UriKind.RelativeOrAbsolute);
                    if (!baseUri.IsAbsoluteUri)
                    {
                        System.Uri sourceUri = new System.Uri(sourceString);
                        string[] segments = sourceUri.Segments;
                        segments[segments.Length - 1] = variant.Url;
                        System.UriBuilder builder = new System.UriBuilder(sourceUri);
                        builder.Path = string.Join("", segments);
                        variant.Url = builder.ToString();
                    }

                    variants.Add(variant);
                }
            }
            catch (System.Exception) { }

            return variants;
        }
    }
}
