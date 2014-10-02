using System;
using System.IO;
using System.Reflection;
using System.Text;
using CODE.Framework.Core.Utilities;
using CODE.Framework.Core.Utilities.Extensions;

namespace BuildPackager
{
    class Program
    {
        static void Main(string[] args)
        {
            var configuration = string.Empty;
            if (args.Length > 0)
                configuration = args[0];
            if (!string.IsNullOrEmpty(configuration) && configuration.ToLower() != "release") return; // we only do automated builds packages in release mode

            var version = Assembly.GetEntryAssembly().FullName;
            var at = version.IndexOf("Version=", StringComparison.Ordinal);
            version = version.Substring(at + 8);
            at = version.IndexOf(",", StringComparison.Ordinal);
            version = version.Substring(0, at);

            var solutionFolder = Directory.GetCurrentDirectory();
            var packagerIndex = solutionFolder.IndexOf("\\BuildPackager", StringComparison.Ordinal);
            if (packagerIndex > -1) solutionFolder = solutionFolder.Substring(0, packagerIndex);


            //if (!string.IsNullOrEmpty(configuration))
            //    ProcessBuildConfiguration(configuration, solutionFolder, version);
            //else
            //{
            ProcessBuildConfiguration("Debug", solutionFolder, version);
            ProcessBuildConfiguration("Release", solutionFolder, version);
            //}

            PackageSource(solutionFolder, version);
            PackageWpfThemeResources(solutionFolder, version);

            if (args.Length < 1)
            {
                Console.WriteLine("Done.");
                Console.ReadLine();
            }
        }

        private static void PackageWpfThemeResources(string solutionFolder, string version)
        {
            Console.WriteLine(string.Empty);
            Console.WriteLine("Packaging theme resources ZIP file.");

            var outputFolder = solutionFolder + @"\Build\" + version + @"\";
            var zipFileName = outputFolder + @"\CODE.Framework.WPFThemeResources." + version + ".zip";
            if (File.Exists(zipFileName)) File.Delete(zipFileName);
            var zip = new ZipFile(zipFileName);

            var folders = Directory.GetDirectories(solutionFolder, "CODE.Framework.Wpf.Theme.*");
            foreach (var folder in folders)
            {
                var parts = folder.Split('.');
                var themeName = parts[parts.Length - 1];
                var files = Directory.GetFiles(folder, "*.xaml");
                foreach (var file in files)
                {
                    var fileInfo = new FileInfo(file);
                    AddFileToZip(zip, fileInfo, themeName + @"\" + fileInfo.Name);
                }
            }
            zip.Save();
        }

        private static void PackageSource(string solutionFolder, string version)
        {
            Console.WriteLine(string.Empty);
            Console.WriteLine("Packaging source ZIP file.");

            var outputFolder = solutionFolder + @"\Build\" + version + @"\";
            var zipFileName = outputFolder + @"\CODE.Framework.Source." + version + ".zip";
            if (File.Exists(zipFileName)) File.Delete(zipFileName);
            var zip = new ZipFile(zipFileName);

            var files = Directory.GetFiles(solutionFolder);
            foreach (var file in files)
            {
                var fileInfo = new FileInfo(file);
                var fileName = fileInfo.Name.ToLower();
                if (!fileName.EndsWith(".user") && !fileName.EndsWith(".suo") && !fileName.EndsWith(".vssscc") && !fileName.EndsWith(".vspscc") &&
                    !fileName.EndsWith(".docstates") && !fileName.ToLower().EndsWith(".ghostdoc.xml") && !fileName.EndsWith(".chw") && !fileName.ToLower().StartsWith("pingme.txt"))
                {
                    Console.WriteLine("Adding file: " + file);

                    switch (fileInfo.Extension.ToLower())
                    {
                        case ".sln":
                            var slnContents = GetStrippedSln(fileInfo.FullName);
                            AddFileToZip(zip, slnContents, fileInfo.Name);
                            break;
                        case ".csproj":
                            var csprojContents = GetStrippedCsProj(fileInfo.FullName);
                            AddFileToZip(zip, csprojContents, fileInfo.Name);
                            break;
                        default:
                            AddFileToZip(zip, fileInfo, fileInfo.Name);
                            break;
                    }
                }
            }

            var folders = Directory.GetDirectories(solutionFolder);
            foreach (var folder in folders)
            {
                var folderName = folder.JustFileName().ToLower();
                if (!folderName.StartsWith("_resharper") && folderName != "build" && !folderName.EndsWith(".pdb") && folderName != "bin" && folderName != "obj")
                {
                    Console.WriteLine("Processing folder: " + folder);
                    ProcessFolder(folder, solutionFolder, zip);
                }
            }

            zip.Save();
        }

        public static string GetStrippedCsProj(string file)
        {
            var output = new StringBuilder();

            using (var sr = new StreamReader(file))
                while (sr.Peek() >= 0)
                {
                    var line = sr.ReadLine();

                    if (line == null) continue;
                    if (line.Contains("SccProjectName")) continue;
                    if (line.Contains("SccLocalPath")) continue;
                    if (line.Contains("SccAuxPath")) continue;
                    if (line.Contains("SccProvider")) continue;

                    output.Append(line + "\r\n");
                }

            return output.ToString();
        }

        public static string GetStrippedSln(string file)
        {
            var output = new StringBuilder();
            var ignore = false;

            using (var sr = new StreamReader(file))
                while (sr.Peek() >= 0)
                {
                    var line = sr.ReadLine();
                    if (line != null && line.Contains("GlobalSection(TeamFoundationVersionControl)"))
                        ignore = true;

                    if (!ignore)
                        output.Append(line + "\r\n");
                    else if (line != null && line.Contains("EndGlobalSection"))
                        ignore = false;
                }
            return output.ToString();
        }

        private static void ProcessFolder(string folder, string solutionFolder, ZipFile zip)
        {
            var files = Directory.GetFiles(folder);
            foreach (var file in files)
            {
                Console.WriteLine("Processing file:   " + file);
                //if (file == @"d:\Projects\CODE.Framework\Framework\DotNet\CODE.Framework\_SharedFiles\Properties\AssemblyInfoShared.cs")
                //    Debugger.Break();

                var fileInfo = new FileInfo(file);
                var fileName = file.Replace(solutionFolder + @"\", string.Empty);
                if (!fileName.EndsWith(".user") && !fileName.EndsWith(".suo") && !fileName.EndsWith(".vssscc") && !fileName.EndsWith(".vspscc") &&
                    !fileName.EndsWith(".docstates") && !fileName.ToLower().EndsWith(".ghostdoc.xml") && !fileName.EndsWith(".chw") && !fileName.ToLower().StartsWith("pingme.txt"))
                    switch (fileInfo.Extension.ToLower())
                    {
                        case ".sln":
                            var slnContents = GetStrippedSln(fileInfo.FullName);
                            AddFileToZip(zip, slnContents, fileName);
                            break;
                        case ".csproj":
                            var csprojContents = GetStrippedCsProj(fileInfo.FullName);
                            AddFileToZip(zip, csprojContents, fileName);
                            break;
                        default:
                            AddFileToZip(zip, fileInfo, fileName);
                            break;
                    }
            }

            var folders = Directory.GetDirectories(folder);
            foreach (var folder2 in folders)
            {
                var folderName = folder2.JustFileName().ToLower();
                if (!folderName.StartsWith("_resharper") && folderName != "build" && !folderName.EndsWith(".pdb") && folderName != "bin" && folderName != "obj")
                {
                    Console.WriteLine("Processing folder: " + folder2);
                    ProcessFolder(folder2, solutionFolder, zip);
                }
            }
        }

        private static void ProcessBuildConfiguration(string configuration, string solutionFolder, string version)
        {
            try
            {
                Console.WriteLine(string.Empty);
                Console.WriteLine("Processing CODE.Framework build in " + configuration + " configuration.");

                var outputFolder = solutionFolder + @"\Build\" + version + @"\" + configuration;
                var counter = 0;
                while (Directory.Exists(outputFolder))
                {
                    counter++;
                    try
                    {
                        Directory.Delete(outputFolder, true);
                    }
                    catch
                    {
                        if (counter > 10) throw;
                    }
                }
                Directory.CreateDirectory(outputFolder);

                CopyAssemblies(configuration, solutionFolder, outputFolder);
                CreateZipFile(configuration, outputFolder, version);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception: " + ex);
                Console.ReadLine();
            }
        }
        private static void CopyAssemblies(string configuration, string solutionFolder, string outputFolder)
        {
            var folders = Directory.GetDirectories(solutionFolder);
            foreach (var folder in folders)
                if (folder.IndexOf(@"\CODE.Framework.", StringComparison.Ordinal) > -1)
                {
                    Console.WriteLine("Processing folder: " + folder);
                    var binFolder = folder + @"\bin\" + configuration;
                    if (Directory.Exists(binFolder))
                    {
                        ProcessFilesInFolder(binFolder, "dll", outputFolder);
                        ProcessFilesInFolder(binFolder, "xml", outputFolder);
                        ProcessFilesInFolder(binFolder, "pdb", outputFolder);
                        ProcessFilesInFolder(binFolder, "exe", outputFolder);
                    }
                }
        }

        private static void ProcessFilesInFolder(string folder, string extension, string outputFolder)
        {
            var files = Directory.GetFiles(folder, "*." + extension);
            foreach (var file in files)
            {
                Console.WriteLine("Copying file: " + file);
                var parts = file.Split('\\');
                var outputFileName = outputFolder + "\\" + parts[parts.Length - 1];
                if (!File.Exists(outputFileName) && !(outputFileName.IndexOf(".TestBench.", StringComparison.Ordinal) > -1))
                    File.Copy(file, outputFileName);
            }
        }

        private static void CreateZipFile(string configuration, string outputFolder, string version)
        {
            Console.WriteLine(string.Empty);
            Console.WriteLine("Packaging assembly ZIP file in " + configuration + " configuration.");
            var zipFileName = outputFolder.Replace("\\" + configuration, string.Empty) + @"\CODE.Framework." + configuration + "." + version + ".zip";
            if (File.Exists(zipFileName)) File.Delete(zipFileName);
            var zip = new ZipFile(zipFileName);

            var files = Directory.GetFiles(outputFolder);
            foreach (var file in files)
            {
                var fileInfo = new FileInfo(file);
                AddFileToZip(zip, fileInfo, fileInfo.Name);
            }
            zip.Save();
        }

        private static void AddFileToZip(ZipFile zip, FileInfo fileInfo, string fileName)
        {
            using (var stream = fileInfo.OpenRead())
            {
                var bytes = new byte[fileInfo.Length];
                stream.Read(bytes, 0, (int)fileInfo.Length);
                zip.AddBytes(bytes, fileName);
                stream.Close();
            }
        }

        private static void AddFileToZip(ZipFile zip, string contents, string fileName)
        {
            var bytes = Encoding.ASCII.GetBytes(contents);
            zip.AddBytes(bytes, fileName);
        }
    }
}
