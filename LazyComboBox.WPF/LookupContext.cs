using System.Threading;

namespace uTILLIty.Controls.WPF.LazyComboBox
{
	public class LookupContext
	{
		internal LookupContext(string input, CancellationToken token, object tag)
		{
			Input = input;
			CancellationToken = token;
			Tag = tag;
		}

		public string Input { get; internal set; }
		public CancellationToken CancellationToken { get; internal set; }
		public object Tag { get; set; }
	}
}