using System;
using System.Collections.Generic;
using System.Linq;
using HtmlAgilityPack;
using Newtonsoft.Json.Linq;

namespace LucasSpider.Extensions
{
	internal static class StringExtensions
	{
		public static bool TryParseJToken(this string jsonString, out JToken token)
		{
			try
			{
				token = JToken.Parse(jsonString);
				return true;
			}
			catch (Exception)
			{
				token = null;
				return false;
			}
		}

		public static bool Contains(this string source, string value, StringComparison comparison)
		{
#if NETSTANDARD2_0
			return source.IndexOf(value, comparison) >= 0;
#else
			return source.Contains(value, comparison);
#endif
		}

		public static bool TryParseXmlDocument(this string xmlString, out System.Xml.XmlDocument xmlDocument)
		{
			try
			{
				xmlDocument = new System.Xml.XmlDocument();
				xmlDocument.LoadXml(xmlString);
				return true;
			}
			catch (Exception)
			{
				xmlDocument = null;
				return false;
			}
		}

		public static bool IsHtmlContent(this string content)
		{
			return content.TrimStart().StartsWith("<!DOCTYPE html", StringComparison.InvariantCultureIgnoreCase) ||
			       content.TrimStart().StartsWith("<html", StringComparison.InvariantCultureIgnoreCase) || content.HasKnownHtmlElements();
		}

		public static HashSet<string> KnownHtmlElements { get; } = new (StringComparer.OrdinalIgnoreCase)
		{
			"html", "head", "title", "base", "link", "meta", "style", "script", "noscript", "template", "body", "section", "nav", "article", "aside", "h1", "h2", "h3", "h4", "h5", "h6", "header", "footer", "address", "main", "p", "hr", "pre", "blockquote", "ol", "ul", "li", "dl", "dt", "dd", "figure", "figcaption", "div", "a", "em", "strong", "small", "s", "cite", "q", "dfn", "abbr", "data", "time", "code", "var", "samp", "kbd", "sub", "sup", "i", "b", "u", "mark", "ruby", "rt", "rp", "bdi", "bdo", "span", "br", "wbr", "ins", "del", "picture", "source", "img", "iframe", "embed", "object", "param", "video", "audio", "track", "map", "area", "table", "caption", "colgroup", "col", "tbody", "thead", "tfoot", "tr", "td", "th", "form", "label", "input", "button", "select", "datalist", "optgroup", "option", "textarea", "output", "progress", "meter", "fieldset", "legend", "details", "summary", "dialog", "script", "noscript", "template", "slot", "canvas", "table", "caption", "colgroup", "col", "tbody", "thead", "tfoot", "tr", "td", "th", "form", "label", "input", "button", "select", "datalist", "optgroup", "option", "textarea", "output", "progress", "meter", "fieldset", "legend", "details", "summary", "dialog", "script", "noscript", "template", "slot", "canvas", "a", "abbr", "address", "area", "article", "aside", "audio", "b", "base", "bdi"
		};

		public static bool HasKnownHtmlElements(this string input)
		{
			// Load the input string into an HtmlDocument
			var doc = new HtmlDocument();
			doc.LoadHtml(input);

			// Check if the document contains any element nodes
			return doc.DocumentNode.Descendants()
				.Any(node => node.NodeType == HtmlNodeType.Element && KnownHtmlElements.Contains(node.Name));
		}
	}
}
