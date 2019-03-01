using System.Threading.Tasks;
using ServiceStack;
using ServiceStack.Templates;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Diagnostics;
using System;
using ServiceStack.Redis;
using ServiceStack.OrmLite;
using System.Reflection;
using ServiceStack.Script;

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

        Type GetFilterType(string name)
        {
            switch(name)
            {
                case nameof(TemplateDefaultFilters):
                    return typeof(TemplateDefaultFilters);
                case nameof(TemplateHtmlFilters):
                    return typeof(TemplateHtmlFilters);
                case nameof(TemplateProtectedFilters):
                    return typeof(TemplateProtectedFilters);
                case nameof(TemplateInfoFilters):
                    return typeof(TemplateInfoFilters);
                case nameof(TemplateRedisFilters):
                    return typeof(TemplateRedisFilters);
                case nameof(TemplateDbFilters):
                    return typeof(TemplateDbFilters);
                case nameof(TemplateDbFiltersAsync):
                    return typeof(TemplateDbFiltersAsync);
                case nameof(TemplateServiceStackFilters):
                    return typeof(TemplateServiceStackFilters);
                case nameof(TemplateAutoQueryFilters):
                    return typeof(TemplateAutoQueryFilters);
            }

            throw new NotSupportedException("Unknown Filter: " + name);
        }

        public IRawString filterLinkToSrc(string name)
        {
            const string prefix = "https://github.com/ServiceStack/ServiceStack/tree/pre-rebrand/src/ServiceStack.Common/Templates/Filters/";

            var type = GetFilterType(name);
            var url = type == typeof(TemplateDefaultFilters)
                ? prefix
                : type == typeof(TemplateHtmlFilters) || type == typeof(TemplateProtectedFilters)
                    ? $"{prefix}{type.Name}.cs"
                    : type == typeof(TemplateInfoFilters)
                    ? "https://github.com/ServiceStack/ServiceStack/blob/pre-rebrand/src/ServiceStack/TemplateInfoFilters.cs"
                    : type == typeof(TemplateRedisFilters)
                    ? "https://github.com/ServiceStack/ServiceStack.Redis/blob/17358b19d5d311f0d6dbf6f3f7e5cfc44d8e6a54/src/ServiceStack.Redis/TemplateRedisFilters.cs"
                    : type == typeof(TemplateDbFilters) || type == typeof(TemplateDbFiltersAsync)
                    ? $"https://github.com/ServiceStack/ServiceStack.OrmLite/blob/8b9c78c8bbe01e33ab468beb4e3a9fb8cc57117b/src/ServiceStack.OrmLite/{type.Name}.cs"
                    : type == typeof(TemplateServiceStackFilters)
                    ? "https://github.com/ServiceStack/ServiceStack/blob/pre-rebrand/src/ServiceStack/TemplateServiceStackFilters.cs"
                    : prefix;

            return new RawString($"<a href='{url}'>{type.Name}.cs</a>");
        }

        public FilterInfo[] filtersAvailable(string name)
        {
            var filterType = GetFilterType(name);
            var filters = filterType.GetMethods(BindingFlags.Instance | BindingFlags.Public);
            var to = filters
                .OrderBy(x => x.Name)
                .ThenBy(x => x.GetParameters().Count())
                .Where(x => x.DeclaringType != typeof(TemplateFilter) && x.DeclaringType != typeof(object))
                .Where(m => !m.IsSpecialName)                
                .Select(x => FilterInfo.Create(x));

            return to.ToArray();
        }
    }

    public class FilterInfo
    {
        public string Name { get; set; }
        public string FirstParam { get; set; }
        public string ReturnType { get; set; }
        public int ParamCount { get; set; }
        public string[] RemainingParams { get; set; }

        public static FilterInfo Create(MethodInfo mi)
        {
            var paramNames = mi.GetParameters()
                .Where(x => x.ParameterType != typeof(TemplateScopeContext))
                .Select(x => x.Name)
                .ToArray();

            var to = new FilterInfo {
                Name = mi.Name,
                FirstParam = paramNames.FirstOrDefault(),
                ParamCount = paramNames.Length,
                RemainingParams = paramNames.Length > 1 ? paramNames.Skip(1).ToArray() : new string[]{},
                ReturnType = mi.ReturnType?.Name,
            };

            return to;
        }

        public string Return => ReturnType != null && ReturnType != nameof(StopExecution) ? " -> " + ReturnType : "";

        public string Body => ParamCount == 0
            ? $"{Name}"
            : ParamCount == 1
                ? $"| {Name}"
                : $"| {Name}(" + string.Join(", ", RemainingParams) + $")";

        public string Display => ParamCount == 0
            ? $"{Name}{Return}"
            : ParamCount == 1
                ? $"{FirstParam} | {Name}{Return}"
                : $"{FirstParam} | {Name}(" + string.Join(", ", RemainingParams) + $"){Return}";
    }
}
