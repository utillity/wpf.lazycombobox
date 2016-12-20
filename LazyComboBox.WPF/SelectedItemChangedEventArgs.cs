using System;

namespace uTILLIty.Controls.WPF.LazyComboBox
{
	public class SelectedItemChangedEventArgs : EventArgs
	{
		public SelectedItemChangedEventArgs(object oldItem, object newItem)
		{
			OldItem = oldItem;
			NewItem = newItem;
		}

		public object OldItem { get; private set; }
		public object NewItem { get; private set; }
	}
}