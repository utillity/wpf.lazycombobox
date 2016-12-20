using System;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using LINQtoCSV;
using uTILLIty.Controls.WPF.LazyComboBox;

namespace uTILLIty.WPF.Demo
{
	public class MainWindowViewModel : NotifyPropertyChangedBase
	{
		private const int PageCount = 10;
		private CompanyInfo[] _list;

		public MainWindowViewModel()
		{
			Filter = OnFilter;
			TextEntry = "Three";
			TextEntries = new[] {"One", "Two", "Three", "Four", "Five"};
			if (!ApplicationHelper.IsInDesignMode())
				Task.Run(() => LoadData());
			else
			{
				SelectedEntry = new CompanyInfo
				{
					CompanyName = "Design Time Company",
					Category = "Some Category",
					SubCategory = "Sub-Category A"
				};
				_list = new[] {(CompanyInfo) SelectedEntry};
			}
		}

		public string TextEntry
		{
			get { return GetValue<string>(); }
			set { SetValue(value, unifyStringValue: false); }
		}

		public string[] TextEntries
		{
			get { return GetValue<string[]>(); }
			set { SetValue(value); }
		}

		public object SelectedEntry
		{
			get { return GetValue<object>(); }
			set { SetValue(value); }
		}

		public string Status
		{
			get { return GetValue<string>(); }
			set
			{
				SetValue(value);
				Debug.WriteLine($"Status: {value}");
			}
		}

		//public ICollection DropDownSource
		//{
		//	get { return GetValue<ICollection>(); }
		//	set { SetValue(value); }
		//}

		public Action<LookupContext> Filter { get; }

		private void OnFilter(LookupContext ctx)
		{
			while (_list == null)
				Thread.Sleep(20);

			CompanyInfo[] list;
			if (ctx.NextPageRequested)
			{
				list = (CompanyInfo[]) ctx.Tag;
				var curList = (CompanyInfo[]) ctx.LoadedList;
				var newList = list.Take(curList.Length + PageCount).ToArray();
				ctx.LoadedList = newList;
				ctx.MoreDataAvailable = list.Length > newList.Length;
				Status = $"Loaded {PageCount} more items for '{ctx.Input}' ({curList.Length} > {newList.Length})";
				return;
			}

			if (string.IsNullOrEmpty(ctx.Input))
			{
				ctx.Tag = _list;
				ctx.MoreDataAvailable = true;
				ctx.LoadedList = _list.Take(PageCount).ToArray();
				return;
			}

			Status = $"Filtering for '{ctx.Input}'...";
			list = _list.Where(c =>
			{
				ctx.CancellationToken.ThrowIfCancellationRequested();
				return c.CompanyName.IndexOf(ctx.Input, StringComparison.CurrentCultureIgnoreCase) >= 0;
			}).ToArray();
			if (!ctx.CancellationToken.IsCancellationRequested)
			{
				ctx.Tag = list;
				ctx.MoreDataAvailable = list.Length > PageCount;
				ctx.LoadedList = list.Take(PageCount).ToArray();
				Status = $"{list.Length} entries contained '{ctx.Input}'.";
			}
		}

		private void LoadData()
		{
			try
			{
				//data courtesy of https://data.gov.in/catalog/company-master-data
				Status = "Loading CSV...";
				var ctx = new CsvContext();
				var desc = new CsvFileDescription {SeparatorChar = ',', IgnoreUnknownColumns = true};
				_list = ctx.Read<CompanyInfo>("demodata.csv", desc)
					.OrderBy(i => i.CompanyName)
					.ToArray();
				Status = $"Loaded {_list.Length:N0} entries";
				SelectedEntry = _list.Skip(1000).First();
			}
			catch (AggregatedException ex)
			{
				var sb = new StringBuilder();
				foreach (var x in ex.m_InnerExceptionsList)
					sb.AppendLine($" {x.Message}");
				Status = $"Error loading data. {sb}";
			}
			catch (Exception ex)
			{
				Status = $"Error loading data. {ex.Message}";
			}
		}
	}
}