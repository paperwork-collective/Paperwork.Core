using System;
namespace Paperwork.Services.Generation
{
	public class PaperworkGenerationLog
	{

		private List<PaperworkGenerationTraceLogEntry> _entries;

		/// <summary>
		/// Gets or sets the number of entries in the log
		/// </summary>
		public int EntryCount
		{
			get
			{
				return _entries.Count;
			}
			set
			{
                /* do nothing, just for serialization */
            }
        }

		/// <summary>
		/// Gets or sets the number of milliseconds since the UNIX Epoch start date the execution began (since Jan 1st 1970 UTC)
		/// </summary>
		public long EpochStartMs { get; set; }

		/// <summary>
		/// Gets or sets the number of milliseconds since the UNIX Epoch start date the execution ended (since Jan 1st 1970 UTC)
		/// </summary>
		public long EpochEndMs { get; set; }

		public PaperworkGenerationTraceLogEntry[] Entries
		{
			get { return _entries.ToArray(); }
			set { _entries = new List<PaperworkGenerationTraceLogEntry>(value); }
		}
		

		public PaperworkGenerationLog()
		{
			this._entries = new List<PaperworkGenerationTraceLogEntry>();
		}

		

		public void AddEntry(PaperworkGenerationTraceLogEntry entry)
		{
			if (null == entry)
				throw new ArgumentNullException("The entry cannot be null");

			this._entries.Add(entry);
		}
	}

	/// <summary>
	/// Represents a heiracical structure of generation entries
	/// </summary>
	public class PaperworkGenerationTraceLogEntry
	{
		
		public int Index { get; set; }
		public string Name { get; set; }
		public string Description { get; set; }
		public long StartMs { get; set; }
		public long EndMs { get; set; }

		public PaperworkGenerationTraceLogRequest[] InnerRequests { get; set; }

		public PaperworkGenerationTraceLogEntry()
		{
			this.Name = string.Empty;
			this.Description = string.Empty;
		}

		public PaperworkGenerationTraceLogEntry(int index, string name, string desc, long start, long end, PaperworkGenerationTraceLogRequest[] innerRequests)
		{
			this.Index = index;
			this.Name = name;
			this.Description = desc;
			this.StartMs = start;
			this.EndMs = end;
			this.InnerRequests = innerRequests;
		}


    }

	public class PaperworkGenerationTraceLogRequest
	{
		public string Path { get; set; }

		public string ErrorMessage { get; set; }

		public long StartMs { get; set; }

		public long EndMs { get; set; }

		public bool Success { get; set; }

		public PaperworkGenerationTraceLogRequest()
		{
			this.Path = string.Empty;
			this.ErrorMessage = string.Empty;
		}
	}


}

