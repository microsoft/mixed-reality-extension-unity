// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
namespace MixedRealityExtension
{
    /// <summary>
    /// Modifiable Sound Instance Options
    /// </summary>
    public class SoundStateOptions
    {
        /// <summary>
        /// pitch offset in halftones (0=default, 12=one octave higher, -12=one octave lower)
        /// </summary>
        public float? Pitch;

        /// <summary>
        /// volume multiplier, (0.0-1.0, where 0.0=no sound, 1.0=maximum). Default to 1.0
        /// </summary>
        public float? Volume;

        /// <summary>
        /// repeat the sound when ended, or turn it off after playing once. Default to off
        /// </summary>
        public bool? Looping;

        /// <summary>
        /// the amount that sound pitch is modified when moving towards/away from sound source.
        /// For music and speech, set this to 0, but for regular objects set to 1.0 or higher. Default to 1.0
        /// /// </summary>
        public float? Doppler;

        /// <summary>
        /// For multi-channel sounds (like music), mix audio direction (which speakers to play) for each channel between angle to actor (0.0) and the audio file's channels' original direction (1.0).
        /// Default to 0.5, to give some directional feel, but collapse all channels to sound like mono.
        /// </summary>
        public float? MultiChannelSpread;

        /// <summary>
        /// Sounds will play at full volume until user is this many meters away, and then volume will decrease logarithmically
        /// Default to 1.0. For sound that needs to fill up a large space (like a concert), increase this number.
        /// </summary>
        public float? RolloffStartDistance;

    }
}
