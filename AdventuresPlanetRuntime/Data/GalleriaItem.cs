using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Runtime.Serialization;

namespace AdventuresPlanetRuntime.Data
{
    [DataContract]
    public class GalleriaItem
    {
        [DataMember(Name = "titolo")]
        public string Titolo { get; set; }
        [DataMember(Name = "link")]
        public string IdGalleria { get; set; }
        public int LastPageLoaded { get; set; } = 0;
        public int NumPages { get; set; }
        public bool HasNext { get; set; } = true;
        public void Reset()
        {
            LastPageLoaded = 0;
            HasNext = true;
        }
    }
    public class AdvImage
    {
        public string Thumb { get; set; }
        public string ImageLink { get; set; }
    }
}
