using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace Crm.UnitTests.Architecture;

// The architecture rules of the project, checked against the real project files and sources
public class ArchitectureRulesTests
{
    private static readonly string Backend = FindBackend();

    private static string FindBackend()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "CrmSystem.slnx")))
            dir = dir.Parent;

        return dir?.FullName ?? throw new InvalidOperationException("CrmSystem.slnx not found");
    }

    private sealed record Proj(string Name, string Module, string Layer, string Path, IReadOnlyList<string> References);

    private static readonly IReadOnlyList<Proj> ModuleProjects = LoadModuleProjects();

    private static List<Proj> LoadModuleProjects()
    {
        var result = new List<Proj>();

        foreach (var file in Directory.GetFiles(Path.Combine(Backend, "src", "Modules"), "*.csproj", SearchOption.AllDirectories))
        {
            var name = Path.GetFileNameWithoutExtension(file);
            var module = name[..name.IndexOf('.')];
            var layer = name[(module.Length + 1)..];
            var references = XDocument.Load(file).Descendants("ProjectReference")
                .Select(r => Path.GetFileNameWithoutExtension(((string)r.Attribute("Include")!).Replace('\\', '/')))
                .ToList();

            result.Add(new Proj(name, module, layer, file, references));
        }

        return result;
    }

    private static IEnumerable<string> SourceFiles(string subPath = "src") =>
        Directory.GetFiles(Path.Combine(Backend, subPath), "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")
                        && !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}")
                        && !f.Contains($"{Path.DirectorySeparatorChar}Migrations{Path.DirectorySeparatorChar}"));

    [Fact]
    public void The_scan_finds_the_whole_solution()
    {
        // Identity, Students, Teachers, Courses, Enrollments, Scheduling, Billing, Notifications, Materials and Reporting
        Assert.Equal(10, ModuleProjects.Select(p => p.Module).Distinct().Count());
    }

    [Fact]
    public void Modules_only_know_each_other_through_contracts()
    {
        var violations = new List<string>();

        foreach (var project in ModuleProjects)
            foreach (var reference in project.References)
            {
                var referencedModule = reference.Contains('.') ? reference[..reference.IndexOf('.')] : reference;
                var isOtherModule = ModuleProjects.Any(p => p.Module == referencedModule) && referencedModule != project.Module;

                if (isOtherModule && !reference.EndsWith(".Contracts", StringComparison.Ordinal))
                    violations.Add($"{project.Name} -> {reference}");
            }

        Assert.Empty(violations);
    }

    [Fact]
    public void Contracts_projects_depend_on_nothing()
    {
        Assert.All(ModuleProjects.Where(p => p.Layer == "Contracts"), p => Assert.Empty(p.References));
    }

    [Fact]
    public void Domain_projects_depend_only_on_the_kernel_and_contracts()
    {
        foreach (var domain in ModuleProjects.Where(p => p.Layer == "Domain"))
            Assert.All(domain.References, r =>
                Assert.True(r == "Shared.Kernel" || r.EndsWith(".Contracts", StringComparison.Ordinal), $"{domain.Name} -> {r}"));
    }

    [Fact]
    public void Application_never_references_infrastructure_and_infrastructure_stays_inside_its_module()
    {
        foreach (var application in ModuleProjects.Where(p => p.Layer == "Application"))
            Assert.DoesNotContain(application.References, r => r.Contains("Infrastructure"));

        foreach (var infrastructure in ModuleProjects.Where(p => p.Layer.StartsWith("Infrastructure")))
            Assert.All(infrastructure.References, r =>
                Assert.True(r.StartsWith(infrastructure.Module + ".") || r.StartsWith("Shared."), $"{infrastructure.Name} -> {r}"));
    }

    [Fact]
    public void The_shared_kernel_is_technical_only_and_has_no_dependencies()
    {
        var kernel = XDocument.Load(Path.Combine(Backend, "src", "Shared", "Shared.Kernel", "Shared.Kernel.csproj"));

        Assert.Empty(kernel.Descendants("ProjectReference"));
        Assert.Empty(kernel.Descendants("PackageReference"));

        // business words do not belong in it
        var text = string.Join("\n", Directory.GetFiles(Path.Combine(Backend, "src", "Shared", "Shared.Kernel"), "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains("/obj/")).Select(File.ReadAllText));
        Assert.DoesNotMatch(new Regex(@"\b(Student|Teacher|Course|Enrollment|Invoice|Payroll|Lesson)\b"), text);
    }

    [Fact]
    public void Every_module_unit_of_work_extends_the_base_one_and_the_base_one_is_never_registered_directly()
    {
        var sources = SourceFiles().ToList();

        var moduleUnits = sources.SelectMany(f => Regex.Matches(File.ReadAllText(f), @"interface\s+(I\w+UnitOfWork)\s*:\s*IUnitOfWork").Select(m => m.Groups[1].Value)).ToList();
        Assert.Equal(9, moduleUnits.Count);

        var direct = sources.Where(f => Regex.IsMatch(File.ReadAllText(f), @"AddScoped<\s*IUnitOfWork\b|AddTransient<\s*IUnitOfWork\b|AddSingleton<\s*IUnitOfWork\b")).ToList();
        Assert.Empty(direct);
    }

    [Fact]
    public void Every_database_has_its_own_schema_and_no_seed_data_lives_in_migrations_or_models()
    {
        var schemas = SourceFiles().Where(f => f.EndsWith("DbContext.cs"))
            .Select(f => Regex.Match(File.ReadAllText(f), "HasDefaultSchema\\(\"(\\w+)\"\\)").Groups[1].Value)
            .Where(s => s.Length > 0)
            .ToList();

        Assert.Equal(9, schemas.Count);
        Assert.Equal(schemas.Count, schemas.Distinct().Count());

        var allSources = Directory.GetFiles(Path.Combine(Backend, "src"), "*.cs", SearchOption.AllDirectories).Where(f => !f.Contains("/obj/") && !f.Contains("/bin/"));
        Assert.DoesNotContain(allSources, f => File.ReadAllText(f).Contains(".HasData(") || File.ReadAllText(f).Contains("InsertData("));
    }

    [Fact]
    public void Every_module_database_is_registered_through_the_shared_transaction_aware_helper()
    {
        // the helper itself lives in Shared.Infrastructure; the modules must only use it
        var registrations = SourceFiles()
            .Where(f => f.Contains($"{Path.DirectorySeparatorChar}Modules{Path.DirectorySeparatorChar}") && f.EndsWith("InfrastructureExtensions.cs"))
            .Select(File.ReadAllText)
            .Where(r => r.Contains("DbContext"))
            .ToList();

        Assert.DoesNotContain(registrations, r => Regex.IsMatch(r, @"\.AddDbContext<"));
        Assert.Equal(9, registrations.Count(r => r.Contains("AddModuleDbContext<")));
    }

    [Fact]
    public void Every_collection_of_an_entity_is_read_only_with_a_backing_field_mapped_in_the_configuration()
    {
        var problems = new List<string>();

        foreach (var entityFile in SourceFiles().Where(f => f.Contains("Domain") && f.Contains($"{Path.DirectorySeparatorChar}Entities{Path.DirectorySeparatorChar}")))
        {
            var text = File.ReadAllText(entityFile);

            foreach (Match property in Regex.Matches(text, @"public\s+(?:IReadOnlyCollection|IReadOnlyList|ICollection|IList|List)<\w+>\s+(\w+)"))
            {
                var name = property.Groups[1].Value;
                var module = Regex.Match(entityFile, @"Modules[/\\](\w+)[/\\]").Groups[1].Value;

                if (!Regex.IsMatch(text, $@"IReadOnlyCollection<\w+>\s+{name}\s*=>\s*_\w+\.AsReadOnly\(\)"))
                    problems.Add($"{Path.GetFileName(entityFile)}.{name} is not a read-only view of a private list");

                var configuration = string.Join("\n", Directory.GetFiles(Path.Combine(Backend, "src", "Modules", module), "*Configuration.cs", SearchOption.AllDirectories).Select(File.ReadAllText));
                if (!Regex.IsMatch(configuration, $@"Navigation\(\w+\s*=>\s*\w+\.{name}\)\s*\.HasField\(""_\w+""\)\s*\.UsePropertyAccessMode\(PropertyAccessMode\.Field\)"))
                    problems.Add($"{Path.GetFileName(entityFile)}.{name} has no HasField/UsePropertyAccessMode in its configuration");
            }
        }

        Assert.Empty(problems);
    }

    [Fact]
    public void Entities_have_private_setters_and_a_private_constructor_for_the_ORM()
    {
        var problems = new List<string>();

        foreach (var file in SourceFiles().Where(f => f.Contains($"{Path.DirectorySeparatorChar}Entities{Path.DirectorySeparatorChar}") && f.Contains("Domain")))
        {
            var text = File.ReadAllText(file);

            foreach (Match setter in Regex.Matches(text, @"public\s+[\w<>?,\s]+\s+(\w+)\s*\{\s*get;\s*set;\s*\}"))
                problems.Add($"{Path.GetFileName(file)}: public setter on {setter.Groups[1].Value}");

            var className = Regex.Match(text, @"class\s+(\w+)").Groups[1].Value;
            if (!Regex.IsMatch(text, $@"private\s+{className}\(\)"))
                problems.Add($"{Path.GetFileName(file)}: no private constructor");
        }

        Assert.Empty(problems);
    }

    [Fact]
    public void Each_module_has_the_four_layer_projects_it_needs()
    {
        foreach (var module in ModuleProjects.Select(p => p.Module).Distinct().Where(m => m != "Reporting"))
        {
            var layers = ModuleProjects.Where(p => p.Module == module).Select(p => p.Layer).ToList();

            Assert.Contains("Domain", layers);
            Assert.Contains("Application", layers);
            Assert.Contains(layers, l => l.StartsWith("Infrastructure"));
        }
    }
}
