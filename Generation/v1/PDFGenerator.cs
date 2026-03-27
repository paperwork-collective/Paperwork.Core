#define OUTPUT_TO_CONSOLE

using System;
using System.Diagnostics;
using System.Text.Json;
using System.Xml.Linq;
using Scryber.Components;
using System.Reflection.Emit;
using System.Text;
using Scryber;
using Scryber.Caching;
using Scryber.Drawing;

namespace Paperwork.Services.Generation.v1
{
    public class PDFGenerator : IPaperworkGenerator, IDisposable
    {
        public static readonly Version MyVersion = new Version(1, 1);
        public static readonly string PDFMimeType = "application/pdf";
        public const string DefaultMainLayoutName = "main";

        
        public event EventHandler<GenerationProgressArgs>? GenerationProgress;

        protected virtual void OnGenerationProgress(GenerationProgressArgs args)
        {
            if (this.GenerationProgress != null)
                this.GenerationProgress(this, args);
        }



        public Version MinVersion { get { return MyVersion; } }

        public Version MaxVersion { get { return MyVersion; } }

        public string ResultMimeType { get { return PDFMimeType; } }

        public IPaperworkAuthService AuthService { get; private set; }

        public List<PaperworkRequestAuthToken> AuthTokens { get; private set; }

        public IPaperworkTracingService TracingService { get; private set; }

        public IPaperworkRemoteFileRequestService RemoteFileService { get; private set; }

        protected IPaperworkGenerationTracer Tracer { get; private set; }

        protected HttpClient HttpClient { get; private set; }

        protected PaperworkRequest Request { get; private set; }

        protected Scryber.ICacheProvider CacheProvider { get; set; }

        protected PaperworkRequestRenderOptions RenderOptions { get; set; }

        private static Scryber.ICacheProvider _staticCacheProvider = new Scryber.Caching.PDFStaticCacheProvider();


        public PDFGenerator(IPaperworkAuthService authService, IPaperworkTracingService tracingService, IPaperworkRemoteFileRequestService fileRequestService, Scryber.ICacheProvider cacheProvider = null)
        {
            AuthService = authService ?? throw new ArgumentNullException(nameof(authService));
            AuthTokens = null;
            RemoteFileService = fileRequestService;
            TracingService = tracingService;
            CacheProvider = cacheProvider ?? new Scryber.Caching.PDFNoCachingProvider();
        }



        public async Task<PaperworkResult> GenerateDocument(PaperworkRequest request, HttpClient client)
        {

            if (null != this.Request)
            {
                throw new InvalidOperationException("This generator is already processing a request. The generators can only process a single request.");
            }


            PaperworkResult result = null;
            this.RenderOptions = request.RenderOptions ?? new PaperworkRequestRenderOptions();

            this.Tracer = this.TracingService.Init(request);
            this.HttpClient = client;
            this.Request = request;
            this.CacheProvider = this.RenderOptions.Cache == PaperworkRequestCacheOption.None ? new Scryber.Caching.PDFNoCachingProvider() : _staticCacheProvider;


            try
            {
                
                result = await DoGenerateDocument(request, client);
            }
            finally
            {
                if(this.RenderOptions.Cache == PaperworkRequestCacheOption.None)
                {
                    //Scryber.Drawing.FontFactory.ReleaseLoaded();
                }
            }

            return result;

        }

        protected async virtual Task<PaperworkResult> DoGenerateDocument(PaperworkRequest request, HttpClient client)
        {
            TemplateConfigV1 template = null;

            using (this.Tracer.Begin(PaperworkGenerationStage.ConfigLoading))
            {

                if (string.IsNullOrEmpty(request.Content))
                    return CreateErrorResult("There was no content provided in the request", this.Tracer, null);

                this.AuthTokens = request.AuthTokens ?? new List<PaperworkRequestAuthToken>();


                bool dataLoaded = false;

                //1. Parse the content into a template
                try
                {
                    template = GetTemplateFromContent(request.Content);
                }
                catch (Exception ex)
                {
                    return CreateErrorResult("The content could not be understood. " + ex.Message, this.Tracer, ex);
                }

                //2. Check that all the remote sources are loaded
                try
                {
                    Console.WriteLine("Starting the load of the config data");
                    dataLoaded = await EnsureConfigDataLoaded(template, client);

                    if (!dataLoaded)
                        throw new InvalidDataException("One of the template contents could not be loaded.");

                }
                catch (Exception ex)
                {
                    return CreateErrorResult("The remote data could not be loaded within the template. " + ex.Message, this.Tracer, ex);
                }

            }

            this.OnGenerationProgress(new GenerationProgressArgs("Initialized", ProgressType.Stage, "Template initialized, loading document", 0.2));

            //3. Parse the document and the other template content from the template.

            Document doc;

            using (this.Tracer.Begin(PaperworkGenerationStage.TemplateParsing))
            {
                try
                {
                    var mainLayout = GetMainLayout(template);
                    doc = ParseLayoutDocument(mainLayout);
                    if (null != request.RenderOptions && null != request.RenderOptions.Overlay)
                        this.ApplyOverlayGrid(doc, request.RenderOptions.Overlay);

                    UpdateDocumentTraceLevel(doc, this.RenderOptions.LogLevel);
                    doc.CacheProvider = this.CacheProvider;
                    
                    //Add the base parameters
                    AddTemplateParametersToDocument(doc, template);
                    
                    //Add or override any that are set on the request
                    AddParametersToDocument(doc, request);

                    AddTemplateContentToDocument(doc, template);
                }
                catch (Exception ex)
                {
                    return CreateErrorResult("The root document template could not be loaded : " + ex.Message, this.Tracer, ex);
                }

                //4. Handle remote requests with authentication

                doc.RemoteFileRegistered += Doc_RemoteFileRegistered;


            }

            //5. Process the actual document

            PaperworkDocumentResult result;

            try
            {
                if (this.RenderOptions.UseAsync)
                    result = await GeneratePDFAsync(doc);
                else
                    result = GeneratePDF(doc);

                if (null == result)
                    throw new NullReferenceException("There was no document generated for the request");

            }
            catch (Exception ex)
            {
                return CreateErrorResult("Document creation failed during processing : " + ex.Message, this.Tracer , ex);
            }


            return CreateSuccessResult(request, result, this.Tracer);


        }

        private void ApplyOverlayGrid(Document doc, PaperworkOverlayGrid overlay)
        {
            if (overlay.Show)
            {
                Color c;
                if (Scryber.Drawing.Color.TryParse(overlay.Color, out Color color))
                    c = color;
                else
                    c = Color.Parse(PaperworkOverlayGrid.DefaultColor);

                Unit spacing = overlay.Spacing;
                int major = overlay.MajorCount;
                double opacity = overlay.Opacity;

                foreach (var pg in doc.Pages)
                {
                    pg.Style.OverlayGrid.ShowGrid = overlay.Show;
                    pg.Style.OverlayGrid.GridColor = c;
                    pg.Style.OverlayGrid.GridSpacing = spacing;
                    pg.Style.OverlayGrid.GridOpacity = opacity;
                    pg.Style.OverlayGrid.GridMajorCount = major;
                }
            }
        }


        private void UpdateDocumentTraceLevel(Document doc, PaperworkRequestLogOption loglevel)
        {
            if (loglevel > PaperworkRequestLogOption.Off)
            {
                doc.AppendTraceLog = true;

                if (doc.TraceLog != null)
                {
                    Scryber.TraceRecordLevel level = Scryber.TraceRecordLevel.Errors;
                    switch (loglevel)
                    {
                        case PaperworkRequestLogOption.Errors:
                            level = Scryber.TraceRecordLevel.Errors;
                            break;
                        case PaperworkRequestLogOption.Warnings:
                            level = Scryber.TraceRecordLevel.Warnings;
                            break;
                        case PaperworkRequestLogOption.Messages:
                            level = Scryber.TraceRecordLevel.Messages;
                            break;
                        case PaperworkRequestLogOption.Verbose:
                            level = Scryber.TraceRecordLevel.Verbose;
                            break;
                        default:
                            level = Scryber.TraceRecordLevel.Errors;
                            break;
                    }
                    doc.TraceLog.SetRecordLevel(level);
                }

            }
            else
            {
                //doc.AppendTraceLog = true;
                //doc.TraceLog.SetRecordLevel(Scryber.TraceRecordLevel.Messages);

                doc.AppendTraceLog = false;
            }
        }

        private void Doc_RemoteFileRegistered(object sender, Scryber.RemoteFileRequestEventArgs args)
        {
#if OUTPUT_TO_CONSOLE
            Console.WriteLine("A remote request has been registered for " + args.Request.FilePath);
#endif

            IPaperworkRemoteFileRequestor requestor;
            if (null != this.Tracer)
                this.Tracer.RegisterRequest(args.Request);

            if (this.RemoteFileService != null && this.RemoteFileService.ShouldHandle(args.Request, out requestor))
            {
                requestor.HandleRequest(this.HttpClient, args.Request);
            }
            //else if(args.Request.) TODO: Interpret the image cors argument for requests.
            else
            {
                args.Request.Owner.Document.TraceLog.Add(Scryber.TraceLevel.Verbose, "Generator", "A request was registered for " + args.Request.StubFilePathForLog + ", as no handler is set - requesting internally with cors applicable, and standard user agent");
            }
            
        }

        

        private PaperworkResult CreateErrorResult(string v, IPaperworkGenerationTracer tracer, Exception err)
        {
            string stack = "";
            if (null != err)
            {
                var full = new StringBuilder();
                while (null != err)
                {
                    var s = "------------------------------\r\n" + err.Message + "\r\n" + err.StackTrace + "\r\n";
                    if (full.Length > 0)
                        full.Insert(0, s);
                    else
                    {
                        full.Append(s);
                    }

                    err = err.InnerException;
                }

                stack = full.ToString();
            }
            
            return new PaperworkResult()
            {
                Document = null,
                Message = string.Format(GenerationErrors.GenerationErrorMessage, v),
                GenerationDuration = 0,
                ResultCode = GenerationErrors.GenerationErrorCode,
                Success = false,
                Error = stack,
                Progress = 1.0,
                Log = tracer.GetLog(),
            };
        }

        private PaperworkResult CreateSuccessResult(PaperworkRequest request, PaperworkDocumentResult documentResult, IPaperworkGenerationTracer tracer)
        {
            PaperworkResult result = new PaperworkResult()
            {
                Document = documentResult,
                Message = "Success",
                Progress = 1.0,
                ResultCode = 200,
                Success = true,
                Log = tracer.GetLog()
            };

            return result;
        }

        #region protected virtual TemplateConfigV1 GetTemplateFromContent(string content)

        /// <summary>
        /// 1. Returns the actual configuration data from the request.
        /// </summary>
        /// <param name="content">The JSON string to convert</param>
        /// <returns></returns>
        /// <exception cref="InvalidDataException">Thrown if the content could not be converted to a valisd</exception>
        protected virtual TemplateConfigV1 GetTemplateFromContent(string content)
        {
            try
            {
                var config = System.Text.Json.JsonSerializer.Deserialize<TemplateConfigV1>(content);

                if (null == config)
                    throw new InvalidDataException("The deserialized template was null");
                
                return config;
            }
            catch (InvalidDataException)
            {
                throw;
            }
            catch(Exception ex)
            {
                throw new InvalidDataException("The template content could not be parsed. " + ex.Message);
            }
        }

        #endregion

        #region protected virtual async Task<bool> EnsureConfigDataLoaded(TemplateConfigV1 config, HttpClient client)

        /// <summary>
        /// 2. Makes sure that all external resources are loaded using the known http client.
        /// </summary>
        /// <param name="config"></param>
        /// <exception cref="InvalidDataException"></exception>
        /// <exception cref="FileNotFoundException"></exception>
        /// <returns></returns>
        protected virtual async Task<bool> EnsureConfigDataLoaded(TemplateConfigV1 config, HttpClient client)
        {
            bool success = true;

            Auth.PaperworkAuthOptions options = new Auth.PaperworkAuthOptions();

            if (null != this.AuthTokens && this.AuthTokens.Count > 0)
            {
                foreach(var auth in this.AuthTokens)
                {
                    options.OAuthTokens.Add(auth.Name, auth.Token);
                }
            }
            try
            {
                var successOne = await config.Load(async (string url, string auth, string mimetype) =>
                {
#if OUTPUT_TO_CONSOLE
                    Console.WriteLine("Starting the load of config data for " + url);
#endif
                    Scryber.RemoteFileRequest request = null;

                    try
                    {
                        request = new Scryber.RemoteFileRequest("Config", url, 
                            Scryber.Caching.PDFCacheProvider.NoCacheDuration, (raiser, request, response) => { return true;});
                        
                        request.StartRequest();

                        this.Tracer.RegisterRequest(request);
                        Uri uri = new Uri(url);
                        

                        if (!string.IsNullOrEmpty(auth))
                        {
                            
                            if (!this.AuthService.CanFetch(auth, uri))
                                throw new ArgumentOutOfRangeException(auth, "The url " + url + "' cannot be retrieved via the authenticated service " + auth);
                            else
                            {
                                string result = await AuthService.Fetch(client, auth, uri, options);
                                success = !string.IsNullOrEmpty(result);
                                if (success)
                                {
                                    request.CompleteRequest(result, success);
                                }
                                else
                                {
#if OUTPUT_TO_CONSOLE
                                    Console.WriteLine("Failed to load the content for " + url + ". An empty string was returned");
#endif
                                }
                                return result;
                            }
                        }
                        
                        else if (mimetype.StartsWith("text/"))
                        {
                            var result = await client.GetStringAsync(uri);
                            success = !string.IsNullOrEmpty(result);
#if OUTPUT_TO_CONSOLE
                            Console.WriteLine("Completed load of text config data for " + url);
#endif
                            request.CompleteRequest(result, success);
                            return result;
                        }
                        else
                        {
                            var bin = await client.GetByteArrayAsync(uri);
                            success = bin != null && bin.Length > 0;
#if OUTPUT_TO_CONSOLE
                            Console.WriteLine("Completed load of binary config data for " + url);
#endif
                            request.CompleteRequest(bin, success);
                            return bin;
                        }
                    }
                    catch (Exception ex)
                    {
                        if (null != request && !request.IsCompleted)
                        {
                            try
                            {
                                request.CompleteRequest(null, false, ex);
                            }
                            catch { }
                        }

                        throw new FileNotFoundException("Could not load the data for the configured resource : " + (url ?? "NULL") + "." + ex.Message, ex);
                    }
                });

                success = success && successOne;

            }
            catch (FileNotFoundException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new InvalidDataException("The configuration template (V1.1) could not load the requested resources: " + ex.Message, ex);
            }

            if (!success)
            {
                throw new InvalidDataException("Not all of the configured resources in the template request (V1.1) could be loaded. Cannot continue");
            }

            return success;
        }


#endregion

        #region protected virtual TemplateItemContentBase GetMainLayout(TemplateConfigV1 template)

        /// <summary>
        /// 3.1 Retrieves the 'main' layout template from the template
        /// </summary>
        /// <param name="template">The template to get the layout from</param>
        /// <returns>A non-null layout template item</returns>
        /// <exception cref="ArgumentOutOfRangeException">If there is more that one layout with the designated name</exception>
        /// <exception cref="ArgumentNullException">If there is no layout with the designated name</exception>
        protected virtual TemplateItemContentBase GetMainLayout(TemplateConfigV1 template)
        {
            TemplateItemContentBase mainLayout = null;
            Dictionary<string, string> otherlayouts = new Dictionary<string, string>();

            string mainName = template.MainLayoutName ?? DefaultMainLayoutName;

            //mainLayout = config.Layout[0];
            foreach (var layout in template.Layout)
            {
                if (!string.IsNullOrEmpty(layout.Name) && layout.Name == mainName)
                {
                    if (null != mainLayout)
                    {
                        throw new ArgumentOutOfRangeException("There are multiple layouts with the name " + mainName + ". Cannot determine the correct layout");
                    }
                    else
                    {
                        mainLayout = layout;
                    }
                }
            }

            if (null == mainLayout)
                throw new ArgumentOutOfRangeException("There is no layout found with the name '" + mainLayout + "'. Ensure that the configured main layout name matches a template in the configuration");

            if (string.IsNullOrEmpty(mainLayout.Value()))
                throw new ArgumentNullException("The content of the layout '" + mainName + "' in the template configuration (V1.1) is empty, and cannot be used");

            return mainLayout;
        }

        #endregion

        #region protected virtual Document ParseLayoutDocument(TemplateItemContentBase item)

        /// <summary>
        /// 3.2. Parses the actual content of the template item into a single document.
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        /// <exception cref="Scryber.PDFParserException"></exception>
        /// <exception cref="NullReferenceException"></exception>
        protected virtual Document ParseLayoutDocument(TemplateItemContentBase item)
        {
            var path = item.BaseSource() ?? "";
            var content = item.Value();

            var isxhtml = DoCheckIsXHTML(content);

            using var stream = new StringReader(content);

            Document doc = null;
            var type = string.IsNullOrEmpty(path) ? Scryber.ParseSourceType.DynamicContent : Scryber.ParseSourceType.RemoteFile;
            try
            {
                if (isxhtml)
                {
                    doc = Document.ParseDocument(stream, path, type);
                }
                else
                {
                    doc = Document.ParseHtmlDocument(stream, path, type);
                }
            }
            catch (Exception ex)
            {
                throw new Scryber.PDFParserException("Could not convert the template layout to a document, please check the source template content. " + ex.Message, ex);
            }

            if (null == doc)
                throw new NullReferenceException("No document was returned from the parsed template content, please check the validity of the content.");

            return doc;
        }

        /// <summary>
		/// Confirms if there is an xmlns attribute declaration within the root elements
		/// </summary>
		/// <param name="content"></param>
		/// <returns></returns>
        private bool DoCheckIsXHTML(string content)
        {
            var start = content.IndexOf("<html ", StringComparison.OrdinalIgnoreCase);
            if (start >= 0)
            {

                var end = content.IndexOf(">", start);
                if (end > start + 6)
                {
                    var sub = content.Substring(start, end - start);

                    var contains = sub.IndexOf("xmlns=", StringComparison.OrdinalIgnoreCase);
                    if (contains > 0)
                        return true;

                    var containsQualified = sub.IndexOf("xmlns:", StringComparison.OrdinalIgnoreCase);
                    if (containsQualified > 0)
                        return true;

                }

            }
            else //check for namespace qualified (e.g <x:html xmlns:x='http namespace')
            {
                start = content.IndexOf(":html ");
                if (start > 0)
                {
                    var nspos = start;
                    while (nspos >= 0 && content[nspos] != '<')
                        nspos--;

                    if (nspos < 0)
                        return false;

                    var ns = content.Substring(nspos, start - nspos);
                    var xmlns = "xmlns:" + ns + "=";

                    var end = content.IndexOf(">", start);
                    if(end > start + 6)
                    {
                        var sub = content.Substring(start, end - start);
                        var contains = sub.IndexOf(xmlns);

                        if (contains > 0)
                            return true;
                    }
                }

            }

            return false;
        }

        #endregion

        public const string ParameterValuesName = "_fields";

        protected virtual void AddTemplateParametersToDocument(Document doc, TemplateConfigBase config)
        {
            TemplateConfigV1 configV1 = config as TemplateConfigV1;
            if(null == configV1)
                return;
            
            Dictionary<string, object> values = new Dictionary<string, object>();

            if (configV1.Fields != null && configV1.Fields.Count > 0)
            {
                foreach (var param in configV1.Fields)
                {
                    values.Add(param.ID, param.Value);
                    doc.TraceLog.Add(Scryber.TraceLevel.Message, "Generator","Set the template parameter " + ParameterValuesName + "." + param.ID + " to '" + param.Value + "'");
                }
            }
            
            doc.Params[ParameterValuesName] = values;
        }
        protected virtual void AddParametersToDocument(Document doc, PaperworkRequest request)
        {
            if (request.Fields != null && request.Fields.Count > 0)
            {
                Dictionary<string, object> values = doc.Params[ParameterValuesName] as Dictionary<string, object>;

                if (null == values)
                    values = new Dictionary<string, object>();

                foreach (var param in request.Fields)
                {
                    switch (param.Type)
                    {
                        case("string"): 
                        default:
                            //TODO: Validate the type and options.
                            values[param.Id] = param.Value;
                            doc.TraceLog.Add(Scryber.TraceLevel.Message, "Generator","Set the request parameter " + ParameterValuesName + "." + param.Id + " to '" + param.Value + "'");
                            break;
                    }
                }

                doc.Params[ParameterValuesName] = values;
            }
            else
            {
                doc.TraceLog.Add(Scryber.TraceLevel.Message, "Generator","No request fields to set on document");
            }
        }

        protected virtual void AddContextToDocument(Document doc, PaperworkRequest request)
        {
            
        }
        #region protected virtual void AddTemplateContentToDocument(Document doc, TemplateConfigBase config)

        /// <summary>
        /// 3.3. Adds all the styles, data and layouts to the document, that is in the content for the config file.
        /// </summary>
        /// <param name="doc"></param>
        /// <param name="config"></param>
        protected virtual void AddTemplateContentToDocument(Document doc, TemplateConfigBase config)
        {
            //Add the data

            try
            {

                var data = config.DataContent();

                if (null != data && data.Count > 0)
                {
                    for (var i = 0; i < data.Count; i++)
                    {
                        try
                        {
                            var item = data[i];
                            var key = item.Name;
                            if (string.IsNullOrEmpty(key))
                            {
                                key = "_unnamed" + i.ToString();
                            }

                            var value = (item.Value() ?? "").Trim();

                            if (!string.IsNullOrEmpty(value))
                            {
                                //Can escape natual values that start with and end with {} or [] by doubling.
                                //Otherwise they will be treated as JSON

                                if (value.StartsWith("{{") && value.EndsWith("}}"))
                                {
                                    doc.Params[key] = value.Substring(1, value.Length - 2);
                                }
                                else if (value.StartsWith("[[") && value.EndsWith("]]"))
                                {
                                    doc.Params[key] = value.Substring(1, value.Length - 2);
                                }
                                else if ((value.StartsWith("{") && value.EndsWith("}")) || (value.StartsWith("[") && value.EndsWith("]")))
                                {
                                    var obj = ParseJSONData(key, value);

                                    if (null != obj)
                                    {
                                        doc.Params[key] = obj.RootElement.Clone();
                                    }
                                }
                                else
                                {
                                    //Just a string value
                                    doc.Params[key] = value;
                                }
                            }
                        }
                        catch (InvalidDataException)
                        {
                            throw;
                        }
                        catch (Exception ex)
                        {
                            throw new InvalidDataException("The data item at index " + i.ToString() + " could not be parsed. Please check the configuration. ", ex);
                        }
                    }
                }
            }
            catch (InvalidDataException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new InvalidDataException("The template data content could not be parsed into valid objects. Please check the source or inner errors.", ex);
            }

            //Add the styles

            var html = doc as Scryber.Html.Components.HTMLDocument;
            var styles = config.StyleContent();

            if (null != html && null != styles && styles.Count > 0)
            {
                try
                {
                    if (null == html.Head)
                        html.Head = new Scryber.Html.Components.HTMLHead();

                    for (var i = 0; i < styles.Count; i++)
                    {
                        var item = (StyleContent)styles[i];
                        var key = item.Name;
                        if (string.IsNullOrEmpty(key))
                        {
                            key = "_style" + i.ToString();
                        }
                        
                        var css = new Scryber.Html.Components.HTMLStyle();
                        css.Contents = item.Value();
                        css.ID = key;
                        css.LoadedSource = item.BaseSource();
                        css.LoadType = Scryber.ParserLoadType.WebBuildProvider;
                        html.Head.Contents.Add(css);

                    }

                }
                catch (InvalidDataException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    throw new InvalidDataException("The template style content could not be parsed into valid css. Please check the source or inner errors. ", ex);
                }
            }
            
            

            //TODO: Add the layouts
        }

        /// <summary>
		/// Deserializes a single JSON data value.
		/// </summary>
		/// <param name="name"></param>
		/// <param name="value"></param>
		/// <returns></returns>
		/// <exception cref="InvalidDataException">Thrown if the data cannot be parsed.</exception>
		protected virtual JsonDocument ParseJSONData(string name, string value)
        {
            JsonDocument result = null;
            try
            {
                result = JsonDocument.Parse(value);
            }
            catch (Exception ex)
            {
                throw new InvalidDataException("The JSON data for '" + name + "' could not be converted to a valid object for the document to consume - " + ex.Message, ex);
            }

            return result;
        }

        #endregion

        private IDisposable _currentTrace;


        protected virtual async Task<PaperworkDocumentResult> GeneratePDFAsync(Document document)
        {

            document.Loaded += Document_Loaded;
            document.DataBound += Document_DataBound;
            document.LayoutComplete += Document_LayoutComplete;

            MemoryStream ms = null;
            StreamReader sr = null;

            byte[] data;

            this._currentTrace = this.Tracer.Begin(PaperworkGenerationStage.DocumentLoading);

            try
            {
                ms = new MemoryStream();
                await document.SaveAsPDFAsync(ms);

                data = ms.ToArray();
                ms.Position = 0;

            }
            finally
            {
                if (ms != null)
                    ms.Dispose();

                if (null != sr)
                    sr.Dispose();

                if (null != _currentTrace)
                    _currentTrace.Dispose();

                this._currentTrace = null;
                ms = null;
                sr = null;
            }

            PaperworkDocumentResult documentResult = new PaperworkDocumentResult(
                data, PDFMimeType, "text/plain", data.Length);

            return documentResult;
        }

        

        protected virtual PaperworkDocumentResult GeneratePDF(Document document)
        {
            document.Loaded += Document_Loaded;
            document.DataBound += Document_DataBound;
            document.LayoutComplete += Document_LayoutComplete;

            MemoryStream ms = null;
            StreamReader sr = null;

            this._currentTrace = this.Tracer.Begin(PaperworkGenerationStage.DocumentLoading);

            byte[] data;

            try
            {
                ms = new MemoryStream();
                document.SaveAsPDF(ms);

                ms.Position = 0;
                data = ms.ToArray();

            }
            finally
            {
                if (ms != null)
                    ms.Dispose();

                if (null != sr)
                    sr.Dispose();

                if (null != _currentTrace)
                    _currentTrace.Dispose();

                this._currentTrace = null;
                sr = null;
                ms = null;
            }

            PaperworkDocumentResult documentResult = new PaperworkDocumentResult(
                data, PDFMimeType, "text/plain", data.Length);

            return documentResult;
        }

        

        private void Document_Loaded(object sender, Scryber.LoadEventArgs args)
        {
            if (null != this._currentTrace)
                _currentTrace.Dispose();
            
            //TODO: Raise Progress 40%
            this.OnGenerationProgress(new GenerationProgressArgs("Loaded", ProgressType.Stage, "Document loaded, binding data", 0.4));

            _currentTrace = this.Tracer.Begin(PaperworkGenerationStage.DataBinding);
        }

        

        private void Document_DataBound(object sender, Scryber.DataBindEventArgs e)
        {

            if (null != this._currentTrace)
                _currentTrace.Dispose();

            //TODO: Raise Progress 60%
            this.OnGenerationProgress(new GenerationProgressArgs("Bound", ProgressType.Stage, "Data bound, laying out content", 0.6));

            _currentTrace = this.Tracer.Begin(PaperworkGenerationStage.LayingOutPages);
        }

        private void Document_LayoutComplete(object sender, Scryber.LayoutEventArgs args)
        {
            if (null != this._currentTrace)
                _currentTrace.Dispose();

            //TODO: Raise Progress 80%
            this.OnGenerationProgress(new GenerationProgressArgs("Laidout", ProgressType.Stage, "Pages created, rendering document", 0.8));

            _currentTrace = this.Tracer.Begin(PaperworkGenerationStage.RenderingDocument);
        }


        public void Dispose()
        {
            this.Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (null != this.HttpClient)
                    this.HttpClient = null;
                if (null != this.Request)
                    this.Request = null;
                if (null != this.Tracer)
                    this.Tracer = null;
            }
        }

    }
}

