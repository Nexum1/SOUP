using System;
using System.Net;
using System.Collections.Generic;

namespace Soup
{

public static class Test_Calls
{
	const int Hash = -1131965206;
	public static void Control()
	{
		HttpStatusCode code;
		Dictionary<string, object> parameters = new Dictionary<string, object>()
		{
			{"hsh", Hash},
		};
		ApiCall.Call("http://localhost:8080/Control", parameters, ApiMethod.Get, out code);
		if(code != HttpStatusCode.OK){throw new Exception("Call Failed:" + code.ToString());}
	}
	public static void Control2()
	{
		HttpStatusCode code;
		Dictionary<string, object> parameters = new Dictionary<string, object>()
		{
			{"hsh", Hash},
		};
		ApiCall.Call("http://localhost:8080/Control2", parameters, ApiMethod.Post, out code);
		if(code != HttpStatusCode.OK){throw new Exception("Call Failed:" + code.ToString());}
	}
}
}
