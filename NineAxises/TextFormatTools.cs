using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NineAxises
{
    public static class TextFormatTools
    {
        public const string FormatText = ": {0}{1}";
        public static string AlignDoubleValue(double v)
        {
            string text = string.Format("{0}", v);
            if (!text.StartsWith("-"))
            {
                text = "+" + text;
            }
            text = text.PadRight(20, '0');
            return text;
        }

    }
}
