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
        /// volume multiplier, (0.0-1.0, where 0.0=no sound, 1.0=maximum)
        /// </summary>
        public float? Volume;

        /// <summary>
        /// repeat the sound when ended, or turn it off after playing once
        /// </summary>
        public bool? Looping;
    }
}
