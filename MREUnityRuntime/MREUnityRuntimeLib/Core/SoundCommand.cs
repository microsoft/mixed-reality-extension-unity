// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
namespace MixedRealityExtension
{
    /// <summary>
    /// Special commands to change the mode of the sound instance?
    /// </summary>
    public enum SoundCommand
    {
        /// <summary>
        /// No command to apply
        /// </summary>
        None,

        /// <summary>
        /// Start a new sound instance
        /// </summary>
        Start,

        /// <summary>
        /// Destroy a sound instance
        /// </summary>
        Stop,

        /// <summary>
        /// Pause a sound instance
        /// </summary>
        Pause,

        /// <summary>
        /// Resume a paused sound instance
        /// </summary>
        Resume,
    }
}
