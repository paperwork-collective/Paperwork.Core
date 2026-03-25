using System;
namespace Paperwork.Services.Generation
{
	public static class GenerationErrors
	{
		public static readonly int RequestNotValidCode = -400;
		public static readonly string RequestNotValidMessage = "The document generation request was not in a valid format";

		public static readonly int NoGeneratorFoundCode = -404;
		public static readonly string NoGeneratorFoundMessage = "A document generator does not exist for the mime-type {1} with request version {0}. The supported versions are : {2}";

		public static readonly int ErrorDuringProcessingCode = -500;
		public static readonly string ErrorDuringProcessingMessage = "The generator could not process the request, and an error was raised : {0}";

		public static readonly int GenerationErrorCode = -406;
		public static readonly string GenerationErrorMessage = "The generator could not process the request as {0}";


    }
}

