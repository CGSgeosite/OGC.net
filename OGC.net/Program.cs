using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;

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

                var applicationName = Assembly.GetExecutingAssembly().GetName().Name;
                (int Left, int Top)? cursorPosition = null;

                var title =
                    $@" {applicationName} for {RuntimeInformation.OSDescription} / {RuntimeInformation.FrameworkDescription} / {RuntimeInformation.ProcessArchitecture} ";
                var copyright =
                    @" Copyright (C) 2019-2021 Geosite Development Team of CGS (R)";

                var splitLine = new string('*', Math.Max(title.Length, copyright.Length));

                Console.WriteLine();
                Console.WriteLine(splitLine);
                Console.WriteLine(title);
                Console.WriteLine(copyright);
                Console.WriteLine(splitLine);

                try
                {
                    var commandName = args[0].ToLower();
                    var options = GetOptions();

                    var helper = options.ContainsKey("?") || options.ContainsKey("h") || options.ContainsKey("help");

                    switch (commandName)
                    {
                        case "mpj":
                        case "mapgismpj":
                            {
                                if (helper)
                                {
                                    MapgisMpjHelper();
                                    break;
                                }

                                if (!options.TryGetValue("i", out var inputFile))
                                    options.TryGetValue("input", out inputFile);
                                if (inputFile == null)
                                    throw new Exception("Input file not found.");

                                if (!options.TryGetValue("o", out var outputFile))
                                    options.TryGetValue("output", out outputFile);
                                if (outputFile == null)
                                    throw new Exception("Output file file not found.");

                                var mapgisMpj = new MapGis.MapGisProject();
                                mapgisMpj.onGeositeEvent += (_, e) =>
                                {
                                    ShowProgress(message: e.message, code: e.code, progress: e.progress);
                                };
                                mapgisMpj.Open(file: inputFile);
                                mapgisMpj.Export(SaveAs: outputFile);
                            }
                            break;
                        case "mapgis":
                            {
                                if (helper)
                                {
                                    MapgisHelper();
                                    break;
                                }

                                if (!options.TryGetValue("i", out var inputFile))
                                    options.TryGetValue("input", out inputFile);
                                if (inputFile == null)
                                    throw new Exception("Input file not found.");

                                if (!options.TryGetValue("o", out var outputFile))
                                    options.TryGetValue("output", out outputFile);
                                if (outputFile == null)
                                    throw new Exception("Output file file not found.");

                                if (!options.TryGetValue("f", out var format))
                                    options.TryGetValue("format", out format);
                                format ??= "geojson";

                                if (!options.TryGetValue("t", out var treePath))
                                    options.TryGetValue("treepath", out treePath);

                                if (!options.TryGetValue("d", out var description))
                                    options.TryGetValue("description", out description);

                                options.TryGetValue("pcolor", out var pcolor);

                                var mapgis = new MapGis.MapGisFile();
                                mapgis.onGeositeEvent += (_, e) =>
                                {
                                    ShowProgress(message: e.message, code: e.code, progress: e.progress);
                                };
                                mapgis.Open(mapgisFile: inputFile, pcolorFile: pcolor);
                                mapgis.Export(
                                    SaveAs: outputFile,
                                    Format: format,
                                    TreePath: string.IsNullOrWhiteSpace(treePath) ? null : treePath,
                                    ExtraDescription: string.IsNullOrWhiteSpace(description)
                                        ? null
                                        : XElement.Parse(description)
                                );
                            }
                            break;
                        case "shp":
                        case "shapefile":
                            {
                                if (helper)
                                {
                                    ShapeFileHelper();
                                    break;
                                }

                                if (!options.TryGetValue("i", out var inputFile))
                                    options.TryGetValue("input", out inputFile);
                                if (inputFile == null)
                                    throw new Exception("Input file not found.");

                                if (!options.TryGetValue("o", out var outputFile))
                                    options.TryGetValue("output", out outputFile);
                                if (outputFile == null)
                                    throw new Exception("Output file file not found.");

                                if (!options.TryGetValue("f", out var format))
                                    options.TryGetValue("format", out format);
                                format ??= "geojson";

                                if (!options.TryGetValue("c", out var codePage))
                                    options.TryGetValue("codepage", out codePage);
                                codePage ??= "936"; //GB2312 / GBK

                                if (!options.TryGetValue("t", out var treePath))
                                    options.TryGetValue("treepath", out treePath);

                                if (!options.TryGetValue("d", out var description))
                                    options.TryGetValue("description", out description);

                                var shapefile = new ShapeFile.ShapeFile();
                                shapefile.onGeositeEvent += (_, e) =>
                                {
                                    ShowProgress(message: e.message, code: e.code, progress: e.progress);
                                };
                                shapefile.Open(shapeFilePath: inputFile, codePage: int.Parse(codePage));
                                shapefile.Export(
                                    SaveAs: outputFile,
                                    Format: format,
                                    TreePath: treePath,
                                    ExtraDescription: string.IsNullOrWhiteSpace(description)
                                        ? null
                                        : XElement.Parse(description)
                                );
                            }
                            break;
                        case "txt":
                        case "csv":
                            {
                                if (helper)
                                {
                                    TextFileHelper();
                                    break;
                                }

                                if (!options.TryGetValue("i", out var inputFile))
                                    options.TryGetValue("input", out inputFile);
                                if (inputFile == null)
                                    throw new Exception("Input file not found.");

                                if (!options.TryGetValue("o", out var outputFile))
                                    options.TryGetValue("output", out outputFile);
                                if (outputFile == null)
                                    throw new Exception("Output file file not found.");

                                if (!options.TryGetValue("f", out var format))
                                    options.TryGetValue("format", out format);
                                format ??= "geojson";

                                if (!options.TryGetValue("t", out var treePath))
                                    options.TryGetValue("treepath", out treePath);

                                if (!options.TryGetValue("d", out var description))
                                    options.TryGetValue("description", out description);

                                if (!options.TryGetValue("c", out var coordinateFieldName))
                                    options.TryGetValue("coordinate", out coordinateFieldName);

                                var freeTextFields = commandName == ".txt"
                                    ? FreeText.TXT.TXT.GetFieldNames(inputFile)
                                    : FreeText.CSV.CSV.GetFieldNames(inputFile);
                                if (freeTextFields.Length == 0)
                                    throw new Exception("No valid fields found.");

                                coordinateFieldName = freeTextFields.Any(f => f == "_position_") ? "_position_" :
                                    coordinateFieldName;

                                if (coordinateFieldName != null)
                                {
                                    //Polymorphism: assigning derived class objects to base class objects
                                    FreeText.FreeText freeText = commandName == ".txt"
                                        ? new FreeText.TXT.TXT(CoordinateFieldName: coordinateFieldName)
                                        : new FreeText.CSV.CSV(CoordinateFieldName: coordinateFieldName);
                                    freeText.onGeositeEvent += (_, e) =>
                                    {
                                        ShowProgress(message: e.message, code: e.code, progress: e.progress);
                                    };
                                    freeText.Open(inputFile);
                                    freeText.Export(
                                        SaveAs: outputFile,
                                        Format: format,
                                        TreePath: treePath,
                                        ExtraDescription: string.IsNullOrWhiteSpace(description)
                                            ? null
                                            : XElement.Parse(description)
                                    );
                                }
                                else
                                    throw new Exception("No valid coordinate fields found.");
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
                        Console.WriteLine(@"    -?/h/help");
                        Console.WriteLine(@"    mpj/mapgismpj");
                        Console.WriteLine(@"    mapgis");
                        Console.WriteLine(@"    shp/shapefile");
                        Console.WriteLine(@"    txt/csv");
                        Console.WriteLine(@"Options:");
                        Console.WriteLine(@"    -[key] [value]");
                        Console.WriteLine();
                        Console.WriteLine($@"Run '{applicationName} [Command] -?/h/help' for more information on a command.");
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

                Dictionary<string, string> GetOptions()
                {
                    //args[0] = command
                    //args[>0] = kvps
                    var result = new Dictionary<string, string>();
                    string oldKey = null;
                    for (var i = 1; i < argCount; i++)
                    {
                        var newKey = Regex.Match(args[i], @"^[-]+([\S]+)").Groups[1].Value;
                        if (!string.IsNullOrWhiteSpace(newKey))
                        {
                            //key
                            if (oldKey != null)
                                result.Add(oldKey, null);
                            oldKey = newKey.ToLower();
                        }
                        else
                        {
                            //value
                            if (oldKey != null)
                            {
                                var value = args[i];
                                if (result.ContainsKey(oldKey))
                                    result[oldKey] = value;
                                else
                                    result.Add(oldKey, value);
                                oldKey = null;
                            }
                            //else
                            //{
                            //    //ignore this value, because there are no keys
                            //}
                        }
                    }
                    if (oldKey != null)
                        result.Add(oldKey, null);

                    return result;
                }

                void MapgisMpjHelper()
                {
                    Console.WriteLine(@"Command: mpj/mapgismpj [Options]");
                    Console.WriteLine(@"    Options: -i/input InputFile -o/output OutputFile");
                    Console.WriteLine(@"        InputFile: *.mpj");
                    Console.WriteLine(@"        OutputFile: *.geojson");
                    Console.WriteLine(@"Example:");
                    Console.WriteLine($@"   {applicationName} mpj -i ./mapgis.mpj -o ./test.geojson");
                }

                void MapgisHelper()
                {
                    Console.WriteLine(@"Command: mapgis [Options]");
                    Console.WriteLine(@"    Options: -i/input InputFile -o/output OutputFile -f/format Format -t/treepath TreePath -d/description Description -pcolor Pcolor");
                    Console.WriteLine(@"        InputFile: *.wt, *.wl, *.wp");
                    Console.WriteLine(@"        OutputFile: *.shp, *.geojson, *.gml, *.kml, *.xml");
                    Console.WriteLine(@"        Format: shp/shapefile, geojson[default], gml, kml, xml");
                    Console.WriteLine(@"        TreePath: null[default]");
                    Console.WriteLine(@"        Description: null[default]");
                    Console.WriteLine(@"        Pcolor: MapGIS Pcolor.lib");
                    Console.WriteLine(@"Example:");
                    Console.WriteLine($@"   {applicationName} mapgis -i ./point.wt -o ./test.shp -f shapefile");
                }

                void ShapeFileHelper()
                {
                    Console.WriteLine(@"Command: shp/shapefile [Options]");
                    Console.WriteLine(@"    Options: -i/input InputFile -o/output OutputFile -f/format Format -t/treepath TreePath -d/description Description -c/codepage CodePage");
                    Console.WriteLine(@"        SourceFile: *.shp");
                    Console.WriteLine(@"        TargetFile: *.geojson, *.gml, *.kml, *.shp, *.xml");
                    Console.WriteLine(@"        Format: geojson[default], gml, kml, shp, xml");
                    Console.WriteLine(@"        TreePath: null[default]");
                    Console.WriteLine(@"        Description: null[default]");
                    Console.WriteLine(@"        CodePage: 936[default]");
                    Console.WriteLine(@"Example:");
                    Console.WriteLine($@"   {applicationName} shapefile -i ./theme.shp -o ./test.geojson -f geojson");
                }

                void TextFileHelper()
                {
                    Console.WriteLine(@"Command: txt/csv [Options]");
                    Console.WriteLine(@"    Options: -i/input InputFile -o/output OutputFile -f/format Format -t/treepath TreePath -d/description Description -c/coordinate Coordinate");
                    Console.WriteLine(@"        SourceFile: *.txt/csv");
                    Console.WriteLine(@"        TargetFile: *.geojson, *.gml, *.kml, *.shp, *.xml");
                    Console.WriteLine(@"        Format: geojson[default], gml, kml, shp, xml");
                    Console.WriteLine(@"        TreePath: null[default]");
                    Console.WriteLine(@"        Description: null[default]");
                    Console.WriteLine(@"        Coordinate: _position_[default]");
                    Console.WriteLine(@"Example:");
                    Console.WriteLine($@"   {applicationName} txt -i ./line.txt -o ./test.shp -f shp");
                    Console.WriteLine($@"   {applicationName} csv -i ./line.csv -o ./test.shp -f shp");
                }
            }
        }
    }
}
