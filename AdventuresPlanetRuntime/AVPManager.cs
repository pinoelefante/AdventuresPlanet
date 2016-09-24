using AdventuresPlanetRuntime.Data;
using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;
using Utils;
using Windows.Storage;
using Windows.Web.Http.Filters;

namespace AdventuresPlanetRuntime
{
    public class AVPManager : HttpClient
    {
        
        private Windows.Web.Http.HttpClient jsonClient;
        public AVPManager()
        {
            //this.DefaultRequestHeaders.Add("User-Agent", "Adventure's Planet UWP");
            BaseAddress = new Uri(URL_BASE);
            HttpBaseProtocolFilter filter = new HttpBaseProtocolFilter();
            filter.AutomaticDecompression = true;
            jsonClient = new Windows.Web.Http.HttpClient(filter);
            jsonClient.DefaultRequestHeaders.Accept.TryParseAdd("application/json");
        }
        public const string URL_BASE = "http://www.adventuresplanet.it/";
        public async Task<bool> LoadListNews(int anno, int mese, Action<News> addAction = null)
        {
            try
            {
                string meselink = GetPeriodoString(anno, mese);
                string meseTimestamp = TimeUtils.GetUnixTimestamp(anno, mese).ToString();
                string page = $"{URL_BASE}index.php?old=si&data={meseTimestamp}";
                string response = await GetStringAsync(page);
                HtmlDocument doc = new HtmlDocument();
                doc.LoadHtml(response);
                IEnumerable<HtmlNode> news = doc.DocumentNode.Descendants("div").Where(x => x.Attributes.Contains("class") && x.Attributes["class"].Value.Equals("main_news"));
                Debug.WriteLine("news caricate: " + news.Count());
                foreach (HtmlNode node in news)
                {
                    string data = node.Descendants("h1").ToArray()[0].InnerText.Trim();
                    HtmlNode n_titolo = node.Descendants("a").Where(x => x.Attributes.Contains("class") && x.Attributes["class"].Value.Equals("news_title")).ToArray()[0];
                    string link = URL_BASE + n_titolo.Attributes["href"].Value;
                    string titolo = WebUtility.HtmlDecode(n_titolo.InnerText.Trim());
                    string img = URL_BASE + node.Descendants("span").Where(x => x.Attributes.Contains("class") && x.Attributes["class"].Value.Equals("padd")).ToArray()[0].Descendants("img").ToArray()[0].Attributes["src"].Value;
                    HtmlNode news_ante_cont = node.Descendants("p").Where(x => x.Attributes.Contains("class") && x.Attributes["class"].Value.Equals("news")).ToArray()[0];
                    string news_s = WebUtility.HtmlDecode(news_ante_cont.InnerText.Trim());

                    if (link.CompareTo(URL_BASE + "index.php") == 0 || news_ante_cont.ChildNodes.Count == 1)
                    {
                        var anchor_desc = news_ante_cont.Descendants("a");
                        HtmlNode anchor = anchor_desc.Count() == 1 ? anchor_desc.ElementAt(0) : null;
                        if (anchor != null)
                        {
                            string tlink = URL_BASE + anchor.Attributes["href"].Value;
                            if (IsRecensione(tlink) || IsSoluzione(tlink))
                                link = tlink;
                        }
                    }

                    News news_item = new News()
                    {
                        AnteprimaNews = news_s,
                        DataPubblicazione = data,
                        Immagine = img,
                        Link = link,
                        Titolo = titolo,
                        MeseLink = meselink
                    };
                    addAction?.Invoke(news_item);
                }
                return true;
            }
            catch
            {
                return false;
            }
        }
        public async Task<News> LoadNews (string link)
        {
            try
            {
                string response = await GetStringAsync(link);
                HtmlDocument doc = new HtmlDocument();
                doc.LoadHtml(response);

                HtmlNode news_node = doc.DocumentNode.Descendants("p").Where(x => x.Attributes.Contains("class") && x.Attributes["class"].Value.Equals("news")).ToArray()[0];
                News news = new News()
                {
                    CorpoNews = WebUtility.HtmlDecode(news_node.InnerText.Trim()),
                    TestoRich = ParseHtmlNews(news_node)
                };
                return news;
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message);
                return null;
            }
        }
        public async Task<Boolean> LoadNews(News n)
        {
            News resp = await LoadNews(n.Link);
            if(resp == null)
                return false;
            else
            {
                n.CorpoNews = resp.CorpoNews;
                n.TestoRich = resp.TestoRich;
                return true;
            }
        }
        private List<String> ParseHtmlNews(HtmlNode node)
        {
            List<string> rich = new List<string>();

            foreach (HtmlNode n in node.ChildNodes)
            {
                if (n.GetType() == typeof(HtmlTextNode))
                    rich.Add("@TEXT" + WebUtility.HtmlDecode(n.InnerText.Trim()));
                else
                {
                    if (n.OriginalName.Equals("a"))
                    {
                        string link = WebUtility.HtmlDecode(n.Attributes["href"].Value);
                        string text = WebUtility.HtmlDecode(n.InnerText.Trim());
                        rich.Add("@ANCHOR;link=" + link + ";text=" + text);
                    }
                    else if (n.OriginalName.Equals("b") || n.OriginalName.Equals("strong"))
                    {
                        if (n.FirstChild.GetType() == typeof(HtmlTextNode))
                            rich.Add("@BOLD" + WebUtility.HtmlDecode(n.FirstChild.InnerText));
                        else
                        {
                            if (n.FirstChild.OriginalName.Equals("a"))
                            {
                                string link = WebUtility.HtmlDecode(n.FirstChild.Attributes["href"].Value);
                                string text = WebUtility.HtmlDecode(n.FirstChild.InnerText.Trim());
                                rich.Add("@ANCHOR;link=" + link + ";text=" + text);
                            }
                        }
                    }
                    else if (n.OriginalName.Equals("i"))
                    {
                        string text = WebUtility.HtmlDecode(n.FirstChild.InnerText.Trim());
                        rich.Add("@ITALIC" + text);
                    }
                    else if (n.OriginalName.Equals("br"))
                    {
                        rich.Add("@DIVIDER");
                    }
                }
            }
            return rich;
        }
        public async Task<long> LoadListPodcast(Action<List<PodcastItem>> AddAction, Action<List<PodcastItem>> SaveAction, long time = 0)
        {
            var response = await jsonClient.GetStringAsync(new Uri($"http://pinoelefante.altervista.org/avp_it/avp_podcast.php?from={time}"));
            MemoryStream memstream = new MemoryStream(Encoding.UTF8.GetBytes(response));
            DataContractJsonSerializer ds = new DataContractJsonSerializer(typeof(JsonData.PodcastRoot));
            JsonData.PodcastRoot pods = (JsonData.PodcastRoot)ds.ReadObject(memstream);
            AddAction?.Invoke(pods.list);
            SaveAction?.Invoke(pods.list);
            return pods.time;
        }
        public async Task<long> LoadListRecensioni(Action<List<RecensioneItem>> AddAction, Action<List<RecensioneItem>> SaveAction = null, long time = 0)
        {
            var response = await jsonClient.GetStringAsync(new Uri($"http://pinoelefante.altervista.org/avp_it/avp_rece.php?from={time}"));
            MemoryStream memstream = new MemoryStream(Encoding.UTF8.GetBytes(response));
            DataContractJsonSerializer ds = new DataContractJsonSerializer(typeof(JsonData.RecensioniRoot));
            JsonData.RecensioniRoot rece = (JsonData.RecensioniRoot)ds.ReadObject(memstream);
            AddAction?.Invoke(rece.list);
            SaveAction?.Invoke(rece.list);
            return rece.time;
        }
        public async Task<Boolean> LoadRecensione(RecensioneItem rec)
        {
            try
            {
                string response = await GetStringAsync(rec.Link);
                HtmlDocument doc = new HtmlDocument();
                doc.LoadHtml(response);

                HtmlNode rec_s = doc.GetElementbyId("scheda_breve");
                if (rec_s != null)
                    rec.TestoBreve = WebUtility.HtmlDecode(rec_s.InnerText.Trim());

                HtmlNode vr = doc.GetElementbyId("bar_vote");
                if (vr != null)
                    rec.VotoText = string.IsNullOrEmpty(rec.VotoText) ? vr.InnerText.Trim() : rec.VotoText;

                HtmlNode vu = doc.GetElementbyId("bar_vote2");
                if (vu != null)
                    rec.VotoUtentiText = vu.InnerText.Trim();

                HtmlNode rec_l = doc.GetElementbyId("scheda_completa");
                if (rec_l != null)
                {
                    Debug.WriteLine(rec_l.InnerHtml);
                    rec.Testo = WebUtility.HtmlDecode(rec_l.InnerText.Trim());
                    rec.TestoRich = parseTestoRich(rec_l);
                }

                HtmlNode shop = doc.GetElementbyId("sch_shop_box");
                if (shop != null)
                {
                    IEnumerable<HtmlNode> link = shop.Descendants("a").Where(x => x.Attributes.Contains("href"));
                    if (link.Count() > 0)
                    {
                        rec.LinkStore = link.ElementAt(0).Attributes["href"].Value;
                    }
                }
                return true;
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message);
                return false;
            }
        }
        public async Task<RecensioneItem> LoadRecensione(string rec_link)
        {
            try
            {
                string response = await GetStringAsync(rec_link);
                HtmlDocument doc = new HtmlDocument();
                doc.LoadHtml(response);
                RecensioneItem rec = new RecensioneItem() { Link = rec_link.Replace(URL_BASE, "") };

                rec.Titolo = doc.GetElementbyId("scheda_text").Descendants("h1")?.ElementAt(0).InnerText.Trim();
                rec.Id = UrlUtils.GetUrlParameterValue(rec_link, "game");
                HtmlNode rec_s = doc.GetElementbyId("scheda_breve");
                if (rec_s != null)
                    rec.TestoBreve = WebUtility.HtmlDecode(rec_s.InnerText.Trim());

                HtmlNode vr = doc.GetElementbyId("bar_vote");
                if (vr != null)
                    rec.VotoText = string.IsNullOrEmpty(rec.VotoText) ? vr.InnerText.Trim() : rec.VotoText;

                HtmlNode vu = doc.GetElementbyId("bar_vote2");
                if (vu != null)
                    rec.VotoUtentiText = vu.InnerText.Trim();

                HtmlNode rec_l = doc.GetElementbyId("scheda_completa");
                if (rec_l != null)
                {
                    Debug.WriteLine(rec_l.InnerHtml);
                    rec.Testo = WebUtility.HtmlDecode(rec_l.InnerText.Trim());
                    rec.TestoRich = parseTestoRich(rec_l);
                }

                HtmlNode shop = doc.GetElementbyId("sch_shop_box");
                if (shop != null)
                {
                    IEnumerable<HtmlNode> link = shop.Descendants("a").Where(x => x.Attributes.Contains("href"));
                    if (link.Count() > 0)
                    {
                        rec.LinkStore = link.ElementAt(0).Attributes["href"].Value;
                    }
                }
                return rec;
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message);
                return null;
            }
        }
        public async Task<long> LoadListSoluzioni(Action<List<SoluzioneItem>> AddAction, Action<IEnumerable<SoluzioneItem>> SaveAction, long time = 0)
        {
            var response = await jsonClient.GetStringAsync(new Uri($"http://pinoelefante.altervista.org/avp_it/avp_solu.php?from={time}"));
            MemoryStream memstream = new MemoryStream(Encoding.UTF8.GetBytes(response));
            DataContractJsonSerializer ds = new DataContractJsonSerializer(typeof(JsonData.SoluzioniRoot));
            JsonData.SoluzioniRoot solu = (JsonData.SoluzioniRoot)ds.ReadObject(memstream);
            AddAction?.Invoke(solu.list);
            SaveAction?.Invoke(solu.list);
            return solu.time;
        }
        public async Task<Boolean> LoadSoluzione(SoluzioneItem sol)
        {
            try
            {
                string response = await GetStringAsync(sol.Link);
                HtmlDocument doc = new HtmlDocument();
                doc.LoadHtml(response);

                HtmlNode rec_l = doc.GetElementbyId("scheda_completa");
                if (rec_l != null)
                {
                    sol.Testo = WebUtility.HtmlDecode(rec_l.InnerText.Trim());
                    sol.TestoRich = parseTestoRich(rec_l);
                }
                HtmlNode shop = doc.GetElementbyId("sch_shop_box");
                if (shop != null)
                {
                    IEnumerable<HtmlNode> link = shop.Descendants("a").Where(x => x.Attributes.Contains("href"));
                    if (link.Count() > 0)
                    {
                        sol.LinkStore = link.ElementAt(0).Attributes["href"].Value;
                    }
                }
                return true;
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message);
                return false;
            }
        }
        public async Task<SoluzioneItem> LoadSoluzione(string sol_link)
        {
            try
            {
                string response = await GetStringAsync(sol_link);
                HtmlDocument doc = new HtmlDocument();
                doc.LoadHtml(response);
                SoluzioneItem sol = new SoluzioneItem()
                {
                    Link = sol_link.Replace(URL_BASE, ""),
                    Id = UrlUtils.GetUrlParameterValue(sol_link, "game"),
                    Titolo = doc.GetElementbyId("scheda_text").Descendants("h1")?.ElementAt(0).InnerText.Trim()
                };
                HtmlNode rec_l = doc.GetElementbyId("scheda_completa");
                if (rec_l != null)
                {
                    sol.Testo = WebUtility.HtmlDecode(rec_l.InnerText.Trim());
                    sol.TestoRich = parseTestoRich(rec_l);
                }
                HtmlNode shop = doc.GetElementbyId("sch_shop_box");
                if (shop != null)
                {
                    IEnumerable<HtmlNode> link = shop.Descendants("a").Where(x => x.Attributes.Contains("href"));
                    if (link.Count() > 0)
                    {
                        sol.LinkStore = link.ElementAt(0).Attributes["href"].Value;
                    }
                }
                return sol;
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message);
                return null;
            }
        }
        public async Task<List<PaginaContenuti>> LoadSaga(string url)
        {
            var content = await GetStringAsync(url);
            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(content);
            var listFound = doc.GetElementbyId("rece_list").Descendants("div");//.Where(x => x.Attributes.Contains("rece_nome"));
            List<PaginaContenuti> contenuti = new List<PaginaContenuti>(listFound.Count());
            for (int i = 1;i<listFound.Count();i++)
            {

                var itemDiv = listFound.ElementAt(i);
                var item = itemDiv.FirstChild;
                var link = WebUtility.HtmlDecode(item.Attributes["href"].Value);
                var nome = WebUtility.HtmlDecode(item.InnerText);
                if (IsRecensione($"{URL_BASE}{link}"))
                {
                    contenuti.Add(new RecensioneItem()
                    {
                        Titolo = nome,
                        Link = link
                    });
                }
                else if (IsSoluzione($"{URL_BASE}{link}"))
                {
                    contenuti.Add(new SoluzioneItem()
                    {
                        Titolo = nome,
                        Link = link
                    });
                }
            }
            return contenuti;
        }
        public async Task<long> LoadListGallerie(Action<List<GalleriaItem>> AddAction, Action<IEnumerable<GalleriaItem>> SaveAction, long time = 0)
        {
            var response = await jsonClient.GetStringAsync(new Uri($"http://pinoelefante.altervista.org/avp_it/avp_gallerie.php?from={time}"));
            MemoryStream memstream = new MemoryStream(Encoding.UTF8.GetBytes(response));
            DataContractJsonSerializer ds = new DataContractJsonSerializer(typeof(JsonData.GallerieRoot));
            JsonData.GallerieRoot gall = (JsonData.GallerieRoot)ds.ReadObject(memstream);
            AddAction?.Invoke(gall.list);
            SaveAction?.Invoke(gall.list);
            return gall.time;
        }
        public async Task LoadGalleria(GalleriaItem galleria, Action<string, AdvImage> AddAction)
        {
            if (!galleria.HasNext)
                return;
            try
            {
                string url_page = $"{URL_BASE}scheda_immagini.php?game={galleria.IdGalleria}&pagina={galleria.LastPageLoaded+1}";
                string response = await GetStringAsync(url_page);
                HtmlDocument doc = new HtmlDocument();
                doc.LoadHtml(response);

                HtmlNode mainNode = doc.GetElementbyId("scheda_completa");
                if (mainNode != null)
                {
                    IEnumerable<HtmlNode> schede = mainNode.Descendants("div").Where(x => x.Attributes.Contains("class") && x.Attributes["class"].Value.Equals("scheda_immagini"));
                    if (schede != null && schede.Count() > 0)
                    {
                        foreach (var scheda in schede)
                        {
                            var nomeGruppo = scheda.Descendants("b")?.FirstOrDefault()?.InnerText;
                            List<AdvImage> imgs = new List<AdvImage>();
                            var descendants = scheda.Descendants("a").ToList();
                            foreach (var htmlImg in descendants)
                            {
                                var link = htmlImg.Attributes["href"].Value;
                                var thumb = htmlImg.Descendants("img").First().Attributes["src"].Value;
                                AdvImage image = new AdvImage() { ImageLink = link, Thumb = thumb };
                                AddAction.Invoke(nomeGruppo, image);
                            }
                        }
                        HtmlNode numPages = doc.GetElementbyId("act_page");
                        if (numPages != null)
                        {
                            var pages = numPages.InnerText.Replace("Pagina", "").Trim().Split(new char[] { '/' });
                            if (pages != null && pages.Length == 2)
                                galleria.NumPages = Int32.Parse(pages[1]);
                        }
                        HtmlNode nodeFoot = doc.GetElementbyId("next_page");
                        bool nextPage = nodeFoot != null;
                        galleria.HasNext = nextPage;
                        galleria.LastPageLoaded++;
                    }
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message);
                galleria.HasNext = false;
            }
        }
        private List<string> parseTestoRich(HtmlNode scheda)
        {
            List<string> rich = new List<string>();
            foreach (HtmlNode n in scheda.ChildNodes)
            {
                if (n.GetType() == typeof(HtmlTextNode))
                {
                    string newstring = WebUtility.HtmlDecode(n.InnerText.Trim());
                    if (newstring.Length > 0)
                        rich.Add("@TEXT" + newstring);
                }
                else
                {
                    if (n.Attributes.Contains("class") && n.Attributes["class"].Value.Contains("scheda_immagini"))
                    {
                        string img = n.Descendants("a").ToArray()[0].Attributes["href"].Value;
                        rich.Add("@IMG" + img);
                    }
                    else if (n.OriginalName.CompareTo("br") == 0)
                    {
                        if ((rich.Count - 1 >= 0) && !rich.ElementAt(rich.Count - 1).StartsWith("@DIVIDER"))
                            rich.Add("@DIVIDER");
                    }
                    else if (n.OriginalName.Equals("h3")) //indice soluzione
                    {
                        IEnumerable<HtmlNode> indice = n.Descendants("a");
                        foreach (HtmlNode i in indice)
                        {
                            string titolo = WebUtility.HtmlDecode(i.InnerText.Trim());
                            string index = WebUtility.HtmlDecode(i.Attributes["href"].Value.Trim());
                            if (titolo.Length > 0 && index.Length > 0)
                                rich.Add("@INDEX;link=" + index + ";title=" + titolo);
                        }
                    }
                    else if (n.OriginalName.Equals("table"))
                    {
                        IEnumerable<HtmlNode> indiceItem = n.Descendants("a").Where(x => x.Attributes.Contains("name"));
                        foreach (HtmlNode i in indiceItem)
                        {
                            string index = "#" + WebUtility.HtmlDecode(i.Attributes["name"].Value.Trim());
                            string titolo = WebUtility.HtmlDecode(i.InnerText.Trim());
                            rich.Add("@POSINDEX;link=" + index + ";title=" + titolo);
                        }
                    }
                    else if (n.OriginalName.Equals("iframe")) //per videosoluzione
                    {
                        Debug.WriteLine("trovato video");
                        if (n.Attributes.Contains("src"))
                        {
                            string src = n.Attributes["src"].Value;
                            if (!src.StartsWith("http"))
                            {
                                src = "http:" + src;
                            }
                            string height = n.Attributes["height"].Value;
                            string width = n.Attributes["width"].Value;
                            Debug.WriteLine(src);
                            rich.Add("@VIDEO;" + src + ";" + width + ";" + height);
                        }
                    }
                    else
                    {
                        Debug.WriteLine(n.OriginalName);
                        string newstring = WebUtility.HtmlDecode(n.InnerText.Trim());
                        if (newstring.Length == 0)
                            continue;

                        if ((rich.Count - 1) >= 0 && rich.ElementAt(rich.Count - 1).StartsWith("@TEXT"))
                        {
                            string old = rich.ElementAt(rich.Count - 1);
                            old += " " + newstring;
                            rich.RemoveAt(rich.Count - 1);
                            rich.Add(old);
                        }
                        else
                            rich.Add("@TEXT" + newstring);
                    }
                }
            }
            compattaRichText(rich);
            return rich;
        }
        private void compattaRichText(List<string> rich)
        {
            for (int i = 0; i < rich.Count - 1;)
            {
                if (rich[i].StartsWith("@TEXT") && rich[i + 1].StartsWith("@TEXT"))
                {
                    rich[i] += " " + rich[i + 1].Replace("@TEXT", "");
                    rich.RemoveAt(i + 1);
                }
                else
                    i++;
            }
        }
        public async Task<List<string>> GetComponentsFrom(string url, string id)
        {
            try
            {
                string response = await GetStringAsync(url);
                HtmlDocument doc = new HtmlDocument();
                doc.LoadHtml(response);

                HtmlNode htmlComponents = doc.GetElementbyId(id);
                return parseTestoRich(htmlComponents);
            }
            catch
            {
                return null;
            }
        }
        public static bool IsRecensione(string uri)
        {
            var link = uri.ToString();
            if (!link.StartsWith("http"))
                link = URL_BASE + link;
            if (link.StartsWith(URL_BASE + "scheda_recensione.php"))
                return true;
            return false;
        }
        public static bool IsSoluzione(string uri)
        {
            var link = uri.ToString();
            if (!link.StartsWith("http"))
                link = URL_BASE + link;
            if (link.StartsWith(URL_BASE + "scheda_soluzione.php"))
                return true;
            return false;
        }
        public static bool IsGalleriaImmagini(String uri)
        {
            var link = uri.ToString();
            if (!link.StartsWith("http"))
                link = URL_BASE + link;
            if (link.StartsWith(URL_BASE + "scheda_immagini.php"))
                return true;
            return false;
        }
        public static bool IsPodcast(String uri)
        {
            var link = uri.ToString();
            if (!link.StartsWith("http"))
                link = URL_BASE + link;
            if (link.StartsWith($"{URL_BASE}podcast.php"))
                return true;
            return false;
        }
        public static bool IsExtra(string uri)
        {
            var link = uri.ToString();
            if (!link.StartsWith("http"))
                link = URL_BASE + link;
            if (link.StartsWith(URL_BASE + "scheda_extra.php"))
                return true;
            return false;
        }
        public static bool IsTrailer(string uri)
        {
            var link = uri.ToString();
            if (!link.StartsWith("http"))
                link = URL_BASE + link;
            if (link.StartsWith(URL_BASE + "scheda_trailer.php"))
                return true;
            return false;
        }
        public static bool IsSaga(string url)
        {
            var link = url.ToString();
            if (!link.StartsWith("http"))
                link = URL_BASE + link;
            if (link.StartsWith(URL_BASE + "schedario.php") && UrlUtils.GetUrlParameterValue(url, "saga")!=null)
                return true;
            return false;
        }
        private string GetPeriodoString(int anno, int mese)
        {
            return $"{anno.ToString("D4")}{mese.ToString("D2")}";
        }
        private ApplicationDataContainer localData = ApplicationData.Current.LocalSettings;
        public long UpdateTimeNews { get { return GetUpdateTime("News"); } set { SetUpdateTime("News", value); } }
        public long UpdateTimeRecensioni { get { return GetUpdateTime("Recensioni"); } set { SetUpdateTime("Recensioni", value); } }
        public long UpdateTimeSoluzioni { get { return GetUpdateTime("Soluzioni"); } set { SetUpdateTime("Soluzioni", value); } }
        public long UpdateTimeGallerie { get { return GetUpdateTime("Gallerie"); } set { SetUpdateTime("Gallerie", value); } }
        public long UpdateTimePodcast { get { return GetUpdateTime("Podcast"); } set { SetUpdateTime("Podcast", value); } }
        private long GetUpdateTime(string field)
        {
            if (localData.Values.ContainsKey($"UpdateTime{field}"))
                return (long)localData.Values[$"UpdateTime{field}"];
            return 0;
        }
        private void SetUpdateTime(string field, long value)
        {
            localData.Values[$"UpdateTime{field}"] = value;
        }
    }
}
