using System;
using System.Linq;
using System.IO;
using System.Diagnostics;
using System.Collections.Generic;
using ServiceStack;
using ServiceStack.VirtualPath;
using ServiceStack.Templates;

namespace TemplatePages
{
    [Route("/introspect/state")]
    public class IntrospectState 
    {
        public string Page { get; set; }
        public string ProcessInfo { get; set; }
        public string DriveInfo { get; set; }
    }

    public class StateTemplateFilters : TemplateFilter
    {
        bool HasAccess(Process process)
        {
            try { return process.TotalProcessorTime >= TimeSpan.Zero; } 
            catch (Exception) { return false; }
        }

        public IEnumerable<Process> processes() => Process.GetProcesses().Where(HasAccess);
        public Process processById(int processId) => Process.GetProcessById(processId);
        public Process currentProcess() => Process.GetCurrentProcess();
        public DriveInfo[] drives() => DriveInfo.GetDrives();
    }

    public class IntrospectStateServices : Service
    {
        public object Any(IntrospectState request)
        {
            var context = new TemplateContext {
                ScanTypes = { typeof(StateTemplateFilters) }, //Autowires (if needed)
                RenderExpressionExceptions = true
            }.Init();

            context.VirtualFiles.WriteFile("page.html", request.Page);
            context.VirtualFiles.WriteFile("process-info.html", request.ProcessInfo);
            context.VirtualFiles.WriteFile("drive-info.html", request.DriveInfo);

            return new PageResult(context.GetPage("page"));
        }
    }
}