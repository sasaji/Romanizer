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
        /// ���Ȃ����[�}���ɕϊ����܂��B
        /// </remarks>
        public string Romanize(string kana)
        {
            return Kakasi.Do(kana, (_spellingStyle == SpellingStyle.Kunrei))[0];
        }

        /// <summary>
        /// �������Q�Ƃ��Ă��Ȃ����[�}���ɕϊ����܂��B
        /// </summary>
        public string Romanize(string kana, string kanji)
        {
            // �܂����Ȃ����[�}���ɕϊ�����B
            string kanaConverted = Romanize(kana);
            Logger.Debug("kanaConverted >>>>" + kanaConverted);

            // ���������[�}���ɕϊ�����B
            // ���Ƃ��� "���" "�C�m�E�G" �ŕϊ������ "i,ue" ���Ԃ����B
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
            // �������������� OutOfMemory �ɂȂ�̂� 16 �����ɐ����B
            if (kanji.Length > 16)
                return kanaConverted;

            List<IList<string>> dividedList = new List<IList<string>>();
            string result = string.Empty;
            string resultPartial = string.Empty;
            int matchPartialSaved = 0;

            // ����������̂��ׂĂ̕����p�^�[�����擾�� dividedList �Ɋi�[����B
            // ���Ƃ��Ί��������񂪁u��R�c�v�̏ꍇ�A
            // ��,�R,�c
            // ��,�R�c
            // ��R,�c
            // ��R�c
            // �� 4 �̌��ʂ�������B
            Divide(kanji, string.Empty, dividedList);
#if DEBUG
            dividedList.ForEach(i => {
                i.ToList().ForEach(j => Console.Write("[" + j + "]"));
                Console.Write(Environment.NewLine);
            });
#endif

            foreach (IList<string> dividedArray in dividedList) {
                // ������������������̊e�v�f�����[�}���ɕϊ�����B
                // �e�v�f�͕����̕ϊ����ʂ�������B��̗Ⴞ�ƁA
                // dai,oo - yama,san - ta,den
                // dai,oo - yamada,sanda
                // ooyama,daisen - ta,den
                // ooyamada
                // �̂悤�Ȍ��ʂ�������B
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

                // ���ꂼ��̌��ʂ̑g�ݍ��킹�����ׂČ����������X�g���擾����B��̗Ⴞ�ƁA
                // dai,yama,ta - dai,yama,den - dai,san,ta - dai,san,den - oo,yama,ta - oo,yama,den - oo,san,ta - oo,san,den
                // dai,yamada - dai,sanda - oo,yamada - oo,sanda
                // ooyama,ta - ooyama,den - daisen,ta - daisen,den
                // ooyamada
                // �̂悤�Ȍ��ʂ�������B
                List<string> combinedList = Combine(conversionList);

                foreach (string combinedString in combinedList) {
                    string[] combinedArray = combinedString.Split(',');

                    // �g�ݍ��킹�̐��������̏ꍇ
                    if (combinedArray.Length > 1) {
                        debug = combinedString;
                        // �g�ݍ��킹���Ăь����������̂��A�J�i��ϊ��������̂ƈ�v�����ꍇ�͂�����̗p�B
                        if (combinedString.Replace(",", "") == kanaConverted) {
                            Logger.Debug(debug + " !--------------------");
                            return combinedString;
                        } else {
                            // �g�ݍ��킹�̗v�f���A�J�i��ϊ�����������̒��ɂ���Ȃ��o�����邩�ǂ������ׂ�B
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

                            // ����Ȃ��o�������ꍇ�͂��̑g�ݍ��킹���߂�l�̌��B
                            // �ł��ق��ɂ����邩���m��Ȃ��̂ł܂����[�v���甲���Ȃ��B
                            if (matchAll) {
                                Logger.Debug(debug + " ?--------------------");
                                result = combinedString;
                            }
                            // �����I�Ɉ�v�����ꍇ�͈�Ԃ��������v���Ă�����̂��̗p�B
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

            // ����Ȃ��o���������̂�����ꍇ�͂��ꂪ�߂�l�B
            if (result != string.Empty)
                return result;

            // ������v���Ă�����̂�����ꍇ�͂��ꂪ�߂�l�B
            else if (resultPartial != string.Empty)
                return resultPartial;

            // �Ȃ��ꍇ�̓J�i��ϊ����������񂪖߂�l�B
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
                    // �Ђ炪�ȂƂ������Ȃ͕��f���Ȃ��B
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