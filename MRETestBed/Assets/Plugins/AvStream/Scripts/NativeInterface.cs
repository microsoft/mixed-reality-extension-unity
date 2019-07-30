using System;
using System.Text;
using System.Runtime.InteropServices;
using UnityEngine;

namespace AvStreamPlugin
{
    public enum VideoFormat
    {
        R8,      // 1 plane: r. 8bits per pixel. Probably shouldn't be used directly as a requested video format. Meant to help with multi-planar formats.
        RG16,     // 1 plane: rg. 16 bits per pixel. Probably shouldn't be used directly as a requested video format. Meant to help with multi-planar formats.
        RGBA32,   // 1 plane: rgba. 32 bits per pixel.
        BGRA32,   // 1 plane: bgra. 32 bits per pixel.
        YUV420P, // 3 planes: y, u, and v. 12 bits per pixel.
        NV12,    // 2 planes: y and uv. 12 bits per pixel.
    };

    public enum AvStreamState
    {
        Connecting,
        Connected,
        Disconnected,
    }

    public enum RdControlLevel{
        None,
        Viewer,
        Controller
    }

    public struct AudioDesc
    {
        public Int16 FormatTag;
        public Int16 Channels;
        public Int32 SamplesPerSec;
        public Int32 AvgBytesPerSec;
        public Int16 BitsPerSample;
        public Int16 BlockAlign;

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
            {
                return false;
            }
            return obj is AudioDesc && this == (AudioDesc)obj;
        }

        public override int GetHashCode()
        {
            return 0;
        }

        public static bool operator ==(AudioDesc ad1, AudioDesc ad2)
        {
            return !(ad1 != ad2);
        }

        public static bool operator !=(AudioDesc ad1, AudioDesc ad2)
        {
            return ad1.FormatTag != ad2.FormatTag ||
                ad1.Channels != ad2.Channels ||
                ad1.SamplesPerSec != ad2.SamplesPerSec ||
                ad1.AvgBytesPerSec != ad2.AvgBytesPerSec ||
                ad1.BitsPerSample != ad2.BitsPerSample ||
                ad1.BlockAlign != ad2.BlockAlign;
        }
    }

    public struct VideoRect
    {
        public Int32 X;
        public Int32 Y;
        public Int32 Width;
        public Int32 Height;
    }

    public struct VideoDesc
    {
        public VideoFormat Format;
        public UInt32 Width;
        public UInt32 Height;

        public static bool operator ==(VideoDesc vd1, VideoDesc vd2)
        {
            return !(vd1 != vd2);
        }

        public static bool operator !=(VideoDesc vd1, VideoDesc vd2)
        {
            return vd1.Format != vd2.Format ||
                vd1.Width != vd2.Width ||
                vd1.Height != vd2.Height;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
            {
                return false;
            }
            return obj is VideoDesc && this == (VideoDesc)obj;
        }

        public override int GetHashCode()
        {
            return 0;
        }
    }

    public struct CursorPosition{
        public Int16 x;
        public Int16 y;

        public void SetPosition(float x, float y)
        {
            this.x = (Int16) x;
            this.y = (Int16) y;
        }
    }

    internal class NativeUtilities
    {
        public static int GetPlaneCount(VideoFormat videoFormat)
        {
            switch (videoFormat)
            {
                default:
                case VideoFormat.R8:
                case VideoFormat.RG16:
                case VideoFormat.BGRA32:
                case VideoFormat.RGBA32:
                    return 1;
                case VideoFormat.NV12:
                    return 2;
                case VideoFormat.YUV420P:
                    return 3;
            }
        }

        public static VideoFormat GetVideoFormat(TextureFormat tf)
        {
            switch (tf)
            {
                case TextureFormat.R8:
                    return VideoFormat.R8;
                case TextureFormat.RG16:
                    return VideoFormat.RG16;
                case TextureFormat.BGRA32:
                    return VideoFormat.BGRA32;
                case TextureFormat.RGBA32:
                    return VideoFormat.RGBA32;
            }
            throw new NotImplementedException();
        }

        public static TextureFormat GetTextureFormat(VideoFormat vf)
        {
            switch (vf)
            {
                case VideoFormat.R8:
                    return TextureFormat.R8;
                case VideoFormat.RG16:
                    return TextureFormat.RG16;
                case VideoFormat.BGRA32:
                    return TextureFormat.BGRA32;
                case VideoFormat.RGBA32:
                    return TextureFormat.RGBA32;
            }
            throw new NotImplementedException();
        }


        public static VideoDesc VideoDescFromTexture(Texture2D t)
        {
            return new VideoDesc()
            {
                Format = GetVideoFormat(t.format),
                Width = (uint)t.width,
                Height = (uint)t.height,
            };
        }

        private delegate void LogCallback(string str);

        [AOT.MonoPInvokeCallback(typeof(NativeUtilities.LogCallback))]
        public static void LogDebugCallback(string str)
        {
            Debug.LogFormat("AvStreamPlugin: {0}", str);
        }

        [AOT.MonoPInvokeCallback(typeof(NativeUtilities.LogCallback))]
        public static void LogWarningCallback(string str)
        {
            Debug.LogWarningFormat("AvStreamPlugin: {0}", str);
        }

        [AOT.MonoPInvokeCallback(typeof(NativeUtilities.LogCallback))]
        public static void LogErrorCallback(string str)
        {
            Debug.LogErrorFormat("AvStreamPlugin: {0}", str);
        }

        private static bool pluginInitialized = false;

        public static void EnsurePluginInitialized()
        {
            if (!pluginInitialized)
            {
#if UNITY_ANDROID && !UNITY_EDITOR
                // Android doesn't guarantee library load order and we have some pretty specific
                // requirements. This function makes sure everything is loaded in the correct
                // order and gives us better error messages than we would get otherwise as a
                // bonus.
                Debug.Log("AvStreamPlugin: Loading Android libraries...");
                try
                {
                    AndroidJavaClass pluginClass = new AndroidJavaClass("com.microsoft.avstreamplugin.AvStreamPluginLoader");
                    pluginClass.CallStatic("LoadLibraries");
                }
                catch (Exception e)
                {
                    Debug.LogError("AvStreamPlugin: Failed to load Android libraries! AvStreamPlugin cannot function.");
                    throw e;
                }
#endif
                SetLoggingFunctions(LogDebugCallback, LogErrorCallback, LogWarningCallback);

                pluginInitialized = true;
            }
        }

        [DllImport("AvStreamPlugin", CallingConvention = CallingConvention.StdCall)]
        private static extern void SetLoggingFunctions(LogCallback debugLogCallback, LogCallback logErrorCallback, LogCallback logWarningCallback);
    }

    internal class NativeUpdateManager
    {
        [DllImport("AvStreamPlugin")]
        public static extern void RegisterTexture(IntPtr producer, IntPtr texture, int index, VideoDesc desc);

        [DllImport("AvStreamPlugin")]
        public static extern void DoAudioUpdate(IntPtr producer, AudioDesc desc, float[] buffer, int count);

        [DllImport("AvStreamPlugin")]
        public static extern IntPtr GetRenderEventFunc();

        [DllImport("AvStreamPlugin")]
        public static extern void GetStatUpdates(
            IntPtr producer,
            ref UInt32 audioUpdateCount,
            ref UInt32 audioSampleThroughput,
            ref UInt32 videoUpdateCount,
            ref UInt32 videoPixelThroughput);

    }

    internal class NativeAvStream
    {
        [DllImport("AvStreamPlugin")]
        public static extern IntPtr CreateVideoStream(string videoUri, ref VideoDesc requestedVideoFormat, ref AudioDesc requestedAudioFormat);

        [DllImport("AvStreamPlugin")]
        public static extern void DestroyAvFrameProducer(IntPtr producer);

        [DllImport("AvStreamPlugin")]
        public static extern void UpdateAvFrameProducer(IntPtr producer, Single pendingAudioDurationS);

        [DllImport("AvStreamPlugin")]
        public static extern void SeekToTime(IntPtr producer, double seconds);

        [DllImport("AvStreamPlugin")]
        public static extern double GetVideoTime(IntPtr producer);

        [DllImport("AvStreamPlugin")]
        public static extern void SetPaused(IntPtr producer, bool paused);

        [DllImport("AvStreamPlugin")]
        public static extern bool GetPaused(IntPtr producer);

        [DllImport("AvStreamPlugin")]
        public static extern AvStreamState GetConnectionState(IntPtr producer);

        [DllImport("AvStreamPlugin")]
        public static extern void GetDisconnectReason(IntPtr producer, StringBuilder buffer, Int32 size);

        [DllImport("AvStreamPlugin")]
        public static extern VideoDesc GetVideoDesc(IntPtr producer);

        [DllImport("AvStreamPlugin")]
        public static extern AudioDesc GetAudioDesc(IntPtr producer);

        [DllImport("AvStreamPlugin")]
        public static extern IntPtr GetRenderEventFunc();
    }
}
