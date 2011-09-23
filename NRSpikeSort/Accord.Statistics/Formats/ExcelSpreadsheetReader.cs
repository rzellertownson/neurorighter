// Accord Statistics Library
// The Accord.NET Framework
// http://accord-net.origo.ethz.ch
//
// Copyright © César Souza, 2009-2011
// cesarsouza at gmail.com
//
//    This library is free software; you can redistribute it and/or
//    modify it under the terms of the GNU Lesser General Public
//    License as published by the Free Software Foundation; either
//    version 2.1 of the License, or (at your option) any later version.
//
//    This library is distributed in the hope that it will be useful,
//    but WITHOUT ANY WARRANTY; without even the implied warranty of
//    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
//    Lesser General Public License for more details.
//
//    You should have received a copy of the GNU Lesser General Public
//    License along with this library; if not, write to the Free Software
//    Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA  02110-1301  USA
//

namespace Accord.Statistics.Formats
{
    using System;
    using System.Data;
    using System.Data.OleDb;

    /// <summary>
    ///   Excel file reader using Microsoft Jet Database Engine.
    /// </summary>
    /// 
    /// <remarks>
    ///   This class requires either the Jet engine for 32-bit
    ///   applications or the ACE 14.0 for 64 bit applications.
    /// </remarks>
    /// 
    public class ExcelSpreadsheetReader
    {

        private string path;
        private string strConnection;

        /// <summary>
        ///   Creates a new spreadsheet reader.
        /// </summary>
        /// 
        /// <param name="path">The path of for the spreadsheet file.</param>
        /// <param name="hasHeaders">True if the spreadsheet contains headers, false otherwise.</param>
        /// <param name="hasMixedData">True to read "intermixed" data columns as text, false otherwise.</param>
        /// 
        public ExcelSpreadsheetReader(string path, bool hasHeaders = true, bool hasMixedData = true)
        {
            this.path = path;
            OleDbConnectionStringBuilder strBuilder = new OleDbConnectionStringBuilder();
            strBuilder.Provider = "Microsoft.Jet.OLEDB.4.0";
            strBuilder.DataSource = path;
            strBuilder.Add("Extended Properties", "Excel 8.0;" +
                "HDR=" + (hasHeaders ? "Yes" : "No") + ';' +
                "Imex=" + (hasMixedData ? "2" : "0") + ';' +
              "");


            strConnection = strBuilder.ToString();
        }

        /// <summary>
        ///   Gets the list of worksheets in the spreadsheet.
        /// </summary>
        /// 
        public string[] GetWorksheetList()
        {
            string[] worksheets;

            OleDbConnection connection = new OleDbConnection(strConnection);
            connection.Open();
            DataTable tableWorksheets = connection.GetSchema("Tables");
            connection.Close();

            worksheets = new string[tableWorksheets.Rows.Count];

            for (int i = 0; i < worksheets.Length; i++)
            {
                worksheets[i] = (string)tableWorksheets.Rows[i]["TABLE_NAME"];
                worksheets[i] = worksheets[i].Remove(worksheets[i].Length - 1).Trim('"', '\'');

                // removes the trailing $ and other characters appended in the table name
                while (worksheets[i].EndsWith("$"))
                    worksheets[i] = worksheets[i].Remove(worksheets[i].Length - 1).Trim('"', '\'');
            }


            return worksheets;
        }

        /// <summary>
        ///   Gets the list of columns in a worksheet.
        /// </summary>
        /// 
        public string[] GetColumnsList(string worksheet)
        {
            string[] columns;

            OleDbConnection connection = new OleDbConnection(strConnection);
            connection.Open();
            DataTable tableColumns = connection.GetSchema("Columns", new string[] { null, null, worksheet + '$', null });
            connection.Close();

            columns = new string[tableColumns.Rows.Count];

            for (int i = 0; i < columns.Length; i++)
                columns[i] = (string)tableColumns.Rows[i]["COLUMN_NAME"];

            return columns;
        }

        /// <summary>
        ///   Gets an worksheet as a data table.
        /// </summary>
        /// 
        public DataTable GetWorksheet(string worksheet)
        {
            DataTable ws;

            OleDbConnection connection = new OleDbConnection(strConnection);
            OleDbDataAdapter adaptor = new OleDbDataAdapter(String.Format("SELECT * FROM [{0}$]", worksheet), connection);
            ws = new DataTable(worksheet);
            adaptor.FillSchema(ws, SchemaType.Source);
            adaptor.Fill(ws);

            adaptor.Dispose();
            connection.Close();

            return ws;
        }

        /// <summary>
        ///   Gets the entire worksheet as a data set.
        /// </summary>
        /// 
        public DataSet GetWorksheet()
        {
            DataSet workplace;

            OleDbConnection connection = new OleDbConnection(strConnection);
            OleDbDataAdapter adaptor = new OleDbDataAdapter("SELECT * FROM *", connection);
            workplace = new DataSet();
            adaptor.FillSchema(workplace, SchemaType.Source);
            adaptor.Fill(workplace);

            adaptor.Dispose();
            connection.Close();

            return workplace;
        }
    }
}
