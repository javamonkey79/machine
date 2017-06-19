﻿using System;

namespace SIL.Machine.Web
{
	public class MockRequest
	{
		public HttpRequestMethod Method { get; set; }
		public string Url { get; set; }
		public string Body { get; set; }
		public Action<string> CheckBody { get; set; }
		public string ResponseText { get; set; }
		public int ErrorStatus { get; set; }
	}
}
