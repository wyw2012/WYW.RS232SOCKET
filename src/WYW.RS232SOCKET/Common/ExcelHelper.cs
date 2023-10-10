using GemBox.Spreadsheet;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;

namespace WYW.RS232SOCKET.Common
{
    internal class ExcelHelper
    {
        public static readonly string License = "E02V-XUB1-52LA-994F";
        public static void DataTableToExcel(DataTable dataTable, string fileName, byte[] excelTemplate=null)
        {
            SpreadsheetInfo.SetLicense(License);
            ExcelFile ef = null;
            ExcelWorksheet ws = null;
            var rowsLength = dataTable.Rows.Count;
            var columnsLength = dataTable.Columns.Count;
            if (excelTemplate==null)
            {
                ef = new ExcelFile();
                ws = ef.Worksheets.Add("Sheet1");

                for (var i = 0; i < columnsLength; i++)
                {
                    ws.Cells[0, i].Value = dataTable.Columns[i].ColumnName;
                }
            }
            else
            {
                ef = ExcelFile.Load(new MemoryStream(excelTemplate));
                ws = ef.Worksheets[0];
          
            }
            for (var i = 0; i < rowsLength; i++)
            {
                for (var j = 0; j < columnsLength; j++)
                {
                    ws.Cells[i + 1, j].Value = dataTable.Rows[i][j];
                }
            }
            ef.Save(fileName);
        }
        /// <summary>
        /// 获取当前Excel文件与目标Excel中Sheet1表头匹配的表单索引，匹配不成功返回-1
        /// </summary>
        /// <param name="sourceFileName"></param>
        /// <param name="targetFileBytes"></param>
        /// <returns></returns>
        public static int FindSheetIndex(string sourceFileName, byte[] targetFileBytes)
        {
            SpreadsheetInfo.SetLicense(License);
            var sourceSheet = ExcelFile.Load(new MemoryStream(targetFileBytes)).Worksheets[0];
            var ef = ExcelFile.Load(sourceFileName);
            int index = 0;
            foreach (var item in ef.Worksheets)
            {
                if (item.Rows[0].AllocatedCells.Count>= sourceSheet.Rows[0].AllocatedCells.Count)
                {
                    int count = sourceSheet.Rows[0].AllocatedCells.Count;
                    for (int i = 0; i < count; i++)
                    {
                       if(item.Cells[0, i].Value?.ToString()!= sourceSheet.Cells[0, i].Value?.ToString())
                        {
                            break;
                        }
                    }
                    return index;
                }
                index++;
            }
            return -1;
        }
        public static DataTable ExcelToDataTable(string fileName, int sheetIndex = 0)
        {
            SpreadsheetInfo.SetLicense(License);
            var ef = ExcelFile.Load(fileName);
            var ws = ef.Worksheets[sheetIndex];
            // Create DataTable from an Excel worksheet.
            var dataTable = ws.CreateDataTable(new CreateDataTableOptions()
            {
                ColumnHeaders = true,
                StartRow = 0,
                NumberOfColumns = ws.Rows.Max(x => x.AllocatedCells.Count),
                NumberOfRows = ws.Rows.Count,
                Resolution = ColumnTypeResolution.AutoPreferStringCurrentCulture
            });
            return dataTable;
        }
        public static DataTable ExcelToDataTable(MemoryStream stream,int sheetIndex=0)
        {
            SpreadsheetInfo.SetLicense(License);
            var ef = ExcelFile.Load(stream);
            var ws = ef.Worksheets[sheetIndex];

            // Create DataTable from an Excel worksheet.
            var dataTable = ws.CreateDataTable(new CreateDataTableOptions()
            {
                ColumnHeaders = true,
                StartRow = 0,
                NumberOfColumns = ws.Rows.Max(x=>x.AllocatedCells.Count),
                NumberOfRows = ws.Rows.Count,
                Resolution = ColumnTypeResolution.AutoPreferStringCurrentCulture
            });
            return dataTable; 
        }
        public static void CollectionToExcel(IEnumerable<object> objects, string fileName)
        {
            SpreadsheetInfo.SetLicense(License);
            var ef = new ExcelFile();
            var ws = ef.Worksheets.Add("Sheet1");
            // 添加首行
            var props = objects.FirstOrDefault().GetType().GetProperties();
            for (int i = 0; i < props.Length; i++)
            {
                ws.Cells[0, i].Value = props[i].Name;
            }
            // 添加数据
            int index = 1;
            foreach (var obj in objects)
            {
                props = obj.GetType().GetProperties();
                for (int i = 0; i < props.Length; i++)
                {
                    ws.Cells[index, i].Value = props[i].GetValue(obj, null);
                }
                index++;
            }
            ef.Save(fileName);
        }
        public static void ObjectToExcel(object obj, string fileName)
        {
            SpreadsheetInfo.SetLicense(License);
            var ef = new ExcelFile();
            var ws = ef.Worksheets.Add("Sheet1");

            var props = obj.GetType().GetProperties();
            for (int i = 0; i < props.Length; i++)
            {
                // 添加首行
                ws.Cells[0, i].Value = props[i].Name;
                // 添加数据
                ws.Cells[1, i].Value = props[i].GetValue(obj, null);
            }
            ef.Save(fileName);
        }
    }
}
