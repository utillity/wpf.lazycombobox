using System;
using System.Collections;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LINQtoCSV;

namespace uTILLIty.WPF.Demo
{
	public class MainWindowViewModel : NotifyPropertyChangedBase
	{
		public MainWindowViewModel()
		{
			if (!ApplicationHelper.IsInDesignMode())
				Task.Run(() => LoadData());
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

		private void LoadData()
		{
			try
			{
				Status = "Loading CSV...";
				var ctx = new CsvContext();
				var desc = new CsvFileDescription {SeparatorChar = ',', IgnoreUnknownColumns = true};
				var list = ctx.Read<CompanyInfo>("demodata.csv", desc)
					.OrderBy(i => i.CompanyName)
					.ToList();
				SelectedEntry = list.First();
				Status = $"Loaded {list.Count:N0} entries";
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