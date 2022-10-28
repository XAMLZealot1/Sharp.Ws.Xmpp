using System;

namespace XMPP.Net.Extensions
{
    public class ThumbnailEventArgs : EventArgs
    {
        /// <summary>
        /// The file id 
        /// </summary>
        public String FileId { get; private set; }

        /// <summary>
        /// Witdh of the image file 
        /// </summary>
        public int Width{ get; private set; }

        /// <summary>
        /// Height of the image file 
        /// </summary>
        public int Height { get; private set; }

        public ThumbnailEventArgs(String fileId, int width, int height)
        {
            FileId = fileId;
            Width = width;
            Height = height;
        }
    }
}
