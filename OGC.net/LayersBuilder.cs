using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.Xml.Linq;

namespace Geosite
{
    public partial class LayersBuilder : Form
    {
        public string TreePathString;

        public List<XElement> Description;

        public bool OK;

        public LayersBuilder(string TreePathDefault = null)
        {
            InitializeComponent();
            if (string.IsNullOrWhiteSpace(TreePathDefault))
            {
                treePathTab.TabPages[0].Enabled = false;
                treePathTab.SelectedIndex = 1;
            }
            else
            {
                treePathTab.SelectedIndex = 0;
                //尽可能从文件夹或文件路径中提取分类树
                if(Regex.IsMatch(TreePathDefault, @"(^([a-z]+):)([\s\S]*?)([\.][\s\S]*)?$", RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Multiline))
                {
                    TreePathDefault = string.Join("/", Regex.Split(Regex.Replace(
                            TreePathDefault, //D:\zk\result\error\234.txt  D:\zk\result\error
                            @"(^([a-z]+):)([\s\S]*?)([\.][\s\S]*)?$",
                            "$3", // \zk\result\error\234  \zk\result\error
                            RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Multiline
                        ).Trim('\\','/'), // zk\result\error\234  zk\result\error
                        @"[/\\]+", // zk/result/error/234  zk/result/error
                        RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Multiline));
                }
                treePathBox.Text = TreePathDefault;
                treePathBox.Focus();
            }
        }
        
        private void OKbutton_Click(object sender, EventArgs e)
        {
            var canExit = true;

            TreePathString = treePathBox.Text.Trim();
            if (!string.IsNullOrWhiteSpace(TreePathString))
            {
                var levels = new List<string>();
                foreach (var thisLevel in Regex.Split(
                        TreePathString,
                        @"[/\\]+", //约定为正斜杠【/】或者反斜杠【\】分隔
                        RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Multiline)
                    .Select(level => level.Trim())
                    .Where(thisLevel => thisLevel.Length > 0)
                )
                {
                    try
                    {
                        levels.Add(new XElement(thisLevel).Name.LocalName);
                    }
                    catch (Exception error)
                    {
                        canExit = false;
                        tipsBox.Text = error.Message;
                        break;
                    }
                }

                if (levels.Count == 0)
                {
                    canExit = false;
                    tipsBox.Text = @"Incorrect input";
                }
                else
                {
                    TreePathString = treePathBox.Text = string.Join("/", levels);
                }
            }

            var XElementList = new List<XElement>();
            if (!string.IsNullOrWhiteSpace(downloadBox.Text))
                XElementList.Add(new XElement("download", downloadBox.Text.Trim()));
            if (!string.IsNullOrWhiteSpace(legendBox.Text))
                XElementList.Add(new XElement("legend", legendBox.Text.Trim()));
            if (!string.IsNullOrWhiteSpace(thumbnailBox.Text))
                XElementList.Add(new XElement("thumbnail", thumbnailBox.Text.Trim()));
            if (!string.IsNullOrWhiteSpace(authorBox.Text))
                XElementList.Add(new XElement("author", authorBox.Text.Trim()));
            if (!string.IsNullOrWhiteSpace(contactBox.Text))
                XElementList.Add(new XElement("contact", contactBox.Text.Trim()));
            if (!string.IsNullOrWhiteSpace(keywordBox.Text))
                XElementList.Add(new XElement("keyword", keywordBox.Text.Trim()));
            if (!string.IsNullOrWhiteSpace(abstractBox.Text))
                XElementList.Add(new XElement("abstract", abstractBox.Text.Trim()));
            if (!string.IsNullOrWhiteSpace(remarksBox.Text))
                XElementList.Add(new XElement("remarks", remarksBox.Text.Trim()));

            if (XElementList.Count > 0)
                Description = XElementList;

            if (canExit)
            {
                OK = true;
                Close();
            } else 
                OK = false;
        }
    }
}
