﻿using System;
using System.Collections;
using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;

namespace uTILLIty.Controls.WPF.LazyComboBox
{
	[TemplatePart(Name = "PART_Popup", Type = typeof (Popup))]
	[TemplatePart(Name = "PART_TextBox", Type = typeof (TextBox))]
	[TemplatePart(Name = "PART_ListView", Type = typeof (ListView))]
	[TemplatePart(Name = "PART_SelectedItemColumn", Type = typeof (Border))]
	public class LazyComboBox : Control, INotifyPropertyChanged
	{
		private ICollectionView _itemsView;
		private ListView _listView;
		private ScrollViewer _scroller;
		private Border _selItemCol;
		private TextBox _textBox;

		private bool _textChangedFromCode;

		private CancellationTokenSource _token;

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

			_listView = (ListView) Template.FindName("PART_ListView", this);

			_textBox = (TextBox) Template.FindName("PART_TextBox", this);
			_textBox.PreviewKeyDown += OnTextBoxKeyPressed;
			//_textBox.TextChanged += OnTextChanged;
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
					IsDropDownOpen = true;
					view.MoveCurrentToNext();
					_listView.ScrollIntoView(view.CurrentItem);
					e.Handled = true;
					break;
				case Key.Left:
				case Key.Up:
					IsDropDownOpen = true;
					view.MoveCurrentToPrevious();
					_listView.ScrollIntoView(view.CurrentItem);
					e.Handled = true;
					break;
				case Key.Return:
					if (view.CurrentItem != null)
					{
						SelectedItem = view.CurrentItem;
						IsDropDownOpen = false;
						e.Handled = true;
					}
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

		private async void OnTextChanged()
		{
			if (_textChangedFromCode && !ListUpdating)
				return;
			//Debug.WriteLine("Text changed by user");
			var action = LookupAction;
			if (action != null)
			{
				try
				{
					_token?.Cancel(true);
					Dispatcher.InvokeAsync(() => ListUpdating = true);
					var input = _textBox.Text;
					_token = new CancellationTokenSource();
					var ctx = new LookupContext(input, _token.Token);
					await Task.Run(() => { action.Invoke(ctx); }); //.ConfigureAwait(true);
					//ConfigureAwait must be true to be back in UI thread afterward
					IsDropDownOpen = true;
				}
				finally
				{
					Dispatcher.InvokeAsync(() => ListUpdating = false);
				}
			}
		}

		private void UpdateTypedText()
		{
			var selItem = SelectedItem;
			string text = null;
			if (selItem != null)
			{
				var pi = TryGetProperty(TextMember, selItem.GetType());
				text = pi == null ? selItem.ToString() : pi.GetValue(selItem)?.ToString();
			}
			try
			{
				_textChangedFromCode = true;
				TypedText = text;
				SelectedItemText = text;
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

		#region ListUpdating Property

		public static readonly DependencyProperty ListUpdatingProperty = DependencyProperty
			.Register(nameof(ListUpdating), typeof (bool), typeof (LazyComboBox)
			/*, new FrameworkPropertyMetadata(string.Empty, OnListUpdatingChanged)*/);

		public bool ListUpdating
		{
			get { return (bool) GetValue(ListUpdatingProperty); }
			private set { SetValue(ListUpdatingProperty, value); }
		}

		#endregion

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

		#region SelectedItemText Property

		public static readonly DependencyProperty SelectedItemTextProperty = DependencyProperty
			.Register(nameof(SelectedItemText), typeof (string), typeof (LazyComboBox)
			/*, new FrameworkPropertyMetadata(string.Empty, OnSelectedItemTextChanged)*/);

		private string SelectedItemText
		{
			get { return (string) GetValue(SelectedItemTextProperty); }
			set { SetValue(SelectedItemTextProperty, value); }
		}

		#endregion

		#region ItemsSource Property

		public static readonly DependencyProperty ItemsSourceProperty = DependencyProperty
			.Register(nameof(ItemsSource), typeof (IEnumerable), typeof (LazyComboBox)
			/*, new FrameworkPropertyMetadata(string.Empty, OnItemsSourceChanged)*/);

		public IEnumerable ItemsSource
		{
			get { return (IEnumerable) GetValue(ItemsSourceProperty); }
			set { SetValue(ItemsSourceProperty, value); }
		}

		#endregion

		#region LookupAction Property

		public static readonly DependencyProperty LookupActionProperty = DependencyProperty
			.Register(nameof(LookupAction), typeof (Action<LookupContext>), typeof (LazyComboBox)
			/*, new FrameworkPropertyMetadata(string.Empty, OnLookupActionChanged)*/);

		public Action<LookupContext> LookupAction
		{
			get { return (Action<LookupContext>) GetValue(LookupActionProperty); }
			set { SetValue(LookupActionProperty, value); }
		}

		#endregion

		#region TextBoxStyle Property

		public static readonly DependencyProperty TextBoxStyleProperty = DependencyProperty
			.Register(nameof(TextBoxStyle), typeof (Style), typeof (LazyComboBox)
			/*, new FrameworkPropertyMetadata(string.Empty, OnTextBoxStyleChanged)*/);

		public Style TextBoxStyle
		{
			get { return (Style) GetValue(TextBoxStyleProperty); }
			set { SetValue(TextBoxStyleProperty, value); }
		}

		#endregion

		#region TextMember Property

		public static readonly DependencyProperty TextMemberProperty = DependencyProperty
			.Register(nameof(TextMember), typeof (string), typeof (LazyComboBox)
			/*, new FrameworkPropertyMetadata(string.Empty, OnTextMemberChanged)*/);

		public string TextMember
		{
			get { return (string) GetValue(TextMemberProperty); }
			set { SetValue(TextMemberProperty, value); }
		}

		#endregion

		#region TypedText Property

		public static readonly DependencyProperty TypedTextProperty = DependencyProperty
			.Register(nameof(TypedText), typeof (string), typeof (LazyComboBox)
				, new FrameworkPropertyMetadata(string.Empty, OnTypedTextChanged));

		private static void OnTypedTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			var t = (LazyComboBox) d;
			t.OnTextChanged();
		}

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