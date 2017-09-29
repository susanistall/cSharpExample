using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using Telerik.WinControls;
using Telerik.WinControls.Data;
using Telerik.WinControls.Primitives;
using Telerik.WinControls.UI;
using Telerik.Reporting;
using System.Reflection;
using System.IO;
using System.Linq;

namespace ALS_Prep 
{
    public partial class PrintView : Telerik.WinControls.UI.RadForm
    {

        Telerik.Reporting.Processing.RenderingResult result;

        public PrintView(List<Test> lstT)
        {
            if (lstT.Count > 0)
            {
                InitializeComponent();
                this.rpvNav.CommandBarElement.Rows[0].Strips[0].ItemsLayout.Children[0].Visibility = Telerik.WinControls.ElementVisibility.Collapsed;
                CommandBarButton btn = new CommandBarButton();
                btn.Click += new EventHandler(btn_Click);
                btn.DrawText = false;
                btn.Image = (System.Drawing.Image)Properties.Resources.save;
                btn.ToolTipText = "Save PDF to a specified location";
                this.rpvNav.CommandBarElement.Rows[0].Strips[0].Items.Insert(0, btn);

                BatchReport report = new BatchReport();
                InstanceReportSource rptInstance = new InstanceReportSource();
                rptInstance.ReportDocument = report;

                Test t = new Test();
                t.PrepRun = lstT[0].PrepRun;
                t.DueDate = lstT.Where(te => te.DueDate != null).Select(te => te.DueDate).Min();

                report.DataSource = t;
                report.DocumentMapText = "Prep Benchsheet";


                Telerik.Reporting.Table tableItem = report.Items.Find("table1", true)[0] as Telerik.Reporting.Table;
                tableItem.DataSource = lstT;

                //Render pdf
                Telerik.Reporting.Processing.ReportProcessor reportProcessor = new Telerik.Reporting.Processing.ReportProcessor();
                System.Collections.Hashtable deviceInfo = new System.Collections.Hashtable();
                deviceInfo["FontEmbedding"] = "None";
                result = reportProcessor.RenderReport("PDF", rptInstance, deviceInfo);
                MemoryStream msPDF = new MemoryStream(result.DocumentBytes);
                rpvMain.ViewerMode = FixedDocumentViewerMode.TextSelection;
                rpvMain.LoadDocument(msPDF);
                //msPDF.Dispose();
            }


        }

        private void btn_Click(object sender, EventArgs e)
        {
            SaveFileDialog svPDF = new SaveFileDialog();
            svPDF.Filter = "pdf files (*.pdf)|*.pdf";
            svPDF.FilterIndex = 1;
            //svPDF.FileName = FinalReports.Folder + ".pdf";
            svPDF.RestoreDirectory = true;

            if (svPDF.ShowDialog() == DialogResult.OK)
            {
                using (FileStream fs = new FileStream(svPDF.FileName, FileMode.Create))
                {
                    fs.Write(result.DocumentBytes, 0, result.DocumentBytes.Length);
                }
            }
        }

        private void PrintView_Load(object sender, EventArgs e)
        {

        }

    }
}
