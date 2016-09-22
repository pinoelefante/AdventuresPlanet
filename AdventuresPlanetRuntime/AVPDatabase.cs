using AdventuresPlanetRuntime.Data;
using SQLite;
using SQLite.Net;
using SQLite.Net.Platform.WinRT;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;

namespace AdventuresPlanetRuntime
{
    public class AVPDatabase
    {
        private static readonly string DATABASE = Path.Combine(ApplicationData.Current.LocalFolder.Path, "db.sqlite");
        public AVPDatabase()
        {
            using(var db = DBConnection)
            {
                db.CreateTable<News>();
                db.CreateTable<GalleriaItem>();
                db.CreateTable<PodcastItem>();
                db.CreateTable<SoluzioneItem>();
                db.CreateTable<RecensioneItem>();
            }
        }
        private SQLiteConnection DBConnection
        {
            get
            {
                var connection = new SQLiteConnection(new SQLitePlatformWinRT(), DATABASE);
#if DEBUG
                connection.TraceListener = DebugTraceListener.Instance;
#endif
                return connection;
            }
        }
        public IEnumerable<News> SelectNews(int anno, int mese)
        {
            using (var db = DBConnection)
            {
                return db.Table<News>().Where(x => x.MeseLink.CompareTo($"{anno.ToString("D4")}{mese.ToString("D2")}") == 0);
            }
        }
        public void InsertAll<T>(IEnumerable<T> list)
        {
            using (var db = DBConnection)
            {
                db.InsertAll(list);
            }
        }
        public IEnumerable<RecensioneItem> SelectAllRecensioni()
        {
            using(var db = DBConnection)
            {
                return db.Table<RecensioneItem>()?.OrderBy( x => x.Titolo).ToList();
            }
        }
        public IEnumerable<SoluzioneItem> SelectAllSoluzioni()
        {
            using(var db = DBConnection)
            {
                return db.Table<SoluzioneItem>()?.OrderBy(X => X.Titolo).ToList();
            }
        }
        public List<PodcastItem> SelectAllPodcast()
        {
            using(var db = DBConnection)
            {
                return db.Table<PodcastItem>()?.OrderByDescending(x => x, new PodcastComparer()).ToList();
            }
        }
        public IEnumerable<GalleriaItem> SelectAllGallerie()
        {
            using (var db = DBConnection)
            {
                return db.Table<GalleriaItem>()?.OrderBy(x => x.Titolo).ToList();
            }
        }
        public void Update<T>(T obj)
        {
            using(var db = DBConnection)
            {
                var i = db.Update(obj);
            }
        }
        public GameWrapper GetGameById(string id)
        {
            using (var db = DBConnection)
            {
                GameWrapper game = new GameWrapper(id);
                var recFound = db.Table<RecensioneItem>().Where(x => x.Id.Equals(id));
                if (recFound!=null && recFound.Any())
                    game.Recensione = recFound.ElementAt(0);
                var solFound = db.Table<SoluzioneItem>().Where(x => x.Id.Equals(id));
                if (solFound!=null && solFound.Any())
                    game.Soluzione = solFound.ElementAt(0);
                var gallFound = db.Table<GalleriaItem>().Where(x => x.IdGalleria.Equals(id));
                if (gallFound!=null && gallFound.Any())
                    game.Galleria = gallFound.ElementAt(0);
                return game;
            }
        }
    }
}
