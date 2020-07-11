using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Lovco
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void TestMethod1()
        {
            // SpellingStyle による変換結果の違い。
            Assert.AreEqual("shouko", (new TestData("ショウコ", null)).Roman);
            Assert.AreEqual("shouko", (new TestData("ショウコ", null, SpellingStyle.Hepburn)).Roman);
            Assert.AreEqual("syouko", (new TestData("ショウコ", null, SpellingStyle.Kunrei)).Roman);
            Assert.AreEqual("namba", (new TestData("ナンバ", null)).Roman);
            Assert.AreEqual("nanba", (new TestData("ナンバ", null, SpellingStyle.Kunrei)).Roman);
            Assert.AreEqual("nampa", (new TestData("ナンパ", null)).Roman);
            Assert.AreEqual("nanpa", (new TestData("ナンパ", null, SpellingStyle.Kunrei)).Roman);
            Assert.AreEqual("namma", (new TestData("ナンマ", null)).Roman);
            Assert.AreEqual("nanma", (new TestData("ナンマ", null, SpellingStyle.Kunrei)).Roman);

            // LongVowelStyle による変換結果の違い。
            Assert.AreEqual("shoko", (new TestData("ショウコ", "祥子", LongVowelStyle.Ignore)).Roman);
            Assert.AreEqual("shouko", (new TestData("ショウコ", "祥子", LongVowelStyle.DoNothing)).Roman);
            Assert.AreEqual("shohko", (new TestData("ショウコ", "祥子", LongVowelStyle.OH)).Roman);

            // その他。
            Assert.AreEqual("inoue", (new TestData("イノウエ", "井上")).Roman);
            Assert.AreEqual("joshu", (new TestData("ジョウシュウ", "上州")).Roman);
            Assert.AreEqual("hatchobori", (new TestData("ハッチョウボリ", "八丁堀")).Roman);
            Assert.AreEqual("kani", (new TestData("カンイ", "簡易")).Roman);
            Assert.AreEqual("timu", (new TestData("チーム", null, SpellingStyle.Kunrei)).Roman);
            Assert.AreEqual("kouchiwa", (new TestData("コウチワ", "小団扇")).Roman);
            Assert.AreEqual("kouchiwa", (new TestData("コウチワ", "小団扇", LongVowelStyle.OH)).Roman);
            Assert.AreEqual("kochiwa", (new TestData("コウチワ", "高知和")).Roman);
            Assert.AreEqual("kohchiwa", (new TestData("コウチワ", "高知和", LongVowelStyle.OH)).Roman);
            Assert.AreEqual("kohtsu", (new TestData("コオツ", "高津", LongVowelStyle.OH)).Roman);
        }
    }
}
