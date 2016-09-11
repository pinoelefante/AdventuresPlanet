using SQLite.Net.Attributes;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;

namespace AdventuresPlanetRuntime.Data
{
    [DataContract]
    public class PodcastItem
    {
        private string _titolo = string.Empty;
        [DataMember(Name = "titolo")]
        public string Titolo { get { return _titolo; } set { _titolo = value; estraiDati(); } }
        [Ignore]
        public string TitoloBG
        {
            get
            {
                if (Titolo.StartsWith("Calavera Cafè "))
                {
                    return Titolo.Substring("Calavera Cafè ".Length);
                }
                return Titolo;
            }
        }
        [DataMember(Name = "pubData")]
        public string Data { get; set; } = string.Empty;
        [DataMember(Name = "link")]
        [PrimaryKey]
        public string Link { get; set; } = string.Empty;
        [DataMember(Name = "immagine")]
        public string Immagine { get; set; } = string.Empty;
        [Ignore]
        public string Filename
        {
            get
            {
                return Link.Substring(Link.LastIndexOf('/') + 1);
            }
        }
        [Ignore]
        public int Stagione { get; set; }
        [Ignore]
        public int Episodio { get; set; }
        public override string ToString()
        {
            return Titolo;
        }
        [DataMember(Name = "descrizione")]
        public string Descrizione { get; set; } = string.Empty;
        private void estraiDati()
        {
            //[0-9]{1,2}x[0-9]{1,2}
            string pat = "[0-9]{1,2}x[0-9]{1,2}";
            Regex r = new Regex(pat, RegexOptions.IgnoreCase);
            Match m = r.Match(Titolo);
            if (m.Success)
            {
                Group g = m.Groups[0];
                //Debug.WriteLine("Group" + 0 + "='" + g + "'");
                string[] vals = g.Value.Split(new char[] {'x'}, StringSplitOptions.RemoveEmptyEntries);
                if (vals.Length == 2)
                {
                    Stagione = Int32.Parse(vals[0]);
                    Episodio = Int32.Parse(vals[1]);
                    //Debug.WriteLine("S" + stagione + "E" + episodio);
                }
            }
        }
    }
    public class PodcastComparer : IComparer<PodcastItem>
    {
        public int Compare(PodcastItem x, PodcastItem y)
        {
            if (x.Stagione > y.Stagione)
                return 1;
            else if (x.Stagione < y.Stagione)
                return -1;
            else
            {
                if (x.Episodio > y.Episodio)
                    return 1;
                else if (x.Episodio < y.Episodio)
                    return -1;
                else
                    return 0;
            }
        }
    }
}
