using System;
namespace Paperwork.Services.Generation
{
    public class PaperworkRequest
    {
        /// <summary>
        /// Gets or sets the TemplateConfig content as a string
        /// </summary>
        public string Content { get; set; }

        /// <summary>
        /// Gets or sets the Major version of the template config schema - Default is 1
        /// </summary>
        public int MajorVersion { get; set; }

        /// <summary>
        /// Gets or sets the Minor version of the Template config schema - Default is 1
        /// </summary>
        public int MinorVersion { get; set; }

        /// <summary>
        /// Gets or sets the Format of the Template config (Base64, Content, or Remote)
        /// </summary>
        public string Format { get; set; }

        /// <summary>
        /// Gets or sets the url for the source of the Template Config
        /// </summary>
        public string BaseSource { get; set; }

        /// <summary>
        /// Gets or sets the format for the output required (only PDF is currently supported)
        /// </summary>
        public string Output { get; set; }


        /// <summary>
        /// Gets or sets the list of authenticated services
        /// </summary>
        public List<PaperworkRequestAuthToken> AuthTokens {get;set;}

        public List<PaperworkRequestField> Fields { get; set; }

        /// <summary>
        /// Gets or sets the rendering output options for this request
        /// </summary>
        public PaperworkRequestRenderOptions RenderOptions { get; set; }

        /// <summary>
		/// Gets or sets the start time offset in milliseconds sing the UNIX Epoch start time (Jan 1st 1970 UTC).
		/// </summary>
		public long EpochStartOffset { get; set; }

        /// <summary>
        /// Creates a new Generation request
        /// </summary>
        public PaperworkRequest()
        {
            this.MajorVersion = 1;
            this.MinorVersion = 1;
            this.EpochStartOffset = -1;
            this.Content = string.Empty;
            this.Format = "Content";
            this.Output = "application/pdf";
            this.BaseSource = string.Empty;
            this.RenderOptions = new PaperworkRequestRenderOptions();
            this.AuthTokens = new List<PaperworkRequestAuthToken>();
            this.Fields = new List<PaperworkRequestField>();
        }
    }

    public class PaperworkRequestAuthToken
    {
        public string Name { get; set; }

        public string Token { get; set; }

        public PaperworkRequestAuthToken()
        {
            this.Name = string.Empty;
            this.Token = string.Empty;
        }
    }

    /// <summary>
    /// Represents a single field that can be used within the template
    /// </summary>
    public class PaperworkRequestField
    {
        public string Id { get; set; }

        public string Type { get; set; }

        public string Value { get; set; }

    }

    public class PaperworkRequestRenderOptions
    {

        public bool UseAsync { get; set; }

        public PaperworkRequestLogOption LogLevel { get; set; }

        public PaperworkRequestCacheOption Cache { get; set; }
        
        public PaperworkOverlayGrid Overlay { get; set; }

        public PaperworkRequestRenderOptions()
        {
            this.LogLevel = PaperworkRequestLogOption.Off;
            this.Cache = PaperworkRequestCacheOption.Static;
        }
    }

    public class PaperworkOverlayGrid
    {
        public bool Show { get; set; } = false;

        public int Spacing { get; set; } = DefaultSpacing;

        public int MajorCount { get; set; } = DefaultMajor;

        public string Color { get; set; } = DefaultColor;

        public double Opacity { get; set; } = DefaultOpacity;

        public double LineThickness { get; set; } = DefaultThickness;

        public static string DefaultColor = "#00FFFF";
        public static double DefaultOpacity = 0.5;
        public static int DefaultSpacing = 10;
        public static int DefaultMajor = 5;
        public static double DefaultThickness = 0.5;

    }

    public enum PaperworkRequestLogOption
    {
        Off = 0,
        Errors = 1,
        Warnings = 2,
        Messages = 3,
        Verbose = 4
    }

    public enum PaperworkRequestCacheOption
    {
        None = 0,
        Static = 1
    }
}

