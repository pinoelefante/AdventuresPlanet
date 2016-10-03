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
        private readonly static char[] AmpersandSplit = new char[] { '&' };
        private readonly static char[] EqualSplit = new char[] { '=' };
        public static Dictionary<string, string> GetUrlParameters(string link, bool isQuery = false)
        {
            string query = string.Empty;
            Dictionary<string, string> parameters = null;
            link = WebUtility.UrlDecode(link);
            if (isQuery)
                query = link;
            else
                query = link.Substring(link.IndexOf("?") + 1);
            var res = query.Split(AmpersandSplit, StringSplitOptions.RemoveEmptyEntries);
            parameters = new Dictionary<string, string>(res.Length);
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
        public static string GetUrlParameterValue(string url, string parameter)
        {
            var parameters = GetUrlParameters(url);
            if (parameters.ContainsKey(parameter))
                return parameters[parameter];
            return null;
        }
        public static bool UrlHasParameter(Uri Link, string parameter)
        {
            var parameters = GetUrlParameters(Link.AbsoluteUri);
            return parameters.ContainsKey(parameter);
        }
        public static bool UrlHasParameter(string link, string parameter)
        {
            var parameters = GetUrlParameters(link);
            return parameters.ContainsKey(parameter);
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
