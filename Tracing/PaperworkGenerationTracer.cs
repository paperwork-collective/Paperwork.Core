using System;
using System.Diagnostics;
using Paperwork.Services.Generation;

namespace Paperwork.Services.Tracing
{
	public class PaperworkGenerationTracer : IPaperworkGenerationTracer
	{
        private Stopwatch _stopwatch;
        private long _offset = 0;
        private GenerationTracerEntry _current;
        private PaperworkGenerationLog _log;
        private Dictionary<PaperworkGenerationStage,List<GenerationRemoteRequest>> _remoteRequests;

		public PaperworkGenerationTracer(long epochStartMS)
		{
            _current = null;
            _stopwatch = new Stopwatch();
            _log = new PaperworkGenerationLog();
            _log.EpochStartMs = epochStartMS;
            _remoteRequests = new Dictionary<PaperworkGenerationStage, List<GenerationRemoteRequest>>();

		}

        public PaperworkGenerationStage Current
        {
            get
            {
                if (null != this._current)
                    return this._current.Stage;
                else
                    return PaperworkGenerationStage.None;
            }
        }

        public IDisposable Begin(PaperworkGenerationStage stage)
        {
            if (!_stopwatch.IsRunning)
                _stopwatch.Start();

            this._current = new GenerationTracerEntry(this, stage);
            return this._current;
        }

        private void EndEntry(GenerationTracerEntry entry)
        {
            if (entry == this._current)
            {
                _log.AddEntry(new PaperworkGenerationTraceLogEntry(
                    (int)entry.Stage,
                    GetLogEntryName(entry.Stage),
                    GetLogEntryDescription(entry.Stage),
                    entry.StartMilliSecond + _offset,
                    entry.EndMilliSecond + _offset,
                    new PaperworkGenerationTraceLogRequest[] { }
                    )
                );

                _log.EpochEndMs = (long)Math.Ceiling(DateTime.Now.Subtract(DateTime.UnixEpoch).TotalMilliseconds);

                this._current = null;
                this._stopwatch.Stop();
            }
            else
            {
                throw new InvalidOperationException("The currently executing stage is not " + entry.Stage);
            }
        }

        public void RegisterRequest(Scryber.RemoteFileRequest request)
        {
            List<GenerationRemoteRequest> requests;

            if (!this._remoteRequests.TryGetValue(this.Current, out requests) || null == requests )
            {
                requests = new List<GenerationRemoteRequest>();
                this._remoteRequests[this.Current] = requests;
            }

            var one = GenerationRemoteRequest.Start(request, this._stopwatch);
            requests.Add(one);
        }

        

        public PaperworkGenerationLog GetLog()
        {
            foreach(var entry in this._log.Entries)
            {
                var stage = Enum.Parse<PaperworkGenerationStage>(entry.Name);
                var all = GetInnerRequestsAndClear(stage);
                entry.InnerRequests = all;
            }

            return _log;
        }


        private PaperworkGenerationTraceLogRequest[] GetInnerRequestsAndClear(PaperworkGenerationStage forStage)
        {
            List<GenerationRemoteRequest> requests;

            if (!this._remoteRequests.TryGetValue(forStage, out requests) || requests.Count == 0)
                return null;
            else
            {
                List<PaperworkGenerationTraceLogRequest> all = new List<PaperworkGenerationTraceLogRequest>();
                foreach (var request in requests)
                {
                    if (request.EndMs > 0)
                    {
                        all.Add(new PaperworkGenerationTraceLogRequest()
                        {
                            EndMs = request.EndMs,
                            StartMs = request.StartMs,
                            Success = request.Success,
                            Path = request.Path,
                            ErrorMessage = request.Error
                        });
                    }
                    else
                    {
                        all.Add(new PaperworkGenerationTraceLogRequest()
                        {
                            EndMs = -1,
                            StartMs = request.StartMs,
                            Success = false,
                            Path = request.Path,
                            ErrorMessage = "Request did not complete within the document generation stage"
                        });
                    }
                }
                requests.Clear();
                return all.ToArray();
            }
        }

        private string GetLogEntryName(PaperworkGenerationStage stage)
        {
            return stage.ToString();
        }

        private string GetLogEntryDescription(PaperworkGenerationStage stage)
        {
            return string.Empty;
        }

        

        /// <summary>
        /// Implements the recording of an entry 
        /// </summary>
        private class GenerationTracerEntry : IDisposable
        {
            private PaperworkGenerationTracer Tracer { get; set; }
            
            public long StartMilliSecond { get; private set; }

            public long EndMilliSecond { get; private set; }

            /// <summary>
            /// Gets the stage that this entry was started for.
            /// </summary>
            public PaperworkGenerationStage Stage
            {
                get;
                private set;
            }

            /// <summary>
            /// Gets the total duration of the stage this entry is recording
            /// </summary>
            public long DurationMilliSecond
            {
                get
                {
                    if (this.EndMilliSecond < 0)
                        return 0;
                    else
                        return this.EndMilliSecond - this.StartMilliSecond;
                }
            }

            /// <summary>
            /// Returns true if this stage is running
            /// </summary>
            public bool Running
            {
                get
                {
                    if (this.EndMilliSecond < 0)
                        return true;
                    else
                        return false;
                }
            }

            /// <summary>
            /// Creates a new instance of the tracer entry and starts the measurement process.
            /// </summary>
            /// <param name="tracer">The tracer this entry refers to</param>
            /// <param name="stage">The stage the tracer is at.</param>
            public GenerationTracerEntry(PaperworkGenerationTracer tracer, PaperworkGenerationStage stage)
            {
                this.Tracer = tracer ?? throw new ArgumentNullException("A valid tracer is needed");

                if (this.Tracer.Current != PaperworkGenerationStage.None)
                    throw new ArgumentOutOfRangeException("The tracer is currently executing stage " + this.Tracer.Current);

                this.Stage = stage;
                this.StartMilliSecond = this.Tracer._stopwatch.ElapsedMilliseconds;
                this.EndMilliSecond = -1L;
            }

            /// <summary>
            /// Stops the tracer entry recoding the overall time and ending.
            /// </summary>
            public void Dispose()
            {
                this.EndMilliSecond = this.Tracer._stopwatch.ElapsedMilliseconds;
                this.Tracer.EndEntry(this);
                this.Tracer = null;
            }
        }

        /// <summary>
        /// Implements the recording of a remote request
        /// </summary>
        private class GenerationRemoteRequest
        {
            public string Path { get; set; }

            public long StartMs { get; set; }

            public long EndMs { get; set; }

            public bool Success { get; set; }

            public string Error { get; set; }

            private Stopwatch _stopwatch;

            public static GenerationRemoteRequest Start(Scryber.RemoteFileRequest forRequest, Stopwatch stopwatch)
            {
                var started = new GenerationRemoteRequest();
                started.StartMs = stopwatch.ElapsedMilliseconds;
                started.EndMs = -1;
                started.Path = forRequest.FilePath;
                started._stopwatch = stopwatch;

                if (forRequest.IsCompleted)
                {
                    started.EndMs = stopwatch.ElapsedMilliseconds;
                    started.Success = forRequest.IsSuccessful;
                    started.Error = forRequest.Error == null ? string.Empty : forRequest.Error.Message;
                }
                else
                {
                    forRequest.Completed += started.ForRequest_Completed;

                }

                return started;
            }

            private void ForRequest_Completed(object sender, Scryber.RequestCompletedEventArgs args)
            {
                if(this.EndMs < 0)
                {
                    try
                    {
                        this.EndMs = this._stopwatch.ElapsedMilliseconds;

                        if (args.Result != null)
                            this.Success = args.Request.IsSuccessful;

                        else
                        {
                            this.Success = false;
                            if (null != args.Request.Error)
                                this.Error = args.Request.Error.Message;
                            else
                                this.Error = "Unknown error occurred";
                        }
                    }
                    catch(Exception ex)
                    {
                        this.EndMs = this.StartMs;
                        this.Success = false;
                        this.Error = "Could not record the request details on completion : " + ex.Message;
                    }

                }
            }
        }
    }

    
}

