using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lovco
{
    class TestData
    {
        public string Kana;
        public string Kanji;
        public string Roman;

        public TestData(string kana, string kanji)
        {
            Romanizer romanizer = new Romanizer();
            Kana = kana;
            Kanji = kanji;
            if (string.IsNullOrEmpty(kanji))
                Roman = romanizer.Romanize(kana);
            else
                Roman = romanizer.Romanize(kana, kanji);
        }

        public TestData(string kana, string kanji, SpellingStyle spellingStyle)
        {
            Romanizer romanizer = new Romanizer(spellingStyle);
            Kana = kana;
            Kanji = kanji;
            if (string.IsNullOrEmpty(kanji))
                Roman = romanizer.Romanize(kana);
            else
                Roman = romanizer.Romanize(kana, kanji);
        }

        public TestData(string kana, string kanji, LongVowelStyle longVowelStyle)
        {
            Romanizer romanizer = new Romanizer(longVowelStyle);
            Kana = kana;
            Kanji = kanji;
            if (string.IsNullOrEmpty(kanji))
                Roman = romanizer.Romanize(kana);
            else
                Roman = romanizer.Romanize(kana, kanji);
        }

        public void Log()
        {
            Console.Write("[" + Kana + "]");
            if (!string.IsNullOrEmpty(Kanji))
                Console.Write("[" + Kanji + "]");
            Console.Write("->[" + Roman + "]");
            Console.Write(Environment.NewLine);
        }
    }
}
