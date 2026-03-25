using System;
namespace Paperwork.Services.Generation
{
	public class PaperworkResult
	{

		public bool Success { get; set; }

		public string Message { get; set; }

		public int ResultCode { get; set; }

		public PaperworkDocumentResult Document {get;set;}

		public double GenerationDuration { get; set; }

		public double Progress { get; set; }

		public PaperworkGenerationLog Log { get; set; }

		public string Error { get; set; }

		public PaperworkResult()
		{
			Document = null;
			Success = false;
			Message = "";
			ResultCode = 0;
			Progress = 0.0;
			GenerationDuration = 0.0;
			Log = null;
			Error = "";
		}
	}

	

    public class PaperworkDocumentResult
    {
        

        public byte[] Binary { get; set; }

        public string MimeType { get; set; }

        public int DecodedSize { get; set; }

        public string DataFormat { get; set; }

        public PaperworkDocumentResult(byte[] binary, string mimetype, string dataformat, int decodedSize)
        {
			this.Binary = binary;
            this.MimeType = mimetype;
            this.DataFormat = dataformat;
            this.DecodedSize = decodedSize;
        }

		public PaperworkDocumentResult() { }
    }

    
}

