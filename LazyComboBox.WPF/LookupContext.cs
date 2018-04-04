using System.Collections;
using System.Threading;

// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace uTILLIty.Controls.WPF.LazyComboBox
{
	/// <summary>
	///   The context passed to the <see cref="LazyComboBox.LookupAction" /> delegate, when the <see cref="LazyComboBox" />
	///   needs to populate the popup-list
	/// </summary>
	public class LookupContext
	{
		internal LookupContext(string input, CancellationToken token, object tag, LazyComboBox cb)
		{
			SelectedItem = cb.SelectedItem;
			Input = input;
			CancellationToken = token;
			Tag = tag;
		}

		/// <summary>
		///   The currently <see cref="LazyComboBox.SelectedItem">selected item</see> of the <see cref="LazyComboBox" />
		/// </summary>
		public object SelectedItem { get; private set; }

		/// <summary>
		///   The user's input into the Textbox field of the <see cref="LazyComboBox" />
		/// </summary>
		public string Input { get; private set; }

		/// <summary>
		///   A <see cref="CancellationToken" /> to periodically check for cancellation, in case the user continued
		///   to enter data and a new lookup-request needs to be started
		/// </summary>
		public CancellationToken CancellationToken { get; internal set; }

		/// <summary>
		///   An arbitrary state-object which is passed on subsequent requests to <see cref="LazyComboBox.LookupAction" />, if the
		///   <see cref="NextPageRequested" /> property is set to true
		/// </summary>
		public object Tag { get; set; }

		/// <summary>
		///   Requests the next page to be loaded, because the user has scrolled to the end of the current list and your
		///   <see cref="LazyComboBox.LookupAction" /> has set <see cref="MoreDataAvailable" /> to true on the last run.
		///   After loading the next page, you should add the newly loaded page to the end of the <see cref="LoadedList" />
		/// </summary>
		public bool NextPageRequested { get; internal set; }

		/// <summary>
		///   The list of records returned by the <see cref="LazyComboBox.LookupAction" /> delegate. The current list is also
		///   pre-populated,
		///   if <see cref="NextPageRequested" /> is set to true
		/// </summary>
		public IEnumerable LoadedList { get; set; }

		/// <summary>
		///   Set by the <see cref="LazyComboBox.LookupAction" /> delegate to indicate that more records are available
		///   and can be requested for the current <see cref="Input" />
		/// </summary>
		public bool MoreDataAvailable { get; set; }
	}
}