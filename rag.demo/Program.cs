using Microsoft.Extensions.Configuration;
using Microsoft.KernelMemory;
using System.Text.Json;

var configuration = new ConfigurationBuilder()
           .AddJsonFile("appsettings.json")
           .AddJsonFile("appsettings.Development.json", optional: true)
           .Build();

var outputDir = DateOnly.FromDateTime(DateTime.Now).ToString("o")+TimeOnly.FromDateTime(DateTime.Now).ToString("t").Replace(':', '-');
var outputDirectory = new DirectoryInfo(Path.Join(configuration["OutputDirectory"], outputDir));
if (!outputDirectory.Exists)
{
    outputDirectory.Create();
}

var inputDir = new DirectoryInfo(configuration["InputDirectory"]!);
if (!inputDir.Exists)
{
    inputDir.Create();
}

var inputFiles = Directory.EnumerateFiles(inputDir.FullName).ToList();

var promptText = File.ReadAllText("Prompt.txt");

MemoryServerless memory = new KernelMemoryBuilder()
     .WithOpenAI(
            new OpenAIConfig
            {
                TextModel = configuration["TextModel"]!,
                EmbeddingModel = configuration["EmbeddingModel"]!,
                APIKey = configuration["OPENAI_API_KEY"]!,
                OrgId = null
            })
    .Build<MemoryServerless>();

foreach (var file in inputFiles)
{
    try
    {
        // Import a file
        var fileInfo = new FileInfo(file);
        var documentId = await memory.ImportDocumentAsync(file);

        var answer = await memory.AskAsync(promptText, minRelevance: 0, filter: MemoryFilters.ByDocument(documentId));

        if (answer.NoResult)
        {
            await Console.Out.WriteLineAsync($"{fileInfo.Name}: {answer.NoResultReason}");
        }
        else
        {
            var result = answer.Result;
            await Console.Out.WriteLineAsync(result);

            await File.WriteAllTextAsync(Path.Join(outputDirectory.FullName, fileInfo.Name.Replace(fileInfo.Extension, ".json")), result);
        }
    }
    catch (Exception ex)
    {
        await Console.Out.WriteLineAsync(ex.ToString());
    }
    await Console.Out.WriteLineAsync(new string('-', Console.BufferWidth));
}