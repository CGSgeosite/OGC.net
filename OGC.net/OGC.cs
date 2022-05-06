/******************************************************************************
 *
 * Name: OGC.net
 * Purpose: A free tool for reading ShapeFile, MapGIS, TXT/CSV, converting them
 *          into GML, GeoJSON, ShapeFile, KML and GeositeXML, and pushing vector
 *          or raster to PostgreSQL database.
 *
 ******************************************************************************
 * (C) 2019-2022 Geosite Development Team of CGS (R)
 *
 * Permission is hereby granted, free of charge, to any person obtaining a
 * copy of this software and associated documentation files (the "Software"),
 * to deal in the Software without restriction, including without limitation
 * the rights to use, copy, modify, merge, publish, distribute, sublicense,
 * and/or sell copies of the Software, and to permit persons to whom the
 * Software is furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included
 * in all copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS
 * OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL
 * THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
 * FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
 * DEALINGS IN THE SOFTWARE.
 *****************************************************************************/

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.Xml.Linq;
using Geosite.FreeText.CSV;
using Geosite.FreeText.TXT;
using Geosite.GeositeServer;
using Geosite.GeositeServer.DeepZoom;
using Geosite.GeositeServer.PostgreSQL;
using Geosite.GeositeServer.Raster;
using Geosite.GeositeServer.Vector;
using Geosite.Messager;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Microsoft.VisualBasic;

namespace Geosite
{
    public partial class OGCform : Form
    {
        private readonly string _getCopyright; //软件版权信息
        private bool _postgreSqlConnection; //PostgreSql数据库是否处于连接状态？
        private (bool status, int forest, string name) _clusterUser; //GeositeServer集群用户信息，其中 name 将充当森林名称
        private DataGrid _clusterDate;
        private string _clusterDateGridCell;
        private bool _noPromptMetaData; //是否不再弹出元数据对话框？
        private bool _noPromptLayersBuilder; //是否不再弹出层级分类对话框？
        private LoadingBar _loading; //加载进度条

        public OGCform()
        {
            Opacity = 0;

            InitializeComponent();
            InitializeBackgroundWorker();

            _postgreSqlConnection =
                _noPromptMetaData =
                    _noPromptLayersBuilder = false;
            _getCopyright = Copyright.CopyrightAttribute;
        }

        private void OGCform_Load(object sender, EventArgs e)
        {
            //-----test----


            //-------------

            //窗口标题-----
            Text = Copyright.TitleAttribute + @" V" + Copyright.VersionAttribute;

            //功能卡片定位，首次加载时切换至【help】卡片
            var key = ogcCard.Name;
            var defaultvalue = RegEdit.Getkey(key, "2");
            ogcCard.SelectedIndex = int.Parse(defaultvalue ?? "2");

            //状态栏初始文本-----
            statusText.Text = _getCopyright;

            //设置UI交互控件的默认状态-----
            key = GeositeServerUrl.Name;
            defaultvalue = RegEdit.Getkey(key);
            GeositeServerUrl.Text = defaultvalue ?? "";

            key = GeositeServerUser.Name;
            defaultvalue = RegEdit.Getkey(key);
            GeositeServerUser.Text = defaultvalue ?? "";

            key = GeositeServerPassword.Name;
            defaultvalue = RegEdit.Getkey(key);
            GeositeServerPassword.Text = defaultvalue ?? "";

            key = FormatStandard.Name;
            defaultvalue = RegEdit.Getkey(key, "True");
            FormatStandard.Checked = bool.Parse(defaultvalue);

            key = FormatTMS.Name;
            defaultvalue = RegEdit.Getkey(key, "False");
            FormatTMS.Checked = bool.Parse(defaultvalue);

            key = FormatMapcruncher.Name;
            defaultvalue = RegEdit.Getkey(key, "False");
            FormatMapcruncher.Checked = bool.Parse(defaultvalue);

            key = FormatArcGIS.Name;
            defaultvalue = RegEdit.Getkey(key, "False");
            FormatArcGIS.Checked = bool.Parse(defaultvalue);

            key = FormatDeepZoom.Name;
            defaultvalue = RegEdit.Getkey(key, "False");
            FormatDeepZoom.Checked = bool.Parse(defaultvalue);

            key = FormatRaster.Name;
            defaultvalue = RegEdit.Getkey(key, "False");
            FormatRaster.Checked = bool.Parse(defaultvalue);

            key = EPSG4326.Name;
            defaultvalue = RegEdit.Getkey(key, "False");
            EPSG4326.Checked = bool.Parse(defaultvalue);

            key = UpdateBox.Name;
            defaultvalue = RegEdit.Getkey(key, "True");
            UpdateBox.Checked = bool.Parse(defaultvalue);

            key = topologyCheckBox.Name;
            defaultvalue = RegEdit.Getkey(key, "False");
            topologyCheckBox.Checked = bool.Parse(defaultvalue);

            key = tileLevels.Name;
            defaultvalue = RegEdit.Getkey(key, "-1");
            tileLevels.Text = defaultvalue ?? "-1";

            key = themeNameBox.Name;
            defaultvalue = RegEdit.Getkey(key);
            themeNameBox.Text = defaultvalue ?? "";

            key = localTileFolder.Name;
            defaultvalue = RegEdit.Getkey(key);
            localTileFolder.Text = defaultvalue ?? "";

            key = ModelOpenTextBox.Name;
            defaultvalue = RegEdit.Getkey(key);
            ModelOpenTextBox.Text = defaultvalue ?? "";
            ModelSave.Enabled = !string.IsNullOrWhiteSpace(ModelOpenTextBox.Text);

            key = tilewebapi.Name;
            defaultvalue = RegEdit.Getkey(key);
            tilewebapi.Text = defaultvalue ?? "";

            key = wmtsNorth.Name;
            defaultvalue = RegEdit.Getkey(key, "90");
            wmtsNorth.Text = defaultvalue ?? "90";

            key = wmtsSouth.Name;
            defaultvalue = RegEdit.Getkey(key, "-90");
            wmtsSouth.Text = defaultvalue ?? "-90";

            key = wmtsWest.Name;
            defaultvalue = RegEdit.Getkey(key, "-180");
            wmtsWest.Text = defaultvalue ?? "-180";

            key = wmtsEast.Name;
            defaultvalue = RegEdit.Getkey(key, "180");
            wmtsEast.Text = defaultvalue ?? "180";

            key = subdomainsBox.Name;
            defaultvalue = RegEdit.Getkey(key);
            subdomainsBox.Text = defaultvalue ?? "";

            key = DeepZoomLevels.Name;
            defaultvalue = RegEdit.Getkey(key, "12");
            DeepZoomLevels.Text = defaultvalue ?? "12";

            key = wmtsMinZoom.Name;
            defaultvalue = RegEdit.Getkey(key, "0");
            wmtsMinZoom.Text = defaultvalue ?? "0";

            key = wmtsSpider.Name;
            defaultvalue = RegEdit.Getkey(key, "False");
            wmtsMinZoom.Enabled = wmtsMaxZoom.Enabled = !(wmtsSpider.Checked = bool.Parse(defaultvalue));

            key = wmtsMaxZoom.Name;
            defaultvalue = RegEdit.Getkey(key, "18");
            wmtsMaxZoom.Text = defaultvalue ?? "18";

            key = rasterTileSize.Name;
            defaultvalue = RegEdit.Getkey(key, "100");
            rasterTileSize.Text = defaultvalue ?? "100";

            key = nodatabox.Name;
            defaultvalue = RegEdit.Getkey(key, "-32768");
            nodatabox.Text = defaultvalue ?? "-32768";

            key = maptilertoogc.Name;
            defaultvalue = RegEdit.Getkey(key, "True");
            maptilertoogc.Checked = bool.Parse(defaultvalue);

            key = mapcrunchertoogc.Name;
            defaultvalue = RegEdit.Getkey(key, "False");
            mapcrunchertoogc.Checked = bool.Parse(defaultvalue);

            key = ogctomapcruncher.Name;
            defaultvalue = RegEdit.Getkey(key, "False");
            ogctomapcruncher.Checked = bool.Parse(defaultvalue);

            key = ogctomaptiler.Name;
            defaultvalue = RegEdit.Getkey(key, "False");
            ogctomaptiler.Checked = bool.Parse(defaultvalue);

            key = MIMEBox.Name;
            defaultvalue = RegEdit.Getkey(key, "png");
            MIMEBox.Text = defaultvalue ?? "png";

            tilesource_SelectedIndexChanged(null, null);

            _loading = new LoadingBar(waitingBar);

            //窗体淡入
            var fadeIn = new Timer
            {
                Site = null,
                Tag = null,
                Enabled = false,
                Interval = 16 //间隔（毫秒）
            };
            fadeIn.Tick += (_, _) =>
            {
                if (this.Opacity >= 1)
                    fadeIn.Stop();
                else
                    Opacity += 0.05; //步距
            };
            fadeIn.Start();
        }

        private void OGCform_Closing(object sender, FormClosingEventArgs e)
        {
            e.Cancel = true;

            //窗体关闭之前，强行结束后台处理任务

            Notify.Dispose(); //关闭通知消息框

            //文档处理任务------------------------------------
            if (fileWorker.IsBusy && fileWorker.WorkerSupportsCancellation)
                fileWorker.CancelAsync();
            //矢量推送任务------------------------------------
            if (vectorWorker.IsBusy && vectorWorker.WorkerSupportsCancellation)
                vectorWorker.CancelAsync();
            //瓦片推送任务------------------------------------
            if (rasterWorker.IsBusy && rasterWorker.WorkerSupportsCancellation)
                rasterWorker.CancelAsync();

            //窗体淡出
            var fadeOut = new Timer
            {
                Site = null,
                Tag = null,
                Enabled = false,
                Interval = 16 //间隔（毫秒）
            };
            fadeOut.Tick += (_, _) =>
            {
                if (Opacity <= 0)
                {
                    fadeOut.Stop();
                    Close();
                }
                else
                    this.Opacity -= 0.1;
            };
            fadeOut.Start();
            if (this.Opacity == 0)
                e.Cancel = false;
        }

        private void InitializeBackgroundWorker()
        {
            //文档处理任务------------------------------------

            fileWorker.DoWork += delegate (object sender, DoWorkEventArgs e)
            {
                e.Result = FileWorkStart(sender as BackgroundWorker, e);
            };
            fileWorker.RunWorkerCompleted += FileWorkCompleted;
            fileWorker.ProgressChanged += FileWorkProgress;

            //矢量推送任务------------------------------------

            vectorWorker.DoWork += delegate (object sender, DoWorkEventArgs e)
            {
                e.Result = VectorWorkStart(sender as BackgroundWorker, e);
            };
            vectorWorker.RunWorkerCompleted += VectorWorkCompleted;
            vectorWorker.ProgressChanged += VectorWorkProgress;

            //瓦片推送任务------------------------------------

            rasterWorker.DoWork += delegate (object sender, DoWorkEventArgs e)
            {
                e.Result = RasterWorkStart(sender as BackgroundWorker, e);
            };
            rasterWorker.RunWorkerCompleted += RasterWorkCompleted;
            rasterWorker.ProgressChanged += RasterWorkProgress;
        }

        /*
            _/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/
                                    Loading 异步可视化加载器
            _/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/

            用法：
                1、创建Loading对象
                    Loading.setBar(ProgressBar XXX);
                2、加载效果
                    Loading.run(); //开启加载效果
                    Loading.run(null); //关闭已追加的全部加载效果
                    Loading.run(false); //仅关闭当前加载效果
        */
        private class LoadingBar
        {
            private int _count;

            private readonly ProgressBar _bar;

            /// <summary>
            /// 构造函数
            /// </summary>
            /// <param name="bar">ProgressBar 类型对象</param>
            public LoadingBar(ProgressBar bar)
            {
                _bar = bar;
                _bar.Invoke(
                    new Action(
                        () =>
                        {
                            _bar.MarqueeAnimationSpeed = 0;
                            _bar.Refresh();
                        }
                    )
                );
            }

            /// <summary>
            /// 开启或关闭等待效果
            /// </summary>
            /// <param name="onOff"></param>
            public void Run(bool? onOff = true)
            {
                if (onOff == true)
                {
                    _count++;
                    if (_count == 1)
                    {
                        _bar.Invoke(
                            new Action(
                                () =>
                                {
                                    _bar.MarqueeAnimationSpeed = 1;
                                    _bar.Refresh();
                                }
                            )
                        );
                    }
                }
                else
                {
                    if (onOff == false)
                    {
                        _count--;
                        if (_count < 0)
                            _count = 0;
                    }
                    else
                        _count = 0;

                    if (_count == 0)
                    {
                        _bar.Invoke(
                            new Action(
                                () =>
                                {
                                    _bar.MarqueeAnimationSpeed = 0;
                                    _bar.Refresh();
                                }
                            )
                        );
                    }
                }
            }
        }

        /// <summary>
        /// 窗体控件事件响应函数（暂支持：RadioButton、CheckBox、ComboBox、TextBox、TabControl）
        /// </summary>
        /// <param name="sender">事件源</param>
        /// <param name="e">事件参数</param>
        private void FormEventChanged(object sender, EventArgs e = null)
        {
            switch (sender)
            {
                case TextBox textBox:
                    RegEdit.Setkey(textBox.Name, textBox.Text);
                    break;
                case RadioButton radioButton:
                    RegEdit.Setkey(radioButton.Name, $"{radioButton.Checked}");
                    break;
                case CheckBox checkBox:
                    RegEdit.Setkey(checkBox.Name, $"{checkBox.Checked}");
                    break;
                case ComboBox comboBox:
                    RegEdit.Setkey(comboBox.Name, comboBox.Text);
                    break;
                case TabControl tabControl:
                    RegEdit.Setkey(tabControl.Name, $"{tabControl.SelectedIndex}");
                    break;
            }
        }

        private void FileRun_Click(object sender, EventArgs e)
        {
            if (fileWorker.IsBusy || vectorWorker.IsBusy || rasterWorker.IsBusy)
                return;

            ogcCard.Enabled =
            FileRun.Enabled = false;
            statusProgress.Visible = true;

            fileWorker.RunWorkerAsync
            (
                 (SourcePath: vectorSourceFile.Text, TargetPath: vectorTargetFile.Text, SaveAsFormat: SaveAsFormat.Text)
            );

            //自动调用 FileWorkStart 函数
        }

        private string FileWorkStart(BackgroundWorker fileBackgroundWorker, DoWorkEventArgs e)
        {
            if (fileBackgroundWorker.CancellationPending)
            {
                e.Cancel = true;
                return "Pause...";
            }

            var argument = ((string SourcePath, string TargetPath, string SaveAsFormat)?)e.Argument;
            if (argument != null)
            {
                //files --- D:\test\mapgis\LINE.WL|D:\test\mapgis\POINT.WT|D:\test\mapgis\POLYGON.WP
                var sourceFiles = Regex.Split(argument.Value.SourcePath, @"[\s]*[|][\s]*");

                var targetPath = argument.Value.TargetPath; //folder --- D:\tmp or file --- D:\tmp\wer.xml
                var isDirectory = Path.GetExtension(targetPath) == string.Empty; //there is no 100% way to distinguish a folder from a file by path alone. 
                var saveAsFormat = argument.Value.SaveAsFormat;
                /* Argument.Value.SaveAsFormat
                    JSON(*.json)
                    ESRI ShapeFile(*.shp)
                    GeoJSON(*.geojson)
                    GoogleEarth(*.kml)
                    Gml(*.gml)
                    GeositeXML(*.xml)             
                    ""
                 */
                var targetType = isDirectory
                    ? Regex.IsMatch(saveAsFormat, @"\(\*.json\)", RegexOptions.IgnoreCase)
                        ? ".json"
                        : Regex.IsMatch(saveAsFormat, @"\(\*.shp\)", RegexOptions.IgnoreCase)
                            ? ".shp"
                            : Regex.IsMatch(saveAsFormat, @"\(\*.geojson\)", RegexOptions.IgnoreCase)
                                ? ".geojson"
                                : Regex.IsMatch(saveAsFormat, @"\(\*.kml\)", RegexOptions.IgnoreCase)
                                    ? ".kml"
                                    : Regex.IsMatch(saveAsFormat, @"\(\*.gml\)", RegexOptions.IgnoreCase)
                                        ? ".gml"
                                        : ".xml"
                    : Path.GetExtension(targetPath).ToLower();

                try
                {
                    for (var i = 0; i < sourceFiles.Length; i++)
                    {
                        var sourceFile = sourceFiles[i];

                        string targetFile;
                        if (isDirectory)
                        {
                            var postfix = 0;
                            do
                            {
                                targetFile = Path.Combine(
                                    targetPath,
                                    Path.GetFileNameWithoutExtension(sourceFile) +
                                    (postfix == 0 ? "" : $"({postfix})") + targetType);
                                if (!File.Exists(targetFile))
                                    break;
                                postfix++;
                            } while (true);
                        }
                        else
                            targetFile = targetPath;

                        var fileType = Path.GetExtension(sourceFile)?.ToLower();
                        switch (fileType)
                        {
                            case ".shp":
                                {
                                    var codePage = ShapeFile.ShapeFile.GetDbfCodePage(
                                        Path.Combine(
                                            Path.GetDirectoryName(sourceFile) ?? "",
                                            Path.GetFileNameWithoutExtension(sourceFile) + ".dbf")
                                    );

                                    using var shapeFile = new ShapeFile.ShapeFile();
                                    var localI = i + 1;
                                    shapeFile.onGeositeEvent += delegate (object _, GeositeEventArgs thisEvent)
                                {
                                    object userStatus = !string.IsNullOrWhiteSpace(thisEvent.message)
                                        ? sourceFiles.Length > 1
                                            ? $"[{localI}/{sourceFiles.Length}] {thisEvent.message}"
                                            : thisEvent.message
                                        : null;

                                    fileBackgroundWorker.ReportProgress(thisEvent.progress ?? -1,
                                        userStatus ?? string.Empty);
                                };

                                    shapeFile.Open(sourceFile, codePage);
                                    switch (targetType)
                                    {
                                        case ".shp":
                                            shapeFile.Export(targetFile, "shapefile");
                                            break;
                                        case ".xml":
                                        case ".kml":
                                        case ".gml":
                                            if (isDirectory)
                                            {
                                                shapeFile.Export(
                                                    targetFile,
                                                    Path.GetExtension(targetFile).ToLower().Substring(1),
                                                    ConsoleIO.FilePathToXPath(sourceFile)
                                                    //, null
                                                );
                                            }
                                            else
                                            {
                                                string treePathString = null;
                                                XElement description = null;
                                                var canDo = true;
                                                if (!_noPromptLayersBuilder)
                                                {
                                                    var getTreeLayers = new LayersBuilder(new FileInfo(sourceFile).FullName);
                                                    getTreeLayers.ShowDialog();
                                                    if (getTreeLayers.Ok)
                                                    {
                                                        treePathString = getTreeLayers.TreePathString;
                                                        description = getTreeLayers.Description;
                                                        _noPromptLayersBuilder = getTreeLayers.DonotPrompt;
                                                    }
                                                    else
                                                        canDo = false;
                                                }
                                                else
                                                {
                                                    treePathString = ConsoleIO.FilePathToXPath(new FileInfo(sourceFile).FullName);
                                                }
                                                if (canDo)
                                                {
                                                    shapeFile.Export(
                                                        targetFile,
                                                        Path.GetExtension(targetFile).ToLower().Substring(1),
                                                        treePathString,
                                                        description
                                                    );
                                                }
                                            }

                                            break;
                                        case ".geojson":
                                            shapeFile.Export(
                                                targetFile
                                            );
                                            break;
                                    }
                                }
                                break;
                            case ".mpj":
                                {
                                    var mapgisProject = new MapGis.MapGisProject();
                                    var localI = i + 1;
                                    mapgisProject.onGeositeEvent += delegate (object _, GeositeEventArgs thisEvent)
                                {
                                    object userStatus = !string.IsNullOrWhiteSpace(thisEvent.message)
                                        ? sourceFiles.Length > 1
                                            ? $"[{localI}/{sourceFiles.Length}] {thisEvent.message}"
                                            : thisEvent.message
                                        : null;

                                    fileBackgroundWorker.ReportProgress(thisEvent.progress ?? -1,
                                        userStatus ?? string.Empty);
                                };
                                    mapgisProject.Open(sourceFile);
                                    mapgisProject.Export(targetFile);
                                }

                                break;
                            case ".txt":
                            case ".csv":
                                try
                                {
                                    var freeTextFields = fileType == ".txt"
                                        ? TXT.GetFieldNames(sourceFile)
                                        : CSV.GetFieldNames(sourceFile);
                                    if (freeTextFields.Length == 0)
                                        throw new Exception("No valid fields found");

                                    string coordinateFieldName;
                                    if (freeTextFields.Any(f => f == "_position_"))
                                        coordinateFieldName = "_position_";
                                    else
                                    {
                                        if (isDirectory)
                                        {
                                            coordinateFieldName = "_position_";
                                        }
                                        else
                                        {
                                            var freeTextFieldsForm = new FreeTextField(freeTextFields);
                                            freeTextFieldsForm.ShowDialog();
                                            coordinateFieldName = freeTextFieldsForm.Ok ? freeTextFieldsForm.CoordinateFieldName : null;
                                        }
                                    }

                                    if (coordinateFieldName != null)
                                    {
                                        //多态性：将派生类对象赋予基类对象
                                        FreeText.FreeText freeText = fileType == ".txt"
                                            ? new TXT(CoordinateFieldName: coordinateFieldName)
                                            : new CSV(CoordinateFieldName: coordinateFieldName);
                                        var localI = i + 1;
                                        freeText.onGeositeEvent +=
                                            delegate (object _, GeositeEventArgs thisEvent)
                                            {
                                                object userStatus = !string.IsNullOrWhiteSpace(thisEvent.message)
                                                    ? sourceFiles.Length > 1
                                                        ? $"[{localI}/{sourceFiles.Length}] {thisEvent.message}"
                                                        : thisEvent.message
                                                    : null;

                                                fileBackgroundWorker.ReportProgress(thisEvent.progress ?? -1,
                                                    userStatus ?? string.Empty);
                                            };
                                        freeText.Open(sourceFile);

                                        switch (Path.GetExtension(targetFile)?.ToLower())
                                        {
                                            case ".shp":
                                                freeText.Export(targetFile, "shapefile");
                                                break;
                                            case ".geojson":
                                                freeText.Export(
                                                    targetFile
                                                );
                                                break;
                                            case ".xml":
                                            case ".kml":
                                            case ".gml":
                                                if (isDirectory)
                                                {
                                                    freeText.Export(
                                                        targetFile,
                                                        Path.GetExtension(targetFile).ToLower().Substring(1),
                                                        ConsoleIO.FilePathToXPath(sourceFile)
                                                        //, null
                                                    );
                                                }
                                                else
                                                {
                                                    string treePathString = null;
                                                    XElement description = null;
                                                    var canDo = true;
                                                    if (!_noPromptLayersBuilder)
                                                    {
                                                        var getTreeLayers = new LayersBuilder(new FileInfo(sourceFile).FullName);
                                                        getTreeLayers.ShowDialog();
                                                        if (getTreeLayers.Ok)
                                                        {
                                                            treePathString = getTreeLayers.TreePathString;
                                                            description = getTreeLayers.Description;
                                                            _noPromptLayersBuilder = getTreeLayers.DonotPrompt;
                                                        }
                                                        else
                                                            canDo = false;
                                                    }
                                                    else
                                                    {
                                                        treePathString = ConsoleIO.FilePathToXPath(new FileInfo(sourceFile).FullName);
                                                    }
                                                    if (canDo)
                                                    {
                                                        freeText.Export(
                                                            targetFile,
                                                            Path.GetExtension(targetFile).ToLower().Substring(1),
                                                            treePathString,
                                                            description
                                                        );
                                                    }
                                                }

                                                break;
                                        }
                                    }
                                }
                                catch
                                {
                                    //
                                }

                                break;
                            case ".kml":
                                using (var kml = new GeositeXml.GeositeXml())
                                {
                                    var localI = i + 1;
                                    kml.onGeositeEvent += delegate (object _, GeositeEventArgs thisEvent)
                                    {
                                        object userStatus = !string.IsNullOrWhiteSpace(thisEvent.message)
                                            ? sourceFiles.Length > 1
                                                ? $"[{localI}/{sourceFiles.Length}] {thisEvent.message}"
                                                : thisEvent.message
                                            : null;

                                        fileBackgroundWorker.ReportProgress(thisEvent.progress ?? -1,
                                            userStatus ?? string.Empty);
                                    };

                                    if (isDirectory)
                                    {
                                        switch (Path.GetExtension(targetFile)?.ToLower())
                                        {
                                            case ".xml":
                                                kml.KmlToGeositeXml(
                                                    sourceFile, 
                                                    targetFile
                                                    //, null
                                                    );
                                                break;
                                            case ".shp":
                                                {
                                                    var geositeXml = kml.KmlToGeositeXml(
                                                        sourceFile
                                                        , null
                                                        //, null
                                                        );
                                                    kml.GeositeXmlToShp(
                                                        geositeXml.Root,
                                                        targetFile
                                                    );
                                                }
                                                break;
                                        }
                                    }
                                    else
                                    {
                                        XElement description = null;
                                        var canDo = true;
                                        if (!_noPromptLayersBuilder)
                                        {
                                            var getTreeLayers = new LayersBuilder();
                                            getTreeLayers.ShowDialog();
                                            if (getTreeLayers.Ok)
                                            {
                                                description = getTreeLayers.Description;
                                                _noPromptLayersBuilder = getTreeLayers.DonotPrompt;
                                            }
                                            else
                                                canDo = false;
                                        }

                                        if (canDo)
                                        {
                                            switch (Path.GetExtension(targetFile)?.ToLower())
                                            {
                                                case ".xml":
                                                    kml.KmlToGeositeXml(sourceFile, targetFile, description);
                                                    break;
                                                case ".shp":
                                                    {
                                                        var geositeXml = kml.KmlToGeositeXml(sourceFile, null, description);
                                                        kml.GeositeXmlToShp(
                                                            geositeXml.Root,
                                                            targetFile
                                                        );
                                                    }
                                                    break;
                                            }
                                        }
                                    }
                                }
                                break;
                            case ".xml":
                                using (var xml = new GeositeXml.GeositeXml())
                                {
                                    var localI = i + 1;
                                    xml.onGeositeEvent += delegate (object _, GeositeEventArgs thisEvent)
                                    {
                                        object userStatus = !string.IsNullOrWhiteSpace(thisEvent.message)
                                            ? sourceFiles.Length > 1
                                                ? $"[{localI}/{sourceFiles.Length}] {thisEvent.message}"
                                                : thisEvent.message
                                            : null;

                                        fileBackgroundWorker.ReportProgress(thisEvent.progress ?? -1,
                                            userStatus ?? string.Empty);
                                    };
                                    if (isDirectory)
                                    {
                                        switch (Path.GetExtension(targetFile)?.ToLower())
                                        {
                                            case ".kml":
                                                xml.GeositeXmlToKml(
                                                    sourceFile
                                                    , targetFile
                                                    //, null
                                                    );
                                                break;
                                            case ".xml":
                                                xml.GeositeXmlToGeositeXml(
                                                    sourceFile
                                                    , targetFile
                                                    //, null
                                                    );
                                                break;
                                            case ".gml":
                                                xml.GeositeXmlToGml(
                                                    sourceFile
                                                    , targetFile
                                                    //, null
                                                    );
                                                break;
                                            case ".geojson":
                                                xml.GeositeXmlToGeoJson(
                                                    sourceFile
                                                    , targetFile
                                                    //, null
                                                    );
                                                break;
                                            case ".shp":
                                                {
                                                    var geositeXml = xml.GeositeXmlToGeositeXml(
                                                        sourceFile
                                                        , null
                                                        //, null
                                                        );
                                                    xml.GeositeXmlToShp(
                                                        geositeXml.Root,
                                                        targetFile
                                                    );
                                                }
                                                break;
                                        }
                                    }
                                    else
                                    {
                                        XElement description = null;
                                        var canDo = true;
                                        if (!_noPromptLayersBuilder)
                                        {
                                            var getTreeLayers = new LayersBuilder();
                                            getTreeLayers.ShowDialog();
                                            if (getTreeLayers.Ok)
                                            {
                                                description = getTreeLayers.Description;
                                                _noPromptLayersBuilder = getTreeLayers.DonotPrompt;
                                            }
                                            else
                                            {
                                                canDo = false;
                                            }
                                        }

                                        if (canDo)
                                            switch (Path.GetExtension(targetFile)?.ToLower())
                                            {
                                                case ".kml":
                                                    xml.GeositeXmlToKml(sourceFile, targetFile,
                                                        description);
                                                    break;
                                                case ".xml":
                                                    xml.GeositeXmlToGeositeXml(sourceFile, targetFile,
                                                        description);
                                                    break;
                                                case ".gml":
                                                    xml.GeositeXmlToGml(sourceFile, targetFile,
                                                        description);
                                                    break;
                                                case ".geojson":
                                                    xml.GeositeXmlToGeoJson(sourceFile, targetFile,
                                                        description);
                                                    break;
                                                case ".shp":
                                                    {
                                                        var geositeXml = xml.GeositeXmlToGeositeXml(sourceFile, null,
                                                            description);
                                                        xml.GeositeXmlToShp(
                                                            geositeXml.Root,
                                                            targetFile
                                                        );
                                                    }
                                                    break;
                                            }
                                    }
                                }

                                break;
                            case ".geojson":
                                using (var geoJsonObject = new GeositeXml.GeositeXml())
                                {
                                    var localI = i + 1;
                                    geoJsonObject.onGeositeEvent += delegate (object _, GeositeEventArgs thisEvent)
                                    {
                                        object userStatus = !string.IsNullOrWhiteSpace(thisEvent.message)
                                            ? sourceFiles.Length > 1
                                                ? $"[{localI}/{sourceFiles.Length}] {thisEvent.message}"
                                                : thisEvent.message
                                            : null;

                                        fileBackgroundWorker.ReportProgress(thisEvent.progress ?? -1,
                                            userStatus ?? string.Empty);
                                    };
                                    if (isDirectory)
                                    {
                                        switch (Path.GetExtension(targetFile)?.ToLower())
                                        {
                                            case ".xml":
                                                geoJsonObject.GeoJsonToGeositeXml(
                                                    sourceFile,
                                                    targetFile,
                                                    ConsoleIO.FilePathToXPath(sourceFile)
                                                    //, null
                                                );
                                                break;
                                            case ".shp":
                                                {
                                                    var geositeXmlStringBuilder = new StringBuilder();

                                                    geoJsonObject.GeoJsonToGeositeXml(
                                                        sourceFile,
                                                        geositeXmlStringBuilder,
                                                        ConsoleIO.FilePathToXPath(sourceFile)
                                                        //, null
                                                    );

                                                    var geositeXml = XElement.Parse(geositeXmlStringBuilder.ToString());
                                                    geoJsonObject.GeositeXmlToShp(
                                                        geositeXml,
                                                        targetFile
                                                    );
                                                }
                                                break;
                                        }
                                    }
                                    else
                                    {
                                        string treePathString = null;
                                        XElement description = null;
                                        var canDo = true;
                                        if (!_noPromptLayersBuilder)
                                        {
                                            var getTreeLayers = new LayersBuilder(new FileInfo(sourceFile).FullName);
                                            getTreeLayers.ShowDialog();
                                            if (getTreeLayers.Ok)
                                            {
                                                treePathString = getTreeLayers.TreePathString;
                                                description = getTreeLayers.Description;
                                                _noPromptLayersBuilder = getTreeLayers.DonotPrompt;
                                            }
                                            else
                                                canDo = false;
                                        }
                                        else
                                        {
                                            treePathString = ConsoleIO.FilePathToXPath(new FileInfo(sourceFile).FullName);
                                        }
                                        if (canDo)
                                        {
                                            switch (Path.GetExtension(targetFile)?.ToLower())
                                            {
                                                case ".xml":
                                                    geoJsonObject.GeoJsonToGeositeXml(
                                                        sourceFile,
                                                        targetFile,
                                                        treePathString,
                                                        description
                                                    );
                                                    break;
                                                case ".shp":
                                                    {
                                                        var geositeXmlStringBuilder = new StringBuilder();

                                                        geoJsonObject.GeoJsonToGeositeXml(
                                                            sourceFile,
                                                            geositeXmlStringBuilder,
                                                            treePathString,
                                                            description
                                                        );

                                                        var geositeXml = XElement.Parse(geositeXmlStringBuilder.ToString());
                                                        geoJsonObject.GeositeXmlToShp(
                                                            geositeXml,
                                                            targetFile
                                                        );
                                                    }
                                                    break;
                                            }
                                        }
                                    }
                                }

                                break;
                            //case ".wt":
                            //case ".wl":
                            //case ".wp":
                            default:
                                using (var mapgis = new MapGis.MapGisFile())
                                {
                                    var localI = i + 1;
                                    mapgis.onGeositeEvent += delegate (object _, GeositeEventArgs thisEvent)
                                    {
                                        object userStatus = !string.IsNullOrWhiteSpace(thisEvent.message)
                                            ? sourceFiles.Length > 1
                                                ? $"[{localI}/{sourceFiles.Length}] {thisEvent.message}"
                                                : thisEvent.message
                                            : null;

                                        fileBackgroundWorker.ReportProgress(thisEvent.progress ?? -1,
                                            userStatus ?? string.Empty);
                                    };

                                    mapgis.Open(sourceFile);

                                    switch (Path.GetExtension(targetFile)?.ToLower())
                                    {
                                        case ".shp":
                                            mapgis.Export(targetFile, "shapefile");
                                            break;
                                        case ".xml":
                                        case ".kml":
                                        case ".gml":
                                            if (isDirectory)
                                            {
                                                mapgis.Export(
                                                    targetFile,
                                                    Path.GetExtension(targetFile).ToLower().Substring(1),
                                                    ConsoleIO.FilePathToXPath(sourceFile)
                                                    //, null
                                                );
                                            }
                                            else
                                            {
                                                string treePathString = null;
                                                XElement description = null;
                                                var canDo = true;
                                                if (!_noPromptLayersBuilder)
                                                {
                                                    var getTreeLayers = new LayersBuilder(new FileInfo(sourceFile).FullName);
                                                    getTreeLayers.ShowDialog();
                                                    if (getTreeLayers.Ok)
                                                    {
                                                        treePathString = getTreeLayers.TreePathString;
                                                        description = getTreeLayers.Description;
                                                        _noPromptLayersBuilder = getTreeLayers.DonotPrompt;
                                                    }
                                                    else
                                                        canDo = false;
                                                }
                                                else
                                                {
                                                    treePathString = ConsoleIO.FilePathToXPath(new FileInfo(sourceFile).FullName);
                                                }
                                                if (canDo)
                                                {
                                                    mapgis.Export(
                                                        targetFile,
                                                        Path.GetExtension(targetFile).ToLower().Substring(1),
                                                        treePathString,
                                                        description
                                                    );
                                                }
                                            }
                                            break;
                                        case ".geojson":
                                            mapgis.Export(
                                                targetFile
                                            );
                                            break;
                                    }
                                }
                                break;
                        }
                    }
                }
                catch (Exception error)
                {
                    return error.Message;
                }
            }
            return null;
        }

        private void FileWorkProgress(object sender, ProgressChangedEventArgs e)
        {
            //e.code 状态码（0/null=预处理阶段；1=正在处理阶段；200=收尾阶段；400=异常信息）
            //e.ProgressPercentage 进度值（介于0~100之间，仅当code=1时有效）
            var userState = (string)e.UserState;
            var progressPercentage = e.ProgressPercentage;
            var pv = statusProgress.Value = progressPercentage is >= 0 and <= 100 ? progressPercentage : 0;
            statusText.Text = userState;
            //实时刷新界面进度杆会明显降低执行速度！
            //下面采取每10个要素刷新一次 
            if (pv % 10 == 0)
                statusBar.Refresh();
        }

        private void FileWorkCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            statusProgress.Visible = false;

            if (e.Error != null)
                MessageBox.Show(e.Error.Message, @"Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            else if (e.Cancelled)
                statusText.Text = @"Suspended!";
            else if (e.Result != null)
                statusText.Text = (string)e.Result;

            FileRun.Enabled = true;
            ogcCard.Enabled = true;
        }

        private void vectorOpenFile_Click(object sender, EventArgs e)
        {
            var key = vectorOpenFile.Name;
            if (!int.TryParse(RegEdit.Getkey(key), out var filterIndex))
                filterIndex = 0;

            var path = key + "_path";
            var oldPath = RegEdit.Getkey(path);

            var openFileDialog = new OpenFileDialog
            {
                Filter = @"MapGIS|*.wt;*.wl;*.wp|MapGIS|*.mpj|ShapeFile|*.shp|Excel Tab Delimited|*.txt|Excel Comma Delimited|*.csv|GoogleEarth(*.kml)|*.kml|GeositeXML|*.xml|GeoJson|*.geojson",
                FilterIndex = filterIndex,
                Multiselect = true
            };
            if (Directory.Exists(oldPath))
                openFileDialog.InitialDirectory = oldPath;
            SaveAsFormat.Text = string.Empty;
            SaveAsFormat.Items.Clear();
            vectorTargetFile.Text = string.Empty;
            SaveAsFormat.Enabled = false;

            try
            {
                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    RegEdit.Setkey(key, $"{openFileDialog.FilterIndex}");
                    RegEdit.Setkey(path, Path.GetDirectoryName(openFileDialog.FileName));
                    vectorSourceFile.Text = string.Join("|", openFileDialog.FileNames);

                    var vectorSourceFiles = Regex.Split(vectorSourceFile.Text.Trim(), @"[\s]*[|][\s]*").Where(s => !string.IsNullOrWhiteSpace(s)).ToArray();
                    var vectorSourceFileCount = vectorSourceFiles.Length; // >= 0 
                    if (vectorSourceFileCount > 1)
                    {
                        SaveAsFormat.Enabled = true;
                        switch (openFileDialog.FilterIndex)
                        {
                            case 2: //MapGIS|*.mpj
                                SaveAsFormat.Items.Add(@"JSON(*.json)");
                                SaveAsFormat.SelectedIndex = 0;
                                break;
                            case 1: //MapGIS|*.wt;*.wl;*.wp
                            case 3: //ShapeFile|*.shp
                            case 4: //Excel Tab Delimited|*.txt
                            case 5: //Excel Comma Delimited|*.csv
                            case 7: //GeositeXML|*.xml
                                SaveAsFormat.Items.Add(@"ESRI ShapeFile(*.shp)");
                                SaveAsFormat.Items.Add(@"GeoJSON(*.geojson)");
                                SaveAsFormat.Items.Add(@"GoogleEarth(*.kml)");
                                SaveAsFormat.Items.Add(@"Gml(*.gml)");
                                SaveAsFormat.Items.Add(@"GeositeXML(*.xml)");
                                SaveAsFormat.SelectedIndex = 0;
                                break;
                            case 6: //GoogleEarth(*.kml)|*.kml
                            case 8: //GeoJson|*.geojson
                                SaveAsFormat.Items.Add(@"ESRI ShapeFile(*.shp)");
                                SaveAsFormat.Items.Add(@"GeositeXML(*.xml)");
                                SaveAsFormat.SelectedIndex = 0;
                                break;
                            default:
                                vectorSourceFile.Text = string.Empty;
                                break;
                        }
                    }
                }
                else
                {
                    vectorSourceFile.Text = string.Empty;
                }
            }
            catch (Exception error)
            {
                vectorSourceFile.Text = string.Empty;
                statusText.Text = error.Message;
            }

            FileCheck();
        }

        private void vectorSourceFile_TextChanged(object sender, EventArgs e)
        {
            vectorTargetFile.Text = string.Empty;
            SaveAsFormat.Text = string.Empty;
            SaveAsFormat.Text = string.Empty;
            SaveAsFormat.Items.Clear();
            SaveAsFormat.Enabled = false;
        }

        private void vectorSaveFile_Click(object sender, EventArgs e)
        {
            var vectorSourceFiles = Regex.Split(vectorSourceFile.Text.Trim(), @"[\s]*[|][\s]*").Where(s => !string.IsNullOrWhiteSpace(s)).ToArray();
            var vectorSourceFileCount = vectorSourceFiles.Length; // >= 0
            if (vectorSourceFileCount > 0)
            {
                var vectorSourceFileText = vectorSourceFiles[0];
                if (string.IsNullOrWhiteSpace(vectorSourceFileText))
                {
                    MessageBox.Show(@"Please select file[s] first", @"Tip", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                    return;
                }

                var key = vectorSaveFile.Name;
                var path = key + "_path";
                var oldPath = RegEdit.Getkey(path);

                if (vectorSourceFileCount == 1)
                {
                    var sourceFileExt = Path.GetExtension(vectorSourceFileText).ToLower();

                    int.TryParse(RegEdit.Getkey(key), out var filterIndex);

                    var saveFileDialog = new SaveFileDialog
                    {
                        Filter = sourceFileExt == ".geojson"
                            ? @"GeositeXML(*.xml)|*.xml|ESRI ShapeFile(*.shp)|*.shp"
                            : sourceFileExt == ".kml"
                                ? @"GeositeXML(*.xml)|*.xml|ESRI ShapeFile(*.shp)|*.shp"
                                : sourceFileExt == ".xml"
                                    ? @"ESRI ShapeFile(*.shp)|*.shp|GeoJSON(*.geojson)|*.geojson|GoogleEarth(*.kml)|*.kml|Gml(*.gml)|*.gml|GeositeXML(*.xml)|*.xml"
                                    : sourceFileExt == ".mpj"
                                        ? @"JSON(*.json)|*.json"
                                        : @"GeositeXML(*.xml)|*.xml|GeoJSON(*.geojson)|*.geojson|ESRI ShapeFile(*.shp)|*.shp|GoogleEarth(*.kml)|*.kml|Gml(*.gml)|*.gml",
                        FilterIndex = filterIndex
                    };
                    if (Directory.Exists(oldPath))
                        saveFileDialog.InitialDirectory = oldPath;
                    if (saveFileDialog.ShowDialog() == DialogResult.OK)
                    {
                        RegEdit.Setkey(key, $"{saveFileDialog.FilterIndex}");
                        RegEdit.Setkey(path, Path.GetDirectoryName(saveFileDialog.FileName));
                        vectorTargetFile.Text = saveFileDialog.FileName;
                    }
                    else
                    {
                        vectorTargetFile.Text = string.Empty;
                    }
                }
                else
                {
                    var openFolderDialog = new FolderBrowserDialog()
                    {
                        Description = @"Please select a destination folder",
                        ShowNewFolderButton = true
                        //, RootFolder = Environment.SpecialFolder.MyComputer
                    };
                    if (Directory.Exists(oldPath))
                        openFolderDialog.SelectedPath = oldPath;
                    if (openFolderDialog.ShowDialog() == DialogResult.OK)
                    {
                        RegEdit.Setkey(path, openFolderDialog.SelectedPath);
                        vectorTargetFile.Text = openFolderDialog.SelectedPath;
                    }
                }

                FileCheck();
            }
        }

        private void FileCheck()
        {
            statusText.Text = _getCopyright;
            FileRun.Enabled = !string.IsNullOrWhiteSpace(vectorSourceFile.Text) &&
                              !string.IsNullOrWhiteSpace(vectorTargetFile.Text);
            if (FileRun.Enabled)
                FileRun.Focus();
        }

        private void mapgisIcon_Click(object sender, EventArgs e)
        {
            var key = mapgisIcon.Name;
            var path = key + "_path";
            var oldPath = RegEdit.Getkey(path);
            if (!int.TryParse(RegEdit.Getkey(key), out var filterIndex))
                filterIndex = 0;

            var openFileDialog = new OpenFileDialog
            {
                Filter = @"MapGIS|*.wt;*.wl;*.wp|MapGIS|*.mpj",
                FilterIndex = filterIndex,
                Multiselect = true

            };
            if (Directory.Exists(oldPath))
                openFileDialog.InitialDirectory = oldPath;
            SaveAsFormat.Text = string.Empty;
            SaveAsFormat.Items.Clear();
            SaveAsFormat.Enabled = false;
            vectorTargetFile.Text = string.Empty;
            try
            {
                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    RegEdit.Setkey(key, $"{openFileDialog.FilterIndex}");
                    RegEdit.Setkey(path, Path.GetDirectoryName(openFileDialog.FileName));
                    vectorSourceFile.Text = string.Join("|", openFileDialog.FileNames);
                    var vectorSourceFiles = Regex.Split(vectorSourceFile.Text.Trim(), @"[\s]*[|][\s]*").Where(s => !string.IsNullOrWhiteSpace(s)).ToArray();
                    var vectorSourceFileCount = vectorSourceFiles.Length; // >= 0
                    if (vectorSourceFileCount > 1)
                    {
                        SaveAsFormat.Enabled = true;
                        switch (openFileDialog.FilterIndex)
                        {
                            case 2: //MapGIS|*.mpj
                                SaveAsFormat.Items.Add(@"JSON(*.json)");
                                SaveAsFormat.SelectedIndex = 0;
                                break;
                            case 1: //MapGIS|*.wt;*.wl;*.wp
                                SaveAsFormat.Items.Add(@"ESRI ShapeFile(*.shp)");
                                SaveAsFormat.Items.Add(@"GeoJSON(*.geojson)");
                                SaveAsFormat.Items.Add(@"GoogleEarth(*.kml)");
                                SaveAsFormat.Items.Add(@"Gml(*.gml)");
                                SaveAsFormat.Items.Add(@"GeositeXML(*.xml)");
                                SaveAsFormat.SelectedIndex = 0;
                                break;
                            default:
                                vectorSourceFile.Text = string.Empty;
                                break;
                        }
                    }
                }
                else
                {
                    vectorSourceFile.Text = string.Empty;
                }
            }
            catch (Exception error)
            {
                vectorSourceFile.Text = string.Empty;
                statusText.Text = error.Message;
            }

            FileCheck();
        }

        private void arcgisIcon_Click(object sender, EventArgs e)
        {
            var key = arcgisIcon.Name;
            var path = key + "_path";
            var oldPath = RegEdit.Getkey(path);

            var openFileDialog = new OpenFileDialog
            {
                Filter = @"ShapeFile|*.shp",
                FilterIndex = 0,
                Multiselect = true

            };
            if (Directory.Exists(oldPath))
            {
                openFileDialog.InitialDirectory = oldPath;
            }
            vectorTargetFile.Text = string.Empty;
            SaveAsFormat.Text = string.Empty;
            SaveAsFormat.Items.Clear();
            SaveAsFormat.Enabled = false;

            try
            {
                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    RegEdit.Setkey(path, Path.GetDirectoryName(openFileDialog.FileName));
                    vectorSourceFile.Text = string.Join("|", openFileDialog.FileNames);
                    var vectorSourceFiles = Regex.Split(vectorSourceFile.Text.Trim(), @"[\s]*[|][\s]*").Where(s => !string.IsNullOrWhiteSpace(s)).ToArray();
                    var vectorSourceFileCount = vectorSourceFiles.Length; // >= 0
                    if (vectorSourceFileCount > 1)
                    {
                        SaveAsFormat.Enabled = true;
                        switch (openFileDialog.FilterIndex)
                        {
                            case 1: //ShapeFile|*.shp
                                SaveAsFormat.Items.Add(@"ESRI ShapeFile(*.shp)");
                                SaveAsFormat.Items.Add(@"GeoJSON(*.geojson)");
                                SaveAsFormat.Items.Add(@"GoogleEarth(*.kml)");
                                SaveAsFormat.Items.Add(@"Gml(*.gml)");
                                SaveAsFormat.Items.Add(@"GeositeXML(*.xml)");
                                SaveAsFormat.SelectedIndex = 0;
                                break;
                            default:
                                vectorSourceFile.Text = string.Empty;
                                break;
                        }
                    }
                }
                else
                {
                    vectorSourceFile.Text = string.Empty;
                }
            }
            catch (Exception error)
            {
                vectorSourceFile.Text = string.Empty;
                statusText.Text = error.Message;
            }

            FileCheck();
        }

        private void tabtextIcon_Click(object sender, EventArgs e)
        {
            var key = tabtextIcon.Name;
            var path = key + "_path";
            var oldPath = RegEdit.Getkey(path);

            var openFileDialog = new OpenFileDialog
            {
                Filter = @"Textual format|*.txt;*.csv",
                FilterIndex = 0,
                Multiselect = true

            };
            if (Directory.Exists(oldPath))
            {
                openFileDialog.InitialDirectory = oldPath;
            }
            vectorTargetFile.Text = string.Empty;
            SaveAsFormat.Text = string.Empty;
            SaveAsFormat.Items.Clear();
            SaveAsFormat.Enabled = false;

            try
            {
                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    RegEdit.Setkey(path, Path.GetDirectoryName(openFileDialog.FileName));
                    vectorSourceFile.Text = string.Join("|", openFileDialog.FileNames);
                    var vectorSourceFiles = Regex.Split(vectorSourceFile.Text.Trim(), @"[\s]*[|][\s]*").Where(s => !string.IsNullOrWhiteSpace(s)).ToArray();
                    var vectorSourceFileCount = vectorSourceFiles.Length; // >= 0
                    if (vectorSourceFileCount > 1)
                    {
                        SaveAsFormat.Enabled = true;
                        switch (openFileDialog.FilterIndex)
                        {
                            case 1: //Excel Tab Delimited|*.txt Excel Comma Delimited|*.csv
                                SaveAsFormat.Items.Add(@"ESRI ShapeFile(*.shp)");
                                SaveAsFormat.Items.Add(@"GeoJSON(*.geojson)");
                                SaveAsFormat.Items.Add(@"GoogleEarth(*.kml)");
                                SaveAsFormat.Items.Add(@"Gml(*.gml)");
                                SaveAsFormat.Items.Add(@"GeositeXML(*.xml)");
                                SaveAsFormat.SelectedIndex = 0;
                                break;
                            default:
                                vectorSourceFile.Text = string.Empty;
                                break;
                        }
                    }
                }
                else
                    vectorSourceFile.Text = string.Empty;
            }
            catch (Exception error)
            {
                vectorSourceFile.Text = string.Empty;
                statusText.Text = error.Message;
            }

            FileCheck();
        }

        private void geojsonIcon_Click(object sender, EventArgs e)
        {
            var key = geojsonIcon.Name;
            var path = key + "_path";
            var oldPath = RegEdit.Getkey(path);

            var openFileDialog = new OpenFileDialog
            {
                Filter = @"GeoJson|*.geojson",
                FilterIndex = 0,
                Multiselect = true

            };
            if (Directory.Exists(oldPath)) 
                openFileDialog.InitialDirectory = oldPath;
            vectorTargetFile.Text = SaveAsFormat.Text = string.Empty;
            SaveAsFormat.Items.Clear();
            SaveAsFormat.Enabled = false;

            try
            {
                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    RegEdit.Setkey(path, Path.GetDirectoryName(openFileDialog.FileName));
                    vectorSourceFile.Text = string.Join("|", openFileDialog.FileNames);
                    var vectorSourceFiles = Regex.Split(vectorSourceFile.Text.Trim(), @"[\s]*[|][\s]*").Where(s => !string.IsNullOrWhiteSpace(s)).ToArray();
                    var vectorSourceFileCount = vectorSourceFiles.Length; // >= 0
                    if (vectorSourceFileCount > 1)
                    {
                        SaveAsFormat.Enabled = true;
                        switch (openFileDialog.FilterIndex)
                        {
                            case 1: //GeoJson|*.geojson
                                SaveAsFormat.Items.Add(@"ESRI ShapeFile(*.shp)");
                                SaveAsFormat.Items.Add(@"GeositeXML(*.xml)");
                                SaveAsFormat.SelectedIndex = 0;
                                break;
                            default:
                                vectorSourceFile.Text = string.Empty;
                                break;
                        }
                    }
                }
                else
                    vectorSourceFile.Text = string.Empty;
            }
            catch (Exception error)
            {
                vectorSourceFile.Text = string.Empty;
                statusText.Text = error.Message;
            }

            FileCheck();
        }

        private void geositeIcon_Click(object sender, EventArgs e)
        {
            var key = geositeIcon.Name;
            var path = key + "_path";
            var oldPath = RegEdit.Getkey(path);

            var openFileDialog = new OpenFileDialog
            {
                Filter = @"GeositeXML|*.xml",
                FilterIndex = 0,
                Multiselect = true

            };
            if (Directory.Exists(oldPath))
                openFileDialog.InitialDirectory = oldPath;
            vectorTargetFile.Text = SaveAsFormat.Text = string.Empty;
            SaveAsFormat.Items.Clear();
            SaveAsFormat.Enabled = false;

            try
            {
                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    RegEdit.Setkey(path, Path.GetDirectoryName(openFileDialog.FileName));
                    vectorSourceFile.Text = string.Join("|", openFileDialog.FileNames);
                    var vectorSourceFiles = Regex.Split(vectorSourceFile.Text.Trim(), @"[\s]*[|][\s]*").Where(s => !string.IsNullOrWhiteSpace(s)).ToArray();
                    var vectorSourceFileCount = vectorSourceFiles.Length; // >= 0
                    if (vectorSourceFileCount > 1)
                    {
                        SaveAsFormat.Enabled = true;
                        switch (openFileDialog.FilterIndex)
                        {
                            case 1: //GeositeXML|*.xml
                                SaveAsFormat.Items.Add(@"ESRI ShapeFile(*.shp)");
                                SaveAsFormat.Items.Add(@"GeoJSON(*.geojson)");
                                SaveAsFormat.Items.Add(@"GoogleEarth(*.kml)");
                                SaveAsFormat.Items.Add(@"Gml(*.gml)");
                                SaveAsFormat.Items.Add(@"GeositeXML(*.xml)");
                                SaveAsFormat.SelectedIndex = 0;
                                break;
                            default:
                                vectorSourceFile.Text = string.Empty;
                                break;
                        }
                    }
                }
                else
                    vectorSourceFile.Text = string.Empty;
            }
            catch (Exception error)
            {
                vectorSourceFile.Text = string.Empty;
                statusText.Text = error.Message;
            }

            FileCheck();
        }

        private void kmlIcon_Click(object sender, EventArgs e)
        {
            var key = kmlIcon.Name;
            var path = key + "_path";
            var oldPath = RegEdit.Getkey(path);

            var openFileDialog = new OpenFileDialog
            {
                Filter = @"GoogleEarth(*.kml)|*.kml",
                FilterIndex = 0,
                Multiselect = true

            };
            if (Directory.Exists(oldPath))
                openFileDialog.InitialDirectory = oldPath;
            vectorTargetFile.Text = SaveAsFormat.Text = string.Empty;
            SaveAsFormat.Items.Clear();
            SaveAsFormat.Enabled = false;

            try
            {
                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    RegEdit.Setkey(path, Path.GetDirectoryName(openFileDialog.FileName));
                    vectorSourceFile.Text = string.Join("|", openFileDialog.FileNames);
                    var vectorSourceFiles = Regex.Split(vectorSourceFile.Text.Trim(), @"[\s]*[|][\s]*").Where(s => !string.IsNullOrWhiteSpace(s)).ToArray();
                    var vectorSourceFileCount = vectorSourceFiles.Length; // >= 0
                    if (vectorSourceFileCount > 1)
                    {
                        SaveAsFormat.Enabled = true;
                        switch (openFileDialog.FilterIndex)
                        {
                            case 1: //GoogleEarth(*.kml)|*.kml
                                SaveAsFormat.Items.Add(@"ESRI ShapeFile(*.shp)");
                                SaveAsFormat.Items.Add(@"GeositeXML(*.xml)");
                                SaveAsFormat.SelectedIndex = 0;
                                break;
                            default:
                                vectorSourceFile.Text = string.Empty;
                                break;
                        }
                    }
                }
                else
                    vectorSourceFile.Text = string.Empty;
            }
            catch (Exception error)
            {
                vectorSourceFile.Text = string.Empty;
                statusText.Text = error.Message;
            }

            FileCheck();
        }

        private void GeositeServer_LinkChanged(object sender, EventArgs e)
        {
            GeositeServerLink.BackgroundImage = Properties.Resources.link;
            _clusterUser.status = false;

            deleteForest.Enabled = false;
            GeositeServerName.Text = "";
            GeositeServerPort.Text = "";

            dataGridPanel.Enabled = false;
            PostgresRun.Enabled = false;
            _postgreSqlConnection = false;

            statusText.Text = _getCopyright;
            FormEventChanged(sender);
        }

        private void GeositeServerLink_Click(object sender, EventArgs e)
        {
            var serverUrl = GeositeServerUrl.Text?.Trim();
            var serverUser = GeositeServerUser.Text?.Trim();
            var serverPassword = GeositeServerPassword.Text?.Trim();

            if (string.IsNullOrWhiteSpace(serverUrl) || string.IsNullOrWhiteSpace(serverUser) || string.IsNullOrWhiteSpace(serverPassword))
            {
                statusText.Text = @"Connection parameters should not be blank";
                return;
            }

            /*
                出于安全考虑，数据库连接能否成功，取决于（GeositeServer）管理员在服务器端（appsettings.json）如何配置，样例如下：             
                "clusterUser": [
                    {
                        "name": "用户名",
                        "administrator": true, //是否具备管理员权限
                        "forest": -1 //森林序号，应小于0
                    }
                ]
             */
            _loading.Run();

            statusText.Text = @"Connecting ...";
            GeositeServerLink.BackgroundImage = Properties.Resources.link;
            databasePanel.Enabled =
            deleteForest.Enabled =
            _clusterUser.status =
            dataGridPanel.Enabled =
            PostgresRun.Enabled =
            _postgreSqlConnection = false;

            var task = new Func<(string Message, string Host, int Port, bool Administrator, string DatabaseSize)>(() =>
            {
                var userX = GetClusterUserX(serverUrl, serverUser, serverPassword);
                /*  返回样例：
                    <User>
                        <Servers>
                        <Server> 
                            <Host></Host>
                            <Error></Error>
                            <Username></Username>
                            <Password></Password>
                            <Database Size="? MB"></Database>
                            <Other></Other>
                            <CommandTimeout></CommandTimeout>
                            <Port></Port>
                            <Pooling></Pooling>
                            <LoadBalanceHosts></LoadBalanceHosts>
                            <TargetSessionAttributes></TargetSessionAttributes>
                        </Server>
                        </Servers>
                        <Forest MachineName="" OSVersion="" ProcessorCount="" Administrator="False/True"></Forest>
                    </User>             
                 */
                string errorMessage = null;
                string host = null;
                var port = -1;
                var administrator = false;
                string databaseSize = null;

                if (userX != null)
                {
                    var server = userX.Element("Servers")?.Element("Server");
                    host = server?.Element("Host")?.Value.Trim();

                    if (!int.TryParse(server?.Element("Port")?.Value.Trim(), out port))
                        port = 5432;

                    var databaseX = server?.Element("Database");
                    var database = databaseX?.Value.Trim();
                    databaseSize = databaseX?.Attribute("Size")?.Value;
                    var username = server?.Element("Username")?.Value.Trim();
                    var password = server?.Element("Password")?.Value.Trim();

                    //<Forest MachineName="" OSVersion="Microsoft Windows NT 10.0.19042.0" ProcessorCount=""></Forest>
                    var forestX = userX.Element("Forest");
                    if (!int.TryParse(forestX?.Value.Trim(), out var forest))
                        forest = -1;

                    if (!bool.TryParse(forestX?.Attribute("Administrator")?.Value.Trim() ?? "false", out administrator))
                        administrator = false;

                    var checkGeositeServer =
                        PostgreSqlHelper.Connection(
                            host,
                            port,
                            database,
                            username,
                            password,
                            "forest,tree,branch,leaf" //顺便检查一下这四张表是否存在
                        );
                    //PostgreSQL连接标志
                    //0：连接成功；
                    //-1：PG未安装或者连接参数不正确；
                    //-2：PG版本太低；
                    //1：指定的数据库不存在；
                    //2：数据库同名但数据表不符合要求
                    switch (checkGeositeServer.flag)
                    {
                        case -1:
                        case -2:
                        case 2:
                            _clusterUser.status = false;
                            errorMessage = checkGeositeServer.Message;
                            break;
                        case 1:
                            _clusterUser.status = false;
                            var processorCount = int.Parse(forestX?.Attribute("ProcessorCount")?.Value ?? "1");
                            if (PostgreSqlHelper.NonQuery($"CREATE DATABASE {database} WITH OWNER = {username};", pooling: false, postgres: true) != null)
                            {
                                if ((long)PostgreSqlHelper.Scalar("SELECT count(*) FROM pg_available_extensions WHERE name = 'postgis';") > 0)
                                {
                                    //this.
                                        Invoke(
                                        new Action(
                                            () =>
                                            {
                                                statusText.Text = @"Create PostGIS extension ...";
                                            }
                                        )
                                    );
                                    //用于支持矢量存储及空间运算 
                                    //在 linux 环境下动态创建数据库（create database）时，必须将 pooling 明确指定为 false（因为默认值为true），也就是说不能池化，以便使连接被关闭时立即生效！
                                    PostgreSqlHelper.NonQuery("CREATE EXTENSION postgis;", pooling: false);

                                    if ((long)PostgreSqlHelper.Scalar(
                                        "SELECT count(*) FROM pg_available_extensions WHERE name = 'postgis_raster';") > 0) //PG12+ 需要显示创建此扩展！
                                    {
                                        //this.
                                            Invoke(
                                            new Action(
                                                () =>
                                                {
                                                    statusText.Text = @"Create postgis_raster extension ...";
                                                }
                                            )
                                        );

                                        //用于支持栅格存储及运算
                                        PostgreSqlHelper.NonQuery("CREATE EXTENSION postgis_raster;", pooling: false);

                                        if ((long)PostgreSqlHelper.Scalar(
                                            "SELECT count(*) FROM pg_available_extensions WHERE name = 'intarray';") > 0)
                                        {
                                            //this.
                                                Invoke(
                                                new Action(
                                                    () =>
                                                    {
                                                        statusText.Text = @"Create intarray extension ...";
                                                    }
                                                )
                                            );

                                            //用于支持一维整型数组运算，索引支持的运算符： && @> <@ @@ 以及 =
                                            PostgreSqlHelper.NonQuery("CREATE EXTENSION intarray;", pooling: false);

                                            if ((long)PostgreSqlHelper.Scalar(
                                                "SELECT count(*) FROM pg_available_extensions WHERE name = 'pgroonga';") > 0)
                                            {
                                                //this.
                                                    Invoke(
                                                    new Action(
                                                        () =>
                                                        {
                                                            statusText.Text = @"Create pgroonga extension ...";
                                                        }
                                                    )
                                                );

                                                //用于支持多语种全文检索
                                                /*
                                                    PGroonga (píːzí:lúnɡά) is a PostgreSQL extension to use Groonga as the index.
                                                    PostgreSQL supports full text search against languages that use only alphabet and digit. It means that PostgreSQL doesn't support full text search against Japanese, Chinese and so on. 
                                                 */
                                                PostgreSqlHelper.NonQuery("CREATE EXTENSION pgroonga;", pooling: false);

                                                ///////////////////////////// 支持外挂子表 /////////////////////////////
                                                //this.
                                                    Invoke(
                                                    new Action(
                                                        () =>
                                                        {
                                                            statusText.Text = @"Create forest table（forest）...";
                                                        }
                                                    )
                                                );

                                                var sqlString =
                                                    "CREATE TABLE forest " +
                                                    "(" +
                                                    "id INTEGER, name TEXT, property JSONB, timestamp INTEGER[], status SmallInt DEFAULT 0" +
                                                    ",CONSTRAINT forest_pkey PRIMARY KEY (id)" +
                                                    ",CONSTRAINT forest_status_constraint CHECK (status >= 0 AND status <= 7)" +
                                                    ") PARTITION BY HASH (id);" +
                                                    "COMMENT ON TABLE forest IS '森林表，此表是本系统的第一张表，用于存放节点森林基本信息，每片森林（节点群）将由若干颗文档树（GeositeXml）构成';" +
                                                    "COMMENT ON COLUMN forest.id IS '森林序号标识码（通常由注册表[register.xml]中[forest]节的先后顺序决定，亦可通过接口函数赋值），充当主键（唯一性约束）且通常大于等于0，若设为负值，便不参与后续对等，需通过额外工具进行【增删改】操作';" +
                                                    "COMMENT ON COLUMN forest.name IS '森林简要名称';" +
                                                    "COMMENT ON COLUMN forest.property IS '森林属性描述信息，通常放置图标链接、服务文档等显式定制化信息';" +
                                                    "COMMENT ON COLUMN forest.timestamp IS '森林创建时间戳（由[年月日：yyyyMMdd,时分秒：HHmmss]二元整型数组编码构成）';" +
                                                    "COMMENT ON COLUMN forest.status IS '森林状态码（介于0～7之间）';";
                                                /*  
                                                    节点森林状态码status含义如下：
                                                    持久化	暗数据	完整性	含义
                                                    ======	======	======	==============================================================
                                                    0		0		0		默认值0：非持久化数据（参与对等）		明数据		无值或失败
                                                    0		0		1		指定值1：非持久化数据（参与对等）		明数据		正常
                                                    0		1		0		指定值2：非持久化数据（参与对等）		暗数据		失败
                                                    0		1		1		指定值3：非持久化数据（参与对等）		暗数据		正常
                                                    1		0		0		指定值4：持久化数据（不参与后续对等）	明数据		失败
                                                    1		0		1		指定值5：持久化数据（不参与后续对等）	明数据		正常
                                                    1		1		0		指定值6：持久化数据（不参与后续对等）	暗数据		失败
                                                    1		1		1		指定值7：持久化数据（不参与后续对等）	暗数据		正常
                                                 */
                                                if (PostgreSqlHelper.NonQuery(sqlString) != null)
                                                {
                                                    //暂采用CPU核数充当分区个数
                                                    for (var i = 0; i < processorCount; i++)
                                                    {
                                                        sqlString = $"CREATE TABLE forest_{i} PARTITION OF forest FOR VALUES WITH (MODULUS {processorCount}, REMAINDER {i});";
                                                        PostgreSqlHelper.NonQuery(sqlString);
                                                    }

                                                    //PG自动对主键id创建索引：CREATE INDEX forest_id ON forest USING BTREE (id); 
                                                    //PostgreSQL为每一个唯一约束和主键约束创建一个索引来强制唯一性。因此，没有必要显式地为主键列创建一个索引

                                                    sqlString = "CREATE INDEX forest_name ON forest USING BTREE (name);" + //以便支持 order by 和 group by
                                                                "CREATE INDEX forest_name_FTS ON forest USING PGROONGA (name);" + //以便支持全文检索FTS
                                                                "CREATE INDEX forest_property ON forest USING GIN (property);" +
                                                                "CREATE INDEX forest_property_FTS ON forest USING PGROONGA (property);" +
                                                                "CREATE INDEX forest_timestamp_yyyymmdd ON forest USING BTREE ((timestamp[1]));" +
                                                                "CREATE INDEX forest_timestamp_hhmmss ON forest USING BTREE ((timestamp[2]));" +
                                                                "CREATE INDEX forest_status ON forest USING BTREE (status);";
                                                    if (PostgreSqlHelper.NonQuery(sqlString) != null)
                                                    {
                                                        sqlString =
                                                            "CREATE TABLE forest_relation " +
                                                            "(" +
                                                            "forest INTEGER, action JSONB, detail XML" +
                                                            ",CONSTRAINT forest_relation_pkey PRIMARY KEY (forest)" +
                                                            ",CONSTRAINT forest_relation_cascade FOREIGN KEY (forest) REFERENCES forest (id) MATCH SIMPLE ON DELETE CASCADE NOT VALID" +
                                                            ") PARTITION BY HASH (forest);" +
                                                            "COMMENT ON TABLE forest_relation IS '节点森林关系描述表';" +
                                                            "COMMENT ON COLUMN forest_relation.forest IS '节点森林序号标识码';" +
                                                            "COMMENT ON COLUMN forest_relation.action IS '节点森林事务活动容器';" +
                                                            "COMMENT ON COLUMN forest_relation.detail IS '节点森林关系描述容器';"; //暂不额外创建索引
                                                        PostgreSqlHelper.NonQuery(sqlString);

                                                        //暂采用CPU核数充当分区个数
                                                        for (var i = 0; i < processorCount; i++)
                                                        {
                                                            sqlString = $"CREATE TABLE forest_relation_{i} PARTITION OF forest_relation FOR VALUES WITH (MODULUS {processorCount}, REMAINDER {i});";
                                                            PostgreSqlHelper.NonQuery(sqlString);
                                                        }

                                                        sqlString =
                                                            "CREATE INDEX forest_relation_action_FTS ON forest_relation USING PGROONGA (action);" +
                                                            "CREATE INDEX forest_relation_action ON forest_relation USING GIN (action);";
                                                        PostgreSqlHelper.NonQuery(sqlString);

                                                        ///////////////////////////// 支持外挂子表 /////////////////////////////
                                                        //this.
                                                            Invoke(
                                                            new Action(
                                                                () =>
                                                                {
                                                                    statusText.Text = @"Create tree table（tree）...";
                                                                }
                                                            )
                                                        );
                                                        sqlString =
                                                            "CREATE TABLE tree " +
                                                            "(" +
                                                            "forest INTEGER, sequence INTEGER, id INTEGER, name TEXT, property JSONB, uri TEXT, timestamp INTEGER[], type INTEGER[], status SmallInt DEFAULT 0" +
                                                            ",CONSTRAINT tree_pkey PRIMARY KEY (id)" +
                                                            ",CONSTRAINT tree_cascade FOREIGN KEY (forest) REFERENCES forest (id) MATCH SIMPLE ON DELETE CASCADE NOT VALID" +
                                                            ") PARTITION BY HASH (id);" +
                                                            "COMMENT ON TABLE tree IS '树根表，此表是本系统的第二张表，用于存放某片森林（节点群）中的若干颗文档树（GeositeXML）';" +
                                                            "COMMENT ON COLUMN tree.forest IS '文档树所属节点森林标识码';" + //forest表中的id
                                                            "COMMENT ON COLUMN tree.sequence IS '文档树在节点森林中排列顺序号（由所在森林内的[GeositeXML]文档编号顺序决定）且大于等于0';" +
                                                            "COMMENT ON COLUMN tree.id IS '文档树标识码（相当于每棵树的树根编号），充当主键（唯一性约束）且大于等于0';" +
                                                            "COMMENT ON COLUMN tree.name IS '文档树根节点简要名称';" +
                                                            "COMMENT ON COLUMN tree.property IS '文档树根节点属性描述信息，通常放置根节点辅助说明信息';" +
                                                            "COMMENT ON COLUMN tree.uri IS '文档树数据来源（存放路径及文件名）';" +
                                                            "COMMENT ON COLUMN tree.timestamp IS '文档树编码印章，采用[节点森林序号,文档树序号,年月日（yyyyMMdd）,时分秒（HHmmss）]四元整型数组编码方式';" +
                                                            "COMMENT ON COLUMN tree.type IS '文档树要素类型码构成的数组（类型码约定：0：非空间数据【默认】、1：Point点、2：Line线、3：Polygon面、4：Image地理贴图、10000：Wms栅格金字塔瓦片服务类型[epsg:0 - 无投影瓦片]、10001：Wms瓦片服务类型[epsg:4326 - 地理坐标系瓦片]、10002：Wms栅格金字塔瓦片服务类型[epsg:3857 - 球体墨卡托瓦片]、11000：Wmts栅格金字塔瓦片类型[epsg:0 - 无投影瓦片]、11001：Wmts栅格金字塔瓦片类型[epsg:4326 - 地理坐标系瓦片]、11002：Wmts栅格金字塔瓦片类型[epsg:3857 - 球体墨卡托瓦片]、12000：WPS栅格平铺式瓦片类型[epsg:0 - 无投影瓦片]、12001：WPS栅格平铺式瓦片类型[epsg:4326 - 地理坐标系瓦片]、12002：WPS栅格平铺式瓦片类型[epsg:3857 - 球体墨卡托瓦片]）';" +
                                                            "COMMENT ON COLUMN tree.status IS '文档树状态码（介于0～7之间），继承自[forest.status]';";

                                                        /*  
                                                            文档树状态码status，继承自[forest.status]，含义如下
                                                            持久化	暗数据	完整性	含义
                                                            ======	======	======	==============================================================
                                                            0		0		0		默认值0：非持久化数据（参与对等）		明数据		无值或失败
                                                            0		0		1		指定值1：非持久化数据（参与对等）		明数据		正常
                                                            0		1		0		指定值2：非持久化数据（参与对等）		暗数据		失败
                                                            0		1		1		指定值3：非持久化数据（参与对等）		暗数据		正常
                                                            1		0		0		指定值4：持久化数据（不参与后续对等）	明数据		失败
                                                            1		0		1		指定值5：持久化数据（不参与后续对等）	明数据		正常
                                                            1		1		0		指定值6：持久化数据（不参与后续对等）	暗数据		失败
                                                            1		1		1		指定值7：持久化数据（不参与后续对等）	暗数据		正常
                                                         */

                                                        if (PostgreSqlHelper.NonQuery(sqlString) != null)
                                                        {
                                                            //暂采用CPU核数充当分区个数
                                                            for (var i = 0; i < processorCount; i++)
                                                            {
                                                                sqlString = $"CREATE TABLE tree_{i} PARTITION OF tree FOR VALUES WITH (MODULUS {processorCount}, REMAINDER {i});";
                                                                PostgreSqlHelper.NonQuery(sqlString);
                                                            }

                                                            PostgreSqlHelper.NonQuery("CREATE SEQUENCE tree_id_seq INCREMENT 1 MINVALUE 1 MAXVALUE 2147483647 START 1 CACHE 1;");
                                                            //PG自动对主键id创建索引：CREATE INDEX tree_id ON tree USING BTREE (id); 
                                                            sqlString =
                                                                "CREATE INDEX tree_forest_sequence ON tree USING BTREE (forest, sequence);" +
                                                                "CREATE INDEX tree_name ON tree USING BTREE (name);" +
                                                                "CREATE INDEX tree_name_FTS ON tree USING PGROONGA (name);" +
                                                                "CREATE INDEX tree_property ON tree USING GIN (property);" +
                                                                "CREATE INDEX tree_property_FTS ON tree USING PGROONGA (property);" +
                                                                "CREATE INDEX tree_timestamp_forest ON tree USING BTREE ((timestamp[1]));" +
                                                                "CREATE INDEX tree_timestamp_tree ON tree USING BTREE ((timestamp[2]));" +
                                                                "CREATE INDEX tree_timestamp_yyyymmdd ON tree USING BTREE ((timestamp[3]));" +
                                                                "CREATE INDEX tree_timestamp_hhmmss ON tree USING BTREE ((timestamp[4]));" +
                                                                "CREATE INDEX tree_type ON tree USING GIST (type gist__int_ops);" + //需要 intarray 扩展模块
                                                                "CREATE INDEX tree_status ON tree USING BTREE (status);";
                                                            if (PostgreSqlHelper.NonQuery(sqlString) != null)
                                                            {
                                                                sqlString =
                                                                    "CREATE TABLE tree_relation " +
                                                                    "(" +
                                                                    "tree INTEGER, action JSONB, detail XML" +
                                                                    ",CONSTRAINT tree_relation_pkey PRIMARY KEY (tree)" +
                                                                    ",CONSTRAINT tree_relation_cascade FOREIGN KEY (tree) REFERENCES tree (id) MATCH SIMPLE ON DELETE CASCADE NOT VALID" +
                                                                    ") PARTITION BY HASH (tree);" +
                                                                    "COMMENT ON TABLE tree_relation IS '文档树关系描述表';" +
                                                                    "COMMENT ON COLUMN tree_relation.tree IS '文档树的标识码';" +
                                                                    "COMMENT ON COLUMN tree_relation.action IS '文档树事务活动容器';" +
                                                                    "COMMENT ON COLUMN tree_relation.detail IS '文档树关系描述容器';";
                                                                PostgreSqlHelper.NonQuery(sqlString);

                                                                //暂采用CPU核数充当分区个数
                                                                for (var i = 0; i < processorCount; i++)
                                                                {
                                                                    sqlString = $"CREATE TABLE tree_relation_{i} PARTITION OF tree_relation FOR VALUES WITH (MODULUS {processorCount}, REMAINDER {i});";
                                                                    PostgreSqlHelper.NonQuery(sqlString);
                                                                }

                                                                sqlString =
                                                                    "CREATE INDEX tree_relation_action_FTS ON tree_relation USING PGROONGA (action);" +
                                                                    "CREATE INDEX tree_relation_action ON tree_relation USING GIN (action);";
                                                                PostgreSqlHelper.NonQuery(sqlString);

                                                                ///////////////////////////////////////////////////////////////////////////////////////
                                                                //this.
                                                                    Invoke(
                                                                    new Action(
                                                                        () =>
                                                                        {
                                                                            statusText.Text = @"Create branch table（branch）...";
                                                                        }
                                                                    )
                                                                );

                                                                sqlString =
                                                                    "CREATE TABLE branch " +
                                                                    "(" +
                                                                    "tree INTEGER, level SmallInt, name TEXT, property JSONB, id INTEGER, parent INTEGER DEFAULT 0" +
                                                                    ",CONSTRAINT branch_pkey PRIMARY KEY (id)" +
                                                                    ",CONSTRAINT branch_cascade FOREIGN KEY (tree) REFERENCES tree (id) MATCH SIMPLE ON DELETE CASCADE NOT VALID" +
                                                                    ") PARTITION BY HASH (id);" +
                                                                    "COMMENT ON TABLE branch IS '枝干谱系表，此表是本系统第三张表，用于存放某棵树（GeositeXml文档）的枝干体系';" +
                                                                    "COMMENT ON COLUMN branch.tree IS '枝干隶属文档树的标识码';" + //forest表中的id字段
                                                                    "COMMENT ON COLUMN branch.level IS '枝干所处分类级别：1是树干、2是树枝、3是树杈、...、n是树梢';" +
                                                                    "COMMENT ON COLUMN branch.name IS '枝干简要名称';" +
                                                                    "COMMENT ON COLUMN branch.property IS '枝干属性描述信息，通常放置分类别名、分类链接、时间戳等定制化信息';" +
                                                                    "COMMENT ON COLUMN branch.id IS '枝干标识码，充当主键（唯一性约束）';" +
                                                                    "COMMENT ON COLUMN branch.parent IS '枝干的父级标识码（约定树根的标识码为0）';";

                                                                if (PostgreSqlHelper.NonQuery(sqlString) != null)
                                                                {
                                                                    //暂采用CPU核数充当分区个数
                                                                    for (var i = 0; i < processorCount; i++)
                                                                    {
                                                                        sqlString = $"CREATE TABLE branch_{i} PARTITION OF branch FOR VALUES WITH (MODULUS {processorCount}, REMAINDER {i});";
                                                                        PostgreSqlHelper.NonQuery(sqlString);
                                                                    }

                                                                    PostgreSqlHelper.NonQuery("CREATE SEQUENCE branch_id_seq INCREMENT 1 MINVALUE 1 MAXVALUE 2147483647 START 1 CACHE 1;");

                                                                    //PG自动对主键id创建索引：CREATE INDEX branch_id ON branch USING btree (id);  
                                                                    sqlString =
                                                                        "CREATE INDEX branch_tree ON branch USING BTREE (tree);" + //【WHERE tree = {tree} AND level = {currentLevel} AND name = {name}::text LIMIT 1】 需要这个索引
                                                                        "CREATE INDEX branch_level_name_parent ON branch USING BTREE (level, name, parent);" + //【GROUP BY level, name】需要这个索引
                                                                        "CREATE INDEX branch_name ON branch USING BTREE (name);" + //【GROUP BY name】 需要这个索引
                                                                        "CREATE INDEX branch_name_FTS ON branch USING PGROONGA (name);" +
                                                                        "CREATE INDEX branch_property_FTS ON branch USING PGROONGA (property);" +
                                                                        "CREATE INDEX branch_property ON branch USING GIN (property);";

                                                                    if (PostgreSqlHelper.NonQuery(sqlString) != null)
                                                                    {
                                                                        sqlString =
                                                                            "CREATE TABLE branch_relation " +
                                                                            "(" +
                                                                            "branch INTEGER, action JSONB, detail XML" +
                                                                            ",CONSTRAINT branch_relation_pkey PRIMARY KEY (branch)" +
                                                                            ",CONSTRAINT branch_relation_cascade FOREIGN KEY (branch) REFERENCES branch (id) MATCH SIMPLE ON DELETE CASCADE NOT VALID" +
                                                                            ") PARTITION BY HASH (branch);" +
                                                                            "COMMENT ON TABLE branch_relation IS '枝干关系描述表';" +
                                                                            "COMMENT ON COLUMN branch_relation.branch IS '枝干标识码';" +
                                                                            "COMMENT ON COLUMN branch_relation.action IS '枝干事务活动容器';" +
                                                                            "COMMENT ON COLUMN branch_relation.detail IS '枝干关系描述容器';";
                                                                        PostgreSqlHelper.NonQuery(sqlString);

                                                                        //暂采用CPU核数充当分区个数
                                                                        for (var i = 0; i < processorCount; i++)
                                                                        {
                                                                            sqlString = $"CREATE TABLE branch_relation_{i} PARTITION OF branch_relation FOR VALUES WITH (MODULUS {processorCount}, REMAINDER {i});";
                                                                            PostgreSqlHelper.NonQuery(sqlString);
                                                                        }

                                                                        sqlString =
                                                                            "CREATE INDEX branch_relation_action_FTS ON branch_relation USING PGROONGA (action);" +
                                                                            "CREATE INDEX branch_relation_action ON branch_relation USING GIN (action);";
                                                                        PostgreSqlHelper.NonQuery(sqlString);

                                                                        ///////////////////////////// 支持外挂子表 /////////////////////////////
                                                                        //this.
                                                                            Invoke(
                                                                            new Action(
                                                                                () =>
                                                                                {
                                                                                    statusText.Text = @"Create leaf table（leaf）...";
                                                                                }
                                                                            )
                                                                        );

                                                                        sqlString =
                                                                            "CREATE TABLE leaf " +
                                                                            "(" +
                                                                            "branch INTEGER, id BigInt, rank SmallInt DEFAULT 1, type INT DEFAULT 0, name TEXT, property INTEGER, timestamp INT[], frequency BigInt DEFAULT 0" +
                                                                            ",CONSTRAINT leaf_pkey PRIMARY KEY (id)" +
                                                                            ",CONSTRAINT leaf_cascade FOREIGN KEY (branch) REFERENCES branch (id) MATCH SIMPLE ON DELETE CASCADE NOT VALID" +
                                                                            ") PARTITION BY HASH (id);" + //为应对大数据，特按哈希键进行了分区，以便提升查询性能
                                                                            "COMMENT ON TABLE leaf IS '叶子表，此表是本系统第四表，用于存放某个树梢挂接的若干叶子（实体要素）的摘要信息';" +
                                                                            "COMMENT ON COLUMN leaf.branch IS '叶子要素隶属树梢（父级枝干）标识码';" + //branch表中的id字段 
                                                                            "COMMENT ON COLUMN leaf.id IS '叶子要素标识码，充当主键（唯一性约束）';" +
                                                                            "COMMENT ON COLUMN leaf.rank IS '叶子要素访问级别或权限序号，通常用于充当交互访问层的约束条件（0：可编辑；1：可查看属性（默认值）；2：可浏览提示；...）';" +
                                                                            "COMMENT ON COLUMN leaf.type IS '叶子要素类别码（0：非空间数据【默认】、1：Point点、2：Line线、3：Polygon面、4：Image地理贴图、10000：Wmts栅格金字塔瓦片服务类型[epsg:0 - 无投影瓦片]、10001：Wmts瓦片服务类型[epsg:4326 - 地理坐标系瓦片]、10002：Wmts栅格金字塔瓦片服务类型[epsg:3857 - 球体墨卡托瓦片]、11000：Tile栅格金字塔瓦片类型[epsg:0 - 无投影瓦片]、11001：Tile栅格金字塔瓦片类型[epsg:4326 - 地理坐标系瓦片]、11002：Tile栅格金字塔瓦片类型[epsg:3857 - 球体墨卡托瓦片]、12000：Tile栅格平铺式瓦片类型[epsg:0 - 无投影瓦片]、12001：Tile栅格平铺式瓦片类型[epsg:4326 - 地理坐标系瓦片]、12002：Tile栅格平铺式瓦片类型[epsg:3857 - 球体墨卡托瓦片]）';" +
                                                                            "COMMENT ON COLUMN leaf.name IS '叶子要素名称';" +
                                                                            "COMMENT ON COLUMN leaf.property IS '叶子要素属性架构哈希值';" +
                                                                            "COMMENT ON COLUMN leaf.timestamp IS '叶子要素创建时间戳（由[年月日：yyyyMMdd,时分秒：HHmmss]二元整型数组编码构成）';" + //以便实施btree索引和关系运算
                                                                            "COMMENT ON COLUMN leaf.frequency IS '叶子要素访问频度';";
                                                                        /*  
                                                                            叶子要素类别码type，约定含义如下                                                    
                                                                            =============================
                                                                            0	非空间数据【默认】

                                                                            // 矢量类：>=1 <=9999
                                                                            1	点
                                                                            2	线
                                                                            3	面
                                                                            4	贴图  

                                                                            // 栅格类： >=10000 <=19999
                                                                            //10000：Tile栅格  金字塔瓦片     wms服务类型     [epsg:0       无投影瓦片]
                                                                            //10001：Tile栅格  金字塔瓦片     wms服务类型     [epsg:4326    地理坐标系瓦片]
                                                                            //10002：Tile栅格  金字塔瓦片     wms服务类型     [epsg:3857    球体墨卡托瓦片]
                                                                            //11000：Tile栅格  金字塔瓦片     wmts服务类型    [epsg:0       无投影瓦片]
                                                                            //11001：Tile栅格  金字塔瓦片     wmts服务类型    [epsg:4326    地理坐标系瓦片]
                                                                            //11002：Tile栅格  金字塔瓦片     wmts服务类型    [epsg:3857    球体墨卡托瓦片]
                                                                            //12000：Tile栅格  平铺式瓦片     wps服务类型     [epsg:0       无投影瓦片]
                                                                            //12001：Tile栅格  平铺式瓦片     wps服务类型     [epsg:4326    地理坐标系瓦片]
                                                                            //12002：Tile栅格  平铺式瓦片     wps服务类型     [epsg:3857    球体墨卡托瓦片]
                                                                        */

                                                                        /*  									
                                                                            注意：键值对【jsonb】格式需要postgresql 9.4及其以上版本支持！ 经验证，btree索引比brin索引的检索效率高，但存储尺寸较大，brin需要postgresql 9.5及其以上版本支持！
                                                                            另外，针对文本型text字段，若创建了btree索引，只有当使用 like '关键字' 时且前面不能加%才能启用索引！

                                                                            在接口模块里，暗数据通常用于不便于传输的复杂几何数据、敏感数据或不便于浏览器展现的数据，该类数据不直接向外界提供检索、传输和展现服务，但可充当背景数据参与分析和运算。                                                       
                                                                            为便于将leaf表按大数据分布式存储，避免因多台服务器节点对bigserial类型数据均自动产生而造成重复或交叉现象，需将bigserial更改为bigint（-9223372036854775808 to +9223372036854775807），其值可通过序列函数手动赋值。
                                                                        */

                                                                        if (PostgreSqlHelper.NonQuery(sqlString) != null)
                                                                        {
                                                                            //暂采用CPU核数充当分区个数
                                                                            for (var i = 0; i < processorCount; i++)
                                                                            {
                                                                                sqlString = $"CREATE TABLE leaf_{i} PARTITION OF leaf FOR VALUES WITH (MODULUS {processorCount}, REMAINDER {i});";
                                                                                PostgreSqlHelper.NonQuery(sqlString);
                                                                            }

                                                                            PostgreSqlHelper.NonQuery("CREATE SEQUENCE leaf_id_seq INCREMENT 1 MINVALUE 1 MAXVALUE 9223372036854775807 START 1 CACHE 1;");
                                                                            //PG自动对主键创建索引：CREATE INDEX leaf_id ON leaf USING btree (id);
                                                                            sqlString =
                                                                                "CREATE INDEX leaf_branch ON leaf USING BTREE (branch);" +
                                                                                "CREATE INDEX leaf_type ON leaf USING BTREE (type);" +
                                                                                "CREATE INDEX leaf_name ON leaf USING BTREE (name);" +
                                                                                "CREATE INDEX leaf_name_FTS ON leaf USING PGROONGA (name);" + //以便支持全文检索
                                                                                "CREATE INDEX leaf_property ON leaf USING BTREE (property);" + //为 group by 提供索引，以便提取异构记录 
                                                                                "CREATE INDEX leaf_timestamp_yyyymmdd ON leaf USING BTREE ((timestamp[1]));" +
                                                                                "CREATE INDEX leaf_timestamp_hhmmss ON leaf USING BTREE ((timestamp[2]));" +
                                                                                //创建键集索引的目的是：
                                                                                //1、体现频度优先原则（DESC 逆序）；
                                                                                //2、为基于【键集】分页技术提供高速定位手段（ASC顺序和DESC逆序）；
                                                                                //3、解决负值偏移问题（ASC 顺序），因为SQL-offset必须大于等于0，若想回溯，需采用顺序和逆序联合模拟方式
                                                                                //4、采用 frequency 和 id 联合索引的目的是确保索引具有唯一性 
                                                                                "CREATE INDEX leaf_frequency_id ON leaf USING BTREE (frequency ASC NULLS LAST, id ASC NULLS LAST);";

                                                                            if (PostgreSqlHelper.NonQuery(sqlString) != null)
                                                                            {
                                                                                sqlString =
                                                                                    "CREATE TABLE leaf_relation " +
                                                                                    "(" +
                                                                                    "leaf BigInt, action JSONB, detail XML" +
                                                                                    ",CONSTRAINT leaf_relation_pkey PRIMARY KEY (leaf)" +
                                                                                    ",CONSTRAINT leaf_relation_cascade FOREIGN KEY (leaf) REFERENCES leaf (id) MATCH SIMPLE ON DELETE CASCADE NOT VALID" +
                                                                                    ") PARTITION BY HASH (leaf);" + //按哈希键进行分区，以便提升大数据查询性能
                                                                                    "COMMENT ON TABLE leaf_relation IS '叶子关系描述表';" +
                                                                                    "COMMENT ON COLUMN leaf_relation.leaf IS '叶子要素标识码';" +
                                                                                    "COMMENT ON COLUMN leaf_relation.action IS '叶子事务活动容器';" +
                                                                                    "COMMENT ON COLUMN leaf_relation.detail IS '叶子关系描述容器';";
                                                                                PostgreSqlHelper.NonQuery(sqlString);
                                                                                //暂采用CPU核数充当分区个数
                                                                                for (var i = 0; i < processorCount; i++)
                                                                                {
                                                                                    sqlString = $"CREATE TABLE leaf_relation_{i} PARTITION OF leaf_relation FOR VALUES WITH (MODULUS {processorCount}, REMAINDER {i});";
                                                                                    PostgreSqlHelper.NonQuery(sqlString);
                                                                                }
                                                                                sqlString =
                                                                                    "CREATE INDEX leaf_relation_action_FTS ON leaf_relation USING PGROONGA (action);" +
                                                                                    "CREATE INDEX leaf_relation_action ON leaf_relation USING GIN (action);";
                                                                                PostgreSqlHelper.NonQuery(sqlString);

                                                                                //this.
                                                                                Invoke(
                                                                                    new Action(
                                                                                        () =>
                                                                                        {
                                                                                            statusText.Text =
                                                                                                @"Create leaf table（leaf_description）...";
                                                                                        }
                                                                                    )
                                                                                );

                                                                                sqlString =
                                                                                    "CREATE TABLE leaf_description " +
                                                                                    "(" + //parent字段增设于 2022年1月20日，便于实现xml重构
                                                                                    "leaf bigint, level SmallInt, sequence SmallInt, parent SmallInt, name TEXT, attribute JSONB, flag BOOLEAN DEFAULT false, type SmallInt DEFAULT 0, content Text, numericvalue Numeric" +
                                                                                    ",CONSTRAINT leaf_description_pkey PRIMARY KEY (leaf, level, sequence, parent)" + //唯一性
                                                                                    ",CONSTRAINT leaf_description_cascade FOREIGN KEY (leaf) REFERENCES leaf (id) MATCH SIMPLE ON DELETE CASCADE NOT VALID" +
                                                                                    ") PARTITION BY HASH (leaf, level, sequence, parent);" + //(leaf, level, sequence, parent) 为应对大数据，特按哈希键进行了分区，以便提升查询性能
                                                                                    "COMMENT ON TABLE leaf_description IS '叶子要素表（leaf）的属性描述子表';" +
                                                                                    "COMMENT ON COLUMN leaf_description.leaf IS '叶子要素的标识码';" + //leaf表中的id
                                                                                    "COMMENT ON COLUMN leaf_description.level IS '字段（键）的嵌套层级';" +
                                                                                    "COMMENT ON COLUMN leaf_description.sequence IS '字段（键）的同级序号';" +
                                                                                    "COMMENT ON COLUMN leaf_description.parent IS '字段所属父级层级的排列序号';" +
                                                                                    "COMMENT ON COLUMN leaf_description.name IS '字段（键）的名称';" +
                                                                                    "COMMENT ON COLUMN leaf_description.attribute IS '字段（键）的属性，由若干扁平化键值对（KVP）构成';" +
                                                                                    "COMMENT ON COLUMN leaf_description.flag IS '字段（键）的逻辑标识（false：此键无值；true：此键有值）';" +
                                                                                    "COMMENT ON COLUMN leaf_description.type IS '字段（值）的数据类型码，目前支持：-1【分类型字段】、0【string（null）】、1【integer】、2【decimal】、3【hybrid】、4【boolean】';" +
                                                                                    "COMMENT ON COLUMN leaf_description.content IS '字段（值）的全文内容，以便实施全文检索以及自然语言处理';" + //若开展自然语言处理，可将语义规则存入leaf_relation
                                                                                    "COMMENT ON COLUMN leaf_description.numericvalue IS '字段（值）的数值型（1【integer】、2【decimal】、3【hybrid】、4【boolean】）容器，以便支持超大值域聚合计算';";

                                                                                if (PostgreSqlHelper
                                                                                     .NonQuery(sqlString) != null)
                                                                                {
                                                                                    //暂采用CPU核数充当分区个数
                                                                                    for (var i = 0;
                                                                                     i < processorCount;
                                                                                     i++)
                                                                                    {
                                                                                        sqlString =
                                                                                            $"CREATE TABLE leaf_description_{i} PARTITION OF leaf_description FOR VALUES WITH (MODULUS {processorCount}, REMAINDER {i});";
                                                                                        PostgreSqlHelper.NonQuery(
                                                                                            sqlString);
                                                                                    }

                                                                                    sqlString =
                                                                                        "CREATE INDEX leaf_description_name ON leaf_description USING BTREE (name);" +
                                                                                        "CREATE INDEX leaf_description_name_FTS ON leaf_description USING PGROONGA (name);" +
                                                                                        "CREATE INDEX leaf_description_flag ON leaf_description USING BTREE (flag);" +
                                                                                        "CREATE INDEX leaf_description_type ON leaf_description USING BTREE (type);" +
                                                                                        "CREATE INDEX leaf_description_content ON leaf_description USING PGROONGA (content);" + //全文检索（FTS）采用了 PGROONGA 扩展
                                                                                        "CREATE INDEX leaf_description_numericvalue ON leaf_description USING BTREE (numericvalue);";

                                                                                    if (PostgreSqlHelper.NonQuery(
                                                                                         sqlString) != null)
                                                                                    {
                                                                                        ///////////////////////////////////////////////////////////////////////////////////////
                                                                                        //this.
                                                                                        Invoke(
                                                                                            new Action(
                                                                                                () =>
                                                                                                {
                                                                                                    statusText.Text =
                                                                                                        @"Create leaf table（leaf_style）...";
                                                                                                }
                                                                                            )
                                                                                        );

                                                                                        sqlString =
                                                                                            "CREATE TABLE leaf_style " +
                                                                                            "(" +
                                                                                            "leaf BigInt, style JSONB" +
                                                                                            ",CONSTRAINT leaf_style_pkey PRIMARY KEY (leaf)" +
                                                                                            ",CONSTRAINT leaf_style_cascade FOREIGN KEY (leaf) REFERENCES leaf (id) MATCH SIMPLE ON DELETE CASCADE NOT VALID" +
                                                                                            ") PARTITION BY HASH (leaf);" + //为应对大数据，特按哈希键进行了分区，以便提升查询性能
                                                                                            "COMMENT ON TABLE leaf_style IS '叶子要素表（leaf）的样式子表';" +
                                                                                            "COMMENT ON COLUMN leaf_style.leaf IS '叶子要素的标识码';" + //leaf表中的id
                                                                                            "COMMENT ON COLUMN leaf_style.style IS '叶子要素可视化样式信息，由若干键值对（KVP）构成';";

                                                                                        if (PostgreSqlHelper.NonQuery(
                                                                                             sqlString) != null)
                                                                                        {
                                                                                            //暂采用CPU核数充当分区个数
                                                                                            for (var i = 0;
                                                                                             i < processorCount;
                                                                                             i++)
                                                                                            {
                                                                                                sqlString =
                                                                                                    $"CREATE TABLE leaf_style_{i} PARTITION OF leaf_style FOR VALUES WITH (MODULUS {processorCount}, REMAINDER {i});";
                                                                                                PostgreSqlHelper
                                                                                                    .NonQuery(
                                                                                                        sqlString);
                                                                                            }

                                                                                            sqlString =
                                                                                                "CREATE INDEX leaf_style_style_FTS ON leaf_style USING PGROONGA (style);" +
                                                                                                "CREATE INDEX leaf_style_style ON leaf_style USING GIN (style);";
                                                                                            if (PostgreSqlHelper
                                                                                                 .NonQuery(
                                                                                                     sqlString) !=
                                                                                             null)
                                                                                            {
                                                                                                ///////////////////////////////////////////////////////////////////////////////////////
                                                                                                //this.
                                                                                                Invoke(
                                                                                                    new Action(
                                                                                                        () =>
                                                                                                        {
                                                                                                            statusText
                                                                                                                    .Text =
                                                                                                                @"Create leaf table（leaf_geometry）...";
                                                                                                        }
                                                                                                    )
                                                                                                );

                                                                                                sqlString =
                                                                                                    "CREATE TABLE leaf_geometry " +
                                                                                                    "(" +
                                                                                                    "leaf BigInt, coordinate GEOMETRY, boundary GEOMETRY, centroid GEOMETRY" +
                                                                                                    ",CONSTRAINT leaf_geometry_pkey PRIMARY KEY (leaf)" +
                                                                                                    ",CONSTRAINT leaf_geometry_cascade FOREIGN KEY (leaf) REFERENCES leaf (id) MATCH SIMPLE ON DELETE CASCADE NOT VALID" +
                                                                                                    ") PARTITION BY HASH (leaf);" + //为应对大数据，特按哈希键进行了分区，以便提升查询性能
                                                                                                    "COMMENT ON TABLE leaf_geometry IS '叶子要素表（leaf）的几何坐标子表';" +
                                                                                                    "COMMENT ON COLUMN leaf_geometry.leaf IS '叶子要素的标识码';" + //leaf表中的id
                                                                                                    "COMMENT ON COLUMN leaf_geometry.coordinate IS '叶子要素几何坐标（【EPSG:4326】）';" + //EPSG：4326 地理坐标 - 十进制经纬度格式
                                                                                                    "COMMENT ON COLUMN leaf_geometry.boundary IS '叶子要素几何边框（【EPSG:4326】）';" + //EPSG：4326 地理坐标 - 十进制经纬度格式
                                                                                                    "COMMENT ON COLUMN leaf_geometry.centroid IS '叶子要素几何内点（通常用于几何瘦身、标注锚点等场景）';";
                                                                                                if (PostgreSqlHelper
                                                                                                     .NonQuery(
                                                                                                         sqlString) !=
                                                                                                 null)
                                                                                                {
                                                                                                    //暂采用CPU核数充当分区个数
                                                                                                    for (var i = 0;
                                                                                                     i <
                                                                                                     processorCount;
                                                                                                     i++)
                                                                                                    {
                                                                                                        sqlString =
                                                                                                            $"CREATE TABLE leaf_geometry_{i} PARTITION OF leaf_geometry FOR VALUES WITH (MODULUS {processorCount}, REMAINDER {i});";
                                                                                                        PostgreSqlHelper
                                                                                                            .NonQuery(
                                                                                                                sqlString);
                                                                                                    }

                                                                                                    sqlString =
                                                                                                        "CREATE INDEX leaf_geometry_coordinate ON leaf_geometry USING GIST (coordinate);" + //需要postgis扩展
                                                                                                        "CREATE INDEX leaf_geometry_boundary ON leaf_geometry USING GIST (boundary);" + //需要postgis扩展
                                                                                                        "CREATE INDEX leaf_geometry_centroid ON leaf_geometry USING GIST (centroid);"; //需要postgis扩展
                                                                                                    if (PostgreSqlHelper
                                                                                                         .NonQuery(
                                                                                                             sqlString) !=
                                                                                                     null)
                                                                                                    {
                                                                                                        ///////////////////////////////////////////////////////////////////////////////////////
                                                                                                        //this.
                                                                                                        Invoke(
                                                                                                            new Action(
                                                                                                                () =>
                                                                                                                {
                                                                                                                    statusText
                                                                                                                            .Text =
                                                                                                                        @"Create leaf table（leaf_tile）...";
                                                                                                                }
                                                                                                            )
                                                                                                        );
                                                                                                        sqlString =
                                                                                                            "CREATE TABLE leaf_tile " +
                                                                                                            "(" +
                                                                                                            "leaf BigInt, z INTEGER, x INTEGER, y INTEGER, tile RASTER, boundary geometry" +
                                                                                                            ",CONSTRAINT leaf_tile_pkey PRIMARY KEY (leaf, z, x, y)" +
                                                                                                            ",CONSTRAINT leaf_tile_cascade FOREIGN KEY (leaf) REFERENCES leaf (id) MATCH SIMPLE ON DELETE CASCADE NOT VALID" +
                                                                                                            ") PARTITION BY HASH (leaf, z, x, y);" + //为应对大数据，特按哈希键进行了分区，以便提升查询性能
                                                                                                            "COMMENT ON TABLE leaf_tile IS '叶子要素表（leaf）的栅格瓦片子表，支持【四叉树金字塔式瓦片】和【平铺式地图瓦片】两种类型，每类瓦片的元数据信息需在叶子属性子表中的type进行表述';" +
                                                                                                            "COMMENT ON COLUMN leaf_tile.leaf IS '叶子要素的标识码';" + //leaf表中的id
                                                                                                            "COMMENT ON COLUMN leaf_tile.z IS '叶子瓦片缩放级（注：平铺式瓦片类型的z值强制为【-1】，四叉树金字塔式瓦片类型的z值通常介于【0～24】之间）';" +
                                                                                                            "COMMENT ON COLUMN leaf_tile.x IS '叶子瓦片横向坐标编码';" +
                                                                                                            "COMMENT ON COLUMN leaf_tile.y IS '叶子瓦片纵向坐标编码';" +
                                                                                                            "COMMENT ON COLUMN leaf_tile.tile IS '叶子瓦片栅格影像（RASTER类型-WKB格式，目前支持【EPSG:4326】、【EPSG:3857】、【EPSG:0】）';" +
                                                                                                            "COMMENT ON COLUMN leaf_tile.boundary IS '叶子瓦片几何边框（【EPSG:4326】）';"; //经纬度，针对deepzoom或者raster无投影类型，边框为null
                                                                                                        if
                                                                                                            (PostgreSqlHelper
                                                                                                                 .NonQuery(
                                                                                                                     sqlString) !=
                                                                                                             null)
                                                                                                        {
                                                                                                            //暂采用CPU核数充当分区个数 当采用多列哈希分区表的分区时，无论使用多少列，都只需要指定一个界限即可
                                                                                                            for (var i =
                                                                                                                 0;
                                                                                                             i <
                                                                                                             processorCount;
                                                                                                             i++)
                                                                                                            {
                                                                                                                sqlString =
                                                                                                                    $"CREATE TABLE leaf_tile_{i} PARTITION OF leaf_tile FOR VALUES WITH (MODULUS {processorCount}, REMAINDER {i});";
                                                                                                                PostgreSqlHelper
                                                                                                                    .NonQuery(
                                                                                                                        sqlString);
                                                                                                            }

                                                                                                            sqlString =
                                                                                                                "CREATE INDEX leaf_tile_tile ON leaf_tile USING GIST (st_convexhull(tile));" //需要postgis_raster扩展
                                                                                                                + "CREATE INDEX leaf_tile_boundary ON leaf_tile USING gist(boundary);" //需要postgis扩展
                                                                                                                + "CREATE INDEX leaf_tile_leaf_z ON leaf_tile USING btree (leaf ASC NULLS LAST, z DESC NULLS LAST)" //为提取最大缩放级提供逆序索引
                                                                                                                + ";";
                                                                                                            if
                                                                                                                (PostgreSqlHelper
                                                                                                                     .NonQuery(
                                                                                                                         sqlString) !=
                                                                                                                 null)
                                                                                                            {
                                                                                                                ///////////////////////////////////////////////////////////////////////////////////////
                                                                                                                //this.
                                                                                                                Invoke(
                                                                                                                    new
                                                                                                                        Action(
                                                                                                                            () =>
                                                                                                                            {
                                                                                                                                statusText
                                                                                                                                        .Text =
                                                                                                                                    @"Create leaf table（leaf_wms）...";
                                                                                                                            }
                                                                                                                        )
                                                                                                                );

                                                                                                                sqlString =
                                                                                                                    "CREATE TABLE leaf_wms " +
                                                                                                                    "(" +
                                                                                                                    "leaf BigInt, wms TEXT, boundary geometry" +
                                                                                                                    ",CONSTRAINT leaf_wms_pkey PRIMARY KEY (leaf)" +
                                                                                                                    ",CONSTRAINT leaf_wms_cascade FOREIGN KEY (leaf) REFERENCES leaf (id) MATCH SIMPLE ON DELETE CASCADE NOT VALID" +
                                                                                                                    ") PARTITION BY HASH (leaf);" +
                                                                                                                    "COMMENT ON TABLE leaf_wms IS '叶子要素表（leaf）的瓦片服务子表，元数据信息需在叶子属性表中的type中进行表述';" +
                                                                                                                    "COMMENT ON COLUMN leaf_wms.leaf IS '叶子要素的标识码';" + //leaf表中的id
                                                                                                                    "COMMENT ON COLUMN leaf_wms.wms IS '叶子要素服务地址模板，暂支持【OGC】、【BingMap】、【DeepZoom】和【ESRI】瓦片编码类型';" +
                                                                                                                    "COMMENT ON COLUMN leaf_wms.boundary IS '叶子要素几何边框（EPSG:4326）';"; //经纬度
                                                                                                                if
                                                                                                                    (PostgreSqlHelper
                                                                                                                         .NonQuery(
                                                                                                                             sqlString) !=
                                                                                                                     null)
                                                                                                                {
                                                                                                                    //暂采用CPU核数充当分区个数 当采用多列哈希分区表的分区时，无论使用多少列，都只需要指定一个界限即可
                                                                                                                    for
                                                                                                                        (var
                                                                                                                         i =
                                                                                                                             0;
                                                                                                                         i <
                                                                                                                         processorCount;
                                                                                                                         i++)
                                                                                                                    {
                                                                                                                        sqlString =
                                                                                                                            $"CREATE TABLE leaf_wms_{i} PARTITION OF leaf_wms FOR VALUES WITH (MODULUS {processorCount}, REMAINDER {i});";
                                                                                                                        PostgreSqlHelper
                                                                                                                            .NonQuery(
                                                                                                                                sqlString);
                                                                                                                    }

                                                                                                                    sqlString =
                                                                                                                        "CREATE INDEX leaf_wms_boundary ON leaf_wms USING gist(boundary);" //需要postgis扩展
                                                                                                                        ;
                                                                                                                    if
                                                                                                                        (PostgreSqlHelper
                                                                                                                             .NonQuery(
                                                                                                                                 sqlString) !=
                                                                                                                         null)
                                                                                                                    {
                                                                                                                        //仅用于充当访问频度缓冲区，重建索引时将自动清除/////////////////////////////////////////////////////////////////////////////////////
                                                                                                                        //this.
                                                                                                                        Invoke(
                                                                                                                            new
                                                                                                                                Action(
                                                                                                                                    () =>
                                                                                                                                    {
                                                                                                                                        statusText
                                                                                                                                                .Text =
                                                                                                                                            @"Create leaf table（leaf_hits）...";
                                                                                                                                    }
                                                                                                                                )
                                                                                                                        );

                                                                                                                        sqlString =
                                                                                                                            "CREATE TABLE leaf_hits " +
                                                                                                                            "(" +
                                                                                                                            "leaf BigInt, hits BigInt DEFAULT 0" +
                                                                                                                            ",CONSTRAINT leaf_hits_pkey PRIMARY KEY (leaf)" +
                                                                                                                            ",CONSTRAINT leaf_hits_cascade FOREIGN KEY (leaf) REFERENCES leaf (id) MATCH SIMPLE ON DELETE CASCADE NOT VALID" +
                                                                                                                            ") PARTITION BY HASH (leaf);" + //为应对大数据，特按哈希键进行了分区，以便提升查询性能
                                                                                                                            "COMMENT ON TABLE leaf_hits IS '叶子要素表（leaf）的搜索命中率子表';" +
                                                                                                                            "COMMENT ON COLUMN leaf_hits.leaf IS '叶子要素的标识码';" + //leaf表中的id
                                                                                                                            "COMMENT ON COLUMN leaf_hits.hits IS '叶子要素的命中次数';";

                                                                                                                        if
                                                                                                                            (PostgreSqlHelper
                                                                                                                                 .NonQuery(
                                                                                                                                     sqlString) !=
                                                                                                                             null)
                                                                                                                        {
                                                                                                                            //暂采用CPU核数充当分区个数
                                                                                                                            for
                                                                                                                                (var
                                                                                                                                 i =
                                                                                                                                     0;
                                                                                                                                 i <
                                                                                                                                 processorCount;
                                                                                                                                 i++)
                                                                                                                            {
                                                                                                                                sqlString =
                                                                                                                                    $"CREATE TABLE leaf_hits_{i} PARTITION OF leaf_hits FOR VALUES WITH (MODULUS {processorCount}, REMAINDER {i});";
                                                                                                                                PostgreSqlHelper
                                                                                                                                    .NonQuery(
                                                                                                                                        sqlString);
                                                                                                                            }

                                                                                                                            ///////////////////////////////////////////////////////////////////////////////////////
                                                                                                                            //this.
                                                                                                                            Invoke(
                                                                                                                                new
                                                                                                                                    Action(
                                                                                                                                        () =>
                                                                                                                                        {
                                                                                                                                            statusText
                                                                                                                                                    .Text =
                                                                                                                                                @"Create the age sub table of leaf table（leaf_age）...";
                                                                                                                                        }
                                                                                                                                    )
                                                                                                                            );

                                                                                                                            sqlString =
                                                                                                                                "CREATE TABLE leaf_age " +
                                                                                                                                "(" +
                                                                                                                                "leaf BigInt, age BigInt[]" + //DDE 深时数字地球计划 -- 地质年龄表
                                                                                                                                ",CONSTRAINT leaf_age_pkey PRIMARY KEY (leaf)" +
                                                                                                                                ",CONSTRAINT leaf_age_cascade FOREIGN KEY (leaf) REFERENCES leaf (id) MATCH SIMPLE ON DELETE CASCADE NOT VALID" +
                                                                                                                                ") PARTITION BY HASH (leaf);" +
                                                                                                                                "COMMENT ON TABLE leaf_age IS '叶子要素表（leaf）的年龄子表';" +
                                                                                                                                "COMMENT ON COLUMN leaf_age.leaf IS '叶子要素的标识码';" + //leaf表中的id
                                                                                                                                "COMMENT ON COLUMN leaf_age.age IS '叶子要素的年龄（通常为地质年龄，由【±年月日、时分秒】构成）';";
                                                                                                                            if
                                                                                                                                (PostgreSqlHelper
                                                                                                                                     .NonQuery(
                                                                                                                                         sqlString) !=
                                                                                                                                 null)
                                                                                                                            {
                                                                                                                                for
                                                                                                                                    (var
                                                                                                                                     i =
                                                                                                                                         0;
                                                                                                                                     i <
                                                                                                                                     processorCount;
                                                                                                                                     i++)
                                                                                                                                {
                                                                                                                                    sqlString =
                                                                                                                                        $"CREATE TABLE leaf_age_{i} PARTITION OF leaf_age FOR VALUES WITH (MODULUS {processorCount}, REMAINDER {i});";
                                                                                                                                    PostgreSqlHelper
                                                                                                                                        .NonQuery(
                                                                                                                                            sqlString);
                                                                                                                                }

                                                                                                                                sqlString =
                                                                                                                                    "CREATE INDEX leaf_age_yearmmdd ON leaf_age USING BTREE ((age[1]));" +
                                                                                                                                    "CREATE INDEX leaf_age_hhmmss ON leaf_age USING BTREE ((age[2]));";
                                                                                                                                if
                                                                                                                                    (PostgreSqlHelper
                                                                                                                                         .NonQuery(
                                                                                                                                             sqlString) !=
                                                                                                                                     null)
                                                                                                                                {
                                                                                                                                    //嵌入式自定义SQL函数/////////////////////////////////////////////////////////////

                                                                                                                                    /*
                                                                                                                                        * 嵌入式SQL函数区
                                                                                                                                        */

                                                                                                                                    /* 扩展聚合函数类，为【GROUP BY】提供首部和尾部成员
                                                                                                                                       例如：
                                                                                                                                            SELECT first(id order by id), customer, first(total order by id) 
                                                                                                                                            FROM 班级 
                                                                                                                                            GROUP BY 性别 
                                                                                                                                            ORDER BY first(total);
                                                                                                                                    */
                                                                                                                                    int.TryParse($"{PostgreSqlHelper.Scalar("SELECT count(*) FROM pg_proc WHERE proname = 'first_agg' OR proname = 'first';")}", out var firstAggregateExist);
                                                                                                                                    if (firstAggregateExist != 2)
                                                                                                                                        PostgreSqlHelper.NonQuery(
                                                                                                                                                "CREATE OR REPLACE FUNCTION public.first_agg (anyelement, anyelement)" +
                                                                                                                                                "  RETURNS anyelement" +
                                                                                                                                                "  LANGUAGE sql IMMUTABLE STRICT PARALLEL SAFE AS" +
                                                                                                                                                "  'SELECT $1';" +
                                                                                                                                                "  CREATE OR REPLACE AGGREGATE public.first (anyelement) (" +
                                                                                                                                                "    SFUNC = public.first_agg" +
                                                                                                                                                "    , STYPE = anyelement" +
                                                                                                                                                "    , PARALLEL = safe" +
                                                                                                                                                "    );"
                                                                                                                                            );

                                                                                                                                    int.TryParse($"{PostgreSqlHelper.Scalar("SELECT count(*) FROM pg_proc WHERE proname = 'last_agg' OR proname = 'last';")}",
                                                                                                                                        out var lastAggregateExist);
                                                                                                                                    if (lastAggregateExist != 2)
                                                                                                                                        PostgreSqlHelper.NonQuery
                                                                                                                                            (
                                                                                                                                                "CREATE OR REPLACE FUNCTION public.last_agg (anyelement, anyelement)" +
                                                                                                                                                "  RETURNS anyelement" +
                                                                                                                                                "  LANGUAGE sql IMMUTABLE STRICT PARALLEL SAFE AS" +
                                                                                                                                                "  'SELECT $2';" +
                                                                                                                                                "  CREATE OR REPLACE AGGREGATE public.last (anyelement) (" +
                                                                                                                                                "    SFUNC = public.last_agg" +
                                                                                                                                                "    , STYPE = anyelement" +
                                                                                                                                                "    , PARALLEL = safe" +
                                                                                                                                                "    );"
                                                                                                                                            );

                                                                                                                                    const string ogcBranches = "ogc_branches";
                                                                                                                                    int.TryParse($"{PostgreSqlHelper.Scalar($"SELECT count(*) FROM pg_proc WHERE proname = '{ogcBranches}';")}", out var ogcBranchesExist);
                                                                                                                                    if (ogcBranchesExist == 0)
                                                                                                                                        PostgreSqlHelper.NonQuery
                                                                                                                                            (
                                                                                                                                                //  依据分类名称获取所属子类（枝干）id，可抵御SQL注入攻击
                                                                                                                                                //  用法：select * from leaf where branch = any(array(select * from ogc_branches('data.地质'))) 
                                                                                                                                                //  typename：分类名称需从顶级分类开始并逐级限定，层名可采用星号[*]进行模糊匹配，层级之间需采用小数点[.]分隔
                                                                                                                                                //  path：是否获取所属全部枝干id，省略时取默认值：false = 仅获取末端树梢id；若指定为tree，将返回所属全部子代类别（枝干）id
                                                                                                                                                $"CREATE OR REPLACE FUNCTION public.{ogcBranches}(typename text, path boolean DEFAULT NULL::boolean) RETURNS TABLE(branch integer) LANGUAGE 'plpgsql' AS $$" +
                                                                                                                                                " DECLARE" +
                                                                                                                                                "    layerArray text[] := string_to_array(typeName, '.');" +
                                                                                                                                                "    levelSelectList text[];" +
                                                                                                                                                "    levelWhereList text[];" +
                                                                                                                                                "    parameters text[];" +
                                                                                                                                                "    theTypeName text;" +
                                                                                                                                                "    size integer;" +
                                                                                                                                                "    index integer;" +
                                                                                                                                                "    sql text;" +
                                                                                                                                                " BEGIN" +
                                                                                                                                                "    size := array_length(layerArray, 1);" +
                                                                                                                                                "    IF size IS null THEN" +
                                                                                                                                                "      size := 1;" +
                                                                                                                                                "      layerArray[1] := '*';" +
                                                                                                                                                "    END IF;" +
                                                                                                                                                "    index := 0;" +
                                                                                                                                                "    FOR i IN REVERSE size .. 1 LOOP" +
                                                                                                                                                "      theTypeName := layerArray[i];" +
                                                                                                                                                "      IF theTypeName <> '' AND theTypeName <> '*' AND theTypeName <> '＊' THEN" +
                                                                                                                                                "        index := index + 1;" +
                                                                                                                                                "        sql := ' AND name ILIKE $1[' || index || ']::text';" +
                                                                                                                                                "        parameters[index] := theTypeName;" +
                                                                                                                                                "      ELSE" +
                                                                                                                                                "        sql := '';" +
                                                                                                                                                "      END IF;" +
                                                                                                                                                "      levelSelectList := array_append(levelSelectList, '(SELECT * FROM branch WHERE level = ' || i || sql || ') AS level' || i);" +
                                                                                                                                                "      IF i > 1 THEN" +
                                                                                                                                                "        levelWhereList := array_append(levelWhereList, 'level' || i || '.parent = level' || (i - 1) || '.id');" +
                                                                                                                                                "      END IF;" +
                                                                                                                                                "  END LOOP;" +
                                                                                                                                                "  IF array_length(levelWhereList, 1) >= 1 THEN" +
                                                                                                                                                "    sql := ' WHERE ' || array_to_string(levelWhereList, ' AND ');" +
                                                                                                                                                "  ELSE" +
                                                                                                                                                "    sql := '';" +
                                                                                                                                                "  END IF;" +
                                                                                                                                                "  sql :=" +
                                                                                                                                                "    'WITH RECURSIVE cte AS' ||" +
                                                                                                                                                "    '  (' ||" +
                                                                                                                                                "    '    SELECT branch.* FROM branch,' ||" + //初始表
                                                                                                                                                "    '    (' ||" +
                                                                                                                                                "    '        SELECT level' || size ||'.* FROM ' || array_to_string(levelSelectList, ',') || sql ||" +
                                                                                                                                                "    '    ) AS levels' ||" +
                                                                                                                                                "    '    WHERE branch.id = levels.id' ||" +
                                                                                                                                                "    '    UNION ALL' ||" + //递归
                                                                                                                                                "    '    SELECT branch.* FROM branch' ||" +
                                                                                                                                                "    '    INNER JOIN cte' ||" +
                                                                                                                                                "    '    ON branch.parent = cte.id' ||" +
                                                                                                                                                "    '  )' ||" +
                                                                                                                                                "    '  SELECT DISTINCT id FROM cte';" + //剔除重复枝干
                                                                                                                                                "  IF path IS NOT true THEN" + //仅提取末端树梢
                                                                                                                                                "    sql := sql ||" +
                                                                                                                                                "    '  AS cte1 WHERE NOT EXISTS' ||" +
                                                                                                                                                "    '  (' ||" +
                                                                                                                                                "    '    SELECT id FROM cte AS cte2' ||" +
                                                                                                                                                "    '    WHERE cte1.id = cte2.parent' ||" +
                                                                                                                                                "    '  )';" +
                                                                                                                                                "  END IF;" +
                                                                                                                                                //"  --RAISE NOTICE '% %',sql,parameters;" +
                                                                                                                                                "  RETURN QUERY EXECUTE sql USING parameters;" +
                                                                                                                                                " END;" +
                                                                                                                                                " $$"
                                                                                                                                            );

                                                                                                                                    const string ogcBranch = "ogc_branch"; //相当于【ogc_branches】的反函数
                                                                                                                                    int.TryParse($"{PostgreSqlHelper.Scalar($"SELECT count(*) FROM pg_proc WHERE proname = '{ogcBranch}';")}",
                                                                                                                                        out var ogcBranchExist);
                                                                                                                                    if (ogcBranchExist == 0)
                                                                                                                                        PostgreSqlHelper.NonQuery
                                                                                                                                            (
                                                                                                                                                // 依据树梢id回溯至树根，返回隶属的枝干信息
                                                                                                                                                // id：通常为树梢id
                                                                                                                                                /* 返回样例
                                                                                                                                                    tree|levels |layer              |layerproperty|layerdetail|
                                                                                                                                                    ----+-------+-------------------+-------------+-----------+
                                                                                                                                                       1|{1,2,3}|{test,mapgis,POINT}|{,,}         |{,,}       |                                                                                                                                                 
                                                                                                                                                 */
                                                                                                                                                $"CREATE OR REPLACE FUNCTION public.{ogcBranch}(id integer) RETURNS TABLE(tree integer, levels smallint[], layer text[], layerproperty jsonb[], layerdetail xml[]) LANGUAGE 'plpgsql' AS $$" +
                                                                                                                                                " BEGIN" +
                                                                                                                                                "    RETURN QUERY" +
                                                                                                                                                "    WITH RECURSIVE cte AS" +
                                                                                                                                                "    (" +
                                                                                                                                                "      SELECT branch.* FROM branch" +
                                                                                                                                                "      WHERE branch.id = $1" + //此处启用参数：id
                                                                                                                                                "      UNION ALL" +
                                                                                                                                                "      SELECT branch.* FROM branch" +
                                                                                                                                                "      INNER JOIN cte" +
                                                                                                                                                "      ON branch.id = cte.parent" + //自树梢递归回溯至树根
                                                                                                                                                "    )" +
                                                                                                                                                "    SELECT * FROM" +
                                                                                                                                                "    (" +
                                                                                                                                                "      SELECT FIRST(t.tree) AS tree, ARRAY_AGG(t.level) as levels, ARRAY_AGG(t.name) AS layer, ARRAY_AGG(t.property) AS layerproperty, ARRAY_AGG(tt.detail) AS layerdetail" +
                                                                                                                                                "      FROM" +
                                                                                                                                                "      (" +
                                                                                                                                                "        SELECT * FROM cte ORDER BY level" +
                                                                                                                                                "      ) AS t" +
                                                                                                                                                "      LEFT JOIN branch_relation AS tt" +
                                                                                                                                                "      ON t.id = tt.branch" +
                                                                                                                                                "    ) AS t" +
                                                                                                                                                "    WHERE t.tree IS NOT NULL;" +
                                                                                                                                                " END;" +
                                                                                                                                                " $$"
                                                                                                                                            );

                                                                                                                                    _clusterUser
                                                                                                                                            .status =
                                                                                                                                        true;
                                                                                                                                }
                                                                                                                                else
                                                                                                                                    errorMessage =
                                                                                                                                        $"Failed to create some indexes of leaf_age - {PostgreSqlHelper.ErrorMessage}";
                                                                                                                            }
                                                                                                                            else
                                                                                                                                errorMessage =
                                                                                                                                    $"Failed to create leaf_age - {PostgreSqlHelper.ErrorMessage}";
                                                                                                                        }
                                                                                                                        else
                                                                                                                            errorMessage =
                                                                                                                                $"Failed to create leaf_hits - {PostgreSqlHelper.ErrorMessage}";
                                                                                                                    }
                                                                                                                    else
                                                                                                                        errorMessage =
                                                                                                                            $"Failed to create some indexes of leaf_wms - {PostgreSqlHelper.ErrorMessage}";
                                                                                                                }
                                                                                                                else
                                                                                                                    errorMessage =
                                                                                                                        $"Failed to create leaf_wms - {PostgreSqlHelper.ErrorMessage}";
                                                                                                            }
                                                                                                            else
                                                                                                                errorMessage =
                                                                                                                    $"Failed to create some indexes of leaf_tile - {PostgreSqlHelper.ErrorMessage}";
                                                                                                        }
                                                                                                        else
                                                                                                            errorMessage =
                                                                                                                $"Failed to create leaf_tile - {PostgreSqlHelper.ErrorMessage}";
                                                                                                    }
                                                                                                    else
                                                                                                        errorMessage =
                                                                                                            $"Failed to create some indexes of leaf_geometry - {PostgreSqlHelper.ErrorMessage}";
                                                                                                }
                                                                                                else
                                                                                                    errorMessage =
                                                                                                        $"Failed to create leaf_geometry - {PostgreSqlHelper.ErrorMessage}";
                                                                                            }
                                                                                            else
                                                                                                errorMessage =
                                                                                                    $"Failed to create some indexes of leaf_style - {PostgreSqlHelper.ErrorMessage}";
                                                                                        }
                                                                                        else
                                                                                            errorMessage =
                                                                                                $"Failed to create leaf_style - {PostgreSqlHelper.ErrorMessage}";
                                                                                    }
                                                                                    else
                                                                                        errorMessage =
                                                                                            $"Failed to create some indexes of leaf_description - {PostgreSqlHelper.ErrorMessage}";
                                                                                }
                                                                                else
                                                                                    errorMessage =
                                                                                        $"Failed to create leaf_description - {PostgreSqlHelper.ErrorMessage}";

                                                                            }
                                                                            else
                                                                                errorMessage = $"Failed to create some indexes of leaf - {PostgreSqlHelper.ErrorMessage}";
                                                                        }
                                                                        else
                                                                            errorMessage = $"Failed to create leaf - {PostgreSqlHelper.ErrorMessage}";
                                                                    }
                                                                    else
                                                                        errorMessage = $"Failed to create some indexes of branch - {PostgreSqlHelper.ErrorMessage}";
                                                                }
                                                                else
                                                                    errorMessage = $"Failed to create branch - {PostgreSqlHelper.ErrorMessage}";
                                                            }
                                                            else
                                                                errorMessage = $"Failed to create some indexes of tree - {PostgreSqlHelper.ErrorMessage}";
                                                        }
                                                        else
                                                            errorMessage = $"Failed to create tree - {PostgreSqlHelper.ErrorMessage}";
                                                    }
                                                    else
                                                        errorMessage = $"Failed to create some indexes of forest - {PostgreSqlHelper.ErrorMessage}";
                                                }
                                                else
                                                    errorMessage = $"Failed to create forest - {PostgreSqlHelper.ErrorMessage}";
                                            }
                                            else
                                                errorMessage = "No multilingual full text retrieval extension module (pgroonga) found";
                                        }
                                        else
                                            errorMessage = "One dimensional integer array extension module (intarray) not found";
                                    }
                                    else
                                        errorMessage = "No raster data expansion module was found（postgis_raster）";
                                }
                                else
                                    errorMessage = "No vector data expansion module was found（postgis）";
                            }
                            else
                                errorMessage = $"Unable to create database [{PostgreSqlHelper.ErrorMessage}]";
                            break;
                    }

                    if (string.IsNullOrWhiteSpace(errorMessage))
                    {
                        _clusterUser = (true, forest, GeositeServerUser.Text?.Trim());
                        _clusterDate = new DataGrid(
                            dataGridView: clusterDataPool,
                            firstPage: firstPage,
                            previousPage: previousPage,
                            nextPage: nextPage,
                            lastPage: lastPage,
                            pageBox: pagesBox,
                            deleteTree: deleteTree,
                            forest: forest
                        //,-1
                        //, 10
                        );
                    }
                }
                else
                {
                    _clusterUser.status = false;
                    errorMessage = @"Connection failed."; //通常因为服务器端管理员尚未设置账户群信息
                }

                return (Message: errorMessage, Host: host, Port: port, Administrator: administrator, DatabaseSize: databaseSize);
            });

            task.BeginInvoke(
                x =>
                {
                    var resultMessage = task.EndInvoke(x);
                    //this.
                        Invoke(
                        new Action(
                            () =>
                            {
                                if (resultMessage.Message == null)
                                {
                                    statusText.Text = @"Connection OK.";
                                    GeositeServerLink.BackgroundImage = Properties.Resources.linkok;

                                    deleteForest.Enabled = true;
                                    GeositeServerName.Text = resultMessage.Host;
                                    GeositeServerPort.Text = $@"{resultMessage.Port}";
                                    DatabaseSize.Text = resultMessage.DatabaseSize;

                                    dataGridPanel.Enabled =
                                    _postgreSqlConnection = true;
                                    PostgresRun.Enabled =
                                        dataCards.SelectedIndex == 0
                                        ? !string.IsNullOrWhiteSpace(themeNameBox.Text) && tilesource.SelectedIndex is >= 0 and <= 2
                                        : vectorFilePool.Rows.Count > 0;
                                }
                                else
                                {
                                    statusText.Text = resultMessage.Message;
                                    GeositeServerLink.BackgroundImage = Properties.Resources.linkfail;

                                    deleteForest.Enabled = false;
                                    DatabaseSize.Text =
                                        GeositeServerName.Text =
                                            GeositeServerPort.Text = "";
                                }
                                //ReIndex.Enabled = 
                                    ReClean.Enabled = resultMessage.Message == null && resultMessage.Administrator;
                                databasePanel.Enabled = true;
                                _loading.Run(false);
                            }
                        )
                    );
                },
                null
            );
        }

        private XElement GetClusterUserX(string serverUrl,string serverUser,string serverPassword)
        {
            return GeositeServerUsers.GetClusterUser(
                serverUrl,
                serverUser,
                //出于安全考虑，密码以哈希密文形式传输，以防链路侦听密码
                $"{GeositeConfuser.Cryptography.hashEncoder(serverPassword)}" 
            );
        }

        private void UpdateDatabaseSize(string serverUrl, string serverUser, string serverPassword)
        {
            var userX = GetClusterUserX(serverUrl, serverUser, serverPassword);
            if (userX != null)
            {
                DatabaseSize.Text =
                    userX.Element("Servers")
                        ?.Element("Server")
                        ?.Element("Database")
                        ?.Attribute("Size")
                        ?.Value ?? "";
            }
        }

        //private void ReIndex_Click(object sender, EventArgs e)
        //{
        //    _loading.Run();
        //    var task = new Action(() =>
        //    {
        //        try
        //        {
        //            ogcCard.Enabled = false;
        //            statusText.Text = @"● forest ...";
        //            Application.DoEvents();
        //            if (PostgreSqlHelper.NonQuery("REINDEX TABLE forest;", timeout: 0) == null)
        //                throw new Exception(PostgreSqlHelper.ErrorMessage);
        //            statusText.Text = @"● forest_relation ...";
        //            Application.DoEvents();
        //            if (PostgreSqlHelper.NonQuery("REINDEX TABLE forest_relation;", timeout: 0) == null)
        //                throw new Exception(PostgreSqlHelper.ErrorMessage);
        //            statusText.Text = @"● tree ...";
        //            Application.DoEvents();
        //            if (PostgreSqlHelper.NonQuery("REINDEX TABLE tree;", timeout: 0) == null)
        //                throw new Exception(PostgreSqlHelper.ErrorMessage);
        //            statusText.Text = @"● tree_relation ...";
        //            Application.DoEvents();
        //            if (PostgreSqlHelper.NonQuery("REINDEX TABLE tree_relation;", timeout: 0) == null)
        //                throw new Exception(PostgreSqlHelper.ErrorMessage);
        //            statusText.Text = @"● branch ...";
        //            Application.DoEvents();
        //            if (PostgreSqlHelper.NonQuery("REINDEX TABLE branch;", timeout: 0) == null)
        //                throw new Exception(PostgreSqlHelper.ErrorMessage);
        //            statusText.Text = @"● branch_relation ...";
        //            Application.DoEvents();
        //            if (PostgreSqlHelper.NonQuery("REINDEX TABLE branch_relation;", timeout: 0) == null)
        //                throw new Exception(PostgreSqlHelper.ErrorMessage);
        //            statusText.Text = @"● leaf ...";
        //            Application.DoEvents();
        //            if (PostgreSqlHelper.NonQuery("REINDEX TABLE leaf;", timeout: 0) == null)
        //                throw new Exception(PostgreSqlHelper.ErrorMessage);
        //            statusText.Text = @"● leaf_relation ...";
        //            Application.DoEvents();
        //            if (PostgreSqlHelper.NonQuery("REINDEX TABLE leaf_relation;", timeout: 0) == null)
        //                throw new Exception(PostgreSqlHelper.ErrorMessage);
        //            statusText.Text = @"● leaf_description ...";
        //            Application.DoEvents();
        //            if (PostgreSqlHelper.NonQuery("REINDEX TABLE leaf_description;", timeout: 0) == null)
        //                throw new Exception(PostgreSqlHelper.ErrorMessage);
        //            statusText.Text = @"● leaf_style ...";
        //            Application.DoEvents();
        //            if (PostgreSqlHelper.NonQuery("REINDEX TABLE leaf_style;", timeout: 0) == null)
        //                throw new Exception(PostgreSqlHelper.ErrorMessage);
        //            statusText.Text = @"● leaf_geometry ...";
        //            Application.DoEvents();
        //            if (PostgreSqlHelper.NonQuery("REINDEX TABLE leaf_geometry;", timeout: 0) == null)
        //                throw new Exception(PostgreSqlHelper.ErrorMessage);
        //            statusText.Text = @"● leaf_tile ...";
        //            Application.DoEvents();
        //            if (PostgreSqlHelper.NonQuery("REINDEX TABLE leaf_tile;", timeout: 0) == null)
        //                throw new Exception(PostgreSqlHelper.ErrorMessage);
        //            statusText.Text = @"● leaf_wms ...";
        //            Application.DoEvents();
        //            if (PostgreSqlHelper.NonQuery("REINDEX TABLE leaf_wms;", timeout: 0) == null)
        //                throw new Exception(PostgreSqlHelper.ErrorMessage);
        //            statusText.Text = @"● leaf_age ...";
        //            Application.DoEvents();
        //            if (PostgreSqlHelper.NonQuery("REINDEX TABLE leaf_age;", timeout: 0) == null)
        //                throw new Exception(PostgreSqlHelper.ErrorMessage);
        //            statusText.Text = @"● leaf_hits ...";
        //            Application.DoEvents();
        //            if (PostgreSqlHelper.NonQuery("REINDEX TABLE leaf_hits;", timeout: 0) == null)
        //                throw new Exception(PostgreSqlHelper.ErrorMessage);
        //            statusText.Text = @"Reindex finished";
        //            Application.DoEvents();
        //        }
        //        catch (Exception error)
        //        {
        //            statusText.Text = @$"Reindex failed ({error.Message})";
        //            Application.DoEvents();
        //        }
        //        finally
        //        {
        //            ogcCard.Enabled = true;
        //        }
        //    });
        //    task.BeginInvoke(
        //        (x) =>
        //        {
        //            task.EndInvoke(x);
        //            _loading.Run(false);
        //        }, null
        //    );
        //}

        private void ReClean_Click(object sender, EventArgs e)
        {
            var serverUrl = GeositeServerUrl.Text?.Trim();
            var serverUser = GeositeServerUser.Text?.Trim();
            var serverPassword = GeositeServerPassword.Text?.Trim();

            _loading.Run();

            var task = new Action(() =>
            {
                try
                {
                    ogcCard.Enabled = false;

                    // 先处理【键集】问题：刷新叶子表频度并删除键集子表内容
                    statusText.Text = @"● Access frequency synchronization ...";
                    Application.DoEvents();
                    GeositeHits.Refresh();

                    statusText.Text = @"● forest ...";
                    Application.DoEvents();
                    if (PostgreSqlHelper.NonQuery("VACUUM ANALYZE forest;", timeout: 0) == null)
                        throw new Exception(PostgreSqlHelper.ErrorMessage);
                    statusText.Text = @"● forest_relation ...";
                    Application.DoEvents();
                    if (PostgreSqlHelper.NonQuery("VACUUM ANALYZE forest_relation;", timeout: 0) == null)
                        throw new Exception(PostgreSqlHelper.ErrorMessage);

                    statusText.Text = @"● tree ...";
                    Application.DoEvents();
                    if (PostgreSqlHelper.NonQuery("VACUUM ANALYZE tree;", timeout: 0) == null)
                        throw new Exception(PostgreSqlHelper.ErrorMessage);
                    statusText.Text = @"● tree_relation ...";
                    Application.DoEvents();
                    if (PostgreSqlHelper.NonQuery("VACUUM ANALYZE tree_relation;", timeout: 0) == null)
                        throw new Exception(PostgreSqlHelper.ErrorMessage);

                    statusText.Text = @"● branch ...";
                    Application.DoEvents();
                    if (PostgreSqlHelper.NonQuery("VACUUM ANALYZE branch;", timeout: 0) == null)
                        throw new Exception(PostgreSqlHelper.ErrorMessage);
                    statusText.Text = @"● branch_relation ...";
                    Application.DoEvents();
                    if (PostgreSqlHelper.NonQuery("VACUUM ANALYZE branch_relation;", timeout: 0) == null)
                        throw new Exception(PostgreSqlHelper.ErrorMessage);

                    statusText.Text = @"● leaf ...";
                    Application.DoEvents();
                    if (PostgreSqlHelper.NonQuery("VACUUM ANALYZE leaf;", timeout: 0) == null)
                        throw new Exception(PostgreSqlHelper.ErrorMessage);
                    statusText.Text = @"● leaf_relation ...";
                    Application.DoEvents();
                    if (PostgreSqlHelper.NonQuery("VACUUM ANALYZE leaf_relation;", timeout: 0) == null)
                        throw new Exception(PostgreSqlHelper.ErrorMessage);
                    
                    statusText.Text = @"● leaf_description ...";
                    Application.DoEvents();
                    if (PostgreSqlHelper.NonQuery("VACUUM ANALYZE leaf_description;", timeout: 0) == null)
                        throw new Exception(PostgreSqlHelper.ErrorMessage);

                    statusText.Text = @"● leaf_style ...";
                    Application.DoEvents();
                    if (PostgreSqlHelper.NonQuery("VACUUM ANALYZE leaf_style;", timeout: 0) == null)
                        throw new Exception(PostgreSqlHelper.ErrorMessage);

                    statusText.Text = @"● leaf_geometry ...";
                    Application.DoEvents();
                    if (PostgreSqlHelper.NonQuery("VACUUM ANALYZE leaf_geometry;", timeout: 0) == null)
                        throw new Exception(PostgreSqlHelper.ErrorMessage);

                    statusText.Text = @"● leaf_tile ...";
                    Application.DoEvents();
                    if (PostgreSqlHelper.NonQuery("VACUUM ANALYZE leaf_tile;", timeout: 0) == null)
                        throw new Exception(PostgreSqlHelper.ErrorMessage);

                    statusText.Text = @"● leaf_wms ...";
                    Application.DoEvents();
                    if (PostgreSqlHelper.NonQuery("VACUUM ANALYZE leaf_wms;", timeout: 0) == null)
                        throw new Exception(PostgreSqlHelper.ErrorMessage);

                    statusText.Text = @"● leaf_age ...";
                    Application.DoEvents();
                    if (PostgreSqlHelper.NonQuery("VACUUM ANALYZE leaf_age;", timeout: 0) == null)
                        throw new Exception(PostgreSqlHelper.ErrorMessage);

                    statusText.Text = @"● leaf_hits ...";
                    Application.DoEvents();
                    if (PostgreSqlHelper.NonQuery("VACUUM ANALYZE leaf_hits;", timeout: 0) == null)
                        throw new Exception(PostgreSqlHelper.ErrorMessage);

                    statusText.Text = @"Reclean finished";
                    Application.DoEvents();

                    UpdateDatabaseSize(serverUrl, serverUser, serverPassword);
                }
                catch (Exception error)
                {
                    statusText.Text = @$"Reclean failed ({error.Message})";
                    Application.DoEvents();
                }
                finally
                {
                    ogcCard.Enabled = true;
                }
            });
            task.BeginInvoke(
                (x) =>
                {
                    task.EndInvoke(x);
                    _loading.Run(false);
                }, null
            );
        }

        private void firstPage_Click(object sender, EventArgs e)
        {
            _loading.Run();
            var task = new Action(() => _clusterDate?.First());
            task.BeginInvoke(
                (x) =>
                {
                    task.EndInvoke(x);
                    _loading.Run(false);
                }, null
            );
        }

        private void previousPage_Click(object sender, EventArgs e)
        {
            _loading.Run();
            var task = new Action(() => _clusterDate?.Previous());
            task.BeginInvoke(
                (x) =>
                {
                    task.EndInvoke(x);
                    _loading.Run(false);
                }, null
            );
        }

        private void nextPage_Click(object sender, EventArgs e)
        {
            _loading.Run();
            var task = new Action(() => _clusterDate?.Next());
            task.BeginInvoke(
                (x) =>
                {
                    task.EndInvoke(x);
                    _loading.Run(false);
                }, null
            );
        }

        private void lastPage_Click(object sender, EventArgs e)
        {
            _loading.Run();
            var task = new Action(() => _clusterDate?.Last());
            task.BeginInvoke(
                (x) =>
                {
                    task.EndInvoke(x);
                    _loading.Run(false);
                }, null
            );
        }

        private void deleteTree_Click(object sender, EventArgs e)
        {
            var selectedRows = clusterDataPool.SelectedRows;

            if (selectedRows.Count > 0 && MessageBox.Show(
                @"Are you sure you want to delete selected?",
                @"Caution",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Exclamation
            ) == DialogResult.Yes)
            {
                _loading.Run();
                statusText.Text = @"Deleting ...";
                databasePanel.Enabled = false;
                Application.DoEvents();

                var ids =
                    selectedRows
                        .Cast<DataGridViewRow>()
                        .Select(row => row.Cells[1])
                        .Select(typeCell => Regex.Split(typeCell.ToolTipText, @"[\b]",
                            RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Multiline))
                        .Select(typeCellArray => typeCellArray[0])
                        .ToList();

                var task = new Func<bool>(
                    () =>
                        PostgreSqlHelper.NonQuery(
                            $"DELETE FROM tree WHERE id in ({string.Join(",", ids)});"
                        ) != null
                    );
                task.BeginInvoke(
                    (x) =>
                    {
                        var result = task.EndInvoke(x);
                        //this.
                            Invoke(
                            new Action(
                                () =>
                                {
                                    _loading.Run(false);
                                    if (result)
                                    {
                                        var rowStack = new Stack<DataGridViewRow>();
                                        selectedRows
                                            .Cast<DataGridViewRow>()
                                            .Where(row => !row.IsNewRow)
                                            .ToList()
                                            .ForEach(row => rowStack.Push(row));
                                        while (rowStack.Count > 0)
                                        {
                                            try
                                            {
                                                clusterDataPool.Rows.Remove(rowStack.Pop());
                                            }
                                            catch
                                            {
                                                //dataPool必须设置为【允许删除】，否则导致异常？
                                            }
                                        }
                                        _clusterDate?.Reset();
                                    }
                                    statusText.Text = @"Delete succeeded";
                                    databasePanel.Enabled = true;
                                }
                            )
                        );
                    },
                    null
                );
            }
        }

        private void statusText_DoubleClick(object sender, EventArgs e)
        {
            //Clipboard.SetDataObject(
            //    Regex.Replace(
            //        statusText.Text,
            //        @"Layers\s\-\s\[([\s\S]*?)\]", "$1",
            //        RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace | RegexOptions.Singleline |
            //        RegexOptions.Multiline
            //    ).Trim()
            //);
            Clipboard.SetDataObject(statusText.Text.Trim());
        }

        private void clusterDataPool_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            var colIndex = e.ColumnIndex;
            var rowIndex = e.RowIndex;
            if (colIndex == -1 && rowIndex >= 0) //如果点击了最左侧单元格
            {
                _loading.Run();
                // $"{id}\b{timestamp}\b{status}"
                var tree = Regex.Split(((DataGridView)sender).Rows[rowIndex].Cells[1].ToolTipText, @"[\b]", RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace | RegexOptions.Singleline | RegexOptions.Multiline)[0];
                //this.
                    Invoke(new Action(
                        () =>
                        {
                            statusText.Text =
                                @"Layers - [ " +
                                (string) PostgreSqlHelper.Scalar
                                (
                                    "SELECT array_to_string(array_agg(name), '/') FROM (SELECT name FROM branch WHERE tree = @tree ORDER BY level) AS route;",
                                    new Dictionary<string, object>
                                    {
                                        {"tree", int.Parse(tree)}
                                    }
                                ) +
                                @" ]";
                            _loading.Run(false);
                        }
                    )
                );
            }
        }

        private void dataPool_CellBeginEdit(object sender, DataGridViewCellCancelEventArgs e)
        {
            var colIndex = e.ColumnIndex;
            var rowIndex = e.RowIndex;
            if (colIndex == 0 && rowIndex >= 0) //theme 列
                _clusterDateGridCell = $"{((DataGridView) sender).Rows[rowIndex].Cells[colIndex].Value}".Trim();
        }

        private void dataPool_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            var colIndex = e.ColumnIndex;
            var rowIndex = e.RowIndex;
            if (colIndex == 0 && rowIndex >= 0) //theme 列
            {
                var row = ((DataGridView)sender).Rows[rowIndex];
                var col = row.Cells[colIndex];
                var newName = $"{col.Value}".Trim();
                var oldName = _clusterDateGridCell;
                if (string.IsNullOrWhiteSpace(newName))
                    col.Value = newName = oldName;
                else
                {
                    try
                    {
                        newName = new XElement(newName).Name.LocalName;
                    }
                    catch
                    {
                        col.Value = newName = oldName;
                    }
                }

                if (newName != oldName)
                {
                    _loading.Run();

                    var forest = _clusterUser.forest;
                    var oldId = PostgreSqlHelper.Scalar(
                        "SELECT id FROM tree WHERE forest = @forest AND name ILIKE @name::text LIMIT 1;",
                        new Dictionary<string, object>()
                        {
                            {"forest", forest},
                            {"name", newName}
                        }
                    );
                    if (oldId != null)
                    {
                        row.Cells[colIndex].Value = oldName;
                        MessageBox.Show(
                            $@"Duplicate [{newName}] are not allowed.",
                            @"Tip",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error
                        );
                    }
                    else
                    {
                        var typeCellArray = Regex.Split(
                            row.Cells[1].ToolTipText,
                            @"[\b]",
                            RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Multiline
                        );
                        var id = typeCellArray[0];

                        //var timestamp = typeCellArray[1]; //0,0,20210714,41031 //Regex.Split(typeCellArray[1], "[,]", RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Multiline);
                        //var status = typeCellArray[2];
                        //var nameCell = row.Cells[1];
                        //var uri = nameCell.ToolTipText;
                        //var name = nameCell.Value;

                        if (PostgreSqlHelper.NonQuery(
                            "UPDATE tree SET name = @name WHERE id = @id;", //@name::text
                            new Dictionary<string, object>
                            {
                                {"name", newName},
                                {"id", long.Parse(id)}
                            }
                            ) == null)
                        {
                            row.Cells[colIndex].Value = oldName;
                            if (!string.IsNullOrWhiteSpace(PostgreSqlHelper.ErrorMessage))
                                MessageBox.Show(
                                    PostgreSqlHelper.ErrorMessage,
                                    @"Tip",
                                    MessageBoxButtons.OK,
                                    MessageBoxIcon.Error
                                );
                        }
                    }

                    _loading.Run(false);
                }
            }
            _clusterDateGridCell = null;
        }

        private void VectorOpen_Click(object sender, EventArgs e)
        {
            var key = VectorOpen.Name;
            int.TryParse(RegEdit.Getkey(key), out var filterIndex);

            var pathKey = key + "_path";
            var oldPath = RegEdit.Getkey(pathKey);

            var openFileDialog = new OpenFileDialog
            {
                Filter = @"MapGIS|*.wt;*.wl;*.wp|ShapeFile|*.shp|Excel Tab Delimited|*.txt|Excel Comma Delimited|*.csv|GoogleEarth(*.kml)|*.kml|GeositeXML|*.xml|GeoJson|*.geojson",
                FilterIndex = filterIndex,
                Multiselect = true

            };
            if (Directory.Exists(oldPath))
                openFileDialog.InitialDirectory = oldPath;

            if (openFileDialog.ShowDialog() != DialogResult.OK)
                return;
            RegEdit.Setkey(key, $"{openFileDialog.FilterIndex}");
            RegEdit.Setkey(pathKey, Path.GetDirectoryName(openFileDialog.FileName));

            var files = openFileDialog.FileNames;
            foreach (var path in files)
            {
                //var LastWriteTime = File.GetLastWriteTime(path);
                var theme = Path.GetFileNameWithoutExtension(path);
                try
                {
                    vectorFilePool.Rows.Add(new XElement(theme).Name.LocalName, path);
                }
                catch
                {
                    vectorFilePool.Rows.Add($"Untitled_{theme}", path);
                }
            }
            //vectorFilePool_RowsRemoved(sender, null);
        }

        private void vectorFilePool_RowsAdded(object sender, DataGridViewRowsAddedEventArgs e)
        {
            vectorFilePool_RowsRemoved(sender, null);
        }

        private void vectorFilePool_RowsRemoved(object sender, DataGridViewRowsRemovedEventArgs e)
        {
            VectorFileClear.Enabled = vectorFilePool.Rows.Count > 0;
            PostgresRun.Enabled = _postgreSqlConnection && VectorFileClear.Enabled;
        }

        private void VectorFileClear_Click(object sender, EventArgs e)
        {
            foreach (var row in vectorFilePool.SelectedRows.Cast<DataGridViewRow>().Where(row => !row.IsNewRow))
            {
                try
                {
                    vectorFilePool.Rows.Remove(row);
                }
                catch
                {
                    //
                }
            }

            vectorFilePool_RowsRemoved(sender, null);
        }

        private void PostgresRun_Click(object sender, EventArgs e)
        {
            if (fileWorker.IsBusy || vectorWorker.IsBusy || rasterWorker.IsBusy)
                return;

            switch (dataCards.SelectedIndex)
            {
                case 0:
                    RasterRunClick();
                    break;
                case 1:
                    VectorRunClick();
                    break;
            }
        }

        private void VectorRunClick()
        {
            if (vectorFilePool.SelectedRows.Cast<DataGridViewRow>().All(row => row.IsNewRow))
                return;

            _loading.Run();
            ogcCard.Enabled =
            PostgresRun.Enabled = false;
            statusProgress.Visible = true;
            vectorWorker.RunWorkerAsync(); // 异步执行 VectorWorkStart 函数
        }

        private string VectorWorkStart(BackgroundWorker vectorBackgroundWorker, DoWorkEventArgs e)
        {
            if (vectorBackgroundWorker.CancellationPending)
            {
                e.Cancel = true;
                return "Pause...";  
            }

            var doTopology = topologyCheckBox.Checked; //针对矢量数据，是否执行【拓扑】？ 
            var forest = _clusterUser.forest;
            var oneForest = new GeositeXmlPush();

            var forestResult = oneForest.Forest(
                id: forest,
                name: _clusterUser.name
            //, timestamp: $"{DateTime.Now: yyyyMMdd, HHmmss}"
            );

            if (!forestResult.Success)
                return forestResult.Message; //此结果信息将出现在状态行

            /*  
            文档树状态码status，继承自[forest.status]，含义如下
            持久化	暗数据	完整性	含义
            ======	======	======	==============================================================
            0		0		0		默认值0：非持久化数据（参与对等）		明数据		无值或失败
            0		0		1		指定值1：非持久化数据（参与对等）		明数据		正常
            0		1		0		指定值2：非持久化数据（参与对等）		暗数据		失败
            0		1		1		指定值3：非持久化数据（参与对等）		暗数据		正常
            1		0		0		指定值4：持久化数据（不参与后续对等）	明数据		失败
            1		0		1		指定值5：持久化数据（不参与后续对等）	明数据		正常
            1		1		0		指定值6：持久化数据（不参与后续对等）	暗数据		失败
            1		1		1		指定值7：持久化数据（不参与后续对等）	暗数据		正常
            */
            var status = (short)(PostgresLight.Checked ? 4 : 6);
            string statusInfo = null;

            foreach (var row in vectorFilePool.SelectedRows.Cast<DataGridViewRow>().Where(row => !row.IsNewRow).OrderBy(row => row.Index)) //vectorFilePool.Rows
            {
                var theme = $"{row.Cells[0].Value}";
                var path = $"{row.Cells[1].Value}";
                var statusCell = row.Cells[2];

                //this.
                    Invoke(
                    new Action(
                        () =>
                        {
                            vectorFilePool.CurrentCell = row.Cells[2]; //滚动到当前单元格 
                        }
                    )
                );

                var oldTree = PostgreSqlHelper.Scalar(
                    "SELECT id FROM tree WHERE forest = @forest AND (name ILIKE @name::text) LIMIT 1;", // OR (timestamp[3] = @timestamp3 AND timestamp[4] = @timestamp4)
                    new Dictionary<string, object>
                    {
                        {"forest", forest},
                        {"name", theme}
                        //,
                        //{"timestamp3", yyyyMMdd}, 
                        //{"timestamp4", HHmmss}
                    }
                );
                if (oldTree != null)
                {
                    //this.
                        Invoke(
                        new Action(
                            () =>
                            {
                                statusCell.Value = "✔!";
                                statusCell.ToolTipText = "Exist";
                            }
                        )
                    );
                }
                else
                {
                    //this.
                        Invoke(
                        new Action(
                            () =>
                            {
                                statusCell.Value = "…";
                                statusCell.ToolTipText = "Processing";
                            }
                        )
                    );

                    var sequenceMax =
                        PostgreSqlHelper.Scalar(
                            "SELECT sequence FROM tree WHERE forest = @forest ORDER BY sequence DESC LIMIT 1;",
                            new Dictionary<string, object>
                            {
                                {"forest", forest}
                            }
                        );
                    var sequence = sequenceMax == null ? 0 : 1 + int.Parse($"{sequenceMax}");

                    var fileType = Path.GetExtension(path).ToLower();
                    switch (fileType)
                    {
                        case ".wt":
                        case ".wl":
                        case ".wp":
                            {
                                try
                                {
                                    string treePathString = null;
                                    XElement description = null;
                                    var canDo = true;
                                    if (!_noPromptLayersBuilder)
                                    {
                                        var getTreeLayers = new LayersBuilder(new FileInfo(path).FullName);
                                        getTreeLayers.ShowDialog();
                                        if (getTreeLayers.Ok)
                                        {
                                            treePathString = getTreeLayers.TreePathString;
                                            description = getTreeLayers.Description;
                                            _noPromptLayersBuilder = getTreeLayers.DonotPrompt;
                                        }
                                        else
                                            canDo = false;
                                    }
                                    else
                                    {
                                        treePathString = ConsoleIO.FilePathToXPath(new FileInfo(path).FullName);
                                    }
                                    if (canDo)
                                    {
                                        using var mapgis = new MapGis.MapGisFile();
                                        mapgis.onGeositeEvent += delegate (object _, GeositeEventArgs thisEvent)
                                        {
                                            vectorBackgroundWorker.ReportProgress(
                                                thisEvent.progress ?? -1,
                                                thisEvent.message ?? string.Empty
                                            );
                                        };

                                        mapgis.Open(path);
                                        //-------------------------------
                                        {
                                            if (mapgis.RecordCount == 0)
                                                throw new Exception("No features found");
                                            mapgis.fire("Preprocessing ...", 0);
                                            var getFileInfo = mapgis.GetCapabilities();
                                            var getFileType = $"{getFileInfo["fileType"]}";

                                            //处理属性问题 
                                            var fields = mapgis.GetField();
                                            var haveFields = fields.Length > 0;

                                            var featureCollectionX = new XElement(
                                                "FeatureCollection",
                                                new XAttribute("type", getFileType),
                                                new XAttribute("timeStamp", $"{getFileInfo["timeStamp"]}"),
                                                new XElement("name", theme)
                                                );
                                            if (description != null)
                                                featureCollectionX.Add(
                                                    new XElement
                                                    (
                                                        "property",
                                                        description
                                                            .Elements()
                                                            .Select(x => new XElement($"{x.Name}", x.Value))
                                                    )
                                                );
                                            var bbox = (JArray)getFileInfo["bbox"]; // $"[{west}, {south}, {east}, {north}]"
                                            featureCollectionX.Add(
                                                new XElement(
                                                    "boundary",
                                                    new XElement("north", $"{bbox[3]}"),
                                                    new XElement("south", $"{bbox[1]}"),
                                                    new XElement("west", $"{bbox[0]}"),
                                                    new XElement("east", $"{bbox[2]}")
                                                    )
                                                );

                                            var treeTimeStamp =
                                                $"{forest},{sequence},{DateTime.Parse($"{getFileInfo["timeStamp"]}"): yyyyMMdd,HHmmss}";
                                            var treeResult = 
                                                oneForest.Tree(
                                                    treeTimeStamp,
                                                    featureCollectionX,
                                                    path,
                                                    status
                                                );
                                            if (treeResult.Success)
                                            {
                                                var pointer = 0; 
                                                var valid = 0;
                                                var treePath = treePathString;
                                                if (string.IsNullOrWhiteSpace(treePath))
                                                    treePath = "Untitled";
                                                var treeNameArray = Regex.Split(treePath, @"[\/\\\|]+");
                                                var treeId = treeResult.Id;
                                                //此时，文档树所容纳的叶子类型type默认值：0
                                                var treeType = new List<int>();
                                                var isOk = true;
                                                var recordCount = mapgis.RecordCount;

                                                // 为提升进度视觉体验，特将进度值限定在0--10之间
                                                var leafPointer = 0;
                                                var oldscale10 = -1;
                                                var flagMany = 10.0 / recordCount;
                                                var scale1 = (int)Math.Ceiling(flagMany);
                                                var flag10 = 0;

                                                //提供追加元数据的机会
                                                XElement themeMetadataX = null;
                                                if (!_noPromptMetaData)
                                                {
                                                    var metaData = new MetaData();
                                                    metaData.ShowDialog();
                                                    if (metaData.Ok)
                                                    {
                                                        themeMetadataX = metaData.MetaDataX;
                                                        _noPromptMetaData = metaData.DonotPrompt;
                                                    }
                                                }

                                                //最末层
                                                XElement layerX = null;
                                                for (var index = treeNameArray.Length - 1; index >= 0; index--)
                                                {
                                                    layerX = new XElement(
                                                        "layer",
                                                        new XElement("name", treeNameArray[index].Trim()),
                                                        //将元数据添加到最末层
                                                        index == treeNameArray.Length - 1 ? themeMetadataX : null,
                                                        index == treeNameArray.Length - 1 ? new XElement("member") : null,
                                                        layerX
                                                    );
                                                }
                                                featureCollectionX.Add(layerX);

                                                //写叶子
                                                //由叶子对象反向回溯并创建枝干分类谱系，返回枝干谱系id数组
                                                var createRoute = oneForest.Branch(
                                                    forest: forest,
                                                    sequence: sequence,
                                                    tree: treeId,
                                                    leafX: featureCollectionX.Descendants("member").First(),
                                                    leafRootX: featureCollectionX
                                                );
                                                if (!createRoute.Success)
                                                {
                                                    //this.
                                                        Invoke(
                                                        new Action(
                                                            () =>
                                                            {
                                                                statusCell.Value = "✘";
                                                                statusCell.ToolTipText = createRoute.Message;
                                                            }
                                                        )
                                                    );
                                                    isOk = false;
                                                }
                                                else
                                                {
                                                    foreach (var feature in mapgis.GetFeature())
                                                    {
                                                        pointer++;
                                                        if (feature != null)
                                                        {
                                                            var featureType = $"{feature["geometry"]["type"]}";
                                                            mapgis.fire(
                                                                message: $"{featureType} [{pointer} / {recordCount}]",
                                                                1,
                                                                progress: 100 * pointer / recordCount
                                                            );
                                                            var featureId = $"{feature["id"]}";
                                                            try
                                                            {
                                                                XElement elementdescriptionX;
                                                                if (haveFields)
                                                                {
                                                                    //处理属性问题 
                                                                    var fieldValues = ((JObject)feature["properties"])
                                                                        .Properties()
                                                                        .Select(field => $"{field.Value["value"]}")
                                                                        .ToArray();

                                                                    elementdescriptionX = new XElement("property");
                                                                    for (var item = 0; item < fields.Length; item++)
                                                                        elementdescriptionX.Add(
                                                                            new XElement(
                                                                                Regex.Replace($"{fields[item]["name"]}", @"[:""（）\(\)]+", "_",
                                                                                    RegexOptions.IgnoreCase | RegexOptions.Singleline |
                                                                                    RegexOptions.Multiline)
                                                                                ,
                                                                                fieldValues[item]
                                                                            )
                                                                        );
                                                                }
                                                                else
                                                                    elementdescriptionX = null;

                                                                //几何坐标 = GeoJson
                                                                var coordinates =
                                                                    //(JArray)((JObject)feature["geometry"])["coordinates"];
                                                                    feature["geometry"].ToString(Formatting.None);
                                                              
                                                                //内点
                                                                var centroid = (JArray)feature["centroid"];
                                                                //边框 (double west, double south, double east, double north)
                                                                var featureBbox = (JArray)feature["bbox"];
                                                                var featureBoundaryX = new XElement(
                                                                    "boundary",
                                                                    new XAttribute(
                                                                        "centroid", $"{centroid[0]} {centroid[1]}"
                                                                    ),
                                                                    new XElement(
                                                                        "north", $"{featureBbox[3]}"
                                                                    ),
                                                                    new XElement(
                                                                        "south", $"{featureBbox[1]}"
                                                                    ),
                                                                    new XElement(
                                                                        "west", $"{featureBbox[0]}"
                                                                    ),
                                                                    new XElement(
                                                                        "east", $"{featureBbox[2]}"
                                                                    )
                                                                );
                                                                var featureTimeStamp = feature["timeStamp"]?.Value<string>();  //DateTime.Now.ToString("s");

                                                                var style = (JObject)feature["style"];

                                                                XElement featureX = null;
                                                                switch (featureType)
                                                                {
                                                                    case "Point":
                                                                        //var subType = $"{feature["subType"]}"; //subType == "0" || subType == "5" ? style["text"] : ""
                                                                        featureX = new XElement
                                                                        (
                                                                            "member",
                                                                            new XAttribute("type", "Point"),
                                                                            new XAttribute("typeCode", "1"),
                                                                            new XAttribute("id", featureId),
                                                                            new XAttribute("timeStamp", featureTimeStamp),
                                                                            elementdescriptionX,
                                                                            new XElement(
                                                                                "geometry",
                                                                                //$"POINT({coordinates[0]} {coordinates[1]})"
                                                                                coordinates
                                                                            ),
                                                                            featureBoundaryX,
                                                                            new XElement(
                                                                                "style",
                                                                                style.Properties()
                                                                                    .Select(field =>
                                                                                        new XElement(field.Name,
                                                                                            field.Value.ToString()))
                                                                            )
                                                                        );

                                                                        break;
                                                                    case "LineString":
                                                                        featureX = new XElement
                                                                        (
                                                                            "member",
                                                                            new XAttribute("type", "Line"),
                                                                            new XAttribute("typeCode", "2"),
                                                                            new XAttribute("id", featureId),
                                                                            new XAttribute("timeStamp", featureTimeStamp),
                                                                            elementdescriptionX,
                                                                            new XElement(
                                                                                "geometry",
                                                                                //"LINESTRING(" + string.Join(",", coordinates.Select(coor => $"{coor[0]} {coor[1]}").ToArray()) + ")"
                                                                                coordinates
                                                                            ),
                                                                            featureBoundaryX,
                                                                            new XElement(
                                                                                "style",
                                                                                style.Properties()
                                                                                    .Select(field =>
                                                                                        new XElement(field.Name,
                                                                                            field.Value.ToString()))
                                                                            )
                                                                        );

                                                                        break;
                                                                    case "Polygon":
                                                                        featureX = new XElement
                                                                        (
                                                                            "member",
                                                                            new XAttribute("type", "Polygon"),
                                                                            new XAttribute("typeCode", "3"),
                                                                            new XAttribute("id", featureId),
                                                                            new XAttribute("timeStamp",
                                                                                featureTimeStamp),
                                                                            elementdescriptionX,
                                                                            new XElement(
                                                                                "geometry",
                                                                                //WKT：
                                                                                //单面 POLYGON((x y z ...,x y z ...,x y z ...)) 
                                                                                //母子面 POLYGON((x y z ...,x y z ...,x y z ...),(x y z ...,x y z ...,x y z ...),(x y z ...,x y z ...,x y z ...),...)
                                                                                //多面已处理为单面或母子面 MULTIPOLYGON(((x y z ...,x y z ...,x y z ...),(x y z ...,x y z ...,x y z ...)), ((x y z ...,x y z ...,x y z ...)))
                                                                                //"POLYGON(" + string.Join(",", coordinates.Cast<JArray>().Select(coordinate => "(" + string.Join(",", coordinate.Select(xy => $"{xy[0]} {xy[1]}").ToArray()) + ")").ToList()) + ")"
                                                                                coordinates
                                                                            ),
                                                                            featureBoundaryX,
                                                                            new XElement(
                                                                                "style",
                                                                                style.Properties()
                                                                                    .Select(field =>
                                                                                        new XElement(field.Name,
                                                                                            field.Value.ToString()))
                                                                            )
                                                                        );

                                                                        break;
                                                                }
                                                                if (featureX != null)
                                                                {
                                                                    //写叶子

                                                                    //依据枝干正向分类谱系创建叶子记录
                                                                    var createLeaf = oneForest.Leaf(
                                                                        route: createRoute.Route,
                                                                        leafX: featureX,
                                                                        timestamp:
                                                                        $"{DateTime.Parse(featureX.Attribute("timeStamp").Value): yyyyMMdd,HHmmss}",
                                                                        topology: doTopology
                                                                    );

                                                                    var scale10 =
                                                                        (int)Math.Ceiling(10.0 * ++leafPointer /
                                                                            recordCount);

                                                                    if (scale10 > oldscale10)
                                                                    {
                                                                        oldscale10 = scale10;
                                                                        flag10 += scale1;
                                                                        if (flag10 < 10)
                                                                            mapgis.fire(path,
                                                                                1,
                                                                                progress: scale10 * 10);
                                                                        else
                                                                        {
                                                                            //目的是凑满10个刻度
                                                                            var rest = 10 - (flag10 - scale1);
                                                                            if (rest > 0)
                                                                                mapgis.fire(path,
                                                                                    1,
                                                                                    progress: 10 * 10);
                                                                        }
                                                                    }

                                                                    if (createLeaf.Success)
                                                                    {
                                                                        if (!treeType.Contains(createLeaf.Type))
                                                                            treeType.Add(createLeaf.Type);

                                                                        valid++;
                                                                    }
                                                                    else
                                                                    {
                                                                        //this.
                                                                            Invoke(
                                                                            new Action(
                                                                                () =>
                                                                                {
                                                                                    statusCell.Value = "!";
                                                                                    statusCell.ToolTipText =
                                                                                        createLeaf.Message;
                                                                                }
                                                                            )
                                                                        );

                                                                        isOk = false;
                                                                        //break; //即使出现异常，也继续遍历
                                                                    }
                                                                }
                                                            }
                                                            catch (Exception localError)
                                                            {
                                                                isOk = false;
                                                                //this.
                                                                    Invoke(
                                                                    new Action(
                                                                        () =>
                                                                        {
                                                                            statusCell.Value = "!";
                                                                            statusCell.ToolTipText = localError.Message;
                                                                        }
                                                                    )
                                                                );
                                                            }
                                                        }
                                                    }

                                                    mapgis.fire(
                                                        $" [{valid} feature{(valid > 1 ? "s" : "")}]", 200);
                                                }

                                                oneForest.Tree(enclosure: (treeId,
                                                    treeType, isOk)); //向树记录写入完整性标志以及类型数组
                                                _clusterDate?.Reset();
                                                if (isOk)
                                                {
                                                    //（0：非空间数据【默认】
                                                    //1：Point点、2：Line线、3：Polygon面、4：Image地理贴图
                                                    //10000：Tile栅格  金字塔瓦片     wms服务类型     [epsg:0       无投影瓦片]
                                                    //10001：Tile栅格  金字塔瓦片     wms服务类型     [epsg:4326    地理坐标系瓦片]
                                                    //10002：Tile栅格  金字塔瓦片     wms服务类型     [epsg:3857    球体墨卡托瓦片]
                                                    //11000：Tile栅格  金字塔瓦片     wmts服务类型    [epsg:0       无投影瓦片]
                                                    //11001：Tile栅格  金字塔瓦片     wmts服务类型    [epsg:4326    地理坐标系瓦片]
                                                    //11002：Tile栅格  金字塔瓦片     wmts服务类型    [epsg:3857    球体墨卡托瓦片]
                                                    //12000：Tile栅格  平铺式瓦片     wps服务类型     [epsg:0       无投影瓦片]
                                                    //12001：Tile栅格  平铺式瓦片     wps服务类型     [epsg:4326    地理坐标系瓦片]
                                                    //12002：Tile栅格  平铺式瓦片     wps服务类型     [epsg:3857    球体墨卡托瓦片]

                                                    //this.
                                                        Invoke(
                                                        new Action(
                                                            () =>
                                                            {
                                                                statusCell.Value = "✔"; 
                                                                statusCell.ToolTipText = "OK";
                                                            }
                                                        )
                                                    );
                                                }
                                            }
                                            else
                                            {
                                                //this.
                                                    Invoke(
                                                    new Action(
                                                        () =>
                                                        {
                                                            statusCell.Value = "✘";
                                                            statusCell.ToolTipText = treeResult.Message;
                                                        }
                                                    )
                                                );
                                            }
                                        }
                                    }
                                    else
                                    {
                                        //this.
                                            Invoke(
                                            new Action(
                                                () =>
                                                {
                                                    statusCell.Value = "?";
                                                    statusCell.ToolTipText = "Cancelled";
                                                }
                                            )
                                        );
                                    }
                                }
                                catch (Exception error)
                                {
                                    //this.
                                        Invoke(
                                        new Action(
                                            () =>
                                            {
                                                statusCell.Value = "!";
                                                statusCell.ToolTipText = error.Message;
                                            }
                                        )
                                    );
                                }
                            }
                            break;
                        case ".shp":
                            {
                                try
                                {
                                    string treePathString = null;
                                    XElement description = null;
                                    var canDo = true;
                                    if (!_noPromptLayersBuilder)
                                    {
                                        var getTreeLayers = new LayersBuilder(new FileInfo(path).FullName);
                                        getTreeLayers.ShowDialog();
                                        if (getTreeLayers.Ok)
                                        {
                                            treePathString = getTreeLayers.TreePathString;
                                            description = getTreeLayers.Description;
                                            _noPromptLayersBuilder = getTreeLayers.DonotPrompt;
                                        }
                                        else
                                            canDo = false;
                                    }
                                    else
                                    {
                                        treePathString = ConsoleIO.FilePathToXPath(new FileInfo(path).FullName);
                                    }
                                    if (canDo)
                                    {
                                        var codePage = ShapeFile.ShapeFile.GetDbfCodePage(Path.Combine(
                                                                                    Path.GetDirectoryName(path) ?? "",
                                                                                    Path.GetFileNameWithoutExtension(path) + ".dbf"));

                                        using var shapeFile = new ShapeFile.ShapeFile();
                                        shapeFile.onGeositeEvent += delegate (object _, GeositeEventArgs thisEvent)
                                        {
                                            vectorBackgroundWorker.ReportProgress(thisEvent.progress ?? -1,
                                                thisEvent.message ?? string.Empty);
                                        };

                                        shapeFile.Open(path, codePage);

                                        //-------------------------------
                                        if (shapeFile.RecordCount == 0)
                                            return "No features found";

                                        shapeFile.fire("Preprocessing ...", 0);
                                        var getFileInfo = shapeFile.GetCapabilities();
                                        var getFileType = $"{getFileInfo["fileType"]}";

                                        //处理属性问题 
                                        var fields = shapeFile.GetField();
                                        var haveFields = fields.Length > 0;

                                        var featureCollectionX = new XElement(
                                            "FeatureCollection",
                                            new XAttribute("type", getFileType),
                                            new XAttribute("timeStamp", $"{getFileInfo["timeStamp"]}"),
                                            new XElement("name", theme)
                                        );
                                        if (description != null)
                                        {
                                            featureCollectionX.Add
                                            (
                                                new XElement
                                                (
                                                    "property",
                                                    description
                                                        .Elements()
                                                        .Select(x => new XElement($"{x.Name}", x.Value))
                                                )
                                            );
                                        }

                                        var bbox = (JArray)getFileInfo[
                                            "bbox"]; // $"[{west}, {south}, {east}, {north}]"
                                        featureCollectionX.Add(
                                            new XElement(
                                                "boundary",
                                                new XElement("north", $"{bbox[3]}"),
                                                new XElement("south", $"{bbox[1]}"),
                                                new XElement("west", $"{bbox[0]}"),
                                                new XElement("east", $"{bbox[2]}")
                                            )
                                        );
                                        var pointer = 0;
                                        var valid = 0;
                                        var getTreePath = treePathString;
                                        if (string.IsNullOrWhiteSpace(getTreePath))
                                            getTreePath = "Untitled";
                                        var treeNameArray = Regex.Split(getTreePath, @"[\/\\\|]+");
                                        var treeTimeStamp =
                                            $"{forest},{sequence},{DateTime.Parse($"{getFileInfo["timeStamp"]}"): yyyyMMdd,HHmmss}";
                                        var treeResult =
                                            oneForest.Tree(
                                                treeTimeStamp,
                                                featureCollectionX,
                                                path,
                                                status
                                            );
                                        if (treeResult.Success)
                                        {
                                            var treeId = treeResult.Id;
                                            //此时，文档树所容纳的叶子类型type默认值：0
                                            var treeType = new List<int>();
                                            var isOk = true;
                                            var recordCount = shapeFile.RecordCount;

                                            // 为提升进度视觉体验，特将进度值限定在0--10之间
                                            var leafPointer = 0;
                                            var oldscale10 = -1;
                                            var flagMany = 10.0 / recordCount;
                                            var scale1 = (int)Math.Ceiling(flagMany);
                                            var flag10 = 0;

                                            //提供追加元数据的机会
                                            XElement themeMetadataX = null;
                                            if (!_noPromptMetaData)
                                            {
                                                var metaData = new MetaData();
                                                metaData.ShowDialog();
                                                if (metaData.Ok)
                                                {
                                                    themeMetadataX = metaData.MetaDataX;
                                                    _noPromptMetaData = metaData.DonotPrompt;
                                                }
                                            }

                                            //最末层
                                            XElement layerX = null;
                                            for (var index = treeNameArray.Length - 1; index >= 0; index--)
                                            {
                                                layerX = new XElement(
                                                    "layer",
                                                    new XElement("name", treeNameArray[index].Trim()),
                                                    //将元数据添加到最末层
                                                    index == treeNameArray.Length - 1 ? themeMetadataX : null,
                                                    index == treeNameArray.Length - 1
                                                        ? new XElement("member")
                                                        : null,
                                                    layerX
                                                );
                                            }

                                            featureCollectionX.Add(layerX);

                                            //写叶子
                                            //由叶子对象反向回溯并创建枝干分类谱系，返回枝干谱系id数组
                                            var createRoute = oneForest.Branch(
                                                forest: forest,
                                                sequence: sequence,
                                                tree: treeId,
                                                leafX: featureCollectionX.Descendants("member").First(),
                                                leafRootX: featureCollectionX
                                            );
                                            if (!createRoute.Success)
                                            {
                                                //this.
                                                    Invoke(
                                                    new Action(
                                                        () =>
                                                        {
                                                            statusCell.Value = "✘";
                                                            statusCell.ToolTipText = createRoute.Message;
                                                        }
                                                    )
                                                );
                                                isOk = false;
                                            }
                                            else
                                            {
                                                foreach (var feature in shapeFile.GetFeature())
                                                {
                                                    pointer++;
                                                    if (feature != null) // 如果实体要素不合理，feature 等于 null
                                                    {
                                                        var featureType = $"{feature["geometry"]["type"]}";
                                                        shapeFile.fire(
                                                            message: $"{featureType} [{pointer} / {recordCount}]",
                                                            1,
                                                            progress: 100 * pointer / recordCount
                                                        );
                                                        var featureId = $"{feature["id"]}";

                                                        try
                                                        {
                                                            XElement styleX = null;
                                                            var style = feature["style"];
                                                            if (style != null)
                                                            {
                                                                styleX = new XElement(
                                                                    "style",
                                                                    ((JObject)style).Properties()
                                                                    .Select(field =>
                                                                        new XElement(field.Name,
                                                                            field.Value.ToString()))
                                                                );
                                                            }

                                                            XElement elementdescriptionX;
                                                            if (haveFields)
                                                            {
                                                                //处理属性问题
                                                                var fieldValues = ((JObject)feature["properties"])
                                                                    .Properties()
                                                                    .Select(field => $"{field.Value["value"]}")
                                                                    .ToArray();

                                                                elementdescriptionX = new XElement("property");
                                                                for (var item = 0; item < fields.Length; item++)
                                                                    elementdescriptionX.Add(
                                                                        new XElement(
                                                                            Regex.Replace($"{fields[item]["name"]}",
                                                                                @"[:""（）\(\)]+", "_",
                                                                                RegexOptions.IgnoreCase |
                                                                                RegexOptions.Singleline |
                                                                                RegexOptions.Multiline)
                                                                            ,
                                                                            fieldValues[item]
                                                                        )
                                                                    );
                                                            }
                                                            else
                                                                elementdescriptionX = null;

                                                            //处理坐标问题 - GeoJson
                                                            var coordinates =
                                                                //(JArray)((JObject)feature["geometry"])["coordinates"];
                                                                feature["geometry"].ToString(Formatting.None);

                                                            //内点
                                                            var centroid = (JArray)feature["centroid"];
                                                            //边框 (double west, double south, double east, double north)
                                                            var featureBbox = (JArray)feature["bbox"];
                                                            var featureBoundaryX = new XElement(
                                                                "boundary",
                                                                new XAttribute(
                                                                    "centroid", $"{centroid[0]} {centroid[1]}"
                                                                ),
                                                                new XElement(
                                                                    "north", $"{featureBbox[3]}"
                                                                ),
                                                                new XElement(
                                                                    "south", $"{featureBbox[1]}"
                                                                ),
                                                                new XElement(
                                                                    "west", $"{featureBbox[0]}"
                                                                ),
                                                                new XElement(
                                                                    "east", $"{featureBbox[2]}"
                                                                )
                                                            );
                                                            var featureTimeStamp =
                                                                feature["timeStamp"]
                                                                    ?.Value<string>(); //DateTime.Now.ToString("s");

                                                            var featureX = featureType switch
                                                            {
                                                                "Point" => new XElement("member",
                                                                    new XAttribute("type", "Point"),
                                                                    new XAttribute("typeCode", "1"),
                                                                    new XAttribute("id", featureId),
                                                                    new XAttribute("timeStamp", featureTimeStamp),
                                                                    elementdescriptionX,
                                                                    new XElement(
                                                                        "geometry",
                                                                        //单点 POINT(x y z ...)
                                                                        //多点 MULTIPOINT(x y z ...,x y z ...,x y z ...) 
                                                                        //$"POINT({coordinates[0]} {coordinates[1]})"
                                                                        coordinates
                                                                        ),
                                                                    featureBoundaryX, 
                                                                    styleX),
                                                                "LineString" => new XElement("member",
                                                                    new XAttribute("type", "Line"),
                                                                    new XAttribute("typeCode", "2"),
                                                                    new XAttribute("id", featureId),
                                                                    new XAttribute("timeStamp", featureTimeStamp),
                                                                    elementdescriptionX, 
                                                                    new XElement(
                                                                        "geometry",
                                                                        //单线 LINESTRING(x y z ...,x y z ...,x y z ...)
                                                                        //多线 MULTILINESTRING((x y z ...,x y z ...,x y z ...),(x y z ...,x y z ...,x y z ...))
                                                                        //"LINESTRING(" + string.Join(",", coordinates.Select(coor => $"{coor[0]} {coor[1]}").ToArray()) + ")"
                                                                        coordinates
                                                                        ),
                                                                    featureBoundaryX,
                                                                    styleX),
                                                                "Polygon" => new XElement("member",
                                                                    new XAttribute("type", "Polygon"),
                                                                    new XAttribute("typeCode", "3"),
                                                                    new XAttribute("id", featureId),
                                                                    new XAttribute("timeStamp", featureTimeStamp),
                                                                    elementdescriptionX, 
                                                                    new XElement("geometry",
                                                                        //单面 POLYGON((x y z ...,x y z ...,x y z ...)) 
                                                                        //母子面 POLYGON((x y z ...,x y z ...,x y z ...),(x y z ...,x y z ...,x y z ...),(x y z ...,x y z ...,x y z ...),...)
                                                                        //多面已处理为单面或母子面  MULTIPOLYGON(((x y z ...,x y z ...,x y z ...),(x y z ...,x y z ...,x y z ...)), ((x y z ...,x y z ...,x y z ...)))
                                                                        //"POLYGON(" + string.Join(",", (coordinates.Cast<JArray>().Select(coordinate => "(" + string.Join(",", coordinate.Select(xy => $"{xy[0]} {xy[1]}").ToArray()) + ")")).ToList()) + ")"
                                                                        coordinates
                                                                        ), 
                                                                    featureBoundaryX, 
                                                                    styleX),
                                                                _ => null
                                                            };
                                                            if (featureX != null)
                                                            {
                                                                //写叶子

                                                                //依据枝干正向分类谱系创建叶子记录
                                                                var createLeaf = oneForest.Leaf(
                                                                    route: createRoute.Route,
                                                                    leafX: featureX,
                                                                    timestamp:
                                                                    $"{DateTime.Parse(featureX.Attribute("timeStamp").Value): yyyyMMdd,HHmmss}",
                                                                    topology: doTopology
                                                                );

                                                                var scale10 =
                                                                    (int)Math.Ceiling(10.0 * ++leafPointer /
                                                                        recordCount);

                                                                if (scale10 > oldscale10)
                                                                {
                                                                    oldscale10 = scale10;
                                                                    flag10 += scale1;
                                                                    if (flag10 < 10)
                                                                        shapeFile.fire(path,
                                                                            1,
                                                                            progress: scale10 * 10);
                                                                    else
                                                                    {
                                                                        //目的是凑满10个刻度
                                                                        var rest = 10 - (flag10 - scale1);
                                                                        if (rest > 0)
                                                                            shapeFile.fire(path,
                                                                                1,
                                                                                progress: 10 * 10);
                                                                    }
                                                                }

                                                                if (createLeaf.Success)
                                                                {
                                                                    if (!treeType.Contains(createLeaf.Type))
                                                                        treeType.Add(createLeaf.Type);

                                                                    valid++;
                                                                }
                                                                else
                                                                {
                                                                    //this.
                                                                        Invoke(
                                                                        new Action(
                                                                            () =>
                                                                            {
                                                                                statusCell.Value = "!";
                                                                                statusCell.ToolTipText =
                                                                                    createLeaf.Message;
                                                                            }
                                                                        )
                                                                    );

                                                                    isOk = false;
                                                                    //break;  //即使出现异常，也继续遍历
                                                                }
                                                            }
                                                        }
                                                        catch (Exception localError)
                                                        {
                                                            isOk = false;
                                                            //this.
                                                                Invoke(
                                                                new Action(
                                                                    () =>
                                                                    {
                                                                        statusCell.Value = "!";
                                                                        statusCell.ToolTipText = $"Feature Id {featureId} : {localError.Message}";
                                                                    }
                                                                )
                                                            );
                                                        }
                                                    }
                                                }

                                                shapeFile.fire(
                                                    $" [{valid} feature{(valid > 1 ? "s" : "")}]", 200);
                                            }

                                            oneForest.Tree(enclosure: (treeId,
                                                treeType, isOk)); //向树记录写入完整性标志以及类型数组
                                            _clusterDate?.Reset();
                                            if (isOk)
                                            {
                                                //this.
                                                    Invoke(
                                                    new Action(
                                                        () =>
                                                        {
                                                            statusCell.Value = "✔"; 
                                                            statusCell.ToolTipText = "OK";
                                                        }
                                                    )
                                                );
                                            }
                                        }
                                        else
                                        {
                                            //this.
                                                Invoke(
                                                new Action(
                                                    () =>
                                                    {
                                                        statusCell.Value = "✘";
                                                        statusCell.ToolTipText = treeResult.Message;
                                                    }
                                                )
                                            );
                                        }
                                    }
                                    else
                                    {
                                        //this.
                                            Invoke(
                                            new Action(
                                                () =>
                                                {
                                                    statusCell.Value = "?";
                                                    statusCell.ToolTipText = "Cancelled";
                                                }
                                            )
                                        );
                                    }
                                }
                                catch (Exception error)
                                {
                                    //this.
                                        Invoke(
                                        new Action(
                                            () =>
                                            {
                                                statusCell.Value = "!";
                                                statusCell.ToolTipText = error.Message;
                                            }
                                        )
                                    );
                                }
                            }
                            break;
                        case ".txt":
                        case ".csv":
                            {
                                try
                                {
                                    var freeTextFields = fileType == ".txt"
                                        ? TXT.GetFieldNames(path)
                                        : CSV.GetFieldNames(path);
                                    if (freeTextFields.Length == 0)
                                        throw new Exception("No valid fields found");

                                    string coordinateFieldName;
                                    if (freeTextFields.Any(f => f == "_position_"))
                                        coordinateFieldName = "_position_";
                                    else
                                    {
                                        var txtForm = new FreeTextField(freeTextFields);
                                        txtForm.ShowDialog();
                                        coordinateFieldName = txtForm.Ok ? txtForm.CoordinateFieldName : null;
                                    }

                                    if (coordinateFieldName != null)
                                    {
                                        string treePathString = null;
                                        XElement description = null;
                                        var canDo = true;
                                        if (!_noPromptLayersBuilder)
                                        {
                                            var getTreeLayers = new LayersBuilder(new FileInfo(path).FullName);
                                            getTreeLayers.ShowDialog();
                                            if (getTreeLayers.Ok)
                                            {
                                                treePathString = getTreeLayers.TreePathString;
                                                description = getTreeLayers.Description;
                                                _noPromptLayersBuilder = getTreeLayers.DonotPrompt;
                                            }
                                            else
                                                canDo = false;
                                        }
                                        else
                                        {
                                            treePathString = ConsoleIO.FilePathToXPath(new FileInfo(path).FullName);
                                        }
                                        if (canDo)
                                        {
                                            //多态性：将派生类对象赋予基类对象
                                            FreeText.FreeText freeText = fileType == ".txt"
                                                ? new TXT(CoordinateFieldName: coordinateFieldName)
                                                : new CSV(CoordinateFieldName: coordinateFieldName);
                                            freeText.onGeositeEvent +=
                                                delegate (object _, GeositeEventArgs thisEvent)
                                                {
                                                    vectorBackgroundWorker.ReportProgress(
                                                        thisEvent.progress ?? -1,
                                                        thisEvent.message ?? string.Empty);
                                                };
                                            freeText.Open(path);
                                            {
                                                if (freeText.RecordCount == 0)
                                                    return "No features found";
                                                freeText.fire("Preprocessing ...", 0);
                                                var getFileInfo = freeText.GetCapabilities();
                                                var getFileType = $"{getFileInfo["fileType"]}";

                                                //处理属性问题 
                                                var fields = freeText.GetField();
                                                var haveFields = fields.Length > 0;

                                                var featureCollectionX = new XElement(
                                                    "FeatureCollection",
                                                    new XAttribute("type", getFileType),
                                                    new XAttribute("timeStamp", $"{getFileInfo["timeStamp"]}"),
                                                    new XElement("name", theme)
                                                    );
                                                if (description != null)
                                                {
                                                    featureCollectionX.Add
                                                    (
                                                        new XElement(
                                                            "property",
                                                            description
                                                                .Elements()
                                                                .Select(x => new XElement($"{x.Name}", x.Value))
                                                        )
                                                    );
                                                }
                                                var bbox = (JArray)getFileInfo["bbox"]; // $"[{west}, {south}, {east}, {north}]"
                                                featureCollectionX.Add(
                                                    new XElement(
                                                        "boundary",
                                                        new XElement("north", $"{bbox[3]}"),
                                                        new XElement("south", $"{bbox[1]}"),
                                                        new XElement("west", $"{bbox[0]}"),
                                                        new XElement("east", $"{bbox[2]}")
                                                        )
                                                    );
                                                var pointer = 0;
                                                var valid = 0;
                                                var treePath = treePathString;
                                                if (string.IsNullOrWhiteSpace(treePath))
                                                    treePath = "Untitled";
                                                var treeNameArray = Regex.Split(treePath, @"[\/\\\|]+");
                                                var treeTimeStamp =
                                                    $"{forest},{sequence},{DateTime.Parse($"{getFileInfo["timeStamp"]}"): yyyyMMdd,HHmmss}";
                                                var treeResult =
                                                    oneForest.Tree(
                                                        treeTimeStamp,
                                                        featureCollectionX,
                                                        path,
                                                        status
                                                    );
                                                if (treeResult.Success)
                                                {
                                                    var treeId = treeResult.Id;
                                                    //此时，文档树所容纳的叶子类型type默认值：0
                                                    var treeType = new List<int>();
                                                    var isOk = true;
                                                    var recordCount = freeText.RecordCount;

                                                    // 为提升进度视觉体验，特将进度值限定在0--10之间
                                                    var leafPointer = 0;
                                                    var oldscale10 = -1;
                                                    var flagMany = 10.0 / recordCount;
                                                    var scale1 = (int)Math.Ceiling(flagMany);
                                                    var flag10 = 0;

                                                    //提供追加元数据的机会
                                                    XElement themeMetadataX = null;
                                                    if (!_noPromptMetaData)
                                                    {
                                                        var metaData = new MetaData();
                                                        metaData.ShowDialog();
                                                        if (metaData.Ok)
                                                        {
                                                            themeMetadataX = metaData.MetaDataX;
                                                            _noPromptMetaData = metaData.DonotPrompt;
                                                        }
                                                    }

                                                    //最末层
                                                    XElement layerX = null;
                                                    for (var index = treeNameArray.Length - 1; index >= 0; index--)
                                                    {
                                                        layerX = new XElement(
                                                            "layer",
                                                            new XElement("name", treeNameArray[index].Trim()),
                                                            //将元数据添加到最末层
                                                            index == treeNameArray.Length - 1 ? themeMetadataX : null,
                                                            index == treeNameArray.Length - 1 ? new XElement("member") : null,
                                                            layerX
                                                        );
                                                    }
                                                    featureCollectionX.Add(layerX);

                                                    //写叶子
                                                    //由叶子对象反向回溯并创建枝干分类谱系，返回枝干谱系id数组
                                                    var createRoute = oneForest.Branch(
                                                        forest: forest,
                                                        sequence: sequence,
                                                        tree: treeId,
                                                        leafX: featureCollectionX.Descendants("member").First(),
                                                        leafRootX: featureCollectionX
                                                    );
                                                    if (!createRoute.Success)
                                                    {
                                                        //this.
                                                            Invoke(
                                                            new Action(
                                                                () =>
                                                                {
                                                                    statusCell.Value = "✘";
                                                                    statusCell.ToolTipText = createRoute.Message;
                                                                }
                                                            )
                                                        );

                                                        isOk = false;
                                                    }
                                                    else
                                                    {
                                                        foreach (var feature in freeText.GetFeature())
                                                        {
                                                            pointer++;
                                                            if (feature != null)
                                                            {
                                                                var featureType = $"{feature["geometry"]["type"]}";
                                                                freeText.fire(
                                                                    message: $"{featureType} [{pointer} / {recordCount}]",
                                                                    1,
                                                                    progress: 100 * pointer / recordCount
                                                                );
                                                                var featureId = $"{feature["id"]}";
                                                                try
                                                                {
                                                                    XElement elementdescriptionX;
                                                                    if (haveFields)
                                                                    {
                                                                        //处理属性问题
                                                                        var fieldValues = ((JObject)feature["properties"])
                                                                            .Properties()
                                                                            .Select(field => $"{field.Value["value"]}")
                                                                            .ToArray();

                                                                        elementdescriptionX = new XElement("property");
                                                                        for (var item = 0; item < fields.Length; item++)
                                                                            elementdescriptionX.Add(
                                                                                new XElement(
                                                                                    Regex.Replace($"{fields[item]["name"]}", @"[:""（）\(\)]+", "_",
                                                                                        RegexOptions.IgnoreCase | RegexOptions.Singleline |
                                                                                        RegexOptions.Multiline)
                                                                                    ,
                                                                                    fieldValues[item]
                                                                                )
                                                                            );
                                                                    }
                                                                    else
                                                                        elementdescriptionX = null;

                                                                    //处理坐标问题 - GeoJson
                                                                    var coordinates =
                                                                        //(JArray)((JObject)feature["geometry"])["coordinates"];
                                                                        feature["geometry"].ToString(Formatting.None);

                                                                    //内点
                                                                    var centroid = (JArray)feature["centroid"];
                                                                    //边框 (double west, double south, double east, double north)
                                                                    var featureBbox = (JArray)feature["bbox"];
                                                                    var featureBoundaryX = new XElement(
                                                                        "boundary",
                                                                        new XAttribute(
                                                                            "centroid", $"{centroid[0]} {centroid[1]}"
                                                                        ),
                                                                        new XElement(
                                                                            "north", $"{featureBbox[3]}"
                                                                        ),
                                                                        new XElement(
                                                                            "south", $"{featureBbox[1]}"
                                                                        ),
                                                                        new XElement(
                                                                            "west", $"{featureBbox[0]}"
                                                                        ),
                                                                        new XElement(
                                                                            "east", $"{featureBbox[2]}"
                                                                        )
                                                                    );
                                                                    var featureTimeStamp = feature["timeStamp"]?.Value<string>();  //DateTime.Now.ToString("s");

                                                                    XElement featureX = null;
                                                                    switch (featureType)
                                                                    {
                                                                        case "Point":
                                                                            //var subType = $"{feature["subType"]}"; //subType == "0" || subType == "5" ? style["text"] : ""
                                                                            featureX = new XElement
                                                                            (
                                                                                "member",
                                                                                new XAttribute("type", "Point"),
                                                                                new XAttribute("typeCode", "1"),
                                                                                new XAttribute("id", featureId),
                                                                                new XAttribute("timeStamp", featureTimeStamp),
                                                                                elementdescriptionX,
                                                                                new XElement(
                                                                                    "geometry",
                                                                                    //$"POINT({coordinates[0]} {coordinates[1]})"
                                                                                    coordinates
                                                                                ),
                                                                                featureBoundaryX
                                                                            //,
                                                                            //new XElement(
                                                                            //    "style",
                                                                            //    style.Properties()
                                                                            //        .Select(field =>
                                                                            //            new XElement(field.Name,
                                                                            //                field.Value.ToString()))
                                                                            //)
                                                                            );

                                                                            break;
                                                                        case "LineString":
                                                                            featureX = new XElement
                                                                            (
                                                                                "member",
                                                                                new XAttribute("type", "Line"),
                                                                                new XAttribute("typeCode", "2"),
                                                                                new XAttribute("id", featureId),
                                                                                new XAttribute("timeStamp",
                                                                                    featureTimeStamp),
                                                                                elementdescriptionX,
                                                                                new XElement(
                                                                                    "geometry",
                                                                                    //"LINESTRING(" +
                                                                                    //string.Join(
                                                                                    //    ",",
                                                                                    //    coordinates.Select(coor =>
                                                                                    //        $"{coor[0]} {coor[1]}").ToArray()
                                                                                    //) +
                                                                                    //")"
                                                                                    coordinates
                                                                                ),
                                                                                featureBoundaryX
                                                                            //,
                                                                            //new XElement(
                                                                            //    "style",
                                                                            //    style.Properties()
                                                                            //        .Select(field =>
                                                                            //            new XElement(field.Name,
                                                                            //                field.Value.ToString()))
                                                                            //)
                                                                            );

                                                                            break;
                                                                        case "Polygon":
                                                                            featureX = new XElement
                                                                            (
                                                                                "member",
                                                                                new XAttribute("type", "Polygon"),
                                                                                new XAttribute("typeCode", "3"),
                                                                                new XAttribute("id", featureId),
                                                                                new XAttribute("timeStamp",
                                                                                    featureTimeStamp),
                                                                                elementdescriptionX,
                                                                                new XElement(
                                                                                    "geometry",
                                                                                    //"POLYGON(" +
                                                                                    //string.Join
                                                                                    //(
                                                                                    //    ",",
                                                                                    //    coordinates
                                                                                    //        .Cast<JArray>()
                                                                                    //        .Select(coordinate =>
                                                                                    //            $"({string.Join(",", (coordinate.Select(xy => $"{xy[0]} {xy[1]}")).ToArray())})")
                                                                                    //    .ToList()
                                                                                    //) +
                                                                                    //")"
                                                                                    coordinates
                                                                                ),
                                                                                featureBoundaryX
                                                                            //,
                                                                            //new XElement(
                                                                            //    "style",
                                                                            //    style.Properties()
                                                                            //        .Select(field => new XElement(field.Name, field.Value.ToString()))
                                                                            //)
                                                                            );

                                                                            break;
                                                                    }
                                                                    if (featureX != null)
                                                                    {
                                                                        //依据枝干正向分类谱系创建叶子记录
                                                                        var createLeaf = oneForest.Leaf(
                                                                            route: createRoute.Route,
                                                                            leafX: featureX,
                                                                            timestamp:
                                                                            $"{DateTime.Parse(featureX.Attribute("timeStamp").Value): yyyyMMdd,HHmmss}",
                                                                            topology: doTopology
                                                                        );

                                                                        var scale10 =
                                                                            (int)Math.Ceiling(10.0 * ++leafPointer /
                                                                                recordCount);

                                                                        if (scale10 > oldscale10)
                                                                        {
                                                                            oldscale10 = scale10;
                                                                            flag10 += scale1;
                                                                            if (flag10 < 10)
                                                                                freeText.fire(path,
                                                                                    1,
                                                                                    progress: scale10 * 10);
                                                                            else
                                                                            {
                                                                                //目的是凑满10个刻度
                                                                                var rest = 10 - (flag10 - scale1);
                                                                                if (rest > 0)
                                                                                    freeText.fire(path,
                                                                                        1,
                                                                                        progress: 10 * 10);
                                                                            }
                                                                        }

                                                                        if (createLeaf.Success)
                                                                        {
                                                                            if (!treeType.Contains(createLeaf.Type))
                                                                                treeType.Add(createLeaf.Type);

                                                                            valid++;
                                                                        }
                                                                        else
                                                                        {
                                                                            //this.
                                                                                Invoke(
                                                                                new Action(
                                                                                    () =>
                                                                                    {
                                                                                        statusCell.Value = "!";
                                                                                        statusCell.ToolTipText =
                                                                                            createLeaf.Message;
                                                                                    }
                                                                                )
                                                                            );

                                                                            isOk = false;
                                                                            //break; //即使出现异常，也继续遍历
                                                                        }
                                                                    }
                                                                }
                                                                catch (Exception localError)
                                                                {
                                                                    isOk = false;
                                                                    //this.
                                                                        Invoke(
                                                                        new Action(
                                                                            () =>
                                                                            {
                                                                                statusCell.Value = "!";
                                                                                statusCell.ToolTipText = localError.Message;
                                                                            }
                                                                        )
                                                                    );
                                                                }
                                                            }
                                                        }

                                                        freeText.fire(
                                                            $" [{valid} feature{(valid > 1 ? "s" : "")}]", 200);
                                                    }

                                                    oneForest.Tree(enclosure: (treeId, treeType, isOk)); //向树记录写入完整性标志以及类型数组
                                                    _clusterDate?.Reset();
                                                    if (isOk)
                                                    {
                                                        //this.
                                                            Invoke(
                                                            new Action(
                                                                () =>
                                                                {
                                                                    statusCell.Value = "✔";
                                                                    statusCell.ToolTipText = "OK";
                                                                }
                                                            )
                                                        );
                                                    }
                                                }
                                                else
                                                {
                                                    //this.
                                                        Invoke(
                                                        new Action(
                                                            () =>
                                                            {
                                                                statusCell.Value = "✘";
                                                                statusCell.ToolTipText = treeResult.Message;
                                                            }
                                                        )
                                                    );
                                                }
                                            }
                                        }
                                        else
                                        {
                                            //this.
                                                Invoke(
                                                new Action(
                                                    () =>
                                                    {
                                                        statusCell.Value = "?";
                                                        statusCell.ToolTipText = "Cancelled";
                                                    }
                                                )
                                            );
                                        }
                                    }
                                }
                                catch (Exception error)
                                {
                                    //this.
                                        Invoke(
                                        new Action(
                                            () =>
                                            {
                                                statusCell.Value = "!";
                                                statusCell.ToolTipText = error.Message;
                                            }
                                        )
                                    );
                                }
                            }
                            break;
                        case ".xml":
                        {
                            try
                            {
                                XElement description = null;
                                var canDo = true;
                                if (!_noPromptLayersBuilder)
                                {
                                    var getTreeLayers = new LayersBuilder(new FileInfo(path).FullName);
                                    getTreeLayers.ShowDialog();
                                    if (getTreeLayers.Ok)
                                    {
                                        description = getTreeLayers.Description;
                                        _noPromptLayersBuilder = getTreeLayers.DonotPrompt;
                                    }
                                    else
                                        canDo = false;
                                }

                                if (canDo)
                                {
                                    using var xml = new GeositeXml.GeositeXml();
                                    xml.onGeositeEvent += delegate(object _, GeositeEventArgs thisEvent)
                                    {
                                        vectorBackgroundWorker.ReportProgress(thisEvent.progress ?? -1,
                                            thisEvent.message ?? string.Empty);
                                    };

                                    XElement featureCollectionX = null;
                                    var theTree = xml.GetTree(path);
                                    var geositeXml = theTree as XElement[] ?? theTree.ToArray();
                                    if (geositeXml.DescendantsAndSelf("member").Any())
                                    {
                                        var nameAndOthers = geositeXml
                                            .Where(x => x.Name != "layer")
                                            .Select(x => x)
                                            .ToList();
                                        var layers = geositeXml.Where(x => x.Name == "layer").Select(x => x);
                                        var name = nameAndOthers.Where(x => x.Name == "name").Select(x => x);
                                        if (nameAndOthers != null && name.Any() && layers.Any())
                                        {
                                            featureCollectionX = new XElement(
                                                "FeatureCollection",
                                                nameAndOthers,
                                                layers
                                            );
                                        }
                                    }
                                    else
                                    {
                                        featureCollectionX =
                                            xml.GeositeXmlToGeositeXml(geositeXml, null, description).Root;
                                    }

                                    if (featureCollectionX != null && geositeXml.DescendantsAndSelf("member").Any())
                                    {
                                        featureCollectionX.Element("name").Value = theme;
                                        var treeTimeStamp =
                                            $"{forest},{sequence},{DateTime.Parse(featureCollectionX?.Attribute("timeStamp")?.Value ?? DateTime.Now.ToString("s")): yyyyMMdd,HHmmss}";
                                        
                                        var treeResult =
                                            oneForest.Tree(
                                                treeTimeStamp,
                                                featureCollectionX,
                                                path,
                                                status
                                            );

                                        if (treeResult.Success)
                                        {
                                            var treeId = treeResult.Id;

                                            //此时，文档树所容纳的叶子类型type默认值：0
                                            var treeType = new List<int>();

                                            var isOk = true;

                                            //第3层：遍历识别不同的实体要素标签，以便提升兼容性
                                            foreach
                                            (
                                                var leafArray in new[]
                                                    {
                                                        "member", "Member", "MEMBER"
                                                    }
                                                    .Select
                                                    (
                                                        leafName => featureCollectionX
                                                            .DescendantsAndSelf(leafName).ToList()
                                                    )
                                                    .Where
                                                    (
                                                        leafX => leafX.Any()
                                                    )
                                            )
                                            {
                                                //第4层：遍历全部叶子，以回溯方式创建叶子的归属枝干、创建叶子节点

                                                //本棵树的叶子总数量
                                                var leafCount = leafArray.Count;
                                                if (leafCount > 0)
                                                {
                                                    // 为提升进度视觉体验，特将进度值限定在0--10之间
                                                    var leafPointer = 0;
                                                    var valid = 0;
                                                    var oldscale10 = -1;
                                                    var flagMany = 10.0 / leafCount;
                                                    var scale1 = (int) Math.Ceiling(flagMany);
                                                    var flag10 = 0;
                                                    foreach (var leafX in leafArray)
                                                    {
                                                        //由叶子对象反向回溯并创建枝干分类谱系，返回枝干谱系id数组
                                                        var createRoute = oneForest.Branch(
                                                            forest: forest,
                                                            sequence: sequence,
                                                            tree: treeId,
                                                            leafX: leafX,
                                                            leafRootX: featureCollectionX
                                                        );

                                                        if (!createRoute.Success)
                                                        {
                                                            //this.
                                                            Invoke(
                                                                new Action(
                                                                    () =>
                                                                    {
                                                                        statusCell.Value = "✘";
                                                                        statusCell.ToolTipText = createRoute.Message;
                                                                    }
                                                                )
                                                            );

                                                            isOk = false;
                                                            //break; //即使出现异常，也继续遍历？
                                                        }
                                                        else
                                                        {
                                                            var scale10 =
                                                                (int) Math.Ceiling(10.0 * ++leafPointer / leafCount);

                                                            if (scale10 > oldscale10)
                                                            {
                                                                oldscale10 = scale10;
                                                                flag10 += scale1;
                                                                if (flag10 < 10)
                                                                    xml.fire(path,
                                                                        1,
                                                                        progress: scale10 * 10);
                                                                else
                                                                {
                                                                    //目的是凑满10个刻度
                                                                    var rest = 10 - (flag10 - scale1);
                                                                    if (rest > 0)
                                                                        xml.fire(path,
                                                                            1,
                                                                            progress: 10 * 10);
                                                                }
                                                            }

                                                            //依据枝干正向分类谱系创建叶子记录
                                                            var createLeaf = oneForest.Leaf(
                                                                route: createRoute.Route,
                                                                leafX: leafX,
                                                                timestamp:
                                                                $"{DateTime.Parse(leafX?.Attribute("timeStamp")?.Value ?? DateTime.Now.ToString("s")): yyyyMMdd,HHmmss}",
                                                                topology: doTopology
                                                            );
                                                            
                                                            if (createLeaf.Success)
                                                            {
                                                                valid++;
                                                                if (!treeType.Contains(createLeaf.Type))
                                                                    treeType.Add(createLeaf.Type);
                                                            }
                                                            else
                                                            {
                                                                //this.
                                                                Invoke(
                                                                    new Action(
                                                                        () =>
                                                                        {
                                                                            statusCell.Value = "!";
                                                                            statusCell.ToolTipText = createLeaf.Message;
                                                                        }
                                                                    )
                                                                );

                                                                isOk = false;
                                                                //break; //即使出现异常，也继续遍历？
                                                            }
                                                        }
                                                    }

                                                    xml.fire(
                                                        $" [{valid} feature{(valid > 1 ? "s" : "")}]", 200);
                                                }

                                                //只要发现【"member", "Member", "MEMBER"】任何一个，就中止后续遍历
                                                break;
                                            }

                                            oneForest.Tree(enclosure: (treeId,
                                                treeType, isOk)); //向树记录写入完整性标志以及类型数组
                                            _clusterDate?.Reset();
                                            if (isOk)
                                            {
                                                //this.
                                                Invoke(
                                                    new Action(
                                                        () =>
                                                        {
                                                            statusCell.Value = "✔";
                                                            statusCell.ToolTipText = "OK";
                                                        }
                                                    )
                                                );
                                            }
                                        }
                                        else
                                        {
                                            //this.
                                            Invoke(
                                                new Action(
                                                    () =>
                                                    {
                                                        statusCell.Value = "✘";
                                                        statusCell.ToolTipText = treeResult.Message;
                                                    }
                                                )
                                            );
                                        }
                                    }
                                    else
                                    {
                                        throw new Exception("Nothing");
                                    }
                                }
                                else
                                {
                                    //this.
                                    Invoke(
                                        new Action(
                                            () =>
                                            {
                                                statusCell.Value = "?";
                                                statusCell.ToolTipText = "Cancelled";
                                            }
                                        )
                                    );
                                }
                            }
                            catch (Exception error)
                            {
                                //this.
                                Invoke(
                                    new Action(
                                        () =>
                                        {
                                            statusCell.Value = "!";
                                            statusCell.ToolTipText = error.Message;
                                        }
                                    )
                                );
                            }
                        }
                            break;
                        case ".kml":
                            {
                                try
                                {
                                    XElement description = null;
                                    var canDo = true;
                                    if (!_noPromptLayersBuilder)
                                    {
                                        var getTreeLayers = new LayersBuilder(new FileInfo(path).FullName);
                                        getTreeLayers.ShowDialog();
                                        if (getTreeLayers.Ok)
                                        {
                                            description = getTreeLayers.Description;
                                            _noPromptLayersBuilder = getTreeLayers.DonotPrompt;
                                        }
                                        else
                                            canDo = false;
                                    }

                                    if (canDo)
                                    {
                                        using var kml = new GeositeXml.GeositeXml();
                                        kml.onGeositeEvent += delegate (object _, GeositeEventArgs thisEvent)
                                        {
                                            vectorBackgroundWorker.ReportProgress(thisEvent.progress ?? -1,
                                                thisEvent.message ?? string.Empty);
                                        };
                                        var featureCollectionX = kml.KmlToGeositeXml(kml.GetTree(path), null, description).Root;
                                        featureCollectionX.Element("name").Value = theme;
                                        var treeTimeStamp =
                                            $"{forest},{sequence},{DateTime.Parse(featureCollectionX?.Attribute("timeStamp")?.Value ?? DateTime.Now.ToString("s")): yyyyMMdd,HHmmss}";
                                        
                                        var treeResult =
                                            oneForest.Tree(
                                                treeTimeStamp,
                                                featureCollectionX,
                                                path,
                                                status
                                            );

                                        if (treeResult.Success)
                                        {
                                            var treeId = treeResult.Id;

                                            //此时，文档树所容纳的叶子类型type默认值：0
                                            var treeType = new List<int>();

                                            var isOk = true;

                                            //第3层：遍历识别不同的实体要素标签，以便提升兼容性
                                            foreach
                                            (
                                                var leafArray in new[]
                                                    {
                                                        "member", "Member", "MEMBER"
                                                    }
                                                    .Select
                                                    (
                                                        leafName => featureCollectionX
                                                            .DescendantsAndSelf(leafName).ToList()
                                                    )
                                                    .Where
                                                    (
                                                        leafX => leafX.Any()
                                                    )
                                            )
                                            {
                                                //第4层：遍历全部叶子，以回溯方式创建叶子的归属枝干、创建叶子节点

                                                //本棵树的叶子总数量
                                                var leafCount = leafArray.Count;
                                                if (leafCount > 0)
                                                {
                                                    // 为提升进度视觉体验，特将进度值限定在0--10之间
                                                    var leafPointer = 0;
                                                    var valid = 0;
                                                    var oldscale10 = -1;
                                                    var flagMany = 10.0 / leafCount;
                                                    var scale1 = (int)Math.Ceiling(flagMany);
                                                    var flag10 = 0;
                                                    foreach (var leafX in leafArray)
                                                    {
                                                        //由叶子对象反向回溯并创建枝干分类谱系，返回枝干谱系id数组
                                                        var createRoute = oneForest.Branch(
                                                            forest: forest,
                                                            sequence: sequence,
                                                            tree: treeId,
                                                            leafX: leafX,
                                                            leafRootX: featureCollectionX
                                                        );

                                                        if (!createRoute.Success)
                                                        {
                                                            //this.
                                                                Invoke(
                                                                new Action(
                                                                    () =>
                                                                    {
                                                                        statusCell.Value = "✘";
                                                                        statusCell.ToolTipText = createRoute.Message;
                                                                    }
                                                                )
                                                            );

                                                            isOk = false;
                                                            //break;
                                                        }
                                                        else
                                                        {
                                                            //依据枝干正向分类谱系创建叶子记录
                                                            var createLeaf = oneForest.Leaf(
                                                                route: createRoute.Route,
                                                                leafX: leafX,
                                                                timestamp:
                                                                $"{DateTime.Parse(leafX?.Attribute("timeStamp")?.Value ?? DateTime.Now.ToString("s")): yyyyMMdd,HHmmss}",
                                                                topology: doTopology
                                                            );
                                                            
                                                            var scale10 = (int)Math.Ceiling(10.0 * ++leafPointer / leafCount);

                                                            if (scale10 > oldscale10)
                                                            {
                                                                oldscale10 = scale10;
                                                                flag10 += scale1;
                                                                if (flag10 < 10)
                                                                    kml.fire(path,
                                                                        1,
                                                                        progress: scale10 * 10);
                                                                else
                                                                {
                                                                    //目的是凑满10个刻度
                                                                    var rest = 10 - (flag10 - scale1);
                                                                    if (rest > 0)
                                                                        kml.fire(path,
                                                                            1,
                                                                            progress: 10 * 10);
                                                                }
                                                            }

                                                            if (createLeaf.Success)
                                                            {
                                                                valid++;
                                                                if (!treeType.Contains(createLeaf.Type))
                                                                    treeType.Add(createLeaf.Type);
                                                            }
                                                            else
                                                            {
                                                                //this.
                                                                    Invoke(
                                                                    new Action(
                                                                        () =>
                                                                        {
                                                                            statusCell.Value = "!";
                                                                            statusCell.ToolTipText = createLeaf.Message;
                                                                        }
                                                                    )
                                                                );

                                                                isOk = false;
                                                                //break;
                                                            }
                                                        }
                                                    }

                                                    kml.fire(
                                                        $" [{valid} feature{(valid > 1 ? "s" : "")}]", 200);
                                                }

                                                //只要发现任何一个，就中止后续遍历
                                                break;
                                            }

                                            oneForest.Tree(enclosure: (treeId,
                                                treeType, isOk)); //向树记录写入完整性标志以及类型数组
                                            _clusterDate?.Reset();
                                            if (isOk)
                                            {
                                                //this.
                                                    Invoke(
                                                    new Action(
                                                        () =>
                                                        {
                                                            statusCell.Value = "✔";
                                                            statusCell.ToolTipText = "OK";
                                                        }
                                                    )
                                                );
                                            }
                                        }
                                        else
                                        {
                                            //this.
                                                Invoke(
                                                new Action(
                                                    () =>
                                                    {
                                                        statusCell.Value = "✘";
                                                        statusCell.ToolTipText = treeResult.Message;
                                                    }
                                                )
                                            );
                                        }
                                    }
                                    else
                                    {
                                        //this.
                                            Invoke(
                                            new Action(
                                                () =>
                                                {
                                                    statusCell.Value = "?";
                                                    statusCell.ToolTipText = "Cancelled";
                                                }
                                            )
                                        );
                                    }
                                }
                                catch (Exception error)
                                {
                                    //this.
                                        Invoke(
                                        new Action(
                                            () =>
                                            {
                                                statusCell.Value = "!";
                                                statusCell.ToolTipText = error.Message;
                                            }
                                        )
                                    );
                                }
                            }
                            break;
                        case ".geojson":
                            {
                                try
                                {
                                    string treePathString = null;
                                    XElement description = null;
                                    var canDo = true;
                                    if (!_noPromptLayersBuilder)
                                    {
                                        var getTreeLayers = new LayersBuilder(new FileInfo(path).FullName);
                                        getTreeLayers.ShowDialog();
                                        if (getTreeLayers.Ok)
                                        {
                                            treePathString = getTreeLayers.TreePathString;
                                            description = getTreeLayers.Description;
                                            _noPromptLayersBuilder = getTreeLayers.DonotPrompt;
                                        }
                                        else
                                            canDo = false;
                                    }
                                    else
                                        treePathString = ConsoleIO.FilePathToXPath(new FileInfo(path).FullName);

                                    if (canDo)
                                    {
                                        using var geoJsonObject = new GeositeXml.GeositeXml();
                                        geoJsonObject.onGeositeEvent += delegate (object _, GeositeEventArgs thisEvent)
                                        {
                                            vectorBackgroundWorker.ReportProgress(thisEvent.progress ?? -1,
                                                thisEvent.message ?? string.Empty);
                                        };
                                        var getGeositeXml = new StringBuilder();
                                        geoJsonObject
                                            .GeoJsonToGeositeXml(
                                                path,
                                                getGeositeXml,
                                                treePathString,
                                                description
                                            );

                                        if (getGeositeXml.Length > 0)
                                        {
                                            var featureCollectionX = XElement.Parse(getGeositeXml.ToString());
                                            featureCollectionX.Element("name").Value = theme;

                                            var treeTimeStamp =
                                                $"{forest},{sequence},{DateTime.Parse(featureCollectionX?.Attribute("timeStamp")?.Value ?? DateTime.Now.ToString("s")): yyyyMMdd,HHmmss}";
                                            
                                            var treeResult =
                                                oneForest.Tree(
                                                    treeTimeStamp,
                                                    featureCollectionX,
                                                    path,
                                                    status
                                                );

                                            if (treeResult.Success)
                                            {
                                                var treeId = treeResult.Id;

                                                //此时，文档树所容纳的叶子类型type默认值：0
                                                var treeType = new List<int>();

                                                var isOk = true;

                                                //第3层：遍历识别不同的实体要素标签，以便提升兼容性
                                                foreach
                                                (
                                                    var leafArray in new[]
                                                        {
                                                            "member", "Member", "MEMBER"
                                                        }
                                                        .Select
                                                        (
                                                            leafName => featureCollectionX
                                                                .DescendantsAndSelf(leafName).ToList()
                                                        )
                                                        .Where
                                                        (
                                                            leafX => leafX.Any()
                                                        )
                                                )
                                                {
                                                    //第4层：遍历全部叶子，以回溯方式创建叶子的归属枝干、创建叶子节点

                                                    //本棵树的叶子总数量
                                                    var leafCount = leafArray.Count;
                                                    if (leafCount > 0)
                                                    {
                                                        // 为提升进度视觉体验，特将进度值限定在0--10之间
                                                        var leafPointer = 0;
                                                        var valid = 0;
                                                        var oldscale10 = -1;
                                                        var flagMany = 10.0 / leafCount;
                                                        var scale1 = (int)Math.Ceiling(flagMany);
                                                        var flag10 = 0;
                                                        foreach (var leafX in leafArray)
                                                        {
                                                            //由叶子对象反向回溯并创建枝干分类谱系，返回枝干谱系id数组
                                                            var createRoute = oneForest.Branch(
                                                                forest: forest,
                                                                sequence: sequence,
                                                                tree: treeId,
                                                                leafX: leafX,
                                                                leafRootX: featureCollectionX
                                                            );

                                                            if (!createRoute.Success)
                                                            {
                                                                //this.
                                                                    Invoke(
                                                                    new Action(
                                                                        () =>
                                                                        {
                                                                            statusCell.Value = "✘";
                                                                            statusCell.ToolTipText = createRoute.Message;
                                                                        }
                                                                    )
                                                                );

                                                                isOk = false;
                                                                //break;
                                                            }
                                                            else
                                                            {
                                                                //依据枝干正向分类谱系创建叶子记录
                                                                var createLeaf = oneForest.Leaf(
                                                                    route: createRoute.Route,
                                                                    leafX: leafX,
                                                                    timestamp:
                                                                    $"{DateTime.Parse(leafX?.Attribute("timeStamp")?.Value ?? DateTime.Now.ToString("s")): yyyyMMdd,HHmmss}",
                                                                    topology: doTopology
                                                                );
                                                                
                                                                var scale10 =
                                                                    (int)Math.Ceiling(10.0 * ++leafPointer /
                                                                        leafCount);

                                                                if (scale10 > oldscale10)
                                                                {
                                                                    oldscale10 = scale10;
                                                                    flag10 += scale1;
                                                                    if (flag10 < 10)
                                                                        geoJsonObject.fire(path,
                                                                            1,
                                                                            progress: scale10 * 10);
                                                                    else
                                                                    {
                                                                        //目的是凑满10个刻度
                                                                        var rest = 10 - (flag10 - scale1);
                                                                        if (rest > 0)
                                                                            geoJsonObject.fire(path,
                                                                                1,
                                                                                progress: 10 * 10);
                                                                    }
                                                                }

                                                                if (createLeaf.Success)
                                                                {
                                                                    valid++;
                                                                    if (!treeType.Contains(createLeaf.Type))
                                                                        treeType.Add(createLeaf.Type);
                                                                }
                                                                else
                                                                {
                                                                    //this.
                                                                        Invoke(
                                                                        new Action(
                                                                            () =>
                                                                            {
                                                                                statusCell.Value = "!";
                                                                                statusCell.ToolTipText = createLeaf.Message;
                                                                            }
                                                                        )
                                                                    );

                                                                    isOk = false;
                                                                    //break;
                                                                }
                                                            }
                                                        }

                                                        geoJsonObject.fire(
                                                            $" [{valid} feature{(valid > 1 ? "s" : "")}]", 200);
                                                    }

                                                    //只要发现任何一个，就中止后续遍历
                                                    break;
                                                }

                                                oneForest.Tree(enclosure: (treeId,
                                                    treeType, isOk)); //向树记录写入完整性标志以及类型数组
                                                _clusterDate?.Reset();
                                                if (isOk)
                                                {
                                                    //this.
                                                        Invoke(
                                                        new Action(
                                                            () =>
                                                            {
                                                                statusCell.Value = "✔";
                                                                statusCell.ToolTipText = "OK";
                                                            }
                                                        )
                                                    );
                                                }
                                            }
                                            else
                                            {
                                                //this.
                                                    Invoke(
                                                    new Action(
                                                        () =>
                                                        {
                                                            statusCell.Value = "✘";
                                                            statusCell.ToolTipText = treeResult.Message;
                                                        }
                                                    )
                                                );
                                            }
                                        }
                                        else
                                        {
                                            //this.
                                                Invoke(
                                                new Action(
                                                    () =>
                                                    {
                                                        statusCell.Value = "✘";
                                                        statusCell.ToolTipText = "Fail";
                                                    }
                                                )
                                            );
                                        }
                                    }
                                    else
                                    {
                                        //this.
                                            Invoke(
                                            new Action(
                                                () =>
                                                {
                                                    statusCell.Value = "?";
                                                    statusCell.ToolTipText = "Cancelled";
                                                }
                                            )
                                        );
                                    }
                                }
                                catch (Exception error)
                                {
                                    //this.
                                        Invoke(
                                        new Action(
                                            () =>
                                            {
                                                statusCell.Value = "!";
                                                statusCell.ToolTipText = error.Message;
                                            }
                                        )
                                    );
                                }
                            }
                            break;
                        default:
                            //this.
                                Invoke(
                                new Action(
                                    () =>
                                    {
                                        statusCell.Value = "?";
                                        statusCell.ToolTipText = "Unknown";
                                    }
                                )
                            );
                            break;
                    }
                }
            }

            ////更新 DataGrid 控件 - ClusterDate //由于每个文档推送时已经刷新了一次，这里无需额外执行了
            //this.
            //Invoke(
            //new Action(
            //    () =>
            //    {
            //        _clusterDate?.Reset();
            //    }
            //)
            //);

            return statusInfo; //此结果信息将出现在状态行
        }

        private void VectorWorkProgress(object sender, ProgressChangedEventArgs e)
        {
            //e.code 状态码（0/null=预处理阶段；1=正在处理阶段；200=收尾阶段；400=异常信息）
            //e.ProgressPercentage 进度值（介于0~100之间，仅当code=1时有效）
            var userState = (string)e.UserState;
            var progressPercentage = e.ProgressPercentage;
            var pv = statusProgress.Value = progressPercentage is >= 0 and <= 100 ? progressPercentage : 0;
            statusText.Text = userState;
            //实时刷新界面进度杆会明显降低执行速度！下面采取每10个要素刷新一次 
            if (pv % 10 == 0)
                statusBar.Refresh();
        }

        private void VectorWorkCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            statusProgress.Visible = false;

            if (e.Error != null)
                MessageBox.Show(e.Error.Message, @"Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            else if (e.Cancelled)
                statusText.Text = @"Suspended!";
            else if (e.Result != null)
                statusText.Text = (string)e.Result;

            var serverUrl = GeositeServerUrl.Text?.Trim();
            var serverUser = GeositeServerUser.Text?.Trim();
            var serverPassword = GeositeServerPassword.Text?.Trim();
            UpdateDatabaseSize(serverUrl, serverUser, serverPassword);

            PostgresRun.Enabled = true;

            _loading.Run(false);
            ogcCard.Enabled = true;

        }

        private void vectorFilePool_CellBeginEdit(object sender, DataGridViewCellCancelEventArgs e)
        {
            var colIndex = e.ColumnIndex;
            var rowIndex = e.RowIndex;
            if (colIndex == 0)
            {
                var dataGridView = (DataGridView)sender;
                var col = dataGridView.Rows[rowIndex].Cells[colIndex];
                _clusterDateGridCell = $"{col.Value}".Trim();
            }
        }

        private void vectorFilePool_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            var colIndex = e.ColumnIndex;
            var rowIndex = e.RowIndex;
            if (colIndex == 0)
            {
                var row = ((DataGridView)sender).Rows[rowIndex];
                var col = row.Cells[colIndex];
                var newName = $"{col.Value}".Trim();
                var oldName = _clusterDateGridCell;
                if (string.IsNullOrWhiteSpace(newName))
                    col.Value = oldName;
                else
                {
                    try
                    {
                        col.Value = new XElement(newName).Name.LocalName;
                    }
                    catch
                    {
                        col.Value = oldName;
                    }
                }
            }
            _clusterDateGridCell = null;
        }

        private void PostgresLight_CheckedChanged(object sender, EventArgs e)
        {
            if (!PostgresLight.Checked)
                MessageBox.Show(
                    @"Unchecked means that the data is only provided for background calculation without sharing.",
                    @"Tip", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
        }

        private void localTileOpen_Click(object sender, EventArgs e)
        {
            var key = localTileOpen.Name;
            var path = key + "_path";
            var oldPath = RegEdit.Getkey(path);

            var openFolderDialog = new FolderBrowserDialog
            {
                Description = @"Please select a folder",
                ShowNewFolderButton = false
            };

            if (Directory.Exists(oldPath)) openFolderDialog.SelectedPath = oldPath;

            if (openFolderDialog.ShowDialog() == DialogResult.OK)
            {
                RegEdit.Setkey(path, openFolderDialog.SelectedPath);
                localTileFolder.Text = openFolderDialog.SelectedPath;
            }
            else
                localTileFolder.Text = string.Empty;
        }

        private void ModelOpen_Click(object sender, EventArgs e)
        {
            var key = ModelOpen.Name;
            if (!int.TryParse(RegEdit.Getkey(key), out var filterIndex))
                filterIndex = 0;
            var path = key + "_path";
            var oldPath = RegEdit.Getkey(path);

            var openFileDialog = new OpenFileDialog()
            {
                Title = @"Please select raster file[s]",
                Filter = @"Raster|*.tif;*.tiff;*.hgt;*.img;*.jp2;*.j2k;*.vrt;*.sid;*.ecw",
                FilterIndex = filterIndex,
                Multiselect = true
            };

            if (Directory.Exists(oldPath))
                openFileDialog.InitialDirectory = oldPath;

            try
            {
                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    RegEdit.Setkey(key, $"{openFileDialog.FilterIndex}");
                    RegEdit.Setkey(path, Path.GetDirectoryName(openFileDialog.FileName));
                    ModelOpenTextBox.Text = string.Join("|", openFileDialog.FileNames);
                    var rasterSourceFiles = Regex.Split(ModelOpenTextBox.Text.Trim(), @"[\s]*[|][\s]*")
                        .Where(s => !string.IsNullOrWhiteSpace(s)).ToArray();
                    if (rasterSourceFiles.Length > 0)
                    {
                        themeNameBox.Text = string.Join(
                            "|",
                            rasterSourceFiles.Select(Path.GetFileNameWithoutExtension).ToArray()
                        );
                    }
                }
                else
                    ModelOpenTextBox.Text = string.Empty;
            }
            catch (Exception error)
            {
                ModelOpenTextBox.Text = string.Empty;
                statusText.Text = error.Message;
            }
        }

        private void ModelOpenTextBox_TextChanged(object sender, EventArgs e)
        {
            tilesource_SelectedIndexChanged(sender, e);
            FormEventChanged(sender);
            ModelSave.Enabled = !string.IsNullOrWhiteSpace(ModelOpenTextBox.Text);
        }

        private void ModelSave_Click(object sender, EventArgs e)
        {
            var rasterSourceFiles = Regex.Split(ModelOpenTextBox.Text.Trim(), @"[\s]*[|][\s]*").Where(s => !string.IsNullOrWhiteSpace(s)).ToArray();
            var rasterSourceFileCount = rasterSourceFiles.Length; // >= 0 
            if (rasterSourceFileCount > 0)
            {
                var key = ModelSave.Name;
                if (!int.TryParse(RegEdit.Getkey(key), out var filterIndex))
                    filterIndex = 0;

                var path = key + "_path";
                var oldPath = RegEdit.Getkey(path);

                string saveAs = null;
                if (rasterSourceFileCount == 1)
                {
                    var saveFileDialog = new SaveFileDialog
                    {
                        Filter = @"Image(*.tif)|*.tif",
                        FilterIndex = filterIndex
                    };
                    if (Directory.Exists(oldPath))
                        saveFileDialog.InitialDirectory = oldPath;

                    if (saveFileDialog.ShowDialog() == DialogResult.OK)
                    {
                        RegEdit.Setkey(key, $"{saveFileDialog.FilterIndex}");
                        RegEdit.Setkey(path, Path.GetDirectoryName(saveFileDialog.FileName));
                        saveAs = saveFileDialog.FileName;
                    }
                }
                else
                {
                    var openFolderDialog = new FolderBrowserDialog()
                    {
                        Description = @"Please select a destination folder",
                        ShowNewFolderButton = true
                        //, RootFolder = Environment.SpecialFolder.MyComputer
                    };
                    if (Directory.Exists(oldPath))
                        openFolderDialog.SelectedPath = oldPath;
                    if (openFolderDialog.ShowDialog() == DialogResult.OK)
                    {
                        RegEdit.Setkey(path, openFolderDialog.SelectedPath);
                        saveAs = openFolderDialog.SelectedPath;
                    }
                }

                if (saveAs != null)
                {
                    var isDirectory = Path.GetExtension(saveAs) == string.Empty; //there is no 100% way to distinguish a folder from a file by path alone. 
                    statusText.Text = @"Saving ...";
                    Application.DoEvents();
                    var pointer = 0;
                    foreach (var rasterSourceFile in rasterSourceFiles)
                    {
                        if (File.Exists(rasterSourceFile))
                        {
                            var task = new Func<(bool Success, string Message)>(
                                () =>
                                {
                                    string targetFile;
                                    if (isDirectory)
                                    {
                                        var postfix = 0;
                                        do
                                        {
                                            targetFile = Path.Combine(
                                                saveAs,
                                                Path.GetFileNameWithoutExtension(rasterSourceFile) +
                                                (postfix == 0 ? "" : $"({postfix})") + ".tif");
                                            if (!File.Exists(targetFile))
                                                break;
                                            postfix++;
                                        } while (true);
                                    }
                                    else
                                        targetFile = saveAs;

                                    return GeositeTilePush.SaveAsGeoTiff(
                                        sourceFile: rasterSourceFile,
                                        targetFile: targetFile
                                    );
                                }
                            );

                            task.BeginInvoke(
                                (asyncResult) =>
                                {
                                    var result = task.EndInvoke(asyncResult);
                                    statusText.Text =
                                        $@"[{++pointer} / {rasterSourceFileCount}] {(result.Success ? @"saved." : result.Message)}";
                                    Application.DoEvents();
                                },
                                null
                            );
                        }
                    }
                }
            }
        }

        private void tilesource_SelectedIndexChanged(object sender, EventArgs e)
        {
            switch (tilesource.SelectedIndex)
            {
                case 0:
                    if (FormatStandard.Checked)
                    {
                        EPSG4326.Enabled = true;
                        EPSG4326.ThreeState =
                        EPSG4326.Checked = false;
                        tileLevels.Text = @"-1";
                        tileLevels.Enabled = true;
                    }
                    else
                    {
                        if (FormatTMS.Checked || FormatMapcruncher.Checked || FormatArcGIS.Checked)
                        {
                            EPSG4326.Enabled =
                            EPSG4326.ThreeState =
                            EPSG4326.Checked = false;
                            tileLevels.Text = @"-1";
                            tileLevels.Enabled = true;
                        }
                        else
                        {
                            EPSG4326.Enabled = false;
                            EPSG4326.ThreeState = true;
                            EPSG4326.CheckState = CheckState.Indeterminate;
                            tileLevels.Text = @"-1";
                            tileLevels.Enabled = false;
                        }
                    }
                    break;
                case 1:
                    EPSG4326.Enabled = true;
                    EPSG4326.ThreeState =
                    EPSG4326.Checked = false;
                    tileLevels.Text = @"0";
                    tileLevels.Enabled = true;
                    break;
                case 2:
                    EPSG4326.Enabled =
                    EPSG4326.ThreeState = false;
                    EPSG4326.Checked = true;
                    tileLevels.Text = @"-1";
                    tileLevels.Enabled = false;
                    break;
                default:
                    EPSG4326.Enabled = false;
                    EPSG4326.ThreeState = true;
                    EPSG4326.CheckState = CheckState.Indeterminate;
                    tileLevels.Text = @"-1";
                    tileLevels.Enabled = false;
                    break;
            }

            try
            {
                PostgresRun.Enabled = dataCards.SelectedIndex == 0
                    ? _postgreSqlConnection && !string.IsNullOrWhiteSpace(themeNameBox.Text) && tilesource.SelectedIndex is >= 0 and <= 2
                    : _postgreSqlConnection && vectorFilePool.Rows.Count > 0;
            }
            catch
            {
                PostgresRun.Enabled = dataCards.SelectedIndex != 0 && vectorFilePool.Rows.Count > 0;
            }
        }

        private void nodatabox_TextChanged(object sender, EventArgs e)
        {
            nodatabox.Text = int.TryParse(nodatabox.Text, out var i) ? $@"{i}" : @"-32768";
            FormEventChanged(sender);
        }

        private void rasterTileSize_TextChanged(object sender, EventArgs e)
        {
            rasterTileSize.Text = int.TryParse(rasterTileSize.Text, out var i) ? i < 10 ? "10" : $"{i}" : "100";
            FormEventChanged(sender);
        }

        private void DeepZoomOpen_Click(object sender, EventArgs e)
        {
            var key = DeepZoomOpen.Name;
            if (!int.TryParse(RegEdit.Getkey(key), out var filterIndex))
                filterIndex = 0;

            var path = key + "_path";
            var oldPath = RegEdit.Getkey(path);

            var openFileDialog = new OpenFileDialog()
            {
                Title = @"Please select image file[s]",
                Filter = @"Image|*.bmp;*.gif;*.jpg;*.jpeg;*.png;*.tif;*.tiff",
                FilterIndex = filterIndex,
                Multiselect = true
            };
            if (Directory.Exists(oldPath))
                openFileDialog.InitialDirectory = oldPath;
            try
            {
                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    RegEdit.Setkey(key, $"{openFileDialog.FilterIndex}");
                    RegEdit.Setkey(path, Path.GetDirectoryName(openFileDialog.FileName));
                    DeepZoomOpenTextBox.Text = string.Join("|", openFileDialog.FileNames);
                }
                else
                    DeepZoomOpenTextBox.Text = string.Empty;
            }
            catch (Exception error)
            {
                DeepZoomOpenTextBox.Text = string.Empty;
                statusText.Text = error.Message;
            }
        }

        private void DeepZoomSave_Click(object sender, EventArgs e)
        {
            var key = DeepZoomSave.Name;

            var path = key + "_path";
            var oldPath = RegEdit.Getkey(path);

            var openFolderDialog = new FolderBrowserDialog()
            {
                Description = @"Please select a destination folder",
                ShowNewFolderButton = true
                //, RootFolder = Environment.SpecialFolder.MyComputer
            };
            if (Directory.Exists(oldPath))
                openFolderDialog.SelectedPath = oldPath;
            var result = openFolderDialog.ShowDialog();
            if (result == DialogResult.OK)
            {
                RegEdit.Setkey(path, openFolderDialog.SelectedPath);
                DeepZoomSaveTextBox.Text = openFolderDialog.SelectedPath;
            }
        }

        private void DeepZoomRun_Click(object sender, EventArgs e)
        {
            var deepZoomFiles = Regex.Split(DeepZoomOpenTextBox.Text.Trim(), @"[\s]*[|][\s]*").Where(s => !string.IsNullOrWhiteSpace(s)).ToArray();
            var deepZoomFileCount = deepZoomFiles.Length; // >= 0 

            if (deepZoomFileCount > 0 && DeepZoomSaveTextBox.Text.Length > 0)
            {
                if (Directory.Exists(DeepZoomSaveTextBox.Text))
                {
                    for (var i = 0; i < deepZoomFileCount; i++)
                    {
                        var deepZoomFile = deepZoomFiles[i];
                        if (File.Exists(deepZoomFile))
                        {
                            var newfile = Path.Combine(
                                DeepZoomSaveTextBox.Text, //目标文件夹
                                Path.GetFileNameWithoutExtension(deepZoomFile) //目标文件名，暂取原始图像文件基本名称
                            );

                            var xmlfile = Path.ChangeExtension(newfile, "xml");
                            var tilespath = Path.ChangeExtension(newfile, null) + "_files";
                            var candowork = true;
                            if (File.Exists(xmlfile) || Directory.Exists(tilespath))
                            {
                                //if (MessageBox.Show(
                                //    @"The target file already exists, do you want to replace it?",
                                //    @"Tips",
                                //    MessageBoxButtons.YesNo,
                                //    MessageBoxIcon.Question
                                //) == DialogResult.Yes)
                                //{
                                //    if (File.Exists(xmlfile))
                                //        File.Delete(xmlfile);
                                //    if (Directory.Exists(tilespath))
                                //        Directory.Delete(tilespath, true);
                                //    candowork = true;
                                //}
                                try
                                {
                                    if (File.Exists(xmlfile))
                                        File.Delete(xmlfile);
                                    if (Directory.Exists(tilespath))
                                        Directory.Delete(tilespath, true);
                                }
                                catch
                                {
                                    candowork = false;
                                }
                            }

                            if (candowork)
                            {
                                var deepZoomObject = new ImageCreator();
                                //DeepZoomObj对象的默认值如下：
                                //DeepZoomObj.TileSize = 256;
                                //DeepZoomObj.TileFormat =Microsoft.DeepZoomTools.ImageFormat.Jpg; //Jpg Png Wdp AutoSelect
                                //DeepZoomObj.ImageQuality = 0.95;
                                //DeepZoomObj.TileOverlap = 0;
                                if (DeepZoomLevels.Text.Trim() != "-1")
                                {
                                    if (int.TryParse(DeepZoomLevels.Text.Trim(), out var level))
                                        deepZoomObject.MaxLevel = level; // 0 --- 30
                                }

                                //----------- 事件侦听 ---------------

                                //DeepZoomObject.InputNeeded += delegate (object Sender, StreamEventArgs Event)
                                //{
                                //    //第一步：若输入为文件流时，可通过【Event】参数指定文件流；若输入为文件时，此事件不可侦听！否则抛出异常
                                //};

                                //DeepZoomObject.InputCompleted += delegate (object Sender, StreamEventArgs Event)
                                //{
                                //    //第二步：到达这里，说明所需的各项参数输入完成并成功初始化【DeepZoomObject】对象
                                //};

                                deepZoomObject.InputImageInfo += delegate
                                //(object Sender, ImageInfoEventArgs Event)
                                {
                                    //第三步
                                    //this.
                                        Invoke(
                                        new Action(
                                            () =>
                                            {
                                                _loading.Run();
                                                statusText.Text = @"Slicing ...";
                                                DeepZoomRun.Enabled = false;
                                            }
                                        )
                                    );

                                };

                                deepZoomObject.CreateDirectory += delegate (object _, DirectoryEventArgs thisEvent)
                                {
                                    //第四步 ......
                                    //this.
                                        Invoke(
                                        new Action(
                                            () =>
                                            {
                                                statusText.Text = $@"Creating - {thisEvent.DirectoryName}";
                                            }
                                        )
                                    );

                                };

                                //DeepZoomObject.OutputCompleted += delegate (object Sender, StreamEventArgs Event)
                                //{
                                //    //第五步
                                //};

                                deepZoomObject.OutputInfo += delegate
                                //(object Sender, OutputInfoEventArgs Event)
                                {
                                    //第六步
                                    //this.
                                        Invoke(
                                        new Action(
                                            () =>
                                            {
                                                _loading.Run(false);
                                                DeepZoomRun.Enabled = true;
                                                statusText.Text = @"Finished";
                                            }
                                        )
                                    );

                                };

                                //DeepZoomObj.OutputNeeded += delegate (object Sender, StreamEventArgs Event)
                                //{
                                //    //第七步：若输出为文件流时，可通过【Event】参数指定文件流；若输出为文件时，此事件不可侦听！否则抛出异常
                                //};

                                //将【Create】函数的执行结果（XElement类型）转换为文本（Json）格式，输出给界面控件
                                //themeMetadata.Text =
                                //    DeepZoomObject.XElementToJson(
                                //DeepZoomObject.Create( 
                                //        //Create函数自动启动上述事件
                                //       deepZoomFile,
                                //        newfile
                                //    )
                                //);

                                /*
                                    <Image TileSize="254" Overlap="1" MinZoom="0" MaxZoom="12" Type="deepzoom" CRS="simple" Format="jpg" ServerFormat="Default" xmlns="http://schemas.microsoft.com/deepzoom/2009">
                                      <Size Width="3968" Height="2976" />
                                    </Image>                             
                                 */

                                deepZoomObject.Create(
                                    //Create函数自动启动上述事件
                                    deepZoomFile,
                                    newfile
                                );
                            }
                        }
                    }
                }
            }
        }

        private void TileFormatOpen_Click(object sender, EventArgs e)
        {
            var key = TileFormatOpen.Name;
            var path = key + "_path";
            var oldPath = RegEdit.Getkey(path);

            var openFolderDialog = new FolderBrowserDialog
            {
                Description = @"Please select a folder that contains tiles",
                ShowNewFolderButton = false
            };
            if (Directory.Exists(oldPath))
                openFolderDialog.SelectedPath = oldPath;
            var result = openFolderDialog.ShowDialog();
            if (result == DialogResult.OK)
            {
                RegEdit.Setkey(path, openFolderDialog.SelectedPath);
                TileFormatOpenBox.Text = openFolderDialog.SelectedPath;
            }
            else
                TileFormatOpenBox.Text = string.Empty;
        }

        private void TileFormatSave_Click(object sender, EventArgs e)
        {
            var key = TileFormatOpen.Name;
            var path = key + "_path";
            var oldPath = RegEdit.Getkey(path);

            var openFolderDialog = new FolderBrowserDialog()
            {
                Description = @"Please select a destination folder",
                ShowNewFolderButton = true
                //, RootFolder = Environment.SpecialFolder.MyComputer
            };
            if (Directory.Exists(oldPath))
            {
                openFolderDialog.SelectedPath = oldPath;
            }
            var result = openFolderDialog.ShowDialog();
            if (result == DialogResult.OK)
            {
                RegEdit.Setkey(path, openFolderDialog.SelectedPath);
                TileFormatSaveBox.Text = openFolderDialog.SelectedPath;
            }
            else
                TileFormatSaveBox.Text = string.Empty;
        }

        private void tileconvert_Click(object sender, EventArgs e)
        {
            if (Directory.Exists(TileFormatOpenBox.Text) && Directory.Exists(TileFormatSaveBox.Text))
            {
                var methodCode =
                    maptilertoogc.Checked ? 0 :
                    mapcrunchertoogc.Checked ? 3 :
                    ogctomapcruncher.Checked ? 2 :
                    ogctomaptiler.Checked ? 1 :
                    -1;
                if (methodCode > -1)
                {
                    //-------------------- 异步消息模式 ---------
                    var tileFormatTask = new TileConversion();
                    tileFormatTask.onGeositeEvent += delegate (object _, GeositeEventArgs thisEvent)
                    {
                        switch (thisEvent.code)
                        {
                            case 0:
                                _loading.Run();
                                break;
                            case 1:
                                _loading.Run(false);
                                break;
                            default:
                                statusText.Text = thisEvent.message ?? string.Empty;
                                break;
                        }
                    };
                    statusText.Text =
                        $@"{tileFormatTask.Convert(TileFormatOpenBox.Text, TileFormatSaveBox.Text, methodCode)} tiles were processed";
                }
            }
        }

        private void DeepZoomChanged(object sender, EventArgs e)
        {
            DeepZoomRun.Enabled = !string.IsNullOrWhiteSpace(DeepZoomOpenTextBox.Text) && !string.IsNullOrWhiteSpace(DeepZoomSaveTextBox.Text);
        }

        private void TileFormatChanged(object sender, EventArgs e)
        {
            tileconvert.Enabled = !string.IsNullOrWhiteSpace(TileFormatOpenBox.Text) && !string.IsNullOrWhiteSpace(TileFormatSaveBox.Text);
        }

        private void themeNameBox_TextChanged(object sender, EventArgs e)
        {
            tilesource_SelectedIndexChanged(sender, e);
            FormEventChanged(sender);
        }

        private void localTileFolder_TextChanged(object sender, EventArgs e)
        {
            tilesource_SelectedIndexChanged(sender, e);
            FormEventChanged(sender);
        }

        private void tilewebapi_TextChanged(object sender, EventArgs e)
        {
            tilesource_SelectedIndexChanged(sender, e);
            FormEventChanged(sender);
        }

        private void wmtsMinZoom_SelectedIndexChanged(object sender, EventArgs e)
        {
            //if (int.Parse(wmtsMinZoom.Text) >= int.Parse(wmtsMaxZoom.Text))
            //{
            //    wmtsMinZoom.Text = @"0";
            //}
            FormEventChanged(sender);
        }

        private void wmtsMaxZoom_SelectedIndexChanged(object sender, EventArgs e)
        {
            //if (int.Parse(wmtsMaxZoom.Text) <= int.Parse(wmtsMinZoom.Text))
            //{
            //    wmtsMinZoom.Text = @"24";
            //}
            FormEventChanged(sender);
        }

        private void FormatStandard_CheckedChanged(object sender, EventArgs e)
        {
            EPSG4326.Enabled = true;
            EPSG4326.ThreeState =
                EPSG4326.Checked = false;
            tileLevels.Text = @"-1";
            tileLevels.Enabled = true;
            FormEventChanged(sender);
        }

        private void FormatTMS_CheckedChanged(object sender, EventArgs e)
        {
            EPSG4326.Enabled =
                EPSG4326.ThreeState =
                    EPSG4326.Checked = false;
            tileLevels.Text = @"-1";
            tileLevels.Enabled = true;
            FormEventChanged(sender);
        }

        private void FormatMapcruncher_CheckedChanged(object sender, EventArgs e)
        {
            EPSG4326.Enabled =
                EPSG4326.ThreeState =
                    EPSG4326.Checked = false;
            tileLevels.Text = @"-1";
            tileLevels.Enabled = true;
            FormEventChanged(sender);
        }

        private void FormatArcGIS_CheckedChanged(object sender, EventArgs e)
        {
            EPSG4326.Enabled =
                EPSG4326.ThreeState =
                    EPSG4326.Checked = false;
            tileLevels.Text = @"-1";
            tileLevels.Enabled = true;
            FormEventChanged(sender);
        }

        private void FormatDeepZoom_CheckedChanged(object sender, EventArgs e)
        {
            EPSG4326.Enabled = false;
            EPSG4326.ThreeState = true;
            EPSG4326.CheckState = CheckState.Indeterminate;
            tileLevels.Text = @"-1";
            tileLevels.Enabled = false;
            FormEventChanged(sender);
        }

        private void FormatRaster_CheckedChanged(object sender, EventArgs e)
        {
            EPSG4326.Enabled = false;
            EPSG4326.ThreeState = true;
            EPSG4326.CheckState = CheckState.Indeterminate;
            tileLevels.Text = @"-1";
            tileLevels.Enabled = false;
            FormEventChanged(sender);
        }

        private void wmtsSpider_CheckedChanged(object sender, EventArgs e)
        {
            wmtsMinZoom.Enabled = wmtsMaxZoom.Enabled = !wmtsSpider.Checked;
            FormEventChanged(sender);
        } 

        private void deleteForest_Click(object sender, EventArgs e)
        {
            if (!_clusterUser.status) 
                return;

            var result = PostgreSqlHelper.Scalar(
                "SELECT id FROM forest WHERE id = @id AND name = @name::text LIMIT 1;",
                new Dictionary<string, object>
                {
                    {"id", _clusterUser.forest},
                    {"name", _clusterUser.name}
                }
            );
            if (result == null)
            {
                statusText.Text = @"Nothing was found";
                return;
            }

            var random = new Random((int)DateTime.Now.Ticks & 0x0000FFFF);
            var r1 = random.Next(0, 100);
            var r2 = random.Next(0, 100);
            {
                if (Interaction.InputBox($"  For safety reasons, Please answer a question.\n\n  {r1} + {r2} = ?",
                        "Caution")
                    == $"{r1 + r2}")
                {
                    _loading.Run();

                    statusText.Text = @"Deleting ...";
                    databasePanel.Enabled = false;

                    _clusterUser.status = false;

                    var task = new Func<bool>(
                        () =>
                            PostgreSqlHelper.NonQueryAsync(
                                "DELETE FROM forest WHERE id = @id AND name = @name::text;",
                                new Dictionary<string, object>
                                {
                                    {"id", _clusterUser.forest},
                                    {"name", _clusterUser.name}
                                }
                            ).Result !=
                            null
                    );

                    task.BeginInvoke(
                        (x) =>
                        {
                            var success = task.EndInvoke(x);
                            //this.
                            Invoke(
                                new Action(
                                    () =>
                                    {
                                        if (success)
                                        {
                                            //更新 DataGrid 控件 - ClusterDate
                                            _clusterDate?.Reset();
                                            foreach (var statusCell in 
                                                     vectorFilePool
                                                         .SelectedRows
                                                         .Cast<DataGridViewRow>()
                                                         .Where(row => !row.IsNewRow)
                                                         .Select(row => vectorFilePool.CurrentCell = row.Cells[2]))
                                            {
                                                statusCell.Value =
                                                statusCell.ToolTipText = "";
                                            }

                                            statusText.Text = @"Delete succeeded";
                                        }
                                        else
                                        {
                                            statusText.Text = @"Delete failed";
                                        }

                                        databasePanel.Enabled = true;
                                        _loading.Run(false);
                                    }
                                )
                            );
                        },
                        null
                    );
                }
                else
                {
                    statusText.Text = @"The delete operation was not performed";
                }
            }
        }

        private void dataCards_SelectedIndexChanged(object sender, EventArgs e)
        {
            PostgresRun.Enabled = dataCards.SelectedIndex == 0
                ? _postgreSqlConnection && !string.IsNullOrWhiteSpace(themeNameBox.Text) &&
                  tilesource.SelectedIndex is >= 0 and <= 2
                : _postgreSqlConnection && vectorFilePool.Rows.Count > 0;
        }

        private void ogcCard_SelectedIndexChanged(object sender, EventArgs e)
        {
            FormEventChanged(sender);
        }

        private void RasterRunClick()
        {
            string statusError = null;
            short.TryParse(tileLevels.Text, out var tileMatrix);
            var tileType = TileType.Standard;
            var typeCode = 0;
            XElement themeMetadataX = null;

            switch (tilesource.SelectedIndex)
            {
                case 0:
                    if (!Directory.Exists(localTileFolder.Text))
                        statusError = @"Folder does not exist";
                    else
                    {
                        if (FormatStandard.Checked)
                        {
                            tileType = TileType.Standard;
                            typeCode = EPSG4326.Checked ? 11001 : 11002;
                            if (!Directory
                                    .GetDirectories(localTileFolder.Text)
                                    .Any(dir => Regex.IsMatch(Path.GetFileName(dir), @"^\d+$",
                                        RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Multiline))
                               )
                                statusError = @"Folder does not meet the requirements";
                        }
                        else if (FormatTMS.Checked)
                        {
                            tileType = TileType.TMS;
                            typeCode = EPSG4326.Checked ? 11001 : 11002;
                            if (!Directory
                                    .GetDirectories(localTileFolder.Text)
                                    .Any(dir => Regex.IsMatch(Path.GetFileName(dir), @"^\d+$",
                                        RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Multiline))
                                )
                                statusError = @"Folder does not meet the requirements";
                        }
                        else if (FormatMapcruncher.Checked)
                        {
                            tileType = TileType.MapCruncher;
                            typeCode = 11002; //微软MapCruncher仅支持【球体墨卡托】投影
                            if (!Directory
                                    .EnumerateFiles(localTileFolder.Text)
                                    .Any(file => Regex.IsMatch(Path.GetFileName(file), @"^[\d]+.png$",
                                        RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Multiline))
                                )
                                statusError = @"Folder does not meet the requirements";
                        }
                        else if (FormatArcGIS.Checked)
                        {
                            tileType = TileType.ARCGIS;
                            typeCode = EPSG4326.Checked ? 11001 : 11002;
                            // 默认约定：???\*_alllayers
                            /*
                                ESRI切片符合【专题层名称（Layers） / _alllayers / LXX / RXXXXXXXX / CXXXXXXXX.扩展名】五级目录树结构
                                其中：
                                1）缩放级目录采用L字母打头并按【二位十进制数字】命名
                                2）纵向图块坐标编码按【八位十六进制数字】命名且以【R】字母打头
                                3）横向图块坐标编码按【八位十六进制数字】命名且以【C】字母打头
                                4）扩展名支持：".png" or ".jpg" or ".jpeg" or ".gif"
                            */
                            var tileFolder =
                                Directory
                                    .GetDirectories(localTileFolder.Text)
                                    .FirstOrDefault(dir => Regex.IsMatch(Path.GetFileName(dir), @"^([\s\S]*?)(_alllayers)$",
                                        RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Multiline));
                            if (tileFolder != null)
                                localTileFolder.Text = tileFolder;

                            if (!Directory
                                    .GetDirectories(localTileFolder.Text)
                                    .Any(dir => Regex.IsMatch(Path.GetFileName(dir), "^L([0-9]+)$",
                                        RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Multiline))
                               )
                                statusError = @"Folder does not meet the requirements";
                        }
                        else if (FormatDeepZoom.Checked)
                        {
                            tileType = TileType.DeepZoom;
                            typeCode = 11000;
                            // 默认约定：???_files
                            var tileFolder =
                                Directory
                                    .GetDirectories(localTileFolder.Text)
                                    .FirstOrDefault(dir => Regex.IsMatch(Path.GetFileName(dir), @"^([\s\S]+)(_files)$",
                                        RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Multiline));
                            if (tileFolder != null)
                                localTileFolder.Text = tileFolder;
                            if (!Directory
                                    .GetDirectories(localTileFolder.Text)
                                    .Any(dir => Regex.IsMatch(Path.GetFileName(dir), "([0-9]+)$",
                                        RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Multiline)))
                                statusError = @"Folder does not meet the requirements";
                            else
                            {
                                var xmlName = Regex.Match(localTileFolder.Text, @"^([\s\S]+)(_files)$",
                                        RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Multiline)
                                    .Groups[1].Value;
                                //image_files ===> Groups[1]:【image】  Groups[2]:【_files】
                                if (!string.IsNullOrWhiteSpace(xmlName))
                                {
                                    var xmlFile = $"{xmlName}.xml";
                                    if (File.Exists(xmlFile))
                                    {
                                        try
                                        {
                                            /*  deepzoom 的元数据xml文件样例：
                                                <?xml version="1.0" encoding="utf-8"?>
                                                <Image TileSize="254" Overlap="1" MinZoom="0" MaxZoom="12" Type="deepzoom" CRS="simple" Format="jpg" ServerFormat="Default" xmlns="http://schemas.microsoft.com/deepzoom/2009">
                                                    <Size Width="3968" Height="2976" />
                                                </Image>               
                                             */
                                            var metaDataX = XElement.Load(xmlFile, LoadOptions.None);
                                            XNamespace ns = metaDataX.Attribute("xmlns")?.Value;
                                            var sizeX = metaDataX.Element(ns + "Size");

                                            /*  GeositeXML 约定样例：
                                                <property remarks="注意：瓦片层的元数据信息应在member父级（最近容器）的property中表述">
	                                                <minZoom remarks="最小缩放级，默认0">0</minZoom>
	                                                <maxZoom remarks="最大缩放级，默认18" >18</maxZoom>
	                                                <tileSize remarks="瓦片像素尺寸，默认256">256</tileSize>
	                                                <boundary remarks="边框范围">
		                                                <north remarks="上北，比如：85.0511287798066">85.0511287798066</north>
		                                                <south remarks="下南，比如：-85.0511287798066">-85.0511287798066</south>
		                                                <west remarks="左西，比如：-180">-180.0</west>
		                                                <east remarks="右东,比如：180">180.0</east>
	                                                </boundary>
                                                </property>                                             
                                             */
                                            themeMetadataX = new XElement(
                                                "property",
                                                new XElement(
                                                    "name", "deepzoom"
                                                ),
                                                new XElement(
                                                    "minZoom", metaDataX.Attribute("MinZoom")?.Value //
                                                ),
                                                new XElement(
                                                    "maxZoom", metaDataX.Attribute("MaxZoom")?.Value //
                                                ),
                                                new XElement(
                                                    "tileSize", metaDataX.Attribute("TileSize")?.Value
                                                ),
                                                new XElement(
                                                    "overlap", metaDataX.Attribute("Overlap")?.Value
                                                ),
                                                new XElement(
                                                    "type", metaDataX.Attribute("Type")?.Value
                                                ),
                                                new XElement(
                                                    "crs", metaDataX.Attribute("CRS")?.Value
                                                ),
                                                new XElement(
                                                    "format", metaDataX.Attribute("Format")?.Value //可忽略
                                                ),
                                                new XElement(
                                                    "serverFormat", metaDataX.Attribute("ServerFormat")?.Value
                                                ),
                                                new XElement(
                                                    "xmlns", metaDataX.Attribute("xmlns")?.Value
                                                ),
                                                new XElement(
                                                    "size", new XElement(
                                                        "width", sizeX?.Attribute("Width")?.Value //
                                                    ), new XElement(
                                                        "height", sizeX?.Attribute("Height")?.Value //
                                                    )
                                                ), new XElement(
                                                    "boundary", new XElement(
                                                        "north", sizeX?.Attribute("Height")?.Value
                                                    ), new XElement(
                                                        "south", 0
                                                    ), new XElement(
                                                        "west", 0
                                                    ), new XElement(
                                                        "east", sizeX?.Attribute("Width")?.Value
                                                    )
                                                )
                                            );
                                        }
                                        catch (Exception xmlError)
                                        {
                                            statusError = xmlError.Message;
                                        }
                                    }
                                    else
                                        statusError = @$"[{xmlName}.xml] metadata file not found";
                                }
                                else
                                    statusError = @"Folder does not meet the requirements";
                            }
                        }
                        else
                        {
                            tileType = TileType.Raster; //maptiler 软件自动产生元数据文件：metadata.json
                            typeCode = 11000;

                            if (!Directory
                                    .GetDirectories(localTileFolder.Text)
                                    .Any(dir => Regex.IsMatch(Path.GetFileName(dir), @"^\d+$",
                                        RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Multiline))
                                )
                                statusError = @"Folder does not meet the requirements";
                            else
                            {
                                /*  metadata.json 样例：
                                {
                                    "name": "maptiler",
                                    "version": "1.1.0",
                                    "description": "",
                                    "attribution": "Rendered with <a href=\"https://www.maptiler.com/\">MapTiler Desktop</a>",
                                    "type": "overlay",
                                    "format": "png",
                                    "minzoom": "0",
                                    "maxzoom": "4",
                                    "scale": "1.000000",
                                    "profile": "custom",
                                    "crs": "RASTER",
                                    "extent": [0.00000000, -2976.00000000, 3968.00000000, 0.00000000],
                                    "tile_matrix": [{
                                            "id": "0",
                                            "tile_size": [256, 256],
                                            "origin": [0.00000000, 0.00000000],
                                            "extent": [0.00000000, -2976.00000000, 3968.00000000, 0.00000000],
                                            "pixel_size": [16.00000000, -16.00000000],
                                            "scale_denominator": 57142.85714286
                                        },
                                        {
                                            "id": "1",
                                            "tile_size": [256, 256],
                                            "origin": [0.00000000, 0.00000000],
                                            "extent": [0.00000000, -2976.00000000, 3968.00000000, 0.00000000],
                                            "pixel_size": [8.00000000, -8.00000000],
                                            "scale_denominator": 28571.42857143
                                        }
                                        ......
                                    ]
                                    }
                                 */

                                var jsonFile = Path.Combine(localTileFolder.Text, "metadata.json");
                                if (File.Exists(jsonFile))
                                {
                                    using var sr = FreeText.FreeTextEncoding.OpenFreeTextFile(jsonFile);
                                    var metaDataX = JsonConvert.DeserializeXNode(sr.ReadToEnd(), "MapTiler")?.Root;

                                    /*  
                                        <MapTiler>
                                          <name>maptiler</name>
                                          <version>1.1.0</version>
                                          <description></description>
                                          <attribution>Rendered with &lt;a href="https://www.maptiler.com/"&gt;MapTiler Desktop&lt;/a&gt;</attribution>
                                          <type>overlay</type>
                                          <format>png</format>
                                          <minzoom>0</minzoom>
                                          <maxzoom>4</maxzoom>
                                          <scale>1.000000</scale>
                                          <profile>custom</profile>
                                          <crs>RASTER</crs>
                                          <extent>0</extent>
                                          <extent>-2976</extent> //高度
                                          <extent>3968</extent> //宽度
                                          <extent>0</extent>
                                          <tile_matrix>
                                            ...
                                          </tile_matrix>
                                          <tile_matrix>
                                            ...
                                          </tile_matrix>
                                          <tile_matrix>
                                           ...
                                          </tile_matrix>
                                          ......  
                                        </MapTiler>                                 
                                    */
                                    if (metaDataX != null)
                                    {
                                        var extent = metaDataX.Elements("extent").ToArray();
                                        if (extent.Length == 4)
                                        {
                                            themeMetadataX = new XElement(
                                                "property",
                                                new XElement(
                                                    "name", metaDataX.Element("name")?.Value
                                                ),
                                                new XElement(
                                                    "minZoom", metaDataX.Element("minzoom")?.Value
                                                ),
                                                new XElement(
                                                    "maxZoom", metaDataX.Element("maxzoom")?.Value
                                                ),
                                                new XElement(
                                                    "tileSize", metaDataX.Elements("tile_matrix").FirstOrDefault()?.Element("tile_size")?.Value
                                                ),
                                                new XElement(
                                                    "overlap", 0
                                                ),
                                                new XElement(
                                                    "type", metaDataX.Element("type")?.Value
                                                ),
                                                new XElement(
                                                    "crs", metaDataX.Element("crs")?.Value
                                                ),
                                                new XElement(
                                                    "format", metaDataX.Element("Format")?.Value //可忽略
                                                ),
                                                new XElement(
                                                    "scale", metaDataX.Element("scale")?.Value
                                                ),
                                                new XElement(
                                                    "profile", metaDataX.Element("profile")?.Value
                                                ),
                                                new XElement(
                                                    "version", metaDataX.Element("version")?.Value
                                                ),
                                                new XElement(
                                                    "attribution", metaDataX.Element("attribution")?.Value
                                                ),
                                                new XElement(
                                                    "description", metaDataX.Element("description")?.Value
                                                ),
                                                new XElement(
                                                    "size", new XElement(
                                                        "width", Math.Abs(int.Parse(extent[2].Value))
                                                    ), new XElement(
                                                        "height", Math.Abs(int.Parse(extent[1].Value))
                                                    )
                                                ), new XElement(
                                                    "boundary", new XElement(
                                                        "north", Math.Abs(int.Parse(extent[1].Value))
                                                    ), new XElement(
                                                        "south", 0
                                                    ), new XElement(
                                                        "west", 0
                                                    ), new XElement(
                                                        "east", Math.Abs(int.Parse(extent[2].Value))
                                                    )
                                                )
                                            );
                                        }
                                        else
                                            statusError = @"[metadata.json] metadata format is incorrect";
                                    }
                                    else
                                        statusError = @"[metadata.json] metadata format is incorrect";
                                }
                                else
                                    statusError = @"[metadata.json] metadata file not found";
                            }
                        }
                    }

                    break;
                case 1:
                    var tilesWest = Map4326.Degree2DMS(DMS: wmtsWest.Text);
                    var tilesEast = Map4326.Degree2DMS(DMS: wmtsEast.Text);
                    var tilesSouth = Map4326.Degree2DMS(DMS: wmtsSouth.Text);
                    var tilesNorth = Map4326.Degree2DMS(DMS: wmtsNorth.Text);

                    if (tileMatrix < 0 && wmtsSpider.Checked)
                        statusError = @"Level should be >= 0"; // 爬虫需要每级单独干活
                    else
                    {
                        if (!Regex.IsMatch(
                            tilewebapi.Text,
                            @"\b(https?|ftp|file)://[\s\S]+",
                            RegexOptions.IgnoreCase | RegexOptions.Multiline))
                            statusError = @"URL template does not meet requirements";
                        else
                        {
                            if (tilesWest == string.Empty || double.Parse(tilesWest) < -180 ||
                                double.Parse(tilesWest) > 180)
                                statusError = @"West Should be between [-180，180]";
                            else
                            {
                                if (tilesEast == string.Empty || double.Parse(tilesEast) < -180 ||
                                    double.Parse(tilesEast) > 180)
                                    statusError = @"East Should be between [-180，180]";
                                else
                                {
                                    if (tilesSouth == string.Empty || double.Parse(tilesSouth) < -90 ||
                                        double.Parse(tilesSouth) > 90)
                                        statusError = @"South Should be between [-90，90]";
                                    else
                                    {
                                        if (tilesNorth == string.Empty || double.Parse(tilesNorth) < -90 ||
                                            double.Parse(tilesNorth) > 90)
                                            statusError = @"North Should be between [-90，90]";
                                        else
                                        {
                                            if (double.Parse(tilesWest) > double.Parse(tilesEast))
                                                statusError = @"West should not exceed East";
                                            else
                                            {
                                                if (double.Parse(tilesSouth) > double.Parse(tilesNorth))
                                                    statusError = @"South should not exceed North";
                                                else
                                                {
                                                    typeCode = EPSG4326.Checked ? 10001 : 10002; //10000 //其余暂按无投影对待??

                                                    //是否符合{x}{y}{z}模板样式，无论{x}{y}{z}次序如何排列! 且z前后可附带+-运算符，以便应对z起始定义不一致的问题
                                                    if (!Regex.IsMatch(tilewebapi.Text,
                                                            @".*?(?=.*?{x})(?=.*?{y})(?=.*?{([\d]+\s*[\+\-]\s*)?z(\s*[\+\-]\s*[\d]+)?}).*",
                                                            RegexOptions.IgnoreCase | RegexOptions.Multiline))
                                                    {
                                                        var foundBingmap = Regex.IsMatch(tilewebapi.Text,
                                                            ".*?{bingmap}.*",
                                                            RegexOptions.IgnoreCase | RegexOptions.Multiline);
                                                        if (!foundBingmap)
                                                        {
                                                            var foundEsri = Regex.IsMatch(tilewebapi.Text,
                                                                ".*?{esri}.*",
                                                                RegexOptions.IgnoreCase | RegexOptions.Multiline);
                                                            if (!foundEsri)
                                                                statusError =
                                                                    @"URL template does not meet requirements";
                                                            else
                                                                tileType = TileType.ARCGIS;
                                                        }
                                                        else
                                                            tileType = TileType.MapCruncher;
                                                    }
                                                    else
                                                        tileType = TileType.Standard;

                                                    if (string.IsNullOrWhiteSpace(statusError))
                                                    {
                                                        if (Regex.IsMatch(tilewebapi.Text, @"\{s\}",
                                                            RegexOptions.IgnoreCase))
                                                        {
                                                            if (string.IsNullOrWhiteSpace(subdomainsBox.Text))
                                                                statusError = @"Subdomains should be specified";
                                                            else
                                                            {
                                                                if (!Regex.IsMatch(subdomainsBox.Text, @"^[a-z\d]+$",
                                                                    RegexOptions.IgnoreCase))
                                                                    statusError = @"Subdomains does not meet requirements";
                                                            }
                                                        }
                                                        else
                                                        {
                                                            if (!string.IsNullOrWhiteSpace(subdomainsBox.Text))
                                                                statusError = @"Subdomains should be blank";
                                                        }
                                                        if (wmtsSpider.Checked && int.Parse(wmtsMinZoom.Text) > int.Parse(wmtsMaxZoom.Text))
                                                            statusError = @"MinZoom Should be <= MaxZoom";

                                                        if (string.IsNullOrWhiteSpace(statusError) && !wmtsSpider.Checked)
                                                        {
                                                            //如果不执行爬虫操作（不将远程瓦片推送到数据库，仅推送远程服务地址模板）
                                                            themeMetadataX = new XElement(
                                                                "property", new XElement(
                                                                    "minZoom", wmtsMinZoom.Text
                                                                ), new XElement(
                                                                    "maxZoom", wmtsMaxZoom.Text
                                                                ), new XElement(
                                                                    "tileSize", wmtsSize.Text
                                                                    ), new XElement(
                                                                    "format", MIMEBox.Text
                                                                ), new XElement(
                                                                    "boundary", new XElement(
                                                                        "north", wmtsNorth.Text
                                                                    ), new XElement(
                                                                        "south", wmtsSouth.Text
                                                                    ), new XElement(
                                                                        "west", wmtsWest.Text
                                                                    ), new XElement(
                                                                        "east", wmtsEast.Text
                                                                    )
                                                                )
                                                            );
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }

                    break;
                case 2:
                    if (string.IsNullOrWhiteSpace(ModelOpenTextBox.Text))
                        statusError = @"File cannot be empty";
                    else
                    {
                        rasterTileSize.Text = int.TryParse(rasterTileSize.Text, out var size)
                            ? size < 10
                                ? @"10"
                                : size > 1024 ? "1024" : $"{size}"
                            : @"100";
                        tileType = TileType.Standard;
                        EPSG4326.Checked = true; //暂强行按4326对待
                        typeCode = 12001; //暂强行按4326对待。//EPSG4326.Checked ? 12001 : 12002; //12000; //其余暂按无投影对待???

                        themeMetadataX = new XElement("property"); //暂仅产生属性标签，以便不提示交互对话框
                    }

                    break;
                default:
                    return;
            }

            if (!string.IsNullOrWhiteSpace(statusError))
            {
                statusText.Text = statusError;
                return;
            }

            if (themeMetadataX == null)
            {
                //提供追加自定义元数据的机会
                if (!_noPromptMetaData)
                {
                    var metaData = new MetaData();
                    metaData.ShowDialog();
                    if (metaData.Ok)
                    {
                        themeMetadataX = metaData.MetaDataX;
                        _noPromptMetaData = metaData.DonotPrompt;
                    }
                }
            }
            if (themeMetadataX != null && themeMetadataX.Name != "property")
                themeMetadataX.Name = "property";

            ogcCard.Enabled = false;
            _loading.Run();
            PostgresRun.Enabled = dataCards.SelectedIndex != 0 && vectorFilePool.Rows.Count > 0;
            statusProgress.Visible = true;
            rasterWorker.RunWorkerAsync(
                (
                    index: tilesource.SelectedIndex,
                    theme: themeNameBox.Text.Trim(), //针对平铺栅格类型，可能有多个专题名称且由【|】分隔
                    type: tileType, //Type: 0=ogc 1=tms 2=mapcruncher 3=arcgis 4=deepzoom 5=raster 
                    typeCode,
                    update: UpdateBox.Checked, //更新模式？
                    light: PostgresLight.Checked, //明数据/共享模式？
                    metadata: themeMetadataX,
                    srid: tileType is TileType.DeepZoom or TileType.Raster ? 0 : tileType == TileType.Standard && EPSG4326.Checked ? 4326 : 3857,
                    tileMatrix
                )
            ); // 异步执行：RasterWorkStart 函数
        }

        private string RasterWorkStart(BackgroundWorker rasterBackgroundWorker, DoWorkEventArgs e)
        {
            if (rasterBackgroundWorker.CancellationPending)
            {
                e.Cancel = true;
                return "Pause...";
            }

            var parameter = ((int index, string theme, TileType type, int typeCode, bool update, bool light, XElement metadata, int srid, short tileMatrix))e.Argument;

            //创建或获取森林对象
            var oneForest = new GeositeXmlPush();
            var oneForestResult = oneForest.Forest(
                id: _clusterUser.forest, //森林编号采用GeositeServer系统管理员指定的【集群编号（小于0的整数）】
                name: _clusterUser.name  //森林名称采用GeositeServer系统管理员指定的【集群用户名】
                                        //, timestamp: $"{DateTime.Now: yyyyMMdd, HHmmss}" //默认按当前时间创建时间戳 
            );
            if (!oneForestResult.Success)
                return oneForestResult.Message;

            var tabIndex = parameter.index;
            var themeMetadataX = parameter.metadata;

            /*  
            文档树状态码status，继承自[forest.status]，含义如下
            持久化	暗数据	完整性	含义
            ======	======	======	==============================================================
            0		0		0		默认值0：非持久化数据（参与对等）		明数据		无值或失败
            0		0		1		指定值1：非持久化数据（参与对等）		明数据		正常
            0		1		0		指定值2：非持久化数据（参与对等）		暗数据		失败
            0		1		1		指定值3：非持久化数据（参与对等）		暗数据		正常
            1		0		0		指定值4：持久化数据（不参与后续对等）	明数据		失败
            1		0		1		指定值5：持久化数据（不参与后续对等）	明数据		正常
            1		1		0		指定值6：持久化数据（不参与后续对等）	暗数据		失败
            1		1		1		指定值7：持久化数据（不参与后续对等）	暗数据		正常
            */
            var status = (short)(parameter.light ? 4 : 6);

            // 瓦片存储约定：
            // 1）为便于高速提取瓦片，采用一个member存储全部瓦片方案，或将某专题不同缩放级的全部瓦片所属的member个数不超过24个（每个member可对应一个缩放级）
            // 2）瓦片层的元数据信息应在member父级（最近layer）的property中表述，以便适配OGC-GML模板
            // 3）森林名称：采用GeositeServer系统管理员指定的【集群用户名】，也就是说，一个用户对应一个群（一片森林）
            // 4）文档树名称：采用界面提供的【专题名】
            // 5）分类树：默认的逐级分类名称采用瓦片路径
            // 6）叶子名称：采用界面提供的【专题名】，与文档树名称保持一致的好处是便于识别，同时意味着一棵树、一片叶子将对应一个专题

            var forest = _clusterUser.forest;

            string[] themeNames;
            string[] rasterSourceFiles = null;
            if (tabIndex == 2)
            {
                rasterSourceFiles = Regex.Split(ModelOpenTextBox.Text.Trim(), @"[\s]*[|][\s]*").Where(s => !string.IsNullOrWhiteSpace(s)).ToArray();
                var rasterSourceFileCount = rasterSourceFiles.Length; // >= 0 
                //针对平铺栅格类型，可能有多个专题名称且由【|】分隔
                themeNames = Regex.Split(parameter.theme, @"[\s]*[|][\s]*").Where(s => !string.IsNullOrWhiteSpace(s)).ToArray();
                var themeNameCount = themeNames.Length; // >= 0 
                if (rasterSourceFileCount != themeNameCount)
                    return "The number of files is inconsistent with the number of themes";
            }
            else
                themeNames = new[] { parameter.theme };

            long total = 0;
            for (var pointer = 0; pointer < themeNames.Length; pointer++)
            {
                //不允许出现特殊字符，也不能有小数点【.】，因为小数点在GML元素名中有特殊含义
                var themeName = themeNames[pointer];

                try
                {
                    var xmlNodeName = new XElement(themeName);
                    if (xmlNodeName.Name.LocalName != themeName || Regex.IsMatch(themeName, @"[\.]+", RegexOptions.IgnoreCase))
                    {
                        throw new Exception($"[{themeName}] does not conform to XML naming rules");
                    }
                }
                catch(Exception errorXml)
                {
                    return errorXml.Message;
                }

                //先大致检测是否存在指定的树记录，重点甄别类型码是否合适
                var oldTreeType = PostgreSqlHelper.Scalar(
                    "SELECT type FROM tree WHERE forest = @forest AND name ILIKE @name::text LIMIT 1;",
                    new Dictionary<string, object>
                    {
                    {"forest", forest},
                    {"name", themeName}
                    }
                );
                if (oldTreeType != null)
                {
                    //文档树要素类型码：
                    //0：非空间数据【默认】、
                    //1：Point点、
                    //2：Line线、
                    //3：Polygon面、
                    //4：Image地理贴图、

                    //10000：Tile栅格  金字塔瓦片     wms服务类型     [epsg:0       无投影瓦片]
                    //10001：Tile栅格  金字塔瓦片     wms服务类型     [epsg:4326    地理坐标系瓦片]
                    //10002：Tile栅格  金字塔瓦片     wms服务类型     [epsg:3857    球体墨卡托瓦片]
                    //11000：Tile栅格  金字塔瓦片     wmts服务类型    [epsg:0       无投影瓦片]
                    //11001：Tile栅格  金字塔瓦片     wmts服务类型    [epsg:4326    地理坐标系瓦片]
                    //11002：Tile栅格  金字塔瓦片     wmts服务类型    [epsg:3857    球体墨卡托瓦片]
                    //12000：Tile栅格  平铺式瓦片     wps服务类型     [epsg:0       无投影瓦片]
                    //12001：Tile栅格  平铺式瓦片     wps服务类型     [epsg:4326    地理坐标系瓦片]
                    //12002：Tile栅格  平铺式瓦片     wps服务类型     [epsg:3857    球体墨卡托瓦片]

                    if (oldTreeType.GetType().Name != "DBNull")
                    {
                        var typeArray = (int[])oldTreeType;
                        var tileArray = typeArray.Where(t => t is >= 10000 and <= 12002).Select(t => t);
                        if (typeArray.Length != tileArray.Count())
                            return $"[{themeName}] is already used for vector theme";
                    }
                }

                //声明创建叶子子表所需的关键参量：
                int tree;
                string[] routeName;
                int[] routeId;
                long leaf; //之后将大于等于0
                int typeCode; //非空间数据【默认】
                XElement propertyX; //叶子属性

                var oldTree = PostgreSqlHelper.Scalar(
                    //-------------------------------------------
                    "WITH t1 AS" +
                    "(" +
                    "    SELECT branch.tree, branch.routename, branch.routeid, leaf.id, leaf.type FROM leaf," +
                    "    (" +
                    "        SELECT tree, array_agg(name) AS routename, array_agg(id) AS routeid FROM" +
                    "        (" +
                    "            SELECT * FROM branch WHERE tree IN" +
                    "            (" +
                    "                SELECT id FROM tree WHERE forest = @forest AND name ILIKE @name::text LIMIT 1" +
                    "            ) ORDER BY tree, level" +
                    "        ) AS branchtable" +
                    "        GROUP BY tree" +
                    "    ) AS branch" +
                    "    WHERE leaf.name ILIKE @name::text AND leaf.branch = branch.routeid[array_length(branch.routeid, 1)] LIMIT 1" +
                    ")" +
                    //-------------------------------------------
                    "SELECT (t1.*, tt.description) FROM t1," +
                    "(" +
                    "    SELECT t2.leaf, array_agg((t2.name,t2.attribute,t2.level,t2.sequence,t2.parent,t2.flag,t2.type,t2.content)) AS description" +
                    "    FROM leaf_description AS t2, t1" +
                    "    WHERE t1.id = t2.leaf" +
                    "    GROUP BY t2.leaf" +
                    ") AS tt " +
                    "WHERE t1.id = tt.leaf LIMIT 1;",
                    new Dictionary<string, object>
                    {
                        {"forest", forest},
                        {"name", themeName}
                    }
                );

                /*  瓦片树基本信息模板：
                    <FeatureCollection timeStamp="2021-07-27T08:26:02"> 
                        <name>xxx</name>
                        <layer>
                            <name>yyy</name>
                            <property remarks="注意：瓦片层的元数据信息应在member父级（最近容器）的property中表述">
                                <minZoom remarks="最小缩放级，默认0">0</minZoom>
                                <maxZoom remarks="最大缩放级，默认18" >18</maxZoom>
                                <tileSize remarks="瓦片像素尺寸，默认256">256</tileSize>
                                <boundary remarks="边框范围">
                                    <north remarks="上北，比如：85.0511287798066">85.0511287798066</north>
                                    <south remarks="下南，比如：-85.0511287798066">-85.0511287798066</south>
                                    <west remarks="左西，比如：-180">-180.0</west>
                                    <east remarks="右东,比如：180">180.0</east>
                                </boundary>
                            </property>
                            <member type="Tile" timeStamp="2021-07-27T08:26:02">
                                <name>yyy</name>
                                <property>
                                    <srid>3857</srid>
                                    <bands remarks="波段/通道数">4</bands>
                                </property>
                            </member>
                        </layer>
                    </FeatureCollection>                                                     
                */
                //下列代码将针对不同情况获取用于创建叶子子表的关键参量
                if (oldTree != null)
                {
                    var oldTreeResult = (object[])oldTree;
                    //oldTree包括6项：tree routename routeid id type description
                    //其中，description包括8项：name attribute level sequence parent flag type content
                    tree = (int)oldTreeResult[0]; //tree
                    routeName = (string[])oldTreeResult[1]; //routename
                    routeId = (int[])oldTreeResult[2]; //routeid
                    leaf = (long)oldTreeResult[3]; //id
                    typeCode = (int)oldTreeResult[4]; //type
                    propertyX = GeositeXmlFormatting.TableToXml //将扁平化后的二维元组列表结构转换为深度嵌套的树状结构，实现二维关系模型向树状数据结构映射
                    (
                        oldTreeResult[5]
                    );
                }
                else
                {
                    var sequenceMax =
                        PostgreSqlHelper.Scalar(
                            "SELECT sequence FROM tree WHERE forest = @forest ORDER BY sequence DESC LIMIT 1;",
                            new Dictionary<string, object>
                            {
                                {"forest", forest}
                            }
                        );
                    //文档树序号--[0,已有的最大值+1]
                    var sequence = sequenceMax == null ? 0 : 1 + int.Parse($"{sequenceMax}");

                    string getTreePathString = null; 
                    XElement description = null;
                    var canDo = true;
                    if (!_noPromptLayersBuilder)
                    {
                        LayersBuilder getTreeLayers;
                        switch (tabIndex)
                        {
                            case 0:
                                getTreeLayers = new LayersBuilder(new DirectoryInfo(localTileFolder.Text).FullName);
                                break;
                            case 1:
                                getTreeLayers = new LayersBuilder("Untitled"); //暂将分类路由信息默认为：Untitled
                                break;
                            case 2:
                                getTreeLayers = new LayersBuilder(new FileInfo(rasterSourceFiles[pointer]).FullName);
                                break;
                            default:
                                return "This option is not supported";
                        }
                        getTreeLayers.ShowDialog();
                        if (getTreeLayers.Ok)
                        {
                            getTreePathString = getTreeLayers.TreePathString;
                            description = getTreeLayers.Description;
                            _noPromptLayersBuilder = getTreeLayers.DonotPrompt;
                        }
                        else
                            canDo = false;
                    }
                    else
                    {
                        switch (tabIndex)
                        {
                            case 0:
                                getTreePathString = ConsoleIO.FilePathToXPath(new DirectoryInfo(localTileFolder.Text).FullName);
                                break;
                            case 1:
                                getTreePathString = "Untitled"; //暂将分类路由信息默认为：Untitled
                                break;
                            case 2:
                                getTreePathString = ConsoleIO.FilePathToXPath(new FileInfo(rasterSourceFiles[pointer]).FullName);
                                break;
                            default:
                                return "This option is not supported";
                        }
                    }
                    if (canDo)
                    {
                        string treeUri;
                        DateTime treeLastWriteTime;
                        switch (tabIndex)
                        {
                            case 0:
                                var getFolder = new DirectoryInfo(localTileFolder.Text);
                                treeLastWriteTime = getFolder.LastWriteTime;
                                treeUri = getFolder.FullName;
                                break;
                            case 1:
                                treeUri = tilewebapi.Text;
                                treeLastWriteTime = DateTime.Now;
                                break;
                            case 2:
                                var fileInfo = new FileInfo(rasterSourceFiles[pointer]);
                                treeUri = fileInfo.FullName;
                                treeLastWriteTime = fileInfo.LastWriteTime;
                                themeMetadataX = GeositeTilePush.GetRasterMetaData(treeUri, rasterTileSize.Text);
                                break;
                            default:
                                return "This option is not supported";
                        }

                        var treePathString = getTreePathString; // 分类层级 由正斜杠【/】分隔
                        var treeDescription = description; // 分类树的属性
                        var lastWriteTime = Regex.Split(
                            $"{treeLastWriteTime: yyyyMMdd,HHmmss}",
                            "[,]",
                            RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace | RegexOptions.Singleline | RegexOptions.Multiline
                        );
                        int.TryParse(lastWriteTime[0], out var yyyyMMdd);
                        int.TryParse(lastWriteTime[1], out var HHmmss);

                        var timestamp =
                            $"{forest},{sequence},{yyyyMMdd},{HHmmss}"; //[森林序号,文档序号,年月日（yyyyMMdd）,时分秒（HHmmss）]

                        //构造一颗含有分类层级的 GeositeXML 文档树对象，以便启用【推模式】类
                        var treeXml = new XElement(
                            "FeatureCollection",
                            new XAttribute("timeStamp", DateTime.Now.ToString("s")), //文档树时间戳以当前时间为准
                            new XElement("name", themeName) //文档树名称采用UI界面提供的专题名
                        );
                        if (treeDescription != null)
                            treeXml.Add(new XElement("property", treeDescription.Elements().Select(x => x)));

                        XElement layersX = null;
                        routeName = Regex.Split(treePathString, "[/]",
                            RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Multiline);
                        for (var i = routeName.Length - 1; i >= 0; i--)
                            layersX = layersX == null //最末层
                                ? new XElement(
                                    "layer",
                                    new XElement("name", routeName[i]),
                                    themeMetadataX, //将元数据xml存入最末层
                                    new XElement(
                                        "member", //在最末层放置一片叶子 parameter
                                        new XAttribute("type", "Tile"),
                                        new XAttribute("typeCode", parameter.typeCode),
                                        new XAttribute("timeStamp", treeLastWriteTime.ToString("s")), //叶子时间戳以瓦片目录创建时间为准
                                        new XElement("name", themeName),
                                        new XElement("property",
                                            new XElement("srid", parameter.srid),
                                            //针对WMS服务路径，若存在子域占位符{s}，必须携带子域替换符，以便实施负载均衡策略
                                            tabIndex == 1 && !string.IsNullOrWhiteSpace(subdomainsBox.Text)
                                                ? new XElement("subdomains", subdomainsBox.Text)
                                                : null
                                        )
                                    )
                                )
                                : new XElement(
                                    "layer",
                                    new XElement("name", routeName[i]),
                                    layersX
                                );
                        treeXml.Add(layersX);

                        //创建【树】
                        var oneTree = oneForest.Tree(
                            timestamp,
                            treeXml,
                            treeUri,
                            status,
                            parameter.typeCode
                        );
                        if (oneTree.Success)
                        {
                            //this.
                                Invoke(
                                new Action(
                                    () =>
                                    {
                                        _clusterDate.Reset(); //刷新界面---专题列表
                                    }
                                )
                            );

                            tree = oneTree.Id;
                            var leafX = treeXml.DescendantsAndSelf("member").FirstOrDefault();

                            //创建【枝干】
                            var oneBranch = oneForest.Branch(
                                forest,
                                sequence,
                                tree,
                                leafX,
                                treeXml
                            );
                            if (oneBranch.Success)
                            {
                                var routeArray = oneBranch.Route;
                                //枝干id路由（route）数组的前三个元素分别是【节点森林（群）编号，文档树序号，文档树标识码】，后面依次为枝干id序列
                                routeId = new ArraySegment<int>(routeArray, 3, routeArray.Length - 3).ToArray();
                                //创建【叶子】瓦片存储约定策略：本颗树所属的全部瓦片均存入一片叶子
                                var oneLeaf = oneForest.Leaf(routeArray, leafX);
                                if (oneLeaf.Success)
                                {
                                    leaf = oneLeaf.Id; //大于等于0
                                    propertyX = oneLeaf.Property; //可能为null
                                    typeCode = oneLeaf.Type;
                                }
                                else
                                    return oneLeaf.Message;
                            }
                            else
                                return oneBranch.Message;
                        }
                        else
                            return oneTree.Message;
                    }
                    else
                        return "Abort task";
                }

                //将在单片叶子里，推送指定专题所属的全部瓦片
                var geositeTilePush = new GeositeTilePush(
                    oneForest,
                    tree,
                    routeName,
                    routeId,
                    leaf,
                    typeCode,
                    propertyX,
                    parameter.update
                );
                var localI = pointer + 1;
                geositeTilePush.onGeositeEvent += delegate (object _, GeositeEventArgs thisEvent)
                {
                    object userStatus = !string.IsNullOrWhiteSpace(thisEvent.message)
                        ? themeNames.Length > 1
                            ? $"[{localI}/{themeNames.Length}] {thisEvent.message}" 
                            : thisEvent.message
                        : null;

                    rasterWorker.ReportProgress(thisEvent.progress ?? -1, userStatus ?? string.Empty);
                };

                switch (tabIndex)
                {
                    case 0: //本地文件夹（金字塔存储）
                        try
                        {
                            var result0 = geositeTilePush.TilePush(
                                0,
                                localTileFolder.Text,
                                parameter.type,
                                parameter.tileMatrix,
                                EPSG4326.Checked
                            );
                            total += result0.total;
                            //var metaDataX= result0.metaDataX; //可获取元数据xml
                        }
                        catch (Exception error)
                        {
                            return error.Message;
                        }

                        break;
                    case 1: //远程wms服务地址（金字塔存储） 
                        try
                        {
                            var result1 = geositeTilePush.TilePush(
                                1,
                                tilewebapi.Text,
                                parameter.type,
                                parameter.tileMatrix,
                                EPSG4326.Checked,
                                (wmtsNorth.Text, wmtsSouth.Text, wmtsWest.Text, wmtsEast.Text), 
                                wmtsSpider.Checked,
                                //tabIndex == 1 && 
                                !string.IsNullOrWhiteSpace(subdomainsBox.Text)
                                    ? subdomainsBox.Text
                                    : null
                            );
                            total += result1.total;
                            //var metaDataX= result1.metaDataX; //可获取元数据xml
                        }
                        catch (Exception error)
                        {
                            return error.Message;
                        }

                        break;
                    case 2: //平铺式瓦片
                        try
                        {
                            var result2 = geositeTilePush.TilePush(
                                2,
                                rasterSourceFiles[pointer],
                                TileType.Standard, //强制为 OGC，以便识别 
                                -1, //平铺式瓦片的【z】取【-1】
                                true, //平铺式瓦片的投影系暂支持4326
                                (rasterTileSize.Text, rasterTileSize.Text, nodatabox.Text, null) //注意：暂将宽度和高度以及无值信息按边框参数传递
                            );
                            total += result2.total;
                            //var metaDataX= result2.metaDataX; //可获取元数据xml
                        }
                        catch (Exception error)
                        {
                            return error.Message;
                        }

                        break;
                }

                oneForest.Tree(
                    enclosure:
                    (
                        tree,
                        new List<int>() {typeCode}, 
                        true
                    )
                ); //向树记录写入完整性标志以及类型数组
                _clusterDate.Reset(); //刷新界面---专题列表
            }
            return total > 0 ? $"Pushed {total} tile" + (total > 1 ? "s" : "") : "No tile pushed";
        }

        private void RasterWorkProgress(object sender, ProgressChangedEventArgs e)
        {
            //e.code 状态码（0/null=预处理阶段；1=正在处理阶段；200=收尾阶段；400=异常信息）
            //e.ProgressPercentage 进度值（介于0~100之间，仅当code=1时有效）
            var userState = (string)e.UserState;
            var progressPercentage = e.ProgressPercentage;
            var pv = statusProgress.Value = progressPercentage is >= 0 and <= 100 ? progressPercentage : 0;
            statusText.Text = userState;
            //实时刷新界面进度杆会明显降低执行速度！
            //下面采取每10个要素刷新一次 
            if (pv % 10 == 0)
                statusBar.Refresh();
        }

        private void RasterWorkCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            statusProgress.Visible = false;

            if (e.Error != null)
                MessageBox.Show(e.Error.Message, @"Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            else if (e.Cancelled)
                statusText.Text = @"Suspended!";
            else if (e.Result != null)
                statusText.Text = (string)e.Result;

            var serverUrl = GeositeServerUrl.Text?.Trim();
            var serverUser = GeositeServerUser.Text?.Trim();
            var serverPassword = GeositeServerPassword.Text?.Trim();
            UpdateDatabaseSize(serverUrl, serverUser, serverPassword);

            PostgresRun.Enabled = dataCards.SelectedIndex == 0 || vectorFilePool.Rows.Count > 0;

            _loading.Run(false);
            ogcCard.Enabled = true;
        }
    }
}
