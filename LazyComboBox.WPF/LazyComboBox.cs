using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

// ReSharper disable EventNeverSubscribedTo.Global

namespace uTILLIty.Controls.WPF.LazyComboBox
{
	[TemplatePart(Name = "PART_TextBox", Type = typeof(TextBox))]
	[TemplatePart(Name = "PART_ListView", Type = typeof(ListView))]
	[TemplatePart(Name = "PART_SelectedItemColumn", Type = typeof(FrameworkElement))]
	// ReSharper disable once ClassWithVirtualMembersNeverInherited.Global
	public class LazyComboBox : Control, INotifyPropertyChanged
	{
		private bool _applySelection;
		private PropertyInfo _displayProp;

		private ICollectionView _itemsView;

		private LookupContext _lastContext;

		private int _lastIdx = -1;

		private DateTime _lastLoadFromScroll;
		private ListView _listView;

		private FrameworkElement _selItemCol;
		private TextBox _textBox;

		private bool _textChangedFromCode;

		private CancellationTokenSource _token;

		static LazyComboBox()
		{
			DefaultStyleKeyProperty.OverrideMetadata(typeof(LazyComboBox), new FrameworkPropertyMetadata(typeof(LazyComboBox)));
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
					}
					_itemsView = s;
				}
				return _itemsView;
			}
		}

		public event PropertyChangedEventHandler PropertyChanged;

		/// <summary>
		///   Occurs when the selection of a <see cref="T:System.Windows.Controls.Primitives.Selector" /> changes.
		/// </summary>
		[Category("Behavior")]
		public event EventHandler<SelectedItemChangedEventArgs> SelectedItemChanged;

		public event EventHandler DropDownOpened;

		public override void OnApplyTemplate()
		{
			base.OnApplyTemplate();

			_selItemCol = (FrameworkElement) Template.FindName("PART_SelectedItemColumn", this);
			_selItemCol.PreviewMouseLeftButtonDown += OnSelectedItemContentClicked;

			_listView = (ListView) Template.FindName("PART_ListView", this);
			_listView.ItemContainerGenerator.StatusChanged += OnListViewItemsChanged;
			_listView.AddHandler(ScrollViewer.ScrollChangedEvent, new RoutedEventHandler(OnScrolled));
			//_listView.AddHandler(ScrollBar.ScrollEvent, new RoutedEventHandler(OnScrolled));
			_listView.AddHandler(Selector.SelectionChangedEvent, new RoutedEventHandler(OnListItemChanged));
			_listView.AddHandler(PreviewMouseDownEvent, new RoutedEventHandler(OnListClicked));
			_listView.AddHandler(LostFocusEvent, new RoutedEventHandler(OnListLostFocus));

			//_popup = (Popup) Template.FindName("PART_Popup", this);

			_textBox = (TextBox) Template.FindName("PART_TextBox", this);
			_textBox.PreviewKeyDown += (o, e) => OnPreviewKeyDown(e);
			_textBox.LostFocus += OnTextBoxLostFocus;
		}

		private void OnListViewItemsChanged(object sender, EventArgs e)
		{
			switch (_listView.ItemContainerGenerator.Status)
			{
				case GeneratorStatus.ContainersGenerated:
					var idx = _lastIdx;
					if (idx < 0) return;
					_lastIdx = -1;
					var view = ItemsView;
					view.MoveCurrentToPosition(idx);
					_listView.ScrollIntoView(view.CurrentItem);
					break;
			}
		}

		private void OnListLostFocus(object sender, RoutedEventArgs e)
		{
			IsDropDownOpen = false;
			UpdateSelection(_listView.SelectedItem);
		}

		private void OnListItemChanged(object sender, RoutedEventArgs e)
		{
			//occurs on change of the current item of the view!
			//var view = ItemsView;
			//if (view == null)
			//	return;

			//Debug.WriteLine($"OnListItemChanged: Setting SelectedItem to {_listView.SelectedItem}", nameof(LazyComboBox));
			//UpdateSelection(_listView.SelectedItem);
			if (_applySelection)
			{
				_applySelection = false;
				Debug.WriteLine($"OnListClicked: Setting SelectedItem to {_listView.SelectedItem}", nameof(LazyComboBox));
				UpdateSelection(_listView.SelectedItem);
				//UpdateTypedText(_listView.SelectedItem);
				IsDropDownOpen = false;
			}
		}

		private void OnListClicked(object sender, RoutedEventArgs e)
		{
			var view = ItemsView;
			if (view == null)
				return;

			var cur = (DependencyObject) e.OriginalSource;
			while (cur != null)
			{
				//Debug.WriteLine($"OnListClicked: Source={cur}", nameof(LazyComboBox));
				cur = VisualTreeHelper.GetParent(cur);
				if (cur is ListViewItem) break;
				if (cur == null || cur is ScrollBar)
					return;
			}
			_applySelection = true;
		}

		private void UpdateSelection(object item)
		{
			Debug.WriteLine($"Updating SelectedItem to {item}", nameof(LazyComboBox));
			SelectedItem = item;

			var path = SelectedValuePath;
			if (!string.IsNullOrEmpty(path))
			{
				var value = TryGetPropertyValueByPath(item, path);
				Debug.WriteLine($"Updating SelectedValue to {value} (from {path})", nameof(LazyComboBox));
				SelectedValue = value;
			}
		}

		private void RaiseDropDownOpened()
		{
			DropDownOpened?.Invoke(this, EventArgs.Empty);
		}

		protected override void OnPreviewKeyDown(KeyEventArgs e)
		{
			var view = ItemsView;

			var key = e.Key;
			switch (key)
			{
				case Key.Back:
				case Key.Delete:
					if (IsEditing && SelectedItem != null)
					{
						if (!string.IsNullOrEmpty(Text))
						{
							var tb = _textBox;
							if (tb != null)
							{
								//not everything is selected, DELETE won't clear input
								if (!Equals(Text, tb.SelectedText))
									return; //don't set IsHandled to true!
							}
						}
						UpdateSelection(null);
						UpdateTypedText(null);
						if (IsDropDownOpen)
							ExecuteLookup(null);
					}
					return;
				case Key.Tab:
					return;
				case Key.PageDown:
				{
					if (view == null)
						return;
					var sv = _listView.GetScrollViewer();
					var pageSize = (int) sv.ViewportHeight + 1;
					var newPos = view.CurrentPosition + pageSize;
					if (newPos >= _listView.Items.Count)
						newPos = _listView.Items.Count - 1;
					view.MoveCurrentToPosition(newPos);
					_listView.ScrollIntoView(view.CurrentItem);
				}
					break;
				case Key.PageUp:
				{
					if (view == null)
						return;
					var sv = _listView.GetScrollViewer();
					var pageSize = (int) sv.ViewportHeight + 1;
					var newPos = view.CurrentPosition - pageSize;
					if (newPos < 0) newPos = 0;
					view.MoveCurrentToPosition(newPos);
					_listView.ScrollIntoView(view.CurrentItem);
				}
					break;
				case Key.Home:
					if (view == null)
						return;
					if (IsEditable || Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift))
						return;
					IsDropDownOpen = true;
					view.MoveCurrentToFirst();
					_listView.SelectedItem = view.CurrentItem;
					_listView.ScrollIntoView(view.CurrentItem);
					break;
				case Key.End:
					if (view == null)
						return;
					if (IsEditable || Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift))
						return;
					IsDropDownOpen = true;
					view.MoveCurrentToLast();
					_listView.SelectedItem = view.CurrentItem;
					_listView.ScrollIntoView(view.CurrentItem);
					break;
				case Key.Down:
					IsDropDownOpen = true;
					if (view == null)
						return;
					if (_listView.SelectedItem == null)
						view.MoveCurrentToFirst();
					else
						view.MoveCurrentToNext();
					if (view.IsCurrentAfterLast)
					{
						var ctx = _lastContext;
						if (ctx?.MoreDataAvailable ?? false)
							RequestNextPage();
						else
							view.MoveCurrentToFirst();
					}
					_listView.SelectedItem = view.CurrentItem;
					_listView.ScrollIntoView(view.CurrentItem);
					break;
				case Key.Up:
					IsDropDownOpen = true;
					if (view == null)
						return;
					view.MoveCurrentToPrevious();
					if (view.IsCurrentBeforeFirst)
						view.MoveCurrentToLast();
					_listView.SelectedItem = view.CurrentItem;
					_listView.ScrollIntoView(view.CurrentItem);
					break;
				case Key.Return:
					if (view == null)
						return;
					if (_listView.SelectedItem != null)
					{
						Debug.WriteLine($"OnPreviewKeyDown (RETURN): Setting SelectedItem to {_listView.SelectedItem}",
							nameof(LazyComboBox));
						UpdateSelection(_listView.SelectedItem);
						UpdateTypedText(_listView.SelectedItem);
					}
					IsEditing = false;
					IsDropDownOpen = false;
					break;
				default:
					if (!IsEditing && IsEditable)
						EnterEditMode();
					return;
			}
			e.Handled = true;
		}

		private void OnTextBoxLostFocus(object sender, RoutedEventArgs e)
		{
			IsEditing = false;
			IsDropDownOpen = false;
		}

		private void OnScrolled(object sender, RoutedEventArgs e)
		{
			var sv = _listView.GetScrollViewer();
			CheckScrollPositionForReload(sv.ScrollableHeight, 0, sv.ContentVerticalOffset);
		}

		private void CheckScrollPositionForReload(double max, double min, double cur)
		{
			if (DateTime.Now.Subtract(_lastLoadFromScroll).TotalSeconds < 0.5)
				return;
			var range = max - min;
			var cur2 = cur - min;
			var percent = cur2/range;
			Debug.WriteLine($"Scroll Position={cur:N1}, Max={max:N1} (range={range:N1}, cu2r={cur2:N1}, percent={percent:N1}",
				nameof(LazyComboBox));
			if (percent > 0.98)
			{
				_lastLoadFromScroll = DateTime.Now;
				RequestNextPage();
			}
		}

		private void RequestNextPage()
		{
			var action = LookupAction;
			var ctx = _lastContext;
			if (action == null || !(ctx?.MoreDataAvailable ?? false))
				return;

			ctx.NextPageRequested = true;
			_lastIdx = ItemsView.CurrentPosition;
			ExecuteLookup(ctx, false);
		}

		private void OnSelectedItemContentClicked(object sender, MouseButtonEventArgs e)
		{
			// activate Textbox and Dropdown asynchronously, as synchronous causes problem with ScrollViewer for some reason
			Task.Run(() =>
			{
				Dispatcher.Invoke(() =>
				{
					if (IsEditable)
					{
						EnterEditMode();
						if (e.ClickCount > 1)
						{
							Debug.WriteLine("Opening LazyComboBox DropDown, because Content was clicked", nameof(LazyComboBox));
							IsDropDownOpen = true;
						}
					}
					else
					{
						IsDropDownOpen = !IsDropDownOpen;
					}
				}, DispatcherPriority.Input);
			}).ConfigureAwait(false);
		}

		private void EnterEditMode()
		{
			IsEditing = true;
			_textBox.SelectAll();
			_textBox.Focus();
		}

		private void OnTextChanged()
		{
			if (_textChangedFromCode)
				return;

			if (IsTextPropertyBound())
			{
				if (TrySelectFirstCandidate())
					return;
				SelectedItemText = Text;
				ItemsView?.MoveCurrentTo(Text);
			}
			else
			{
				if (TrySelectFirstCandidate()) return;
			}
			_lastIdx = -1;
			ExecuteLookup(null, selectFirstCandidate: true);
		}

		private bool TrySelectFirstCandidate()
		{
			var text = Text ?? string.Empty;
			var ctx = _lastContext;
			//use local list to find candidate
			if (ItemsSource != null && (ctx == null || !ctx.MoreDataAvailable && text.StartsWith(ctx.Input ?? string.Empty)))
			{
				var candidates = TryLocateCandidatesByText(text).ToArray();
				var candidate = candidates.FirstOrDefault();
				if (candidates.Length > 1 && IsEditing)
					IsDropDownOpen = true;
				if (candidate != null)
				{
					UpdateTypedText(candidate);
					ItemsView?.MoveCurrentTo(candidate);
					UpdateSelection(candidate);
					return true;
				}
				UpdateSelection(null);
			}
			return false;
		}

		private IEnumerable<object> TryLocateCandidatesByText(string text)
		{
			var c =
				ItemsSource?.Cast<object>()
					.FirstOrDefault(i => string.Equals(text, GetTextValueOfItem(i), StringComparison.InvariantCultureIgnoreCase));
			if (c != null)
				yield return c;

			var candidates = ItemsSource?.Cast<object>()
				.Where(i => GetTextValueOfItem(i).IndexOf(text, StringComparison.OrdinalIgnoreCase) == 0);
			if (candidates != null)
			{
				foreach (var candidate in candidates)
				{
					yield return candidate;
				}
			}
		}

		private void ExecuteLookup(LookupContext ctx, bool async = true, bool selectFirstCandidate = false)
		{
#if DEBUG
			var source = new StackFrame(1).GetMethod().ToString();
			Debug.WriteLine($"Executing Lookup from {source}", nameof(LazyComboBox));
#endif

			var action = LookupAction;
			if (action == null)
				return;

			_token?.Cancel(true);
			_token = new CancellationTokenSource();

			var input = _textBox?.Text;
			ctx = ctx ?? new LookupContext(input, _token.Token, null, this);

			ListUpdating = true;
			Action x = () =>
			{
				action.Invoke(ctx);
				if (!_token.IsCancellationRequested)
				{
					_lastContext = ctx;
					Dispatcher.Invoke(() =>
					{
						ItemsSource = ctx.LoadedList;
						MoreDataAvailableContentVisibility = ctx.MoreDataAvailable ? Visibility.Visible : Visibility.Collapsed;
						if (selectFirstCandidate)
							TrySelectFirstCandidate();
						ListUpdating = false;
					});
				}
			};
			//ConfigureAwait must be true to be back in UI thread afterward
			if (async)
				Task.Run(x);
			else
				x();
		}

		private void UpdateTypedText(object selItem)
		{
			string text = null;
			if (selItem != null)
			{
				text = GetTextValueOfItem(selItem);
			}
			try
			{
				_textChangedFromCode = true;

#if DEBUG
				var source = new StackFrame(1).GetMethod().ToString();
				Debug.WriteLine($"Updating Text and SelectedItemText to '{text}' (Caller: {source})", nameof(LazyComboBox));
#endif

				var curIdx = _textBox?.SelectionStart ?? 0;
				Text = text;
				SelectedItemText = text;
				if (_textBox != null)
				{
					_textBox.Text = text;
					_textBox.SelectAll();
					_textBox.SelectionStart = curIdx;
				}
			}
			finally
			{
				_textChangedFromCode = false;
			}
		}

		private string GetTextValueOfItem(object selItem)
		{
			if (selItem == null)
				return string.Empty;
			string text;
			if (!string.IsNullOrEmpty(DisplayMemberPath))
			{
				text = TryGetPropertyValueByPath(selItem, DisplayMemberPath)?.ToString();
				if (!string.IsNullOrEmpty(text))
					return text;
			}
			if (_displayProp != null && _displayProp.DeclaringType != selItem.GetType())
				UpdateDisplayProp(selItem);
			var pi = _displayProp;
			text = pi == null ? selItem.ToString() : pi.GetValue(selItem)?.ToString() ?? selItem.ToString();
			return text;
		}

		private object TryGetPropertyValueByPath(object item, string path)
		{
			var parts = path.Split('.');
			var value = item;
			foreach (var part in parts)
			{
				var pi = value.GetType().GetRuntimeProperty(part);
				value = pi?.GetValue(value);
				if (value == null)
					break;
			}
			return value;
		}

		private void UpdateDisplayProp(object selItem)
		{
			_displayProp = string.IsNullOrEmpty(DisplayMemberPath) || selItem == null
				? null
				: TryGetProperty(DisplayMemberPath, selItem.GetType());
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

		protected override void OnLostFocus(RoutedEventArgs e)
		{
			base.OnLostFocus(e);
			IsDropDownOpen = false;
		}

		private void RaiseSelectedItemChanged(object oldItem, object newItem)
		{
			SelectedItemChanged?.Invoke(this, new SelectedItemChangedEventArgs(oldItem, newItem));
		}

		#region ListUpdating Property

		public static readonly DependencyProperty ListUpdatingProperty = DependencyProperty.Register(nameof(ListUpdating),
			typeof(bool), typeof(LazyComboBox)
			/*, new FrameworkPropertyMetadata(string.Empty, OnListUpdatingChanged)*/);

		public bool ListUpdating
		{
			get { return (bool) GetValue(ListUpdatingProperty); }
			private set { SetValue(ListUpdatingProperty, value); }
		}

		#endregion

		#region DropDownButtonStyle Property

		public static readonly DependencyProperty DropDownButtonStyleProperty =
			DependencyProperty.Register("DropDownButtonStyle", typeof(Style), typeof(LazyComboBox));

		public Style DropDownButtonStyle
		{
			get { return (Style) GetValue(DropDownButtonStyleProperty); }
			set { SetValue(DropDownButtonStyleProperty, value); }
		}

		#endregion

		#region PopupBorderStyle Property

		public static readonly DependencyProperty PopupBorderStyleProperty =
			DependencyProperty.Register(nameof(PopupBorderStyle), typeof(Style), typeof(LazyComboBox)
				/*, new FrameworkPropertyMetadata(string.Empty, OnPopupBorderStyleChanged)*/);

		public Style PopupBorderStyle
		{
			get { return (Style) GetValue(PopupBorderStyleProperty); }
			set { SetValue(PopupBorderStyleProperty, value); }
		}

		#endregion

		#region ListStyle Property

		public static readonly DependencyProperty ListStyleProperty = DependencyProperty.Register(nameof(ListStyle),
			typeof(Style), typeof(LazyComboBox)
			/*, new FrameworkPropertyMetadata(string.Empty, OnListStyleChanged)*/);

		public Style ListStyle
		{
			get { return (Style) GetValue(ListStyleProperty); }
			set { SetValue(ListStyleProperty, value); }
		}

		#endregion

		#region SelectedItemText Property

		public static readonly DependencyProperty SelectedItemTextProperty =
			DependencyProperty.Register(nameof(SelectedItemText), typeof(string), typeof(LazyComboBox),
				new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

		private string SelectedItemText
		{
			get { return (string) GetValue(SelectedItemTextProperty); }
			set { SetValue(SelectedItemTextProperty, value); }
		}

		#endregion

		#region LookupAction Property

		public static readonly DependencyProperty LookupActionProperty = DependencyProperty.Register(nameof(LookupAction),
			typeof(Action<LookupContext>), typeof(LazyComboBox)
			/*, new FrameworkPropertyMetadata(string.Empty, OnLookupActionChanged)*/);

		/// <summary>
		///   This action is called in a background thread to populate the <see cref="Selector.ItemsSource" /> with elements
		///   filtered by the user's input
		/// </summary>
		/// <remarks>
		///   The <see cref="LookupContext" /> provided can hold an arbitrary state object in the <see cref="LookupContext.Tag" />
		///   property, which will be passed to you on subsequent calls. Also check the
		///   <see cref="LookupContext.CancellationToken" /> frequently and abort your lookup-operation as soon as possible
		///   without setting the <see cref="Selector.ItemsSource" /> property. This operation is cancelled, if additional
		///   user-input
		///   has occured since calling the LookupAction
		/// </remarks>
		public Action<LookupContext> LookupAction
		{
			get { return (Action<LookupContext>) GetValue(LookupActionProperty); }
			set { SetValue(LookupActionProperty, value); }
		}

		#endregion

		#region IsEditable Property

		public static readonly DependencyProperty IsEditableProperty = DependencyProperty.Register(nameof(IsEditable),
			typeof(bool), typeof(LazyComboBox), new FrameworkPropertyMetadata(true));

		public bool IsEditable
		{
			get { return (bool) GetValue(IsEditableProperty); }
			set { SetValue(IsEditableProperty, value); }
		}

		#endregion

		#region TextBoxStyle Property

		public static readonly DependencyProperty TextBoxStyleProperty = DependencyProperty.Register(nameof(TextBoxStyle),
			typeof(Style), typeof(LazyComboBox)
			/*, new FrameworkPropertyMetadata(string.Empty, OnTextBoxStyleChanged)*/);

		public Style TextBoxStyle
		{
			get { return (Style) GetValue(TextBoxStyleProperty); }
			set { SetValue(TextBoxStyleProperty, value); }
		}

		#endregion

		#region Text Property

		public static readonly DependencyProperty TextProperty = DependencyProperty.Register(nameof(Text), typeof(string),
			typeof(LazyComboBox),
			new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnTypedTextChanged));

		private static void OnTypedTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			var t = (LazyComboBox) d;
			t.OnTextChanged();
		}

		public string Text
		{
			get { return (string) GetValue(TextProperty); }
			set { SetValue(TextProperty, value); }
		}

		private bool IsTextPropertyBound()
		{
			return BindingOperations.GetBinding(this, TextProperty) != null;
		}

		#endregion

		#region IsEditing Property

		public static readonly DependencyProperty IsEditingProperty = DependencyProperty.Register(nameof(IsEditing),
			typeof(bool), typeof(LazyComboBox)
			/*, new FrameworkPropertyMetadata(string.Empty, OnIsEditingChanged)*/);

		internal bool IsEditing
		{
			get { return (bool) GetValue(IsEditingProperty); }
			set { SetValue(IsEditingProperty, value); }
		}

		#endregion

		#region MoreDataAvailableContent Property

		public static readonly DependencyProperty MoreDataAvailableContentProperty =
			DependencyProperty.Register(nameof(MoreDataAvailableContent), typeof(object), typeof(LazyComboBox),
				new FrameworkPropertyMetadata("..."));

		public object MoreDataAvailableContent
		{
			get { return GetValue(MoreDataAvailableContentProperty); }
			set { SetValue(MoreDataAvailableContentProperty, value); }
		}

		#endregion

		#region MoreDataAvailableContentVisibility Property

		public static readonly DependencyProperty MoreDataAvailableContentVisibilityProperty =
			DependencyProperty.Register(nameof(MoreDataAvailableContentVisibility), typeof(Visibility), typeof(LazyComboBox)
				, new FrameworkPropertyMetadata(Visibility.Collapsed));

		private Visibility MoreDataAvailableContentVisibility
		{
			get { return (Visibility) GetValue(MoreDataAvailableContentVisibilityProperty); }
			set { SetValue(MoreDataAvailableContentVisibilityProperty, value); }
		}

		#endregion

		#region IsDropDownOpen Property

		public static readonly DependencyProperty IsDropDownOpenProperty = DependencyProperty.Register(
			nameof(IsDropDownOpen), typeof(bool), typeof(LazyComboBox),
			new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnIsDropDownOpenChanged));

		private static void OnIsDropDownOpenChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			var t = (LazyComboBox) d;
			if (t.IsDropDownOpen)
			{
				if (t.ItemsSource == null)
					t.ExecuteLookup(null);
				t.RaiseDropDownOpened();
				if (!t.IsEditing)
					Keyboard.Focus(t);
			}
		}

		private bool IsDropDownOpen
		{
			get { return (bool) GetValue(IsDropDownOpenProperty); }
			set { SetValue(IsDropDownOpenProperty, value); }
		}

		#endregion

		#region ItemsSource Property

		public static readonly DependencyProperty ItemsSourceProperty = DependencyProperty
			.Register(nameof(ItemsSource), typeof(IEnumerable), typeof(LazyComboBox)
				, new FrameworkPropertyMetadata(null, OnItemsSourceChanged));

		private static void OnItemsSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			var t = (LazyComboBox) d;
			//var updatedFromLookup = t.ListUpdating;
			t.ResetItemsView();
			var view = t.ItemsView;
			var item = t.SelectedItem;
			if (!t.IsEditing && item != null && (view == null || !view.Contains(item)))
			{
				t.UpdateSelection(null);
			}
			else if (!string.IsNullOrEmpty(t.SelectedValuePath) && t.SelectedValue != null)
			{
				t.TrySetSelectedItemByValue(t.SelectedValue);
			}

			if (t.IsTextPropertyBound())
			{
				view?.MoveCurrentTo(t.Text);
			}
			else if (!t.IsEditing)
			{
				t.UpdateTypedText(t.SelectedItem);
				if (view != null && view.Contains(item))
					view.MoveCurrentTo(item);
			}
		}

		public IEnumerable ItemsSource
		{
			get { return (IEnumerable) GetValue(ItemsSourceProperty); }
			set { SetValue(ItemsSourceProperty, value); }
		}

		#endregion

		#region SelectedItem Property

		public static readonly DependencyProperty SelectedItemProperty = DependencyProperty
			.Register(nameof(SelectedItem), typeof(object), typeof(LazyComboBox)
				, new FrameworkPropertyMetadata(null, OnSelectedItemChanged, OnCoerceSelectedItem) {BindsTwoWayByDefault = true});

		private static object OnCoerceSelectedItem(DependencyObject d, object basevalue)
		{
			var t = (LazyComboBox) d;
			//if (t.ItemsSource == null)
			if (!t.IsEditing)
			{
				t.UpdateTypedText(basevalue);
				t.ItemsView?.MoveCurrentTo(basevalue);
			}
			return basevalue;
		}

		private static void OnSelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			var t = (LazyComboBox) d;
			t.RaiseSelectedItemChanged(e.OldValue, e.NewValue);
		}

		public object SelectedItem
		{
			get { return GetValue(SelectedItemProperty); }
			set { SetValue(SelectedItemProperty, value); }
		}

		#endregion

		#region DisplayMemberPath Property

		public static readonly DependencyProperty DisplayMemberPathProperty = DependencyProperty
			.Register(nameof(DisplayMemberPath), typeof(string), typeof(LazyComboBox)
				, new FrameworkPropertyMetadata(null, OnDisplayMemberPathChanged));

		public string DisplayMemberPath
		{
			get { return (string) GetValue(DisplayMemberPathProperty); }
			set { SetValue(DisplayMemberPathProperty, value); }
		}

		private static void OnDisplayMemberPathChanged(DependencyObject d,
			DependencyPropertyChangedEventArgs dependencyPropertyChangedEventArgs)
		{
		}

		#endregion

		#region SelectedValuePath Property

		public static readonly DependencyProperty SelectedValuePathProperty = DependencyProperty
			.Register(nameof(SelectedValuePath), typeof(string), typeof(LazyComboBox)
				, new FrameworkPropertyMetadata(null, OnSelectedValuePathChanged));

		private static void OnSelectedValuePathChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			//var t = (LazyComboBox)d;
			//t._selectedValuePathProperties = null;
		}

		public string SelectedValuePath
		{
			get { return (string) GetValue(SelectedValuePathProperty); }
			set { SetValue(SelectedValuePathProperty, value); }
		}

		#endregion

		#region SelectedValue Property

		public static readonly DependencyProperty SelectedValueProperty = DependencyProperty
			.Register(nameof(SelectedValue), typeof(object), typeof(LazyComboBox)
				, new FrameworkPropertyMetadata(null, OnSelectedValueChanged) {BindsTwoWayByDefault = true});

		private static void OnSelectedValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			var t = (LazyComboBox) d;
			if (t.SelectedItem == null || !Equals(e.NewValue, t.TryGetPropertyValueByPath(t.SelectedItem, t.SelectedValuePath)))
				t.TrySetSelectedItemByValue(e.NewValue);
		}

		private void TrySetSelectedItemByValue(object newValue)
		{
			if (ItemsSource == null)
				ExecuteLookup(null, false);

			var items = ItemsSource?.Cast<object>().ToArray();
			var path = SelectedValuePath;
			if (items == null || string.IsNullOrEmpty(SelectedValuePath))
			{
				UpdateTypedText(newValue);
				return;
			}
			foreach (var item in items)
			{
				var v = TryGetPropertyValueByPath(item, path);
				if (!Equals(newValue, v))
					continue;
				UpdateSelection(item);
				//SelectedItem = item;
			}
		}

		public object SelectedValue
		{
			get { return GetValue(SelectedValueProperty); }
			set { SetValue(SelectedValueProperty, value); }
		}

		#endregion
	}
}