using DotnetSpider.Http;

namespace DotnetSpider.Infrastructure
{
	/// <summary>
	/// Requested hash calculation interface
	/// </summary>
	public interface IRequestHasher
	{
		/// <summary>
		/// Compile Hash
		/// </summary>
		/// <returns></returns>
		void ComputeHash(Request request);
	}
}
