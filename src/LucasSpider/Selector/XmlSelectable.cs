using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using LucasSpider.Extensions;
using LucasSpider.Infrastructure;

namespace LucasSpider.Selector
{
	public class XmlSelectable : Selectable
	{
		private readonly XmlNode _node;

		public XmlSelectable(XmlNode node)
		{
			_node = node;
		}

		public XmlSelectable(string text)
		{
			_node = text.TryParseXmlDocument(out var node) ? node : null;
		}

		public override IEnumerable<string> Links()
		{
			throw new NotImplementedException();
		}

		public override IEnumerable<ISelectable> Nodes()
		{
			return _node?.ChildNodes.Cast<XmlNode>().Select(xmlNode => new XmlSelectable(xmlNode));
		}

		public override string Value => OuterXml;

		public string InnerXml => _node?.InnerXml;

		public string OuterXml => _node?.OuterXml;

		public override ISelectable Select(ISelector selector)
		{
			selector.NotNull(nameof(selector));
			var selectable = selector.Select(_node?.OuterXml);
			return selectable is HtmlSelectable htmlSelectable ? new XmlSelectable(htmlSelectable.OuterHtml) : selectable;
		}

		public override IEnumerable<ISelectable> SelectList(ISelector selector)
		{
			selector.NotNull(nameof(selector));
			return selector.SelectList(_node?.OuterXml).Select(selectable => selectable is HtmlSelectable htmlSelectable ? new XmlSelectable(htmlSelectable.OuterHtml) : selectable);
		}

		public override object Clone()
		{
			return new XmlSelectable(_node.Clone());
		}

		public override SelectableType Type => SelectableType.Xml;
	}
}
