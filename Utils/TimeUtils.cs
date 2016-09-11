using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Utils
{
    public class TimeUtils
    {
        public static long GetUnixTimestamp()
        {
            DateTime periodo = DateTime.Now;
            long unixTimestamp = (long)(periodo.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
            return unixTimestamp;
        }
        public static long GetUnixTimestamp(int anno, int mese)
        {
            DateTime periodo = new DateTime(anno, mese, 1);
            long unixTimestamp = (long)(periodo.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
            return unixTimestamp;
        }
    }
}
