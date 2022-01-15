using System;
using System.Windows.Forms;
using System.Xml.Linq;
using Newtonsoft.Json;

namespace Geosite
{
    public partial class MetaData : Form
    {
        public bool Ok;
        public XElement MetaDataX;
        public bool DonotPrompt;
        

        public MetaData(string metaDataString = null)
        {
            InitializeComponent();
            themeMetadata.Text = metaDataString ?? "";
        }

        private void OKbutton_Click(object sender, EventArgs e)
        {
            string error = null;
            var themeMetadataText = themeMetadata.Text;
            if (themeMetadataText.Length > 0)
            {
                try
                {
                    MetaDataX = XElement.Parse(themeMetadataText);
                }
                catch(Exception xmlError)
                {
                    error = xmlError.Message;
                    try
                    {
                        var x = JsonConvert.DeserializeXNode(themeMetadataText, "property")?.ToString();
                        if (x != null)
                        {
                            MetaDataX = XElement.Parse(x);
                            error = null;
                        }
                    }
                    catch (Exception jsonError)
                    {
                        error = jsonError.Message;
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

            if (error == null)
            {
                Ok = true;
                Close();
            }
            else
            {
                Info.Text = error;
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
