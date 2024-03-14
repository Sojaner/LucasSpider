using System;

namespace DotnetSpider.DataFlow.Parser.Formatters
{
	/// <summary>
	/// Removes all leading and trailing white-space characters from the current System.String object.
	/// </summary>
	[AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
	public class TrimFormatter : Formatter
	{
		/// <summary>
		/// Trim type
		/// </summary>
		public enum TrimType
		{
			/// <summary>
			/// Only the back of Trim
			/// </summary>
			Right,
			/// <summary>
			/// Only the Trim front
			/// </summary>
			Left,

			/// <summary>
			/// Trim before and after
			/// </summary>
			All
		}

		/// <summary>
		/// Trim type
		/// </summary>
		public TrimType Type { get; set; } = TrimType.All;

		/// <summary>
		/// Achieve numerical conversion
		/// </summary>
		/// <param name="value">Value</param>
		/// <returns>The formatted value</returns>
		protected override string Handle(string value)
		{
			switch (Type)
			{
				case TrimType.All:
					{
						return value.Trim();
					}
				case TrimType.Left:
					{
						return value.TrimStart();
					}
				case TrimType.Right:
					{
						return value.TrimEnd();
					}
				default:
					{
						return value.Trim();
					}
			}
		}

		/// <summary>
		/// Verify whether the parameters are set correctly
		/// </summary>
		protected override void CheckArguments()
		{
		}
	}
}
