using SQLite.Net.Attributes;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace AdventuresPlanetRuntime.Data
{
    public class News
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        public string Titolo { get; set; } = string.Empty;
        public string AnteprimaNews { get; set; } = string.Empty;
        public string CorpoNews { get; set; } = string.Empty;
        public string DataPubblicazione { get; set; } = string.Empty;
        [Unique]
        public string Link { get; set; } = string.Empty;
        public string Immagine { get; set; } = string.Empty;
        [Ignore]
        public List<string> TestoRich { get; set; }
        public string TestoRichString
        {
            get
            {
                if(TestoRich?.Count > 0)
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
                TestoRich?.Clear();
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
        public string MeseLink { get; set; }
    }
}
