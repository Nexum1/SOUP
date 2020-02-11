using System;
using System.Net;
using System.Collections.Generic;

namespace Soup
{

public static class TestServer_Calls
{
	const int Hash = 1046759330;
	public static String TestMethod()
	{
		HttpStatusCode code;
		Dictionary<string, object> parameters = new Dictionary<string, object>()
		{
			{"hsh", Hash},
		};
		var result = ApiCall.Call<String>("http://localhost:8090/TestMethod", parameters, ApiMethod.Get, out code);
		if(code != HttpStatusCode.OK){throw new Exception("Call Failed:" + code.ToString());}
		return result;
	}
}
}
