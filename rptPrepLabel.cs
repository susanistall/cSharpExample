namespace ALS_Prep
{
    using System;
    using System.ComponentModel;
    using System.Drawing;
    using System.Windows.Forms;
    using Telerik.Reporting;
    using Telerik.Reporting.Drawing;

    /// <summary>
    /// Summary description for rptSampleLabel.
    /// </summary>
    public partial class rptPrepLabel : Telerik.Reporting.Report
    {
        public rptPrepLabel(Test T)
        {
            //
            // Required for telerik Reporting designer support
            //
            InitializeComponent();

            string type ="";
            if (T.Type != null && T.Type != "N/A")
            {
                type = T.Type;
            }
            txtLine1.Value = T.SampleName + "." + T.Container + type; ;
            txtLine2.Value = T.TestName + ": " + T.Mass + T.Unit;
            txtDate.Value = DateTime.Now.ToString();
            //
            // TODO: Add any constructor code after InitializeComponent call
            //
        }
    }
}