using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ALS_Prep
{
    public class FolderInfo
    {
        private List<Sample> lstSamples = new List<Sample>();

        public List<Sample> Samples
        {
            get { return lstSamples; }
            set { lstSamples = value; }      
        }
    }

    public class Sample
    {
        string sSampleName;
        string sDisplayName;
        string sParentName = "";
        string sSampleDescription;
        
        private List<Test> lstTests = new List<Test>();

        public List<Test> Tests
        {
            get { return lstTests; }
            set { lstTests = value; }
        }

        public string SampleDescription
        {
            get { return sSampleDescription; }
            set { sSampleDescription = value; }
        }

        public string DisplayName
        {
            get { return sDisplayName; }
            set { sDisplayName = value; } 
        }

        public string ParentName
        {
            get { return sParentName; }
            set { sParentName = value; }
        }

        public string SampleName
        {
            get { return sSampleName; }
            set { sSampleName = value; }
        }
        public bool QC
        {
            get
            {
                if (this.Tests.Where(t => t.Dup != null || t.MS != null).ToList().Count > 0) { return true; }

                else { return false; }
            }
        }
    }

    public class Test
    {
        string sTestName;
        string sTestCode;
        string sMass;
        string sUnit;
        string sDup;
        string sMS;
        string sDMS;
        string sQC;
        string sPrepTName;
        string sType;
        string sSampleName;
        string sPCComments;
        string sPrepComments;
        string sTestComments;
        string sSampleComments;
        string sPrepRun;
        string sContainer;
        string sSampleDescription;
        string dDueDate;
        string sServGrp;
        string sMethod;
        string sRep;
        int iNumber;
        DateTime dToday;
        string sSPCode;
        QCSample sSamp;
        List<Sample> lstSampsForBatch = new List<Sample>();
        List<string> lstFolders = new List<string>();
        string sRowID;

        public string Rep
        {
            get { return sRep; }
            set { sRep = value; }
        }

        public string RowID
        {
            get { return sRowID; }
            set { sRowID = value; }
        }

        public string Method
        {
            get { return sMethod; }
            set { sMethod = value; }
        }

        public string TestNameAndMethod
        {
            get
            {
                if (Method != null && Method != "")
                {
                    return TestName + "  :  " + Method;
                }
                else
                {
                    return TestName;
                }
            }
        }

        public string ServGrp
        {
            get { return sServGrp; }
            set { sServGrp = value; }
        }

        public DateTime Today
        {
            get { return dToday; }
            set { dToday = value; }
        }

        public string SPCode
        {
            get { return sSPCode; }
            set { sSPCode = value; }
        }

        public string DueDate
        {
            get { return dDueDate; }
            set { dDueDate = value; }
        }

        public string SampleDescription
        {
            get { return sSampleDescription; }
            set { sSampleDescription = value; }
        }

        public string SampleComments
        {
            get { return sSampleComments; }
            set { sSampleComments = value; }
        }

        public string PrepRun
        {
            get { return sPrepRun; }
            set { sPrepRun = value; }
        }

        public string Container
        {
            get { return sContainer; }
            set { sContainer = value; }
        }

        public int TypeNo
        {
            get
            {
                if (Type == "N/A")
                {
                    return -1;
                }
                else if (Type == "DUP")
                {
                    return 0;
                }
                else if (Type == "TRP")
                {
                    return 1;
                }
                else if (Type == "QUAD")
                {
                    return 2;
                }
                else if (Type == "MS")
                {
                    return 3;
                }
                else if (Type == "DMS")
                {
                    return 4;
                }
                else
                {
                    return -1;
                }
            }
        }

        public int Number
        {
            get { return iNumber; }
            set { iNumber = value; }
        }

        public string TestComments
        {
            get
            {
                //Both Empty
                if (string.IsNullOrEmpty(PCComments))
                {
                    if (string.IsNullOrEmpty(PrepComments))
                    {
                        sTestComments = "";
                    }
                    else
                    {
                        sTestComments = "PrepComments:" + PrepComments;
                    }
                }
                else
                {
                    if (string.IsNullOrEmpty(PrepComments))
                    {
                        sTestComments = "PC Comments: \n" + PCComments;
                    }
                    else
                    {
                        sTestComments = "PC Comments: \n" + PCComments + "\nPrepComments: \n" + PrepComments;
                    }
                }

                //if ((PCComments == null && PrepComments == null) ||(PCComments == null && PrepComments.Trim() == "")|| (PCComments.Trim() == "" && PrepComments.Trim() == ""))
                //{
                //    sTestComments = "";
                //}
                //else if (PCComments == null || PCComments.Trim() == "")
                //{
                //    sTestComments = "PrepComments:" + PrepComments;
                //}
                //else if (PrepComments == null || PrepComments.Trim() == "")
                //{
                //    sTestComments = "PC Comments: \n" + PCComments;
                //}
                //else
                //{
                //    sTestComments = "PC Comments: \n" + PCComments + "\nPrepComments: \n" + PrepComments;
                //}
                return sTestComments;
            }
        }
        public string PCComments
        {
            get { return sPCComments; }
            set { sPCComments = value;}
        }

        public string PrepComments
        {
            get { return sPrepComments; }
            set { sPrepComments = value; }
        }

        public string TestCode
        {
            get { return sTestCode; }
            set { sTestCode = value; }
        }

        public string SampleName
        {
            get { return sSampleName; }
            set { sSampleName = value; }
        }

        public List<Sample> sampsForBatch
        {
            get { return lstSampsForBatch; }
            set { lstSampsForBatch = value; }
        }

        public List<string> Folders
        {
            get { return lstFolders; }
            set { lstFolders = value; }
        }

        public QCSample Samp
        {
            get { return sSamp; }
            set { sSamp = value; }
        }

        public string TestName
        {
            get { return sTestName; }
            set { sTestName = value; }
        }
        public string Mass
        {
            get { return sMass; }
            set { sMass = value; }
        }
        public string Unit
        {
            get { return sUnit; }
            set { sUnit = value; }
        }
        public string Dup
        {
            get { return sDup; }
            set { sDup = value; }
        }
        public string MS
        {
            get { return sMS; }
            set { sMS = value; }
        }
        public string DMS
        {
            get { return sDMS; }
            set { sDMS = value; }

        }
        public string QC
        {
            get
            {
                if (sDup != null)
                {
                    sQC = "Dup";
                }
                if (sMS != null)
                {
                    if (sQC != null)
                    {
                        sQC += ", MS";
                    }
                    else
                    {
                        sQC = "MS";
                    }
                }
                if (sDMS != null)
                {
                    sQC += ", DMS";
                }

                if (sQC != null)
                {
                    sQC += " required.";
                }

                return sQC;
            }
        }

        public string Type
        {
            get { return sType; }
            set { sType = value; }
        }

        public string PrepTName
        {
            get { return sPrepTName; }
            set { sPrepTName = value; }
        }
    }

    public class QCSample
    {
        string sSampleName;
        string sDisplayName;
        string sParentName = "";
        string sSampleDescription;

        public string SampleDescription
        {
            get { return sSampleDescription; }
            set { sSampleDescription = value; }
        }

        public string DisplayName
        {
            get { return sDisplayName; }
            set { sDisplayName = value; }
        }

        public string ParentName
        {
            get { return sParentName; }
            set { sParentName = value; }
        }

        public string SampleName
        {
            get { return sSampleName; }
            set { sSampleName = value; }
        }
    }
}
