using AATools;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.VisualBasic.FileIO;
using OfficePracticum;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.Sql;
using System.Data.SqlClient;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace Import
{
    public partial class Form1 : Form
    {
        private string _connectionString;

        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click_1(object sender, EventArgs e)
        {
            //CheckFileNames();

            _connectionString = String.Format("Server={0};Database={1};Trusted_Connection=True;", cboServers.Text, cboDatabase.Text);

            Cursor.Current = Cursors.WaitCursor;
            DirectoryInfo dir = new DirectoryInfo(textBox1.Text);
            if (!Directory.Exists(dir + "\\Scrubbed"))
            {
                Directory.CreateDirectory(dir + "\\Scrubbed");
            }

            if (!Directory.Exists(dir + "\\Scrubbed\\Meta"))
            {
                Directory.CreateDirectory(dir + "\\Scrubbed\\Meta");
            }

            foreach (var file in dir.GetFiles())
            {
                TextFieldParser parser = new TextFieldParser(file.FullName);
                parser.HasFieldsEnclosedInQuotes = ckUsesTextQualifier.Checked;
                parser.TrimWhiteSpace = true;
                parser.SetDelimiters(txtDelimiter.Text);

                string[] fields;

                StringBuilder line = new StringBuilder();
                Dictionary<int, int> maxFields = new Dictionary<int, int>();
                List<string> fieldNames = new List<string>();

                ////////TEST/////////////
                SqlConnection conn = new SqlConnection(_connectionString);
                //SqlConnection conn = new SqlConnection("Server=FWSRVPRIME;Database=PediatricCare_OfficePracticum_Stage;Trusted_Connection=True;");
                var sqlBulkCopy = new SqlBulkCopy(conn);
                ///////TEST//////////////////////////////

                while (!parser.EndOfData)
                {
                    List<string> combinedFields = new List<string>();
                    fields = parser.ReadFields();

                    if (fields.Length < 55)
                    {
                        combinedFields.AddRange(fields);
                        int iCount = fields.Length;

                        while (iCount < maxFields.Keys.Count)
                        {
                            string[] f = parser.ReadFields();
                            if (f.Length == 1)
                            {
                                int len = combinedFields.Count() - 1;
                                combinedFields[len] = combinedFields[len] + f[0];
                            }
                            else
                            {
                                int len = combinedFields.Count() - 1;
                                combinedFields[len] = combinedFields[len] + f[0];
                                var foos = new List<string>(f);
                                foos.RemoveAt(0);
                                combinedFields.AddRange(foos.ToArray());
                                iCount = iCount + f.Length;
                            }

                        }
                    }

                    if (combinedFields.Count > 0)
                    {
                        fields = combinedFields.ToArray();
                        //MessageBox.Show("WTF?");
                        //break;
                    }

                    for (int i = 0; i < fields.Length; i++)
                    {
                        if (parser.LineNumber == 2) //parser line count goes up once a line is read, hence we are looking for 2
                        {
                            fieldNames.Add(fields[i]);
                        }

                        if (i == (fields.Length - 1))
                        {
                            line.Append(fields[i].Replace(",", " "));
                        }
                        else
                        {
                            //if(fields[i] == "3697")
                            //{
                            //    string s = "here";
                            //}

                            line.Append(fields[i].Replace(",", " ").Replace("\r", " ").Replace("\n", "").Replace("\"", "").Replace("*", "").Replace("'", "") + ",");

                        }

                        if (maxFields.ContainsKey(i))
                        {
                            if (maxFields[i] < fields[i].Length)
                            {
                                maxFields[i] = fields[i].Length;
                            }
                        }
                        else
                        {
                            maxFields.Add(i, fields[i].Length);
                        }
                    }

                    line.Append(Environment.NewLine);
                }

                File.WriteAllText(dir + "\\Scrubbed\\" + file.Name, line.ToString());
                FileInfo importFile = new FileInfo(dir + "\\Scrubbed\\" + file.Name);

                StringBuilder meta = new StringBuilder();
                for (int i = 0; i < maxFields.Keys.Count; i++)
                {
                    meta.Append(fieldNames[i] + "," + maxFields[i] + Environment.NewLine);
                }
                File.WriteAllText(dir + "\\Scrubbed\\Meta\\META_INFO_" + file.Name, meta.ToString());

                ImportData(importFile.Name.Replace(".csv", "").Replace(".txt", "").Replace(" ", "").Trim(), fieldNames, maxFields, importFile);

                string[] fileNameAndExtension = file.Name.Split('.');
                string updatedFileName = fileNameAndExtension[0] + "_" + DateTime.Now.ToString("MMddyyyyhhmmss");
                string updatedFillNameAndExtension = updatedFileName + '.' + fileNameAndExtension[1];

                if (!Directory.Exists(dir + "\\Source"))
                {
                    Directory.CreateDirectory(dir + "\\Source");
                }

                file.MoveTo(dir + "\\Source\\" + updatedFillNameAndExtension);

            }

            Cursor.Current = Cursors.Arrow;
        }

        private void CheckFileNames()
        {
            DirectoryInfo dir = new DirectoryInfo(textBox1.Text);

            foreach (var file in dir.GetFiles())
            {
                if (file.Name.Contains("."))
                    MessageBox.Show("Please check the files names and remove any '.' - the filenames are used to create the table names and the use of '.' us not permitted in table names");
            }
        }

        private void cmdBrowse_Click(object sender, EventArgs e)
        {
            if (textBox1.Text != "")
            {
                folderBrowserDialog1.SelectedPath = textBox1.Text;
                folderBrowserDialog1.ShowDialog();
            }
            else
            {
                folderBrowserDialog1.ShowDialog();
            }
            textBox1.Text = folderBrowserDialog1.SelectedPath;
        }

        private void ImportData(string tableName, List<string> columnNames, Dictionary<int, int> columnMetaInfo, FileInfo file)
        {
            DataTable dataTable = new DataTable(tableName);
            StringBuilder columnStatments = new StringBuilder();

            for (int i = 0; i < columnNames.Count; i++)
            {
                dataTable.Columns.Add(columnNames[i]);

                if (columnNames[i].Length == 0)
                    columnNames[i] = "UnknownColumnName" + i.ToString();

                if (i == (columnNames.Count - 1))
                {
                    columnStatments.AppendFormat("[{0}] [varchar] ({1}) NULL ", columnNames[i].Replace("[", "").Replace("]", "").Replace("'", ""), columnMetaInfo[i]);
                }
                else
                {
                    columnStatments.AppendFormat("[{0}] [varchar] ({1}) NULL, ", columnNames[i].Replace("]", "").Replace("[", "").Replace("'",""), columnMetaInfo[i]);
                }
            }

            SqlConnection conn = new SqlConnection(_connectionString);
            //SqlConnection conn = new SqlConnection("Server=FWSRVPRIME;Database=PediatricCare_OfficePracticum_Stage;Trusted_Connection=True;");
            StringBuilder sqlCreate = new StringBuilder(String.Format("CREATE TABLE [dbo].[{0}] ({1})", tableName, columnStatments.ToString()));

            SqlCommand cmd = new SqlCommand(sqlCreate.ToString(), conn);
            cmd.CommandType = CommandType.Text;
            using (conn)
            {
                conn.Open();
                cmd.ExecuteNonQuery();
                conn.Close();
            }

            DataTable filledDataTable = Common.CreateDataTableFromFile(dataTable, file);

            conn = new SqlConnection(_connectionString);
            //conn = new SqlConnection("Server=FWSRVPRIME;Database=PediatricCare_OfficePracticum_Stage;Trusted_Connection=True;");
            SqlBulkCopy sqlBulkCopy = new SqlBulkCopy(conn);
            sqlBulkCopy.BulkCopyTimeout = 1800000;

            sqlBulkCopy.DestinationTableName = tableName;

            using (conn)
            {
                conn.Open();
                sqlBulkCopy.WriteToServer(filledDataTable);
                conn.Close();
                conn.Dispose();
            }

        }

        private void Form1_Load(object sender, EventArgs e)
        {
            LoadServersDropDown();
            textBox1.Text = @"C:\";
        }


        private void LoadServersDropDown()
        {
            string myServer = Environment.MachineName;

            DataTable servers = SqlDataSourceEnumerator.Instance.GetDataSources();
            for (int i = 0; i < servers.Rows.Count; i++)
            {
                if (!cboServers.Items.Contains(servers.Rows[i]["ServerName"]))
                    cboServers.Items.Add(servers.Rows[i]["ServerName"]);
                if ((servers.Rows[i]["InstanceName"] as string) != null)
                    cboServers.Items.Add(servers.Rows[i]["ServerName"] + "\\" + servers.Rows[i]["InstanceName"]);
                //else
                //    cboServers.Items.Add(servers.Rows[i]["ServerName"]);
            }
        }

        private void cboServers_SelectedIndexChanged(object sender, EventArgs e)
        {
            var server = new Microsoft.SqlServer.Management.Smo.Server(cboServers.Text);

            foreach (Database db in server.Databases)
            {
                cboDatabase.Items.Add(db.Name);
            }
        }
    }
}
