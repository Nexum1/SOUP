using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;

namespace Soup
{
    public static class ApiCall
    {
        private static readonly HttpClient client = new HttpClient();

        static ApiCall()
        {
            client.Timeout = Timeout.InfiniteTimeSpan;
        }

        public static void Call(string url, Dictionary<string, object> parameters, ApiMethod method, out HttpStatusCode status, Dictionary<string, string> headers = null)
        {
            string res = Call<string>(url, parameters, method, out status, headers);
        }

        public static DType Call<DType>(string url, Dictionary<string, object> parameters, ApiMethod method, out HttpStatusCode status, Dictionary<string, string> headers = null)
        {
            bool authorizationToken = false;
            if (headers != null && headers.ContainsKey("Authorization"))
            {
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", headers["Authorization"]);
                authorizationToken = true;
            }

            HttpResponseMessage response = null;
            switch (method)
            {
                case ApiMethod.Get:
                    if (parameters != null && parameters.Count > 0)
                    {
                        url += "?";
                        foreach (KeyValuePair<string, object> parameter in parameters)
                        {
                            string value = JsonConvert.SerializeObject(parameter.Value);
                            value = Uri.EscapeDataString(value);
                            url += parameter.Key + "=" + value + "&";
                        }
                    }
                    response = client.GetAsync(url).Result;
                    break;
                case ApiMethod.Post:
                    string json = JsonConvert.SerializeObject(parameters);
                    HttpContent content = new StringContent(json, Encoding.UTF8, "application/json");
                    if (headers != null && headers.Count > 0)
                    {
                        foreach (KeyValuePair<string, string> header in headers)
                        {
                            if (header.Key != "Authorization")
                            {
                                content.Headers.Add(header.Key, header.Value);
                            }
                        }
                    }
                    response = client.PostAsync(url, content).Result;
                    break;
            }

            status = response.StatusCode;
            string contentStr = response.Content.ReadAsStringAsync().Result;
            if (authorizationToken)
            {
                client.DefaultRequestHeaders.Authorization = null;
            }
            if (status == HttpStatusCode.OK)
            {
                if (typeof(DType) != typeof(string))
                {
                    return JsonConvert.DeserializeObject<DType>(contentStr);
                }
                else
                {
                    return (DType)(object)contentStr;
                }
            }
            return default(DType);
        }
    }

    public enum ApiMethod
    {
        Get,
        Post
    }
}
