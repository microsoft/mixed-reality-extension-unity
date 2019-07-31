using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AvStreamPlugin
{
    public static class Subsystem
    {
        public static void Init(string youTubeApiKey)
        {
            YouTubeVideoSource.Init(youTubeApiKey);
        }
    }
}
