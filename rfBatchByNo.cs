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
    public partial class rfBatchByNo : Telerik.WinControls.UI.RadForm
    {
        public string batchNo = "";
        public rfBatchByNo()
        {
            InitializeComponent();
        }

        private void rbDone_Click(object sender, EventArgs e)
        {
            doneSlashEnter();
        }

        private void rtbBatch_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Return)
            {
                doneSlashEnter();
            }
        }

        private void doneSlashEnter()
        {
            batchNo = rtbBatch.Text;
            this.Close();
        }
    }
}
