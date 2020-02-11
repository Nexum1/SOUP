using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using static Soup.SoupClientModel;

namespace Soup
{
    public class SoupClientModel
    {
        public string ServerName { get; set; }
        public List<SoupMethod> Methods { get; set; } = new List<SoupMethod>();
        public List<SoupType> Types { get; set; } = new List<SoupType>();
        public bool ModelValidation { get; set; }
        public int Hash { get; set; }
        public string HashParameterName { get; set; }

        public SoupClientModel(string ServerName)
        {
            this.ServerName = ServerName;
        }

        public override int GetHashCode()
        {
            string combined = ServerName;

            foreach (SoupMethod method in Methods)
            {
                combined += method.ToString();
            }

            foreach (SoupType type in Types)
            {
                combined += type.ToString();
            }

            return combined.GetHashCode();
        }

        public class SoupMethod
        {
            public string MethodName { get; set; }
            public string MethodReturnType { get; set; }
            public bool Post { get; set; }
            public bool ReturnCode { get; set; }
            public List<SoupParamProp> MethodParameters { get; set; } = new List<SoupParamProp>();

            public SoupMethod(string MethodName, string MethodReturnType, bool Post, bool ReturnCode)
            {
                this.MethodName = MethodName;
                this.MethodReturnType = MethodReturnType;
                this.Post = Post;
                this.ReturnCode = ReturnCode;
            }

            public override string ToString()
            {
                string combined = MethodName + "|" + MethodReturnType + "|" + Post.ToString() + "|";
                combined += string.Join("|", MethodParameters.Select(x => x.ToString()).ToArray());
                return combined;
            }
        }

        public class SoupParamProp
        {
            public string ParameterName { get; set; }
            public string ParameterType { get; set; }

            public SoupParamProp(string ParameterName, string ParameterType)
            {
                this.ParameterName = ParameterName;
                this.ParameterType = ParameterType;
            }

            public override string ToString()
            {
                return ParameterName + "|" + ParameterType + "|";
            }
        }

        public class SoupType
        {
            public SoupType(string Name)
            {
                this.Name = Name;
            }

            public string Name { get; set; }
            public List<SoupParamProp> Properties { get; set; } = new List<SoupParamProp>();

            public override string ToString()
            {
                string combined = Name + "|";
                combined += string.Join("|", Properties.Select(x => x.ToString()).ToArray());
                return combined;
            }
        }
    }

    public static class ServerModelHelper
    {
        public static byte[] GetServerModel(Type serverType, bool ModelValidation, out int Hash)
        {
            string[] Ignore = new string[] { "Equals", "GetHashCode", "GetType", "ToString" };
            SoupClientModel soupClientModel = new SoupClientModel(serverType.Name);
            foreach (MethodInfo method in serverType.GetMethods(BindingFlags.Public | BindingFlags.Instance))
            {
                if (!Ignore.Contains(method.Name))
                {
                    CheckType(ref soupClientModel, method.ReturnType);
                    bool Post = method.GetCustomAttribute<Post>() != null;
                    bool ReturnCode = method.GetCustomAttribute<ReturnCode>() != null;
                    SoupMethod Method = new SoupMethod(method.Name, method.ReturnType.GetFriendlyName(), Post, ReturnCode);

                    foreach (ParameterInfo parameter in method.GetParameters())
                    {
                        CheckType(ref soupClientModel, parameter.ParameterType);
                        Method.MethodParameters.Add(new SoupParamProp(parameter.Name, parameter.ParameterType.GetFriendlyName()));
                    }

                    soupClientModel.Methods.Add(Method);
                }
            }
            Hash = soupClientModel.GetHashCode();
            soupClientModel.Hash = Hash;
            soupClientModel.HashParameterName = HashParameterName;
            soupClientModel.ModelValidation = ModelValidation;
            return Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(soupClientModel));
        }

        internal static string GetFriendlyName(this Type type)
        {
            string friendlyName = type.Name;
            if (type.IsGenericType)
            {
                int iBacktick = friendlyName.IndexOf('`');
                if (iBacktick > 0)
                {
                    friendlyName = friendlyName.Remove(iBacktick);
                }
                friendlyName += "<";
                Type[] typeParameters = type.GetGenericArguments();
                for (int i = 0; i < typeParameters.Length; ++i)
                {
                    string typeParamName = GetFriendlyName(typeParameters[i]);
                    friendlyName += (i == 0 ? typeParamName : "," + typeParamName);
                }
                friendlyName += ">";
            }
            if (friendlyName == "Void")
            {
                friendlyName = "void";
            }

            return friendlyName;
        }

        internal const string HashParameterName = "hsh";

        static void CheckType(ref SoupClientModel model, Type t)
        {
            if (!IsSystemType(t) && !model.Types.Any(x => x.Name == t.Name))
            {
                SoupType type = new SoupType(t.Name);
                foreach (PropertyInfo property in t.GetProperties(BindingFlags.Public | BindingFlags.Instance))
                {
                    CheckType(ref model, property.PropertyType);
                    type.Properties.Add(new SoupParamProp(property.Name, property.PropertyType.GetFriendlyName()));
                }
                model.Types.Add(type);
            }
        }

        static string ExecutingAssembly = typeof(string).Assembly.FullName;

        internal static bool IsSystemType(Type type)
        {
            return type.Assembly.FullName == ExecutingAssembly;
        }

        public static SoupClientModel GetClientModel(string url)
        {
            string call = $"{url}/GetServerModel";
            var result = ApiCall.Call<SoupClientModel>(call, null, ApiMethod.Get, out HttpStatusCode status);
            return result;
        }
    }
}
