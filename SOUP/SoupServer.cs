using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Soup
{
    public class SoupServer<T>
        where T : class
    {
        private readonly HttpListener _listener = new HttpListener();
        Dictionary<string, MethodInfo> _actionCache = new Dictionary<string, MethodInfo>();
        Dictionary<string, ParameterInfo[]> _actionParamsCacheGet = new Dictionary<string, ParameterInfo[]>();
        Dictionary<string, bool> isPost = new Dictionary<string, bool>();
        Type Controller = typeof(T);
        T Instance;
        bool crossOrigin = false;
        Func<string, string, bool> Authentication;
        public int Processing = 0;
        public bool Stopping = false;
        bool ModelValidation = false;
        bool Parallel = true;
        int Hash = 0;

        public SoupServer(bool ModelValidation = false, T instance = null, bool crossOrigin = false, string hostUrl = "http://*:8080/", Func<string, string, bool> Authentication = null, bool Parallel = true)
        {
            this.ModelValidation = ModelValidation;
            ServerModelHelper.GetServerModel(Controller, ModelValidation, out Hash);
            if (instance == null)
            {
                Instance = (T)Activator.CreateInstance(Controller);
            }
            else
            {
                Instance = instance;
            }
            this.Authentication = Authentication;
            this.crossOrigin = crossOrigin;
            this.Parallel = Parallel;
            if (!HttpListener.IsSupported)
            {
                throw new NotSupportedException("Needs Windows XP SP2, Server 2003 or later.");
            }

            _listener.Prefixes.Add(hostUrl);
            if (Authentication != null)
            {
                _listener.AuthenticationSchemes = AuthenticationSchemes.Basic;
            }
        }

        public void Run()
        {
            _listener.Start();
           
            ThreadPool.QueueUserWorkItem((o) =>
            {
                Console.WriteLine("Webserver running...");

                while (_listener.IsListening)
                {                   
                    if (Parallel)
                    {
                        Task.Factory.StartNew((c) => Request(c), _listener.GetContext(), TaskCreationOptions.LongRunning);
                    }
                    else
                    {
                        while (Processing > 0)
                        {
                            Thread.Sleep(1);
                        }

                        Request(_listener.GetContext());
                    }
                }
            });
        }

        void Request(object c)
        {
            var ctx = c as HttpListenerContext;
            if (Stopping)
            {
                byte[] buf = Encoding.UTF8.GetBytes("Shutting Down");
                ctx.Response.StatusCode = 503;
                ctx.Response.ContentLength64 = buf.Length;
                ctx.Response.OutputStream.Write(buf, 0, buf.Length);
            }
            else
            {
                HttpStatusCode code = HttpStatusCode.OK;
                try
                {
                    Processing++;
                    bool authinticated = true;
                    if (Authentication != null)
                    {
                        HttpListenerBasicIdentity identity = (HttpListenerBasicIdentity)ctx.User.Identity;
                        if (!Authentication.Invoke(identity.Name, identity.Password))
                        {
                            byte[] buf = Encoding.UTF8.GetBytes("Authentication Failed");
                            ctx.Response.StatusCode = 401;
                            ctx.Response.ContentLength64 = buf.Length;
                            ctx.Response.OutputStream.Write(buf, 0, buf.Length);
                            authinticated = false;
                        }
                    }

                    if (authinticated)
                    {
                        if (ctx.Request.Url.Segments.Length > 1)
                        {
                            string methodName = ctx.Request.Url.Segments.Last().Replace("/", "");
                            byte[] buf = null;
                            if (methodName == "GetServerModel")
                            {
                                buf = ServerModelHelper.GetServerModel(Controller, ModelValidation, out int Hash);
                            }
                            else
                            {

                                if (!_actionCache.ContainsKey(methodName))
                                {
                                    MethodInfo method = Controller.GetMethod(methodName);
                                    if (method == null)
                                    {
                                        throw new Exception("Method not found on webserver");
                                    }

                                    _actionCache.Add(methodName, method);
                                    bool Post = method.GetCustomAttribute<Post>() != null;
                                    isPost.Add(methodName, Post);
                                    ParameterInfo[] parameters = method.GetParameters();
                                    _actionParamsCacheGet.Add(methodName, parameters);
                                }

                                if (ctx.Request.HttpMethod == "GET")
                                {
                                    List<object> @params = new List<object>();

                                    if (ModelValidation)
                                    {
                                        string hashStr = ctx.Request.QueryString.Get(ServerModelHelper.HashParameterName);
                                        if (hashStr != null)
                                        {
                                            int clientHash = Convert.ToInt32(hashStr);
                                            if (clientHash != Hash)
                                            {
                                                code = HttpStatusCode.NotAcceptable;
                                                throw new Exception("Model Mismatch - Incorrect Hash");
                                            }
                                        }
                                        else
                                        {
                                            code = HttpStatusCode.BadRequest;
                                            throw new Exception("Model Mismatch - No Hash");
                                        }
                                    }

                                    if (isPost[methodName])
                                    {
                                        code = HttpStatusCode.MethodNotAllowed;
                                        throw new Exception("Tried to use GET on POST call");
                                    }

                                    foreach (ParameterInfo parameter in _actionParamsCacheGet[methodName])
                                    {
                                        string current = ctx.Request.QueryString.Get(parameter.Name);
                                        @params.Add(JsonConvert.DeserializeObject(current, parameter.ParameterType));
                                    }

                                    string bufString = null;
                                    object returnObj = _actionCache[methodName].Invoke(Instance, @params.ToArray());
                                    if (_actionCache[methodName].ReturnType != typeof(string))
                                    {
                                        bufString = JsonConvert.SerializeObject(returnObj);
                                    }
                                    else
                                    {
                                        bufString = (string)returnObj;
                                    }
                                    buf = Encoding.UTF8.GetBytes(bufString);
                                }

                                if (ctx.Request.HttpMethod == "POST")
                                {
                                    StreamReader reader = new StreamReader(ctx.Request.InputStream);
                                    string payload = reader.ReadToEnd();
                                    Dictionary<string, object> payloadObj = JsonConvert.DeserializeObject<Dictionary<string, object>>(payload);
                                    List<object> @params = new List<object>();

                                    if (ModelValidation)
                                    {
                                        if (payloadObj.ContainsKey(ServerModelHelper.HashParameterName))
                                        {
                                            if (Convert.ToInt32(payloadObj[ServerModelHelper.HashParameterName]) != Hash)
                                            {
                                                code = HttpStatusCode.NotAcceptable;
                                                throw new Exception("Model Mismatch - Incorrect Hash");
                                            }
                                        }
                                        else
                                        {
                                            code = HttpStatusCode.BadRequest;
                                            throw new Exception("Model Mismatch - No Hash");
                                        }
                                    }

                                    if (!isPost[methodName])
                                    {
                                        code = HttpStatusCode.MethodNotAllowed;
                                        throw new Exception("Tried to use POST on GET call");
                                    }

                                    foreach (ParameterInfo parameter in _actionParamsCacheGet[methodName])
                                    {
                                        object current = payloadObj[parameter.Name];
                                        if (current.GetType() == typeof(JObject))
                                        {
                                            @params.Add(((JObject)current).ToObject(parameter.ParameterType));
                                        }
                                        else
                                        {
                                            @params.Add(Convert.ChangeType(current, parameter.ParameterType));
                                        }
                                    }
                                    string bufString = null;
                                    object returnObj = _actionCache[methodName].Invoke(Instance, @params.ToArray());
                                    if (_actionCache[methodName].ReturnType != typeof(string))
                                    {
                                        bufString = JsonConvert.SerializeObject(returnObj);
                                    }
                                    else
                                    {
                                        bufString = (string)returnObj;
                                    }
                                    buf = Encoding.UTF8.GetBytes(bufString);
                                }


                                if (crossOrigin)
                                {
                                    ctx.Response.AppendHeader("Access-Control-Allow-Origin", "*");
                                }
                            }

                            ctx.Response.ContentLength64 = buf.Length;
                            ctx.Response.OutputStream.Write(buf, 0, buf.Length);
                        }
                        else
                        {
                            code = HttpStatusCode.NotImplemented;
                            throw new Exception("Method not specified in call");
                        }
                    }
                }
                catch (Exception ex)
                {
                    if (code == HttpStatusCode.OK)
                    {
                        code = HttpStatusCode.BadRequest;
                    }
                    string msg = ex.Message;
                    if (ex.InnerException != null)
                    {
                        msg = ex.InnerException.Message;
                    }
                    byte[] buf = Encoding.UTF8.GetBytes(msg);
                    ctx.Response.StatusCode = (int)code;
                    ctx.Response.ContentLength64 = buf.Length;
                    ctx.Response.OutputStream.Write(buf, 0, buf.Length);
                }
                Processing--;
            }
        }

        public void Stop(bool gentle = false)
        {
            if (gentle)
            {
                Stopping = true;
                Thread waitToStop = new Thread(() =>
                {
                    while (Processing > 0)
                    {
                        Thread.Sleep(1000);
                    }

                    _listener.Stop();
                    _listener.Close();
                });
                waitToStop.Start();
            }
            else
            {
                _listener.Stop();
                _listener.Close();
            }
        }



    }
}
