using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LINQtoCSV;

namespace uTILLIty.WPF.Demo
{
	public class MainWindowViewModel : NotifyPropertyChangedBase
	{
		private List<CompanyInfo> _list;

		public MainWindowViewModel()
		{
			Filter = OnFilter;
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
				DropDownSource = new[] {SelectedEntry};
			}
		}

		public object SelectedEntry
		{
			get { return GetValue<object>(); }
			set { SetValue(value); }
		}

		public string Status
		{
			get { return GetValue<string>(); }
			set { SetValue(value); }
		}

		public ICollection DropDownSource
		{
			get { return GetValue<ICollection>(); }
			set { SetValue(value); }
		}

		public Action<string> Filter { get; }

		private void OnFilter(string input)
		{
			Debug.WriteLine($"Filtering for '{input}'...");
			var list = _list.Where(c => c.CompanyName
				.IndexOf(input, StringComparison.CurrentCultureIgnoreCase) >= 0)
				.ToList();
			DropDownSource = list;
			Status = $"{list.Count} entries contained '{input}'.";
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
					.ToList();
				SelectedEntry = _list.First();
				Status = $"Loaded {_list.Count:N0} entries";
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