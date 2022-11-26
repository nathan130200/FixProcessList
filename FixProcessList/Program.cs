using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace FixProcessList;

internal class Program
{
    static void Main(string[] args)
    {
        var path = Path.Combine(Directory.GetCurrentDirectory(), "rules.xml");

        if (!File.Exists(path))
            File.WriteAllText(path, "<rules />");
        else
        {
            var counter = 1;
            var rules = new List<ProcessRule>();

            using (var fs = new FileStream(path, FileMode.Open))
            {
                var element = XElement.Load(fs);

                if (element.Name != "Rules")
                    return;

                foreach (var child in element.Elements())
                {
                    var tagType = child.Name?.ToString();

                    var type = tagType switch
                    {
                        "set_priority" or "SetPriority" => RuleType.SetPriority,
                        "terminate" or "KillProcess" => RuleType.KillProcess,
                        _ => Enum.TryParse<RuleType>(tagType, out var tempType) ? tempType : RuleType.Unknown
                    };

                    if (type == RuleType.Unknown)
                        continue;

                    var name = child.Attribute("Name")?.Value ?? ($"#{counter}");

                    var attrKind = child.Attribute("Kind")?.Value;

                    var filter = attrKind switch
                    {
                        "by_name" or "by_name_exact" => FilterType.ByNameExact,
                        "by_name_match" or "by_name_pattern" or "by_pattern" => FilterType.ByNamePattern,
                        "first" or "single" => FilterType.First,
                        _ => Enum.TryParse<FilterType>(attrKind, out var tempFilter) ? tempFilter : FilterType.Unknown
                    };

                    if (filter == FilterType.Unknown)
                        continue;

                    counter++;
                    var arguments = new List<string>();

                    foreach (var argument in child.Elements("Param"))
                        arguments.Add(argument.Value);

                    rules.Add(new ProcessRule
                    {
                        Name = name,
                        Filter = filter,
                        Type = type,
                        Arguments = arguments
                    });
                }
            }

            foreach (var rule in rules)
            {
                var cusor = (left: 0, top: 0);

                try
                {
                    Console.Write("Execute rule: '{0}' ", rule.Name);
                    cusor = (Console.CursorLeft, Console.CursorTop);
                    Console.WriteLine("\n");

                    rule.Execute();
                    Console.WriteLine("\n");
                }
                catch (Exception ex)
                {
                    var oldCursor = (left: Console.CursorLeft, top: Console.CursorTop);
                    Console.SetCursorPosition(cusor.left, cusor.top);
                    Console.WriteLine("failed. " + ex);
                    Console.SetCursorPosition(oldCursor.left, oldCursor.top);
                }
            }
        }

#if DEBUG
        Console.ReadKey();
#endif
    }
}

public class ProcessRule
{
    public string Name { get; set; }
    public RuleType Type { get; set; }
    public List<string> Arguments { get; set; }
    public FilterType Filter { get; set; }

    public string GetParam(int index)
    {
        try
        {
            return Arguments[index];
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
            return string.Empty;
        }
    }

    static ProcessPriorityClass ParseProcessPriorityClass(string param)
    {
        if (int.TryParse(param, out int value))
            return (ProcessPriorityClass)value;
        else
            Enum.Parse<ProcessPriorityClass>(param, true);

        return ProcessPriorityClass.Idle;
    }

    public virtual void Execute()
    {
        var processes = BuildProcessList(Filter, GetParam(0));

        if (Type == RuleType.SetPriority)
        {
            var param = GetParam(1);

            var priority = param.ToLowerInvariant() switch
            {
                "low" => ProcessPriorityClass.BelowNormal,
                "lowest" => ProcessPriorityClass.Idle,
                "high" => ProcessPriorityClass.AboveNormal,
                "highest" => ProcessPriorityClass.RealTime,
                "normal" => ProcessPriorityClass.Normal,
                _ => ParseProcessPriorityClass(param)
            };

            foreach (var process in processes)
            {
                Console.WriteLine(" -- Set process pid={0,6}; priority={1}", process.Id, priority);
                process.PriorityClass = priority;
            }
        }
        else if (Type == RuleType.KillProcess)
        {
            var exceptions = new List<Exception>();

            foreach (var process in processes)
            {
                try
                {
                    var entireProcessTree = GetParam(1)?.ToLowerInvariant() switch
                    {
                        "1" or "true" => true,
                        "0" or "false" or _ => false
                    };

                    Console.WriteLine(" -- Terminate process: " + process.Id);

                    process.Kill(entireProcessTree);
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                }
            }

            if (exceptions.Any())
                throw new AggregateException(exceptions);
        }
    }

    static List<Process> BuildProcessList(FilterType type, string query)
    {
        List<Process> result = default;
        IEnumerable<Process> temp = default;

        if (type.HasFlag(FilterType.ByNameExact))
        {
            temp = Process.GetProcesses()
                .Where(x => x.ProcessName.Equals(query));

            if (type.HasFlag(FilterType.First))
            {
                if (result.Any())
                {
                    result = new();
                    result.Add(temp.FirstOrDefault());
                }
            }
            else
            {
                result = new(temp);
            }
        }
        else if (type.HasFlag(FilterType.ByNamePattern))
        {
            var pattern = new Regex(query, RegexOptions.IgnoreCase
                | RegexOptions.CultureInvariant
                | RegexOptions.ECMAScript);

            temp = Process.GetProcesses()
                .Where(x => pattern.IsMatch(x.ProcessName));
        }

        if (type.HasFlag(FilterType.First))
        {
            if (result.Any())
            {
                result = new()
                {
                    temp.FirstOrDefault()
                };
            }
        }
        else
        {
            result = new(temp);
        }

        return result;
    }
}

public enum RuleType
{
    Unknown,
    SetPriority,
    KillProcess
}

public enum FilterType
{
    Unknown = 0,
    ByNameExact = 1 << 0,
    ByNamePattern = 1 << 1,
    First = 1 << 2
}