using Microsoft.VisualBasic.FileIO;
using OfficePracticum;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;

namespace ConsoleApplication1
{
    class Program
    {
        static void Main(string[] args)
        {
            TextFieldParser parser = new TextFieldParser(@"C:\PediatricCare\Code_Table.csv");
            parser.HasFieldsEnclosedInQuotes = true;
            parser.SetDelimiters(",");

            string[] fields;

            List<CodeTable> list = new List<CodeTable>();
            while (!parser.EndOfData)
            {
                fields = parser.ReadFields();
                foreach(string field in fields)
                {
                    CodeTable ct = new CodeTable(fields[0], fields[1], fields[2], fields[3], fields[4], fields[5], fields[6], fields[7], fields[8]);
                    list.Add(ct);
                }
              
            }

            parser.Close();
        }
    }
}

