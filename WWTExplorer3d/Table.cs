﻿using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.IO.Compression;
using System.Threading;

namespace TerraViewer
{
    class Table
    {
        public Table()
        {
        }

        public Guid Guid = new Guid();
        public string[] Header = new string[0];
        public List<string[]> Rows = new List<string[]>();
        public char Delimiter = '\t';
        public bool Locked = false;

        private Mutex tableMutex = new Mutex();

        public void Lock()
        {
            Locked = true;
            tableMutex.WaitOne();
        }

        public void Unlock()
        {
            Locked = false;
            tableMutex.ReleaseMutex();
        }

        public void Save(string path)
        {

            using (Stream s = new FileStream(path, FileMode.Create))
            {
                Save(s);
            }
        }
        public void Save(Stream stream)
        {
            stream = new GZipStream(stream, CompressionMode.Compress);

            StreamWriter sw = new StreamWriter(stream, Encoding.UTF8);
            bool first = true;

            foreach (string col in Header)
            {
                if (!first)
                {
                    sw.Write("\t");
                }
                else
                {
                    first = false;
                }

                sw.Write(col);
            }
            sw.Write("\r\n");
            foreach (string[] row in Rows)
            {
                first = true;
                foreach (string col in row)
                {
                    if (!first)
                    {
                        sw.Write("\t");
                    }
                    else
                    {
                        first = false;
                    }
                    
                    sw.Write(col);
                }
                sw.Write("\r\n");
            }
            sw.Close();


        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();

            StringWriter sw = new StringWriter(sb);
            bool first = true;

            foreach (string col in Header)
            {
                if (!first)
                {
                    sw.Write("\t");
                }
                else
                {
                    first = false;
                }

                sw.Write(col);
            }
            sw.Write("\r\n");
            foreach (string[] row in Rows)
            {
                first = true;
                foreach (string col in row)
                {
                    if (!first)
                    {
                        sw.Write("\t");
                    }
                    else
                    {
                        first = false;
                    }

                    sw.Write(col);
                }
                sw.Write("\r\n");
            }
            return sb.ToString();

        }
       
        public static bool IsGzip(Stream stream)
        {
            BinaryReader br = new BinaryReader(stream);
            byte[] line = br.ReadBytes(2);

            if (line[0] == 31 && line[1] == 139)
            {
                return true;
            }
            else
            {
                return false;
            }
        }


        byte[] UnGzip(byte[] buffer)
        {
            if (buffer[0] == 31 && buffer[1] == 139)
            {
                MemoryStream msIn = new MemoryStream(buffer);
                MemoryStream msOut = new MemoryStream();


                byte[] data = new byte[2048];

                while (true)
                {
                    int count = msIn.Read(data, 0, 2048);
                    msOut.Write(data, 0, count);

                    if (count < 2048)
                    {
                        break;
                    }
                }
                msIn.Close();
                msOut.Close();
                return msOut.ToArray();

            }
            return buffer;
        }

        public static Table Load(string path, char delimiter)
        {
            if (path.ToLower().EndsWith("csv"))
            {
                delimiter = ',';
            }

            using (Stream s = new FileStream(path, FileMode.Open))
            {
                return Load(s, delimiter);
            }
        }

        public static Table Load(Stream stream, char delimiter)
        {
            
            bool gZip = IsGzip(stream);
            stream.Seek(0, SeekOrigin.Begin);
            if (gZip)
            {
                stream = new GZipStream(stream, CompressionMode.Decompress);
            }

            Table table = new Table();
            table.Delimiter = delimiter;

            StreamReader sr = new StreamReader(stream);

            if (sr.Peek() >= 0)
            {
                string headerLine = sr.ReadLine();
                while(headerLine.StartsWith("#") && sr.Peek() >= 0)
                {
                    headerLine = sr.ReadLine();
                }
                table.Rows.Clear();
                table.Header = UiTools.SplitString(headerLine, delimiter);
            }
            else
            {
                table.Header = new string[0];
            }

            int count = 0;
            while (sr.Peek() >= 0)
            {
                string line = sr.ReadLine();
                while (line.StartsWith("#") ) 
                {
                    if (sr.Peek() == -1)
                    {
                        break;
                    }
                    line = sr.ReadLine();
                }
                string[] rowData = UiTools.SplitString(line, delimiter);
                if (rowData.Length < 2)
                {
                    break;
                }
                table.Rows.Add(rowData);
                count++;
            }
            return table;
        }

        public void LoadFromString(string data, bool isUpdate, bool purge, bool hasHeader)
        {
            
            StringReader sr = new StringReader(data);
            if (!isUpdate || hasHeader)
            {
                if (sr.Peek() >= 0)
                {
                    string headerLine = sr.ReadLine();

                    if (!headerLine.Contains("\t") && headerLine.Contains(","))
                    {
                        Delimiter = ',';
                    }

                    if (!isUpdate)
                    {
                        Rows.Clear();
                    }
                    Header = UiTools.SplitString(headerLine, Delimiter);
                }
                else
                {
                    Header = new string[0];
                }
            }
            List<string[]> temp = new List<string[]>();
            if (!purge)
            {
                temp = Rows;
            }

            int count = 0;
            while (sr.Peek() >= 0)
            {
                string line = sr.ReadLine();
                string[] rowData = UiTools.SplitString(line, Delimiter);
                if (rowData.Length < 1)
                {
                    break;
                }
                temp.Add(rowData);
                count++;
            }
            if (purge)
            {
                Rows = temp;
            }

        }

        public void Append(string data)
        {
            StringReader sr = new StringReader(data);
            int count = 0;
            while (sr.Peek() >= 0)
            {
                string line = sr.ReadLine();
                string[] rowData = UiTools.SplitString(line, Delimiter);
                if (rowData.Length < 2)
                {
                    break;
                }
                Rows.Add(rowData);
                count++;
            }
        }
    }
}
