using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Collections.Generic;
using ServiceStack;
using ServiceStack.IO;
using ServiceStack.Text;
using ServiceStack.Templates;

namespace TemplatePages
{
    public class GitHubMarkdownFilters : TemplateFilter
    {
        public string ApiBaseUrl { get; set; } = "https://api.github.com";

        public bool UseMemoryCache { get; set; } = true;

        private static readonly MarkdownSharp.Markdown md = new MarkdownSharp.Markdown();

        public IRawString markdown(TemplateScopeContext scope, string markdown)
        {
             var html = md.Transform(markdown);
             return html.ToRawString();
        }

        public async Task githubMarkdown(TemplateScopeContext scope, string markdownPath) 
        {
            var file = Context.ProtectedFilters.ResolveFile(nameof(githubMarkdown), scope, markdownPath);
            var htmlFilePath = file.VirtualPath.LastLeftPart('.') + ".html";
            var cacheKey = nameof(GitHubMarkdownFilters) + ">" + htmlFilePath;

            var htmlFile = Context.VirtualFiles.GetFile(htmlFilePath);
            if (htmlFile != null && htmlFile.LastModified >= file.LastModified)
            {
                if (UseMemoryCache)
                {
                    byte[] bytes;
                    if (!Context.Cache.TryGetValue(cacheKey, out object oBytes))
                    {
                        using (var stream = htmlFile.OpenRead())
                        {
                            var ms = MemoryStreamFactory.GetStream();
                            using (ms)
                            {
                                await stream.CopyToAsync(ms);
                                ms.Position = 0;
                                bytes = ms.ToArray();
                                Context.Cache[cacheKey] = bytes;
                            }
                        }
                    }
                    else
                    {
                        bytes = (byte[])oBytes;
                    }
                    scope.OutputStream.Write(bytes, 0, bytes.Length);                    
                }
                else
                {
                    using (var htmlReader = htmlFile.OpenRead())
                    {
                        await htmlReader.CopyToAsync(scope.OutputStream);
                    }
                }
            }
            else
            {
                var ms = MemoryStreamFactory.GetStream();
                using (ms)
                {
                    using (var stream = file.OpenRead())
                    {
                        await stream.CopyToAsync(ms);
                    }

                    ms.Position = 0;
                    var bytes = ms.ToArray();
                    var htmlBytes = await ApiBaseUrl.CombineWith("markdown", "raw")
                        .PostBytesToUrlAsync(bytes, contentType:MimeTypes.PlainText, requestFilter:x => x.UserAgent = "TemplatePages");

                    var headerBytes = "<div class=\"gfm\">".ToUtf8Bytes();
                    var footerBytes = "</div>".ToUtf8Bytes();
                    
                    var wrappedBytes = new byte[headerBytes.Length + htmlBytes.Length + footerBytes.Length];
                    System.Buffer.BlockCopy(headerBytes, 0, wrappedBytes, 0, headerBytes.Length);
                    System.Buffer.BlockCopy(htmlBytes, 0, wrappedBytes, headerBytes.Length, htmlBytes.Length);
                    System.Buffer.BlockCopy(footerBytes, 0, wrappedBytes, headerBytes.Length + htmlBytes.Length, footerBytes.Length);

                    if (Context.VirtualFiles is IVirtualFiles vfs)
                    {
                        vfs.WriteFile(htmlFilePath, wrappedBytes);
                    }

                    if (UseMemoryCache)
                    {
                        Context.Cache[cacheKey] = wrappedBytes;
                    }
                    await scope.OutputStream.WriteAsync(wrappedBytes, 0, wrappedBytes.Length);
                }
            }
        }
    }
}
