using System;
namespace Paperwork.Services.Generation
{
	public enum ProgressType
	{
		Stage,
		FileLoad
	}

	public class GenerationProgressArgs
	{
		public double Progress { get; set; }

		public string Stage { get; set; }

		public string Message { get; set; }

		public string Type { get; set; }

		public GenerationProgressArgs(string stage, ProgressType type, string message, double progress)
		{
			this.Progress = progress;
			this.Stage = stage;
			this.Message = message;
			this.Type = type.ToString();
		}

        public override string ToString()
        {
			return ToString("|");
        }

		public string ToString(string separator)
		{
			return $"Progress{separator}{this.Progress}{separator}{this.Stage}{separator}{this.Type}{separator}{this.Message}";
		}
    }
}

