using System;
using System.Windows.Forms;
using System.Reflection;
using System.Runtime.InteropServices;

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
            if (args.Length == 0)
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
                    $@" {applicationName} for {RuntimeInformation.OSDescription} / {RuntimeInformation.FrameworkDescription} / {RuntimeInformation.ProcessArchitecture}";
                var copyright =
                    @" Copyright (c) 2019-2021 Geosite Development Team of CGS";

                var splitLine = new string('*', Math.Max(title.Length, copyright.Length));

                Console.WriteLine();
                Console.WriteLine(splitLine);
                Console.WriteLine(title);
                Console.WriteLine(copyright);
                Console.WriteLine(splitLine);

                try
                {
                    var argCount = args.Length;
                    switch (args[0].ToLower())
                    {
                        case "mapgis":
                            {
                                if (argCount < 3)
                                    throw new Exception("Insufficient number of parameters");

                                var mapgis = new MapGis.MapGisFile();
                                mapgis.onGeositeEvent += (_, e) =>
                                {
                                    ShowProgress(message: e.message, code: e.code, progress: e.progress);
                                };
                                mapgis.Open(file: args[1]);
                                mapgis.Export(SaveAs: args[2], Format: argCount > 3 ? args[3] : "geojson");
                            }
                            break;
                        case "mapgismpj":
                        case "mpj":
                            {
                                if (argCount < 2)
                                    throw new Exception("Insufficient number of parameters");

                                var mapgisMpj = new MapGis.MapGisProject();
                                mapgisMpj.onGeositeEvent += (_, e) =>
                                {
                                    ShowProgress(message: e.message, code: e.code, progress: e.progress);
                                };
                                mapgisMpj.Open(file: args[1]);
                                mapgisMpj.Export(SaveAs: args[2]);
                            }
                            break;
                        case "shapefile":
                        case "shp":
                            {
                                if (argCount < 3)
                                    throw new Exception("Insufficient number of parameters");

                                var shapefile = new ShapeFile.ShapeFile();
                                shapefile.onGeositeEvent += (_, e) =>
                                {
                                    ShowProgress(message: e.message, code: e.code, progress: e.progress);
                                };
                                shapefile.Open(shapeFilePath: args[1], codePage: int.Parse(argCount > 4 ? args[4] : "936"));
                                shapefile.Export(SaveAs: args[2], Format: argCount > 3 ? args[3] : "geojson");
                            }
                            break;
                        default:
                            throw new Exception();
                    }
                }
                catch
                {
                    Console.WriteLine($@"Usage: {applicationName} Command Parameters");
                    Console.WriteLine($@"Run '{applicationName} -help' for more information on a command.");
                    Console.WriteLine();
                    Console.WriteLine(@"Command: mapgis");
                    Console.WriteLine(@"    Parameters: SourceFile TargetFile Format");
                    Console.WriteLine(@"        SourceFile: *.wt, *.wl, *.wp");
                    Console.WriteLine(@"        TargetFile: *.shp, *.geojson, *.gml, *.kml, *.xml");
                    Console.WriteLine(@"        Format: shp/shapefile, geojson[default], gml, kml, xml");
                    Console.WriteLine();
                    Console.WriteLine(@"Command: mpj/mapgismpj");
                    Console.WriteLine(@"    Parameters: SourceFile TargetFile");
                    Console.WriteLine(@"        SourceFile: *.mpj");
                    Console.WriteLine(@"        TargetFile: *.geojson");
                    Console.WriteLine();
                    Console.WriteLine(@"Command: shp/shapefile");
                    Console.WriteLine(@"    Parameters: SourceFile TargetFile Format CodePage");
                    Console.WriteLine(@"        SourceFile: *.shp");
                    Console.WriteLine(@"        TargetFile: *.geojson, *.gml, *.kml, *.shp, *.xml");
                    Console.WriteLine(@"        Format: geojson[default], gml, kml, shp, xml");
                    Console.WriteLine(@"        CodePage: 936[default]");
                    Console.WriteLine();
                    Console.Write(@"Press any key to exit ...");
                    Console.ReadKey();
                }

                SendKeys.SendWait("{ENTER}");
                return;

                ///// <param name="message">message</param>
                ///// <param name="code">statusCode（0/null=Pre-processing；1=processing；200=Post-processing）</param>
                ///// <param name="progress">progress（0～100，only for code = 1）</param>
                void ShowProgress(string message = null!, int? code = null, int? progress = null)
                {
                    switch (code)
                    {
                        case 1: //processing
                            cursorPosition ??= (Console.CursorLeft, Console.CursorTop);
                            if (cursorPosition != null)
                            {
                                Console.SetCursorPosition(cursorPosition.Value.Left, cursorPosition.Value.Top);
                                Console.Write(@"{0} {1}%", message, progress);
                            }
                            break;
                        default: //Pre-processing / Post-processing
                            if (!string.IsNullOrWhiteSpace(message))
                            {
                                Console.WriteLine();
                                cursorPosition = (Console.CursorLeft, Console.CursorTop);
                                Console.WriteLine(message);
                            }
                            break;
                    }
                }
            }
        }
    }
}
