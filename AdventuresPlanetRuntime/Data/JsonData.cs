using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace AdventuresPlanetRuntime.Data
{
    public class JsonData
    {
        [DataContract]
        public class PodcastRoot
        {
            [DataMember(Name = "time")]
            public long time { get; set; }
            [DataMember(Name = "podcast")]
            public List<PodcastItem> list { get; set; }
        }
        [DataContract]
        public class RecensioniRoot
        {
            [DataMember(Name = "time")]
            public long time { get; set; }
            [DataMember(Name = "avventure")]
            public List<RecensioneItem> list { get; set; }
        }
        [DataContract]
        public class SoluzioniRoot
        {
            [DataMember(Name = "time")]
            public long time { get; set; }
            [DataMember(Name = "avventure")]
            public List<SoluzioneItem> list { get; set; }
        }
        [DataContract]
        public class GallerieRoot
        {
            [DataMember(Name = "time")]
            public long time { get; set; }
            [DataMember(Name = "avventure")]
            public List<GalleriaItem> list { get; set; }
        }
    }
}
