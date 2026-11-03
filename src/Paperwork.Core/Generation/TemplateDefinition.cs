using System;
using Paperwork.Generation;

namespace Paperwork.Generation
{
    public abstract class TemplateDefinition
    {

        public Version Version { get; private set; }

        public TemplateDefinition(int major, int minor, string mainlayout)
        {
            this.Version = new Version(major, minor);
            this.MainLayoutName = mainlayout;
        }

        public virtual string MainLayoutName { get; set; }

        public abstract List<TemplateDefinitionItem> LayoutContent();

        public abstract List<TemplateDefinitionItem> DataContent();

        public abstract List<TemplateDefinitionItem> StyleContent();

        public async Task<bool> Load(LoadActionAsync loadCallback)
        {
            return await this.DoLoad(loadCallback);
        }

        public abstract Task<bool> DoLoad(LoadActionAsync loadCallback);

        public static string FromBase64ToString(string encoded)
        {
            var bin = Convert.FromBase64String(encoded);
            using var stream = new MemoryStream(bin);
            using var reader = new StreamReader(stream);
            var content = reader.ReadToEnd();

            return content;

        }
    }

    public abstract class TemplateDefinitionItem
    {
        public virtual string Name { get; set; }

        /// <summary>
        /// Returns the actual content of the template item
        /// </summary>
        /// <returns></returns>
        public virtual string Value()
        {
            return string.Empty;
        }

        /// <summary>
        /// Returns the original source url for the content value (if any)
        /// </summary>
        /// <returns></returns>
        public virtual string BaseSource()
        {
            return string.Empty;
        }

        public TemplateDefinitionItem()
        {
            this.Name = string.Empty;
        }

    }
}

