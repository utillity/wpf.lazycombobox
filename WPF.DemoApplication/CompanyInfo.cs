using System.Globalization;
using LINQtoCSV;

namespace uTILLIty.WPF.Demo
{
	public class CompanyInfo
	{
		/*
			Columns:
			"CORPORATE_IDENTIFICATION_NUMBER","DATE_OF_REGISTRATION","COMPANY_NAME","COMPANY_STATUS","COMPANY_CLASS","COMPANY_CATEGORY",
			"AUTHORIZED_CAPITAL","PAIDUP_CAPITAL","REGISTERED_STATE","REGISTRAR_OF_COMPANIES","PRINCIPAL_BUSINESS_ACTIVITY",
			"REGISTERED_OFFICE_ADDRESS","SUB_CATEGORY"

			Data:
			"ALLA","5-01-1955","ALLAHABAD BANK","ACTIVE","Public","Company Limited by Shares",
			"30,000,000,000.00",NA,"West Bengal","RoC-Kolkata",NA,
			"Head Office 2, Netaji Subhas Road Calcutta West Bengal INDIA","Indian Non-Government Company"
		*/

		[CsvColumn("CORPORATE_IDENTIFICATION_NUMBER", 0, false, null, NumberStyles.Any, 500)]
		public string Id { get; set; }

		//[CsvColumn("DATE_OF_REGISTRATION", 1, true, "{0:d-MM-yyyy}", NumberStyles.Any, 10)]
		//public DateTime Registered { get; set; }

		[CsvColumn("COMPANY_NAME", 2, false, null, NumberStyles.Any, 500)]
		public string CompanyName { get; set; }

		[CsvColumn("COMPANY_STATUS", 3, false, null, NumberStyles.Any, 50)]
		public string Status { get; set; }

		[CsvColumn("COMPANY_CLASS", 4, false, null, NumberStyles.Any, 50)]
		public string Class { get; set; }

		[CsvColumn("COMPANY_CATEGORY", 5, false, null, NumberStyles.Any, 50)]
		public string Category { get; set; }

		[CsvColumn("SUB_CATEGORY", 12, false, null, NumberStyles.Any, 50)]
		public string SubCategory { get; set; }
	}
}