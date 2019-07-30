using System.Threading.Tasks;
using System.Collections.Generic;

namespace AvStreamPlugin
{
    public class Playlist
    {
        private VideoDetails activePlaylist = null;
        private List<VideoDetails> pendingPlaylistItems = new List<VideoDetails>();
        private int dequeuedVideos = 0;
        private string nextPageId = "";

        public int BufferAhead { get; set; } = 25;

        public VideoDetails Details
        {
            get
            {
                return activePlaylist;
            }

            set
            {
                if (IsActive)
                {
                    Clear();
                }

                if (value != null && value.Type == VideoType.Playlist)
                {
                    activePlaylist = value;
                }
            }
        }

        public int RemainingVideos
        {
            get { return activePlaylist != null ? activePlaylist.Playlist.ItemCount - dequeuedVideos : 0; }
        }

        public bool IsActive
        {
            get { return activePlaylist != null && RemainingVideos > 0; }
        }

        public void Clear()
        {
            activePlaylist = null;
            pendingPlaylistItems.Clear();
            dequeuedVideos = 0;
            nextPageId = "";
        }

        public async Task<VideoDetails> GetNextPlaylistItem()
        {
            if (!IsActive)
            {
                return null;
            }

            if (pendingPlaylistItems.Count == 0)
            {
                PagedQueryResult result = await VideoSource.QueryPlaylistVideosAsync(activePlaylist, BufferAhead, nextPageId);

                // We add the reversed list so we can pull them off the end of the array and
                // avoid the aggressive copying that comes with removing the front of a list.
                pendingPlaylistItems.AddRange(result.Videos);
                pendingPlaylistItems.Reverse();

                nextPageId = result.NextPageId;
            }

            VideoDetails video = null;
            if (pendingPlaylistItems.Count > 0)
            {
                video = pendingPlaylistItems[pendingPlaylistItems.Count - 1];
                pendingPlaylistItems.RemoveAt(pendingPlaylistItems.Count - 1);
                dequeuedVideos++;
            }

            return video;
        }
    }
}
