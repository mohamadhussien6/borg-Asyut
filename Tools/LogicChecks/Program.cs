using System.Reflection;
using System.Text.Json;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

string root = Path.GetFullPath(args[0]);
var sources = new List<SyntaxTree>();
var options = new CSharpParseOptions(LanguageVersion.CSharp9);
// Only these two scalar Unity helpers are needed by the extracted difficulty rule.
// This adapter does not validate Unity's engine, rendering or physics.
sources.Add(CSharpSyntaxTree.ParseText(@"
internal static class Mathf {
    public static float Lerp(float a, float b, float t) => a + (b - a) * System.Math.Clamp(t, 0f, 1f);
    public static float InverseLerp(float a, float b, float value) => a == b ? 0f : System.Math.Clamp((value - a) / (b - a), 0f, 1f);
}", options));
var targets = new Dictionary<string, string[]> {
    ["PlayerController"] = new[] { "JumpTiming" }, ["ScoreManager"] = new[] { "HeightTracker", "FloorCombo" },
    ["PlatformSpawner"] = new[] { "Difficulty", "Placement", "Route" }, ["AudioManager"] = new[] { "VoiceCue", "VoicePolicy" }
};
foreach (var target in targets)
{
    string path = Directory.GetFiles(Path.Combine(root, "Assets/Ma3alem/Scripts"), target.Key + ".cs", SearchOption.AllDirectories).Single();
    var outer = CSharpSyntaxTree.ParseText(File.ReadAllText(path)).GetRoot().DescendantNodes()
        .OfType<ClassDeclarationSyntax>().First(type => type.Identifier.Text == target.Key);
    string members = string.Join("\n", outer.Members.OfType<BaseTypeDeclarationSyntax>()
        .Where(type => target.Value.Contains(type.Identifier.Text)).Select(type => type.ToFullString()));
    sources.Add(CSharpSyntaxTree.ParseText("using System; using System.Collections.Generic; namespace Ma3alem { public class " + target.Key + " { " + members + " } }", options));
}
foreach (string path in Directory.GetFiles(Path.Combine(root, "Assets/ThirdParty/RTLTMPro"), "*.cs", SearchOption.AllDirectories))
    sources.Add(CSharpSyntaxTree.ParseText(File.ReadAllText(path), options, path));
sources.Add(CSharpSyntaxTree.ParseText(File.ReadAllText(Path.Combine(root, "Assets/Ma3alem/Tests/EditMode/ContractCases.cs")), options));
var references = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")).Split(Path.PathSeparator)
    .Select(path => MetadataReference.CreateFromFile(path));
var compilation = CSharpCompilation.Create("ActualProductionRules", sources, references,
    new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
using var bytes = new MemoryStream();
var emitted = compilation.Emit(bytes);
if (!emitted.Success) { foreach (var error in emitted.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error)) Console.WriteLine(error); return 1; }
var assembly = Assembly.Load(bytes.ToArray());
var contracts = assembly.GetType("Ma3alem.Tests.ContractCases");
var results = new List<object>(); int failures = 0;
foreach (var method in contracts.GetMethods(BindingFlags.Public | BindingFlags.Static).Where(method => method.ReturnType == typeof(void)))
{
    try { method.Invoke(null, null); results.Add(new { name = method.Name, status = "PASS" }); Console.WriteLine("PASS " + method.Name); }
    catch (TargetInvocationException error) { failures++; results.Add(new { name = method.Name, status = "FAIL", error = error.InnerException.Message }); Console.WriteLine("FAIL " + method.Name + ": " + error.InnerException.Message); }
}
var strings = new HashSet<string>();
foreach (string path in Directory.GetFiles(Path.Combine(root, "Assets/Ma3alem/Scripts"), "*.cs", SearchOption.AllDirectories))
{
    var syntax = CSharpSyntaxTree.ParseText(File.ReadAllText(path));
    foreach (var literal in syntax.GetRoot().DescendantNodes().OfType<LiteralExpressionSyntax>())
        if (literal.Token.ValueText.Any(c => c >= '\u0600' && c <= '\u06FF')) strings.Add(literal.Token.ValueText);
    foreach (var expression in syntax.GetRoot().DescendantNodes().OfType<InterpolatedStringExpressionSyntax>())
    {
        string text = string.Join("", expression.Contents.Select(part => part is InterpolatedStringTextSyntax segment ? segment.TextToken.ValueText : "123"));
        if (text.Any(c => c >= '\u0600' && c <= '\u06FF')) strings.Add(text);
    }
}
var shaped = strings.Select(text => new { logical = text, visual = (string)contracts.GetMethod("Shape").Invoke(null, new object[] { text }) }).ToArray();
var json = new JsonSerializerOptions { WriteIndented = true, Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping };
File.WriteAllText(Path.Combine(root, "Documentation/ArabicShapingSamples.json"), JsonSerializer.Serialize(shaped, json) + "\n");
File.WriteAllText(Path.Combine(root, "Documentation/LogicValidation.json"), JsonSerializer.Serialize(new {
    status = failures == 0 ? "PASS" : "FAIL", runner = ".NET 10 console with Roslyn-extracted production nested rules and actual RTLTMPro source",
    scope = "Pure logic and shaping only; not Unity compilation, physics, UI rendering or audio playback",
    route_links_checked = 80000, pool_window_steps = 20000, results
}, json) + "\n");
Console.WriteLine($"{results.Count - failures} passed, {failures} failed; {shaped.Length} UI strings shaped.");
return failures == 0 ? 0 : 1;
