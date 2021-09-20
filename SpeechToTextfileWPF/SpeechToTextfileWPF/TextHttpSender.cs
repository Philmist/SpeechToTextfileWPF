#nullable enable
using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;
using System.Net;
using System.Diagnostics;

namespace SpeechToTextfileWPF
{
    class TextHttpSender
    {
        public class RecognizedText
        {
            public string code { get; set; } = "R";
            public string text { get; set; } = "";
        }

        public string Uri { get; protected set; } = "";
        public bool isEnable { get; protected set; } = true;

        public TextHttpSender(string Uri) {
            this.Uri = Uri;
            if (String.IsNullOrWhiteSpace(this.Uri))
            {
                isEnable = false;
            }
        }

        public bool Send(RecognizedText text)
        {
            if (!isEnable)
            {
                return false;
            }

            string json = JsonConvert.SerializeObject(text, Formatting.Indented);
            var jsonUtf8String = Encoding.UTF8.GetBytes(json);

            WebRequest request = WebRequest.Create(Uri);
            request.Method = "POST";
            request.ContentLength = jsonUtf8String.Length;
            request.ContentType = "application/json";
            request.Timeout = 3000;

            try
            {
                var dataStream = request.GetRequestStream();
                Debug.WriteLine(json);
                dataStream.Write(jsonUtf8String, 0, jsonUtf8String.Length);
                dataStream.Close();

                var response = request.GetResponse();
                var statusCode = ((HttpWebResponse)response).StatusCode;
                if (statusCode == HttpStatusCode.OK)
                {
                    isEnable = true;
                    response.Close();
                    return true;
                }
            }
            catch (WebException ex)
            {
                Debug.WriteLine(String.Format("HTTP Exception: {0}", ex.Message));
                isEnable = false;
                return false;
            }

            isEnable = false;
            return false;
        }
    }
}
