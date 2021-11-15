using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using Geosite.GeositeServer.PostgreSQL;

namespace Geosite
{
    class DataGrid
    {
        private readonly DataGridView _dataGridView;

        private readonly Button _firstPage;
        private readonly Button _previousPage;
        private readonly Button _nextPage;
        private readonly Button _lastPage;
        private readonly TextBox _pageBox;
        private readonly Button _deleteTree;

        private readonly int _forest; //森林编号
        private int _totel; //总记录数
        private readonly int _limit; //每页多个条记录
        private int _page; //当前页码

        public DataGrid(
            DataGridView dataGridView,
            Button firstPage, Button previousPage, Button nextPage, Button lastPage,
            TextBox pageBox,
            Button deleteTree,
            int forest,
            int page = -1,
            int limit = 10
            )
        {
            _dataGridView = dataGridView;
            _firstPage = firstPage;
            _previousPage = previousPage;
            _nextPage = nextPage;
            _lastPage = lastPage;
            _pageBox = pageBox;
            _deleteTree = deleteTree;

            _forest = forest; // = 0; 
            _page = page;
            _limit = limit <= 0 ? 10 : limit;

            Reset();
        }

        public void Reset()
        {
            var result = PostgreSqlHelper.Scalar(
                "SELECT COUNT(*) FROM tree WHERE forest = @forest;",
                new Dictionary<string, object>
                {
                    { "forest", _forest }
                }
            );

            int.TryParse($"{result}", out _totel);
            
            Show(_page);
        }

        private void Show(int page = 0)
        {
            _page = page;

            _dataGridView.Invoke(
                new Action(
                    () =>
                    {
                        _dataGridView?.Rows.Clear();
                    }
                )
            );

            var pages = (int)Math.Ceiling(1.0 * _totel / _limit);
            if (pages == 0) 
                pages = 1;
            if (_page >= pages)
                _page = pages - 1;
            else
            {
                if (_page < 0)
                    _page = 0;
            }
            _pageBox.Invoke(
                new Action(
                    () =>
                    {
                        _pageBox.Text = $@"{_page + 1} / {pages}";
                    }
                )
            );
            var offset = _page * _limit;
            var trees = PostgreSqlHelper.XElementReader(
                "SELECT id, name, uri, timestamp, type, status FROM tree WHERE forest = @forest ORDER BY id OFFSET @offset LIMIT @limit;",
                new Dictionary<string, object>
                {
                    {"forest", _forest},
                    {"offset", offset},
                    {"limit", _limit}
                }
            );
            if (trees != null)
            {
                var rows = trees.Elements("row");
                /*
                <table>
                  <row>
                    <id>1</id>
                    <name>bound_l_www_1</name>
                    <uri>http://172.29.240.1/0.xml</uri>
                    <timestamp>0,0,20210714,41031</timestamp>
                    <type>2</type>
                    <status>1</status>
                  </row>
                  <row>
                    <id>2</id>
                    <name>res_P_1</name>
                    <uri>http://172.29.240.1/1.xml</uri>
                    <timestamp>0,1,20210714,41201</timestamp>
                    <type>1</type>
                    <status>1</status>
                  </row>
                </table>                 
                 */

                foreach (var row in rows)
                {
                    var id = row.Element("id")?.Value;
                    var name = row.Element("name")?.Value.Trim(); 
                    var uri = row.Element("uri")?.Value;
                    var timestamp = row.Element("timestamp")?.Value;
                    var type = row.Element("type")?.Value ?? "1";
                    var status = row.Element("status")?.Value;
                    // id：
                    // name：文档树根节点简要名称，通常是入库文件基本名
                    // uri：文档树数据来源（存放路径及文件名）
                    // timestamp：文档树编码印章，采用[节点森林序号,文档树序号,年月日（yyyyMMdd）,时分秒（HHmmss）]四元整型数组编码方式
                    // type：文档树要素类型码
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
                    // status：文档树状态码（介于0～7之间），继承自[forest.status]

                    var typeArray = Regex.Split(type, @"[\s,]+",
                        RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Multiline);

                    Bitmap typeBitmap;
                    if (typeArray.Length == 1)
                    {
                        switch (typeArray[0])
                        {
                            case "1":
                                typeBitmap = Properties.Resources.point;
                                break;
                            case "2":
                                typeBitmap = Properties.Resources.line;
                                break;
                            case "3":
                                typeBitmap = Properties.Resources.polygon;
                                break;
                            case "4":
                                typeBitmap = Properties.Resources.image;
                                break;
                            case "10000":
                            case "10001":
                            case "10002":
                                typeBitmap = Properties.Resources.wms;
                                break;
                            case "11000":
                            case "11001":
                            case "11002":
                                typeBitmap = Properties.Resources.wmts;
                                break;
                            case "12000":
                            case "12001":
                            case "12002":
                                typeBitmap = Properties.Resources.rastertile;
                                break;
                            default:
                                typeBitmap = Properties.Resources.hybrid;
                                break;
                        }
                    }
                    else
                        typeBitmap = Properties.Resources.hybrid;

                    if (_dataGridView != null)
                    {
                        _dataGridView.Invoke(
                            new Action(
                                () =>
                                {
                                    var index = _dataGridView.Rows.Add(name, typeBitmap);

                                    _dataGridView.Rows[index].Cells[0].ToolTipText = uri;
                                    _dataGridView.Rows[index].Cells[1].ToolTipText =
                                        $"{id}\b{timestamp}\b{status}"; //Environment.NewLine
                                }
                            )
                        );
                    }
                }
            }

            _dataGridView.Invoke(
                new Action(
                    () =>
                    {
                        _deleteTree.Enabled = _dataGridView?.SelectedRows.Count > 0;
                    }
                )
            );

            if (pages == 1)
            {
                _firstPage.Invoke(
                    new Action(
                        () =>
                        {
                            _firstPage.Enabled =
                                _previousPage.Enabled =
                                    _nextPage.Enabled =
                                        _lastPage.Enabled = false;
                        }
                    )
                );
              
            }
            else
            {
                if (_page == 0)
                {
                    _firstPage.Invoke(
                        new Action(
                            () =>
                            {
                                _firstPage.Enabled =
                                    _previousPage.Enabled = false;
                                _nextPage.Enabled =
                                    _lastPage.Enabled = true;
                            }
                        )
                    );
                  
                }
                else
                {
                    if (_page == pages - 1)
                    {
                        _firstPage.Invoke(
                            new Action(
                                () =>
                                {
                                    _firstPage.Enabled =
                                        _previousPage.Enabled = true;
                                    _nextPage.Enabled =
                                        _lastPage.Enabled = false;
                                }
                            )
                        );
                       
                    }
                    else
                    {
                        _firstPage.Invoke(
                            new Action(
                                () =>
                                {
                                    _firstPage.Enabled =
                                        _previousPage.Enabled =
                                            _nextPage.Enabled =
                                                _lastPage.Enabled = true;
                                }
                            )
                        );
                       
                    }
                }
            }
        }

        public void First()
        {
            if (_page != 0)
                Show();
        }

        public void Previous()
        {
            if (_page > 0)
                Show(_page - 1);
        }

        public void Next()
        {
            var pages = (int)Math.Ceiling(1.0 * _totel / _limit);
            if (_page < pages - 1)
                Show(_page + 1);
        }

        public void Last()
        {
            var pages = (int)Math.Ceiling(1.0 * _totel / _limit);
            if (_page != pages - 1)
                Show(pages - 1);
        }
    }
}
