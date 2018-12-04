using System;
using System.Configuration;
using System.Data;
using System.IO;
using System.Net;
using System.Web;
using System.Web.Security;
using System.Web.UI.HtmlControls;
using System.Web.UI.WebControls;


namespace VibeDirect
{
    /// <summary>
    /// Summary description for Utility.
    /// </summary>
    public class Utility
    {
        private Utility()
        {
            //
            // TODO: Add constructor logic here
            //
        }


        public static HttpContext Context
        {
            get
            {
                return HttpContext.Current;
            }
        }

        public static HttpRequest Request
        {
            get
            {
                return Context.Request;
            }
        }

        public static HttpResponse Response
        {
            get
            {
                return Context.Response;
            }
        }

        public static TraceContext Trace
        {
            get
            {
                return Context.Trace;
            }
        }

        public static System.Web.SessionState.HttpSessionState Session
        {
            get
            {
                return Context.Session;
            }
        }

        public static object GetSessionValue(string key)
        {
            object value = null;
            try
            {
                value = Session[key];
            }
            catch (NullReferenceException)
            {
                //nooop. value remains null
            }

            return value;
        }

        public static string GetConfigSetting(string key)
        {
            string returnValue = ConfigurationSettings.AppSettings[key];

            if (IsEmpty(returnValue))
            {
                returnValue = "";
            }

            return returnValue;
        }


        public static bool IsEmpty(string valueToCheck)
        {
            bool isEmpty = true;

            // the check could be done in one line: if (valueToCheck != null && valueToCheck.length != 0).
            // But just on the off chance that it will ported to a language that doesn't stop the if when the left half
            // fails, making to if statements is safer.
            if (valueToCheck != null)
            {
                if (valueToCheck.Length != 0)
                {
                    isEmpty = false;
                }
            }

            return isEmpty;
        }


        public static DateTime GetDate(string fieldValue)
        {
            DateTime returnValue = DateTime.MinValue;

            if (!IsEmpty(fieldValue))
            {
                try
                {
                    returnValue = DateTime.Parse(fieldValue);
                }
                catch (Exception)
                { }
            }

            return returnValue;
        }


        public static double GetDouble(string fieldValue)
        {
            double returnValue = 0D;

            if (fieldValue != null && fieldValue.Length > 0)
            {
                try
                {
                    returnValue = double.Parse(fieldValue);
                }
                catch (Exception)
                {
                    // noop
                }
            }

            return returnValue;
        }


        public static decimal GetDecimal(string fieldValue)
        {
            decimal returnValue = 0M;

            if (fieldValue != null && fieldValue.Length > 0)
            {
                try
                {
                    returnValue = decimal.Parse(fieldValue);
                }
                catch (Exception)
                {
                }

            }

            return returnValue;
        }


        public static int GetInt(string fieldValue)
        {
            int returnValue = 0;

            if (fieldValue != null && fieldValue.Length > 0)
            {
                try
                {
                    returnValue = int.Parse(fieldValue);
                }
                catch (Exception)
                {
                    // noop
                }
            }

            return returnValue;
        }


        public static int GetIntFromQueryString(string fieldName)
        {
            return GetInt(Request[fieldName]);
        }

        public static short GetShort(string fieldValue)
        {
            short returnValue = 0;

            if (fieldValue != null && fieldValue.Length > 0)
            {
                try
                {
                    returnValue = short.Parse(fieldValue);
                }
                catch (Exception)
                {
                    // noop
                }
            }

            return returnValue;
        }


        public static long GetLong(string fieldValue)
        {
            long returnValue = 0;

            if (fieldValue != null && fieldValue.Length > 0)
            {
                try
                {
                    returnValue = long.Parse(fieldValue);
                }
                catch (Exception)
                {
                    // noop
                }
            }

            return returnValue;
        }


        public static Guid GetGuid(string fieldValue)
        {
            Guid returnGuid = Guid.Empty;

            try
            {
                returnGuid = new Guid(fieldValue);
            }
            catch (Exception exception)
            {
                //System.Web.HttpContext.Current.Trace.Write(string.Format("While attempting to cast {0} to Guid: {1} {2}", fieldValue, exception.GetType().ToString(), exception.Message));	
            }

            return returnGuid;
        }


        public static bool IsNumber(string fieldValue)
        {
            bool returnValue = false;

            if (!IsEmpty(fieldValue))
            {
                if (Microsoft.VisualBasic.Information.IsNumeric(fieldValue))
                {
                    returnValue = true;
                }
            }

            return returnValue;
        }


        public static bool IsDate(string fieldValue)
        {
            bool returnValue = false;

            if (!IsEmpty(fieldValue))
            {
                if (Microsoft.VisualBasic.Information.IsDate(fieldValue))
                {
                    returnValue = true;
                }
            }

            return returnValue;
        }


        public static void FillYearDropDown(HtmlSelect yearField, int numberOfYears, int selectedYear)
        {
            int todayYear = DateTime.Today.Year;
            bool hasSelected = false;

            // clear the years.
            yearField.Items.Clear();

            for (int itemCounter = numberOfYears; itemCounter > -1; itemCounter--)
            {
                int currentYear = todayYear - itemCounter;
                ListItem item = new ListItem(currentYear.ToString(), currentYear.ToString());

                if (currentYear == selectedYear && !hasSelected)
                {
                    item.Selected = true;
                    hasSelected = true;
                }

                yearField.Items.Add(item);

            }
        }


        public static void FillYearDropDownFuture(HtmlSelect yearField, int numberOfYears, int selectedYear)
        {
            int todayYear = DateTime.Today.Year;
            bool hasSelected = false;

            // clear the years.
            yearField.Items.Clear();

            for (int itemCounter = 0; itemCounter < numberOfYears; itemCounter++)
            {
                int currentYear = todayYear + itemCounter;
                ListItem item = new ListItem(currentYear.ToString(), currentYear.ToString());

                if (currentYear == selectedYear && !hasSelected)
                {
                    item.Selected = true;
                    hasSelected = true;
                }

                yearField.Items.Add(item);

            }
        }

        public static bool SetSelectedValue(System.Web.UI.WebControls.DropDownList select, string value, string defaultSetting)
        {
            bool returnValue = false;

            for (int count = 0; count < select.Items.Count && !returnValue; count++) //  each (ListItem item in select.Items)
            {
                ListItem item = select.Items[count];

                Trace.Write("SetSelectedValue", string.Format("{0} [{1}] [{2}] [{3}]", count, item.Text, item.Value, value));

                if (item.Value == value)
                {
                    Trace.Write(string.Format("Value match! Setting selected index to {0}", count));
                    select.SelectedIndex = count;
                    returnValue = true;
                }
                else if (item.Value == defaultSetting)
                {
                    select.SelectedIndex = count;
                }

            }

            return returnValue;
        }

        public static bool SetSelectedValue(System.Web.UI.HtmlControls.HtmlSelect select, string value, string defaultSetting)
        {
            bool returnValue = false;

            for (int count = 0; count < select.Items.Count && !returnValue; count++) //  each (ListItem item in select.Items)
            {
                ListItem item = select.Items[count];

                Trace.Write("SetSelectedValue", string.Format("{0} [{1}] [{2}] [{3}]", count, item.Text, item.Value, value));

                if (item.Value == value)
                {
                    Trace.Write(string.Format("Value match! Setting selected index to {0}", count));
                    select.SelectedIndex = count;
                    returnValue = true;
                }
                else if (item.Value == defaultSetting)
                {
                    select.SelectedIndex = count;
                }

            }

            return returnValue;
        }

        public static string GetSelectedValue(System.Web.UI.HtmlControls.HtmlSelect select)
        {
            return select.Items[select.SelectedIndex].Value;
        }

        public static void TraceWrite(string message)
        {
            TraceWrite("", message);
        }

        public static void TraceWrite(string category, string message)
        {
            System.Web.HttpContext.Current.Trace.Write(category, message);
        }

        public static void TraceWrite(string category, string messageFormat, params object[] args)
        {
            string message = string.Format(messageFormat, args);
            TraceWrite(category, message);
        }

        public static void TraceWrite(string messageFormat, params object[] args)
        {
            TraceWrite("", messageFormat, args);
        }

        public static DataSet GetReferralData()
        {
            DataSet dataSet = new DataSet();
            string promoName = GetPromoName();
            Trace.Write("GetReferralData()", string.Format("promo name: {0}", promoName));

            dataSet = GetReferralData(promoName);

            return dataSet;
        }
        public static DataSet GetReferralData(string promoName)
        {
            DataSet dataSet = new DataSet();
            Trace.Write("GetReferralData()", string.Format("promo name: {0}", promoName));
            if (promoName != null && promoName.Length > 0)
            {
                Trace.Write("GetReferralData()", "Promo name not null! Calling db.GetReferralDisplayInfo.");
                DataBase dataBase = new DataBase();
                dataSet = dataBase.GetReferralDisplayInfo(promoName);
            }

            return dataSet;
        }



        public static string GetPromoName()
        {
            string address = Request.Url.Host.ToString();
            string prefix = "";
            if (address.Split('.').Length == 4)
                prefix = address.Split('.')[1].ToString();
            else
                prefix = address.Split('.')[0].ToString();
            return (prefix);
        }


        public static void SignOut()
        {
            FormsAuthentication.SignOut();
            Session.Abandon();
            Response.Redirect("~/");
        }

        public static string postXMLData(string destinationUrl, string requestXml)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create("https://apitest.authorize.net/xml/v1/request.api");
            byte[] bytes;
            bytes = System.Text.Encoding.ASCII.GetBytes(requestXml);
            request.ContentType = "application/xml";
            request.ContentLength = bytes.Length;
            request.Method = "POST";
            Stream requestStream = request.GetRequestStream();
            requestStream.Write(bytes, 0, bytes.Length);
            requestStream.Close();
            HttpWebResponse response;
            response = (HttpWebResponse)request.GetResponse();
            if (response.StatusCode == HttpStatusCode.OK)
            {
                Stream responseStream = response.GetResponseStream();
                string responseStr = new StreamReader(responseStream).ReadToEnd();
                return responseStr;
            }
            return null;
        }
    }
}
