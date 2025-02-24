﻿namespace VaccineChildren.Core.Store;

public enum StatusCodeHelper
{
	[CustomName("Success")] OK = 200,
	[CustomName("Created")] Created = 201,

	[CustomName("Bad Request")] BadRequest = 400,

	[CustomName("Not Found")] NotFound = 404,

	[CustomName("Unauthorized")] Unauthorized = 401,

	[CustomName("Internal Server Error")] ServerError = 500
}
