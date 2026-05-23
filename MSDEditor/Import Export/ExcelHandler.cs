using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MSDEditor
{
    internal class ExcelHandler : IImportable, IExportable
    {
        public bool Export(string filename, in List<MSDEntry> data)
        {
            try
            {
                using (SpreadsheetDocument spreadsheetDocument = SpreadsheetDocument.Create(filename, SpreadsheetDocumentType.Workbook))
                {
                    WorkbookPart workbookPart = spreadsheetDocument.AddWorkbookPart();
                    workbookPart.Workbook = new Workbook();

                    var worksheetPart = workbookPart.AddNewPart<WorksheetPart>();
                    SheetData sheetData = new SheetData();

                    worksheetPart.Worksheet = new Worksheet(sheetData);

                    Sheets sheets = spreadsheetDocument.WorkbookPart.Workbook.AppendChild<Sheets>(new Sheets());

                    Sheet sheet = new Sheet()
                    {
                        Id = spreadsheetDocument.WorkbookPart.GetIdOfPart(worksheetPart),
                        SheetId = 1,
                        Name = Path.GetFileNameWithoutExtension(filename)
                    };

                    foreach (MSDEntry entry in data)
                    {
                        var row = worksheetPart.Worksheet.GetFirstChild<SheetData>().AppendChild(new Row());

                        var IdCell = row.AppendChild(new Cell());
                        var TextCell = row.AppendChild(new Cell());


                        IdCell.CellValue = new CellValue(entry.Id.ToString());
                        IdCell.DataType = new EnumValue<CellValues>(CellValues.Number);

                        TextCell.CellValue = new CellValue(entry.Text.Replace("\n", "\\n").TrimEnd('\0'));
                        TextCell.CellValue.Space = SpaceProcessingModeValues.Preserve;
                        TextCell.DataType = new EnumValue<CellValues>(CellValues.String);

                    }

                    sheets.Append(sheet);
                    workbookPart.Workbook.Save();
                    spreadsheetDocument.Save();
                }
            }
            catch
            {
                return false;
            }

            return true;
        }
        public bool Import(string filename, out List<MSDEntry> data)
        {
            try
            {
                using (SpreadsheetDocument spreadsheetDocument = SpreadsheetDocument.Open(filename, false))
                {
                    // No null checks, I can't really be bothered to handle all excel funk.
                    var workbookPart = spreadsheetDocument.WorkbookPart;
                    var sheetId = workbookPart.Workbook.Descendants<Sheet>().First().Id;

                    var rows = (workbookPart.GetPartById(sheetId) as WorksheetPart).Worksheet.GetFirstChild<SheetData>().Elements<Row>();

                    data = new List<MSDEntry>(rows.Count());

                    foreach (Row row in rows)
                    {
                        var IdCell = (Cell)row.ChildElements[0];
                        var TextCell = (Cell)row.ChildElements[1];

                        string textContent = "";

                        // insert laments about how it's never simple with excel..
                        // can't even use a switch
                        if (TextCell.DataType == CellValues.String)
                        {
                            textContent = TextCell.InnerText.Replace("\\n", "\n") + "\0\0";
                        }
                        else if (TextCell.DataType == CellValues.SharedString)
                        {
                            // go find the thing
                            var stringTable = workbookPart.SharedStringTablePart.SharedStringTable;
                            textContent = stringTable.ChildElements[int.Parse(TextCell.CellValue.Text)].InnerText.Replace("\\n", "\n") + "\0\0"; ;
                        }
                        else
                        {
                            throw new NotImplementedException("The text cell was not the expected data type.");
                        }

                        MSDEntry entry = new MSDEntry(
                            UInt32.Parse(IdCell.InnerText),
                            textContent);

                        data.Add(entry);
                    }
                }
            }
            catch
            {
                data = null;
                return false;
            }

            return true;
        }
    }
}
