using System;
using System.Collections;
using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;

namespace uTILLIty.Controls.WPF.LazyComboBox
{
	[TemplatePart(Name = "PART_Popup", Type = typeof (Popup))]
	[TemplatePart(Name = "PART_TextBox", Type = typeof (TextBox))]
	[TemplatePart(Name = "PART_SelectedItemColumn", Type = typeof (Border))]
	public class LazyComboBox : Control, INotifyPropertyChanged
	{
		private ICollectionView _itemsView;
		private ScrollViewer _scroller;
		private Border _selItemCol;
		private TextBox _textBox;

		private bool _textChangedFromCode;

		static LazyComboBox()
		{
			DefaultStyleKeyProperty.OverrideMetadata(typeof (LazyComboBox), new FrameworkPropertyMetadata(typeof (LazyComboBox)));
		}

		private ICollectionView ItemsView
		{
			get
			{
				if (_itemsView == null && ItemsSource != null)
				{
					var s = CollectionViewSource.GetDefaultView(ItemsSource);
					using (s.DeferRefresh())
					{
						s.SortDescriptions.Clear();
						s.GroupDescriptions.Clear();
						//s.SortDescriptions.Add(new SortDescription(nameof(class.property), ListSortDirection.Ascending));
						//s.GroupDescriptions.Add(new PropertyGroupDescription(nameof(class.property)));
						//s.Filter = FilterItemsViewItem;
					}
					_itemsView = s;
				}
				return _itemsView;
			}
		}

		public event PropertyChangedEventHandler PropertyChanged;

		public override void OnApplyTemplate()
		{
			base.OnApplyTemplate();
			_scroller = (ScrollViewer) Template.FindName("PART_ScrollViewer", this);
			_scroller.ScrollChanged += OnScrolled;

			_selItemCol = (Border) Template.FindName("PART_SelectedItemColumn", this);
			_selItemCol.PreviewMouseLeftButtonDown += OnSelectedItemContentClicked;

			_textBox = (TextBox) Template.FindName("PART_TextBox", this);
			_textBox.PreviewKeyDown += OnTextBoxKeyPressed;
			_textBox.TextChanged += OnTextChanged;
			_textBox.LostFocus += OnTextBoxLostFocus;
		}

		private void OnTextBoxKeyPressed(object sender, KeyEventArgs e)
		{
			var view = ItemsView;
			if (view == null)
				return;

			switch (e.Key)
			{
				case Key.Right:
				case Key.Down:
					view.MoveCurrentToNext();
					break;
				case Key.Left:
				case Key.Up:
					view.MoveCurrentToPrevious();
					break;
			}
		}

		private void OnTextBoxLostFocus(object sender, RoutedEventArgs e)
		{
			IsEditing = false;
		}

		private void OnScrolled(object sender, ScrollChangedEventArgs e)
		{
			Debug.WriteLine($"Scrolled: VerticalChange={e.VerticalChange}, VerticalOffset={e.VerticalOffset}");
		}

		private void OnSelectedItemContentClicked(object sender, MouseButtonEventArgs e)
		{
			IsEditing = true;
			_textBox.Focus();
			_textBox.SelectionStart = 0;
			_textBox.SelectionLength = _textBox.Text?.Length ?? 0;
		}

		private void OnTextChanged(object sender, TextChangedEventArgs e)
		{
			if (_textChangedFromCode)
				return;
			Debug.WriteLine("Text changed by user");
		}

		private void UpdateTypedText()
		{
			var selItem = SelectedItem;
			string text = null;
			if (selItem != null)
			{
				var pi = TryGetProperty(DisplayMember, selItem.GetType());
				text = pi == null ? selItem.ToString() : pi.GetValue(selItem)?.ToString();
			}
			try
			{
				_textChangedFromCode = true;
				TypedText = text;
			}
			finally
			{
				_textChangedFromCode = false;
			}
		}

		private PropertyInfo TryGetProperty(string propertyName, Type type)
		{
			if (string.IsNullOrEmpty(propertyName))
				return null;
			var pi = type?.GetProperty(propertyName,
				BindingFlags.Instance | BindingFlags.FlattenHierarchy | BindingFlags.IgnoreCase | BindingFlags.Public);
			return pi;
		}

		protected internal void ResetItemsView()
		{
			_itemsView = null;
			// ReSharper disable once ExplicitCallerInfoArgument
			RaisePropertyChanged(nameof(ItemsView));
		}

		[NotifyPropertyChangedInvocator]
		protected virtual void RaisePropertyChanged([CallerMemberName] string propertyName = null)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}

		#region DropDownButtonStyle Property

		public static readonly DependencyProperty DropDownButtonStyleProperty = DependencyProperty
			.Register("DropDownButtonStyle", typeof (Style), typeof (LazyComboBox));

		public Style DropDownButtonStyle
		{
			get { return (Style) GetValue(DropDownButtonStyleProperty); }
			set { SetValue(DropDownButtonStyleProperty, value); }
		}

		#endregion

		#region PopupBorderStyle Property

		public static readonly DependencyProperty PopupBorderStyleProperty = DependencyProperty
			.Register(nameof(PopupBorderStyle), typeof (Style), typeof (LazyComboBox)
			/*, new FrameworkPropertyMetadata(string.Empty, OnPopupBorderStyleChanged)*/);

		public Style PopupBorderStyle
		{
			get { return (Style) GetValue(PopupBorderStyleProperty); }
			set { SetValue(PopupBorderStyleProperty, value); }
		}

		#endregion

		#region ListStyle Property

		public static readonly DependencyProperty ListStyleProperty = DependencyProperty
			.Register(nameof(ListStyle), typeof (Style), typeof (LazyComboBox)
			/*, new FrameworkPropertyMetadata(string.Empty, OnListStyleChanged)*/);

		public Style ListStyle
		{
			get { return (Style) GetValue(ListStyleProperty); }
			set { SetValue(ListStyleProperty, value); }
		}

		#endregion

		#region SelectedItem Property

		public static readonly DependencyProperty SelectedItemProperty = DependencyProperty
			.Register(nameof(SelectedItem), typeof (object), typeof (LazyComboBox)
				, new FrameworkPropertyMetadata(string.Empty, OnSelectedItemChanged));

		private static void OnSelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			var t = (LazyComboBox) d;
			t.UpdateTypedText();
			t.IsEditing = false;
			t.IsDropDownOpen = false;
		}

		public object SelectedItem
		{
			get { return GetValue(SelectedItemProperty); }
			set { SetValue(SelectedItemProperty, value); }
		}

		#endregion

		#region ItemsSource Property

		public static readonly DependencyProperty ItemsSourceProperty = DependencyProperty
			.Register(nameof(ItemsSource), typeof (IEnumerable), typeof (LazyComboBox)
			/*, new FrameworkPropertyMetadata(string.Empty, OnItemsSourceChanged)*/);

		public string ItemsSource
		{
			get { return (string) GetValue(ItemsSourceProperty); }
			set { SetValue(ItemsSourceProperty, value); }
		}

		#endregion

		#region DisplayMember Property

		public static readonly DependencyProperty DisplayMemberProperty = DependencyProperty
			.Register(nameof(DisplayMember), typeof (string), typeof (LazyComboBox)
			/*, new FrameworkPropertyMetadata(string.Empty, OnDisplayMemberChanged)*/);

		public string DisplayMember
		{
			get { return (string) GetValue(DisplayMemberProperty); }
			set { SetValue(DisplayMemberProperty, value); }
		}

		#endregion

		#region TypedText Property

		public static readonly DependencyProperty TypedTextProperty = DependencyProperty
			.Register(nameof(TypedText), typeof (string), typeof (LazyComboBox)
			/*, new FrameworkPropertyMetadata(string.Empty, OnTypedTextChanged)*/);

		public string TypedText
		{
			get { return (string) GetValue(TypedTextProperty); }
			set { SetValue(TypedTextProperty, value); }
		}

		#endregion

		#region IsEditing Property

		public static readonly DependencyProperty IsEditingProperty = DependencyProperty
			.Register(nameof(IsEditing), typeof (bool), typeof (LazyComboBox)
			/*, new FrameworkPropertyMetadata(string.Empty, OnIsEditingChanged)*/);

		public bool IsEditing
		{
			get { return (bool) GetValue(IsEditingProperty); }
			set { SetValue(IsEditingProperty, value); }
		}

		#endregion

		#region IsDropDownOpen Property

		public static readonly DependencyProperty IsDropDownOpenProperty = DependencyProperty
			.Register(nameof(IsDropDownOpen), typeof (bool), typeof (LazyComboBox)
			/*, new FrameworkPropertyMetadata(string.Empty, OnIsDropDownOpenChanged)*/);

		private bool IsDropDownOpen
		{
			get { return (bool) GetValue(IsDropDownOpenProperty); }
			set { SetValue(IsDropDownOpenProperty, value); }
		}

		#endregion
	}
}