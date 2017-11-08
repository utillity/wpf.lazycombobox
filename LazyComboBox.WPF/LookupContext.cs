using System.Collections;
using System.Threading;

namespace uTILLIty.Controls.WPF.LazyComboBox
{
    /// <summary>
    ///   The context passed to the <see cref="LazyComboBox.LookupAction" /> delegate, when the <see cref="LazyComboBox" />
    ///   needs to populate the popup-list
    /// </summary>
    public interface LookupContext
    {
        /// <summary>
        ///   A <see cref="CancellationToken" /> to periodically check for cancellation, in case the user continued
        ///   to enter data and a new lookup-request needs to be started
        /// </summary>
        CancellationToken CancellationToken { get; }

        /// <summary>
        ///   The user's input into the Textbox field of the <see cref="LazyComboBox" />
        /// </summary>
        string Input { get; }

        /// <summary>
        ///   The list of records returned by the <see cref="LazyComboBox.LookupAction" /> delegate. The current list is also
        ///   pre-populated,
        ///   if <see cref="NextPageRequested" /> is set to true
        /// </summary>
        IEnumerable LoadedList { get; set; }

        /// <summary>
        ///   Set by the <see cref="LazyComboBox.LookupAction" /> delegate to indicate that more records are available
        ///   and can be requested for the current <see cref="Input" />
        /// </summary>
        bool MoreDataAvailable { get; set; }

        /// <summary>
        ///   Requests the next page to be loaded, because the user has scrolled to the end of the current list and your
        ///   <see cref="LazyComboBox.LookupAction" /> has set <see cref="MoreDataAvailable" /> to true on the last run.
        ///   After loading the next page, you should add the newly loaded page to the end of the <see cref="LoadedList" />
        /// </summary>
        bool NextPageRequested { get; }

        /// <summary>
        ///   The currently <see cref="LazyComboBox.SelectedItem">selected item</see> of the <see cref="LazyComboBox" />
        /// </summary>
        object SelectedItem { get; }

        /// <summary>
        ///   An arbitrary state-object which is passed on subsequent requests to <see cref="LazyComboBox.LookupAction" />, if the
        ///   <see cref="NextPageRequested" /> property is set to true
        /// </summary>
        object Tag { get; set; }
    }
}