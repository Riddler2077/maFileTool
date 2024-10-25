using maFileTool.Model;
using OfficeOpenXml;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace maFileTool.Utilities
{
    public class Excel
    {
        public static async Task<List<Account>> ReadAccountsFromExcel(string filename)
        {
            // If you use EPPlus in a noncommercial context
            // according to the Polyform Noncommercial license:
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            //FileInfo existingFile = new FileInfo(FilePath);
            using (ExcelPackage package = new ExcelPackage())
            {
                await package.LoadAsync(filename);
                //get the first worksheet in the workbook
                ExcelWorksheet worksheet = package.Workbook.Worksheets[0];
                //int colCount = worksheet.Dimension.End.Column;  //get Column Count
                //int rowCount = worksheet.Dimension.End.Row;     //get row count

                IEnumerable<Account> newcollection = worksheet.ConvertSheetToObjects<Account>();
                return newcollection.ToList();
            }
        }

        public static async Task WriteAccountToExcel(string filename, Account account, int row)
        {
            row = (row + 1);//Оступ под шапку

            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            FileInfo filePath = new FileInfo(filename);
            using (var package = new ExcelPackage(filePath))
            {
                var ws = package.Workbook.Worksheets[0];

                ws.Cells[row, 6].Value = account.Phone;
                ws.Cells[row, 7].Value = account.RevocationCode;

                await package.SaveAsync();
            }
        }

        public static async Task WriteCellInExcel(string filename, string val, int row, int col, int offset = 0)
        {
            row = row + offset;//Оступ под шапку

            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            FileInfo filePath = new FileInfo(filename);
            using (var package = new ExcelPackage(filePath))
            {
                if (package.Workbook.Worksheets.Count == 0)
                    package.Workbook.Worksheets.Add("Table1");

                var ws = package.Workbook.Worksheets[0];

                ws.Cells[row, col].Value = val;

                await package.SaveAsync();
            }
        }
    }

    public static class EPPLusExtensions
    {
        public static DataTable ToDataTable<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] T>(this IList<T> list)
        {
            DataTable table = new DataTable();

            PropertyInfo[] properties = typeof(T).GetProperties();

            foreach (PropertyInfo prop in properties)
            {
                table.Columns.Add(prop.Name, prop.PropertyType);
            }

            object[] values = new object[properties.Length];
            foreach (T item in list)
            {
                for (int i = 0; i < values.Length; i++)
                {
                    values[i] = properties[i].GetValue(item) ?? new object();
                }
                table.Rows.Add(values);
            }
            return table;
        }
        public static IEnumerable<T> ConvertSheetToObjects<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] T>(this ExcelWorksheet worksheet) where T : new()
        {

            Func<CustomAttributeData, bool> columnOnly = y => y.AttributeType == typeof(Column);

            var columns = typeof(T).GetProperties()
            .Where(x => x.CustomAttributes.Any(columnOnly))
            .Select(p => new
            {
                Property = p,
                Column = p.GetCustomAttributes<Column>().First().ColumnIndex //safe because if where above
            }).ToList();


            var rows = worksheet.Cells
                .Select(cell => cell.Start.Row)
                .Distinct()
                .OrderBy(x => x);


            //Create the collection container
            var collection = rows.Skip(1)
                .Select(row =>
                {
                    var tnew = new T();
                    columns.ForEach(col =>
                    {
                        //This is the real wrinkle to using reflection - Excel stores all numbers as double including int
                        var val = worksheet.Cells[row, col.Column];
                        //If it is numeric it is a double since that is how excel stores all numbers
                        if (val.Value == null)
                        {
                            col.Property.SetValue(tnew, null);
                            return;
                        }
                        if (col.Property.PropertyType == typeof(int))
                        {
                            col.Property.SetValue(tnew, val.GetValue<int>());
                            return;
                        }
                        if (col.Property.PropertyType == typeof(double))
                        {
                            col.Property.SetValue(tnew, val.GetValue<double>());
                            return;
                        }
                        if (col.Property.PropertyType == typeof(DateTime))
                        {
                            col.Property.SetValue(tnew, val.GetValue<DateTime>());
                            return;
                        }
                        //Its a string
                        col.Property.SetValue(tnew, val.GetValue<string>());
                    });

                    return tnew;
                });


            //Send it back
            return collection;
        }
    }
}
