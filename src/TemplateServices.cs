using System.Collections.Generic;
using ServiceStack;
using ServiceStack.Templates;
using ServiceStack.IO;

namespace TemplatePages
{
    [Route("/pages/eval")]
    public class EvaluateTemplates : IReturn<string>
    {
        public Dictionary<string,string> Files { get; set; }
        public Dictionary<string,string> Args { get; set; }

        public string Page { get; set; }
    }

    [Route("/template/eval")]
    public class EvaluateTemplate
    {
        public string Template { get; set; }
    }

    public class TemplateServices : Service
    {
        public object Any(EvaluateTemplates request)
        {
            var context = new TemplateContext {
                TemplateFilters = {
                    new TemplateProtectedFilters()
                }
            }.Init();

            foreach (var entry in request.Files.Safe())
            {
                context.VirtualFiles.WriteFile(entry.Key, entry.Value);
            }

            var pageResult = new PageResult(context.GetPage(request.Page ?? "page"));

            foreach (var entry in request.Args.Safe())
            {
                pageResult.Args[entry.Key] = entry.Value;
            }

            return pageResult;
        }

        public object Any(EvaluateTemplate request)
        {
            var context = new TemplateContext().Init();

            var page = context.OneTimePage(request.Template);

            return new PageResult(page);
        }
    }
    
}