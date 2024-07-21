using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cusede.helpers
{
    public class VideoItem
    {
        public string titulo { get; set; }
        public string url { get; set; }
        public string videoId { get; set; }
        public override string ToString() => titulo;
    }
}
