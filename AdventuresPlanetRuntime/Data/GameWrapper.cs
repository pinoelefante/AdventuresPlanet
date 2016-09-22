using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AdventuresPlanetRuntime.Data
{
    public class GameWrapper : NotificableItem
    {
        public GameWrapper(string id)
        {
            Id = id;
        }

        private RecensioneItem rece;
        private SoluzioneItem solu;
        private GalleriaItem gall;
        public string Id { get; private set; }
        public string Titolo
        {
            get
            {
                if (Recensione != null)
                    return Recensione.Titolo;
                else if (Soluzione != null)
                    return Soluzione.Titolo;
                else if (Galleria != null)
                    return Galleria.Titolo;
                else
                    return "Aggiorna le recensioni, soluzioni e gallerie per conoscere il titolo";
            }
        }
        public RecensioneItem Recensione { get { return rece; } set { Set(ref rece, value); } }
        public SoluzioneItem Soluzione { get { return solu; } set { Set(ref solu, value); } }
        public GalleriaItem Galleria { get { return gall; } set { Set(ref gall, value); } }
    }
}
