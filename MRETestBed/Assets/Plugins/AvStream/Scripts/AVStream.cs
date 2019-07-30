using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Text;

using UnityEngine;
using UnityEngine.UI;
using System.Threading;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace AvStreamPlugin
{
    public struct PlaybackStatistics
    {
        public float AudioUpdateFrequency;
        public float SamplesPerSecond;
        public float VideoUpdateFrequency;
        public float PixelsPerSecond;
    }

	//--------------------------------------------------------------------------------------------------
	[RequireComponent(typeof(AudioSource))]
	public class AVStream : MonoBehaviour
	{
		private static Texture2D linearGrayTexture;

		public event Action<AVStream> OnConnecting;
		public event Action<AVStream> OnConnected;
		public event Action<AVStream, string> OnDisconnected;

		[SerializeField]
		private string defaultConnectionString;

		[SerializeField]
		private VideoFormat requestedVideoFormat = VideoFormat.NV12;

		[SerializeField]
		private int reconnectionTryCount = 3; // -1 for infinite.
		[SerializeField]
		private int reconnectionWaitTimeSeconds = 2;

		private TimeSpan? reconnectionTime = null;
		private Stopwatch reconnectionTimer = new Stopwatch();
		private int reconnectCount = 0;

		private IntPtr videoHandle { get; set; } = IntPtr.Zero;
		private CancellationTokenSource videoConnectionCancellation;
		private Task videoConnectionTask;

        private Coroutine pendingLiveStream = null;

        private AvStreamState state = AvStreamState.Disconnected;

		private TimeSpan audioLatency = TimeSpan.Zero;

		private Playlist playlist = new Playlist();

        private TimeSpan lastPlaybackTime = TimeSpan.FromSeconds(0);

		// Statistics.
		private Stopwatch stopwatch = new Stopwatch();
		private MovingAverage frametimeS = new MovingAverage(64);
		private MovingAverage audioUpdateCount = new MovingAverage(64);
		private MovingAverage audioSampleThroughput = new MovingAverage(64);
		private MovingAverage videoUpdateCount = new MovingAverage(64);
		private MovingAverage pixelThroughput = new MovingAverage(64);

		#region Connection Information and State Management

		/// <summary>
		/// AvStream will select a video variant based on this target vertical resolution if one has
		/// not been explicitly given on connection.
		/// </summary>
		public int TargetVerticalResolution { get; set; } = 720;

		public bool IsConnecting
		{
			get { return state == AvStreamState.Connecting; }
		}

		public bool IsConnected
		{
			get { return state == AvStreamState.Connected; }
		}

		public bool IsStopped
		{
			get { return state == AvStreamState.Disconnected; }
		}

		public string ErrorString { get; private set; } = "";

        public void ConnectToArbitraryString(string url)
        {
			StartNewConnectionTask((ct) => { return ConnectToVideoFromRawUrlTask(ct, url); });
        }

		/// <summary>
		/// Loads a playlist in, but otherwise triggers no action. If we're already playing, we'll
		/// pick up the playlist when the current video is done. If we're not playing, calling
		/// 'Play' will get us going.
		/// </summary>
		public void LoadPlaylist(VideoDetails video)
		{
			playlist.Details = video;
		}

		public void ConnectToVideo(VideoDetails video)
		{
			ConnectToVideo(video, null);
		}

		private void ConnectToVideo(VideoDetails video, VideoVariant variant)
        {
            StartNewConnectionTask((ct) => { return ConnectToVideoTask(ct, video, variant); });
        }

        public void Disconnect()
        {
            // Don't immediately reconnect.
            CancelPendingConnectionTask();

            // This will actually disconnect us.
            CloseVideoHandle();

            // Clear out our video references. This will also prevent reconnect, state change callbacks, etc...
            this.CurrentVideo = null;
            this.CurrentVariant = null;
            this.playlist.Clear();

			// This will trigger notifications, shutdowns, etc...
			SetStreamState(AvStreamState.Disconnected);
        }

        private void CloseVideoHandle()
        {
            if (this.videoHandle != IntPtr.Zero)
            {
                NativeAvStream.DestroyAvFrameProducer(this.videoHandle);
                this.videoHandle = IntPtr.Zero;
                LoadDummyTextures();
            }
        }

        private void CancelPendingConnectionTask()
        {
            if (this.videoConnectionTask != null)
            {
                this.videoConnectionCancellation.Cancel();
                this.videoConnectionCancellation = null;
                this.videoConnectionTask = null;
            }
        }

        #endregion
        #region Stream Information

        private List<Texture2D> dummyTextures = new List<Texture2D>();
        public List<Texture2D> DummyTextures
        {
            get { return dummyTextures; }
            set
            {
                this.dummyTextures.Clear();

                // Make sure that no-matter the input, we always assign the correct number of dummy
                // textures.
                int planes = NativeUtilities.GetPlaneCount(requestedVideoFormat);
                for (int i = 0; i < planes; ++i)
                {
                    // Take from the supplied list if it makes sense. Otherwise assign a default
                    // value.
                    if (value != null && i < value.Count && value[i] != null)
                    {
                        dummyTextures.Add(value[i]);
                    }
                    // To support YUV (in an admitedly lazy way) supply black on the Y and
                    // linear-gray on the UV. This produces a black visual for all currently
                    // supported image formats.
                    else if (i == 0)
                    {
                        dummyTextures.Add(Texture2D.blackTexture);
                    }
                    else
                    {
                        if (linearGrayTexture == null)
                        {
                            linearGrayTexture = new Texture2D(1, 1, TextureFormat.RGBA32, false, true);
                            linearGrayTexture.SetPixel(0, 0, new Color(0.5f, 0.5f, 0.5f, 1.0f));
                            linearGrayTexture.Apply();
                        }

                        dummyTextures.Add(linearGrayTexture);
                    }
                }
            }
        }

        public List<Texture2D> VideoTextures { get; private set; } = new List<Texture2D>();

        public Renderer TargetRenderer { get; private set; } = null;
        public AudioSource TargetAudioSource { get; private set; } = null;

        public VideoDetails CurrentVideo { get; private set; }
        public VideoVariant CurrentVariant { get; private set; }

		public VideoDetails CurrentPlaylist
		{
			get { return playlist.IsActive ? playlist.Details : null; }
		}

        public AudioDesc AudioDesc { get; private set; } = new AudioDesc();
        public VideoDesc VideoDesc { get; private set; } = new VideoDesc();

        public PlaybackStatistics PlaybackStats { get; private set; } = new PlaybackStatistics();

        #endregion
        #region Playback Controls and State

        public void Play()
        {
            if (IsConnected && IsPaused)
            {
                NativeAvStream.SetPaused(this.videoHandle, false);
            }
            else if (IsStopped)
            {
				// If we have a playlist and we're stopped, either someone loaded a playlist without
				// playing or we hit a serious issue playing the last video. Continue the playlist,
				// rather than trying to connect to the loaded video. Otherwise, connect back to any
				// currently loaded video!
				if (this.playlist.IsActive)
				{
					ConnectToVideo(null, null);
				}
				else if (this.CurrentVideo != null)
				{
					ConnectToVideo(this.CurrentVideo, this.CurrentVariant);
				}
            }
        }

        public void Pause()
        {
            if (IsConnected && !IsPaused)
            {
                NativeAvStream.SetPaused(this.videoHandle, true);
            }
        }

        public void Seek(TimeSpan time)
        {
            if (IsConnected && CurrentVideo.Type == VideoType.Recording)
            {
                if (time >= CurrentVideo.Recording.Duration)
                {
                    lastPlaybackTime = CurrentVideo.Recording.Duration;
                    SetStreamState(AvStreamState.Disconnected);
                }
                else
                {
                    double seconds = Math.Max(time.TotalSeconds, 0);
                    lastPlaybackTime = TimeSpan.FromSeconds(seconds);
                    NativeAvStream.SeekToTime(this.videoHandle, seconds);
                }
            }
        }

        public bool IsPaused
        {
            get { return IsConnected && NativeAvStream.GetPaused(this.videoHandle); }
        }

        public bool IsPlaying
        {
            // While we're only really pushing AV data in the connected state, everything but
            // disconnected represents some state of trying to make something play.
            get { return IsConnected && !IsPaused; }
        }

        public TimeSpan PlaybackTime
        {
            get
            {
                // If we're playing or we successfully completed, then report the last known playback time.
                if (IsConnected || (this.CurrentVideo != null && string.IsNullOrWhiteSpace(ErrorString)))
                {
                    return lastPlaybackTime;
                }
                return TimeSpan.FromSeconds(0);
               
            }
        }

        public float PlaybackRatio
        {
            get
            {
                if (IsConnected)
                {
                    if (this.CurrentVideo.Type == VideoType.Recording)
                    {
                        double duration = this.CurrentVideo.Recording.Duration.TotalSeconds;
                        if (duration > 0)
                        {
                            return (float)(PlaybackTime.TotalSeconds / duration);
                        }
                    }
                    else if (this.CurrentVideo.Type == VideoType.LiveStream)
                    {
                        return 1;
                    }
                }
                else if (this.CurrentVideo != null && string.IsNullOrWhiteSpace(ErrorString))
                {
                    // Return '1' to indicate a successfully completed video.
                    return 1;
                }

                return 0;
            }
        }

		#endregion
		#region Unity Hooks

		private void Awake()
        {
            // Ensure that the native logging functions are set before anything else
            NativeUtilities.EnsurePluginInitialized();
        }

        private void Start()
        {
			InitializeVideo();
			InitializeAudio();

			// If a default stream is set, connect to it now.
			if (!string.IsNullOrEmpty(this.defaultConnectionString))
            {
                this.defaultConnectionString = this.defaultConnectionString.Trim();
                ConnectToArbitraryString(this.defaultConnectionString);
            }

            AvStreamPluginUpdate.AddRef();
        }

        private void OnDestroy()
        {
			AvStreamPluginUpdate.DecRef();
            CloseVideoHandle();
        }

        private void Update()
        {
			// Monitor our outstanding connection task and clean it up when it is done.
			if (this.videoConnectionTask != null && this.videoConnectionTask.IsCompleted)
			{
				if (videoConnectionTask.Exception != null)
				{
					UnityEngine.Debug.LogException(videoConnectionTask.Exception);
				}

				this.videoConnectionCancellation = null;
				this.videoConnectionTask = null;
			}

			// If we have a videoHandle, we need to pump it for audio and video updates to make them
			// visible to the user. This is where we spend most of our time.
			if (this.videoHandle != IntPtr.Zero)
            {
                // A negative queue depth requests that audio be disabled. If we don't inform the
                // producer that we're not draining audio, we'll see the content queue fill and
                // playback will freeze.
                bool audioIsPlaying = this.TargetAudioSource != null && this.TargetAudioSource.enabled && this.TargetAudioSource.isPlaying;
                float audioLatency = audioIsPlaying
                    ? (float)this.audioLatency.TotalSeconds
                    : -1;

                NativeAvStream.UpdateAvFrameProducer(this.videoHandle, audioLatency);
                lastPlaybackTime = TimeSpan.FromSeconds(NativeAvStream.GetVideoTime(this.videoHandle));
                SetStreamState(NativeAvStream.GetConnectionState(this.videoHandle));
                UpdateStatistics();
            }

            // If we have no video handle and no task, then something has gone wrong and we've
            // disconnected. If reconnect logic is relevant, it will trigger inside of the state
            // change handler.
            if (this.videoHandle == IntPtr.Zero && this.videoConnectionTask == null)
            {
                SetStreamState(AvStreamState.Disconnected);
            }
        }

		#endregion

		private void InitializeVideo()
		{
			this.TargetRenderer = GetComponentInChildren<Renderer>();

			// If a texture is already assigned, store it into our dummy texture set.
			List<Texture2D> defaultDummyTextures = new List<Texture2D>();
			if (this.TargetRenderer != null && this.TargetRenderer.material != null)
			{
				switch (requestedVideoFormat)
				{
					case VideoFormat.R8:
					case VideoFormat.RG16:
					case VideoFormat.BGRA32:
					case VideoFormat.RGBA32:
						defaultDummyTextures.Add(this.TargetRenderer.material.GetTexture("_MainTex") as Texture2D);
						break;
					case VideoFormat.NV12:
						defaultDummyTextures.Add(this.TargetRenderer.material.GetTexture("_Y") as Texture2D);
						defaultDummyTextures.Add(this.TargetRenderer.material.GetTexture("_UV") as Texture2D);
						break;
					case VideoFormat.YUV420P:
						defaultDummyTextures.Add(this.TargetRenderer.material.GetTexture("_Y") as Texture2D);
						defaultDummyTextures.Add(this.TargetRenderer.material.GetTexture("_U") as Texture2D);
						defaultDummyTextures.Add(this.TargetRenderer.material.GetTexture("_V") as Texture2D);
						break;
				}
			}
			DummyTextures = defaultDummyTextures;
			LoadDummyTextures();
		}

		private void InitializeAudio()
		{
			this.TargetAudioSource = GetComponentInChildren<AudioSource>();

			// Unity expects a very specific format for OnAudioFilterRead, since it's post-decode.
			// We need to make a manual request down to the AV code to do the decode for us. If we
			// don't have an audio source, we do nothing, which will cause the native plugin to
			// disable audio processing entirely.
			if (this.TargetAudioSource != null)
			{
				var config = AudioSettings.GetConfiguration();

				// The audio desc is fixed by unity. Set it up now.
				AudioDesc desc = new AudioDesc();
				desc.FormatTag = 3; // WAVE_FORMAT_IEEE_FLOAT. Unity only does floats for "procedural" audio.
				switch (config.speakerMode)
				{
					case AudioSpeakerMode.Raw:
						desc.Channels = 1;
						break;
					case AudioSpeakerMode.Mono:
						desc.Channels = 1;
						break;
					case AudioSpeakerMode.Stereo:
						desc.Channels = 2;
						break;
					case AudioSpeakerMode.Quad:
						desc.Channels = 4;
						break;
					case AudioSpeakerMode.Surround:
						desc.Channels = 5;
						break;
					case AudioSpeakerMode.Mode5point1:
						desc.Channels = 6;
						break;
					case AudioSpeakerMode.Mode7point1:
						desc.Channels = 8;
						break;
					case AudioSpeakerMode.Prologic:
						desc.Channels = 2;
						break;

				}
				desc.SamplesPerSec = config.sampleRate; // Use the native rate.
				desc.BitsPerSample = sizeof(float) * 8; // 8 bits per byte.
				desc.BlockAlign = (short)(desc.Channels * sizeof(float));
				desc.AvgBytesPerSec = desc.SamplesPerSec * desc.BlockAlign;
				this.AudioDesc = desc;

				this.audioLatency = TimeSpan.FromSeconds(config.dspBufferSize / (float)config.sampleRate);

				// Pause audio until we have a video stream.
				if (this.TargetAudioSource.enabled)
				{
					this.TargetAudioSource.Pause();
				}
			}
		}

		private void SetStreamState(AvStreamState state)
        {
            if (this.state == state)
            {
                return;
            }

            this.state = state;

            if (this.state == AvStreamState.Disconnected)
            {
				// We're done. Collect error info and clean up our handle object.
				StringBuilder buffer = new StringBuilder(256);
				if (this.videoHandle != IntPtr.Zero)
                {
					NativeAvStream.GetDisconnectReason(this.videoHandle, buffer, buffer.Capacity);
					CloseVideoHandle();
                }
				this.ErrorString = buffer.ToString();

				// Log the error, if there is one.
				bool errorState = !string.IsNullOrWhiteSpace(this.ErrorString);
				if (errorState)
				{
					UnityEngine.Debug.LogErrorFormat("Disconnected from AVStream with error: {0}", this.ErrorString);
				}
				else
				{
					UnityEngine.Debug.Log("Video gracefully completed.");
				}

				// Pause our audio to avoid unnecessary processing.
				if (this.TargetAudioSource != null && this.TargetAudioSource.enabled)
                {
                    this.TargetAudioSource.Pause();
                }

                // We need something to reconnect to and a case where we *want* to reconnect
                // (errors, basically. This isn't a "loop video" feature). 
                if (errorState && this.reconnectCount != 0)
                {
                    UnityEngine.Debug.LogWarning("An error has occured durring video playback. Attempting reconnection!");
					StartNewConnectionTask((ct) => { return ReconnectToCurrentVideoTask(ct); });
                    this.reconnectCount--;
                }
                else if (playlist.IsActive)
                {
                    UnityEngine.Debug.Log("Connecting to next video in playlist!");
					StartNewConnectionTask((ct) => { return ConnectToVideoTask(ct, null, null); });
                }
            }
            else if (this.state == AvStreamState.Connected)
            {
				// If we manage to connect, we must have a valid connection. Reset our reconnect count.
				this.reconnectCount = this.reconnectionTryCount;

				if (this.TargetAudioSource != null && this.TargetAudioSource.enabled)
                {
                    this.TargetAudioSource.Play();
                }

				CreateAndRegisterVideoTexturesForVariant(this.CurrentVariant);

                if (reconnectionTime != null)
                {
                    UnityEngine.Debug.LogFormat("Reconnection successful. Seeking to correct time: {0}", string.Format("{0:%h\\:mm\\:ss}", reconnectionTime.Value));

                    // Seek to the time the video was at before we disconnected. Add a small buffer incase there is a bad part of the video.
                    Seek(reconnectionTime.Value + TimeSpan.FromSeconds(5));
                    reconnectionTime = null;
                }
            }
            else if (this.state == AvStreamState.Connecting)
            {
                this.reconnectionTimer.Stop();
                this.reconnectionTimer.Reset();
                this.ErrorString = "";
            }

            FireUpdate();
        }

        private void LoadDummyTextures()
        {
            VideoDesc desc = new VideoDesc();
            desc.Width = 2;
            desc.Height = 2;
            desc.Format = requestedVideoFormat;
            VideoDesc = desc;

            this.VideoTextures.Clear();
            for (int i = 0; i < this.DummyTextures.Count; ++i)
            {
                this.VideoTextures.Add(this.DummyTextures[i]);
            }

            AssignVideoTextures(1);
        }

        private void CreateAndRegisterVideoTexturesForVariant(VideoVariant v)
        {
            VideoDesc desc = new VideoDesc();
            desc.Width = (uint)v.Width;
            desc.Height = (uint)v.Height;
            desc.Format = requestedVideoFormat;
            VideoDesc = desc;

            this.VideoTextures.Clear();
            switch (requestedVideoFormat)
            {
                case VideoFormat.R8:
                case VideoFormat.RG16:
                case VideoFormat.BGRA32:
                case VideoFormat.RGBA32:
                    this.VideoTextures.Add(new Texture2D(v.Width, v.Height, NativeUtilities.GetTextureFormat(requestedVideoFormat), false, false));
                    break;
                case VideoFormat.NV12:
                    if (v.Width % 2 != 0 || v.Height % 2 != 0)
                    {
                        throw new Exception("YUV textures must have even dimensions.");
                    }
                    this.VideoTextures.Add(new Texture2D(v.Width, v.Height, TextureFormat.R8, false, true));
                    this.VideoTextures.Add(new Texture2D(v.Width / 2, v.Height / 2, TextureFormat.RG16, false, true));
                    break;
                case VideoFormat.YUV420P:
                    if (v.Width % 2 != 0 || v.Height % 2 != 0)
                    {
                        throw new Exception("YUV textures must have even dimensions.");
                    }
                    this.VideoTextures.Add(new Texture2D(v.Width, v.Height, TextureFormat.R8, false, true));
                    this.VideoTextures.Add(new Texture2D(v.Width / 2, v.Height / 2, TextureFormat.R8, false, true));
                    this.VideoTextures.Add(new Texture2D(v.Width / 2, v.Height / 2, TextureFormat.R8, false, true));
                    break;
            }

            for (int i = 0; i < this.VideoTextures.Count; ++i)
            {
                // Reset all pixels color to transparent. This is slow, but I don't see another way
                // to avoid seconds of bright pink texture.
                byte value = (byte)((i == 0) ? 0x00 : 0x7F);
                byte[] pixels = this.VideoTextures[i].GetRawTextureData();
                for (int j = 0; j < pixels.Length; j++)
                {
                    pixels[j] = value;
                }
                this.VideoTextures[i].LoadRawTextureData(pixels);
                this.VideoTextures[i].Apply();
            }

            AssignVideoTextures(-1);

            for (int i = 0; i < this.VideoTextures.Count; ++i)
            {
                NativeUpdateManager.RegisterTexture(
                    this.videoHandle,
                    this.VideoTextures[i].GetNativeTexturePtr(),
                    i,
                    NativeUtilities.VideoDescFromTexture(this.VideoTextures[i]));
            }
        }

        private void AssignVideoTextures(float yScale)
        {
            if (this.TargetRenderer == null || this.TargetRenderer.material == null)
            {
                return;
            }

            switch (requestedVideoFormat)
            {
                case VideoFormat.R8:
                case VideoFormat.RG16:
                case VideoFormat.BGRA32:
                case VideoFormat.RGBA32:
                    this.TargetRenderer.material.SetTexture("_MainTex", this.VideoTextures[0]);
                    this.TargetRenderer.material.SetTextureScale("_MainTex", new Vector2(1, yScale));
                    break;
                case VideoFormat.NV12:
                    this.TargetRenderer.material.SetTexture("_Y", this.VideoTextures[0]);
                    this.TargetRenderer.material.SetTexture("_UV", this.VideoTextures[1]);
                    this.TargetRenderer.material.SetTextureScale("_Y", new Vector2(1, yScale));
                    this.TargetRenderer.material.SetTextureScale("_UV", new Vector2(1, yScale));
                    break;
                case VideoFormat.YUV420P:
                    this.TargetRenderer.material.SetTexture("_Y", this.VideoTextures[0]);
                    this.TargetRenderer.material.SetTexture("_U", this.VideoTextures[1]);
                    this.TargetRenderer.material.SetTexture("_V", this.VideoTextures[2]);
                    this.TargetRenderer.material.SetTextureScale("_Y", new Vector2(1, yScale));
                    this.TargetRenderer.material.SetTextureScale("_U", new Vector2(1, yScale));
                    this.TargetRenderer.material.SetTextureScale("_V", new Vector2(1, yScale));
                    break;
            }
        }

        private void OnAudioFilterRead(float[] dst, int channels)
        {
            if (channels == this.AudioDesc.Channels)
            {
                NativeUpdateManager.DoAudioUpdate(this.videoHandle, this.AudioDesc, dst, dst.Length);
            }
            else
            {
                UnityEngine.Debug.LogWarning("Channel count mismatch. Cannot surface audio.");
            }
        }

        private void UpdateStatistics()
        {
            // If we don't have a valid audio stream, there is nothing to do.
            if (this.state != AvStreamState.Connected || this.videoHandle == IntPtr.Zero)
            {
                stopwatch.Reset();
                return;
            }

            if (this.stopwatch.IsRunning)
            {
                UInt32 audioUpdateCount = 0;
                UInt32 audioSampleThroughput = 0;
                UInt32 videoUpdateCount = 0;
                UInt32 pixelThroughput = 0;
                NativeUpdateManager.GetStatUpdates(this.videoHandle, ref audioUpdateCount, ref audioSampleThroughput, ref videoUpdateCount, ref pixelThroughput);

                this.audioUpdateCount.AddSample(audioUpdateCount);
                this.audioSampleThroughput.AddSample(audioSampleThroughput);

                this.videoUpdateCount.AddSample(videoUpdateCount);
                this.pixelThroughput.AddSample(pixelThroughput);

                this.frametimeS.AddSample((float)this.stopwatch.Elapsed.TotalSeconds);
                this.stopwatch.Reset();
            }
            this.stopwatch.Start();

            float totalSeconds = this.frametimeS.Total;

            PlaybackStatistics stats;

            float totalVideoUpdateCount = this.videoUpdateCount.Total;
            float totalPixelThroughput = this.pixelThroughput.Total;
            stats.VideoUpdateFrequency = (totalSeconds != 0) ? (totalVideoUpdateCount / totalSeconds) : 0;
            stats.PixelsPerSecond = (totalSeconds != 0) ? (totalPixelThroughput / totalSeconds) : 0;

            float totalAudioUpdateCount = this.audioUpdateCount.Total;
            float totalSampleThroughput = this.audioSampleThroughput.Total;
            stats.AudioUpdateFrequency = (totalSeconds != 0) ? (totalAudioUpdateCount / totalSeconds) : totalSeconds;
            stats.SamplesPerSecond = (totalSeconds != 0) ? (totalSampleThroughput / totalSeconds) : 0;

            PlaybackStats = stats;
        }

        private void FireUpdate()
        {
            switch (state)
            {
                case AvStreamState.Connecting:
                    OnConnecting?.Invoke(this);
                    break;
                case AvStreamState.Connected:
                    OnConnected?.Invoke(this);
                    break;
                case AvStreamState.Disconnected:
                    OnDisconnected?.Invoke(this, ErrorString);
                    break;
            }
        }

        private delegate Task StartNewConnectionDelegate(CancellationToken ct);
		private void StartNewConnectionTask(StartNewConnectionDelegate startTask)
		{
            CancelPendingConnectionTask();

            if (pendingLiveStream != null)
            {
                StopCoroutine(pendingLiveStream);
                pendingLiveStream = null;
            }

			CloseVideoHandle();
			SetStreamState(AvStreamState.Connecting);

			videoConnectionCancellation = new CancellationTokenSource();
			videoConnectionTask = startTask.Invoke(videoConnectionCancellation.Token);
		}
        
        private async Task ConnectToVideoTask(CancellationToken ct, VideoDetails video, VideoVariant variant = null, TimeSpan? reconnectionTime = null)
        {
			// Playlist management. If we have an incoming video, we either need to clear the old
			// playlist or setup a new one. Be careful about reconnects. We don't want to clear
			// the playlist in this case, despite getting an explicit connection.
			if (video != null)
            {
                if (video.Type == VideoType.Playlist)
                {
                    playlist.Details = video;
                    video = null;
                }
                else
                {
                    // Be careful not to clear the playlist if we're reconnecting. Otherwise overwrite it.
                    if (reconnectionTime == null)
                    {
                        playlist.Clear();
                    }
                }
            }

            // Playlists pass in null videos to this function to indicate an item should be pulled
            // off of the queue. Do that now if there is an active playlist.
            if (video == null && playlist.IsActive)
            {
                // Select the next item off the playlist. If we're searching for a specific item,
                // select until we run out or we find a match.
                string targetPlaylistId = playlist.Details.Playlist.StartId;
                do
                {
					video = await playlist.GetNextPlaylistItem();
					ct.ThrowIfCancellationRequested();
				}
                while (video != null && !string.IsNullOrWhiteSpace(targetPlaylistId) && video.Id != targetPlaylistId);

                // We're done. Don't keep searching.
                playlist.Details.Playlist.StartId = null;

                if (playlist.RemainingVideos == 0)
                {
                    playlist.Clear();
                }
            }

			if (video != null)
            {
				// Don't try to connect to offline livestreams, but we can try to load some basic
				// details and even schedule a reconnect if we have a start time.
				if (video.Type == VideoType.LiveStream && !video.LiveStream.Online)
				{
					DateTime now = DateTime.Now;
					DateTime streamStart = video.LiveStream.StartTime.ToLocalTime();
					if (streamStart > now)
					{
						// Add a minute to make sure we don't connect too early.
						TimeSpan timeUntilStart = (streamStart - now) + TimeSpan.FromMinutes(1);
						pendingLiveStream = StartCoroutine(WaitForPendingLiveStream(timeUntilStart, video.Source, video.Id));
					}

					// Clean everything out, but report a loaded video.
					this.CurrentVideo = video;
					this.CurrentVariant = null;
					this.reconnectionTime = null;
					CloseVideoHandle();
					playlist.Clear();
				}
				else
				{
					// Pick a variant if one hasn't been selected already.
					if (variant == null)
					{
						variant = await VideoSource.SelectVariantAsync(video, TargetVerticalResolution);
						ct.ThrowIfCancellationRequested();
					}

					IntPtr result = IntPtr.Zero;
					if (variant != null)
					{
						AudioDesc audioDesc = this.AudioDesc;
						VideoDesc videoDesc = new VideoDesc();
						videoDesc.Width = variant.Width > 0 ? (uint)variant.Width : 640;
                        videoDesc.Height = variant.Height > 0 ? (uint)variant.Height : 480;
						videoDesc.Format = requestedVideoFormat;
						string url = variant.Url;
						result = await Task.Run(() =>
						{
							return NativeAvStream.CreateVideoStream(url, ref videoDesc, ref audioDesc);
						});

						if (ct.IsCancellationRequested && result != IntPtr.Zero)
						{
							NativeAvStream.DestroyAvFrameProducer(result);
							result = IntPtr.Zero;
						}
						ct.ThrowIfCancellationRequested();
					}

					if (result != IntPtr.Zero)
					{
						UnityEngine.Debug.LogFormat("Connecting to video: {0} / {1}", video.Title, variant.Name);

						this.CurrentVideo = video;
						this.CurrentVariant = variant;
						this.videoHandle = result;
						this.reconnectionTime = reconnectionTime;
					}
				}
            }

            // It's very possible we land with a null video handle here. Just make sure we're
            // disconnected if we do.
            if (this.videoHandle == IntPtr.Zero)
            {
				SetStreamState(AvStreamState.Disconnected);
            }
        }

		private async Task ReconnectToCurrentVideoTask(CancellationToken ct)
        {
            // Run a timer to prevent reconnecting too aggressively. Once it runs up, we attempt
            // to recreate our connection context. Only do this on subsequent reconnections, so at
            // least our first reconnection attempt happens quickly.
            if (reconnectionTime != null)
            {
                await Task.Delay((int)(this.reconnectionWaitTimeSeconds * 1000));
				ct.ThrowIfCancellationRequested();
			}

			await ConnectToVideoTask(ct, this.CurrentVideo, this.CurrentVariant, lastPlaybackTime);
			ct.ThrowIfCancellationRequested();
		}

        private async Task ConnectToVideoFromRawUrlTask(CancellationToken ct, string url)
        {
            VideoDetails result = await VideoSource.GetVideoFromStringAsync(url);
			await ConnectToVideoTask(ct, result);
			ct.ThrowIfCancellationRequested();
		}

        /// <summary>
        /// After time has passed, connect to the current video again, assuming that the video has
        /// no changed and the player is not already running. The intention is this will be used to
        /// connect to livestreams once they start.
        /// </summary>
        private IEnumerator WaitForPendingLiveStream(TimeSpan time, VideoSourceType type, string id)
        {
            yield return new WaitForSecondsRealtime((float)time.TotalSeconds);

            if (this.IsStopped &&
                this.CurrentVideo != null &&
                this.CurrentVideo.Source == type &&
                this.CurrentVideo.Id == id &&
                this.CurrentVideo.Type == VideoType.LiveStream)
            {
                // Tag the livestream as online, so we don't get stuck in a loop.
                this.CurrentVideo.LiveStream.Online = true;
                ConnectToVideo(this.CurrentVideo);
            }
        }
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(AVStream))]
    public class AVStreamInspector : Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            AVStream script = (AVStream)target;
            AudioSource source = script.GetComponentInChildren<AudioSource>();
            if (source == null)
            {
                EditorGUILayout.HelpBox("Audio will not play without an AudioSource component attached.", MessageType.Warning);
            }

            Renderer renderer = script.GetComponentInChildren<Renderer>();
            if (renderer == null)
            {
                EditorGUILayout.HelpBox("Video will not play without a Renderer component attached.", MessageType.Warning);
            }
        }
    }
#endif
}
