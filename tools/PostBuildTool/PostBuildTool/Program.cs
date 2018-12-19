// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml;
using System.Text.RegularExpressions;

namespace PostBuildTool
{
    class Program
    {
        enum BuildConfiguration
        {
            Debug,
            Debug_Android,
            Release
        };

        enum ResultCode : int
        {
            Success = 0,
            Error_InvalidBuildConfigArg = -1,
            Error_IncorrectArgsCount = -2,
            Error_InvalidOptionalArg = -3,
        }

        static BuildConfiguration _buildConfig;
        static string _workingDir = "";

        static void Main(string[] args)
        {
            ParseArgs(args);

            _workingDir = new Uri(Path.GetDirectoryName(Assembly.GetExecutingAssembly().CodeBase)).LocalPath;

            var filename = $"{_workingDir}\\MREUnityProjects.xml";

            if (!File.Exists(filename))
            {
                Console.WriteLine($"File not found: {filename}");
                Environment.Exit((int)ResultCode.Success);
            }

            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.Load(filename);

            var projectPathsNodes = xmlDoc.GetElementsByTagName("projectTargetDir");
            foreach (var node in projectPathsNodes)
            {
                CopyLibaries(((XmlNode)node).InnerText);
            }

            Environment.Exit((int)ResultCode.Success);
        }

        static void CopyLibaries(string targetDir)
        {
            var targetDirPath = Path.GetFullPath(targetDir);
            if (!Directory.Exists(targetDirPath))
            {
                Directory.CreateDirectory(targetDirPath);
            }

            var mwLibDir = $"{_workingDir}\\bin\\{_buildConfig.ToString()}";
			var re = new Regex(@"^MREUnityRuntime.*\.(?:pdb|dll|xml)$");
            var libFiles = Directory.GetFiles(mwLibDir).Where((file) => re.IsMatch(Path.GetFileName(file))).ToList();
            CopyFiles(libFiles.ToArray(), targetDirPath);
        }

        static void ParseArgs(string[] args)
        {
            if (args.Count() != 1)
            {
                Console.Error.WriteLine($"The PostBuildTool should be run as PostBuildTool (Debug | Debug_Android | Release)");
                Environment.Exit((int)ResultCode.Error_IncorrectArgsCount);
            }

            if (!Enum.TryParse(args[0], true, out _buildConfig))
            {
                Console.Error.WriteLine($"The value for the build configuration argument must appear first and be (Debug | Debug_Android | Release).  Value given was {args[1]}");
                Environment.Exit((int)ResultCode.Error_InvalidBuildConfigArg);
            }
        }

        static void CopyFiles(string sourceDir, string targetDir)
        {
            CopyFiles(Directory.GetFiles(sourceDir), targetDir);
        }

        static void CopyFiles(string[] files, string targetDir)
        {
            foreach (var file in files)
            {
                var fileName = Path.GetFileName(file);
                var destFile = Path.Combine(targetDir, fileName);
                File.Copy(file, destFile, true);
            }
        }
    }
}
