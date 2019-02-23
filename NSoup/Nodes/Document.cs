﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NSoup.Parse;
using NSoup.Select;
using NSoup.Helper;

namespace NSoup.Nodes
{
	/// <summary>
	/// A HTML Document.
	/// </summary>
	/// <!--
	/// Original Author: Jonathan Hedley, jonathan@hedley.net
	/// Ported to .NET by: Amir Grozki
	/// -->
	public class Document : Element
	{
		public enum QuirksModeEnum
		{
			NoQuirks, Quirks, LimitedQuirks
		}

		public enum Syntax { html, xml }

		private OutputSettings _outputSettings = new OutputSettings();
		private QuirksModeEnum _quirksMode = QuirksModeEnum.NoQuirks;
		private string _location = string.Empty;
		private bool _updateMetaCharset = false;

		/// <summary>
		/// Create a new, empty Document.

		/// </summary>
		/// <param name="baseUri">base URI of document</param>
		/// <see cref="NSoup.Parse()" />
		/// <see cref="CreateShell(string)"/>
		public Document(string baseUri)
			: base(Tag.ValueOf("#root"), baseUri)
		{
			this._location = baseUri;
		}

		protected Document() { } // Used for Node.Clone().

		/// <summary>
		/// Create a valid, empty shell of a document, suitable for adding more elements to.
		/// </summary>
		/// <param name="baseUri">baseUri of document</param>
		/// <returns>document with html, head, and body elements.</returns>
		static public Document CreateShell(string baseUri)
		{
			if (baseUri == null)
			{
				throw new ArgumentNullException("baseUri");
			}

			Document doc = new Document(baseUri);
			Element html = doc.AppendElement("html");
			html.AppendElement("head");
			html.AppendElement("body");

			return doc;
		}

		public string Location { get { return this._location; } }
		/// <summary>
		/// Gets the document's <code>head</code> element.
		/// </summary>
		public Element Head
		{
			get { return FindFirstElementByTagName("head", this); }
		}

		/// <summary>
		/// Gets the document's <code>body</code> element.
		/// </summary>
		public Element Body
		{
			get { return FindFirstElementByTagName("body", this); }
		}

		/// <summary>
		/// Gets or sets the string contents of the document's {@code title} element.
		/// On set, updates the existing element, or adds {@code title} to {@code head} if
		/// not present.
		/// </summary>
		public string Title
		{
			get
			{
				// title is a preserve whitespace tag (for document output), but normalised here
				var titleEl = GetElementsByTag("title").First;
				return titleEl != null ? StringUtil.NormaliseWhitespace(titleEl.Text()).Trim() : string.Empty;
			}
			set
			{
				if (value == null)
				{
					throw new ArgumentNullException();
				}

				var titleEl = GetElementsByTag("title").First;
				if (titleEl == null)
				{ // add to head
					Head.AppendElement("title").Text(value);
				}
				else
				{
					titleEl.Text(value);
				}
			}
		}

		/// <summary>
		/// Gets the document's output settings.
		/// </summary>
		public OutputSettings OutputSettings()
		{
			return _outputSettings;
		}

		/// <summary>
		/// Sets the document's output settings.
		/// </summary>
		/// <param name="outputSettings">New output settings</param>
		/// <returns>This document, for chaining</returns>
		public Document OutputSettings(OutputSettings outputSettings)
		{
			if (outputSettings == null)
			{
				throw new ArgumentNullException("outputSettings");
			}

			this._outputSettings = outputSettings;
			return this;
		}

		/// <summary>
		/// Create a new Element, with this document's base uri. Does not make the new element a child of this document.
		/// </summary>
		/// <param name="tagName">element tag name (e.g. <code>a</code>)</param>
		/// <returns>new Element</returns>
		public Element CreateElement(string tagName)
		{
			return new Element(Tag.ValueOf(tagName), this.BaseUri);
		}

		/// <summary>
		/// Normalise the document. This happens after the parse phase so generally does not need to be called.
		/// Moves any text content that is not in the body element into the body.
		/// </summary>
		/// <returns>this document after normalisation</returns>
		public Document Normalise()
		{
			var htmlEl = FindFirstElementByTagName("html", this);
			if (htmlEl == null)
			{
				htmlEl = AppendElement("html");
			}
			if (Head == null)
			{
				htmlEl.PrependElement("head");
			}
			if (Body == null)
			{
				htmlEl.AppendElement("body");
			}

			// pull text nodes out of root, html, and head els, and push into body. non-text nodes are already taken care
			// of. do in inverse order to maintain text order.
			NormaliseTextNodes(Head);
			NormaliseTextNodes(htmlEl);
			NormaliseTextNodes(this);

			NormaliseStructure("head", htmlEl);
			NormaliseStructure("body", htmlEl);

			EnsureMetaCharsetElement();

			return this;
		}

		public QuirksModeEnum QuirksMode()
		{
			return _quirksMode;
		}

		public Document QuirksMode(QuirksModeEnum quirksMode)
		{
			this._quirksMode = quirksMode;
			return this;
		}

		// does not recurse.
		private void NormaliseTextNodes(Element element)
		{
			var toMove = new List<Node>();
			foreach (var node in element.ChildNodes)
			{
				if (node is TextNode)
				{
					var tn = (TextNode)node;
					if (!tn.IsBlank)
					{
						toMove.Add(tn);
					}
				}
			}

			for (var i = toMove.Count - 1; i >= 0; i--)
			{
				var node = toMove[i];
				element.RemoveChild(node);
				Body.PrependChild(new TextNode(" ", string.Empty));
				Body.PrependChild(node);
			}
		}

		// merge multiple <head> or <body> contents into one, delete the remainder, and ensure they are owned by <html>
		private void NormaliseStructure(string tag, Element htmlEl)
		{
			var elements = this.GetElementsByTag(tag);

			var master = elements.First; // will always be available as created above if not existent
			if (elements.Count > 1)
			{ // dupes, move contents to master
				var toMove = new List<Node>();

				for (var i = 1; i < elements.Count; i++)
				{
					var dupe = elements[i];

					foreach (var node in dupe.ChildNodes)
					{
						toMove.Add(node);
					}

					dupe.Remove();
				}

				foreach (var dupe in toMove)
				{
					master.AppendChild(dupe);
				}
			}

			// ensure parented by <html>
			if (!master.Parent.Equals(htmlEl))
			{
				htmlEl.AppendChild(master); // includes remove()            
			}
		}

		// fast method to get first by tag name, used for html, head, body finders
		private Element FindFirstElementByTagName(string tag, Node node)
		{
			if (node.NodeName.Equals(tag))
			{
				return (Element)node;
			}
			else
			{
				foreach (var child in node.ChildNodes)
				{
					var found = FindFirstElementByTagName(tag, child);
					if (found != null)
					{
						return found;
					}
				}
			}
			return null;
		}

		public override string OuterHtml()
		{
			return base.Html();
		}

		/// <summary>
		/// Set the text of the <code>body</code> of this document. Any existing nodes within the body will be cleared.
		/// </summary>
		/// <param name="text">unencoded text</param>
		/// <returns>this document</returns>
		public override Element Text(string text)
		{
			Body.Text(text); // overridden to not nuke doc structure
			return this;
		}

		/// <summary>
		/// Gets the node's name.
		/// </summary>
		public override string NodeName
		{
			get
			{
				return "#document";
			}
		}

		public new object Clone()
		{
			Document clone = (Document)base.Clone();
			clone._outputSettings = (OutputSettings)this._outputSettings.Clone();
			return clone;
		}

		public void charset(Encoding encoding)
		{
			SetUpdateMetaCharsetElement(true);
			this._outputSettings.SetEncoding(encoding);
			EnsureMetaCharsetElement();
		}

		public void SetUpdateMetaCharsetElement(bool update)
		{
			this._updateMetaCharset = update;
		}

		public bool GetUpdateMetaCharsetElement()
		{
			return this._updateMetaCharset;
		}

		public Encoding GetEncoding()
		{
			return _outputSettings.Encoding;
		}

		private void EnsureMetaCharsetElement()
		{
			if (this._updateMetaCharset)
			{
				var syntax = this._outputSettings.GetSyntax;

				if (syntax == Syntax.html)
				{
					var metaCharset = Select("meta[charset]").First;

					if (metaCharset != null)
					{
						metaCharset.Attr("charset", GetEncoding().EncodingName);
					}
					else
					{
						if (Head != null)
						{
							Head.AppendElement("meta").Attr("charset", GetEncoding().EncodingName);
						}
					}

					// Remove obsolete elements
					Select("meta[name=charset]").Remove();
				}
			}
		}
	}

	/// <summary>
	/// A Document's output settings control the form of the Text() and H   tml() methods.
	/// </summary>
	public class OutputSettings : ICloneable
	{
		private Entities.EscapeMode _escapeMode = Entities.EscapeMode.Base;
        private Encoding _encoding = Encoding.UTF8;
		private Encoder _encoder = null;
		private bool _prettyPrint = true;
		private bool _outline = false;
		private int _indentAmount = 1;
		private Document.Syntax _syntax = Document.Syntax.html;

		public OutputSettings()
		{
			_encoder = _encoding.GetEncoder();
		}

        /// <summary>
        /// Gets or sets the document's current HTML escape mode: <code>base</code>, which provides a limited set of named HTML 
        /// entities and escapes other characters as numbered entities for maximum compatibility; or <code>extended</code>, 
        /// which uses the complete set of HTML named entities. 
        /// <p> 
        /// The default escape mode is <code>base</code>. 
        /// </summary>
        public Entities.EscapeMode EscapeMode
		{
			get { return _escapeMode; }
		}

		/// <summary>
		/// Set the document's escape mode
		/// </summary>
		/// <param name="escapeMode">the new escape mode to use</param>
		/// <returns>the document's output settings, for chaining</returns>
		public OutputSettings SetEscapeMode(Entities.EscapeMode escapeMode)
		{
			this._escapeMode = escapeMode;
			return this;
		}

		/// <summary>
		/// Gets or sets the document's current output charset, which is used to control which characters are escaped when 
		/// generating HTML (via the <code>html()</code> methods), and which are kept intact. 
		/// <p> 
		/// Where possible (when parsing from a URL or File), the document's output charset is automatically set to the 
		/// input charset. Otherwise, it defaults to UTF-8.
		/// </summary>
		public Encoding Encoding
		{
			get { return _encoding; }
		}

		/// <summary>
		/// Update the document's output charset.
		/// </summary>
		/// <param name="encoding">the new encoding to use.</param>
		/// <returns>the document's output settings, for chaining</returns>
		public OutputSettings SetEncoding(Encoding encoding)
		{
			// todo: this should probably update the doc's meta charset
			this._encoding = encoding;
			this._encoder = _encoding.GetEncoder();
			return this;
		}

		/// <summary>
		/// Update the document's output charset.
		/// </summary>
		/// <param name="encoding">the new charset (by name) to use.</param>
		/// <returns>the document's output settings, for chaining</returns>
		public OutputSettings SetEncoding(string encoding)
		{
			SetEncoding(Encoding.GetEncoding(encoding));
			return this;
		}

		public Encoder Encoder
		{
			get { return _encoder; }
		}

		public Document.Syntax GetSyntax
		{
			get
			{
				return _syntax;
			}
		}

		public OutputSettings SetSyntax(Document.Syntax syntax)
		{
			this._syntax = syntax;
			return this;
		}

		/// <summary>
		/// Get if pretty printing is enabled. Default is true. If disabled, the HTML output methods will not re-format 
		/// the output, and the output will generally look like the input.
		/// </summary>
		/// <returns>if pretty printing is enabled.</returns>
		public bool PrettyPrint()
		{
			return this._prettyPrint;
		}

		/// <summary>
		/// Enable or disable pretty printing.
		/// </summary>
		/// <param name="pretty">new pretty print setting</param>
		/// <returns>this, for chaining</returns>
		public OutputSettings PrettyPrint(bool pretty)
		{
			this._prettyPrint = pretty;
			return this;
		}

		public bool GetOutline()
		{
			return this._outline;
		}

		public OutputSettings outline(bool outlineMode)
		{
			this._outline = outlineMode;
			return this;
		}

		/// <summary>
		/// Get the current tag indent amount, used when pretty printing.
		/// </summary>
		/// <returns>the current indent amount</returns>
		public int IndentAmount()
		{
			return this._indentAmount;
		}

		/// <summary>
		/// Set the indent amount for pretty printing
		/// </summary>
		/// <param name="indentAmount">number of spaces to use for indenting each level. Must be >= 0.</param>
		/// <returns>this, for chaining</returns>
		public OutputSettings IndentAmount(int indentAmount)
		{
			if (indentAmount < 0)
			{
				throw new ArgumentOutOfRangeException("indentAmount");
			}
			this._indentAmount = indentAmount;
			return this;
		}

		#region ICloneable Members

		public object Clone()
		{
			OutputSettings clone = new OutputSettings();

			clone.SetEncoding(_encoding.WebName); // new charset and charset encoder
			clone.SetEscapeMode(_escapeMode.Clone());
			clone.PrettyPrint(_prettyPrint);
			clone.IndentAmount(_indentAmount);

			return clone;
		}

		#endregion
	}
}
