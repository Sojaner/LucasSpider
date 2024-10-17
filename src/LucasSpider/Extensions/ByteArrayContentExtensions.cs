using System.Linq;
using System.Text;
using LucasSpider.Http;

namespace LucasSpider.Extensions;

public static class ByteArrayContentExtensions
{
	public static bool HasContent(this ByteArrayContent content)
	{
		return content is { Bytes.Length: > 0 };
	}

	public static bool HasContentType(this ByteArrayContent content)
	{
		return content.HasContent() && content is { Headers.ContentType: not null };
	}

	public static bool IsTextContent(this ByteArrayContent content)
	{
		if (!content.HasContent())
		{
			return false;
		}

		if (content.HasContentType())
		{
			var contentType = content.Headers.ContentType.ToLower();

			return contentType.StartsWith("text/") || contentType switch
			{
				"application/json" => true,
				"application/xml" => true,
				"application/xhtml+xml" => true,
				"application/ld+json" => true,
				"application/javascript" => true,
				_ => false
			};
		}

		return content.IsTextData() && content.GetEncoding().GetString(content.Bytes).IsHtmlContent();
	}

	public static bool IsTextData(this ByteArrayContent content)
	{
		if (!TryDecodeWithEncoding(content.Bytes, Encoding.UTF8))
		{
			return false;
		}

		return !HasExcessiveControlCharacters(content.Bytes);
	}

	private static bool TryDecodeWithEncoding(byte[] data, Encoding encoding)
	{
		try
		{
			var decoder = encoding.GetDecoder();
			decoder.Fallback = DecoderFallback.ExceptionFallback;
			var chars = new char[encoding.GetCharCount(data, 0, data.Length)];
			decoder.GetChars(data, 0, data.Length, chars, 0);
			return true;
		}
		catch (DecoderFallbackException)
		{
			return false;
		}
	}

	private static bool HasExcessiveControlCharacters(byte[] data)
	{
		var totalChars = data.Length;

		var controlChars = data.Count(b => b == 0x00 || (b < 0x20 && b != '\r' && b != '\n' && b != '\t'));

		var controlCharRatio = (double)controlChars / totalChars;
		return controlCharRatio > 0.05;
	}
}
