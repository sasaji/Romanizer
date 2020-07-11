using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using Microsoft.VisualBasic;

namespace Lovco
{
    public class Romanizer
    {
        private readonly SpellingStyle _spellingStyle = SpellingStyle.Hepburn;
        private readonly LongVowelStyle _longVowelStyle = LongVowelStyle.Ignore;

        public Romanizer()
        {
        }

        public Romanizer(SpellingStyle spellingStyle)
        {
            _spellingStyle = spellingStyle;
        }

        public Romanizer(LongVowelStyle longVowelStyle)
        {
            _longVowelStyle = longVowelStyle;
        }

        public Romanizer(SpellingStyle spellingStyle, LongVowelStyle longVowelStyle)
        {
            _spellingStyle = spellingStyle;
            _longVowelStyle = longVowelStyle;
        }

        /// <summary>
        /// かなをローマ字に変換します。
        /// </remarks>
        public string Romanize(string kana)
        {
            return Kakasi.Do(kana, (_spellingStyle == SpellingStyle.Kunrei))[0];
        }

        /// <summary>
        /// 漢字を参照してかなをローマ字に変換します。
        /// </summary>
        public string Romanize(string kana, string kanji)
        {
            // まずかなをローマ字に変換する。
            string kanaConverted = Romanize(kana);
            Logger.Debug("kanaConverted >>>>" + kanaConverted);

            // 漢字をローマ字に変換する。
            // たとえば "井上" "イノウエ" で変換すると "i,ue" が返される。
            string candidateCsv = Kanji2Roman(kanji, kanaConverted);
            Logger.Debug("candidateCsv >>>>" + candidateCsv);

            string[] candidateArray = candidateCsv.Split(',');
            string result = null;

            foreach (string s in candidateArray) {
                Logger.Debug("kanaConverted >>>>" + kanaConverted);
                Logger.Debug("s >>>>" + s);
                int pos = kanaConverted.IndexOf(s);
                Logger.Debug("pos >>>>" + pos);
                if (pos >= 0) {
                    string tmp = kanaConverted.Substring(0, pos) + s;
                    Logger.Debug(">>>>" + kanaConverted.Substring(0, pos) + "," + s);
                    result += LongVowel(kanaConverted.Substring(0, pos)) + LongVowel(s);
                    kanaConverted = kanaConverted.Substring(tmp.Length);
                    Logger.Debug(result + "," + kanaConverted);
                }
                if (kanaConverted.Length == 0)
                    break;
            }

            if (kanaConverted.Length > 0)
                result += LongVowel(kanaConverted);

            return result;
        }

        private string LongVowel(string source)
        {
            string result = source;
            if (_longVowelStyle == LongVowelStyle.DoNothing) return result;
            if (_longVowelStyle == LongVowelStyle.OH)
                result = source.Replace("ou", "oh").Replace("oo", "oh");
            return result.Replace("ou", "o").Replace("oo", "o").Replace("uu", "u");
        }

        private string Kanji2Roman(string kanji, string kanaConverted)
        {
            // 文字数が多いと OutOfMemory になるので 16 文字に制限。
            if (kanji.Length > 16)
                return kanaConverted;

            List<IList<string>> dividedList = new List<IList<string>>();
            string result = string.Empty;
            string resultPartial = string.Empty;
            int matchPartialSaved = 0;

            // 漢字文字列のすべての分割パターンを取得し dividedList に格納する。
            // たとえば漢字文字列が「大山田」の場合、
            // 大,山,田
            // 大,山田
            // 大山,田
            // 大山田
            // の 4 つの結果が得られる。
            Divide(kanji, string.Empty, dividedList);
#if DEBUG
            dividedList.ForEach(i => {
                i.ToList().ForEach(j => Console.Write("[" + j + "]"));
                Console.Write(Environment.NewLine);
            });
#endif

            foreach (IList<string> dividedArray in dividedList) {
                // 分割した漢字文字列の各要素をローマ字に変換する。
                // 各要素は複数の変換結果が得られる。先の例だと、
                // dai,oo - yama,san - ta,den
                // dai,oo - yamada,sanda
                // ooyama,daisen - ta,den
                // ooyamada
                // のような結果が得られる。
                List<IList<string>> conversionList = new List<IList<string>>();
                string debug = null;
                foreach (string dividedToken in dividedArray)
                    conversionList.Add(Kakasi.Do(dividedToken, false));
#if DEBUG
                conversionList.ForEach(i => {
                    i.ToList().ForEach(j => Console.Write("<" + j + ">"));
                    Console.Write(Environment.NewLine);
                });
#endif

                // それぞれの結果の組み合わせをすべて結合したリストを取得する。先の例だと、
                // dai,yama,ta - dai,yama,den - dai,san,ta - dai,san,den - oo,yama,ta - oo,yama,den - oo,san,ta - oo,san,den
                // dai,yamada - dai,sanda - oo,yamada - oo,sanda
                // ooyama,ta - ooyama,den - daisen,ta - daisen,den
                // ooyamada
                // のような結果が得られる。
                List<string> combinedList = Combine(conversionList);

                foreach (string combinedString in combinedList) {
                    string[] combinedArray = combinedString.Split(',');

                    // 組み合わせの数が複数の場合
                    if (combinedArray.Length > 1) {
                        debug = combinedString;
                        // 組み合わせを再び結合したものが、カナを変換したものと一致した場合はそれを採用。
                        if (combinedString.Replace(",", "") == kanaConverted) {
                            Logger.Debug(debug + " !--------------------");
                            return combinedString;
                        } else {
                            // 組み合わせの要素が、カナを変換した文字列の中にもれなく出現するかどうか調べる。
                            bool matchAll = true;
                            int pos = 0;
                            for (int i = 0; i < combinedArray.Length; i++) {
                                if (i == 0) {
                                    if (!kanaConverted.StartsWith(combinedArray[i])) {
                                        matchAll = false;
                                        break;
                                    }
                                } else if (i == combinedArray.Length - 1) {
                                    if (!kanaConverted.EndsWith(combinedArray[i])) {
                                        matchAll = false;
                                        break;
                                    }
                                } else {
                                    pos = kanaConverted.IndexOf(combinedArray[i], pos);
                                    if (pos < 0) {
                                        matchAll = false;
                                        break;
                                    }
                                }
                            }

                            // もれなく出現した場合はその組み合わせが戻り値の候補。
                            // でもほかにもあるかも知れないのでまだループから抜けない。
                            if (matchAll) {
                                Logger.Debug(debug + " ?--------------------");
                                result = combinedString;
                            }
                            // 部分的に一致した場合は一番たくさん一致しているものを採用。
                            else {
                                int matchPartial = 0;
                                for (int i = 0; i < combinedArray.Length; i++) {
                                    if (i == 0) {
                                        if (kanaConverted.StartsWith(combinedArray[i]))
                                            matchPartial++;
                                    } else if (i == combinedArray.Length - 1) {
                                        if (kanaConverted.EndsWith(combinedArray[i]))
                                            matchPartial++;
                                    } else {
                                        if (kanaConverted.IndexOf(combinedArray[i]) >= 0)
                                            matchPartial++;
                                    }
                                }
                                if (matchPartialSaved < matchPartial) {
                                    Logger.Debug(debug + " *--------------------");
                                    matchPartialSaved = matchPartial;
                                    resultPartial = combinedString;
                                } else
                                    Logger.Debug(debug);
                            }
                        }
                    }
                }
                Logger.Debug("");
            }

            // もれなく出現したものがある場合はそれが戻り値。
            if (result != string.Empty)
                return result;

            // 部分一致しているものがある場合はそれが戻り値。
            else if (resultPartial != string.Empty)
                return resultPartial;

            // ない場合はカナを変換した文字列が戻り値。
            else
                return kanaConverted;
        }

        internal void Divide(string source, string target, List<IList<string>> result)
        {
            for (int i = 1; i <= source.Length; i++) {
                string left = (target.Length > 0 ? target + "," : "") + source.Substring(0, i);
                string right = source.Substring(i);
                if (right.Length > 0)
                    Divide(right, left, result);
                else {
                    // ひらがなとかたかなは分断しない。
                    if (!Regex.IsMatch(left, "[\u3040-\u30FF],[\u3040-\u30FF]"))
                        result.Add(left.Split(','));
                }
            }
        }

        private static List<string> Combine(List<IList<string>> args)
        {
            List<string> result = new List<string>();
            if (args.Count == 0) {
                result.Add(string.Empty);
                return result;
            }
            foreach (string leftItem in args[0]) {
                List<IList<string>> rest = new List<IList<string>>();
                for (int i = 1; i < args.Count; i++)
                    rest.Add(args[i]);
                foreach (string combined in Combine(rest)) {
                    if (args.Count == 1)
                        result.Add(leftItem + combined);
                    else
                        result.Add(leftItem + "," + combined);
                }
            }
            return result;
        }

        public SpellingStyle SpellingStyle
        {
            get { return _spellingStyle; }
        }
    }
}