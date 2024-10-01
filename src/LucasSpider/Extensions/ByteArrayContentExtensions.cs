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

		var contentType = content.HasContentType() ? content.Headers.ContentType.ToLower() : "";

		return contentType.StartsWith("text/") || contentType switch
		{
			"application/json" => true,
			"application/xml" => true,
			"application/xhtml+xml" => true,
			"application/ld+json" => true,
			"application/javascript" => true,
			_ => false
		} || content.GetEncoding().GetString(content.Bytes).IsHtmlContent();
	}
}
