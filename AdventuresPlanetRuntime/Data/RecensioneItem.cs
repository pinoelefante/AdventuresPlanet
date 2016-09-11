using SQLite.Net.Attributes;
using System;
using System.ComponentModel;
using System.Linq;
using System.Runtime.Serialization;
using Utils;

namespace AdventuresPlanetRuntime.Data
{
    [DataContract]
    public class RecensioneItem : PaginaContenuti
    {
        private string _vt = string.Empty, _vut = string.Empty;
        [DataMember(Name = "voto")]
        public string VotoText { get { return _vt; } set { Set(ref _vt, value); } }
        public string VotoUtentiText { get { return _vut; } set { Set(ref _vut, value); } }
        public string TestoBreve { get; set; } = string.Empty;
        [Ignore]
        public bool IsRecensioneBreve
        {
            get
            {
                if (string.IsNullOrEmpty(Testo) && !string.IsNullOrEmpty(TestoBreve))
                    return true;
                return false;
            }
        }
        [Ignore]
        public override bool IsVideo
        {
            get
            {
                string par = UrlUtils.GetUrlParameterValue(Link, "cont");//getUrlParameter("cont");
                if (par != null && (par.CompareTo("Video%20Recensione") == 0 || par.CompareTo("Video Recensione")==0))
                    return true;
                return false;
            }
        }
        [Ignore]
        public int VotoInt
        {
            get
            {
                if (!string.IsNullOrEmpty(VotoText))
                {
                    string number = VotoText;
                    if (number.Contains('/'))
                        number = number.Split(new char[] {'/'})[0];
                    int res = 0;
                    if(Int32.TryParse(number, out res))
                    {
                        return res;
                    }
                }
                return 0;
            }
        }
    }
}
