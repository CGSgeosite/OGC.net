using System;
using System.Windows.Forms;

namespace Geosite
{
    public partial class FreeTextField : Form
    {
        public string CoordinateFieldName;
        public bool OK;

        public FreeTextField(string[] fieldNames)
        {
            InitializeComponent();
            foreach (var name in fieldNames) 
                CoordinateComboBox.Items.Add(name);
            CoordinateFieldName = CoordinateComboBox.Text =
            CoordinateComboBox.SelectedText = $"{CoordinateComboBox.Items[0]}";
        }

        private void OKbutton_Click(object sender, EventArgs e)
        {
            CoordinateFieldName = CoordinateComboBox.Text;
            OK = true;
            Close();
        }
    }
}
