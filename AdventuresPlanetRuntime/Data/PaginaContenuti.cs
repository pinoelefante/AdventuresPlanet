using SQLite.Net.Attributes;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Text;

namespace AdventuresPlanetRuntime.Data
{
    [DataContract]
    public abstract class PaginaContenuti : NotificableItem
    {
        [DataMember(Name = "id")]
        [PrimaryKey]
        public string Id { get; set; } = string.Empty;
        [DataMember(Name = "titolo")]
        public string Titolo { get; set; } = string.Empty;
        [DataMember(Name = "link")]
        public string Link { get; set; } = string.Empty;
        [DataMember(Name = "autore")]
        public string AutoreText { get; set; } = string.Empty;
        public string Testo { get; set; } = string.Empty;
        [Ignore]
        public List<string> TestoRich { get; set; }
        public string TestoRichString
        {
            get
            {
                if (TestoRich?.Count > 0)
                {
                    StringBuilder str = new StringBuilder();
                    foreach (var item in TestoRich)
                    {
                        str.Append($"{item}||");
                    }
                    return str.ToString();
                }
                return string.Empty;
            }
            set
            {
                if (!string.IsNullOrEmpty(value))
                {
                    var s = new string[] { "||" };
                    var kv = value.Split(s, StringSplitOptions.RemoveEmptyEntries);
                    TestoRich = new List<string>(kv.Length);
                    foreach (var item in kv)
                        TestoRich.Add(item);
                }
            }
        }
        [Ignore]
        public abstract bool IsVideo { get; }
        private string _store = string.Empty;
        public string LinkStore { get { return _store; } set { Set(ref _store, value); } }
        [Ignore]
        public bool IsPreferita { get; set; }
        [Ignore]
        public bool IsNew { get; set; }
    }
}
