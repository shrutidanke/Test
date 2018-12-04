using System;
using System.Xml;
using System.IO;
using System.Text;
using System.Web;
using System.Net;
using System.Collections.Specialized;
using System.Data;
using System.Threading;

namespace VibeDirect
{
    public class RXEProcessing
    {
        private RXEProcessing()
        {
            //
            // TODO: Add constructor logic here
            //
        }
        public static void BackgroundProcessWholesaleMemberships()
        {
            while (true)
            {
                ProcessWholesaleMemberships();
                Thread.Sleep(new TimeSpan(0, 0, 5, 0, 0));
            }
        }
        public static void ProcessWholesaleMemberships()
        {
            DataBase db = new DataBase();
            DataSet OpenWM = new DataSet();
            OpenWM = db.GetOpenWholesaleMemberships();
            foreach (DataRow row in OpenWM.Tables[0].Rows)
            {
                string transactionID = row["TransactionID"].ToString();
                string referralCode = row["Referral_Code"].ToString();

                string redemptionCode = GetRedemptionCode(transactionID, referralCode);

                if (redemptionCode != "Error")
                    db.SetWholesaleMembershipRedemptionInfo(transactionID, redemptionCode);
            }
        }

        private static string GetRedemptionCode(string transactionID, string ReferralCode)
        {
            string redemptionCode = "";
            try
            {

                byte[] rawXML = GetRXEWholesaleXML(transactionID, ReferralCode);
                string cleanXML = CleanRXEXML(rawXML);
                redemptionCode = GetRedemptionCodeFromXML(cleanXML);
            }
            catch (Exception e)
            {
                //if (!System.Diagnostics.EventLog.SourceExists("SoulmateBiz"))
                //	System.Diagnostics.EventLog.CreateEventSource(
                //		"SoulmateBiz", "Application");
                System.Diagnostics.EventLog EventLog1 = new System.Diagnostics.EventLog();
                EventLog1.Source = "Active Server Pages";
                EventLog1.WriteEntry("RXE Gift Cert Error: " + e.Message);
                redemptionCode = "Error";

            }

            return redemptionCode;
        }

        private static byte[] GetRXEWholesaleXML(string transactionID, string ReferralCode)
        {

            string url = Utility.GetConfigSetting("RXE_Gift_URL");
            string processMode = Utility.GetConfigSetting("RXE_Gift_Mode");
            WebClient wb = new WebClient();
            NameValueCollection uploadValues = new NameValueCollection();
            uploadValues.Add("GenNum", "1");
            uploadValues.Add("RequestID", transactionID);
            uploadValues.Add("ReferralCode", ReferralCode);
            uploadValues.Add("Mode", processMode);

            byte[] result = wb.UploadValues(url, "POST", uploadValues);
            return result;
        }

        private static string CleanRXEXML(byte[] result)
        {
            StringBuilder sb = new StringBuilder();
            MemoryStream ms = new MemoryStream(result);
            StreamReader sr = new StreamReader(ms);
            string str;
            do
            {
                str = sr.ReadLine();
                if (str != null && str.Trim().Length != 0)
                    sb.Append(str.Trim() + "\n");
            }
            while (str != null);

            return sb.ToString();
        }

        private static string GetRedemptionCodeFromXML(string cleanXML)
        {
            string redemptionCode = "";
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(cleanXML);

            redemptionCode = doc.SelectSingleNode("//RedemptionCode").InnerText;

            return redemptionCode;

        }
    }

}
