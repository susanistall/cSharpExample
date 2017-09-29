using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Telerik.WinControls;
using Telerik.WinControls.UI;
using System.Xml;
using System.Xml.Serialization;
using System.IO;
using Telerik.Reporting;
using System.Drawing.Printing;
using Telerik.Reporting.Processing;
using Login;
using GeneralFunctions;

namespace ALS_Prep
{
    public partial class rfMain : Telerik.WinControls.UI.RadForm
    {
        //int tryXML = 0;
        string scannedFolder = "";
        string scannedSample = "";
        string scannedBottle = "";
        DataTable dtData = new DataTable();
        DataTable dtBatch = new DataTable();
        DataTable dtRunNos = new DataTable();
        FolderInfo FolderSamps = new FolderInfo();
        List<Test> TestsNeedingBatch = new List<Test>();
        //List<Test> QCTest = new List<Test>(); //for dup,ms,dms XML serialization
        //List<Test> QCInBatch = new List<Test>(); //to force QC with associated parent into batch
        List<Test> sampsTest;
        Sample page2Samp;
        Test batchTest = new Test();
        FolderInfo ReqQCSamps = new FolderInfo();
        Sample Samp;// = new Sample();
        Test test;// = new Test();
        Test testPage2;
        string sLocation = "";
        string sFirstLetter = "";
        string fullFolder = "";
        string prevMass = "";
        string userLogin = "";
        string balance = "";

        public rfMain()
        {
            InitializeComponent();
            User.SetupUser();

            //Make Sure User info is complete
            if (User.Lab == "")
            {
                User.PopLab();
            }

            login Login = new login();
            Login.ShowDialog();
            
            userLogin = User.UserName + " " + Login.balance;
            balance = Login.balance;
            rlName.Text = "Not " + User.UserName + "?";
            rlName.ForeColor = Color.Blue;

            rtbSampleComments.Enabled = false;
            
            radLabel3.Hide();
            radLabel4.Hide();
            radLabel5.Hide();
            radLabel9.Hide();
            if (User.UserName != "SCHAPPELLE" && User.UserName != "SELDRIDGE" && User.UserName != "WNAGEL" && User.UserName != "JCAULFIELD")
            {
                rbOpenBatch.Hide();
            }
            sLocation = Environment.MachineName.ToString().Substring(2, 3);
            sFirstLetter = sLocation.Substring(0, 1);
            radLabel8.Text = "";
        }

        private void rtbFolder_Click(object sender, EventArgs e)
        {
            if (rtbFolder.Text == "Folder")
            {
                rtbFolder.Text = "";
            }
            rtbFolder.ForeColor = Color.Black;
            rtbFolder.Font = new Font("Segoe", 10, FontStyle.Bold | FontStyle.Regular);
        }

        private void rtbFolder_KeyDown(object sender, KeyEventArgs e)
        { 
            ReqQCSamps = new FolderInfo();
            if (e.KeyData == Keys.Return)
            {
                scannedFolder = "";
                string FolderNo;
                
                if (rtbFolder.Text.Trim() == "")
                {
                    //clear all info
                    rlvSamples.Items.Clear();
                    radLabel8.Text = "";
                    rgvTests.DataSource = null;
                    radLabel5.Text = "";
                    rtbSampleComments.Text = "No Samples";
                    rtbSampleComments.ForeColor = Color.Silver;
                    radLabel6.Text = "TIER: ";
                    rtbSampleComments.Enabled = false;
                }
                else
                {
                    scannedFolder = "";
                    if (rtbFolder.Text.Contains('-'))
                    {
                        FolderNo = rtbFolder.Text.Remove(rtbFolder.Text.IndexOf('-'));
                        scannedFolder = FolderNo;
                        scannedSample = rtbFolder.Text.Remove(0, rtbFolder.Text.IndexOf('-') + 1);
                        scannedBottle = scannedSample.Remove(0, scannedSample.IndexOf('.') + 1);
                        scannedSample = scannedSample.Remove(scannedSample.IndexOf('.'));

                        rtbFolder.Text = FolderNo;
                    }
                    if (rtbFolder.Text.ToUpper().StartsWith(sFirstLetter))
                    {
                        rtbFolder.Text = rtbFolder.Text.ToUpper().Trim();
                        FolderNo = rtbFolder.Text.Substring(1);
                    }
                    else if (rtbFolder.Text.Length == 8 && BasicFunctions.IsNumeric(rtbFolder.Text.Substring(0, 1)) == false)//sample from different location
                    {
                        rtbFolder.Text = rtbFolder.Text.ToUpper().Trim();
                        sFirstLetter = rtbFolder.Text.Substring(0, 1);
                        FolderNo = rtbFolder.Text.Substring(1);
                    }
                    else
                    {
                        rtbFolder.Text = sFirstLetter.ToUpper() + rtbFolder.Text.Trim();
                        FolderNo = rtbFolder.Text.Substring(1, 7);
                    }

                    if (BasicFunctions.IsNumeric(FolderNo) && FolderNo.Length == 7)
                    {
                        getInitialData(FolderNo);
                    }
                    else
                    {
                        MessageBox.Show("Invalid Folder Number");
                        rtbSampleComments.Enabled = false;
                        rlvSamples.Items.Clear();
                        rgvTests.DataSource = null;
                        radLabel8.Text = "";
                        radLabel6.Text = "TIER: ";
                    }
                }
                rlvSamples.SelectedIndex = 0;
            }
            //rlvRunTests.SelectedIndex = -1;
            //rlvRunTests.SelectedIndex = 0;
            
        }

        public void getInitialData(string FolderNo)
        {
            if (BasicFunctions.IsNumeric(FolderNo.Substring(0, 1)))
            {
                FolderNo = sFirstLetter.ToUpper() + FolderNo;
            }
            dtData.Rows.Clear();
            //dtData = BasicFunctions.GetData("Select * from solidsprep");
            //dtData = BasicFunctions.GetData("Select tests.method,orders.sp_code,ordtask.BottleID,folders.comments as foldercomments,folderstatus.tier,orders.sampledescription,Preptasks.Ordno,preptasks.testcomments,preptasks.testcode,tests.testno,preptasks.sampweight,preptasks.sampamntunits,DUP,MS,MSD,preptasks.comments,ordtask.comments as pccomments from folderstatus,folders,orders,ordtask,preptasks,tests where tests.testcode = Ordtask.testcode and ordtask.ordno = preptasks.ordno and orders.ordno=ordtask.ordno and ordtask.testcode = preptasks.testcode and ordtask.sp_Code not in (379, 378) and tests.testno<>'TS' and orders.folderno=folders.folderno and folderstatus.folderno=orders.folderno and folderstatus.dept='" + User.Lab +"' and Orders.Folderno = '" + FolderNo + "' Order by Preptasks.Ordno,tests.testno ");
            dtData = BasicFunctions.GetData("Select TESTS.METHOD, SOLIDSPREP.ROWID, ORDERS.SP_CODE, SOLIDSPREP.BOTTLEID, solidsprep.rep, FOLDERS.COMMENTS as FOLDERCOMMENTS, FOLDERSTATUS.TIER, ORDERS.SAMPLEDESCRIPTION, ORDERS.ORDNO, PREPTASKS.TESTCOMMENTS, ORDTASK.TESTCODE, TESTS.TESTNO, SOLIDSPREP.SAMPWEIGHT, SOLIDSPREP.SAMPWEIGHTUNITS SAMPAMNTUNITS, DUP, MS, MSD, SOLIDSPREP.COMMENTS, SOLIDSPREP.QCTYPE, ORDTASK.COMMENTS as PCCOMMENTS from FOLDERSTATUS, FOLDERS, ORDERS, ORDTASK, PREPTASKS, TESTS, SOLIDSPREP where TESTS.TESTCODE = ORDTASK.TESTCODE and ORDTASK.ORDNO = PREPTASKS.ORDNO and ORDERS.ORDNO=ORDTASK.ORDNO and ORDTASK.TESTCODE = PREPTASKS.TESTCODE and ORDTASK.SP_CODE not in (379, 378) and TESTS.TESTNO<>'TS' and ORDERS.FOLDERNO=FOLDERS.FOLDERNO and FOLDERSTATUS.FOLDERNO=ORDERS.FOLDERNO and ORDTASK.ORDNO = SOLIDSPREP.ORDNO (+) and ORDTASK.TESTCODE = SOLIDSPREP.TESTCODE (+) and FOLDERSTATUS.DEPT='" + User.Lab + "' and ORDERS.FOLDERNO = '" + FolderNo + "' Order by ORDTASK.ORDNO,TESTS.TESTNO");
            //dtData = BasicFunctions.GetData("Select * from solidsprep");
            //dtData = BasicFunctions.GetData("Select * from preptasks where ordno='K1601234-001'");
            if (dtData.Rows.Count == 0)
            {
                MessageBox.Show("No samples found for folder " + FolderNo + ".");
                rtbSampleComments.Enabled = false;
                rlvSamples.Items.Clear();
                rgvTests.DataSource = null;
                radLabel8.Text = "";
                radLabel6.Text = "TIER: ";
            }
            else
            {
                //clear previous samples
                FolderSamps.Samples.Clear();
                fullFolder = FolderNo;
                GetData();
                rtbSampleComments.Enabled = true;
            }
            
        }

        private void GetData()
        {
            //GetQCWeights();
            //tryXML = 0;

            //List<Test> FolderQC = QCTest.Where(t => t.Samp != null && t.Samp.ParentName.Contains(fullFolder)).ToList();

            radLabel8.Text = dtData.AsEnumerable().Select(dr => dr["FOLDERCOMMENTS"].ToString()).First();
            if (radLabel8.Text.Trim() == "")
            {
                radLabel8.Text = "(none)";
            }
            radLabel6.Text = "TIER: " + dtData.AsEnumerable().Select(dr => dr["TIER"].ToString()).First();
            //build object
            List<string> tableSamps = dtData.AsEnumerable().Select(dr => dr["ORDNO"].ToString()).Distinct().ToList();
            foreach (string s in tableSamps)
            {
                Samp = new Sample(); //each sample
                Samp.SampleName = s;
                Samp.SampleDescription = dtData.AsEnumerable().Where(dr => dr["ORDNO"].ToString() == s).Select(dr => dr["SAMPLEDESCRIPTION"].ToString()).First();

                List<string> tableTests = dtData.AsEnumerable().Where(dr => dr["ORDNO"].ToString() == s).Select(dr => dr["TESTNO"].ToString()).Distinct().ToList();

                foreach (string t in tableTests)
                {
                    List<string> methodz = dtData.AsEnumerable().Where(dr => dr["ORDNO"].ToString() == s && dr["TESTNO"].ToString() == t).Select(dr => dr["METHOD"].ToString()).Distinct().ToList();
                    if (methodz.Count == 0)
                    {
                        methodz.Add("");
                    }

                    foreach (string m in methodz)
                    {
                        List<DataRow> drz = dtData.AsEnumerable().Where(dr => dr["ORDNO"].ToString() == s && dr["TESTNO"].ToString() == t && dr["METHOD"].ToString() == m).ToList();
                        foreach (DataRow dr in dtData.AsEnumerable().Where(dr => dr["ORDNO"].ToString() == s && dr["TESTNO"].ToString() == t && dr["METHOD"].ToString() == m).ToList())
                        {
                            test = new Test(); //each test
                            test.TestName = t;
                            //test.PrepTName = dtData.AsEnumerable().Where(dr => dr["ORDNO"].ToString() == s && dr["TESTNO"].ToString() == test.TestName).Select(dr => dr["PREPTMNAME"].ToString()).First();
                            test.Samp = new QCSample();

                            test.Samp.ParentName = Samp.SampleName;
                            test.TestCode = dr["TESTCODE"].ToString();
                            test.PrepComments = dr["COMMENTS"].ToString();
                            test.PCComments = dr["PCCOMMENTS"].ToString();
                            test.SPCode = test.Container = dr["SP_CODE"].ToString();
                            test.Container = dr["BOTTLEID"].ToString();
                            test.Rep = dr["REP"].ToString();
                            test.Container = test.Container.Remove(0, test.Container.IndexOf('.') + 1);
                            test.Method = m;// dtData.AsEnumerable().Where(dr => dr["ORDNO"].ToString() == s && dr["TESTNO"].ToString() == test.TestName).Select(dr => dr["METHOD"].ToString()).First();
                            if (dr["DUP"].ToString() == "Y")
                            {
                                test.Dup = "Y";
                                ReqQCSamps.Samples.Add(Samp);
                            }
                            if (dr["MS"].ToString() == "Y")
                            {
                                test.MS = "Y";
                                ReqQCSamps.Samples.Add(Samp);
                            }
                            if (dr["MSD"].ToString() == "Y")
                            {
                                test.DMS = "Y";
                                ReqQCSamps.Samples.Add(Samp);
                            }
                            test.Mass = dr["SAMPWEIGHT"].ToString();
                            test.Unit = dr["SAMPAMNTUNITS"].ToString();
                            if (test.Unit == null || test.Unit == "")
                            {
                                test.Unit = "g";
                            }
                            test.RowID = dr["ROWID"].ToString();
                            test.Type = dr["QCTYPE"].ToString(); 

                            Samp.Tests.Add(test); //add tests to sample
                        }
                        //if (FolderQC.Where(tQC => tQC.TestName == test.TestName && tQC.Samp.ParentName == s && tQC.Method == m).ToList() != null)
                        //{
                        //    foreach (Test tQC in FolderQC.Where(QCt => QCt.TestName == test.TestName && QCt.Samp.ParentName == s && QCt.Method == m).ToList())
                        //    {
                        //        Samp.Tests.Add(tQC);
                        //    }
                        //}
                    }
                }
                FolderSamps.Samples.Add(Samp); //add them all to folder
            }
            foreach (Sample sa in FolderSamps.Samples) //display name includes sample name and number of tests
            {
                sa.DisplayName = sa.SampleName + " (" + FolderSamps.Samples.Where(samp => samp.SampleName == sa.SampleName).SelectMany(samp => samp.Tests).ToList().Count.ToString() + ")";
            }
            DisplayData();
        }
            
            
        private void DisplayData()
        {
            //display sample object to user
            rlvSamples.DataSource = null;
            rlvSamples.DataSource = FolderSamps.Samples;
            rlvSamples.DisplayMember = "DisplayName";
            if (scannedFolder != "")
            {
                rlvSamples.SelectedIndex = rlvSamples.Items.IndexOf(rlvSamples.Items.Where(lv => ((Sample)lv.DataBoundItem).SampleName == scannedFolder + '-' + scannedSample).FirstOrDefault());
                if (rlvSamples.SelectedIndex >= 0 && rlvSamples.SelectedIndex < rlvSamples.Items.Count)
                {
                    rlvSamples.SelectedItem = rlvSamples.Items[rlvSamples.SelectedIndex];
                }
            }

        }

        private void rlvTests_SelectedItemChanged(object sender, EventArgs e)
        {
        }

        private void rlvSamples_SelectedItemChanged(object sender, EventArgs e)
        {         
            if (rlvSamples.SelectedItem != null)
            {
                if (((Sample)rlvSamples.SelectedItem.DataBoundItem).SampleDescription != null && ((Sample)rlvSamples.SelectedItem.DataBoundItem).SampleDescription != "")
                {
                    rtbSampleComments.ForeColor = Color.Black;
                    rtbSampleComments.Text = ((Sample)rlvSamples.SelectedItem.DataBoundItem).SampleDescription;
                }
                else
                {
                    rtbSampleComments.ForeColor = Color.Silver;
                    rtbSampleComments.Text = "Sample Comments";
                }
                //rgvTests.Rows.Clear();
                rgvTests.DataSource = null;
                if (rlvSamples.SelectedItem.DataBoundItem.GetType() == typeof(Sample))// Samp.GetType())
                {
                    Sample s = (Sample)rlvSamples.SelectedItem.DataBoundItem;
                    rgvTests.DataSource = s.Tests.OrderBy(t => t.TypeNo).OrderBy(t => t.Rep).OrderBy(t => t.TestName);// FolderSamps.Samples.Where(samp => samp == rlvSamples.SelectedItem.Value).FirstOrDefault().Tests;
                }
                else
                {
                    rgvTests.DataSource = FolderSamps.Samples.Where(samp => samp.DisplayName == rlvSamples.SelectedItem.Value.ToString()).FirstOrDefault().Tests.OrderBy(t => t.Rep).OrderBy(t => t.TypeNo).OrderBy(t => t.TestName);
                }
                if (scannedFolder != "")
                {
                    foreach (GridViewRowInfo gv in rgvTests.Rows)
                    {
                        ((Test)gv.DataBoundItem).Container = scannedBottle;
                    }
                }
            }
        }

        private void rtbMass_KeyDown(object sender, KeyEventArgs e)
        {  
        }

        private void rlvSamples_VisualItemCreating(object sender, Telerik.WinControls.UI.ListViewVisualItemCreatingEventArgs e)
        {
            bool allWeigh = false;
            Samp = FolderSamps.Samples.Where(samp => samp == e.DataItem.DataBoundItem).FirstOrDefault();
            //if (Samp == null)
            //{
            //    Samp = FolderSamps.Samples.Where(samp => samp.DisplayName == e.DataItem.Value.ToString()).First();
            //}
            foreach (Test t in Samp.Tests)
            {
                if (t.Mass == null)
                {
                    allWeigh = false;
                    break; //at least one is false
                }
                else // all tests are weighed for sample
                {
                    allWeigh = true;
                }
            }
            if (allWeigh)
            {
                e.DataItem.ForeColor = Color.Green;
            }

            if (FolderSamps.Samples.Where(s => s.QC == true).ToList().Contains((Sample)e.DataItem.DataBoundItem))
            {
                e.DataItem.ForeColor = Color.OrangeRed;
            }
        }

        private void rlvRunTests_VisualItemCreating(object sender, ListViewVisualItemCreatingEventArgs e)
        {
            bool allWeigh = false;
            List<Sample> SampsForTest = new List<Sample>();
            foreach (Sample s in FolderSamps.Samples)
            {
                if (s.Tests.Where(t => t.TestName == e.DataItem.Value.ToString()).FirstOrDefault() != null)
                {
                    if (s.Tests.Where(t => t.TestName == e.DataItem.Value.ToString()).First().Mass == null)
                    {
                        allWeigh = false;
                        break;
                    }
                    else
                    {
                        allWeigh = true;
                    }
                }
            }
            if (allWeigh)
            {
                e.DataItem.ForeColor = Color.Green;
            }
        }

        private void rlvTests_VisualItemCreating(object sender, Telerik.WinControls.UI.ListViewVisualItemCreatingEventArgs e)
        {
        }

        private void rddlUnits_KeyDown(object sender, KeyEventArgs e)
        {
        }

        private void rgvTests_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyData == Keys.Return)
            {
                if (rgvTests.CurrentRow.Cells["Mass"].Value != null)
                {
                    test = FolderSamps.Samples.Where(s => s.SampleName == ((Sample)rlvSamples.SelectedItem.DataBoundItem).SampleName.ToString()).First().Tests.Where(t => t.TestName == ((Test)rgvTests.CurrentRow.DataBoundItem).TestName).First();
                    if (rgvTests.CurrentRow.Cells["Unit"].Value != null)
                    {
                        if (rgvTests.CurrentRow.Cells["Mass"].Value.ToString() != test.Mass || rgvTests.CurrentRow.Cells["Unit"].Value.ToString() != test.Unit)
                        {
                            //sql to insert back into LIMS
                            test.Unit = rgvTests.CurrentRow.Cells["Unit"].Value.ToString();
                            test.Mass = rgvTests.CurrentRow.Cells["Mass"].Value.ToString();
                        }
                    }
                    else
                    {
                        MessageBox.Show("Enter Units");
                    }
                }
                //scroll to next test in sample's list.  go back to top if on last one
                if (rgvTests.CurrentRow.Index == rgvTests.Rows[rgvTests.Rows.Count - 1].Index)
                {
                    rgvTests.CurrentRow = rgvTests.Rows[0];
                }
                else
                {
                    rgvTests.CurrentRow = rgvTests.Rows[rgvTests.CurrentRow.Index + 1];
                }
            }
            
        }

        private void rgvTests_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                if (rgvTests.DataSource != null)
                {
                    RadContextMenu rmi = new RadContextMenu();
                    if (rgvTests.CurrentCell.ColumnIndex == 3)
                    {
                        RadMenuItem rmiFill = new RadMenuItem("Fill All");
                        rmiFill.Click += new EventHandler(FillAllCells);

                        RadMenuItem rmiFillEmpty = new RadMenuItem("Fill Empty");
                        rmiFillEmpty.Click += new EventHandler(FillEmptyCells);

                        rmi.Items.Add(rmiFill);
                        rmi.Items.Add(rmiFillEmpty);
                        rmi.Show(Control.MousePosition);
                    }
                    else if (rgvTests.CurrentCell.ColumnIndex == rgvTests.Columns["Container"].Index)
                    {
                        RadMenuItem rmiFill = new RadMenuItem("Fill All");
                        rmiFill.Click += new EventHandler(FillAllCells);

                        RadMenuItem rmiFillEmpty = new RadMenuItem("Fill Empty");
                        rmiFillEmpty.Click += new EventHandler(FillEmptyCells);

                        rmi.Items.Add(rmiFill);
                        rmi.Items.Add(rmiFillEmpty);
                        rmi.Show(Control.MousePosition);
                    }
                    else if (rgvTests.CurrentCell.ColumnIndex == 0)
                    {
                        //add QC to test


                        if (rgvTests.CurrentRow.Cells["Type"].Value == null || rgvTests.CurrentRow.Cells["Type"].Value.ToString() == "" || rgvTests.CurrentRow.Cells["Type"].Value.ToString() == "N/A")
                        {
                            RadMenuItem rmiDup = new RadMenuItem("Add DUP");
                            rmiDup.Click += new EventHandler(AddDup);

                            RadMenuItem rmiMS = new RadMenuItem("Add MS");
                            rmiMS.Click += new EventHandler(AddMS);

                            RadMenuItem rmiMSDMS = new RadMenuItem("Add MS/DMS");
                            rmiMSDMS.Click += new EventHandler(AddMSDMS);

                            RadMenuItem rmiTRIP = new RadMenuItem("Add DUP/TRP");
                            rmiTRIP.Click += new EventHandler(AddTRIP);

                            RadMenuItem rmiQUAD = new RadMenuItem("Add DUP/TRP/QUAD");
                            rmiQUAD.Click += new EventHandler(AddQUAD);

                            rmi.Items.Add(rmiDup);
                            rmi.Items.Add(rmiMS);
                            rmi.Items.Add(rmiMSDMS);
                            rmi.Items.Add(rmiTRIP);
                            rmi.Items.Add(rmiQUAD);
                        }
                        else
                        {
                            RadMenuItem rmiRemove = new RadMenuItem("Remove");
                            rmiRemove.Click += new EventHandler(RemoveSampleQC);

                            rmi.Items.Add(rmiRemove);

                        }

                        rmi.Show(Control.MousePosition);
                    }
                    else if (rgvTests.CurrentCell.ColumnIndex == rgvTests.Columns["TestComments"].Index)
                    {
                        //edit test comment in window that show pc comment, but does not let pc comment be edited
                        RadMenuItem rmiEdit = new RadMenuItem("Edit Test Prep Comments");
                        rmiEdit.Click += new EventHandler(EditPrepComments);


                        rmi.Items.Add(rmiEdit);
                        rmi.Show(Control.MousePosition);
                    }
                }
            }
        }

        private void EditPrepComments(object sender, EventArgs e)
        {
            EditTestPrepComments editcomments = new EditTestPrepComments((Sample)rlvSamples.SelectedItem.DataBoundItem, (Test)rgvTests.SelectedRows[0].DataBoundItem);
            editcomments.ShowDialog();


            string prepComments = ((Test)rgvTests.CurrentRow.DataBoundItem).PrepComments;
            string testCode = ((Test)rgvTests.CurrentRow.DataBoundItem).TestCode;
            string sampName = ((Sample)rlvSamples.SelectedItem.DataBoundItem).SampleName;
            if (((Test)rgvTests.CurrentRow.DataBoundItem).Type != null && ((Test)rgvTests.SelectedRows[0].DataBoundItem).Type != "")
            {
                test = ((Test)rgvTests.CurrentRow.DataBoundItem);
                //if (QCTest.Contains(test))
                //{
                //    QCTest.Remove(test);
                //}
                test.PrepComments = ((Test)rgvTests.CurrentRow.DataBoundItem).PrepComments;

                //QCTest.Add(test);
                //SaveQCWeights();
                //tryXML = 0;
            }
            else
            {
                BasicFunctions.GetData("Update preptasks set comments='" + prepComments + "' where testcode = '" + testCode + "' and ordno='" + sampName + "'");
            }
            
        }

        private void RemoveSampleQC(object sender, EventArgs e)
        {
            //remove from xml
            //test = QCTest.Where(t => t.SampleName == ((Test)rgvTests.CurrentRow.DataBoundItem).SampleName && t.Mass == ((Test)rgvTests.CurrentRow.DataBoundItem).Mass && t.TestCode == ((Test)rgvTests.CurrentRow.DataBoundItem).TestCode && t.Method == ((Test)rgvTests.CurrentRow.DataBoundItem).Method && t.Number == ((Test)rgvTests.CurrentRow.DataBoundItem).Number && t.TestName == ((Test)rgvTests.CurrentRow.DataBoundItem).TestName).FirstOrDefault();
            //if (test != null)
            //{
            //    QCTest.Remove(test);
            //    SaveQCWeights();
            //    tryXML = 0;
            //}
            
            //remove from list visible to user
            foreach (GridViewRowInfo t in rgvTests.SelectedRows)
            {
                BasicFunctions.GetData("delete from SolidsPrep where rowID = '" + ((Test)t.DataBoundItem).RowID + "'");
                ((Sample)rlvSamples.SelectedItem.DataBoundItem).Tests.Remove((Test)t.DataBoundItem);
            }
            //DataTable susan = BasicFunctions.GetData("select * from solidsPrep where testcode='1116'");
            DisplayData();
        }

        private void AddTRIP(object sender, EventArgs e)
        {
            List<string> trip = new List<string>();
            trip.Add("DUP");
            trip.Add("TRP");
            AddSampleQC(trip);
        }

        private void AddQUAD(object sender, EventArgs e)
        {
            List<string> quad = new List<string>();
            quad.Add("DUP");
            quad.Add("TRP");
            quad.Add("QUAD");
            AddSampleQC(quad);
        }

        private void AddMSDMS(object sender, EventArgs e)
        {
            List<string> MSDMS = new List<string>();
            MSDMS.Add("MS");
            MSDMS.Add("DMS");
            AddSampleQC(MSDMS);
        }

        private void AddMS(object sender, EventArgs e)
        {
            List<string> MS = new List<string>();
            MS.Add("MS");
            AddSampleQC(MS);
        }

        private void AddDup(object sender, EventArgs e)
        {
            List<string> dup = new List<string>();
            dup.Add("DUP");
            AddSampleQC(dup);    
        }

        private void AddSampleQC(List<string> type)
        {
            //add to list
            Samp = FolderSamps.Samples.Where(s => s == rlvSamples.SelectedItem.DataBoundItem as Sample).First();

            foreach (string t in type)
            {
                foreach (GridViewRowInfo gvri in rgvTests.SelectedRows)
                {
                    if (((Test)gvri.DataBoundItem).Type == null || ((Test)gvri.DataBoundItem).Type == "" || ((Test)gvri.DataBoundItem).Type == "N/A")
                    {
                        test = new Test();
                        test.TestName = ((Test)gvri.DataBoundItem).TestName;
                        test.Mass = "";
                        test.Unit = "g";
                        test.Type = t;
                        test.PCComments = "";
                        test.PrepComments = "";
                        test.Unit = "g";
                        test.Samp = new QCSample();
                        test.Samp.ParentName = Samp.SampleName;
                        test.SampleName = test.Samp.ParentName + ' ' + test.Type;
                        test.Today = DateTime.Now;
                        test.TestCode = ((Test)gvri.DataBoundItem).TestCode;
                        test.Method = ((Test)gvri.DataBoundItem).Method;
                        if (test.Container == null && ((Test)gvri.DataBoundItem).Container != null)
                        {
                            test.Container = ((Test)gvri.DataBoundItem).Container;
                        }
                        else
                        {
                            test.Container = "";
                        }

                        Samp.Tests.Add(test);

                        string rep = "";

                        DataTable dtRep = BasicFunctions.GetData("select *, rowid from solidsprep where ordno = '" + test.Samp.ParentName + "' and testcode = '" + test.TestCode + "'");
                        if (dtRep.AsEnumerable().Where(dr => dr["QCTYPE"].ToString() == t).FirstOrDefault() != null)
                        {
                            rep = dtRep.AsEnumerable().Where(dr => dr["ORDNO"].ToString() == test.Samp.ParentName && dr["QCTYPE"].ToString() == t).Select(dr => dr["REP"].ToString()).Last();
                            rep = (Convert.ToInt16(rep) + 1).ToString();
                        }
                        else
                        {
                            rep = "1";
                        }
                        test.Rep = rep;
                        BasicFunctions.GetData("Call RunCreation.SaveSolidsData('" + test.Samp.ParentName + "', '" + test.TestCode + "', '" + test.Type + "', '" + test.Mass + "', '" + test.Unit + "', '" + test.Container + "', '" + test.PrepComments + "', '" + rep + "')");
                        
                        dtRep = BasicFunctions.GetData("select *, rowid from solidsprep where ordno = '" + test.Samp.ParentName + "' and testcode = '" + test.TestCode + "'");
                        test.RowID = dtRep.AsEnumerable().Where(dr => dr["ORDNO"].ToString() == test.Samp.ParentName && dr["QCTYPE"].ToString() == t).Select(dr => dr["ROWID"].ToString()).First();

                        //test.Number = Samp.Tests.Where(tesT => tesT.TestName == test.TestName && Samp.SampleName == tesT.Samp.ParentName && tesT.Method == test.Method).ToList().Count;

                        //QCTest.Add(test);
                        //try
                        //{
                        //    SaveQCWeights();
                        //    tryXML = 0;
                        //}
                        //catch
                        //{
                        //    MessageBox.Show("Error in adding QC.");
                        //}
                    }
                }
            }
            rgvTests.DataSource = ((Sample)rlvSamples.SelectedItem.DataBoundItem).Tests.OrderBy(t => t.TestNameAndMethod);
        }

        private void FillAllCells(object sender, EventArgs e)
        {
            if (rgvTests.CurrentColumn.Index == rgvTests.Columns["Unit"].Index)
            {
                string massforall = rgvTests.CurrentCell.Value.ToString();
                foreach (GridViewRowInfo r in rgvTests.Rows)
                {
                    r.Cells["Unit"].Value = massforall;
                }
            }
            else
            {
                string bottleforall = rgvTests.CurrentCell.Value.ToString();
                foreach (GridViewRowInfo r in rgvTests.Rows)
                {
                    r.Cells["Container"].Value = bottleforall;  
                }
            }
        }

        private void FillEmptyCells(object sender, EventArgs e)
        {
            if (rgvTests.CurrentColumn.Index == rgvTests.Columns["Unit"].Index)
            {
                string massformost = rgvTests.CurrentCell.Value.ToString();
                foreach (GridViewRowInfo r in rgvTests.Rows)
                {
                    if (r.Cells["Unit"].Value == null || r.Cells["Unit"].Value.ToString() == "")
                    {
                        r.Cells["Unit"].Value = massformost;
                    }
                }
            }
            else
            {
                string bottleformost = rgvTests.CurrentCell.Value.ToString();
                foreach (GridViewRowInfo r in rgvTests.Rows)
                {
                    if (r.Cells["Container"].Value == null || r.Cells["Container"].Value.ToString() == "")
                    {
                        r.Cells["Container"].Value = bottleformost;
                    }
                }
            }
        }

        private void rgvTests_SelectionChanging(object sender, GridViewSelectionCancelEventArgs e)
        {
        }

        private void rgvTests_Leave(object sender, EventArgs e)
        {
            if (rgvTests.SelectedRows != null)
            {                
                rgvTests.EndEdit();
            }
        }

        private void rlvRunTests_SelectedItemChanged(object sender, EventArgs e)
        {
            rlvAvailableBatch.DataSource = null;
            rgvSampsinBatch.DataSource = null;
            GetSampsandBatches();              

            rgvSampsinBatch.MasterTemplate.BestFitColumns();
        }

        private void GetSampsandBatches()
        {
            rgvSampsinBatch.DataSource = null;
            radLabel4.Text = "";
            string prepTname = "";
            rlvAvailableBatch.DataSource = null;

            rlvSampsForBatching.Items.Clear();
            if (rlvRunTests.SelectedItem != null)
            {
                testPage2 = (Test)rlvRunTests.SelectedItem.DataBoundItem;
                prepTname = testPage2.PrepTName;
            }

            if (rlvRunTests.SelectedItem != null)
            {
                rlvSampsForBatching.DataSource = TestsNeedingBatch.Where(bt => bt.Method == ((Test)rlvRunTests.SelectedItem.DataBoundItem).Method && bt.TestName == ((Test)rlvRunTests.SelectedItem.DataBoundItem).TestName.ToString()).First().sampsForBatch.ToList().Where(s => !s.SampleName.StartsWith("KQ")).Distinct().ToList();
                rlvSampsForBatching.DisplayMember = "SampleName";
                rlvAvailableBatch.Items.Clear();
                if (prepTname != "" && ((Test)rlvRunTests.SelectedItem.DataBoundItem).sampsForBatch.Count > 0)
                {
                    //dtRunNos = BasicFunctions.GetData("Select PREPTASKS.PREPRUNNO,ORDNO,PREPCUPNO from prepruns,PREPTASKS where prepruns.preprunno = PREPTASKS.preprunno and PREPTASKS.preptmname = '" + prepTname + "' and PrepTasks.Dept = 'KELSO' and status = 'Draft'");
                    dtRunNos = BasicFunctions.GetData("Select prepruns.servgrp,prepduedate,Prepruns.PREPRUNNO,PREPTASKS.ORDNO,PREPTASKS.PREPCUPNO from prepruns Left Join preptasks on prepruns.preprunno = preptasks.preprunno Left Join Ordtask on  Ordtask.ordno = preptasks.ordno and Ordtask.testcode = preptasks.testcode and (ordtask.sp_Code <> 379 and ordtask.sp_Code <> 378) where  prepruns.preptmname = '" + prepTname + "' and Prepruns.Dept = '" + User.Lab + "' and status = 'Draft' order by PREPRUNS.PREPRUNNO desc");


                    ListViewDataItem lvdi;
                    foreach (string strdr in dtRunNos.AsEnumerable().Where(xSel => xSel["SERVGRP"].ToString() == ((Test)rlvRunTests.SelectedItem.DataBoundItem).sampsForBatch[0].Tests[0].ServGrp).Select(xSel => xSel["PREPRUNNO"].ToString()).Distinct().ToList())
                    {
                        lvdi = new ListViewDataItem(strdr);
                        lvdi.Tag = dtRunNos.AsEnumerable().Where(xW => xW["PREPRUNNO"].ToString() == strdr).Select(x => x["ORDNO"].ToString()).ToList();
                        rlvAvailableBatch.Items.Add(lvdi);
                    }

                    rlvAvailableBatch.SelectedIndex = 0;

                    //rlvAvailableBatch.DataSource = dtRunNos;
                    //rlvAvailableBatch.DisplayMember = "PREPRUNNO";
                }
            }

            //only real samples count towards sample number
            radLabel3.Text = "(" + rlvSampsForBatching.Items.Where(lvdi => !lvdi.Value.ToString().StartsWith("KQ")).Count().ToString() + " Samples)";
            radLabel3.Show();

            //foreach (string s in FolderSamps.Samples.Select(samp => samp.SampleName))
            //{
            //    if (rlvRunTests.SelectedItem != null)
            //    {
            //        List<Sample> slist = FolderSamps.Samples.Where(sa => sa.SampleName == s).First().Tests.Select(t => t.Samp).ToList(); //KQs
            //        foreach (Sample sa in slist)
            //        {
            //            if (sa != null)
            //            {
            //                foreach (Test te in sa.Tests)
            //                {
            //                    if (te.TestName == rlvRunTests.SelectedItem.Value.ToString())
            //                    {
            //                        rlvSampsForBatching.Items.Add(sa.SampleName + " " + te.Type);
            //                    }
            //                }
            //            }
            //        }
            //    }
            //}
            rlvSampsForBatching.SelectedIndex = -1;
        }

        private void rclbSampsforRun_VisualItemCreating(object sender, ListViewVisualItemCreatingEventArgs e)
        {
            e.DataItem.CheckState = Telerik.WinControls.Enumerations.ToggleState.On;

            
        }

        private void rgvTests_CellFormatting(object sender, CellFormattingEventArgs e)
        {
            if (e.Row.DataBoundItem != null && e.CellElement.ColumnIndex == 0)
            {
                if (e.Row.DataBoundItem.GetType() == typeof(Test))
                {
                    test = e.Row.DataBoundItem as Test;
                    if (test.Dup == "Y" || test.MS == "Y" || test.DMS == "Y")
                    {
                        e.CellElement.ForeColor = Color.OrangeRed;
                        e.CellElement.ToolTipText = test.QC;
                    }
                }
            }

        }

        private void rlvAvailableBatch_SelectedItemChanged(object sender, EventArgs e)
        {
            if (rlvAvailableBatch.SelectedItem != null)
            {
                string batch = rlvAvailableBatch.SelectedItem.Text;
                GetCurrentBatchItems(batch);

                rgvSampsinBatch.MasterTemplate.BestFitColumns();
            }
        }

        private void rgvTests_SelectionChanged(object sender, EventArgs e)
        {
            radLabel4.Hide();
            test = rgvTests.CurrentRow.DataBoundItem as Test;

            if (test != null)
            {
                List<Sample> sampsWithQC = new List<Sample>();
                if (ReqQCSamps != null)
                {
                    foreach (Sample s in ReqQCSamps.Samples)
                    {
                        if (s.Tests.Where(t => t.TestName == test.TestName) != null)
                        {
                            sampsWithQC.Add(s);
                        }
                    }
                    string QCSamp = "";
                    foreach (Sample s in sampsWithQC)
                    {
                        QCSamp = s.SampleName + " ";
                    }
                    if (QCSamp != "")
                    {
                        radLabel5.Text = QCSamp + " has required QC for " + test.TestName + '.';
                        radLabel5.Show();
                    }
                    else
                    {
                        radLabel5.Text = "";
                        radLabel5.Show();
                    }
                }
            }
        }

        private void radPageView1_SelectedPageChanged(object sender, EventArgs e)
        {
            if (radPageView1.SelectedPage == radPageViewPage2)
            {
                //GetQCWeights();
                //tryXML = 0;
                MakeTestList();
            }
            else
            {
                if (rlvSamples.Items.Count > 0)
                {
                    rlvSamples.SelectedIndex = -1;
                    rlvSamples.SelectedIndex = 0;
                }
        }
        }

        private void MakeTestList()
        {
            rlvRunTests.Items.Clear();
            //rlvRunTests is samples that have weights that do not have batches
            //dtBatch = BasicFunctions.GetData("Select folderno,ordtask.preptmname,Preptasks.Ordno,ordtask.testno,preptasks.sampweight,preptasks.sampamntunits from ordtask,preptasks where ordtask.ordno = preptasks.ordno and ordtask.testcode = preptasks.testcode and preptasks.prepts='Need Prep' and preptasks.dept = 'KELSO' and preptasks.sampweight IS NOT NULL Order by ordtask.testno,Preptasks.Ordno");
            //dtBatch = BasicFunctions.GetData("Select tests.method,ordtask.servgrp,orders.sp_code,prepduedate,ordtask.BottleID,ordtask.folderno,ordtask.preptmname,Preptasks.Ordno,tests.testno,preptasks.sampweight,preptasks.sampamntunits,orders.sampledescription,preptasks.testcode,preptasks.comments from ordtask,preptasks,orders,tests where tests.testcode = Ordtask.testcode and ordtask.ordno = preptasks.ordno and orders.ordno=ordtask.ordno and ordtask.testcode = preptasks.testcode and preptasks.prepts='Need Prep' and preptasks.dept = '" + User.Lab + "' and preptasks.preprunno=-1 and orders.sp_Code not in (379,378) and exists(Select 1 from SOLIDSPREP where sampweight <> '' and ORDNO = ORDTASK.ORDNO AND TESTCODE = ORDTASK.TESTCODE) Order by tests.testno,Preptasks.Ordno");//Select tests.method,ordtask.servgrp,orders.sp_code,prepduedate,ordtask.BottleID,ordtask.folderno,ordtask.preptmname,Preptasks.Ordno,tests.testno,preptasks.sampweight,preptasks.sampamntunits,orders.sampledescription,preptasks.testcode,preptasks.comments from ordtask,preptasks,orders,tests where tests.testcode = Ordtask.testcode and ordtask.ordno = preptasks.ordno and orders.ordno=ordtask.ordno and ordtask.testcode = preptasks.testcode and preptasks.prepts='Need Prep' and preptasks.dept = '" + User.Lab + "' and preptasks.preprunno=-1 and (ordtask.sp_Code <> 379 and ordtask.sp_Code <> 378) and preptasks.sampweight IS NOT NULL  Order by tests.testno,Preptasks.Ordno");
            dtBatch = BasicFunctions.GetData("Select tests.method,ordtask.servgrp,orders.sp_code,prepduedate,ordtask.BottleID,ordtask.folderno,ordtask.preptmname,Preptasks.Ordno,tests.testno,preptasks.sampweight,preptasks.sampamntunits,orders.sampledescription,preptasks.testcode,preptasks.comments from ordtask,preptasks,orders,tests where tests.testcode = Ordtask.testcode and ordtask.ordno = preptasks.ordno and orders.ordno=ordtask.ordno and ordtask.testcode = preptasks.testcode and preptasks.prepts='Need Prep' and preptasks.dept = '" + User.Lab + "' and preptasks.preprunno=-1 and orders.sp_Code not in (379,378) and exists(Select 1 from SOLIDSPREP where ORDNO = ORDTASK.ORDNO AND TESTCODE = ORDTASK.TESTCODE) Order by tests.testno,Preptasks.Ordno");//Select tests.method,ordtask.servgrp,orders.sp_code,prepduedate,ordtask.BottleID,ordtask.folderno,ordtask.preptmname,Preptasks.Ordno,tests.testno,preptasks.sampweight,preptasks.sampamntunits,orders.sampledescription,preptasks.testcode,preptasks.comments from ordtask,preptasks,orders,tests where tests.testcode = Ordtask.testcode and ordtask.ordno = preptasks.ordno and orders.ordno=ordtask.ordno and ordtask.testcode = preptasks.testcode and preptasks.prepts='Need Prep' and preptasks.dept = '" + User.Lab + "' and preptasks.preprunno=-1 and (ordtask.sp_Code <> 379 and ordtask.sp_Code <> 378) and preptasks.sampweight IS NOT NULL  Order by tests.testno,Preptasks.Ordno"); 
            //dtBatch = BasicFunctions.GetData("Select * from preptasks where ordno='K1601234-001'");
            //get list of tests
            List<string> testNames = dtBatch.AsEnumerable().Select(dr => dr["TESTNO"].ToString()).Distinct().ToList();
            TestsNeedingBatch.Clear();

            foreach (string test in testNames)
            {
                List<string> methodz = dtBatch.AsEnumerable().Where(dr => dr["TESTNO"].ToString() == test).Select(dr => dr["METHOD"].ToString()).ToList();
                if (methodz.Count == 0)
                {
                    methodz.Add("");
                }
                foreach (string m in methodz)
                {
                    batchTest = new Test();

                    batchTest.TestName = test;
                    batchTest.PrepTName = dtBatch.AsEnumerable().Where(dr => dr["TESTNO"].ToString() == test).Select(dr => dr["PREPTMNAME"].ToString()).First();
                    batchTest.Folders = dtBatch.AsEnumerable().Where(dr => dr["TESTNO"].ToString() == test).Select(dr => dr["FOLDERNO"].ToString()).Distinct().ToList();
                    batchTest.Method = m;// dtBatch.AsEnumerable().Where(dr => dr["TESTNO"].ToString() == test).Select(dr => dr["METHOD"].ToString()).First();

                    List<string> sampswithtest = dtBatch.AsEnumerable().Where(dr => dr["TESTNO"].ToString() == test).Select(dr => dr["ORDNO"].ToString()).ToList();

                    foreach (string sampleName in sampswithtest)
                    {
                        if (!sampleName.StartsWith("KQ"))
                        {
                            if (dtBatch.AsEnumerable().Where(dr => dr["TESTNO"].ToString() == test && dr["ORDNO"].ToString() == sampleName && dr["METHOD"].ToString() == m).FirstOrDefault() != null)
                            {
                                page2Samp = new Sample();
                                page2Samp.SampleName = sampleName;

                                batchTest.sampsForBatch.Add(page2Samp);
                                Test bt = new Test();
                                bt.Mass = dtBatch.AsEnumerable().Where(dr => dr["TESTNO"].ToString() == test && dr["ORDNO"].ToString() == sampleName && dr["METHOD"].ToString() == m).Select(dr => dr["SAMPWEIGHT"].ToString()).First();
                                bt.Unit = dtBatch.AsEnumerable().Where(dr => dr["TESTNO"].ToString() == test && dr["ORDNO"].ToString() == sampleName && dr["METHOD"].ToString() == m).Select(dr => dr["SAMPAMNTUNITS"].ToString()).First();
                                if (dtBatch.AsEnumerable().Where(dr => dr["TESTNO"].ToString() == test && dr["ORDNO"].ToString() == sampleName && dr["METHOD"].ToString() == m).Select(dr => dr["PREPDUEDATE"].ToString()).First() != "")
                                {
                                    bt.DueDate = (Convert.ToDateTime(dtBatch.AsEnumerable().Where(dr => dr["TESTNO"].ToString() == test && dr["ORDNO"].ToString() == sampleName && dr["METHOD"].ToString() == m).Select(dr => dr["PREPDUEDATE"].ToString()).First())).ToString("MMM-dd-yyyy");
                                }
                                bt.TestName = test;
                                bt.SampleName = sampleName;
                                bt.Container = dtBatch.AsEnumerable().Where(dr => dr["TESTNO"].ToString() == test && dr["ORDNO"].ToString() == sampleName && dr["METHOD"].ToString() == m).Select(dr => dr["BOTTLEID"].ToString()).First();
                                bt.Container = bt.Container.Remove(0, bt.Container.IndexOf('.') + 1);
                                bt.SampleDescription = dtBatch.AsEnumerable().Where(dr => dr["TESTNO"].ToString() == test && dr["ORDNO"].ToString() == sampleName && dr["METHOD"].ToString() == m).Select(dr => dr["SAMPLEDESCRIPTION"].ToString()).First();
                                bt.TestCode = dtBatch.AsEnumerable().Where(dr => dr["TESTNO"].ToString() == test && dr["ORDNO"].ToString() == sampleName && dr["METHOD"].ToString() == m).Select(dr => dr["TESTCODE"].ToString()).First();
                                bt.PrepComments = dtBatch.AsEnumerable().Where(dr => dr["TESTNO"].ToString() == test && dr["ORDNO"].ToString() == sampleName && dr["METHOD"].ToString() == m).Select(dr => dr["COMMENTS"].ToString()).First();
                                bt.SPCode = dtBatch.AsEnumerable().Where(dr => dr["TESTNO"].ToString() == test && dr["ORDNO"].ToString() == sampleName && dr["METHOD"].ToString() == m).Select(dr => dr["SP_CODE"].ToString()).First();
                                bt.ServGrp = dtBatch.AsEnumerable().Where(dr => dr["TESTNO"].ToString() == test && dr["ORDNO"].ToString() == sampleName && dr["METHOD"].ToString() == m).Select(dr => dr["SERVGRP"].ToString()).First();
                                bt.Method = m;// dtBatch.AsEnumerable().Where(dr => dr["TESTNO"].ToString() == test && dr["ORDNO"].ToString() == sampleName).Select(dr => dr["METHOD"].ToString()).First();

                                page2Samp.Tests.Add(bt);
                            }
                        }
                    }
                    if (batchTest.sampsForBatch.Count() > 0 && TestsNeedingBatch.Where(tt => tt.TestNameAndMethod == batchTest.TestNameAndMethod).FirstOrDefault() == null)
                    {
                        TestsNeedingBatch.Add(batchTest);
                    }
                }
            }

            rlvRunTests.DataSource = null;
            rlvRunTests.DataSource = TestsNeedingBatch;
            rlvRunTests.DisplayMember = "TestNameAndMethod";
        }

        private void rlvAvailableBatch_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right) //right click to add batch
            {
                RadMenuItem rmiAddBatch = new RadMenuItem("Add Batch");
                rmiAddBatch.Click += new EventHandler(AddNewBatch);

                RadContextMenu rmi = new RadContextMenu();
                rmi.Items.Add(rmiAddBatch);

                rmi.Show(Control.MousePosition);
            }
        }

        private void AddNewBatch(object sender, EventArgs e)
        {
            //add batch
            testPage2 = (Test)rlvRunTests.SelectedItem.DataBoundItem;
            string sPrepTMName = testPage2.PrepTName;
            string sServgrp = testPage2.sampsForBatch[0].Tests[0].ServGrp;
            // string sMatCode;

            BasicFunctions.GetData("Update LIMSCOUNTERS set LIMSCOUNTER = LIMSCOUNTER + 1 where TABLNAME = 'PREPRUNS' and FLDNAME = 'PREPRUNNO'");
            DataTable dt1 = BasicFunctions.GetData("Select LIMSCOUNTER from LIMSCOUNTERS where TABLNAME = 'PREPRUNS' and FLDNAME = 'PREPRUNNO'");
            DataTable dt2 = BasicFunctions.GetData(@"Insert Into PREPRUNS
                (PREPRUNNO,STATUS,DISPSTS,PREPTMNAME,DEPT,ASSIGNEDTO,DATECREATED,SERVGRP,PREPDATE,PREPTIME,CREATEDFROM) 
                Values 
                (" + dt1.Rows[0][0].ToString() + ",'Draft','Draft','" + sPrepTMName + "','" + User.Lab + "','" + User.UserName + "','" + DateTime.Now.ToString("dd-MMM-yy") + "','" + sServgrp + "','" + DateTime.Now.ToString("dd-MMM-yy") + "','" + DateTime.Now.ToString("HH:mm tt") + "','StarLIMS')");
            BasicFunctions.GetData("Update Prepruns set TMCODE=(select TMCODE from PrepMethods where preptmname=prepruns.preptmname and dept='" + User.Lab + "') where preprunno='" + dt1.Rows[0][0].ToString() + "'");
            DataTable dt4 = BasicFunctions.GetData("Select stepcode from testwfsteps where exists(select tmcode from prepmethods where tmcode=testwfsteps.tmcode and preptmname='" + sPrepTMName + "' and dept='" + User.Lab + "') and sorter=1");

            DataTable dt3 = BasicFunctions.GetData("Select MATCODE from sample_programs where exists(select 1 from ordtask, preptasks where ordtask.ordno=preptasks.ordno and ordtask.testcode=preptasks.testcode and preprunno=-1 and preptasks.preptmname='" + sPrepTMName + "' and ordtask.servgrp='" + sServgrp + "' and preptasks.dept='" + User.Lab + "' and prepts='Need Prep' and TS='Hold' and Authorizationstatus in ('Authorized','Prep/Hold') and sp_code = sample_programs.sp_code)");

            BasicFunctions.GetData("update prepruns set stepcode='" + dt4.Rows[0][0].ToString() + "',configurationmatrix='" + dt3.Rows[0][0].ToString() + "' where preprunno='" + dt1.Rows[0][0].ToString() + "'");
            GetSampsandBatches();
        }

        private void rtbSampleComments_Leave(object sender, EventArgs e)
        {
            radLabel10.Hide();
            //save comment
            if (rtbSampleComments.Text.Trim() != ((Sample)rlvSamples.SelectedItem.DataBoundItem).SampleDescription)
            {
                ((Sample)rlvSamples.SelectedItem.DataBoundItem).SampleDescription = rtbSampleComments.Text;
                BasicFunctions.GetData("Update orders set sampledescription='" + ((Sample)rlvSamples.SelectedItem.DataBoundItem).SampleDescription + "' where ordno='" + ((Sample)rlvSamples.SelectedItem.DataBoundItem).SampleName + "'");
            }
        }

        private void rbBatchQC_Click(object sender, EventArgs e)
        {
            if (rgvSampsinBatch.SelectedRows.Count() > 1)
            {
                MessageBox.Show("Select only one item to add QC");
            }
            else if (rgvSampsinBatch.SelectedRows.Count() > 0)
            {
                //get KQs and add to database
                testPage2 = (Test)rgvSampsinBatch.SelectedRows[0].DataBoundItem;
                if (testPage2.SampleName.StartsWith("KQ") || (testPage2.Samp != null && (testPage2.Samp.ParentName != null && testPage2.Samp.ParentName != "")))
                {
                    MessageBox.Show("Must select sample (not QC item) to add QC");
                }
                else
                {
                    List<string> QC = new List<string>();
                    //DataTable dt = BasicFunctions.GetData("Select qctype from depttestprop,qctemplatedetails where depttestprop.templateid = qctemplatedetails.templateid and sp_Code =' " + testPage2.SPCode + "' and TestCode = '" + testPage2.TestCode + "' and OWNER = 'KELSO' and exists(Select 1 from QC_TYPES where QCTYPE = QCTEMPLATEDETAILS.QCTYPE and UPDPARENT = 'Y') order by QCType");
                    DataTable dt = BasicFunctions.GetData("Select qctype from depttestprop,qctemplatedetails where depttestprop.templateid = qctemplatedetails.templateid and sp_Code =' " + testPage2.SPCode + "' and TestCode = '" + testPage2.TestCode + "' and OWNER = '" + User.Lab + "' order by QCType");
                    AddQC addQC = new AddQC(dt, rlvAvailableBatch.SelectedItem.Text, ((Test)rgvSampsinBatch.SelectedRows[0].DataBoundItem).SampleName);
                    addQC.ShowDialog();
                    QC = AddQC.QCtoAdd;
                    List<string> modifiedQC = new List<string>();
                    if (QC.Count > 0)
                    {
                        foreach (string q in QC)
                        {
                            if (q == "DMS")
                            {
                                modifiedQC.Add("MS");
                            }
                            if (q == "QUAD")
                            {
                                modifiedQC.Add("TRP");
                                modifiedQC.Add("DUP");
                            }
                            if (q == "TRP")
                            {
                                modifiedQC.Add("DUP");
                            }
                            if (q == "DLCS")
                            {
                                modifiedQC.Add("LCS");
                            }
                        }

                        foreach (string m in modifiedQC)
                        {
                            if (QC.Contains(m))
                            {
                                QC.Remove(m);
                            }
                        }

                        foreach (string s in QC)
                        {
                            BasicFunctions.GetData(@"Call RunCreation.AddQCSample(" + rlvAvailableBatch.SelectedItem.Text + ", 'PREP', '" + s + "' , '" + User.Lab + "', '" + User.UserName + "', '" + ((Test)rgvSampsinBatch.CurrentRow.DataBoundItem).TestCode + "', '" + ((Test)rgvSampsinBatch.CurrentRow.DataBoundItem).SampleName + "', 'Below')");
                        }
                        rgvSampsinBatch.DataSource = null;
                        string batch = rlvAvailableBatch.SelectedItem.Text;
                        GetCurrentBatchItems(batch);
                    }
                }
            }
            else
            {
                MessageBox.Show("Must select item to add QC");
            }
        }

        private void rlvSamples_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                RadMenuItem rmiRefresh = new RadMenuItem("Refresh List");
                rmiRefresh.Click += new EventHandler(RefreshList);

                RadContextMenu rmi = new RadContextMenu();
                rmi.Items.Add(rmiRefresh);

                rmi.Show(Control.MousePosition);
            }
        }

        private void RefreshList(object sender, EventArgs e)
        {
            MakeTestList();
        }

        private void rlvSampsForBatching_SelectedItemsChanged(object sender, EventArgs e)
        {
            int n = rlvSampsForBatching.SelectedItems.Count();
            if (n > 0)
            {

                rbAdd.Text = n.ToString() + " -->";
            }
            else
            {
                rbAdd.Text = "-->";
            }

            rgvSampsinBatch.MasterTemplate.BestFitColumns();
        }

        private void rlvSampsForBatching_VisualItemFormatting(object sender, ListViewVisualItemEventArgs e)
        {
            if (e.VisualItem.Text.Contains('-'))
            {
                string Folder = e.VisualItem.Text.Remove(e.VisualItem.Text.IndexOf('-'));

                testPage2 = (Test)rlvRunTests.SelectedItem.DataBoundItem;

                int n = testPage2.Folders.IndexOf(Folder);

                //if (!e.VisualItem.Selected)
                //{
                //    if (n % 2 == 0)
                //    {
                //        e.VisualItem.BackColor = Color.White;
                //        e.VisualItem.BackColor2 = Color.White;
                //        e.VisualItem.BackColor3 = Color.White;
                //        e.VisualItem.BackColor4 = Color.White;
                //    }
                //    else
                //    {
                //        e.VisualItem.BackColor = Color.Gray;
                //        e.VisualItem.BackColor2 = Color.Gray;
                //        e.VisualItem.BackColor3 = Color.Gray;
                //        e.VisualItem.BackColor4 = Color.Gray;
                //    }
                //}
            }
            //if (QCTest.Where(t => t.Samp != null && t.Samp.ParentName == e.VisualItem.Text && t.TestName == ((Test)rlvRunTests.SelectedItem.DataBoundItem).TestName && t.Method == ((Test)rlvRunTests.SelectedItem.DataBoundItem).Method).FirstOrDefault() != null)
            //{
            //    e.VisualItem.ForeColor = Color.OrangeRed;
            //}
            //else
            //{
            //    e.VisualItem.ForeColor = Color.Black;
            //}
        }

        private void rbAdd_Click(object sender, EventArgs e)
        {
            try
            {
            if (rlvSampsForBatching.SelectedItems.Count() > 0)
            {
                if (rlvAvailableBatch.Items.Count == 0)
                {
                    MessageBox.Show("Must create new batch.");
                }
                else
                {
                    //add to radgridview on right hand side of screen
                    if (rlvAvailableBatch.SelectedItem != null)
                    {

                        foreach (ListViewDataItem lvdi in rlvSampsForBatching.SelectedItems)
                        {
                            Test bt = ((Sample)lvdi.DataBoundItem).Tests[0];
                            sampsTest.Add(bt);
                            Sample s = TestsNeedingBatch.Where(t => t.TestName == bt.TestName && t.Method == bt.Method).First().sampsForBatch.Where(sa => sa.SampleName == bt.SampleName).First();
                            testPage2.TestCode = TestsNeedingBatch.Where(t => t.TestName == bt.TestName && t.Method == bt.Method).First().sampsForBatch.Where(sa => sa.SampleName == bt.SampleName).First().Tests.Select(t => t.TestCode).First();
                            TestsNeedingBatch.Where(t => t.TestName == bt.TestName).First().sampsForBatch.Remove(s);
                            //DataTable dt = BasicFunctions.GetData("Select * from prepruns where preprunno=257299");
                            BasicFunctions.GetData("call RunCreation.AddSampleToPrepRun(" + rlvAvailableBatch.SelectedItem.Text + ", '" + s.SampleName + "', " + bt.TestCode + ")");

                            bt = TestsNeedingBatch.Where(t => t.TestName == bt.TestName && t.Method == bt.Method).First();
                            bt.sampsForBatch.Remove(s);
                        }
                    }

                    rlvSampsForBatching.DataSource = null;
                    rlvSampsForBatching.DataSource = TestsNeedingBatch.Where(btest => btest.Method == ((Test)rlvRunTests.SelectedItem.DataBoundItem).Method && btest.TestName == ((Test)rlvRunTests.SelectedItem.DataBoundItem).TestName.ToString()).First().sampsForBatch.ToList().Where(sampp => !sampp.SampleName.StartsWith("KQ")).Distinct().ToList();
                    rlvSampsForBatching.DisplayMember = "SampleName";
                    string batch = rlvAvailableBatch.SelectedItem.Text;
                    GetCurrentBatchItems(batch);
                    rbAdd.Text = "-->";
                }

                //update folder list! AND remove test from list if all samps are gone
                //((Test)rlvRunTests.SelectedItem.DataBoundItem).Folders = TestsNeedingBatch.Where(
            }
        }
            catch(Exception ex)
            {
                MessageBox.Show("Error adding samples to batch: " + ex.Message);
            }
            rgvSampsinBatch.MasterTemplate.BestFitColumns();
        }

        private void GetCurrentBatchItems(string batch)
        {
            radLabel9.Text = batch;
            radLabel9.Show();
            //string batch = rlvAvailableBatch.SelectedItem.Value.ToString();
            sampsTest = new List<Test>();
            dtRunNos = BasicFunctions.GetData("select distinct orders.sp_code,preptasks.testcode,prepduedate,pareantordno,preptasks.sampweight,preptasks.sampamntunits,ordtask.qctype,ordtask.bottleid,dispordno,ordtask.ordno,preptasks.preprunno,TestNo,ordtask.testcode,prepRunno,ordtask.SpecNo,specsetversion,orders.sampledescription,preptasks.comments from orders,ordtask,preptasks where orders.ordno = ordtask.ordno and ordtask.testcode = preptasks.testcode and ordtask.ordno = preptasks.ordno and Preprunno in (" + batch + ")  order by pareantordno");


            foreach (DataRow dr in dtRunNos.Rows)
            {
                QCSample bSamp = new QCSample();
                bSamp.SampleName = dr["ORDNO"].ToString();
                if (dr["PAREANTORDNO"].ToString() != "")
                {
                    bSamp.ParentName = dr["PAREANTORDNO"].ToString();
                }
                if (bSamp.ParentName == "" && dr["QCTYPE"].ToString() == "N/A")
                {
                    bSamp.DisplayName = bSamp.SampleName;
                }
                else if (bSamp.ParentName == "" && dr["QCTYPE"].ToString() != "N/A")
                {
                    bSamp.DisplayName = bSamp.SampleName + ' ' + dr["QCTYPE"].ToString() + "\r\n" + bSamp.SampleName;
                }
                else
                {
                    bSamp.DisplayName = bSamp.ParentName + ' ' + dr["QCTYPE"].ToString() + "\r\n" + bSamp.SampleName;
                }
                Test thisTest = new Test();
                thisTest.Mass = dr["SAMPWEIGHT"].ToString();
                thisTest.Unit = dr["SAMPAMNTUNITS"].ToString();
                thisTest.TestName = dr["TESTNO"].ToString();
                thisTest.SampleComments = dr["SAMPLEDESCRIPTION"].ToString();
                thisTest.PrepComments = dr["COMMENTS"].ToString();
                thisTest.TestCode = dr["TESTCODE"].ToString();
                thisTest.SPCode = dr["SP_CODE"].ToString();
                thisTest.Type = dr["QCTYPE"].ToString();
                thisTest.Method = ((Test)rlvRunTests.SelectedItem.DataBoundItem).Method;
                if (dr["BOTTLEID"].ToString().Contains('.'))
                {
                    thisTest.Container = dr["BOTTLEID"].ToString().Remove(0, dr["BOTTLEID"].ToString().IndexOf('.'));
                }
                else
                {
                    thisTest.Container = dr["BOTTLEID"].ToString();
                }
                if (dr["PREPDUEDATE"].ToString() != "")
                {
                    thisTest.DueDate = Convert.ToDateTime(dr["PREPDUEDATE"].ToString()).ToString("MMM-dd-yyyy");
                }
                thisTest.SampleName = bSamp.DisplayName;
                thisTest.Samp = bSamp;

                sampsTest.Add(thisTest);
            }


            //if (add) //add more samples (button clicked to get here)
            //{
            //if (rlvAvailableBatch.SelectedItem != null)
            //    {

            //    foreach (ListViewDataItem lvdi in rlvSampsForBatching.SelectedItems)
            //    {
            //        Test bt = ((Sample)lvdi.DataBoundItem).Tests[0];
            //        sampsTest.Add(bt);
            //        Sample s = TestsNeedingBatch.Where(t => t.TestName == bt.TestName && t.Method == bt.Method).First().sampsForBatch.Where(sa => sa.SampleName == bt.SampleName).First();
            //        testPage2.TestCode = TestsNeedingBatch.Where(t => t.TestName == bt.TestName && t.Method == bt.Method).First().sampsForBatch.Where(sa => sa.SampleName == bt.SampleName).First().Tests.Select(t => t.TestCode).First();
            //        TestsNeedingBatch.Where(t => t.TestName == bt.TestName).First().sampsForBatch.Remove(s);
            //        //DataTable dt = BasicFunctions.GetData("Select * from prepruns where preprunno=257299");
            //        BasicFunctions.GetData("call RunCreation.AddSampleToPrepRun(" + rlvAvailableBatch.SelectedItem.Text + ", '" + s.SampleName + "', " + bt.TestCode + ")");

            //get QC associated with parents
            //if (QCTest.Where(t => t.TestName == bt.TestName && t.Samp.ParentName == bt.SampleName && t.Method == bt.Method).ToList() != null)
            //{
            //    bool hasDMS = false;
            //    bool hasTRIP = false;
            //    bool hasQUAD = false;
            //    Test QUAD = new Test();
            //    Test TRIP = new Test();
            //    Test DMS = new Test();
            //    Test qt = new Test();
            //    Test qt2 = new Test();
            //    List<Test> thislist = QCTest.Where(t => t.TestName == bt.TestName && t.Samp.ParentName == bt.SampleName && t.Method == bt.Method).ToList().OrderBy(t => t.TypeNo).ToList().OrderBy(t => t.Number).ToList();
            //    foreach (Test QT in thislist)//QCTest.Where(t => t.TestName == bt.TestName && t.Samp.ParentName == bt.SampleName && t.Method == bt.Method).ToList().OrderBy(t => t.TypeNo).ToList())
            //    {

            //        //make KQ number
            //        //sampsTest.Add(QT);
            //        if (QT.Type == "MS")
            //        {
            //            if (thislist.Where(t => t.TestName == bt.TestName && t.Method == bt.Method && t.Samp.ParentName == bt.SampleName && t.Type == "DMS").FirstOrDefault() != null)
            //            {
            //                if (thislist.Where(t => t.TestName == bt.TestName && t.Method == bt.Method && t.Samp.ParentName == bt.SampleName && t.Type == "DMS" && t.Number > 0).FirstOrDefault() == null)
            //                {
            //                    DMS = thislist.Where(t => t.TestName == bt.TestName && t.Method == bt.Method && t.Samp.ParentName == bt.SampleName && t.Type == "DMS").FirstOrDefault();
            //                }
            //                else
            //                {
            //                    DMS = thislist.Where(t => t.TestName == bt.TestName && t.Method == bt.Method && t.Samp.ParentName == bt.SampleName && t.Type == "DMS" && t.Number == QT.Number + 1).FirstOrDefault();
            //                }
            //                qt = QT;
            //                if (DMS != null)
            //                {
            //                    hasDMS = true;
            //                }
            //                // continue;
            //            }
            //        }
            //        if (QT.Type == "DUP")
            //        {
            //            if (thislist.Where(t => t.TestName == bt.TestName && t.Method == bt.Method && t.Samp.ParentName == bt.SampleName && t.Type == "QUAD").FirstOrDefault() != null)
            //            {
            //                qt2 = QT;
            //                if (thislist.Where(t => t.TestName == bt.TestName && t.Method == bt.Method && t.Samp.ParentName == bt.SampleName && t.Type == "QUAD" && t.Number > 0).FirstOrDefault() == null)
            //                {
            //                    TRIP = thislist.Where(t => t.TestName == bt.TestName && t.Method == bt.Method && t.Samp.ParentName == bt.SampleName && t.Type == "TRP").FirstOrDefault();
            //                    QUAD = thislist.Where(t => t.TestName == bt.TestName && t.Method == bt.Method && t.Samp.ParentName == bt.SampleName && t.Type == "QUAD").FirstOrDefault();
            //                }
            //                else
            //                {
            //                    TRIP = thislist.Where(t => t.TestName == bt.TestName && t.Method == bt.Method && t.Samp.ParentName == bt.SampleName && t.Type == "TRP" && t.Number == QT.Number + 1).FirstOrDefault();
            //                    QUAD = thislist.Where(t => t.TestName == bt.TestName && t.Method == bt.Method && t.Samp.ParentName == bt.SampleName && t.Type == "QUAD" && t.Number == QT.Number + 2).FirstOrDefault();
            //                }
            //                if (QUAD != null)
            //                {
            //                    hasQUAD = true;
            //                }
            //                else if (QUAD == null && TRIP != null)
            //                {
            //                    qt = QT;
            //                    hasTRIP = true;
            //                }
            //            }
            //            else if (thislist.Where(t => t.TestName == bt.TestName && t.Samp.ParentName == bt.SampleName && t.Type == "TRP").FirstOrDefault() != null)
            //            {
            //                qt = QT;
            //                if (thislist.Where(t => t.TestName == bt.TestName && t.Samp.ParentName == bt.SampleName && t.Type == "TRP" && t.Number > 0).FirstOrDefault() == null)
            //                {
            //                    TRIP = thislist.Where(t => t.TestName == bt.TestName && t.Method == bt.Method && t.Samp.ParentName == bt.SampleName && t.Type == "TRP").FirstOrDefault();
            //                }
            //                else
            //                {
            //                    TRIP = thislist.Where(t => t.TestName == bt.TestName && t.Method == bt.Method && t.Samp.ParentName == bt.SampleName && t.Type == "TRP" && t.Number == QT.Number + 1).FirstOrDefault();
            //                }
            //                if (TRIP != null)
            //                {
            //                    hasTRIP = true;
            //                }
            //            }
            //        }
            //        if (QT.Type == "TRP" && hasQUAD)
            //        {
            //            continue;
            //        }
            //        if ((!hasDMS && QT.Type == "MS") || (QT.Type == "DUP" && !hasQUAD && !hasTRIP))
            //        {
            //            BasicFunctions.GetData(@"Call RunCreation.AddQCSample(" + rlvAvailableBatch.SelectedItem.Text + ", 'PREP', '" + QT.Type + "' , 'KELSO', '" + User.UserName + "', '" + QT.TestCode + "', '" + QT.Samp.ParentName + "', 'Below')");
            //            DataTable dt = BasicFunctions.GetData("Select ordno from preptasks where preprunno = '" + rlvAvailableBatch.SelectedItem.Text + "' order by preprunno");
            //            List<string> KQs = new List<string>();
            //            foreach (DataRow dr in dt.Rows)
            //            {
            //                if (dr[0].ToString().StartsWith("KQ"))
            //                {
            //                    KQs.Add(dr[0].ToString());
            //                }
            //            }
            //            KQs = KQs.OrderBy(st => Convert.ToInt32(st.Remove(0, st.IndexOf('-') + 1))).ToList();
            //            string KQ = KQs.Last();
            //            QCUpdates(QT, KQ);
            //            //string unit = "";
            //            //if (QT.Unit != null)
            //            //{
            //            //    unit = QT.Unit;
            //            //}
            //            //if (QT.Mass == null || QT.Mass == "")
            //            //{
            //            //    BasicFunctions.GetData("Update preptasks set sampweight=null,sampamntunits='" + unit + "' where testcode = '" + QT.TestCode + "' and ordno='" + KQ + "'");
            //            //    BasicFunctions.GetData("Update preptasks set comments='" + QT.PrepComments + "' where testcode = '" + QT.TestCode + "' and ordno='" + KQ + "'");
            //            //}
            //            //else
            //            //{
            //            //    BasicFunctions.GetData("Update preptasks set sampweight='" + QT.Mass + "',sampamntunits='" + unit + "' where testcode = '" + QT.TestCode + "' and ordno='" + KQ + "'");
            //            //    BasicFunctions.GetData("Update preptasks set comments='" + QT.PrepComments + "' where testcode = '" + QT.TestCode + "' and ordno='" + KQ + "'");
            //            //}
            //            //remove from xml
            //            //QCTest.Remove(QT);
            //            //SaveQCWeights();
            //            //tryXML = 0;
            //        }
            //        else if (hasDMS)
            //        {       //deal with DMS first
            //            BasicFunctions.GetData(@"Call RunCreation.AddQCSample(" + rlvAvailableBatch.SelectedItem.Text + ", 'PREP', '" + DMS.Type + "' , 'KELSO', '" + User.UserName + "', '" + DMS.TestCode + "', '" + DMS.Samp.ParentName + "', 'Below')");
            //            DataTable dt = BasicFunctions.GetData("Select ordno from preptasks where preprunno = '" + rlvAvailableBatch.SelectedItem.Text + "' order by preprunno");
            //            List<string> KQs = new List<string>();
            //            foreach (DataRow dr in dt.Rows)
            //            {
            //                if (dr[0].ToString().StartsWith("KQ"))
            //                {
            //                    KQs.Add(dr[0].ToString());
            //                }
            //            }
            //            KQs = KQs.OrderBy(st => Convert.ToInt32(st.Remove(0, st.IndexOf('-') + 1))).ToList();
            //            string KQ = KQs.Last();
            //            QCUpdates(DMS, KQ);
            //            //string unit = "";
            //            //if (DMS.Unit != null)
            //            //{
            //            //    unit = QT.Unit;
            //            //}
            //            //if (DMS.Mass == null || DMS.Mass == "")
            //            //{
            //            //    BasicFunctions.GetData("Update preptasks set sampweight=null,sampamntunits='" + unit + "' where testcode = '" + DMS.TestCode + "' and ordno='" + KQ + "'");
            //            //    BasicFunctions.GetData("Update preptasks set comments='" + DMS.PrepComments + "' where testcode = '" + DMS.TestCode + "' and ordno='" + KQ + "'");
            //            //}
            //            //else
            //            //{
            //            //    BasicFunctions.GetData("Update preptasks set sampweight='" + DMS.Mass + "',sampamntunits='" + unit + "' where testcode = '" + DMS.TestCode + "' and ordno='" + KQ + "'");
            //            //    BasicFunctions.GetData("Update preptasks set comments='" + DMS.PrepComments + "' where testcode = '" + DMS.TestCode + "' and ordno='" + KQ + "'");
            //            //}
            //            //remove from xml
            //            //QCTest.Remove(DMS);
            //            //SaveQCWeights();
            //            //tryXML = 0;
            //            //now ms
            //            KQ = KQs[KQs.Count - 2];
            //            QCUpdates(qt, KQ);
            //            //unit = "";
            //            //if (qt.Unit != null)
            //            //{
            //            //    unit = qt.Unit;
            //            //}
            //            //if (qt.Mass == null || qt.Mass == "")
            //            //{
            //            //    BasicFunctions.GetData("Update preptasks set sampweight=null,sampamntunits='" + unit + "' where testcode = '" + qt.TestCode + "' and ordno='" + KQ + "'");
            //            //    BasicFunctions.GetData("Update preptasks set comments='" + qt.PrepComments + "' where testcode = '" + qt.TestCode + "' and ordno='" + KQ + "'");
            //            //}
            //            //else
            //            //{
            //            //    BasicFunctions.GetData("Update preptasks set sampweight='" + qt.Mass + "',sampamntunits='" + unit + "' where testcode = '" + qt.TestCode + "' and ordno='" + KQ + "'");
            //            //    BasicFunctions.GetData("Update preptasks set comments='" + qt.PrepComments + "' where testcode = '" + qt.TestCode + "' and ordno='" + KQ + "'");
            //            //}
            //            //remove from xml
            //            //QCTest.Remove(qt);
            //            //SaveQCWeights();
            //            //tryXML = 0;
            //            hasDMS = false;
            //        }

            //        else if (hasTRIP)
            //        {
            //            //deal with trip first
            //            BasicFunctions.GetData(@"Call RunCreation.AddQCSample(" + rlvAvailableBatch.SelectedItem.Text + ", 'PREP', '" + TRIP.Type + "' , 'KELSO', '" + User.UserName + "', '" + TRIP.TestCode + "', '" + TRIP.Samp.ParentName + "', 'Below')");
            //            DataTable dt = BasicFunctions.GetData("Select ordno from preptasks where preprunno = '" + rlvAvailableBatch.SelectedItem.Text + "' order by preprunno");
            //            List<string> KQs = new List<string>();
            //            foreach (DataRow dr in dt.Rows)
            //            {
            //                if (dr[0].ToString().StartsWith("KQ"))
            //                {
            //                    KQs.Add(dr[0].ToString());
            //                }
            //            }
            //            KQs = KQs.OrderBy(st => Convert.ToInt32(st.Remove(0, st.IndexOf('-') + 1))).ToList();
            //            string KQ = KQs.Last();
            //            QCUpdates(TRIP, KQ);
            //            //string unit = "";
            //            //if (TRIP.Unit != null)
            //            //{
            //            //    unit = qt.Unit;
            //            //}
            //            //if (TRIP.Mass == null || TRIP.Mass == "")
            //            //{
            //            //    BasicFunctions.GetData("Update preptasks set sampweight=null,sampamntunits='" + unit + "' where testcode = '" + TRIP.TestCode + "' and ordno='" + KQ + "'");
            //            //    BasicFunctions.GetData("Update preptasks set comments='" + TRIP.PrepComments + "' where testcode = '" + TRIP.TestCode + "' and ordno='" + KQ + "'");
            //            //}
            //            //else
            //            //{
            //            //    BasicFunctions.GetData("Update preptasks set sampweight='" + TRIP.Mass + "',sampamntunits='" + unit + "' where testcode = '" + TRIP.TestCode + "' and ordno='" + KQ + "'");
            //            //    BasicFunctions.GetData("Update preptasks set comments='" + TRIP.PrepComments + "' where testcode = '" + TRIP.TestCode + "' and ordno='" + KQ + "'");
            //            //}
            //            //remove from xml
            //            //QCTest.Remove(TRIP);
            //            //SaveQCWeights();
            //            //tryXML = 0;
            //            //now dup
            //            KQ = KQs[KQs.Count - 2];
            //            QCUpdates(qt, KQ);
            //            //unit = "";
            //            //if (qt.Unit != null)
            //            //{
            //            //    unit = qt.Unit;
            //            //}
            //            //if (qt.Mass == null || qt.Mass == "")
            //            //{
            //            //    BasicFunctions.GetData("Update preptasks set sampweight=null,sampamntunits='" + unit + "' where testcode = '" + qt.TestCode + "' and ordno='" + KQ + "'");
            //            //    BasicFunctions.GetData("Update preptasks set comments='" + qt.PrepComments + "' where testcode = '" + qt.TestCode + "' and ordno='" + KQ + "'");
            //            //}
            //            //else
            //            //{
            //            //    BasicFunctions.GetData("Update preptasks set sampweight='" + qt.Mass + "',sampamntunits='" + unit + "' where testcode = '" + qt.TestCode + "' and ordno='" + KQ + "'");
            //            //    BasicFunctions.GetData("Update preptasks set comments='" + qt.PrepComments + "' where testcode = '" + qt.TestCode + "' and ordno='" + KQ + "'");
            //            //}
            //            //remove from xml
            //            //QCTest.Remove(qt);
            //            //SaveQCWeights();
            //            //tryXML = 0;
            //            hasTRIP = false;
            //        }
            //        else if (hasQUAD)
            //        {
            //            //deal with quad first
            //            BasicFunctions.GetData(@"Call RunCreation.AddQCSample(" + rlvAvailableBatch.SelectedItem.Text + ", 'PREP', '" + QUAD.Type + "' , 'KELSO', '" + User.UserName + "', '" + QUAD.TestCode + "', '" + QUAD.Samp.ParentName + "', 'Below')");
            //            DataTable dt = BasicFunctions.GetData("Select ordno from preptasks where preprunno = '" + rlvAvailableBatch.SelectedItem.Text + "' order by preprunno");
            //            List<string> KQs = new List<string>();
            //            foreach (DataRow dr in dt.Rows)
            //            {
            //                if (dr[0].ToString().StartsWith("KQ"))
            //                {
            //                    KQs.Add(dr[0].ToString());
            //                }
            //            }
            //            KQs = KQs.OrderBy(st => Convert.ToInt32(st.Remove(0, st.IndexOf('-') + 1))).ToList();
            //            string KQ = KQs.Last();
            //            QCUpdates(QUAD, KQ);
            //            //string unit = "";
            //            //if (QUAD.Unit != null)
            //            //{
            //            //    unit = QUAD.Unit;
            //            //}
            //            //if (QUAD.Mass == null || QUAD.Mass == "")
            //            //{
            //            //    BasicFunctions.GetData("Update preptasks set sampweight=null,sampamntunits='" + unit + "' where testcode = '" + QUAD.TestCode + "' and ordno='" + KQ + "'");
            //            //    BasicFunctions.GetData("Update preptasks set comments='" + QUAD.PrepComments + "' where testcode = '" + QUAD.TestCode + "' and ordno='" + KQ + "'");
            //            //}
            //            //else
            //            //{
            //            //    BasicFunctions.GetData("Update preptasks set sampweight='" + QUAD.Mass + "',sampamntunits='" + unit + "' where testcode = '" + QUAD.TestCode + "' and ordno='" + KQ + "'");
            //            //    BasicFunctions.GetData("Update preptasks set comments='" + QUAD.PrepComments + "' where testcode = '" + QUAD.TestCode + "' and ordno='" + KQ + "'");
            //            //}
            //            //remove from xml
            //            //QCTest.Remove(QUAD);
            //            //SaveQCWeights();
            //            //tryXML = 0;
            //            //now trip
            //            KQ = KQs[KQs.Count - 2];
            //            QCUpdates(TRIP, KQ);
            //            //unit = "";
            //            //if (TRIP.Unit != null)
            //            //{
            //            //    unit = qt.Unit;
            //            //}
            //            //if (TRIP.Mass == null || TRIP.Mass == "")
            //            //{
            //            //    BasicFunctions.GetData("Update preptasks set sampweight=null,sampamntunits='" + unit + "' where testcode = '" + TRIP.TestCode + "' and ordno='" + KQ + "'");
            //            //    BasicFunctions.GetData("Update preptasks set comments='" + TRIP.PrepComments + "' where testcode = '" + TRIP.TestCode + "' and ordno='" + KQ + "'");
            //            //}
            //            //else
            //            //{
            //            //    BasicFunctions.GetData("Update preptasks set sampweight='" + TRIP.Mass + "',sampamntunits='" + unit + "' where testcode = '" + TRIP.TestCode + "' and ordno='" + KQ + "'");
            //            //    BasicFunctions.GetData("Update preptasks set comments='" + TRIP.PrepComments + "' where testcode = '" + TRIP.TestCode + "' and ordno='" + KQ + "'");
            //            //}
            //            //remove from xml
            //            //QCTest.Remove(TRIP);
            //            //SaveQCWeights();
            //            //tryXML = 0;
            //            //now dup
            //            KQ = KQs[KQs.Count - 3];
            //            QCUpdates(qt2, KQ);
            //            //unit = "";
            //            //if (qt2.Unit != null)
            //            //{
            //            //    unit = qt2.Unit;
            //            //}
            //            //if (qt2.Mass == null || qt2.Mass == "")
            //            //{
            //            //    BasicFunctions.GetData("Update preptasks set sampweight=null,sampamntunits='" + unit + "' where testcode = '" + qt2.TestCode + "' and ordno='" + KQ + "'");
            //            //    BasicFunctions.GetData("Update preptasks set comments='" + qt2.PrepComments + "' where testcode = '" + qt2.TestCode + "' and ordno='" + KQ + "'");
            //            //}
            //            //else
            //            //{
            //            //    BasicFunctions.GetData("Update preptasks set sampweight='" + qt2.Mass + "',sampamntunits='" + unit + "' where testcode = '" + qt2.TestCode + "' and ordno='" + KQ + "'");
            //            //    BasicFunctions.GetData("Update preptasks set comments='" + qt2.PrepComments + "' where testcode = '" + qt2.TestCode + "' and ordno='" + KQ + "'");
            //            //}
            //            //remove from xml
            //            //QCTest.Remove(qt2);
            //            //SaveQCWeights();
            //            tryXML = 0;
            //            hasDMS = false;
            //            hasQUAD = false;

            //        }
            //    }
            //}
            //}

            
            //batch = rlvAvailableBatch.SelectedItem.Text;
            //GetCurrentBatchItems(batch);
            //}
            //}

            rgvTests.DataSource = null;
            rgvSampsinBatch.DataSource = sampsTest.OrderBy(st => st.Samp.SampleName).ToList();
            radLabel4.Text = "(" + rgvSampsinBatch.Rows.Where(gvri => !((Test)gvri.DataBoundItem).SampleName.Contains(' ')).ToList().Count().ToString() + " Samples)";
            radLabel4.Show();
        }

        private void QCUpdates(Test t, string KQ)
        {
            string unit = "";
            if (t.Unit != null)
            {
                unit = t.Unit;
            }
            if (t.Mass == null || t.Mass == "")
            {
                BasicFunctions.GetData("Update preptasks set sampweight=null,sampamntunits='" + unit + "' where testcode = '" + t.TestCode + "' and ordno='" + KQ + "'");
                BasicFunctions.GetData("Update preptasks set comments='" + t.PrepComments + "' where testcode = '" + t.TestCode + "' and ordno='" + KQ + "'");
                BasicFunctions.GetData("Update prepresults set INITIALAMOUNT=null,INITIALAMNTUNITS='" + unit + "' where testcode = '" + t.TestCode + "' and ordno='" + KQ + "' and STEPCODE = (Select STEPCODE from PREPRUNSTEPS where PREPRUNNO = PREPRESULTS.PREPRUNNO and SORTER = 1)");
            }
            else
            {
                BasicFunctions.GetData("Update preptasks set sampweight='" + t.Mass + "',sampamntunits='" + unit + "' where testcode = '" + t.TestCode + "' and ordno='" + KQ + "'");
                BasicFunctions.GetData("Update preptasks set comments='" + t.PrepComments + "' where testcode = '" + t.TestCode + "' and ordno='" + KQ + "'");
                BasicFunctions.GetData("Update prepresults set INITIALAMOUNT='" + t.Mass + "',INITIALAMNTUNITS='" + unit + "' where testcode = '" + t.TestCode + "' and ordno='" + KQ + "' and STEPCODE = (Select STEPCODE from PREPRUNSTEPS where PREPRUNNO = PREPRESULTS.PREPRUNNO and SORTER = 1)");
            }

            //QCTest.Remove(t);
            //SaveQCWeights();
            //tryXML = 0;
        }

        private void rgvTests_CellValueChanged(object sender, GridViewCellEventArgs e)
        {
            if (e.Value != null)
            {
                if (e.Value.ToString().StartsWith("G ") || e.Value.ToString().StartsWith("N ") || e.Value.ToString().StartsWith("M "))
                {
                    e.Row.Cells["Mass"].Value = e.Value.ToString().ToUpper().Trim(' ', 'G', '+', 'N', 'M');
                }
                else
                {
                    if (e.ColumnIndex == rgvTests.Columns["Unit"].Index || e.ColumnIndex == rgvTests.Columns["Container"].Index)
                    {
                        Test t = ((Test)e.Row.DataBoundItem);
                        t.SampleName = ((Sample)rlvSamples.SelectedItem.DataBoundItem).SampleName;
                        if (t.Type == null || t.Type == "N/A")
                        {
                            if (t.Container != null && t.Container.Trim() != "")
                            {
                                if (t.Container.Contains('.'))
                                {
                                    t.Container = t.Container.Replace(".", "");
                                }
                                if (t.Container.Length == 2 && BasicFunctions.IsNumeric(t.Container))
                                {
                                    BasicFunctions.GetData("Update ordtask set BottleID='" + t.SampleName + "." + t.Container + "' where testcode = '" + t.TestCode + "' and ordno='" + t.SampleName + "'");
                                }
                            }
                            string unit = "";
                            if (t.Unit != null)
                            {
                                unit = t.Unit;
                                BasicFunctions.GetData("Update preptasks set sampweight=null,sampamntunits='" + unit + "' where testcode = '" + t.TestCode + "' and ordno='" + t.SampleName + "'");
                            }
                        }
                        else
                        {
                            //if (QCTest.Where(te => te.SampleName == t.SampleName && te.Type == t.Type && te.Mass == t.Mass).FirstOrDefault() != null)
                            //{
                            //    QCTest.Remove(QCTest.Where(te => te.SampleName == t.SampleName && te.Type == t.Type && t.Mass == te.Mass).FirstOrDefault());
                            //}
                            if (t.Container != null)
                            {
                                if (t.Container.Length != 2 && !(BasicFunctions.IsNumeric(t.Container)))
                                {
                                    t.Container = null;
                                }
                            }
                            //QCTest.Add(t);
                            //SaveQCWeights();
                            //tryXML = 0;
                        }
                    }
                }
            }
        }

        private void rbBatchComplete_Click(object sender, EventArgs e)
        {
            //update LIMS and print
            if (rlvAvailableBatch.SelectedItem != null)
            {
                //BasicFunctions.GetData("call RunCreation.ResequencePrepRun(" + rlvAvailableBatch.SelectedItem.Text + ")");
                List<Test> lstTests = ((List<Test>)rgvSampsinBatch.DataSource);
                lstTests = lstTests.OrderBy(t => t.Samp.ParentName + t.Samp.SampleName).ToList();
                int i = 1;
                foreach (Test t in lstTests)
                {
                //    DataTable d = BasicFunctions.GetData("Select OPTIMALFINALAMNT, OPTIMALFINALAMNTUNITS from DEPTTESTPROP where TESTCODE = '" + t.TestCode + "' and PROFILE = 'Default' and SP_CODE = '" + t.SPCode + "' and OWNER = '" + User.Lab + "'");
                //    if (d.Rows.Count > 0)
                //    {
                //        BasicFunctions.GetData("Update prepresults set finalamount = '" + d.Rows[0]["OPTIMALFINALAMNT"].ToString() + "',finalamntunits = '" + d.Rows[0]["OPTIMALFINALAMNTUNITS"].ToString() + "' where testcode = '" + t.TestCode + "' and ordno='" + t.SampleName + "' and STEPCODE = (Select STEPCODE from PREPRUNSTEPS where PREPRUNNO = PREPRESULTS.PREPRUNNO and SORTER = 1)");
                //    }
                    t.PrepRun = rlvAvailableBatch.SelectedItem.Text;
                    t.Number = i;
                    i++;
                }
                PrintView pv = new PrintView(lstTests);
                pv.ShowDialog();
            }

        }

        private void rgvTests_CellEndEdit(object sender, GridViewCellEventArgs e)
        {
            if (!String.IsNullOrEmpty(((Test)rgvTests.CurrentRow.DataBoundItem).Mass) && rgvTests.CurrentCell.ColumnIndex == rgvTests.Columns["Mass"].Index)
            {
                if (!((Test)rgvTests.CurrentRow.DataBoundItem).PrepComments.Contains(userLogin))
                {
                    ((Test)rgvTests.CurrentRow.DataBoundItem).PrepComments += userLogin;
                    if (((Test)rgvTests.CurrentRow.DataBoundItem).Type == null || ((Test)rgvTests.CurrentRow.DataBoundItem).Type == "N/A")
                    {
                        BasicFunctions.GetData("Update preptasks set comments='" + ((Test)rgvTests.CurrentRow.DataBoundItem).PrepComments + "' where testcode = '" + ((Test)rgvTests.CurrentRow.DataBoundItem).TestCode + "' and ordno='" + ((Sample)rlvSamples.SelectedItem.DataBoundItem).SampleName + "'");
                    }
                }
            }
            
            if (((Test)rgvTests.CurrentRow.DataBoundItem) != null)
            {
                test = (Test)rgvTests.CurrentRow.DataBoundItem;
                if (test.Type == null || test.Type == "")
                {
                    test.Type = "N/A";
                }
                BasicFunctions.GetData("Call RunCreation.SaveSolidsData('" + test.Samp.ParentName + "', '" + test.TestCode + "', '" + test.Type + "', '" + test.Mass + "', '" + test.Unit + "', '" + test.Container + "', '" + test.PrepComments + "', '" + test.Rep + "')");
                //BasicFunctions.GetData("Call RunCreation.SaveSolidsData('" + test.Samp.ParentName + "', '" + test.TestCode + "', '" + test.Type + "', '" + test.Mass + "', '" + test.Unit + "', '" + test.Container + "', '" + test.PrepComments + "'");
            //    //if (QCTest.Where(t => ((Test)rgvTests.CurrentRow.DataBoundItem).SampleName == t.SampleName && t.Number == ((Test)rgvTests.CurrentRow.DataBoundItem).Number && ((Test)rgvTests.CurrentRow.DataBoundItem).Type == t.Type && t.Method == ((Test)rgvTests.CurrentRow.DataBoundItem).Method && t.Number == ((Test)rgvTests.CurrentRow.DataBoundItem).Number).FirstOrDefault() != null)
            //    //{
            //    //    QCTest.Remove(QCTest.Where(t => ((Test)rgvTests.CurrentRow.DataBoundItem).SampleName == t.SampleName && t.Number == ((Test)rgvTests.CurrentRow.DataBoundItem).Number && ((Test)rgvTests.CurrentRow.DataBoundItem).Type == t.Type && t.Method == ((Test)rgvTests.CurrentRow.DataBoundItem).Method && t.Number == ((Test)rgvTests.CurrentRow.DataBoundItem).Number).FirstOrDefault());
            //    //}
            //    //if (((Test)rgvTests.CurrentRow.DataBoundItem).Container != null)
            //    //{
            //    //    string container = ((Test)rgvTests.CurrentRow.DataBoundItem).Container;
            //    //    if (container.Contains("."))
            //    //    {
            //    //        container.Replace(".", "");
            //    //    }
            //    //    if (container.Length != 2 && !(BasicFunctions.IsNumeric(container)))
            //    //    {
            //    //        ((Test)rgvTests.CurrentRow.DataBoundItem).Container = "";
            //    //    }
            //    //}
            //    //QCTest.Add((Test)rgvTests.CurrentRow.DataBoundItem);
            //    //SaveQCWeights();
            //    //tryXML = 0;
            //    //string sSQL = "SOLIDSPREP";
            //    updatateLIMS();
            }
            ////update cell into LIMS
            //else
            //{
            //    //string sSQL = "PREPTASKS";
            //    updatateLIMS();
            //    //if (rgvTests.CurrentCell.ColumnIndex == rgvTests.Columns["Mass"].Index || rgvTests.CurrentCell.ColumnIndex == rgvTests.Columns["Unit"].Index || rgvTests.CurrentCell.ColumnIndex == rgvTests.Columns["Container"].Index || rgvTests.CurrentCell.ColumnIndex == rgvTests.Columns["TestComments"].Index)
            //    //{
            //    //    string testCode = ((Test)rgvTests.CurrentRow.DataBoundItem).TestCode;
            //    //    string sampName = ((Sample)rlvSamples.SelectedItem.DataBoundItem).SampleName;

            //    //    if (rgvTests.CurrentRow.Cells["Unit"].Value == null)
            //    //    {
            //    //        rgvTests.CurrentRow.Cells["Unit"].Value = "";
            //    //    }

            //    //    if (rgvTests.CurrentRow.Cells["Container"].Value != null && rgvTests.CurrentRow.Cells["Container"].Value.ToString().Trim() != "")
            //    //    {
            //    //        if (rgvTests.CurrentRow.Cells["Container"].Value.ToString().Length == 2 && BasicFunctions.IsNumeric(rgvTests.CurrentRow.Cells["Container"].Value.ToString()))
            //    //        {
            //    //            BasicFunctions.GetData("Update ordtask set BottleId='" + ((Sample)rlvSamples.SelectedItem.DataBoundItem).SampleName + "." + rgvTests.CurrentRow.Cells["Container"].Value.ToString() + "' where testcode = '" + testCode + "' and ordno='" + sampName + "'");
            //    //        }
            //    //    }
            //    //    string unit = "";
            //    //    if (rgvTests.CurrentRow.Cells["Unit"].Value != null)
            //    //    {
            //    //        unit = rgvTests.CurrentRow.Cells["Unit"].Value.ToString();
            //    //    }
            //    //    if (rgvTests.CurrentRow.Cells["Mass"].Value == null || rgvTests.CurrentRow.Cells["Mass"].Value.ToString() == "") //enter null if blank
            //    //    {
            //    //        BasicFunctions.GetData("Update preptasks set sampweight=null,sampamntunits='" + unit + "' where testcode = '" + testCode + "' and ordno='" + sampName + "'");
            //    //    }
            //    //    else if (rgvTests.CurrentRow.Cells["Mass"].Value != null && rgvTests.CurrentRow.Cells["Mass"].Value.ToString() != "") //enter null if blank
            //    //    {
            //    //        BasicFunctions.GetData("Update preptasks set sampweight='" + ((Test)rgvTests.CurrentRow.DataBoundItem).Mass + "',sampamntunits='" + unit + "' where testcode = '" + testCode + "' and ordno='" + sampName + "'");
            //    //        //Test x = ((Test)rgvTests.CurrentRow.DataBoundItem);
            //    //        //x.SampleName = ((Sample)rlvSamples.SelectedItem.DataBoundItem).SampleName;
            //    //        //rptPrepLabel label = new rptPrepLabel(x);
            //    //        //label.DataSource = x;
            //    //        //Printing(label);
            //    //    }
            //    //}
            //}
            if (rgvTests.CurrentCell.ColumnIndex == rgvTests.Columns["Mass"].Index)
            {
                Test x = ((Test)rgvTests.CurrentRow.DataBoundItem);
                x.SampleName = ((Sample)rlvSamples.SelectedItem.DataBoundItem).SampleName;
                rptPrepLabel label = new rptPrepLabel(x);
                //label.DataSource = x;
                Printing(label);
            }
            //move to next row or back to top if on last row
            if (rgvTests.CurrentRow.Index == rgvTests.Rows.Count - 1)
            {
                rgvTests.CurrentRow = rgvTests.Rows[0];
            }
            else
            {
                rgvTests.CurrentRow = rgvTests.Rows[rgvTests.CurrentRow.Index + 1];
            }
        }

        private void updatateLIMS()
        {
            if (rgvTests.CurrentCell.ColumnIndex == rgvTests.Columns["Mass"].Index || rgvTests.CurrentCell.ColumnIndex == rgvTests.Columns["Unit"].Index || rgvTests.CurrentCell.ColumnIndex == rgvTests.Columns["Container"].Index || rgvTests.CurrentCell.ColumnIndex == rgvTests.Columns["TestComments"].Index)
            {
                string testCode = ((Test)rgvTests.CurrentRow.DataBoundItem).TestCode;
                string sampName = ((Sample)rlvSamples.SelectedItem.DataBoundItem).SampleName;

                if (rgvTests.CurrentRow.Cells["Unit"].Value == null)
                {
                    rgvTests.CurrentRow.Cells["Unit"].Value = "";
                }

                if (rgvTests.CurrentRow.Cells["Container"].Value != null && rgvTests.CurrentRow.Cells["Container"].Value.ToString().Trim() != "")
                {
                    if (rgvTests.CurrentRow.Cells["Container"].Value.ToString().Length == 2 && BasicFunctions.IsNumeric(rgvTests.CurrentRow.Cells["Container"].Value.ToString()))
                    {
                     //   BasicFunctions.GetData("Update solidsprep set BottleId='" + ((Sample)rlvSamples.SelectedItem.DataBoundItem).SampleName + "." + rgvTests.CurrentRow.Cells["Container"].Value.ToString() + "' where rowid='" + ((Test)rgvTests.CurrentRow.DataBoundItem).RowID + "'");
                    }
                }
                string unit = "";
                if (rgvTests.CurrentRow.Cells["Unit"].Value != null)
                {
                    unit = rgvTests.CurrentRow.Cells["Unit"].Value.ToString();
                }
                if (rgvTests.CurrentRow.Cells["Mass"].Value == null || rgvTests.CurrentRow.Cells["Mass"].Value.ToString() == "") //enter null if blank
                {
                    //if (sSQL == "PREPTASKS")
                    //{
                    //    BasicFunctions.GetData("Update preptasks set sampweight=null,sampamntunits='" + unit + "' where testcode = '" + testCode + "' and ordno='" + sampName + "'");
                    //}
                    //else if (sSQL == "SOLIDSPREP")
                    //{
                       // BasicFunctions.GetData("Update solidsprep set sampweight=null,sampweightunits='" + unit  +"' where rowid='"+ ((Test)rgvTests.CurrentRow.DataBoundItem).RowID + "'");
                    //}
                }
                else if (rgvTests.CurrentRow.Cells["Mass"].Value != null && rgvTests.CurrentRow.Cells["Mass"].Value.ToString() != "") //enter null if blank
                {
                    //if (sSQL == "PREPTASKS")
                    //{
                    //    BasicFunctions.GetData("Update preptasks set sampweight='" + ((Test)rgvTests.CurrentRow.DataBoundItem).Mass + "',sampamntunits='" + unit + "' where testcode = '" + testCode + "' and ordno='" + sampName + "'");
                    //}
                    //else if (sSQL == "SOLIDSPREP")
                    //{
                      //  BasicFunctions.GetData("Update solidsprep set sampweight='" + ((Test)rgvTests.CurrentRow.DataBoundItem).Mass + "',sampweightunits='" + unit + "' where rowid='" + ((Test)rgvTests.CurrentRow.DataBoundItem).RowID + "'");
                    //}
                    //Test x = ((Test)rgvTests.CurrentRow.DataBoundItem);
                    //x.SampleName = ((Sample)rlvSamples.SelectedItem.DataBoundItem).SampleName;
                    //rptPrepLabel label = new rptPrepLabel(x);
                    //label.DataSource = x;
                    //Printing(label);
                }
            }
        }

        private void Printing(rptPrepLabel l)
        {
            if (User.UserName != "SELDRIDGE")
            {
                PrintDocument pd = new PrintDocument();
                foreach (string sPrinter in PrinterSettings.InstalledPrinters)
                {
                    if (sPrinter.Contains("Soils Label Printer"))
                    {
                        pd.PrinterSettings.PrinterName = sPrinter;
                    }
                }

                ReportProcessor reportProcessor = new ReportProcessor();
                InstanceReportSource iReportSource = new InstanceReportSource();
                iReportSource.ReportDocument = l;
                ReportSource rSource = iReportSource;
                //RenderingResult result = reportProcessor.RenderReport();
                reportProcessor.PrintReport(rSource, pd.PrinterSettings);
            }
        }


        //private void Printing(rptPrepLabel l)
        //{
        //    PrintDocument pd = new PrintDocument();
        //    PrintDialog pdialog = new PrintDialog();
        //    pdialog.Document = pd;
        //    DialogResult dr = pdialog.ShowDialog();
        //    if (dr == DialogResult.OK)
        //    {
        //        pd.PrinterSettings = pdialog.PrinterSettings;
        //    }
           
        //    ReportProcessor reportProcessor = new ReportProcessor();
        //    InstanceReportSource iReportSource = new InstanceReportSource();
        //    iReportSource.ReportDocument = l;
        //    ReportSource rSource = iReportSource;
        //    //RenderingResult result = reportProcessor.RenderReport();
        //    reportProcessor.PrintReport(rSource, pd.PrinterSettings);
        //}

        private void rtbSampleComments_Click(object sender, EventArgs e)
        {
            if (rtbSampleComments.ForeColor == Color.Silver)
            {
                rtbSampleComments.Text = "";
                rtbSampleComments.ForeColor = Color.Black;
            }
        }

        private void rlvRunTests_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                RadMenuItem rmiRefresh = new RadMenuItem("Refresh list");
                rmiRefresh.Click += new EventHandler(refreshList);

                RadContextMenu rmi = new RadContextMenu();
                rmi.Items.Add(rmiRefresh);
                rmi.Show(Control.MousePosition);
            }
        }

        private void refreshList(object sender, EventArgs e)
        {
            MakeTestList();
        }

        //public void GetQCWeights()
        //{
        //    if (File.Exists(@"R:\ALS_Prep\Settings\QC.xml"))
        //    {
        //        XmlSerializer xmlSerializer = new XmlSerializer(typeof(List<Test>));

        //        try
        //        {
        //            using (XmlTextReader reader = new XmlTextReader(@"R:\ALS_Prep\Settings\QC.xml"))
        //            {
        //                try
        //                {
        //                    reader.Read();
        //                    QCTest = (List<Test>)xmlSerializer.Deserialize(reader);
        //                    QCTest = QCTest.Where(t => t.Today.AddMonths(2) > DateTime.Now).ToList();
        //                }
        //                catch (Exception)
        //                {
        //                    if (tryXML < 15)
        //                    {
        //                        tryXML++;
        //                        System.Threading.Thread.Sleep(1000);
        //                        GetQCWeights();
        //                    }
        //                    else
        //                    {
        //                        MessageBox.Show("Error getting QC from file.");
        //                    }
        //                }
        //            }
        //        }
        //        catch (Exception)
        //        {
        //            if (tryXML < 15)
        //            {
        //                tryXML++;
        //                System.Threading.Thread.Sleep(1000);
        //                GetQCWeights();
        //            }
        //            else
        //            {
        //                MessageBox.Show("Error getting QC from file.");
        //            }
        //        }
        //    }
        //}

        //public void SaveQCWeights()
        //{
        //    Cursor.Current = Cursors.WaitCursor;
        //    try
        //    {
        //        XmlSerializer xmlSerializer = new XmlSerializer(QCTest.GetType());

        //        using (XmlTextWriter xmlTextWriter = new XmlTextWriter(@"R:\ALS_Prep\Settings\QC.xml", Encoding.UTF8) { Formatting = Formatting.None })
        //        {
        //            xmlSerializer.Serialize(xmlTextWriter, QCTest);
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        if (tryXML < 15)
        //        {
        //            tryXML++;
        //            System.Threading.Thread.Sleep(1000);
        //            SaveQCWeights();
        //        }
        //        else
        //        {
        //            MessageBox.Show("Error: " + ex.Message + " Could not save QC data");
        //        }
        //    }
        //    Cursor.Current = Cursors.Default;
        //}

        private void rtbSampleComments_TextChanged(object sender, EventArgs e)
        {
            int sizeofComment = rtbSampleComments.Text.Length;
            radLabel10.Text = sizeofComment.ToString() + "/100 characters";
        }

        private void rtbSampleComments_Enter(object sender, EventArgs e)
        {
            int sizeofComment = rtbSampleComments.Text.Length;
            radLabel10.Text = sizeofComment.ToString() + "/100 characters";
            radLabel10.Show();
        }

        private void rbRemove_Click(object sender, EventArgs e)
        {
            if (rgvSampsinBatch.SelectedRows.Count() > 0)
            {
                List<Test> realSamples = new List<Test>();
                List<Test> selectedPlusQC = new List<Test>();
                List<Test> batchQC = new List<Test>();
                List<Test> onlyQCSelected = new List<Test>(); //remove higher QC (ex: DMS higher than MS) if lower QC is selected without parent

                foreach (GridViewRowInfo gv in rgvSampsinBatch.SelectedRows)
                {
                    if (((Test)gv.DataBoundItem).Type == null || ((Test)gv.DataBoundItem).Type == "" || ((Test)gv.DataBoundItem).Type == "N/A")
                    {
                        realSamples.Add((Test)gv.DataBoundItem);
                    }
                    else if (((Test)gv.DataBoundItem).TypeNo >= 0 && (((Test)gv.DataBoundItem).Type != "DUP" || ((Test)gv.DataBoundItem).Type != "TRP" || ((Test)gv.DataBoundItem).Type != "QUAD" || ((Test)gv.DataBoundItem).Type != "MS" || ((Test)gv.DataBoundItem).Type != "DMS"))
                    {
                        realSamples.Add((Test)gv.DataBoundItem);
                    }
                    else
                    {
                        onlyQCSelected.Add((Test)gv.DataBoundItem);
                    }
                }
                foreach (Test t in realSamples)
                {
                    if (rgvSampsinBatch.Rows.Where(gvr => ((Test)gvr.DataBoundItem).Samp.ParentName == t.SampleName).ToList() != null)
                    {
                        foreach (GridViewRowInfo gv in rgvSampsinBatch.Rows.Where(gvr => ((Test)gvr.DataBoundItem).Samp.ParentName == t.SampleName).ToList())
                        {
                            selectedPlusQC.Add((Test)gv.DataBoundItem);
                        }
                    }
                }
                foreach (Test t in onlyQCSelected)
                {
                    if (!selectedPlusQC.Contains(t))
                    {
                        selectedPlusQC.Add(t);
                        if (t.TypeNo < 3) //dup/trip/quad
                        {
                            if (rgvSampsinBatch.Rows.Where(gvd => ((Test)gvd.DataBoundItem).Samp != null && ((Test)gvd.DataBoundItem).Samp.ParentName == t.Samp.ParentName && ((Test)gvd.DataBoundItem).TypeNo > t.TypeNo && ((Test)gvd.DataBoundItem).TypeNo < 3).ToList() != null)
                            {
                                foreach (GridViewRowInfo gv in rgvSampsinBatch.Rows.Where(gvd => ((Test)gvd.DataBoundItem).Samp != null && ((Test)gvd.DataBoundItem).Samp.ParentName == t.Samp.ParentName && ((Test)gvd.DataBoundItem).TypeNo > t.TypeNo && ((Test)gvd.DataBoundItem).TypeNo < 3).ToList())
                                {
                                    if (!selectedPlusQC.Contains((Test)gv.DataBoundItem))
                                    {
                                        selectedPlusQC.Add((Test)gv.DataBoundItem);
                                    }
                                }
                            }
                        }
                        else //ms/dms
                        {
                            if (rgvSampsinBatch.Rows.Where(gvd => ((Test)gvd.DataBoundItem).Samp != null && ((Test)gvd.DataBoundItem).Samp.ParentName == t.Samp.ParentName && ((Test)gvd.DataBoundItem).TypeNo > t.TypeNo).ToList() != null)
                            {
                                foreach (GridViewRowInfo gv in rgvSampsinBatch.Rows.Where(gvd => ((Test)gvd.DataBoundItem).Samp != null && ((Test)gvd.DataBoundItem).Samp.ParentName == t.Samp.ParentName && ((Test)gvd.DataBoundItem).TypeNo > t.TypeNo).ToList())
                                {
                                    if (!selectedPlusQC.Contains((Test)gv.DataBoundItem))
                                    {
                                        selectedPlusQC.Add((Test)gv.DataBoundItem);
                                    }
                                }
                            }
                        }
                    }
                }

                foreach (Test t in realSamples)
                {
                    //regular sample
                    if (t.TypeNo < 0)
                    {
                        BasicFunctions.GetData("Delete from prepresults where testcode ='" + t.TestCode + "' and ordno = '" + t.SampleName + "'");
                        BasicFunctions.GetData("Update preptasks set preprunno=-1 where testcode ='" + t.TestCode + "' and ordno = '" + t.SampleName + "'");
                    }
                    //batch qc
                    else
                    {
                        BasicFunctions.GetData("delete from orders where ordno='" + t.Samp.SampleName + "'");
                    }
                }
                if (selectedPlusQC.Count > 0) //save all to XML
                {
                    DataTable dtKQ = new DataTable();
                    //foreach (Test t in selectedPlusQC.OrderBy(te => te.Samp.SampleName).ToList())
                    //{
                    //    //t.Number = QCTest.Where(tesT => tesT.TestName == t.TestName && t.Samp.ParentName == tesT.Samp.ParentName && tesT.Method == t.Method).ToList().Count;
                    //    t.Today = DateTime.Now;
                    //    t.SampleName = t.SampleName.Remove(t.SampleName.IndexOf(' '));
                    //    t.Samp.DisplayName = t.SampleName;
                    //    //QCTest.Add(t);
                    //    //SaveQCWeights();
                    //    //tryXML = 0;
                    //}
                    foreach (Test t in selectedPlusQC) //delete KQs from lims
                    {
                        BasicFunctions.GetData("delete from orders where ordno='" + t.Samp.SampleName + "'");
                    }
                }
            }
            string batch = rlvAvailableBatch.SelectedItem.Text;
            GetCurrentBatchItems(batch);
            MakeTestList();

            rgvSampsinBatch.MasterTemplate.BestFitColumns();
        }

        private void rgvSampsinBatch_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyData == Keys.Return)
            {
                if (rgvSampsinBatch.CurrentRow.Cells["Mass"].Value != null)
                {
                    test = ((Test)rgvSampsinBatch.CurrentRow.DataBoundItem);// FolderSamps.Samples.Where(s => s.SampleName == ((Test)rgvSampsinBatch.CurrentRow.DataBoundItem).SampleName.ToString()).First().Tests.Where(t => t.TestName == ((Test)rgvSampsinBatch.CurrentRow.DataBoundItem).TestName).First();
                    if (rgvSampsinBatch.CurrentRow.Cells["Unit"].Value != null)
                    {
                        if (rgvSampsinBatch.CurrentRow.Cells["Mass"].Value.ToString() != test.Mass || rgvSampsinBatch.CurrentRow.Cells["Unit"].Value.ToString() != test.Unit)
                        {
                            //sql to insert back into LIMS
                            test.Unit = rgvSampsinBatch.CurrentRow.Cells["Unit"].Value.ToString();
                            test.Mass = rgvSampsinBatch.CurrentRow.Cells["Mass"].Value.ToString();
                        }
                    }
                    else
                    {
                        MessageBox.Show("Enter Units");
                    }
                }
                //scroll to next test in sample's list.  go back to top if on last one
                if (rgvSampsinBatch.CurrentRow.Index == rgvSampsinBatch.Rows[rgvSampsinBatch.Rows.Count - 1].Index)
                {
                    rgvSampsinBatch.CurrentRow = rgvSampsinBatch.Rows[0];
                }
                else
                {
                    rgvSampsinBatch.CurrentRow = rgvSampsinBatch.Rows[rgvSampsinBatch.CurrentRow.Index + 1];
                }
            }
        }

        private void rgvSampsinBatch_CellEndEdit(object sender, GridViewCellEventArgs e)
        {
            //update cell into LIMS
            if (rgvSampsinBatch.CurrentCell.ColumnIndex == rgvSampsinBatch.Columns["Mass"].Index || rgvSampsinBatch.CurrentCell.ColumnIndex == rgvSampsinBatch.Columns["Unit"].Index || rgvSampsinBatch.CurrentCell.ColumnIndex == rgvSampsinBatch.Columns["Container"].Index)
            {
                string testCode = ((Test)rgvSampsinBatch.CurrentRow.DataBoundItem).TestCode;
                string sampName = "";
                if (((Test)rgvSampsinBatch.CurrentRow.DataBoundItem).Samp != null && ((Test)rgvSampsinBatch.CurrentRow.DataBoundItem).Samp.SampleName != null)
                {
                    sampName = ((Test)rgvSampsinBatch.CurrentRow.DataBoundItem).Samp.SampleName;
                }
                else
                {
                    sampName = ((Test)rgvSampsinBatch.CurrentRow.DataBoundItem).SampleName;
                }

                if (rgvSampsinBatch.CurrentRow.Cells["Unit"].Value == null)
                {
                    rgvSampsinBatch.CurrentRow.Cells["Unit"].Value = "";
                }

                if (rgvSampsinBatch.CurrentRow.Cells["Container"].Value != null && rgvSampsinBatch.CurrentRow.Cells["Container"].Value.ToString().Trim() != "")
                {
                    if (rgvSampsinBatch.CurrentRow.Cells["Container"].Value.ToString().Length == 2 && BasicFunctions.IsNumeric(rgvSampsinBatch.CurrentRow.Cells["Container"].Value.ToString()))
                    {
                        string id = "";
                        if (((Test)rgvSampsinBatch.CurrentRow.DataBoundItem).SampleName.Contains(" "))
                        {
                            id = ((Test)rgvSampsinBatch.CurrentRow.DataBoundItem).Samp.ParentName;
                        }
                        else
                        {
                            id = ((Test)rgvSampsinBatch.CurrentRow.DataBoundItem).SampleName;
                        }
                        BasicFunctions.GetData("Update ordtask set BottleIDid='" + id + "." + rgvSampsinBatch.CurrentRow.Cells["Container"].Value.ToString() + "' where testcode = '" + testCode + "' and ordno='" + sampName + "'");
                    }
                    else
                    {
                        MessageBox.Show("Bottle ID must be 2 digits");
                    }
                }
                string unit = "";
                if (rgvSampsinBatch.CurrentRow.Cells["Unit"].Value != null)
                {
                    unit = rgvSampsinBatch.CurrentRow.Cells["Unit"].Value.ToString();
                }
                if (rgvSampsinBatch.CurrentRow.Cells["Mass"].Value == null || rgvSampsinBatch.CurrentRow.Cells["Mass"].Value.ToString() == "") //enter null if blank
                {
                    BasicFunctions.GetData("Update preptasks set sampweight=null,sampamntunits='" + unit + "' where testcode = '" + testCode + "' and ordno='" + sampName + "'");
                }
                else if (rgvSampsinBatch.CurrentRow.Cells["Mass"].Value != null && rgvSampsinBatch.CurrentRow.Cells["Mass"].Value.ToString() != "") //enter null if blank
                {
                    BasicFunctions.GetData("Update preptasks set sampweight='" + ((Test)rgvSampsinBatch.CurrentRow.DataBoundItem).Mass + "',sampamntunits='" + unit + "' where testcode = '" + testCode + "' and ordno='" + sampName + "'");
                }
            }
            //move to next row or back to top if on last row
            if (rgvSampsinBatch.CurrentRow.Index == rgvSampsinBatch.Rows[rgvSampsinBatch.Rows.Count - 1].Index)
            {
                rgvSampsinBatch.CurrentRow = rgvSampsinBatch.Rows[0];
            }
            else
            {
                rgvSampsinBatch.CurrentRow = rgvSampsinBatch.Rows[rgvSampsinBatch.CurrentRow.Index + 1];
            }
        }

        private void rgvSampsinBatch_CellValueChanged(object sender, GridViewCellEventArgs e)
        {
            if (e.Value != null)
            {
                if (e.Value.ToString().StartsWith("G ") || e.Value.ToString().StartsWith("N ") || e.Value.ToString().StartsWith("M "))
                {
                    e.Row.Cells["Mass"].Value = e.Value.ToString().ToUpper().Trim(' ', 'G', '+', 'N', 'M');
                }
            }
        }

        private void rgvTests_CellBeginEdit(object sender, GridViewCellCancelEventArgs e)
        {
            if (e.ColumnIndex == rgvTests.Columns["Mass"].Index)
            {
                if (((Test)rgvTests.CurrentRow.DataBoundItem).Container == null || ((Test)rgvTests.CurrentRow.DataBoundItem).Container == "")
                {
                    MessageBox.Show("Please enter bottle number.");
                }
                if (rgvTests.CurrentRow.Cells["Mass"].Value != null)
                {
                    prevMass = rgvTests.CurrentRow.Cells["Mass"].Value.ToString();
                }
            }
        }

        private void rgvSampsinBatch_Leave(object sender, EventArgs e)
        {
            rgvSampsinBatch.EndEdit();
        }

        private void rbFolderView_Click(object sender, EventArgs e)
        {
            rfFolders folders = new rfFolders();
            folders.ShowDialog();
            if (rtbFolder.Text == "Folder")
            {
                rtbFolder.Text = "";
            }
            rtbFolder.ForeColor = Color.Black;
            rtbFolder.Font = new Font("Segoe", 10, FontStyle.Bold | FontStyle.Regular);
            rtbFolder.Text = folders.FolderChoice;
            if (rtbFolder.Text != "")
            {
                getInitialData(rtbFolder.Text);
            }
        }

        private void rlName_Click(object sender, EventArgs e)
        {
            //login Login = new login();
            //Login.ShowDialog();
            //userLogin = Login.name;
            frmradLogin login = new frmradLogin();
            login.ShowDialog();
            rlName.Text = "Not " + login.ValidUser + "?";
            userLogin = User.UserName + " " + balance;
        }

        private void rbOpenBatch_Click(object sender, EventArgs e)
        {
            rfBatchByNo bbn = new rfBatchByNo();
            bbn.ShowDialog();

            string batchNo = bbn.batchNo;
            if (batchNo != "")
            {
                GetCurrentBatchItems(batchNo); 
            }
            else
            {
                MessageBox.Show("Please enter a Prep Number");
            }
        }

        private void rbAddQC_Click(object sender, EventArgs e)
        {
            BasicFunctions.GetData("Call RunCreation.SolidsAppAddQC("+ rlvAvailableBatch.SelectedItem.Text +", '" + User.Lab + "', '" + User.UserName + "')");
            //dtData = BasicFunctions.GetData("Select * from solidsprep where ordno='K1601234-001'");
            
            rgvSampsinBatch.DataSource = null;
            GetCurrentBatchItems(rlvAvailableBatch.SelectedItem.Value.ToString());
            rgvSampsinBatch.MasterTemplate.BestFitColumns();
        }
    }
}
  
