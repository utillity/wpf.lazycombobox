using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace uTILLIty.Controls.WPF.LazyComboBox
{
	public static class DependencyObjectExtensions
	{
		public static ScrollViewer GetScrollViewer(this DependencyObject o)
		{
			// Return the DependencyObject if it is a ScrollViewer
			if (o is ScrollViewer)
			{
				return (ScrollViewer) o;
			}

			for (var i = 0; i < VisualTreeHelper.GetChildrenCount(o); i++)
			{
				var child = VisualTreeHelper.GetChild(o, i);

				var result = GetScrollViewer(child);
				if (result == null)
				{
				}
				else
				{
					return result;
				}
			}
			return null;
		}
	}
}