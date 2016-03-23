using System.Threading;

namespace uTILLIty.Controls.WPF.LazyComboBox
{
	public class LookupContext
	{
		internal LookupContext(string input, CancellationToken token)
		{
			Input = input;
			Token = token;
		}

		public string Input { get; internal set; }
		public CancellationToken Token { get; internal set; }
		public object Tag { get; set; }
	}
}