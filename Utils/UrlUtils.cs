using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Utils
{
    public class UrlUtils
    {
        public static string GetUrlParameterValue(string url, string parameter)
        {
            string[] parametri = url.Substring(url.IndexOf('?')+1).Replace("&amp;", "&").Split(AmpersandSplit);
            for (int i = 0; i < parametri.Length; i++)
            {
                string[] kv = parametri[i].Split(EqualSplit);
                if (kv[0].ToLower().CompareTo(parameter) == 0)
                    return kv[1];
            }
            return null;
        }
        private readonly static char[] AmpersandSplit = new char[] { '&' };
        private readonly static char[] EqualSplit = new char[] { '=' };
        public static Dictionary<string, string> GetUrlParameters(string query)
        {
            var res = query.Split(AmpersandSplit, StringSplitOptions.RemoveEmptyEntries);
            Dictionary<string, string> parameters = new Dictionary<string, string>(res.Length);
            foreach (var item in res)
            {
                var kv = item.Split(EqualSplit);
                parameters.Add(kv[0], WebUtility.UrlDecode(kv[1]));
            }
            return parameters;
        }
        public static string GetUrlParameterValue(Uri Link, string parameter)
        {
            return GetUrlParameterValue(Link.AbsoluteUri, parameter);
        }
        public static bool UrlHasParameter(Uri Link, string parameter)
        {
            string[] parametri = Link.Query.Replace("&amp;", "&").Substring(1).Split(new char[] { '&' });
            for (int i = 0; i < parametri.Length; i++)
            {
                string[] kv = parametri[i].Split(new char[] { '=' });
                if (kv[0].ToLower().CompareTo(parameter) == 0)
                    return true;
            }
            return false;
        }
        public static string GetFilenameFromUrl(string url)
        {
            return url.Substring(url.LastIndexOf('/') + 1);
        }
        public static bool IsDomain(string url, string domain, bool ignoreWWW = true)
        {
            if (Uri.IsWellFormedUriString(url, UriKind.Absolute))
            {
                var domainCompare = domain.ToLower();
                var host = new Uri(url).Host.ToLower();
                if (ignoreWWW)
                {
                    domain = domainCompare.Replace("www.", "");
                    host = host.Replace("www.", "");
                }
                return host.CompareTo(domain) == 0;
            }
            return false;
        }
    }
}
