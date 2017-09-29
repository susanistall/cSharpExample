using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using Telerik.WinControls;
using Telerik.WinControls.UI;

namespace ALS_Prep
{
    public partial class EditTestPrepComments : RadForm
    {
        Test test;
        Sample sample;
        public EditTestPrepComments(Sample s, Test t)
        {
            InitializeComponent();
            test = t;
            sample = s;
            if (s.ParentName == null || s.ParentName == "")
            {
                radLabel1.Text = s.SampleName;
            }
            else
            {
                radLabel1.Text = s.ParentName + " " + t.Type;
            }
            radLabel1.Text += ": ";
            radLabel2.Text = t.TestName;
            if (t.PCComments != null && t.PCComments != "")
            {
                radLabel3.Text = "PC Comments: " + t.PCComments;
            }
            else
            {
                radLabel3.Text = "PC Comments: none";
            }
            if (t.PrepComments != null && t.PrepComments != "")
            {
                rtbComments.Text = t.PrepComments;
            }
        }

        private void rbDone_Click(object sender, EventArgs e)
        {
           //if (rtbComments.Text != "")
            //{
                test.PrepComments = rtbComments.Text;
                this.Close();
            //}
        }
    }
}
