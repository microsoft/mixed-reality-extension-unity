using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

using MixedRealityExtension.Core;
namespace MixedRealityExtension.PluginInterfaces
{
    public interface IVideoPlayer
    {
        void Play(VideoStreamDescription description, SoundStateOptions options, float? startTimeOffset);
        void Seek(float startTimeOffset);
        void ApplyMediaStateOptions(SoundStateOptions options);
    }
}
