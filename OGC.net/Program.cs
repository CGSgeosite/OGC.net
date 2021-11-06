using System;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Xml.Linq;
using Geosite.Messager;

namespace Geosite
{
    static class Program
    {
        [DllImport("kernel32", SetLastError = true)]
        private static extern bool AttachConsole(int dwProcessId);

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            var argCount = args.Length;

            if (argCount == 0)
            {
                //Attach to Winform
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new OGCform());
            }
            else
            {
                //Attach to Console
                AttachConsole(-1);

                //bool PostgreSqlConnection; //PostgreSql数据库是否处于连接状态？
                //(bool status, int forest, string name) ClusterUser; //GeositeServer集群用户信息，其中 name 将充当森林名称

                var applicationName = Assembly.GetExecutingAssembly().GetName().Name;
                (int Left, int Top)? cursorPosition = null;

                var title =
                    $@" {applicationName} for {RuntimeInformation.OSDescription} / {RuntimeInformation.ProcessArchitecture} "; // /{RuntimeInformation.FrameworkDescription} 
                var copyright =
                    @" Copyright (C) Geosite Development Team of CGS (R)";

                var splitLine = new string('*', Math.Max(title.Length, copyright.Length));

                Console.WriteLine();
                Console.WriteLine(splitLine);
                Console.WriteLine(title);
                Console.WriteLine(copyright);
                Console.WriteLine(splitLine);

                try
                {
                    var commandName = args[0].ToLower();

                    //args ---> Dictionary<string, List<string>>
                    var options = ConsoleIO.Arguments(
                        args: args,
                        offset: 0 //1=Skip command parameter 
                    );
                    var helper = options.ContainsKey("?") || options.ContainsKey("h") || options.ContainsKey("help");

                    switch (commandName)
                    {
                        case "mpj":
                        case "mapgismpj":
                            {
                                if (helper)
                                {
                                    CommandHelper(commandName);
                                    break;
                                }

                                if (!options.TryGetValue("i", out var inputFile))
                                    options.TryGetValue("input", out inputFile);
                                if (inputFile == null)
                                    throw new Exception("Input file not found.");

                                if (!options.TryGetValue("o", out var outputFile))
                                    options.TryGetValue("output", out outputFile);
                                if (outputFile == null)
                                    throw new Exception("Output file not found.");

                                var isDirectory = Path.GetExtension(outputFile[0]) == string.Empty; //there is no 100% way to distinguish a folder from a file by path alone. 
                                var inputFileCount = inputFile.Count;

                                if (inputFileCount == 1 && !isDirectory || inputFileCount > 1 && isDirectory)
                                {
                                    for (var i = 0; i < inputFileCount; i++)
                                    {
                                        var sourceFile = inputFile[i];
                                        var mapgisMpj = new MapGis.MapGisProject();
                                        var localI = i + 1;
                                        mapgisMpj.onGeositeEvent += (_, e) =>
                                        {
                                            var userStatus = !string.IsNullOrWhiteSpace(e.message)
                                                ? inputFileCount > 1
                                                    ? $"[{localI}/{inputFileCount}] {e.message}"
                                                    : e.message
                                                : null;
                                            ShowProgress(message: userStatus, code: e.code, progress: e.progress);
                                        };
                                        string targetFile;
                                        if (!isDirectory)
                                            targetFile = Path.ChangeExtension(outputFile[0], ".json");
                                        else
                                        {
                                            var postfix = 0;
                                            do
                                            {
                                                targetFile = Path.Combine(
                                                    outputFile[0],
                                                    Path.GetFileNameWithoutExtension(sourceFile) + (postfix == 0 ? "" : $"({postfix})") + ".json");
                                                if (!File.Exists(targetFile))
                                                    break;
                                                postfix++;
                                            } while (true);
                                        }
                                        mapgisMpj.Open(file: sourceFile);
                                        var directoryName = Path.GetDirectoryName(targetFile);
                                        if (Directory.Exists(directoryName) == false) //如果不存在就创建文件夹
                                            Directory.CreateDirectory(directoryName!);
                                        mapgisMpj.Export(SaveAs: targetFile);
                                    }
                                }
                                else
                                    throw new Exception("Input and output parameters do not match.");
                            }
                            break;
                        case "mapgis":
                            {
                                if (helper)
                                {
                                    CommandHelper(commandName);
                                    break;
                                }

                                if (!options.TryGetValue("i", out var inputFile))
                                    options.TryGetValue("input", out inputFile);
                                if (inputFile == null)
                                    throw new Exception("Input file not found.");

                                if (!options.TryGetValue("o", out var outputFile))
                                    options.TryGetValue("output", out outputFile);
                                if (outputFile == null)
                                    throw new Exception("Output file not found.");

                                if (!options.TryGetValue("f", out var format))
                                    options.TryGetValue("format", out format);

                                if (!options.TryGetValue("t", out var treePath))
                                    options.TryGetValue("treepath", out treePath);

                                if (!options.TryGetValue("d", out var description))
                                    options.TryGetValue("description", out description);

                                options.TryGetValue("pcolor", out var pcolor);

                                var isDirectory = Path.GetExtension(outputFile[0]) == string.Empty; //there is no 100% way to distinguish a folder from a file by path alone. 
                                var inputFileCount = inputFile.Count;

                                if (inputFileCount == 1 && !isDirectory || inputFileCount > 1 && isDirectory)
                                {
                                    for (var i = 0; i < inputFileCount; i++)
                                    {
                                        var sourceFile = inputFile[i];
                                        var localI = i + 1;
                                        var mapgis = new MapGis.MapGisFile();
                                        mapgis.onGeositeEvent += (_, e) =>
                                        {
                                            var userStatus = !string.IsNullOrWhiteSpace(e.message)
                                                ? inputFileCount > 1
                                                    ? $"[{localI}/{inputFileCount}] {e.message}"
                                                    : e.message
                                                : null;
                                            ShowProgress(message: userStatus, code: e.code, progress: e.progress);
                                        };
                                        mapgis.Open(mapgisFile: sourceFile, pcolorFile: pcolor?[0]);
                                        string targetFile;
                                        if (!isDirectory)
                                            targetFile = outputFile[0];
                                        else
                                        {
                                            var postfix = 0;
                                            do
                                            {
                                                targetFile = Path.Combine(
                                                    outputFile[0],
                                                    Path.GetFileNameWithoutExtension(sourceFile) + (postfix == 0 ? "" : $"({postfix})") + "." + (format != null ? format[0] : "geojson"));
                                                if (!File.Exists(targetFile))
                                                    break;
                                                postfix++;
                                            } while (true);
                                        }
                                        var directoryName = Path.GetDirectoryName(targetFile);
                                        if (Directory.Exists(directoryName) == false) //如果不存在就创建文件夹
                                            Directory.CreateDirectory(directoryName!);
                                        mapgis.Export(
                                            SaveAs: targetFile,
                                            Format: format != null ? format[0] : "geojson",
                                            TreePath: treePath?[0],
                                            ExtraDescription:
                                            description == null
                                                ? null
                                                : XElement.Parse(description[0])
                                        );
                                    }
                                }
                                else
                                    throw new Exception("Input and output parameters do not match.");
                            }
                            break;
                        case "shp":
                        case "shapefile":
                            {
                                if (helper)
                                {
                                    CommandHelper(commandName);
                                    break;
                                }

                                if (!options.TryGetValue("i", out var inputFile))
                                    options.TryGetValue("input", out inputFile);
                                if (inputFile == null)
                                    throw new Exception("Input file not found.");

                                if (!options.TryGetValue("o", out var outputFile))
                                    options.TryGetValue("output", out outputFile);
                                if (outputFile == null)
                                    throw new Exception("Output file not found.");

                                if (!options.TryGetValue("f", out var format))
                                    options.TryGetValue("format", out format);

                                if (!options.TryGetValue("c", out var codePage))
                                    options.TryGetValue("codepage", out codePage);

                                if (!options.TryGetValue("t", out var treePath))
                                    options.TryGetValue("treepath", out treePath);

                                if (!options.TryGetValue("d", out var description))
                                    options.TryGetValue("description", out description);

                                var isDirectory = Path.GetExtension(outputFile[0]) == string.Empty; //there is no 100% way to distinguish a folder from a file by path alone. 
                                var inputFileCount = inputFile.Count;
                                if (inputFileCount == 1 && !isDirectory || inputFileCount > 1 && isDirectory)
                                {
                                    for (var i = 0; i < inputFileCount; i++)
                                    {
                                        var sourceFile = inputFile[i];
                                        var localI = i + 1;
                                        var shapefile = new ShapeFile.ShapeFile();
                                        shapefile.onGeositeEvent += (_, e) =>
                                        {
                                            var userStatus = !string.IsNullOrWhiteSpace(e.message)
                                                ? inputFileCount > 1
                                                    ? $"[{localI}/{inputFileCount}] {e.message}"
                                                    : e.message
                                                : null;
                                            ShowProgress(message: userStatus, code: e.code, progress: e.progress);
                                        };
                                        shapefile.Open(
                                            shapeFilePath: sourceFile,
                                            codePage: int.Parse(codePage == null ? "936" : codePage[0])
                                        ); //GB2312 / GBK
                                        string targetFile;
                                        if (!isDirectory)
                                            targetFile = outputFile[0];
                                        else
                                        {
                                            var postfix = 0;
                                            do
                                            {
                                                targetFile = Path.Combine(
                                                    outputFile[0],
                                                    Path.GetFileNameWithoutExtension(sourceFile) + (postfix == 0 ? "" : $"({postfix})") + "." + (format != null ? format[0] : "geojson"));
                                                if (!File.Exists(targetFile))
                                                    break;
                                                postfix++;
                                            } while (true);
                                        }
                                        var directoryName = Path.GetDirectoryName(targetFile);
                                        if (Directory.Exists(directoryName) == false) //如果不存在就创建文件夹
                                            Directory.CreateDirectory(directoryName!);
                                        shapefile.Export(
                                            SaveAs: targetFile,
                                            Format: format == null ? "geojson" : format[0],
                                            TreePath: treePath?[0],
                                            ExtraDescription: description == null
                                                ? null
                                                : XElement.Parse(description[0])
                                        );
                                    }
                                }
                                else
                                    throw new Exception("Input and output parameters do not match.");
                            }
                            break;
                        case "txt":
                        case "csv":
                            {
                                if (helper)
                                {
                                    CommandHelper(commandName);
                                    break;
                                }

                                if (!options.TryGetValue("i", out var inputFile))
                                    options.TryGetValue("input", out inputFile);
                                if (inputFile == null)
                                    throw new Exception("Input file not found.");

                                if (!options.TryGetValue("o", out var outputFile))
                                    options.TryGetValue("output", out outputFile);
                                if (outputFile == null)
                                    throw new Exception("Output file not found.");

                                if (!options.TryGetValue("f", out var format))
                                    options.TryGetValue("format", out format);

                                if (!options.TryGetValue("t", out var treePath))
                                    options.TryGetValue("treepath", out treePath);

                                if (!options.TryGetValue("d", out var description))
                                    options.TryGetValue("description", out description);

                                if (!options.TryGetValue("c", out var coordinateFieldName))
                                    options.TryGetValue("coordinate", out coordinateFieldName);

                                var isDirectory = Path.GetExtension(outputFile[0]) == string.Empty; //there is no 100% way to distinguish a folder from a file by path alone. 
                                var inputFileCount = inputFile.Count;
                                if (inputFileCount == 1 && !isDirectory || inputFileCount > 1 && isDirectory)
                                {
                                    for (var i = 0; i < inputFileCount; i++)
                                    {
                                        var sourceFile = inputFile[i];
                                        var localI = i + 1;
                                        var freeTextFields = commandName == ".txt"
                                            ? FreeText.TXT.TXT.GetFieldNames(sourceFile)
                                            : FreeText.CSV.CSV.GetFieldNames(sourceFile);
                                        if (freeTextFields.Length == 0)
                                            throw new Exception("No valid fields found.");

                                        var position = freeTextFields.Any(f => f == "_position_") ? "_position_" :
                                            coordinateFieldName?[0];

                                        if (position != null)
                                        {
                                            //Polymorphism: assigning derived class objects to base class objects
                                            FreeText.FreeText freeText = commandName == ".txt"
                                                ? new FreeText.TXT.TXT(CoordinateFieldName: position)
                                                : new FreeText.CSV.CSV(CoordinateFieldName: position);
                                            freeText.onGeositeEvent += (_, e) =>
                                            {
                                                var userStatus = !string.IsNullOrWhiteSpace(e.message)
                                                    ? inputFileCount > 1
                                                        ? $"[{localI}/{inputFileCount}] {e.message}"
                                                        : e.message
                                                    : null;
                                                ShowProgress(message: userStatus, code: e.code, progress: e.progress);
                                            };
                                            freeText.Open(sourceFile);
                                            string targetFile;
                                            if (!isDirectory)
                                                targetFile = outputFile[0];
                                            else
                                            {
                                                var postfix = 0;
                                                do
                                                {
                                                    targetFile = Path.Combine(
                                                        outputFile[0],
                                                        Path.GetFileNameWithoutExtension(sourceFile) + (postfix == 0 ? "" : $"({postfix})") + "." + (format != null ? format[0] : "geojson"));
                                                    if (!File.Exists(targetFile))
                                                        break;
                                                    postfix++;
                                                } while (true);
                                            }
                                            var directoryName = Path.GetDirectoryName(targetFile);
                                            if (Directory.Exists(directoryName) == false) //如果不存在就创建文件夹
                                                Directory.CreateDirectory(directoryName!);
                                            freeText.Export(
                                                SaveAs: targetFile,
                                                Format: format == null ? "geojson" : format[0],
                                                TreePath: treePath?[0],
                                                ExtraDescription: description == null
                                                    ? null
                                                    : XElement.Parse(description[0])
                                            );
                                        }
                                        else
                                            throw new Exception("No valid coordinate fields found.");
                                    }
                                }
                                else
                                    throw new Exception("Input and output parameters do not match.");
                            }
                            break;
                        default:
                            throw new Exception("");
                    }
                }
                catch (Exception error)
                {
                    if (!string.IsNullOrEmpty(error.Message))
                    {
                        Console.WriteLine();
                        Console.WriteLine($@"Exception: {error.Message}");
                    }
                    else
                    {
                        Console.WriteLine($@"Usage: {applicationName} [Command] [Options]");
                        Console.WriteLine();
                        Console.WriteLine(@"Command:");
                        Console.WriteLine(@"    -?/help");
                        Console.WriteLine(@"    mpj/mapgismpj");
                        Console.WriteLine(@"    mapgis");
                        Console.WriteLine(@"    shp/shapefile");
                        Console.WriteLine(@"    txt/csv");
                        Console.WriteLine(@"Options:");
                        Console.WriteLine(@"    -[key] [value]");
                        Console.WriteLine();
                        Console.WriteLine($@"Run '{applicationName} [Command] -?/help' for more information on a command.");
                    }
                }

                SendKeys.SendWait("{ENTER}");
                return;

                ///// <param name="message">message</param>
                ///// <param name="code">statusCode（0/null=Pre-processing；1=processing；200=Post-processing）</param>
                ///// <param name="progress">progress（0～100，only for code = 1）</param>
                void ShowProgress(string message = null!, int? code = null, int? progress = null)
                {
                    var showProgressTask = Task.Run(
                        () =>
                        {
                            try
                            {
                                switch (code)
                                {
                                    case 1: //processing
                                        cursorPosition ??= (Console.CursorLeft, Console.CursorTop);
                                        if (cursorPosition != null)
                                        {
                                            Console.SetCursorPosition(0, cursorPosition.Value.Top);
                                            Console.Write(new string(' ', Console.WindowWidth));
                                            Console.SetCursorPosition(0, cursorPosition.Value.Top);
                                            Console.Write(@"{0} {1}%", message, progress);
                                        }
                                        break;
                                    default: //Pre-processing / Post-processing
                                        if (!string.IsNullOrWhiteSpace(message))
                                        {
                                            cursorPosition = (Console.CursorLeft, Console.CursorTop);
                                            if (cursorPosition != null)
                                            {
                                                if (cursorPosition.Value.Left > 0)
                                                    Console.WriteLine();
                                                Console.WriteLine(message);
                                            }
                                        }
                                        break;
                                }
                            }
                            catch
                            {
                                //
                            }
                        }
                    );
                    showProgressTask.Wait();
                }

                void CommandHelper(string command)
                {
                    switch (command)
                    {
                        case "mpj":
                        case "mapgismpj":
                            Console.WriteLine(@"Command: mpj/mapgismpj [Options]");
                            Console.WriteLine(@"    Options: -i/input InputFile[s] -o/output OutputFile/OutputFolder");
                            Console.WriteLine(@"        InputFile[s]: *.mpj");
                            Console.WriteLine(@"        OutputFile/OutputFolder: *.geojson/folderPath");
                            Console.WriteLine(@"Example:");
                            Console.WriteLine($@"   {applicationName} mpj -i ./mapgis.mpj -o ./test.geojson");
                            Console.WriteLine($@"   {applicationName} mpj -i ./mapgis1.mpj ./mapgis2.mpj -o ./testfolder");
                            break;
                        case "mapgis":
                            Console.WriteLine(@"Command: mapgis [Options]");
                            Console.WriteLine(@"    Options: -i/input InputFile[s] -o/output OutputFile/OutputFolder -f/format Format -t/treepath TreePath -d/description Description -pcolor Pcolor");
                            Console.WriteLine(@"        InputFile[s]: *.wt, *.wl, *.wp");
                            Console.WriteLine(@"        OutputFile/OutputFolder: *.shp, *.geojson, *.gml, *.kml, *.xml / folderPath");
                            Console.WriteLine(@"        Format: shp/shapefile, geojson[default], gml, kml, xml");
                            Console.WriteLine(@"        TreePath: null[default]");
                            Console.WriteLine(@"        Description: null[default]");
                            Console.WriteLine(@"        Pcolor: MapGisSlib/Pcolor.lib");
                            Console.WriteLine(@"Example:");
                            Console.WriteLine($@"   {applicationName} mapgis -i ./point.wt -o ./test.shp -f shapefile");
                            Console.WriteLine($@"   {applicationName} mapgis -i ./line.wl -o ./test.shp -f shapefile -pcolor ./Slib/Pcolor.lib");
                            Console.WriteLine($@"   {applicationName} mapgis -i ./point.wt ./line.wl ./polygon.wp -o ./testfolder -f shapefile");
                            break;
                        case "shp":
                        case "shapefile":
                            Console.WriteLine(@"Command: shp/shapefile [Options]");
                            Console.WriteLine(@"    Options: -i/input InputFile[s] -o/output OutputFile/OutputFolder -f/format Format -t/treepath TreePath -d/description Description -c/codepage CodePage");
                            Console.WriteLine(@"        InputFile[s]: *.shp");
                            Console.WriteLine(@"        OutputFile/OutputFolder: *.geojson, *.gml, *.kml, *.shp, *.xml / folderPath");
                            Console.WriteLine(@"        Format: geojson[default], gml, kml, shp, xml");
                            Console.WriteLine(@"        TreePath: null[default]");
                            Console.WriteLine(@"        Description: null[default]");
                            Console.WriteLine(@"        CodePage: 936[default]");
                            Console.WriteLine(@"Example:");
                            Console.WriteLine($@"   {applicationName} shapefile -i ./theme.shp -o ./test.geojson -f geojson");
                            break;
                        case "txt":
                        case "csv":
                            Console.WriteLine(@"Command: txt/csv [Options]");
                            Console.WriteLine(@"    Options: -i/input InputFile[s] -o/output OutputFile/OutputFolder -f/format Format -t/treepath TreePath -d/description Description -c/coordinate Coordinate");
                            Console.WriteLine(@"        InputFile[s]: *.txt/csv");
                            Console.WriteLine(@"        OutputFile/OutputFolder: *.geojson, *.gml, *.kml, *.shp, *.xml");
                            Console.WriteLine(@"        Format: geojson[default], gml, kml, shp, xml");
                            Console.WriteLine(@"        TreePath: null[default]");
                            Console.WriteLine(@"        Description: null[default]");
                            Console.WriteLine(@"        Coordinate: _position_[default]");
                            Console.WriteLine(@"Example:");
                            Console.WriteLine($@"   {applicationName} txt -i ./line.txt -o ./test.shp -f shp");
                            Console.WriteLine($@"   {applicationName} csv -i ./line.csv -o ./test.shp -f shp");
                            break;
                    }
                }
            }
        }
    }
}
