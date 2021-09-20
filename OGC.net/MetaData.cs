using System;
using System.Windows.Forms;
using System.Xml.Linq;
using Newtonsoft.Json;

namespace Geosite
{
    public partial class MetaData : Form
    {
        public bool OK;
        public XElement MetaDataX;
        public bool DonotPrompt;
        

        public MetaData(string MetaDataString = null)
        {
            InitializeComponent();
            themeMetadata.Text = MetaDataString ?? "";
        }

        private void OKbutton_Click(object sender, EventArgs e)
        {
            string Error = null;
            var themeMetadataText = themeMetadata.Text;
            if (themeMetadataText.Length > 0)
            {
                try
                {
                    MetaDataX = XElement.Parse(themeMetadataText);
                }
                catch(Exception xmlError)
                {
                    Error = xmlError.Message;
                    try
                    {
                        var X = JsonConvert
                            .DeserializeXNode(themeMetadataText, "property")
                            ?.ToString();
                        if (X != null)
                        {
                            MetaDataX = XElement.Parse(X);
                            Error = null;
                        }
                    }
                    catch (Exception jsonError)
                    {
                        Error = jsonError.Message;
                    }
                }
                if (MetaDataX != null)
                {
                    if (MetaDataX.Name != "property") 
                        MetaDataX = new XElement("property", MetaDataX);
                }
            }
            else 
                MetaDataX = null;

            if (Error == null)
            {
                OK = true;
                Close();
            }
            else
            {
                Info.Text = Error;
                MetaDataX = null;
            }
        }

        private void themeMetadata_KeyPress(object sender, KeyPressEventArgs e)
        {
            //解决当TextBox控件在设置了MultiLine=True之后，Ctrl+A 无法全选的尴尬问题！
            if (e.KeyChar == '\x1')
                ((TextBox)sender).SelectAll();
        }

        private void donotPrompt_CheckedChanged(object sender, EventArgs e)
        {
            DonotPrompt = donotPrompt.Checked;
        }
    }
}
