using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.Xml.Linq;
using Geosite.Messager;

namespace Geosite
{
    public partial class LayersBuilder : Form
    {
        public string TreePathString;

        public XElement Description;

        public bool Ok;

        public bool DonotPrompt;

        public LayersBuilder(string treePathDefault = null)
        {
            InitializeComponent();
            if (string.IsNullOrWhiteSpace(treePathDefault))
            {
                treePathTab.TabPages[0].Enabled = false;
                treePathTab.SelectedIndex = 1;
            }
            else
            {
                treePathTab.SelectedIndex = 0;
                //尽可能从文件夹或文件路径中提取分类树
                treePathBox.Text = ConsoleIO.FilePathToXPath(treePathDefault);
                treePathBox.Focus();
            }
        }
        
        private void OKbutton_Click(object sender, EventArgs e)
        {
            var canExit = true;

            TreePathString = treePathBox.Text.Trim();
            if (!string.IsNullOrWhiteSpace(TreePathString))
            {
                var layers = new List<string>();
                foreach (var layer in Regex.Split(
                        TreePathString,
                        @"[/\\]+", //约定为正斜杠【/】或者反斜杠【\】分隔，尤其不能出现小数点【.】，因为在后期服务中，小数点有特殊含义！
                        RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Multiline)
                    .Select(layer => layer.Trim())
                    .Where(layer => layer.Length > 0)
                )
                {
                    try
                    {
                        //GML-layer名称需符合xml元素命名规则，至少不能出现特殊字符、小数点、括号、纯数字 ...
                        layers.Add(new XElement(layer).Name.LocalName);
                    }
                    catch (Exception error)
                    {
                        canExit = false;
                        tipsBox.Text = error.Message;
                        break;
                    }
                }

                if (layers.Count == 0)
                {
                    canExit = false;
                    tipsBox.Text = @"Incorrect input";
                }
                else
                {
                    TreePathString = string.Join("/", layers);
                }
            }

            if (!string.IsNullOrWhiteSpace(downloadBox.Text))
            {
                Description ??= new XElement("property");
                Description.Add(new XElement("download", downloadBox.Text.Trim()));
            }

            if (!string.IsNullOrWhiteSpace(legendBox.Text))
            {
                Description ??= new XElement("property");
                Description.Add(new XElement("legend", legendBox.Text.Trim()));
            }

            if (!string.IsNullOrWhiteSpace(thumbnailBox.Text))
            {
                Description ??= new XElement("property");
                Description.Add(new XElement("thumbnail", thumbnailBox.Text.Trim()));
            }

            if (!string.IsNullOrWhiteSpace(authorBox.Text))
            {
                Description ??= new XElement("property");
                Description.Add(new XElement("author", authorBox.Text.Trim()));
            }

            if (!string.IsNullOrWhiteSpace(contactBox.Text))
            {
                Description ??= new XElement("property");
                Description.Add(new XElement("contact", contactBox.Text.Trim()));
            }

            if (!string.IsNullOrWhiteSpace(keywordBox.Text))
            {
                Description ??= new XElement("property");
                Description.Add(new XElement("keyword", keywordBox.Text.Trim()));
            }

            if (!string.IsNullOrWhiteSpace(abstractBox.Text))
            {
                Description ??= new XElement("property");
                Description.Add(new XElement("abstract", abstractBox.Text.Trim()));
            }

            if (!string.IsNullOrWhiteSpace(remarksBox.Text))
            {
                Description ??= new XElement("property");
                Description.Add(new XElement("remarks", remarksBox.Text.Trim()));
            }

            if (canExit)
            {
                Ok = true;
                Close();
            } else 
                Ok = false;
        }

        private void donotPrompt_CheckedChanged(object sender, EventArgs e)
        {
            DonotPrompt = donotPrompt.Checked;
        }

        private void treePathBox_TextChanged(object sender, EventArgs e)
        {
            tipsBox.Text = string.Empty;
        }
    }
}
