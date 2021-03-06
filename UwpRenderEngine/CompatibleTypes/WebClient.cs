﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TerraViewer
{
    public class WebClient
    {
        public bool DownloadFile(string url, string filename)
        {
            try
            {
                System.Net.Http.HttpClient client = new System.Net.Http.HttpClient();

                //todo uwp figure out a way to add headers to this
                Task<byte[]> task = client.GetByteArrayAsync(url);
                byte[] buffer = task.Result;

                //var cfa = Windows.Storage.ApplicationData.Current.LocalFolder.CreateFileAsync(filename, Windows.Storage.CreationCollisionOption.ReplaceExisting);
                //var cfaTask = cfa.AsTask();
                //var file = cfaTask.Result;

                //using (var stream = file.OpenStreamForWriteAsync().Result)
                //{
                //    stream.WriteAsync(buffer, 0, buffer.Length).Wait();
                //}

                string path = Path.GetDirectoryName(filename);
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }

                using (System.IO.Stream stream = System.IO.File.Create(filename))
                {
                    stream.WriteAsync(buffer, 0, buffer.Length).Wait();
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        public string DownloadString(string url)
        {
            System.Net.Http.HttpClient client = new System.Net.Http.HttpClient();

            //todo uwp figure out a way to add headers to this
            Task<string> task = client.GetStringAsync(url);
            return task.Result;
        }


        public HeaderList Headers = new HeaderList();

        public class HeaderList
        {
            Dictionary<string, string> headers = new Dictionary<string, string>();

            public void Add(string key, string value)
            {
                headers[key] = value;
            }
        }

        internal byte[] DownloadData(string url)
        {
            System.Net.Http.HttpClient client = new System.Net.Http.HttpClient();

            //todo uwp figure out a way to add headers to this
            Task<byte[]> task = client.GetByteArrayAsync(url);
            return task.Result;
        }
    }

    

}
