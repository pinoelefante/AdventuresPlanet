using SQLite.Net.Attributes;
using System.Runtime.Serialization;
using Utils;

namespace AdventuresPlanetRuntime.Data
{
    [DataContract]
    public class SoluzioneItem : PaginaContenuti
    {
        [Ignore]
        public override bool IsVideo
        {
            get
            {
                string par = UrlUtils.GetUrlParameterValue(Link, "cont");//getUrlParameter("cont");
                if (par != null && (par.CompareTo("Video%20Soluzione") == 0 || par.CompareTo("Video Soluzione") == 0))
                    return true;
                return false;
            }
        }
    }
}
