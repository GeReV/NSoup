using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSoup.Helper;
using System;
using System.Text;

namespace Test.Helper
{

	[TestClass]
	public class DataUtilTest
	{
		public DataUtilTest()
		{
			//
			// TODO: Add constructor logic here
			//
		}

		private TestContext testContextInstance;

		/// <summary>
		///Gets or sets the test context which provides
		///information about and functionality for the current test run.
		///</summary>
		public TestContext TestContext
		{
			get
			{
				return testContextInstance;
			}
			set
			{
				testContextInstance = value;
			}
		}

		[TestMethod]
		public void testCharset()
		{
			Assert.AreEqual("utf-8", DataUtil.GetCharsetFromContentType("text/html;charset=utf-8 "), true);
			Assert.AreEqual("UTF-8", DataUtil.GetCharsetFromContentType("text/html; charset=UTF-8"));
			Assert.AreEqual("ISO-8859-1", DataUtil.GetCharsetFromContentType("text/html; charset=ISO-8859-1"));
			Assert.AreEqual(null, DataUtil.GetCharsetFromContentType("text/html"));
			Assert.AreEqual(null, DataUtil.GetCharsetFromContentType(null));
			Assert.AreEqual(null, DataUtil.GetCharsetFromContentType("text/html;charset=Unknown"));
		}

		[TestMethod]
		public void testQuotedCharset()
		{
			Assert.AreEqual("utf-8", DataUtil.GetCharsetFromContentType("text/html; charset=\"utf-8\""));
			Assert.AreEqual("UTF-8", DataUtil.GetCharsetFromContentType("text/html;charset=\"UTF-8\""));
			Assert.AreEqual("ISO-8859-1", DataUtil.GetCharsetFromContentType("text/html; charset=\"ISO-8859-1\""));
			Assert.AreEqual(null, DataUtil.GetCharsetFromContentType("text/html; charset=\"Unsupported\""));
			Assert.AreEqual("UTF-8", DataUtil.GetCharsetFromContentType("text/html; charset='UTF-8'"));
		}

		[TestMethod]
		public void discardsSpuriousByteOrderMark()
		{
			var html = "\uFEFF<html><head><title>One</title></head><body>Two</body></html>";
			var buffer = Encoding.UTF8.GetBytes(html);
			var doc = DataUtil.ParseByteData(buffer, "UTF-8", "http://foo.com/", NSoup.Parse.Parser.HtmlParser());
			Assert.AreEqual("One", doc.Head.Text());
		}

		[TestMethod]
		public void discardsSpuriousByteOrderMarkWhenNoCharsetSet()
		{
			/**/
			var html = "\uFEFF<html><head><title>One</title></head><body>Two</body></html>";
			var buffer = Encoding.UTF8.GetBytes(html);
			var doc = DataUtil.ParseByteData(buffer, null, "http://foo.com/", NSoup.Parse.Parser.HtmlParser());
			Assert.AreEqual("One", doc.Head.Text());
			//assertEquals("UTF-8", doc.outputSettings().charset().displayName());
		}

		[TestMethod]
		public void shouldNotThrowExceptionOnEmptyCharset()
		{
			Assert.AreEqual(null, DataUtil.GetCharsetFromContentType("text/html; charset="));
			Assert.AreEqual(null, DataUtil.GetCharsetFromContentType("text/html; charset=;"));
		}

		[TestMethod]
		public void shouldSelectFirstCharsetOnWeirdMultileCharsetsInMetaTags()
		{
			Assert.AreEqual("ISO-8859-1", DataUtil.GetCharsetFromContentType("text/html; charset=ISO-8859-1, charset=1251"));
		}

		[TestMethod]
		public void shouldCorrectCharsetForDuplicateCharsetString()
		{
			Assert.AreEqual("iso-8859-1", DataUtil.GetCharsetFromContentType("text/html; charset=charset=iso-8859-1"));
		}

		[TestMethod]
		public void shouldReturnNullForIllegalCharsetNames()
		{
			Assert.AreEqual(null, DataUtil.GetCharsetFromContentType("text/html; charset=$HJKDF§$/("));
		}

		[TestMethod]
		public void generatesMimeBoundaries()
		{
			var m1 = DataUtil.MimeBoundary();
			var m2 = DataUtil.MimeBoundary();

			Assert.AreEqual(DataUtil.BoundaryLength, m1.Length);
			Assert.AreEqual(DataUtil.BoundaryLength, m2.Length);
			Assert.AreNotSame(m1, m2);
		}

		[TestMethod]
		public void wrongMetaCharsetFallback()
		{
			var html = "<html><head><meta charset=iso-8></head><body></body></html>";
			var buffer = Encoding.UTF8.GetBytes(html);
			Action actionFunc = (() => DataUtil.ParseByteData(buffer, null, "http://example.com", NSoup.Parse.Parser.HtmlParser()));

			var exceptionTriggered = false;
			try
			{
				actionFunc.Invoke();
			}
			catch (ArgumentException e)
			{
				exceptionTriggered = true;
			}

			Assert.AreEqual(true, exceptionTriggered);
		}
	}
}
