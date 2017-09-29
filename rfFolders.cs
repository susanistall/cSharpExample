using GeneralFunctions;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using Telerik.WinControls;

namespace ALS_Prep
{
    public partial class rfFolders : Telerik.WinControls.UI.RadForm
    {
        DataTable dt = new DataTable();
        public string FolderChoice = "";
        public rfFolders()
        {
            InitializeComponent();
            dt = BasicFunctions.GetData("Select Distinct Ordtask.Folderno from ordtask,preptasks,Folders where ordtask.ordno = preptasks.ordno and ordtask.testcode = preptasks.testcode and ordtask.Folderno = Folders.Folderno and preptasks.prepts='Need Prep' and preptasks.dept = 'KELSO' and preptasks.preprunno=-1 and folders.fldsts <> 'Draft' and APPRSTS = 'N/A' and folders.qcfolder = 'N' and ordtask.ts = 'Hold' and (ordtask.sp_Code <> 379 and ordtask.sp_Code <> 378) and preptasks.preptmname is not null and preptasks.sampweight IS NULL  Order by Ordtask.Folderno");
            rlvFolders.DataSource = dt;
            rlvFolders.DisplayMember = "FOLDERNO";
        }

        private void rlvFolders_DoubleClick(object sender, EventArgs e)
        {
            FolderChoice = rlvFolders.CurrentItem.Value.ToString();
            this.Close();
        }
    }
}
