using System;
namespace Paperwork.Generation
{
	public static class GenerationErrors
	{
		public const int RequestNotValidCode = -400;
		public static readonly string RequestNotValidMessage = "The document generation request was not in a valid format";

		public const int NoGeneratorFoundCode = -404;
		public static readonly string NoGeneratorFoundMessage = "A document generator does not exist for the mime-type {1} with request version {0}. The supported versions are : {2}";

		public const int ErrorDuringProcessingCode = -500;
		public static readonly string ErrorDuringProcessingMessage = "The generator could not process the request, and an error was raised : {0}";

		public const int GenerationErrorCode = -406;
		public static readonly string GenerationErrorMessage = "The generator could not process the request as {0}";


    }
}

