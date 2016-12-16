using System.Collections;
using System.Threading;

namespace uTILLIty.Controls.WPF.LazyComboBox
{
	public class LookupContext
	{
		internal LookupContext(string input, CancellationToken token, object tag, LazyComboBox cb)
		{
			SelectedItem = cb.SelectedItem;
			Input = input;
			CancellationToken = token;
			Tag = tag;
		}

		public object SelectedItem { get; private set; }
		public string Input { get; private set; }
		public CancellationToken CancellationToken { get; private set; }
		public object Tag { get; set; }
		public bool NextPageRequested { get; internal set; }

		public IEnumerable LoadedList { get; set; }
		public bool MoreDataAvailable { get; set; }
	}
}