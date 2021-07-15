using System;
using System.Collections.Generic;
using System.Linq;
using Buildalyzer;
using Buildalyzer.Workspaces;
using Microsoft.Build.Definition;
using Microsoft.Build.Evaluation;
using Microsoft.Build.Evaluation.Context;
using Microsoft.Build.Execution;
using Microsoft.Build.Graph;
using Microsoft.CodeAnalysis;
using Spectre.Console;
using Project = Microsoft.Build.Evaluation.Project;

const ProjectLoadSettings defaultProjectLoadSettings = ProjectLoadSettings.IgnoreEmptyImports
                                                       | ProjectLoadSettings.IgnoreInvalidImports
                                                       | ProjectLoadSettings.IgnoreMissingImports
                                                       | ProjectLoadSettings.DoNotEvaluateElementsWithFalseCondition;
        
var sharedEvaluationContext = EvaluationContext.Create(EvaluationContext.SharingPolicy.Shared);

ProjectInstance CreateProjectInstance(string projectFullPath, Dictionary<string, string> globalProperties, ProjectCollection projectCollection)
{
    var projectInstance = Project.FromFile(
            projectFullPath,
            new ProjectOptions
            {
                EvaluationContext = sharedEvaluationContext,
                LoadSettings = defaultProjectLoadSettings,
                ProjectCollection = projectCollection,
            })
        .CreateProjectInstance(
            ProjectInstanceSettings.ImmutableWithFastItemLookup,
            sharedEvaluationContext);
            
    return projectInstance;
}

var graphEntryPoint = new ProjectGraphEntryPoint(
    "../../../../../SampleApp/ConsoleApp1/ConsoleApp1.csproj");

var pg = new ProjectGraph(new[] { graphEntryPoint }, new ProjectCollection(), CreateProjectInstance);
            
var tree = new Tree("[bold blue]Projects[/]");

static void AddProjectToTree(ProjectGraphNode pgn, IHasTreeNodes parentNode)
{
    var node = parentNode.AddNode(pgn.ProjectInstance.GetPropertyValue("MSBuildProjectName"));
    foreach (var childProjectGraphNode in pgn.ProjectReferences)
    {
        AddProjectToTree(childProjectGraphNode, node);
    }
}

var consoleApp = pg.EntryPointNodes.First();
AddProjectToTree(consoleApp, tree);

AnsiConsole.Render(tree);

var analyzerManager = new AnalyzerManager(null, new AnalyzerManagerOptions
{
    //LogWriter = Console.Out
});

var projectAnalyzer = analyzerManager.GetProject(consoleApp.ProjectInstance.FullPath);

var workspace = new AdhocWorkspace();

var analyzerResults = projectAnalyzer
    .Build();
            
analyzerResults
    .First()
    .AddToWorkspace(workspace, true);

var solution = workspace.CurrentSolution;

var comp = await solution.Projects.First().GetCompilationAsync();
var errors = comp.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error).ToList();

Console.WriteLine($"Error count: {errors.Count}");
errors.ForEach(e => Console.WriteLine(e.GetMessage()));

Console.WriteLine("ConsoleApp ProjectReferences:");
foreach (var projectReference in analyzerResults.First().ProjectReferences)
{
    Console.WriteLine(projectReference);
}