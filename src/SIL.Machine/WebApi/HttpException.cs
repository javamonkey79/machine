﻿using System;

namespace SIL.Machine.WebApi
{
    public class HttpException : Exception
    {
        public HttpException(string message) : base(message) { }

        public int StatusCode { get; set; }
    }
}
