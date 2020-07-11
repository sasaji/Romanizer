# coding: cp932

import os
import re
from ctypes import *
from enum import Enum
from itertools import product

class SpellingStyle(Enum):
    Hepburn = 0
    Kunrei = 1

class LongVowelStyle(Enum):
    Ignore = 0
    DoNothing = 1
    OH = 2

class Romanizer:
    def __init__(self, spellingStyle = SpellingStyle.Hepburn, longVowelStyle = LongVowelStyle.Ignore):
        self.spellingStyle = spellingStyle
        self.longVowelStyle = longVowelStyle

    def KakasiDo(self, kana):
        # 環境変数を設定する。kanwadict と itaijidict はスクリプトと同じ場所にある想定。
        os.environ["KANWADICTPATH"] = os.getcwd() + "\kanwadict"
        os.environ["ITAIJIDICTPATH"] = os.getcwd() + "\itaijidict"

        # kakasi.dll をロード。
        kakasi = cdll.LoadLibrary("kakasi.dll")

        # 引数の設定。スペリングが訓令式の場合は -rk を追加。
        # kakasi は UTF-8 非サポート状態でビルド
        argArray = c_char_p * 11
        argv = argArray(b"kakasi", b"-a", b"-j", b"-g", b"-k", b"-p", b"-Ja", b"-Ha", b"-Ka", b"-Ea")
        argc = 10
        if self.spellingStyle == SpellingStyle.Kunrei:
            argv[10] = b"-rk"
            argc += 1
        kakasi.kakasi_getopt_argv(argc, argv)

        # 変換実行。
        kakasi.kakasi_do.restype = c_char_p
        kakasiDone = kakasi.kakasi_do(kana.encode("shift-jis")).decode("utf-8")
        kakasi.kakasi_close_kanwadict()

        kakasiDone = re.sub("['\^]", "", kakasiDone)
        if self.spellingStyle != SpellingStyle.Kunrei:
            kakasiDone = kakasiDone.replace("nb", "mb").replace("nm", "mm").replace("np", "mp")

        matches = re.compile("{.+?}").findall(kakasiDone)
        if len(matches) == 0:
            return [kakasiDone]
        return list(map(lambda x: "".join(x), product(*list(map(lambda x: x.lstrip("{").rstrip("}").split("|"), matches)))))

    def Divide(self, source, target, result):
        for i in range(1, len(source) + 1):
            left = ((target + ",") if (len(target) > 0) else "") + source[0:i]
            right = source[i:]
            if len(right) > 0:
                self.Divide(right, left, result)
            else:
                if not re.match("[\u3040-\u30FF],[\u3040-\u30FF]", left):
                    result.append(left.split(","))

    def Combine(self, args):
        result = []
        if len(args) == 0:
            result.append("")
            return result
        for leftItem in args[0]:
            rest = []
            for i in range(1, len(args)):
                rest.append(args[i])
            for combined in self.Combine(rest):
                if len(args) == 1:
                    result.append(leftItem + combined)
                else:
                    result.append(leftItem + "," + combined)
        return result

    def LongVowel(self, source):
        result = source
        if self.longVowelStyle == LongVowelStyle.DoNothing:
            return result
        if self.longVowelStyle == LongVowelStyle.OH:
            result = source.replace("ou", "oh")
        return result.replace("ou", "o").replace("oo", "o").replace("uu", "u")

    def Romanize(self, kana, kanji = ""):
        kanaConverted = self.KakasiDo(kana)[0]
        if kanji == "":
            return kanaConverted

        candidateCsv = self.Kanji2Roman(kanaConverted, kanji)
        candidateArray = candidateCsv.split(",")
        result = ""
        for s in candidateArray:
            pos = kanaConverted.find(s)
            if pos >= 0:
                tmp = kanaConverted[0:pos] + s
                result += self.LongVowel(kanaConverted[0:pos]) + self.LongVowel(s)
                kanaConverted = kanaConverted[len(tmp):]
            if len(kanaConverted) == 0:
                break
        if len(kanaConverted) > 0:
            result += self.LongVowel(kanaConverted)

        return result

    def Kanji2Roman(self, kana, kanji):
        result = ""
        resultPartial = ""
        matchPartialSaved = 0
        divided = []
        self.Divide(kanji, "", divided)
        #print("divided = ", divided);

        for dividedArray in divided:
            converted = []
            for dividedToken in dividedArray:
                converted.append(self.KakasiDo(dividedToken))
            #print("converted = ", converted)

            for combined in self.Combine(converted):
                #print("combined = ", combined)
                combinedArray = combined.split(",")
                if len(combinedArray) > 1:
                    if combined.replace(",", "") == kana:
                        return combined
                    else:
                        matchAll = False
                        pos = 0
                        for i in range(0, len(combinedArray)):
                            if i == 0:
                                if not kana.startswith(combinedArray[i]):
                                    matchAll = False
                                    break
                            elif i == len(combinedArray) - 1:
                                if not kana.endswith(combinedArray[i]):
                                    matchAll = False
                                    break
                        if matchAll:
                            result = combinedString
                        else:
                            matchPartial = 0
                            for i in range(0, len(combinedArray)):
                                if i == 0:
                                    if kana.startswith(combinedArray[i]):
                                        matchPartial += 1
                                elif i == len(combinedArray) - 1:
                                    if kana.endswith(combinedArray[i]):
                                        matchPartial += 1
                                else:
                                    if kana.find(combinedArray[i]) >= 0:
                                        matchPartial += 1
                            if matchPartialSaved < matchPartial:
                                matchPartialSaved = matchPartial
                                resultPartial = combined

        if result != "":
            return result
        elif resultPartial != "":
            return resultPartial
        else:
            return kana

romanizer = Romanizer()
print(romanizer.Romanize("ショウコ"))
romanizer = Romanizer(SpellingStyle.Kunrei)
print(romanizer.Romanize("ショウコ"))
romanizer = Romanizer()
#print(romanizer.Romanize("上"))
#print(romanizer.Romanize("上下"))
print(romanizer.Romanize("上魚"))
print(romanizer.Romanize("ジョウオ", "上魚"))
#print(romanizer.Romanize("簡易"))
#print(romanizer.Romanize("ナンバ"))
#print(romanizer.Romanize("峠"))
print(romanizer.Romanize("イノウエ", "井上"))
print(romanizer.Romanize("コウチワ", "小團扇"))
print(romanizer.Romanize("コウチワ", "高知和"))
#print(romanizer.Romanize("オオヤマダ", "大山田"))
