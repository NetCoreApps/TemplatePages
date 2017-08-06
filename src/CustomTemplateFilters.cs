using System.Threading.Tasks;
using ServiceStack;
using ServiceStack.Templates;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Diagnostics;

namespace TemplatePages
{
    public class CustomTemplateFilters : TemplateFilter
    {
        public Dictionary<int, KeyValuePair<string, string>> DocsIndex { get; } = new Dictionary<int, KeyValuePair<string, string>>();
        public Dictionary<int, KeyValuePair<string, string>> LinqIndex { get; } = new Dictionary<int, KeyValuePair<string, string>>();
        public Dictionary<int, KeyValuePair<string, string>> UseCasesIndex { get; } = new Dictionary<int, KeyValuePair<string, string>>();

        public object prevDocLink(int order)
        {
            if (DocsIndex.TryGetValue(order - 1, out KeyValuePair<string,string> entry))
                return entry;
            return null;
        }

        public object nextDocLink(int order)
        {
            if (DocsIndex.TryGetValue(order + 1, out KeyValuePair<string,string> entry))
                return entry;
            return null;
        }

        public object prevLinqLink(int order)
        {
            if (LinqIndex.TryGetValue(order - 1, out KeyValuePair<string,string> entry))
                return entry;
            return null;
        }

        public object nextLinqLink(int order)
        {
            if (LinqIndex.TryGetValue(order + 1, out KeyValuePair<string,string> entry))
                return entry;
            return null;
        }

        public object prevUseCaseLink(int order)
        {
            if (UseCasesIndex.TryGetValue(order - 1, out KeyValuePair<string,string> entry))
                return entry;
            return null;
        }

        public object nextUseCaseLink(int order)
        {
            if (UseCasesIndex.TryGetValue(order + 1, out KeyValuePair<string,string> entry))
                return entry;
            return null;
        }

        List<KeyValuePair<string,string>> sortedDocLinks;
        public object docLinks() => sortedDocLinks ?? (sortedDocLinks = sortLinks(DocsIndex));

        List<KeyValuePair<string,string>> sortedLinqLinks;
        public object linqLinks() => sortedLinqLinks ?? (sortedLinqLinks = sortLinks(LinqIndex));

        List<KeyValuePair<string,string>> sorteUseCaseLinks;
        public object useCaseLinks() => sorteUseCaseLinks ?? (sorteUseCaseLinks = sortLinks(UseCasesIndex));

        public List<KeyValuePair<string,string>> sortLinks(Dictionary<int, KeyValuePair<string,string>> links)
        {
            var sortedKeys = links.Keys.ToList();
            sortedKeys.Sort();

            var to = new List<KeyValuePair<string,string>>();

            foreach (var key in sortedKeys)
            {
                var entry = links[key];
                to.Add(entry);
            }

            return to;
        }

        public async Task includeContentFile(TemplateScopeContext scope, string virtualPath)
        {
            var file = HostContext.VirtualFiles.GetFile(virtualPath);
            if (file == null)
                throw new FileNotFoundException($"includeContentFile '{virtualPath}' was not found");

            using (var reader = file.OpenRead())
            {
                await reader.CopyToAsync(scope.OutputStream);
            }
        }

        public List<Customer> customers() => TemplateQueryData.Customers;

        public Process[] processes => Process.GetProcesses();
        public Process[] processesByName(string name) => Process.GetProcessesByName(name);
        public Process processById(int processId) => Process.GetProcessById(processId);
        public Process currentProcess() => Process.GetCurrentProcess();
    }
}
