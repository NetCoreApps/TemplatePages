using System.Collections.Generic;
using ServiceStack;
using ServiceStack.Templates;
using ServiceStack.IO;
using System.Threading.Tasks;
using System;
using ServiceStack.Web;
using ServiceStack.OrmLite;
using ServiceStack.Data;

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

    public class ReturnExceptionsInJsonAttribute : ResponseFilterAttribute
    {
        public override void Execute(IRequest req, IResponse res, object responseDto)
        {
            if (responseDto is Exception || responseDto is IHttpError)
                req.ResponseContentType = MimeTypes.Json;
        }
    }

    [ReturnExceptionsInJson]
    public class TemplateServices : Service
    {
        public async Task<string> Any(EvaluateTemplates request)
        {
            var context = new TemplateContext {
                TemplateFilters = {
                    new TemplateProtectedFilters(),
                },
                ExcludeFiltersNamed = { "fileWrite","fileAppend","fileDelete","dirDelete" }
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

            return await pageResult.RenderToStringAsync(); // render to string so [ReturnExceptionsInJson] can detect Exceptions and return JSON
        }

        public async Task<string> Any(EvaluateTemplate request)
        {
            var context = new TemplateContext {
                TemplateFilters = {
                    new TemplateDbFilters(),
                    new TemplateAutoQueryFilters(),
                    new TemplateServiceStackFilters(),
                    new CustomTemplateFilters(),
                }
            };
            //Register any dependencies filters need:
            context.Container.AddSingleton(() => base.GetResolver().TryResolve<IDbConnectionFactory>());
            context.Init();
            var pageResult = new PageResult(context.OneTimePage(request.Template)) 
            {
                Args = base.Request.GetTemplateRequestParams()
            };
            return await pageResult.RenderToStringAsync(); // render to string so [ReturnExceptionsInJson] can detect Exceptions and return JSON
        }
    }
    
}