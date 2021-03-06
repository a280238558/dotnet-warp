using System.Collections.Generic;
using System.IO;
using System.Linq;
using DotnetWarp.CmdCommands.Options;
using DotnetWarp.Extensions;

namespace DotnetWarp.CmdCommands
{
    internal class DotnetCli : CmdCommand
    {
        private readonly string _projectPath;
        private readonly bool _isVerbose;

        private static readonly Dictionary<Platform.Value, string> PlatformToRid = new Dictionary<Platform.Value, string>
        {
            [Platform.Value.Windows] = "win-x64",
            [Platform.Value.Linux] = "linux-x64",
            [Platform.Value.MacOs] = "osx-x64"
        };
        
        public DotnetCli(string projectPath, bool isVerbose) : base("dotnet")
        {
            _projectPath = projectPath;
            _isVerbose = isVerbose;
        }

        public bool Publish(Context context, DotnetPublishOptions dotnetPublishOptions)
        {
            var argumentList = new List<string>
            {
                "publish",
                "-c Release",
                $"-r {dotnetPublishOptions.Rid ?? PlatformToRid[context.CurrentPlatform]}",
                $"-o {context.TempPublishPath.WithQuotes()}",
                "/p:ShowLinkerSizeComparison=true"
            };

            if (dotnetPublishOptions.IsNoRootApplicationAssemblies)
            {
                argumentList.Add("/p:RootAllApplicationAssemblies=false");    
            }
            
            if (dotnetPublishOptions.IsNoCrossGen)
            {
                argumentList.Add("/p:CrossGenDuringPublish=false");    
            }
            
            argumentList.Add($"{_projectPath.WithQuotes()}");

            var isCommandSuccessful = RunCommand(argumentList, _isVerbose);

            UpdateContext(context);

            return isCommandSuccessful;
        }

        public bool AddLinkerPackage()
        {
            var argumentList = new List<string>
            {
                "add",
                "package",
                "--source https://dotnet.myget.org/F/dotnet-core/api/v3/index.json ILLink.Tasks -v 0.1.5-preview-1841731"
            };

            return RunCommand(argumentList, _isVerbose);
        }

        public bool RemoveLinkerPackage()
        {
            var argumentList = new List<string>
            {
                "remove", 
                "package", 
                "ILLink.Tasks"
            };

            return RunCommand(argumentList, _isVerbose);
        }
        
        private void UpdateContext(Context context)
        {
            const string depsJsonExtension = ".deps.json";
            var depsJsonPath = Directory.EnumerateFiles(context.TempPublishPath, "*" + depsJsonExtension).Single();
            var depsJsonFilename = Path.GetFileName(depsJsonPath);
            context.AssemblyName = depsJsonFilename.Substring(0, depsJsonFilename.Length - depsJsonExtension.Length);
        }
    }
}
