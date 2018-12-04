using System;
using System.Collections;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Data.SqlTypes;
using System.Diagnostics;
using System.Web;
using System.Web.Security;

using Si2.Cryptography;
using Si2.Data;


namespace VibeDirect
{
    /// <summary>
    /// Summary description for DataBase.
    /// </summary>
    public class DataBase : SqlDataBase
    {
        public DataBase()
        {
            Crypt crypt = new Crypt("e307af79-d2fa-46c7-be34-c6ade24671ed");
            //string dbServer = crypt.DecryptString(ConfigurationSettings.AppSettings["db_server"]);
            //string dbUser = crypt.DecryptString(ConfigurationSettings.AppSettings["db_user"]);
            //string dbPassword = crypt.DecryptString(ConfigurationSettings.AppSettings["db_key"]);
            //string dbCatalog = crypt.DecryptString(ConfigurationSettings.AppSettings["db_catalog"]);
            string dbServer = ConfigurationSettings.AppSettings["db_server"];
            string dbUser = ConfigurationSettings.AppSettings["db_user"];
            string dbPassword = ConfigurationSettings.AppSettings["db_key"];
            string dbCatalog = ConfigurationSettings.AppSettings["db_catalog"];
            // _connectionString = string.Format("packet size=4096;user id={0};data source={1};persist security info=True;initial catalog={2};password={3};pooling=true;Max Pool Size=500;", dbUser, dbServer, dbCatalog, dbPassword);
            _connectionString = string.Format("packet size=4096;user id={0};data source={1};persist security info=True;initial catalog={2};password={3}", dbUser, dbServer, dbCatalog, dbPassword);

        }

        public string HashBadWords(string[] badWords)
        {
            Crypt crypt = new Crypt("e307af79-d2fa-46c7-be34-c6ade24671ed");
            System.Text.StringBuilder builder = new System.Text.StringBuilder();
            bool firstOne = true;

            //			builder.Append("{");
            builder.Append("<add key=\"bw\" value=\"");

            foreach (string badWord in badWords)
            {
                if (!firstOne)
                {
                    builder.Append(", ");
                }

                firstOne = false;
                builder.Append(crypt.EncryptString(badWord));

            }

            builder.Append("\" />");

            //builder.Append("}");

            return builder.ToString();


        }


        public static string GetMD5Hash(string password)
        {
            byte[] textBytes = System.Text.Encoding.Default.GetBytes(password);
            try
            {
                System.Security.Cryptography.MD5CryptoServiceProvider cryptHandler;
                cryptHandler = new System.Security.Cryptography.MD5CryptoServiceProvider();
                byte[] hash = cryptHandler.ComputeHash(textBytes);
                string ret = "";
                foreach (byte a in hash)
                {
                    if (a < 16)
                        ret += "0" + a.ToString("x");
                    else
                        ret += a.ToString("x");
                }
                return ret;
            }
            catch
            {
                throw;
            }
        }

        #region Monthly Billing
        public DataSet GetMonthlyBillingLogForUser(int UserID)
        {
            string sql = "Si2_GetMonthlyBillingLogForUser";
            DataSet data = new DataSet();
            using (SqlCommand command = new SqlCommand(sql))
            {
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.Add(NewParameter("@memberid", UserID));
                data = Execute(command);
            }
            return data;


        }
        public DateTime GetSubscriptionEnd(int UserID)
        {
            DateTime returnVal = new DateTime();
            DataSet data = new DataSet();
            string sql = "Si2_GetMonthlySubscriptionEnd";
            DataSet Data = new DataSet();

            using (SqlCommand command = new SqlCommand(sql))
            {
                command.CommandType = CommandType.StoredProcedure;

                command.Parameters.Add(NewParameter("@memberid", UserID));

                data = Execute(command);
            }

            try
            {
                returnVal = (DateTime)data.Tables[0].Rows[0][0];
            }
            catch (Exception)
            {
                returnVal = DateTime.MinValue;
            }

            return returnVal;



        }

        public void AddSubscriptionMonths(int UserID, string TransactionID, string Source, int months, string comments)
        {
            string sql = "Si2_AddMonthlyBillingSubscription";
            DateTime SubscriptionEnd = GetSubscriptionEnd(UserID);

            if (SubscriptionEnd == DateTime.MinValue || SubscriptionEnd < DateTime.Today)
                SubscriptionEnd = DateTime.Today;

            using (SqlCommand command = new SqlCommand(sql))
            {
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.Add(NewParameter("@memberid", UserID));
                command.Parameters.Add(NewParameter("@TransactionID", TransactionID));
                command.Parameters.Add(NewParameter("@Source", Source));
                command.Parameters.Add(NewParameter("@StartDate", SubscriptionEnd));
                command.Parameters.Add(NewParameter("@months", months));
                command.Parameters.Add(NewParameter("@Comments", comments));

                ExecuteNonQuery(command);
            }

        }

        public DataSet GetMonthlyBillingInfo(int UserID)
        {
            DataSet data = new DataSet();
            Crypt crypt = new Crypt("e307af79-d2fa-46c7-be34-c6ade24671ed");

            string sql = "Si2_GetMonthlyBillingInfo";
            DataSet returnData = new DataSet();

            using (SqlCommand command = new SqlCommand(sql))
            {
                command.CommandType = CommandType.StoredProcedure;

                command.Parameters.Add(NewParameter("@memberid", UserID));

                data = Execute(command);
            }
            //do some Decrypting...
            if (data.Tables[0].Rows.Count > 0)
            {
                data.Tables[0].Rows[0]["CardNumber"] = crypt.DecryptString(data.Tables[0].Rows[0]["CardNumber"].ToString());
                data.Tables[0].Rows[0]["Cvv2"] = crypt.DecryptString(data.Tables[0].Rows[0]["Cvv2"].ToString());
            }

            //Done Decrypting



            return data;


        }

        public void SetMonthlyBillingStatus(int UserID, bool billMonthly)
        {
            string sql = "Si2_SetMonthlyBillingStatus";


            using (SqlCommand command = new SqlCommand(sql))
            {
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.Add(NewParameter("@memberid", UserID));
                command.Parameters.Add(NewParameter("@BillMonthly", billMonthly));
                ExecuteNonQuery(command);
            }
        }

        public void SetMonthlyBillingInfo(long UserID, string NameOnCard, string CardType, string CardNumber, string Cvv2, int ExpMonth, int ExpYear, string Address1, string Address2, string City, string State, string Zip, string Phone)
        {
            string sql = "Si2_SetMonthlyBillingInfo";

            //do some Encryption
            Crypt crypt = new Crypt("e307af79-d2fa-46c7-be34-c6ade24671ed");
            CardNumber = crypt.EncryptString(CardNumber);
            Cvv2 = crypt.EncryptString(Cvv2);
            //done encrypting

            using (SqlCommand command = new SqlCommand(sql))
            {
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.Add(NewParameter("@memberid", UserID));
                command.Parameters.Add(NewParameter("@NameOnCard", NameOnCard));
                command.Parameters.Add(NewParameter("@CardType", CardType));
                command.Parameters.Add(NewParameter("@CardNumber", CardNumber));
                command.Parameters.Add(NewParameter("@Cvv2", Cvv2));
                command.Parameters.Add(NewParameter("@ExpireMonth", ExpMonth));
                command.Parameters.Add(NewParameter("@ExpireYear", ExpYear));
                command.Parameters.Add(NewParameter("@Address1", Address1));
                command.Parameters.Add(NewParameter("@Address2", Address2));
                command.Parameters.Add(NewParameter("@city", City));
                command.Parameters.Add(NewParameter("@state", State));
                command.Parameters.Add(NewParameter("@zip", Zip));
                command.Parameters.Add(NewParameter("@phone", Phone));

                ExecuteNonQuery(command);

            }

        }

        #endregion
        #region Admin Functions

        public void AddCMSAdjustment(int userID, decimal amount)
        {
            string sql = "cms_DoManualEntry";

            using (SqlCommand command = new SqlCommand(sql))
            {
                int rVal = 0;
                command.CommandType = CommandType.StoredProcedure;

                command.Parameters.Add(NewParameter("@rmemberid", userID));
                command.Parameters.Add(NewParameter("@rAmount", amount));
                command.Parameters.Add(NewParameter("@rEntryType", 2001));
                command.Parameters.Add(NewParameter("@rVal", rVal));

                ExecuteNonQuery(command);

            }

        }


        public void ReverseCommissions(int userID, int cmsOrderID)
        {
            string sql = "Si2_ReverseCommission";

            using (SqlCommand command = new SqlCommand(sql))
            {

                command.CommandType = CommandType.StoredProcedure;

                command.Parameters.Add(NewParameter("@memberid", userID));
                command.Parameters.Add(NewParameter("@CmsOrderID", cmsOrderID));

                ExecuteNonQuery(command);

            }

        }

        public DataSet GetUserCommerceEntries(int userID)
        {
            string sql = "Si2_GetUserCommerceEntries";
            DataSet data = new DataSet();

            using (SqlCommand command = new SqlCommand(sql))
            {

                command.CommandType = CommandType.StoredProcedure;

                command.Parameters.Add(NewParameter("@memberid", userID));

                data = Execute(command);

            }
            return data;

        }

        public DataSet GetLedgerEntry(string ID, int LineItem)
        {
            string sql = "Si2_GetLedgerEntry";
            DataSet data = new DataSet();

            using (SqlCommand command = new SqlCommand(sql))
            {

                command.CommandType = CommandType.StoredProcedure;

                command.Parameters.Add(NewParameter("@ID", ID));
                command.Parameters.Add(NewParameter("@LineItem", LineItem));

                data = Execute(command);

            }
            return data;

        }

        public void CancelUser(int userID)
        {
            string sql = "Si2_CancelUser";

            using (SqlCommand command = new SqlCommand(sql))
            {

                command.CommandType = CommandType.StoredProcedure;

                command.Parameters.Add(NewParameter("@memberid", userID));

                ExecuteNonQuery(command);

            }

        }

        #endregion

        #region Signup
        public void NewSignupPackagesR(int userID, string sql)
        {
            using (SqlCommand command = new SqlCommand(sql))
            {
                command.CommandType = CommandType.StoredProcedure;
                string daterUserName = Guid.NewGuid().ToString();
                command.Parameters.Add(NewParameter("@rmemberid", userID));
                ExecuteNonQuery(command);
            }
        }
        public void NewSignupPackages(int userID, string sql)
        {
            using (SqlCommand command = new SqlCommand(sql))
            {
                command.CommandType = CommandType.StoredProcedure;
                string daterUserName = Guid.NewGuid().ToString();
                command.Parameters.Add(NewParameter("@memberid", userID));
                ExecuteNonQuery(command);
            }
        }
        public void NewSignupPackages(int userID, string sql, int count)
        {
            for (int i = 0; i < count; i++)
            {
                using (SqlCommand command = new SqlCommand(sql))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    string daterUserName = Guid.NewGuid().ToString();
                    command.Parameters.Add(NewParameter("@memberid", userID));
                    command.Parameters.Add(NewParameter("@signup", DateTime.Now.AddMonths(i)));
                    ExecuteNonQuery(command);
                }
            }
        }

        public void AddCruiseCert(SignupData signup)
        {
            string sql = "Si2_AddTRVL_CruiseCert";
            using (SqlCommand command = new SqlCommand(sql))
            {
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.Add(NewParameter("@memberid", signup.UserID));
                command.Parameters.Add(NewParameter("@FirstName", signup.CertFirstName));
                command.Parameters.Add(NewParameter("@LastName", signup.CertLastName));
                command.Parameters.Add(NewParameter("@MiddleInitial", signup.CertMI));
                command.Parameters.Add(NewParameter("@EmailAddress", signup.CertEmailAddress));
                command.Parameters.Add(NewParameter("@ShipFullName", signup.CertShipFullName));
                command.Parameters.Add(NewParameter("@ShipStreet", signup.CertShipStreet));
                command.Parameters.Add(NewParameter("@ShipCity", signup.CertShipCity));
                command.Parameters.Add(NewParameter("@ShipState", signup.CertShipState));
                command.Parameters.Add(NewParameter("@ShipZip", signup.CertShipZip));
                ExecuteNonQuery(command);
            }

        }

        public void AddCruiseCert(int memberID, string FirstName, string LastName, string MiddleInitial,
            string EmailAddress, string ShipFullName, string ShipStreet, string ShipCity, string ShipState, string ShipZip)
        {
            string sql = "Si2_AddTRVL_CruiseCert";
            using (SqlCommand command = new SqlCommand(sql))
            {
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.Add(NewParameter("@memberid", memberID));
                command.Parameters.Add(NewParameter("@FirstName", FirstName));
                command.Parameters.Add(NewParameter("@LastName", LastName));
                command.Parameters.Add(NewParameter("@MiddleInitial", MiddleInitial));
                command.Parameters.Add(NewParameter("@EmailAddress", EmailAddress));
                command.Parameters.Add(NewParameter("@ShipFullName", ShipFullName));
                command.Parameters.Add(NewParameter("@ShipStreet", ShipStreet));
                command.Parameters.Add(NewParameter("@ShipCity", ShipCity));
                command.Parameters.Add(NewParameter("@ShipState", ShipState));
                command.Parameters.Add(NewParameter("@ShipZip", ShipZip));
                ExecuteNonQuery(command);
            }

        }


        #endregion

        #region Genealogy

        //new code for Agents


        public DataSet GetGenealogyDetail(long userID, string summaryType, int orgID, int levelID)
        {
            switch (summaryType)
            {
                case "Rep":
                    return GetGenealogyRepDetail(userID, orgID, levelID);
                case "RepTotal":
                    return GetGenealogyRepTotalDetail(userID, orgID);
                case "Agent":
                    return GetGenealogyAgentDetail(userID, orgID, levelID);
                case "AgentTotal":
                    return GetGenealogyAgentTotalDetail(userID, orgID);
                case "MonthAgent":
                    return GetGenealogyAgentMonthDetail(userID, orgID, levelID);
                case "MonthRep":
                    return GetGenealogyRepMonthDetail(userID, orgID, levelID);
                case "MonthRepTotal":
                    return GetGenealogyMonthRepTotalDetail(userID, orgID);
                case "MonthAgentTotal":
                    return GetGenealogyMonthAgentTotalDetail(userID, orgID);

                default:
                    return new DataSet();
            }
        }

        public DataSet GetGenealogyRepMonthDetail(long userID, int OrgID, int levelID)
        {
            DataSet data = new DataSet();
            string sql = "Si2_Genealogy_GetRepMonthDetail";
            using (SqlCommand command = new SqlCommand(sql))
            {
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.Add(NewParameter("@memberid", System.Data.SqlDbType.Int, userID, System.Data.ParameterDirection.Input));
                command.Parameters.Add(NewParameter("@Organization", System.Data.SqlDbType.Int, OrgID, System.Data.ParameterDirection.Input));
                command.Parameters.Add(NewParameter("@LevelID", System.Data.SqlDbType.Int, levelID, System.Data.ParameterDirection.Input));
                data = Execute(command);
            }
            return data;
        }

        public DataSet GetGenealogyAgentMonthDetail(long userID, int OrgID, int levelID)
        {
            DataSet data = new DataSet();
            string sql = "Si2_Genealogy_GetAgentMonthDetail";
            using (SqlCommand command = new SqlCommand(sql))
            {
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.Add(NewParameter("@memberid", System.Data.SqlDbType.Int, userID, System.Data.ParameterDirection.Input));
                command.Parameters.Add(NewParameter("@Organization", System.Data.SqlDbType.Int, OrgID, System.Data.ParameterDirection.Input));
                command.Parameters.Add(NewParameter("@LevelID", System.Data.SqlDbType.Int, levelID, System.Data.ParameterDirection.Input));
                data = Execute(command);
            }
            return data;
        }

        public DataSet GetGenealogyRepTotalDetail(long userID, int OrgID)
        {
            DataSet data = new DataSet();
            string sql = "Si2_Genealogy_GetRepTotalDetail";
            using (SqlCommand command = new SqlCommand(sql))
            {
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.Add(NewParameter("@memberid", System.Data.SqlDbType.Int, userID, System.Data.ParameterDirection.Input));
                command.Parameters.Add(NewParameter("@Organization", System.Data.SqlDbType.Int, OrgID, System.Data.ParameterDirection.Input));
                data = Execute(command);
            }
            return data;
        }
        public DataSet GetGenealogyMonthRepTotalDetail(long userID, int OrgID)
        {
            DataSet data = new DataSet();
            string sql = "Si2_Genealogy_GetRepMonthTotalDetail";
            using (SqlCommand command = new SqlCommand(sql))
            {
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.Add(NewParameter("@memberid", System.Data.SqlDbType.Int, userID, System.Data.ParameterDirection.Input));
                command.Parameters.Add(NewParameter("@Organization", System.Data.SqlDbType.Int, OrgID, System.Data.ParameterDirection.Input));
                data = Execute(command);
            }
            return data;
        }
        public DataSet GetGenealogyMonthAgentTotalDetail(long userID, int OrgID)
        {
            DataSet data = new DataSet();
            string sql = "Si2_Genealogy_GetAgentMonthTotalDetail";
            using (SqlCommand command = new SqlCommand(sql))
            {
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.Add(NewParameter("@memberid", System.Data.SqlDbType.Int, userID, System.Data.ParameterDirection.Input));
                command.Parameters.Add(NewParameter("@Organization", System.Data.SqlDbType.Int, OrgID, System.Data.ParameterDirection.Input));
                data = Execute(command);
            }
            return data;
        }
        public DataSet GetGenealogyRepDetail(long userID, int OrgID, int levelID)
        {
            DataSet data = new DataSet();
            string sql = "Si2_Genealogy_GetRepDetail";
            using (SqlCommand command = new SqlCommand(sql))
            {
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.Add(NewParameter("@memberid", System.Data.SqlDbType.Int, userID, System.Data.ParameterDirection.Input));
                command.Parameters.Add(NewParameter("@Organization", System.Data.SqlDbType.Int, OrgID, System.Data.ParameterDirection.Input));
                command.Parameters.Add(NewParameter("@LevelID", System.Data.SqlDbType.Int, levelID, System.Data.ParameterDirection.Input));
                data = Execute(command);
            }
            return data;
        }

        public DataSet GetGenealogyAgentDetail(long userID, int OrgID, int levelID)
        {
            DataSet data = new DataSet();
            string sql = "Si2_Genealogy_GetAgentDetail";
            using (SqlCommand command = new SqlCommand(sql))
            {
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.Add(NewParameter("@memberid", System.Data.SqlDbType.Int, userID, System.Data.ParameterDirection.Input));
                command.Parameters.Add(NewParameter("@Organization", System.Data.SqlDbType.Int, OrgID, System.Data.ParameterDirection.Input));
                command.Parameters.Add(NewParameter("@LevelID", System.Data.SqlDbType.Int, levelID, System.Data.ParameterDirection.Input));
                data = Execute(command);
            }
            return data;
        }

        public DataSet GetGenealogyAgentTotalDetail(long userID, int OrgID)
        {
            DataSet data = new DataSet();
            string sql = "Si2_Genealogy_GetAgentTotalDetail";
            using (SqlCommand command = new SqlCommand(sql))
            {
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.Add(NewParameter("@memberid", System.Data.SqlDbType.Int, userID, System.Data.ParameterDirection.Input));
                command.Parameters.Add(NewParameter("@Organization", System.Data.SqlDbType.Int, OrgID, System.Data.ParameterDirection.Input));
                data = Execute(command);
            }
            return data;
        }



        public DataSet GetGenealogySummary(long userID, string summaryType, int orgID)
        {
            switch (summaryType)
            {
                case "Rep":
                    return GetGenealogyRepSummary(userID, orgID);
                //return new DataSet();
                case "Agent":
                    return GetGenealogyAgentSummary(userID, orgID);
                case "MonthAgent":
                    return GetGenealogyAgentMonthSummary(userID, orgID);
                case "MonthRep":
                    return GetGenealogyRepMonthSummary(userID, orgID);
                default:
                    return GetGenealogyRepSummary(userID, orgID);
                    //return new DataSet();
            }
        }

        public DataSet GetGenealogyRepMonthSummary(long userID, int OrgID)
        {
            DataSet data = new DataSet();
            string sql = "Si2_Genealogy_GetRepMonthSummary";
            using (SqlCommand command = new SqlCommand(sql))
            {
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.Add(NewParameter("@memberid", System.Data.SqlDbType.Int, userID, System.Data.ParameterDirection.Input));
                command.Parameters.Add(NewParameter("@Organization", System.Data.SqlDbType.Int, OrgID, System.Data.ParameterDirection.Input));
                data = Execute(command);
            }
            return data;
        }

        public DataSet GetGenealogyAgentMonthSummary(long userID, int OrgID)
        {
            DataSet data = new DataSet();
            string sql = "Si2_Genealogy_GetAgentMonthSummary";
            using (SqlCommand command = new SqlCommand(sql))
            {
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.Add(NewParameter("@memberid", System.Data.SqlDbType.Int, userID, System.Data.ParameterDirection.Input));
                command.Parameters.Add(NewParameter("@Organization", System.Data.SqlDbType.Int, OrgID, System.Data.ParameterDirection.Input));
                data = Execute(command);
            }
            return data;
        }

        public DataSet GetGenealogyRepSummary(long userID, int OrgID)
        {
            DataSet data = new DataSet();
            string sql = "Si2_Genealogy_GetRepSummary";
            using (SqlCommand command = new SqlCommand(sql))
            {
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.Add(NewParameter("@memberid", System.Data.SqlDbType.Int, userID, System.Data.ParameterDirection.Input));
                command.Parameters.Add(NewParameter("@Organization", System.Data.SqlDbType.Int, OrgID, System.Data.ParameterDirection.Input));
                data = Execute(command);
            }
            return data;
        }

        public DataSet GetGenealogyAgentSummary(long userID, int OrgID)
        {
            DataSet data = new DataSet();
            string sql = "Si2_Genealogy_GetAgentSummary";
            using (SqlCommand command = new SqlCommand(sql))
            {
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.Add(NewParameter("@memberid", System.Data.SqlDbType.Int, userID, System.Data.ParameterDirection.Input));
                command.Parameters.Add(NewParameter("@Organization", System.Data.SqlDbType.Int, OrgID, System.Data.ParameterDirection.Input));
                data = Execute(command);
            }
            return data;
        }

        //new Code


        public DataSet GenealogyGetDetail(long userID, string SQL)
        {
            DataSet data = new DataSet();
            string sql = SQL;
            using (SqlCommand command = new SqlCommand(sql))
            {
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.Add(NewParameter("@memberid", System.Data.SqlDbType.Int, userID, System.Data.ParameterDirection.Input));
                data = Execute(command);
            }
            return data;
        }

        public DataSet GenealogyGetDetail(long userID, string SQL, int orgLevelID)
        {
            DataSet data = new DataSet();
            string sql = SQL;
            using (SqlCommand command = new SqlCommand(sql))
            {
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.Add(NewParameter("@memberid", System.Data.SqlDbType.Int, userID, System.Data.ParameterDirection.Input));
                command.Parameters.Add(NewParameter("@orgLevelID", System.Data.SqlDbType.Int, orgLevelID, System.Data.ParameterDirection.Input));
                data = Execute(command);
            }
            return (data);
        }

        public DataSet GenealogyGetDetail(long userID, string SQL, int orgLevelID, int levelID)
        {
            DataSet data = new DataSet();
            string sql = SQL;
            using (SqlCommand command = new SqlCommand(sql))
            {
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.Add(NewParameter("@memberid", System.Data.SqlDbType.Int, userID, System.Data.ParameterDirection.Input));
                command.Parameters.Add(NewParameter("@orgLevelID", System.Data.SqlDbType.Int, orgLevelID, System.Data.ParameterDirection.Input));
                command.Parameters.Add(NewParameter("@LevelID", System.Data.SqlDbType.Int, levelID, System.Data.ParameterDirection.Input));
                data = Execute(command);
            }
            return (data);
        }

        public int GenealogyGetMemberOrgLevel(long userID)
        {
            DataSet data = new DataSet();
            string sql = "Si2_GenealogyGetMemberOrgLevel";
            using (SqlCommand command = new SqlCommand(sql))
            {
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.Add(NewParameter("@memberid", System.Data.SqlDbType.Int, userID, System.Data.ParameterDirection.Input));
                data = Execute(command);
            }
            return int.Parse(data.Tables[0].Rows[0][0].ToString());

        }

        public int GenealogyGetPromotionCount(long userID, int orgLevelID)
        {
            DataSet data = new DataSet();
            string sql = "Si2_GenealogyGetPromotionCount";
            using (SqlCommand command = new SqlCommand(sql))
            {
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.Add(NewParameter("@memberid", System.Data.SqlDbType.Int, userID, System.Data.ParameterDirection.Input));
                command.Parameters.Add(NewParameter("@orgLevelID", System.Data.SqlDbType.Int, orgLevelID, System.Data.ParameterDirection.Input));
                data = Execute(command);
            }
            return int.Parse(data.Tables[0].Rows[0][0].ToString());

        }


        public int GenealogyGetDirectorTotal(long userID, string totalType, int orgLevelID)
        {
            DataSet data = new DataSet();
            string sql = "Si2_GenealogyGetDirector" + totalType + "Total";
            using (SqlCommand command = new SqlCommand(sql))
            {
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.Add(NewParameter("@memberid", System.Data.SqlDbType.Int, userID, System.Data.ParameterDirection.Input));
                command.Parameters.Add(NewParameter("@orgLevelID", System.Data.SqlDbType.Int, orgLevelID, System.Data.ParameterDirection.Input));
                data = Execute(command);
            }
            return DataBase.GetInt(data.Tables[0].Rows[0], "UserTotal");

        }

        public DataSet GenealogyGetDirectorSummary(long userID, string summaryType, int orgLevelID)
        {
            DataSet data = new DataSet();
            string sql = "Si2_GenealogyGetDirector" + summaryType + "Count";
            using (SqlCommand command = new SqlCommand(sql))
            {
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.Add(NewParameter("@memberid", System.Data.SqlDbType.Int, userID, System.Data.ParameterDirection.Input));
                command.Parameters.Add(NewParameter("@orgLevelID", System.Data.SqlDbType.Int, orgLevelID, System.Data.ParameterDirection.Input));
                data = Execute(command);
            }
            return (data);
        }
        public DataSet GenealogyGetRepSummary(long userID)
        {
            DataSet data = new DataSet();
            string sql = "Si2_GenealogyGetRepSummary";
            using (SqlCommand command = new SqlCommand(sql))
            {
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.Add(NewParameter("@memberid", System.Data.SqlDbType.Int, userID, System.Data.ParameterDirection.Input));
                data = Execute(command);
            }
            return (data);
        }
        public DataSet GenealogyGetUltimateRepSummary(long userID)
        {
            DataSet data = new DataSet();
            string sql = "Si2_GenealogyGetUltimateRepSummary";
            using (SqlCommand command = new SqlCommand(sql))
            {
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.Add(NewParameter("@memberid", System.Data.SqlDbType.Int, userID, System.Data.ParameterDirection.Input));
                data = Execute(command);
            }
            return (data);
        }

        public DataSet GenealogyGetMonthRepSummary(long userID)
        {
            DataSet data = new DataSet();
            string sql = "Si2_GenealogyGetMonthRepSummary";
            using (SqlCommand command = new SqlCommand(sql))
            {
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.Add(NewParameter("@memberid", System.Data.SqlDbType.Int, userID, System.Data.ParameterDirection.Input));
                data = Execute(command);
            }
            return (data);
        }

        public DataSet GenealogyGetMonthCustomerSummary(long userID)
        {
            DataSet data = new DataSet();
            string sql = "Si2_GenealogyGetMonthCustomerSummary";
            using (SqlCommand command = new SqlCommand(sql))
            {
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.Add(NewParameter("@memberid", System.Data.SqlDbType.Int, userID, System.Data.ParameterDirection.Input));
                data = Execute(command);
            }
            return (data);
        }

        public DataSet GenealogyGetCustomerRepSummary(long userID)
        {
            DataSet data = new DataSet();
            string sql = "Si2_GenealogyGetCustomerRepSummary";
            using (SqlCommand command = new SqlCommand(sql))
            {
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.Add(NewParameter("@memberid", System.Data.SqlDbType.Int, userID, System.Data.ParameterDirection.Input));
                data = Execute(command);
            }
            return (data);
        }

        public DataSet GetPartnerOrganizationLevels(long userID)
        {
            DataSet data = new DataSet();
            string sql = "Si2_GetPartnerOrganizationLevels";
            using (SqlCommand command = new SqlCommand(sql))
            {
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.Add(NewParameter("@memberid", System.Data.SqlDbType.Int, userID, System.Data.ParameterDirection.Input));
                data = Execute(command);
            }
            return (data);
        }

        public DataSet GetDirectorOrganizationLevels(long userID)
        {
            DataSet data = new DataSet();
            string sql = "Si2_GetDirectorOrganizationLevels";
            using (SqlCommand command = new SqlCommand(sql))
            {
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.Add(NewParameter("@memberid", System.Data.SqlDbType.Int, userID, System.Data.ParameterDirection.Input));
                data = Execute(command);
            }
            return (data);
        }


        public DataSet GetPartnerOrganizationPromotionCounts(long userID)
        {
            DataSet data = new DataSet();
            string sql = "Si2_GetPartnerOrganizationPromotionCounts";
            using (SqlCommand command = new SqlCommand(sql))
            {
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.Add(NewParameter("@memberid", userID));
                data = Execute(command);
            }
            return (data);

        }
        public DataSet GetDirectorOrganizationPromotionCounts(long userID)
        {
            DataSet data = new DataSet();
            string sql = "Si2_GetDirectorOrganizationPromotionCounts";
            using (SqlCommand command = new SqlCommand(sql))
            {
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.Add(NewParameter("@memberid", userID));
                data = Execute(command);
            }
            return (data);

        }

        public DataSet GetGenealogyDetail(string procedureName, int memberID, int level, int detail, int orgLevel)
        {
            DataSet data = null;

            using (SqlCommand command = new SqlCommand(procedureName))
            {
                command.CommandType = CommandType.StoredProcedure;

                command.Parameters.Add(NewParameter("@memberid", memberID));
                command.Parameters.Add(NewParameter("@level", level));
                command.Parameters.Add(NewParameter("@detaillevel", detail));
                command.Parameters.Add(NewParameter("@orglevel", orgLevel));

                data = Execute(command);
            }

            return data;
        }
        #endregion

        #region User and authentication methods
        public DataSet GetBusinessTypes()
        {
            return Execute("Si2_GetBusinessTypes", CommandType.StoredProcedure);
        }


        public bool AuthenticateUser(string userName, string password)
        {
            string sql = "Si2_AuthenticateUser";
            DataSet data = new DataSet();
            string hashedPassword = GetMD5Hash(password);
            TraceWrite(string.Format("Authenticating: {0}, {1} ({2})", userName, hashedPassword, password), "AuthenticateUser");
            // TraceWrite(_connectionString, "AuthenticateUser");

            using (SqlCommand command = new SqlCommand(sql))
            {
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.Add(NewParameter("@username", 50, userName));
                command.Parameters.Add(NewParameter("@userpassword", 50, hashedPassword));

                data = Execute(command);
            }

            TraceWrite(string.Format("Retrieved {0} tables", data.Tables.Count), "authenticate user");
            try
            {
                TraceWrite(String.Format("Retrieved {0} rows", data.Tables[0].Rows.Count), "authenticate user");
            }
            catch (Exception)
            {
                // never mind
            }

            return AuthenticateUser(data);
        }


        public bool AuthenticateUser(int UserID, string password)
        {
            bool returnValue = false;

            string sql = "Si2_AuthenticateUserByUserId";

            using (SqlCommand command = new SqlCommand(sql))
            {
                DataSet data = null;

                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.Add(NewParameter("@userid", UserID));
                command.Parameters.Add(NewParameter("@userpassword", 50, GetMD5Hash(password)));

                data = Execute(command);

                returnValue = AuthenticateUser(data);
            }

            return returnValue;

        }


        private bool AuthenticateUser(DataSet data)
        {
            bool returnValue = false;
            FormsAuthenticationTicket ticket;

            if (data.Tables.Count > 0 && data.Tables[0].Rows.Count > 0)
            {
                System.Text.StringBuilder builder = new System.Text.StringBuilder("");
                string hash = "";
                HttpCookie cookie;
                string UserID = data.Tables[0].Rows[0]["userID"].ToString();

                string promoName = GetReferralName(int.Parse(UserID));
                builder.Append(promoName + "|");

                string UserFullName = GetUserFullName(int.Parse(UserID));
                builder.Append(UserFullName + "|");

                foreach (DataRow row in data.Tables[0].Rows)
                {
                    Utility.Trace.Write("AuthenticateUser", string.Format("Adding role{0}", row["rolename"].ToString()));
                    builder.Append(row["rolename"].ToString());
                    builder.Append("|");

                }

                FormsAuthentication.Initialize();

                ticket = new FormsAuthenticationTicket(1, UserID, DateTime.Now, DateTime.Now.AddMinutes(30), true, builder.ToString(), FormsAuthentication.FormsCookiePath);

                hash = FormsAuthentication.Encrypt(ticket);
                cookie = new HttpCookie(FormsAuthentication.FormsCookieName, hash);
                HttpContext.Current.Response.Cookies.Add(cookie);

                returnValue = true;
            }

            return returnValue;
        }


        public string GetReferralName(int UserID)
        {
            DataSet data = new DataSet();
            string sql = "Si2_GetReferralCode";
            using (SqlCommand command = new SqlCommand(sql))
            {
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.Add(NewParameter("@memberid", System.Data.SqlDbType.Int, UserID, System.Data.ParameterDirection.Input));
                data = Execute(command);
            }
            if (data.Tables.Count > 0 && data.Tables[0].Rows.Count > 0)
            {
                return (data.Tables[0].Rows[0]["referral_code"].ToString());
            }
            else
            {
                return ("");
            }
        }


        private string GetUserFullName(int UserID)
        {
            DataSet data = new DataSet();
            string sql = "Si2_GetUserFullName";
            using (SqlCommand command = new SqlCommand(sql))
            {
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.Add(NewParameter("@memberid", System.Data.SqlDbType.Int, UserID, System.Data.ParameterDirection.Input));
                data = Execute(command);
            }
            if (data.Tables.Count > 0 && data.Tables[0].Rows.Count > 0)
            {
                string FullName = "";
                if (data.Tables[0].Rows[0]["first_name"].ToString().Length > 0)
                    FullName += data.Tables[0].Rows[0]["first_name"].ToString() + " ";
                if (data.Tables[0].Rows[0]["last_name"].ToString().Length > 0)
                    FullName += data.Tables[0].Rows[0]["last_name"].ToString();
                //if (data.Tables[0].Rows[0]["suffix"].ToString().Length > 0)
                //	FullName += " " + data.Tables[0].Rows[0]["suffix"].ToString();

                return (FullName);
            }
            else
            {
                return ("");
            }
        }


        public bool ReferralCodeExists(string referalCode)
        {
            string sql = "Si2_validateReferralCode";

            SqlCommand command = new SqlCommand(sql);
            DataSet data = new DataSet();


            command.CommandType = CommandType.StoredProcedure;
            command.Parameters.Add(NewParameter("@referralCode", 50, referalCode));
            data = Execute(command);

            if ((int)data.Tables[0].Rows[0]["rec_count"] > 0)
                return true;
            else
                return false;


        }


        public int RegisterUser(SignupData signupData)
        {
            string sql = "Si2_CreateUser";
            int returnValue = 0;

            using (SqlCommand command = new SqlCommand(sql))
            {

                command.CommandType = CommandType.StoredProcedure;

                command.Parameters.Add(NewParameter("@userName", 50, signupData.Username));
                command.Parameters.Add(NewParameter("@password", 50, GetMD5Hash(signupData.Password)));
                command.Parameters.Add(NewParameter("@parentReferralCode", 50, signupData.ParentReferallCode));
                command.Parameters.Add(NewParameter("@referralCode", 50, signupData.ReferralCode));
                command.Parameters.Add(NewParameter("@firstName", 50, signupData.FirstName));
                command.Parameters.Add(NewParameter("@middleInitial", 50, signupData.MiddleName));
                command.Parameters.Add(NewParameter("@lastName", 50, signupData.LastName));
                command.Parameters.Add(NewParameter("@suffix", 50, signupData.Suffix));
                command.Parameters.Add(NewParameter("@business_type", signupData.BusinessType));
                command.Parameters.Add(NewParameter("@business_name", 50, signupData.BusinessName));
                command.Parameters.Add(NewParameter("@type_other", 50, signupData.BusinessTypeOther));
                command.Parameters.Add(NewParameter("@tin", 50, signupData.SSN));
                command.Parameters.Add(NewParameter("@address", 50, signupData.Address.StreetAddress));
                command.Parameters.Add(NewParameter("@city", 50, signupData.Address.City));
                command.Parameters.Add(NewParameter("@state", 50, signupData.Address.State));
                command.Parameters.Add(NewParameter("@zip", 5, signupData.Address.Zip));
                command.Parameters.Add(NewParameter("@phoneNumber", signupData.PhoneNumber));
                command.Parameters.Add(NewParameter("@emailAddress", 50, signupData.EmailAddress));
                command.Parameters.Add(NewParameter("@display_name", 50, signupData.DisplayName));
                command.Parameters.Add(NewParameter("@ProfileImageName", 100, signupData.ProfileImageName));
                command.Parameters.Add(NewParameter("@EIN", 50, signupData.EIN));

                if (signupData.ProfileImage == null)
                {
                    //  command.Parameters.Add(NewParameter("@ProfileImage", 50, null));
                    command.Parameters.Add("@ProfileImage", SqlDbType.VarBinary, -1).Value = DBNull.Value;
                }
                else
                {
                    SqlParameter paramImageData = new SqlParameter()
                    {
                        ParameterName = "@ProfileImage",
                        Value = signupData.ProfileImage
                    };
                    command.Parameters.Add(paramImageData);
                }
                //command.Parameters.Add(NewParameter("@UserID", signupData.UserID));
                command.Parameters.Add(NewParameter("@memberid", SqlDbType.Int, DBNull.Value, ParameterDirection.Output));
                if (signupData.UltimatePackage)
                {
                    command.Parameters.Add(NewParameter("@UltimatePackage", 4, 1));
                }
                else
                {
                    command.Parameters.Add(NewParameter("@UltimatePackage", 4, 0));
                }

                if (signupData.IndependentPackage)
                {
                    command.Parameters.Add(NewParameter("@IndependentPackage", 4, 1));
                }
                else
                {
                    command.Parameters.Add(NewParameter("@IndependentPackage", 4, 0));
                }

                if (signupData.MarketingPackage)
                {
                    command.Parameters.Add(NewParameter("@MarketingPackage", 4, 1));
                }
                else
                {
                    command.Parameters.Add(NewParameter("@MarketingPackage", 4, 0));
                }


                string format = @"
SET @userName = '{0}'
SET @password = '{1}'
SET @parentReferralCode = '{2}'
SET @referralCode = '{3}'
SET @firstName = '{4}'
SET @lastName = '{5}'
SET @middleInitial = '{6}'
SET @suffix = '{7}'
SET @business_type = {8}'
SET @business_name = '{9}'
SET @type_other = {10}'
SET @tin = '{11}'
SET @address = '{12}'
SET @city = '{13}'
SET @State = '{14}'
SET @zip = '{15}'
SET @phoneNumber = '{16}'
SET @emailAddress = '{17}'
SET @display_name = '{18}'
SET @emailAddress = '{19}'
SET @display_name = '{20}'
";
                Utility.Trace.Write("RegisterUser", string.Format(
                    format,
                    command.Parameters[0].Value,
                    command.Parameters[1].Value,
                    command.Parameters[2].Value,
                    command.Parameters[3].Value,
                    command.Parameters[4].Value,
                    command.Parameters[5].Value,
                    command.Parameters[6].Value,
                    command.Parameters[7].Value,
                    command.Parameters[8].Value,
                    command.Parameters[9].Value,
                    command.Parameters[10].Value,
                    command.Parameters[11].Value,
                    command.Parameters[12].Value,
                    command.Parameters[13].Value,
                    command.Parameters[14].Value,
                    command.Parameters[15].Value,
                    command.Parameters[16].Value,
                    command.Parameters[17].Value,
                    command.Parameters[18].Value,
                    command.Parameters[19].Value,
                    command.Parameters[20].Value));

                ExecuteNonQuery(command);



                returnValue = GetInt(command.Parameters["@memberid"]);

                if (returnValue == 0)
                {
                    throw new Exception(string.Format("Invalid use id returned from {0}", sql));
                }
            }

            return returnValue;
        }

        //Code added for adding records for testing using si2_createUser procedure
        public int CreateUser(string Username, string FName, string LName, string SAddress, string City, string State, string Zip, string PhoneNumber, string EmailAddress, double parentID)
        {



            string sql = "Si2_CreateUser";
            int returnValue = 0;
            ReferalCode rc = new ReferalCode();

            string ReferralCode = rc.GenReferalCode();


            DataBase data = new DataBase();
            string ParentReferralCode = "";
            long PID = (long)parentID;
            try
            {
                ParentReferralCode = data.GetReferralCodes(PID);
            }
            catch
            {
                ParentReferralCode = "";
            }


            using (SqlCommand command = new SqlCommand(sql))
            {

                command.CommandType = CommandType.StoredProcedure;

                command.Parameters.Add(NewParameter("@userName", 50, Username));
                command.Parameters.Add(NewParameter("@password", 50, DataBase.GetMD5Hash("password")));
                command.Parameters.Add(NewParameter("@parentReferralCode", 50, ParentReferralCode));
                command.Parameters.Add(NewParameter("@referralCode", 50, ReferralCode));
                command.Parameters.Add(NewParameter("@firstName", 50, FName));
                command.Parameters.Add(NewParameter("@middleInitial", 50, "M"));
                command.Parameters.Add(NewParameter("@lastName", 50, LName));
                command.Parameters.Add(NewParameter("@suffix", 50, "S"));
                command.Parameters.Add(NewParameter("@business_type", 1));
                command.Parameters.Add(NewParameter("@business_name", 50, "Vibe Direct"));
                command.Parameters.Add(NewParameter("@type_other", 50, "Mobile"));
                command.Parameters.Add(NewParameter("@tin", 50, "563700611"));
                command.Parameters.Add(NewParameter("@address", 50, SAddress));
                command.Parameters.Add(NewParameter("@city", 50, City));
                command.Parameters.Add(NewParameter("@state", 50, State));
                command.Parameters.Add(NewParameter("@zip", 5, Zip));
                command.Parameters.Add(NewParameter("@phoneNumber", PhoneNumber));
                command.Parameters.Add(NewParameter("@emailAddress", 50, EmailAddress));
                command.Parameters.Add(NewParameter("@display_name", 50, "Vibe"));
                command.Parameters.Add(NewParameter("@memberid", SqlDbType.Int, DBNull.Value, ParameterDirection.Output));
                command.Parameters.Add(NewParameter("@UltimatePackage", 4, 0));
                command.Parameters.Add(NewParameter("@IndependentPackage", 4, 0));
                command.Parameters.Add(NewParameter("@MarketingPackage", 4, 0));

                string format = @"
SET @userName = '{0}'
SET @password = '{1}'
SET @parentReferralCode = '{2}'
SET @referralCode = '{3}'
SET @firstName = '{4}'
SET @lastName = '{5}'
SET @middleInitial = '{6}'
SET @suffix = '{7}'
SET @business_type = {8}'
SET @business_name = '{9}'
SET @type_other = {10}'
SET @tin = '{11}'
SET @address = '{12}'
SET @city = '{13}'
SET @State = '{14}'
SET @zip = '{15}'
SET @phoneNumber = '{16}'
SET @emailAddress = '{17}'
SET @display_name = '{18}'
";
                Utility.Trace.Write("RegisterUser", string.Format(
                    format,
                    command.Parameters[0].Value,
                    command.Parameters[1].Value,
                    command.Parameters[2].Value,
                    command.Parameters[3].Value,
                    command.Parameters[4].Value,
                    command.Parameters[5].Value,
                    command.Parameters[6].Value,
                    command.Parameters[7].Value,
                    command.Parameters[8].Value,
                    command.Parameters[9].Value,
                    command.Parameters[10].Value,
                    command.Parameters[11].Value,
                    command.Parameters[12].Value,
                    command.Parameters[13].Value,
                    command.Parameters[14].Value,
                    command.Parameters[15].Value,
                    command.Parameters[16].Value,
                    command.Parameters[17].Value,
                    command.Parameters[17].Value));

                ExecuteNonQuery(command);



                returnValue = GetInt(command.Parameters["@memberid"]);

                if (returnValue == 0)
                {
                    throw new Exception(string.Format("Invalid use id returned from {0}", sql));
                }
            }

            return returnValue;
        }
        //End of code added for adding records for testing using si2_createUser procedure
        public void UpgradeMembershipPackage(int userID, string referralCode, bool marketingAssistance)
        {
            string sql = "Si2_UpgradeMembershipPackages";

            using (SqlCommand command = new SqlCommand(sql))
            {

                command.CommandType = CommandType.StoredProcedure;

                command.Parameters.Add(NewParameter("@memberid", userID));
                command.Parameters.Add(NewParameter("@referralCode", 50, referralCode));

                if (marketingAssistance)
                {
                    command.Parameters.Add(NewParameter("@MarketingPackage", 4, 1));
                }
                else
                {
                    command.Parameters.Add(NewParameter("@MarketingPackage", 4, 0));
                }

                ExecuteNonQuery(command);

            }

        }


        public void SetEmail(int userID, string email)
        {
            string sql = "si2_SetEmail";
            using (SqlCommand command = new SqlCommand(sql))
            {
                command.CommandType = CommandType.StoredProcedure;

                command.Parameters.Add(NewParameter("@memberid", userID));
                command.Parameters.Add(NewParameter("@email", email));

                ExecuteNonQuery(command);
            }
        }


        public void SetPassword(int userID, string newPassword)
        {
            string sql = "Si2_SetPassword";
            string password = GetMD5Hash(newPassword);

            using (SqlCommand command = new SqlCommand(sql))
            {
                command.CommandType = CommandType.StoredProcedure;

                command.Parameters.Add(NewParameter("@memberid", userID));
                command.Parameters.Add(NewParameter("@password", password));

                ExecuteNonQuery(command);
            }
        }

        //New Function added for User_Enquiry
        public string SetUser_Enquiry(string FirstName, string LastName, string Email, int IsDeleted)
        {
            try
            {
                string sql = "Si2_Set_USR_User_Enquiry";
                using (SqlCommand command = new SqlCommand(sql))
                {
                    command.CommandType = CommandType.StoredProcedure;

                    command.Parameters.Add(NewParameter("@firstName", FirstName));
                    command.Parameters.Add(NewParameter("@lastName", LastName));
                    command.Parameters.Add(NewParameter("@emailAddress", Email));
                    command.Parameters.Add(NewParameter("@IsDeleted", IsDeleted));

                    ExecuteNonQuery(command);
                    return "";
                }
            }
            catch (Exception ex)
            {
                return ex.Message.ToString();
            }
        }
        public DataSet GetUserAdminInfo(int userID)
        {
            string sql = "si2_GetUserAdminInfo";
            DataSet data = null;

            using (SqlCommand command = new SqlCommand(sql))
            {
                command.CommandType = CommandType.StoredProcedure;

                command.Parameters.Add(NewParameter("@userid", userID));
                data = Execute(command);
            }

            return data;
        }

        public bool IsUserCancelled(int userID)
        {
            string sql = "Si2_GetMemberType";
            DataSet data = null;

            bool cancelled = true;

            using (SqlCommand command = new SqlCommand(sql))
            {
                command.CommandType = CommandType.StoredProcedure;

                command.Parameters.Add(NewParameter("@memberid", userID));
                data = Execute(command);

                if (data.Tables[0].Rows[0]["member_type"].ToString() != "0")
                {
                    cancelled = false;
                }
            }

            return cancelled;
        }


        public DataSet SearchUser(string referralCode, string promoName, string userName, string parentRefCode, string lastName, string firstName, string city, string state, string signUpDate, string memberid)
        {
            DataSet returnData = new DataSet();
            string sql = "si2_SearchUser";

            using (SqlCommand command = new SqlCommand(sql))
            {
                command.CommandType = CommandType.StoredProcedure;

                command.Parameters.Add(GetNullParameterIfEmpty("@memberid", 7, memberid));
                command.Parameters.Add(GetNullParameterIfEmpty("@referralCode", 10, referralCode));
                command.Parameters.Add(GetNullParameterIfEmpty("@promoName", 40, promoName));
                command.Parameters.Add(GetNullParameterIfEmpty("@username", 50, userName));
                command.Parameters.Add(GetNullParameterIfEmpty("@parentRefCode", 10, parentRefCode));
                command.Parameters.Add(GetNullParameterIfEmpty("@lastName", 50, lastName));
                command.Parameters.Add(GetNullParameterIfEmpty("@firstName", 50, firstName));
                command.Parameters.Add(GetNullParameterIfEmpty("@city", 50, city));
                command.Parameters.Add(GetNullParameterIfEmpty("@state", 2, state));
                command.Parameters.Add(GetNullParameterIfEmpty("@signUpDate", 25, signUpDate));
                //				if (signUpDate==null || signUpDate=="")
                //					command.Parameters.Add("@signUpDate", null);
                //				else
                //					command.Parameters.Add("@signUpDate",DateTime.Parse(signUpDate));

                command.CommandTimeout = 0;
                returnData = Execute(command);

                Utility.Trace.Write("referralCode", string.Format("Rows returned: {0}", returnData.Tables[0].Rows.Count));


            }

            return returnData;


        }
        #endregion User and authentication methods

        #region billing and payment methods
        public DataSet GetPaymentHistory(long memberID, DateTime startDate, DateTime endDate)
        {
            DataSet returnData = new DataSet();

            using (SqlCommand command = new SqlCommand("Si2_GetPaymentHistory"))
            {
                command.CommandType = CommandType.StoredProcedure;

                command.Parameters.Add(NewParameter("@memberid", memberID));
                command.Parameters.Add(NewParameter("@start_date", startDate));
                command.Parameters.Add(NewParameter("@end_date", endDate));

                returnData = Execute(command);
            }

            return returnData;
        }

        public DataSet GetClearCommerceLogHistory(int memberID)
        {
            DataSet returnData = new DataSet();

            using (SqlCommand command = new SqlCommand("GetClearCommerceLogHistory"))
            {
                command.CommandType = CommandType.StoredProcedure;

                command.Parameters.Add(NewParameter("@memberid", memberID));
                //				command.Parameters.Add(NewParameter("@productname", ProductName));


                returnData = Execute(command);
            }

            return returnData;
        }

        public void ClearBillingInfo(int userID)
        {
            string sql = "update usr_billingInfo set timestamp_e = getdate() where timestamp_e = '1/1/2200' and memberID = @memberID";

            using (SqlCommand command = new SqlCommand(sql))
            {
                command.CommandType = CommandType.Text;

                command.Parameters.Add(NewParameter("@memberid", userID));

                Execute(command);
            }

        }

        public bool UserHasBillingInfo(int userID)
        {
            string sql = "select * from usr_billingInfo where timestamp_e = '1/1/2200' and memberID = @memberID";
            bool retval = false;
            DataSet returnData = new DataSet();

            using (SqlCommand command = new SqlCommand(sql))
            {
                command.CommandType = CommandType.Text;

                command.Parameters.Add(NewParameter("@memberid", userID));

                returnData = Execute(command);
                if (returnData.Tables[0].Rows.Count > 0)
                    retval = true;
            }
            return retval;

        }

        #endregion billing and payment methods

        #region Commissions
        public string GetCommissionReportLink(int period, long userID, int scheduleID, int orgLevelID)
        {
            //fix for dating org differences
            //if(scheduleID >= 1000 && scheduleID < 20000)
            //	orgLevelID++;
            //end dating org fixes

            DataSet data = new DataSet();
            string sql = "Si2_GetCommissionReportCountByPeriod";

            using (SqlCommand command = new SqlCommand(sql))
            {
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.Add(NewParameter("@period", period));
                command.Parameters.Add(NewParameter("@memberid", userID));
                command.Parameters.Add(NewParameter("@scheduleID", scheduleID));
                command.Parameters.Add(NewParameter("@orgLevelID", orgLevelID));
                data = Execute(command);
            }
            string href = "<A class=\"helplink\" href=\"javascript:MM_openBrWindow('commissionpopup.aspx?PeriodID=" + period.ToString() + "&productID=" + scheduleID + "&orglevelID=" + orgLevelID + "','commissionpopup','toolbar=no,width=670,height=350,status=no,scrollbars=yes,resizable=yes,menubar=no')\">";
            return href + DataBase.GetInt(data.Tables[0].Rows[0], "ItemCount").ToString() + "</A>";

        }
        public int GetCommissionReportCount(int period, long userID, int scheduleID, int orgLevelID)
        {
            //fix for dating org differences
            //if(scheduleID >= 1000 && scheduleID < 20000)
            //	orgLevelID++;
            //end dating org fixes

            DataSet data = new DataSet();
            string sql = "Si2_GetCommissionReportCountByPeriod";

            using (SqlCommand command = new SqlCommand(sql))
            {
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.Add(NewParameter("@period", period));
                command.Parameters.Add(NewParameter("@memberid", userID));
                command.Parameters.Add(NewParameter("@scheduleID", scheduleID));
                command.Parameters.Add(NewParameter("@orgLevelID", orgLevelID));
                data = Execute(command);
            }
            return DataBase.GetInt(data.Tables[0].Rows[0], "ItemCount");

        }


        public int GetCommissionReportCount(int month, int year, long userID, int scheduleID, int orgLevelID, int weekNumber)
        {
            //fix for dating org differences
            //if(scheduleID >= 1000 && scheduleID < 20000)
            //	orgLevelID++;
            //end dating org fixes

            DataSet data = new DataSet();
            string sql = "Si2_GetCommissionReportCount";

            using (SqlCommand command = new SqlCommand(sql))
            {
                command.CommandType = CommandType.StoredProcedure;

                command.Parameters.Add(NewParameter("@memberid", userID));
                command.Parameters.Add(NewParameter("@month", month));
                command.Parameters.Add(NewParameter("@year", year));
                command.Parameters.Add(NewParameter("@scheduleID", scheduleID));
                command.Parameters.Add(NewParameter("@orgLevelID", orgLevelID));
                command.Parameters.Add(NewParameter("@weekNumber", weekNumber));

                data = Execute(command);
            }

            return DataBase.GetInt(data.Tables[0].Rows[0], "ItemCount");
        }

        public Decimal GetCommissionReportAmount(int month, int year, long userID, int scheduleID, int orgLevelID, int weekNumber)
        {
            //fix for dating org differences
            //if(scheduleID >= 1000 && scheduleID < 20000)
            //	orgLevelID++;
            //end dating org fixes

            DataSet data = new DataSet();
            string sql = "Si2_GetCommissionReportAmount";

            using (SqlCommand command = new SqlCommand(sql))
            {
                command.CommandType = CommandType.StoredProcedure;

                command.Parameters.Add(NewParameter("@memberid", userID));
                command.Parameters.Add(NewParameter("@month", month));
                command.Parameters.Add(NewParameter("@year", year));
                command.Parameters.Add(NewParameter("@scheduleID", scheduleID));
                command.Parameters.Add(NewParameter("@orgLevelID", orgLevelID));
                command.Parameters.Add(NewParameter("@weekNumber", weekNumber));

                data = Execute(command);
            }

            return DataBase.GetDecimal(data.Tables[0].Rows[0], "Amount");

        }
        public Decimal GetCommissionReportAmount(int period, long userID, int scheduleID, int orgLevelID)
        {
            //fix for dating org differences
            //if(scheduleID >= 1000 && scheduleID < 20000)
            //	orgLevelID++;
            //end dating org fixes

            DataSet data = new DataSet();
            string sql = "Si2_GetCommissionReportAmountByPeriod";

            using (SqlCommand command = new SqlCommand(sql))
            {
                command.CommandType = CommandType.StoredProcedure;

                command.Parameters.Add(NewParameter("@memberid", userID));
                command.Parameters.Add(NewParameter("@period", period));
                command.Parameters.Add(NewParameter("@scheduleID", scheduleID));
                command.Parameters.Add(NewParameter("@orgLevelID", orgLevelID));

                data = Execute(command);
            }

            return DataBase.GetDecimal(data.Tables[0].Rows[0], "Amount");

        }

        public Decimal GetCommissionReportTotalAmount(int month, int year, long userID, int scheduleID, int weekNumber)
        {
            DataSet data = new DataSet();
            string sql = "Si2_GetCommissionReportTotalAmount";

            using (SqlCommand command = new SqlCommand(sql))
            {
                command.CommandType = CommandType.StoredProcedure;

                command.Parameters.Add(NewParameter("@memberid", userID));
                command.Parameters.Add(NewParameter("@month", month));
                command.Parameters.Add(NewParameter("@year", year));
                command.Parameters.Add(NewParameter("@scheduleID", scheduleID));
                command.Parameters.Add(NewParameter("@weekNumber", weekNumber));

                data = Execute(command);
            }

            return DataBase.GetDecimal(data.Tables[0].Rows[0], "Amount");

        }

        public Decimal GetCommissionReportTotalAmount(int period, long userID, int scheduleID)
        {
            DataSet data = new DataSet();
            string sql = "Si2_GetCommissionReportTotalAmountByPeriod";

            using (SqlCommand command = new SqlCommand(sql))
            {
                command.CommandType = CommandType.StoredProcedure;

                command.Parameters.Add(NewParameter("@memberid", userID));
                command.Parameters.Add(NewParameter("@period", period));
                command.Parameters.Add(NewParameter("@scheduleID", scheduleID));

                data = Execute(command);
            }

            return DataBase.GetDecimal(data.Tables[0].Rows[0], "Amount");

        }


        public DataSet GetCommissionPopUp(long memberID, long periodID)
        {
            DataSet data = new DataSet();
            string sql = "Si2_GetCommissionPopUp";

            using (SqlCommand command = new SqlCommand(sql))
            {
                command.CommandType = CommandType.StoredProcedure;

                command.Parameters.Add(NewParameter("@memberID", memberID));
                command.Parameters.Add(NewParameter("@period", periodID));

                data = Execute(command);
            }

            return data;
        }

        public DataSet GetCommissionPopUp(long memberID, long periodID, int productID, int orgLevelID)
        {
            DataSet data = new DataSet();
            string sql = "Si2_GetCommissionDetailPopUp";

            using (SqlCommand command = new SqlCommand(sql))
            {
                command.CommandType = CommandType.StoredProcedure;

                command.Parameters.Add(NewParameter("@memberID", memberID));
                command.Parameters.Add(NewParameter("@period", periodID));
                command.Parameters.Add(NewParameter("@productID", productID));
                command.Parameters.Add(NewParameter("@orgLevelID", orgLevelID));

                data = Execute(command);
            }

            return data;
        }

        public DataSet GetScheduleAmounts(int period, int scheduleID)
        {
            DataSet data = new DataSet();
            string sql = "Si2_GetScheduleAmountsByPeriod";

            using (SqlCommand command = new SqlCommand(sql))
            {
                command.CommandType = CommandType.StoredProcedure;

                command.Parameters.Add(NewParameter("@period", period));
                command.Parameters.Add(NewParameter("@scheduleID", scheduleID));

                data = Execute(command);
            }

            return data;
        }

        public DataSet GetScheduleAmounts(int month, int year, int scheduleID)
        {
            DataSet data = new DataSet();
            string sql = "Si2_GetScheduleAmounts";

            using (SqlCommand command = new SqlCommand(sql))
            {
                command.CommandType = CommandType.StoredProcedure;

                command.Parameters.Add(NewParameter("@month", month));
                command.Parameters.Add(NewParameter("@year", year));
                command.Parameters.Add(NewParameter("@scheduleID", scheduleID));

                data = Execute(command);
            }

            return data;
        }

        public DataSet GetCommisionSummaryForPartner(long userID, int month, int year)
        {
            DataSet data = new DataSet();

            data.Tables.Add(new DataTable());
            data.Tables[0].Columns.Add("productname");
            data.Tables[0].Columns.Add("Week1_amount");
            data.Tables[0].Columns.Add("Week2_amount");
            data.Tables[0].Columns.Add("Week3_amount");
            data.Tables[0].Columns.Add("Week4_amount");
            data.Tables[0].Columns.Add("month_amount");
            data.Tables[0].Columns.Add("productid");
            data.Tables[0].Columns.Add("chosenmonth");
            data.Tables[0].Columns.Add("chosenyear");



            data.Tables[0].ImportRow(GetCommisionSummaryForPartner(userID, month, year, 1000).Tables[0].Rows[0]);
            data.Tables[0].ImportRow(GetCommisionSummaryForPartner(userID, month, year, 1010).Tables[0].Rows[0]);
            data.Tables[0].ImportRow(GetCommisionSummaryForPartner(userID, month, year, 1020).Tables[0].Rows[0]);
            data.Tables[0].ImportRow(GetCommisionSummaryForPartner(userID, month, year, 20000).Tables[0].Rows[0]);
            data.Tables[0].ImportRow(GetCommisionSummaryForPartner(userID, month, year, 2020).Tables[0].Rows[0]);
            data.Tables[0].ImportRow(GetCommisionSummaryForPartner(userID, month, year, 6000).Tables[0].Rows[0]);
            data.Tables[0].ImportRow(GetCommisionSummaryForPartner(userID, month, year, 6020).Tables[0].Rows[0]);
            data.Tables[0].ImportRow(GetCommisionSummaryForPartner(userID, month, year, 6030).Tables[0].Rows[0]);

            data.Tables[0].ImportRow(GetCommisionSummaryForPartner(userID, month, year, 7000).Tables[0].Rows[0]);
            data.Tables[0].ImportRow(GetCommisionSummaryForPartner(userID, month, year, 7005).Tables[0].Rows[0]);
            data.Tables[0].ImportRow(GetCommisionSummaryForPartner(userID, month, year, 7010).Tables[0].Rows[0]);
            //			data.Tables[0].ImportRow(GetCommisionSummaryForPartner(userID,month,year,9000).Tables[0].Rows[0]);
            //			data.Tables[0].ImportRow(GetCommisionSummaryForPartner(userID,month,year,9005).Tables[0].Rows[0]);
            data.Tables[0].ImportRow(GetCommisionSummaryForPartner(userID, month, year, 9030).Tables[0].Rows[0]);
            data.Tables[0].ImportRow(GetCommisionSummaryForPartner(userID, month, year, 9035).Tables[0].Rows[0]);
            return data;
        }

        public DataSet GetCommisionSummaryForPartner(long userID, int month, int year, int scheduleID)
        {
            DataSet data = new DataSet();
            string sql = "Si2_GetCommissionReportSummary";

            using (SqlCommand command = new SqlCommand(sql))
            {
                command.CommandType = CommandType.StoredProcedure;

                command.Parameters.Add(NewParameter("@memberid", userID));
                command.Parameters.Add(NewParameter("@month", month));
                command.Parameters.Add(NewParameter("@year", year));
                command.Parameters.Add(NewParameter("@scheduleID", scheduleID));

                data = Execute(command);
            }

            return data;
        }

        public DataSet GetCommisionSummaryForPartner(long userID, int periodID)
        {
            DataSet data = new DataSet();

            data.Tables.Add(new DataTable());
            data.Tables[0].Columns.Add("productname");
            data.Tables[0].Columns.Add("Week1_amount");
            data.Tables[0].Columns.Add("Week2_amount");
            data.Tables[0].Columns.Add("Week3_amount");
            data.Tables[0].Columns.Add("Week4_amount");
            data.Tables[0].Columns.Add("month_amount");
            data.Tables[0].Columns.Add("productid");
            //data.Tables[0].Columns.Add("chosenmonth");
            //data.Tables[0].Columns.Add("chosenyear");

            DataSet Products = GetCommissionSummaryReportProducts(userID, periodID);
            foreach (DataRow row in Products.Tables[0].Rows)
            {
                data.Tables[0].ImportRow(GetCommisionSummaryForPartnerByPeriod(userID, periodID, int.Parse(row[0].ToString())).Tables[0].Rows[0]);
            }

            //			data.Tables[0].ImportRow(GetCommisionSummaryForPartnerByPeriod(userID,periodID,1000).Tables[0].Rows[0]);
            //			data.Tables[0].ImportRow(GetCommisionSummaryForPartnerByPeriod(userID,periodID,1010).Tables[0].Rows[0]);
            //			data.Tables[0].ImportRow(GetCommisionSummaryForPartnerByPeriod(userID,periodID,1020).Tables[0].Rows[0]);
            //			data.Tables[0].ImportRow(GetCommisionSummaryForPartnerByPeriod(userID,periodID,2000).Tables[0].Rows[0]);
            //			data.Tables[0].ImportRow(GetCommisionSummaryForPartnerByPeriod(userID,periodID,2010).Tables[0].Rows[0]);
            //			data.Tables[0].ImportRow(GetCommisionSummaryForPartnerByPeriod(userID,periodID,2020).Tables[0].Rows[0]);
            //			data.Tables[0].ImportRow(GetCommisionSummaryForPartnerByPeriod(userID,periodID,20000).Tables[0].Rows[0]);
            //			data.Tables[0].ImportRow(GetCommisionSummaryForPartnerByPeriod(userID,periodID,20010).Tables[0].Rows[0]);
            //			data.Tables[0].ImportRow(GetCommisionSummaryForPartnerByPeriod(userID,periodID,20020).Tables[0].Rows[0]);
            //			data.Tables[0].ImportRow(GetCommisionSummaryForPartnerByPeriod(userID,periodID,20030).Tables[0].Rows[0]);
            //			data.Tables[0].ImportRow(GetCommisionSummaryForPartnerByPeriod(userID,periodID,6000).Tables[0].Rows[0]);
            //			data.Tables[0].ImportRow(GetCommisionSummaryForPartnerByPeriod(userID,periodID,6020).Tables[0].Rows[0]);
            //			data.Tables[0].ImportRow(GetCommisionSummaryForPartnerByPeriod(userID,periodID,6030).Tables[0].Rows[0]);
            //
            //			data.Tables[0].ImportRow(GetCommisionSummaryForPartnerByPeriod(userID,periodID,7000).Tables[0].Rows[0]);
            //			data.Tables[0].ImportRow(GetCommisionSummaryForPartnerByPeriod(userID,periodID,7005).Tables[0].Rows[0]);
            //			data.Tables[0].ImportRow(GetCommisionSummaryForPartnerByPeriod(userID,periodID,7010).Tables[0].Rows[0]);
            //			//data.Tables[0].ImportRow(GetCommisionSummaryForPartnerByPeriod(userID,periodID,9000).Tables[0].Rows[0]);
            //			data.Tables[0].ImportRow(GetCommisionSummaryForPartnerByPeriod(userID,periodID,9005).Tables[0].Rows[0]);
            //			//data.Tables[0].ImportRow(GetCommisionSummaryForPartnerByPeriod(userID,periodID,9030).Tables[0].Rows[0]);
            //			data.Tables[0].ImportRow(GetCommisionSummaryForPartnerByPeriod(userID,periodID,9035).Tables[0].Rows[0]);

            return data;
        }

        public DataSet GetCommissionSummaryReportProducts(long userID, int periodID)
        {
            DataSet data = new DataSet();
            string sql = "Si2_GetCommissionReportProducts";

            using (SqlCommand command = new SqlCommand(sql))
            {
                command.CommandType = CommandType.StoredProcedure;

                command.Parameters.Add(NewParameter("@memberid", userID));
                command.Parameters.Add(NewParameter("@week1Period", GetPeriodByOffset(periodID, 0, 0)));
                command.Parameters.Add(NewParameter("@week2Period", GetPeriodByOffset(periodID, -1, 0)));
                command.Parameters.Add(NewParameter("@week3Period", GetPeriodByOffset(periodID, -2, 0)));
                command.Parameters.Add(NewParameter("@week4Period", GetPeriodByOffset(periodID, -3, 0)));
                command.Parameters.Add(NewParameter("@monthPeriod", GetPeriodByOffset(periodID, 0, 1)));

                data = Execute(command);
            }

            return data;
        }

        public DataSet GetCommisionSummaryForPartnerByPeriod(long userID, int periodID, int scheduleID)
        {
            DataSet data = new DataSet();
            string sql = "Si2_GetCommissionReportSummaryByPeriod";

            using (SqlCommand command = new SqlCommand(sql))
            {
                command.CommandType = CommandType.StoredProcedure;

                command.Parameters.Add(NewParameter("@memberid", userID));
                command.Parameters.Add(NewParameter("@week1Period", GetPeriodByOffset(periodID, 0, 0)));
                command.Parameters.Add(NewParameter("@week2Period", GetPeriodByOffset(periodID, -1, 0)));
                command.Parameters.Add(NewParameter("@week3Period", GetPeriodByOffset(periodID, -2, 0)));
                command.Parameters.Add(NewParameter("@week4Period", GetPeriodByOffset(periodID, -3, 0)));
                command.Parameters.Add(NewParameter("@monthPeriod", GetPeriodByOffset(periodID, 0, 1)));
                command.Parameters.Add(NewParameter("@scheduleID", scheduleID));

                data = Execute(command);
            }

            return data;
        }
        public DateTime GetCommissionPeriodStart(int periodID)
        {
            DataSet data = new DataSet();
            string sql = "Si2_GetCommissionPeriod";

            using (SqlCommand command = new SqlCommand(sql))
            {
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.Add(NewParameter("@periodID", periodID));
                data = Execute(command);
            }
            return (DateTime)data.Tables[0].Rows[0]["TIMESTAMP_PERIOD_START"];

        }
        public DateTime GetCommissionPeriodEnd(int periodID)
        {
            DataSet data = new DataSet();
            string sql = "Si2_GetCommissionPeriod";

            using (SqlCommand command = new SqlCommand(sql))
            {
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.Add(NewParameter("@periodID", periodID));
                data = Execute(command);
            }
            return (DateTime)data.Tables[0].Rows[0]["TIMESTAMP_PERIOD_END"];

        }
        public string GetCommissionPeriodLablesByOffset(int periodID, int offset, int monthPeriodsOnly)
        {
            int retPeriod = GetPeriodByOffset(periodID, offset, monthPeriodsOnly);
            DataSet data = new DataSet();
            string sql = "Si2_GetCommissionPeriod";

            using (SqlCommand command = new SqlCommand(sql))
            {
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.Add(NewParameter("@periodID", retPeriod));
                data = Execute(command);
            }
            DateTime startDate = (DateTime)data.Tables[0].Rows[0]["TIMESTAMP_PERIOD_START"];
            DateTime endDate = (DateTime)data.Tables[0].Rows[0]["TIMESTAMP_PERIOD_END"];
            return startDate.ToString("MMM dd") + "-<br>" + endDate.ToString("MMM dd");
        }
        public int GetPeriodByOffset(int periodID, int offset, int monthPeriodsOnly)
        {
            DataSet data = new DataSet();
            string sql = "";

            if (offset < 0)
            {
                sql = "Si2_GetCommissionPeriodsByOffsetRev";
                offset = Math.Abs(offset);
            }
            else
            {
                sql = "Si2_GetCommissionPeriodsByOffset";
            }

            using (SqlCommand command = new SqlCommand(sql))
            {
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.Add(NewParameter("@periodID", periodID));
                command.Parameters.Add(NewParameter("@month", monthPeriodsOnly));

                data = Execute(command);
            }
            return (int)data.Tables[0].Rows[offset]["PeriodID"];

        }

        public DataSet GetCommissionPeriods()
        {
            DataSet data = new DataSet();
            string sql = "Si2_GetCommissionPeriods";

            using (SqlCommand command = new SqlCommand(sql))
            {
                command.CommandType = CommandType.StoredProcedure;
                data = Execute(command);
            }

            return data;

        }


        /*
		public DataSet GetCommisionSummaryForPartner(long userID, int month, int year)
		{
			DataSet data = new DataSet();
			string sql = "Si2_GetCommisionSummaryForPartner";

			using (SqlCommand command = new SqlCommand(sql))
			{
				command.CommandType = CommandType.StoredProcedure;

				command.Parameters.Add(NewParameter("@memberid", userID));
				command.Parameters.Add(NewParameter("@month", month));
				command.Parameters.Add(NewParameter("@year", year));

				data = Execute(command);
			}

			return data;
		}
		*/

        public DataSet GetCommisionDetailForPartner(long userID, int month, int year, int productID)
        {
            DataSet data = new DataSet();
            string sql = "Si2_GetCommisionDetailForPartner";

            using (SqlCommand command = new SqlCommand(sql))
            {
                command.CommandType = CommandType.StoredProcedure;

                command.Parameters.Add(NewParameter("@productid", productID));
                command.Parameters.Add(NewParameter("@memberid", userID));
                command.Parameters.Add(NewParameter("@month", month));
                command.Parameters.Add(NewParameter("@year", year));

                data = Execute(command);
            }

            return data;
        }

        public DataSet GetUserCommItems(long memberID)
        {
            DataSet data = new DataSet();
            string sql = "Si2_GetUserCommItems";

            using (SqlCommand command = new SqlCommand(sql))
            {
                command.CommandType = CommandType.StoredProcedure;

                command.Parameters.Add(NewParameter("@memberID", memberID));

                data = Execute(command);
            }

            return data;
        }
        public DataSet GetUserCancelItems(long memberID)
        {
            DataSet data = new DataSet();
            string sql = "Si2_GetUserCancelItems";

            using (SqlCommand command = new SqlCommand(sql))
            {
                command.CommandType = CommandType.StoredProcedure;

                command.Parameters.Add(NewParameter("@memberID", memberID));

                data = Execute(command);
            }

            return data;
        }

        #endregion Commissions

        #region Rep Site & Referral Info
        public void SetRepReferral(long memberID, string referralCode)
        {
            string sql = "Si2_SetRepReferral";
            using (SqlCommand command = new SqlCommand(sql))
            {
                command.CommandType = CommandType.StoredProcedure;

                command.Parameters.Add(NewParameter("@memberid", memberID));
                command.Parameters.Add(NewParameter("@referral_code", referralCode));

                ExecuteNonQuery(command);
            }

        }

        public void SetCommunications(int userID, int commTypeOne, bool optoutOne, int commTypeTwo, bool optoutTwo, int commTypeThree, bool optoutThree)
        {
            string sql = "Si2_SetCommunications";
            using (SqlCommand command = new SqlCommand(sql))
            {
                command.CommandType = CommandType.StoredProcedure;

                command.Parameters.Add(NewParameter("@memberid", userID));
                command.Parameters.Add(NewParameter("@commTypeOne", commTypeOne));
                command.Parameters.Add(NewParameter("@optoutOne", optoutOne));

                command.Parameters.Add(NewParameter("@commTypeTwo", commTypeTwo));
                command.Parameters.Add(NewParameter("@optoutTwo", optoutTwo));


                command.Parameters.Add(NewParameter("@commTypeThree", commTypeThree));
                command.Parameters.Add(NewParameter("@optoutThree", optoutThree));

                ExecuteNonQuery(command);

            }
        }

        public DataSet GetCommunicationChoices(int userID)
        {
            DataSet returnData = null;
            string sql = "Si2_GetCommunicationAndEmail";
            using (SqlCommand command = new SqlCommand(sql))
            {
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.Add(NewParameter("@memberid", userID));

                returnData = Execute(command);
            }


            return returnData;
        }

        public DataSet GetReferralDisplayInfo(string promoName)
        {
            string sql = "Si2_GetReferralDisplayInfo";

            DataSet returnData = new DataSet();

            using (SqlCommand command = new SqlCommand(sql))
            {
                command.CommandType = CommandType.StoredProcedure;

                command.Parameters.Add(NewParameter("@promoname", 50, promoName));

                returnData = Execute(command);
            }
            if (returnData.Tables[0].Rows.Count > 0)
                return returnData;
            else
                return GetReferralDisplayInfoByReferralCode(promoName);
        }

        public DataSet GetRepInfo(string referralCode)
        {
            string sql = "Si2_GetRepInfo";
            DataSet returnData = new DataSet();

            using (SqlCommand command = new SqlCommand(sql))
            {
                command.CommandType = CommandType.StoredProcedure;

                command.Parameters.Add(NewParameter("@referralCode", 50, referralCode));

                returnData = Execute(command);
            }

            return returnData;
        }

        public DataSet GetReferralDisplayInfoByReferralCode(string referralCode)
        {
            string sql = "Si2_GetReferralDisplayInfoByReferralCode";
            DataSet returnData = new DataSet();

            using (SqlCommand command = new SqlCommand(sql))
            {
                command.CommandType = CommandType.StoredProcedure;

                command.Parameters.Add(NewParameter("@referralCode", 50, referralCode));

                returnData = Execute(command);
            }

            return returnData;
        }

        public string GetReferralCodes(long memberID)
        {
            string sql = "Si2_GetAllReferralCodes";
            string returnValue = "";

            using (SqlCommand command = new SqlCommand(sql))
            {
                command.CommandType = CommandType.StoredProcedure;

                command.Parameters.Add(NewParameter("@memberid", memberID));

                returnValue = (string)ExcecuteScalar(command);
            }

            return returnValue;
        }

        public string GetBusinessNameByReferalCode(string referralCode)
        {
            string returnValue = "";
            string sql = "Si2_GetBusinessNameByReferalCode";
            using (SqlCommand command = new SqlCommand(sql))
            {
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.Add(NewParameter("@referralcode", 10, referralCode));

                try
                {
                    returnValue = (string)ExcecuteScalar(command);
                }
                catch (Exception exception)
                {
                    Utility.Trace.Write("GetBusinessNameByreferralCode", "!", exception);
                }
            }

            return returnValue;
        }

        public DataSet GetRepSiteInfo(int userID)
        {
            string sql = "Si2_GetRepSiteInfo";
            DataSet returnData = new DataSet();

            using (SqlCommand command = new SqlCommand(sql))
            {
                command.CommandType = CommandType.StoredProcedure;

                command.Parameters.Add(NewParameter("@memberid", userID));

                returnData = Execute(command);
            }

            return returnData;
        }


        public bool SetShowEmail(int userID, bool showEmail)
        {
            bool success = false;

            string sql = "Si2_SetShowEmail";
            int showEmailNumeric = 0;

            if (showEmail)
            {
                showEmailNumeric = 1;
            }

            using (SqlCommand command = new SqlCommand(sql))
            {
                command.CommandType = CommandType.StoredProcedure;

                command.Parameters.Add(NewParameter("@memberid", userID));
                command.Parameters.Add(NewParameter("@showemail", showEmailNumeric));

                ExecuteNonQuery(command);

                success = true;
            }

            return success;

        }

        public bool SetShowPhone(int userID, bool showPhone)
        {
            bool success = false;

            string sql = "Si2_SetShowPhone";
            int showPhoneNumeric = 0;

            if (showPhone)
            {
                showPhoneNumeric = 1;
            }

            using (SqlCommand command = new SqlCommand(sql))
            {
                command.CommandType = CommandType.StoredProcedure;

                command.Parameters.Add(NewParameter("@memberid", userID));
                command.Parameters.Add(NewParameter("@showPhone", showPhoneNumeric));

                ExecuteNonQuery(command);

                success = true;
            }

            return success;

        }

        public bool UserNameExists(string userName)
        {
            string sql = "si2_UserNameExists";
            bool returnValue = false;

            using (SqlCommand command = new SqlCommand(sql))
            {

                command.CommandType = CommandType.StoredProcedure;

                command.Parameters.Add(NewParameter("@username", 40, userName));
                command.Parameters.Add(NewParameter("@exists", SqlDbType.Bit, DBNull.Value, ParameterDirection.Output));

                ExecuteNonQuery(command);
                returnValue = GetBool(command.Parameters["@exists"]);
            }

            return returnValue;
        }

        public string ResetPasswordByEmailAddress(string emailAddress, string newPassword)
        {
            string sql = "Si2_ResetPasswordByEmailAddress";
            string returnValue = "";

            using (SqlCommand command = new SqlCommand(sql))
            {

                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.Add(NewParameter("@email", 100, emailAddress));
                command.Parameters.Add(NewParameter("@newPassword", 100, GetMD5Hash(newPassword)));
                command.Parameters.Add(NewParameter("@username", SqlDbType.VarChar, 50, DBNull.Value, ParameterDirection.Output));

                ExecuteNonQuery(command);
                returnValue = command.Parameters["@username"].Value.ToString();
            }

            return returnValue;
        }

        public bool PromoNameExists(string promoName, long memberID)
        {
            string sql = "si2_PromoNameExists";
            bool returnValue = false;

            using (SqlCommand command = new SqlCommand(sql))
            {

                command.CommandType = CommandType.StoredProcedure;

                command.Parameters.Add(NewParameter("@promoname", 40, promoName));
                command.Parameters.Add(NewParameter("@memberID", memberID));
                command.Parameters.Add(NewParameter("@exists", SqlDbType.Bit, DBNull.Value, ParameterDirection.Output));

                ExecuteNonQuery(command);
                returnValue = GetBool(command.Parameters["@exists"]);
            }

            return returnValue;
        }

        public bool PromoNameExists(string promoName)
        {
            return PromoNameExists(promoName, -1);
        }


        public bool CreateRepPromoAndSite(int userId, string promoName, int reserved, string imageName, string mimeType, string description, string type, bool showEmail)
        {
            string sql = "si2_createRepPromoAndSite";
            bool returnValue = false;

            using (SqlCommand command = new SqlCommand(sql))
            {
                int showEmailNumeric = 0;

                if (showEmail)
                {
                    showEmailNumeric = 1;
                }

                command.CommandType = CommandType.StoredProcedure;

                command.Parameters.Add(NewParameter("@userId", userId));
                command.Parameters.Add(NewParameter("@promoname", 40, promoName));
                command.Parameters.Add(NewParameter("@reserved", reserved));
                command.Parameters.Add(NewParameter("@image", 40, imageName));
                command.Parameters.Add(NewParameter("@mimeType", 30, mimeType));
                command.Parameters.Add(NewParameter("@description", 4096, description));
                command.Parameters.Add(NewParameter("@type", 5, type));
                command.Parameters.Add(NewParameter("@showEmail", showEmail));
                command.Parameters.Add(NewParameter("@success", SqlDbType.Bit, DBNull.Value, ParameterDirection.Output));

                ExecuteNonQuery(command);
                returnValue = GetBool(command.Parameters["@success"]);
            }

            return returnValue;
        }

        #endregion Rep Site

        #region Get Profile Image
        public byte[] GetProfileImage(long userID)
        {
            string sql = "Si2_GetProfileImage";
            byte[] returnValue = null;

            using (SqlCommand command = new SqlCommand(sql))

            {
                command.CommandType = CommandType.StoredProcedure;

                command.Parameters.Add(NewParameter("@UserID", userID));

                returnValue = (byte[])ExcecuteScalar(command);
            }

            return returnValue;
        }
        #endregion
        #region Set Profile Image
        public void SetProfileImage(int UserID, string ImageName, byte[] Image)
        {
            string sql = "Si2_SetProfileImage";
            using (SqlCommand command = new SqlCommand(sql))
            {
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.Add(NewParameter("@UserID", UserID));
                command.Parameters.Add(NewParameter("@ProfileImageName", ImageName));
                //command.Parameters.Add(NewParameter("@ProfileImage", Image));   
                SqlParameter paramImageData = new SqlParameter()
                {
                    ParameterName = "@ProfileImage",
                    Value = Image
                };
                command.Parameters.Add(paramImageData);
                ExecuteNonQuery(command);
            }
        }

        #endregion
        #region Get Profile Image Name
        public string GetProfileImageName(long UserID)
        {
            DataSet data = new DataSet();
            string sql = "Si2_GetProfileImageName";
            using (SqlCommand command = new SqlCommand(sql))
            {
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.Add(NewParameter("@memberid", System.Data.SqlDbType.Int, UserID, System.Data.ParameterDirection.Input));
                data = Execute(command);
            }
            if (data.Tables.Count > 0 && data.Tables[0].Rows.Count > 0)
            {
                return (data.Tables[0].Rows[0]["ProfileImageName"].ToString());
            }
            else
            {
                return ("");
            }

        }
        #endregion
        #region Get MemberID
        public int GetMemberID(string referralCode)
        {
            int returnValue = 0;
            //  int userID=0;
            string sql = "Si2_GetMemberID";
            using (SqlCommand command = new SqlCommand(sql))
            {
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.Add(NewParameter("@RepID", 10, referralCode));

                try
                {
                    returnValue = (int)ExcecuteScalar(command);
                    //userID = Convert.ToInt32(returnValue);
                }
                catch (Exception exception)
                {
                    Utility.Trace.Write("GetMemberID", "!", exception);
                }
            }

            return returnValue;
        }
        #endregion
        #region Business Info
        public DataSet GetBusinessInfo(int userID)
        {
            string sql = "Si2_GetBusinessInfo";
            DataSet returnData = new DataSet();

            using (SqlCommand command = new SqlCommand(sql))
            {
                command.CommandType = CommandType.StoredProcedure;

                command.Parameters.Add(NewParameter("@memberid", userID));

                returnData = Execute(command);
            }

            return returnData;
        }

        public void SetBusinessInfo(int userID,
            string first_name,
            string last_name,
            string middle_name,
            string suffix,
            string business_type,
            string business_name,
            string type_other,
            string address,
            string city,
            string state,
            string zip,
            string phone,
            string display_name)
        {
            string sql = "Si2_SetBusinessInfo";
            using (SqlCommand command = new SqlCommand(sql))
            {
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.Add(NewParameter("@memberid", userID));
                command.Parameters.Add(NewParameter("@first_name", first_name));
                command.Parameters.Add(NewParameter("@last_name", last_name));
                command.Parameters.Add(NewParameter("@middle_name", middle_name));
                command.Parameters.Add(NewParameter("@suffix", suffix));
                command.Parameters.Add(NewParameter("@business_type", int.Parse(business_type)));
                command.Parameters.Add(NewParameter("@business_name", business_name));
                command.Parameters.Add(NewParameter("@type_other", type_other));
                //				command.Parameters.Add(NewParameter("@tin", tin));
                command.Parameters.Add(NewParameter("@address", address));
                command.Parameters.Add(NewParameter("@city", city));
                command.Parameters.Add(NewParameter("@state", state));
                command.Parameters.Add(NewParameter("@zip", zip));
                command.Parameters.Add(NewParameter("@phone", phone));
                command.Parameters.Add(NewParameter("@display_name", display_name));

                ExecuteNonQuery(command);

            }

        }

        #endregion Business Info

        #region ACH Info
        public DataSet GetACHInfo(long memberID)
        {
            string sql = "Si2_GetACHInfo";
            DataSet returnData = new DataSet();
            using (SqlCommand command = new SqlCommand(sql))
            {
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.Add(NewParameter("@memberid", memberID));
                returnData = Execute(command);
            }
            return returnData;
        }

        public void SetACHInfo(int memberID, string bankAccountNumber, string bankRoutingNumber, string bankAccountType, string bankName)
        {
            string sql = "Si2_SetRepAch";
            using (SqlCommand command = new SqlCommand(sql))
            {
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.Add(NewParameter("@memberid", memberID));
                command.Parameters.Add(NewParameter("@account_type", bankAccountType));
                command.Parameters.Add(NewParameter("@bank_name", bankName));
                command.Parameters.Add(NewParameter("@bank_routing_number", bankRoutingNumber));
                command.Parameters.Add(NewParameter("@bank_acct_number", bankAccountNumber));
                ExecuteNonQuery(command);
            }
        }

        public void SetTermsRecieved(int memberID)
        {
            string sql = "Si2_SetTermsRecieved";
            using (SqlCommand command = new SqlCommand(sql))
            {
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.Add(NewParameter("@memberid", memberID));

                ExecuteNonQuery(command);
            }
        }
        #endregion ACH Info

        #region Basic User Info
        public DataSet GetUserInfo(long memberID)
        {
            string sql = "Si2_GetUserInfo";
            DataSet returnData = new DataSet();
            using (SqlCommand command = new SqlCommand(sql))
            {
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.Add(NewParameter("@memberid", memberID));
                returnData = Execute(command);
            }
            return returnData;
        }

        public int GetMemberType(long memberID)
        {
            string sql = "Si2_GetMemberType";
            DataSet returnData = new DataSet();
            int memberType = 0;
            using (SqlCommand command = new SqlCommand(sql))
            {
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.Add(NewParameter("@memberid", memberID));
                returnData = Execute(command);
            }
            if (returnData.Tables[0].Rows.Count == 1)
            {
                memberType = int.Parse(returnData.Tables[0].Rows[0]["member_Type"].ToString());
            }
            return memberType;

        }
        #endregion

        #region Set Password
        public string GetPassword(int memberID)
        {
            string sql = "Si2_GetUserInfo";
            DataSet returnData = new DataSet();
            using (SqlCommand command = new SqlCommand(sql))
            {
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.Add(NewParameter("@memberid", memberID));
                returnData = Execute(command);
            }
            return (GetString(returnData.Tables[0].Rows[0], "password"));
        }
        #endregion

        #region PromoCode	

        public void SetPromoCode(int memberID, string promoCode, bool showEmail)
        {
            string sql = "Si2_SetRepPromoCode";
            using (SqlCommand command = new SqlCommand(sql))
            {
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.Add(NewParameter("@memberid", memberID));
                command.Parameters.Add(NewParameter("@promoname", promoCode));
                command.Parameters.Add(NewParameter("@showEmail", showEmail));
                ExecuteNonQuery(command);
            }
        }

        #endregion PromoCode

        #region reports
        public DataSet NewUsersToday()
        {
            string sql = "si2_newusersfordate";
            DataSet data = null;

            DateTime date = DateTime.Today;

            return NewUsersForDate(date);
        }


        public DataSet NewUsersForDate(DateTime date)
        {
            string sql = "si2_newusersfordate";
            DataSet data = null;

            using (SqlCommand command = new SqlCommand(sql))
            {
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.Add(NewParameter("@date", date));

                data = Execute(command);
            }

            return data;
        }

        public DataSet NewUsersForDate(DateTime start, DateTime end)
        {
            string sql = "si2_newusersfordaterange";
            DataSet data = null;

            using (SqlCommand command = new SqlCommand(sql))
            {
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.Add(NewParameter("@startdate", start));
                command.Parameters.Add(NewParameter("@enddate", end));

                data = Execute(command);
            }

            return data;
        }

        public DataSet GetRepsAndAgentsInDateRange(DateTime start, DateTime end)
        {
            string sql = "Si2_GetRepsAndAgentsInDateRange";
            DataSet data = null;

            using (SqlCommand command = new SqlCommand(sql))
            {
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.Add(NewParameter("@startdate", start));
                command.Parameters.Add(NewParameter("@enddate", end));

                data = Execute(command);
            }

            return data;
        }


        public DataTable GetGenealogyTree(int userID)
        {
            string sql = "Si2_Genealogy_GetTree";
            DataTable table = null;

            using (SqlCommand command = new SqlCommand(sql))
            {
                DataSet dataset;

                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.Add(NewParameter("@MemberID", userID));

                dataset = Execute(command);

                if (dataset != null && dataset.Tables.Count > 0)
                {
                    table = dataset.Tables[0];
                }

            }

            return table;
        }


        public DataSet GetGenealogyChildren(int userID)
        {
            string sql = "Si2_GetGenealogyChildren";
            DataSet returnData = new DataSet();

            using (SqlCommand command = new SqlCommand(sql))
            {
                command.CommandType = CommandType.StoredProcedure;

                command.Parameters.Add(NewParameter("@memberid", userID));

                returnData = Execute(command);
            }

            return returnData;
        }


        #endregion reports

        #region ClearCommerceLog
        public void AddClearCommerceLogEntry(string ID, int LineItem, string Name, double Amount, string TransactionStatus, string Type, string CardNum, string CardNumPrefix, string ProductName, int UserID)
        {
            string sql = "Si2_AddClearCommerceLogEntry";
            DataSet returnData = new DataSet();
            using (SqlCommand command = new SqlCommand(sql))
            {
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.Add(NewParameter("@ID", ID));
                command.Parameters.Add(NewParameter("@LineItem", LineItem));
                command.Parameters.Add(NewParameter("@Name", Name));
                command.Parameters.Add(NewParameter("@Amount", SqlDbType.Money, Amount, ParameterDirection.Input));
                command.Parameters.Add(NewParameter("@TransactionStatus", TransactionStatus));
                command.Parameters.Add(NewParameter("@Type", Type));
                command.Parameters.Add(NewParameter("@CardNum", CardNum));
                command.Parameters.Add(NewParameter("@CardNumPrefix", CardNumPrefix));
                command.Parameters.Add(NewParameter("@ProductName", ProductName));
                command.Parameters.Add(NewParameter("@UserID", UserID));
                ExecuteNonQuery(command);
            }

        }

        #endregion

        #region Store
        public void PurchaseAssistance(int userID, int Type, int Quantity)
        {
            string sql = "";
            if (Type == 1)
                sql = "Si2_AddMarketingAssistant";
            else
                sql = "Si2_AddGiftCertDater";

            string referralCode = GetReferralName(userID);

            for (int i = 1; i <= Quantity; i++)
            {
                using (SqlCommand command = new SqlCommand(sql))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    string daterUserName = Guid.NewGuid().ToString();
                    command.Parameters.Add(NewParameter("@memberid", userID));
                    command.Parameters.Add(NewParameter("@referralCode", referralCode));
                    command.Parameters.Add(NewParameter("@DaterUserName", daterUserName));
                    ExecuteNonQuery(command);
                }
            }
        }

        #endregion

        #region SiCalendar
        public DataTable GetEventData(DateTime startDate, DateTime endDate)
        {
            string cal_sql = "SELECT * From CAL_Calendar WHERE EventDate >= @stDate AND EventDate <= @enDate";
            SqlConnection cal_connection = new SqlConnection(_connectionString);
            cal_connection.Open();
            SqlCommand cal_command = new SqlCommand(cal_sql, cal_connection);
            cal_command.CommandType = CommandType.Text;
            cal_command.Parameters.Add(NewParameter("@stDate", startDate));
            cal_command.Parameters.Add(NewParameter("@enDate", endDate));
            DataSet ds = new DataSet();
            SqlDataAdapter da = new SqlDataAdapter(cal_command);
            da.Fill(ds);
            cal_connection.Close();

            return ds.Tables[0];
        }

        public DataSet GetListData(DateTime startDate, DateTime endDate)
        {
            string cal_sql = "SELECT EventId, EventTitle, EventDate, Location, City, State, ZipCode, ContactName From CAL_Calendar WHERE EventDate >= @stDate AND EventDate <= @enDate";
            SqlConnection cal_connection = new SqlConnection(_connectionString);
            cal_connection.Open();
            SqlCommand cal_command = new SqlCommand(cal_sql, cal_connection);
            cal_command.CommandType = CommandType.Text;
            cal_command.Parameters.Add(NewParameter("@stDate", startDate));
            cal_command.Parameters.Add(NewParameter("@enDate", endDate));
            DataSet ds = new DataSet();
            SqlDataAdapter da = new SqlDataAdapter(cal_command);
            da.Fill(ds);
            cal_connection.Close();

            return ds;
        }

        public DataSet GetEventDetails(int eventID)
        {
            string cal_sql = "SELECT * FROM CAL_Calendar WHERE EventId = @evntID";
            SqlConnection cal_connection = new SqlConnection(_connectionString);
            cal_connection.Open();
            SqlCommand cal_command = new SqlCommand(cal_sql, cal_connection);
            cal_command.CommandType = CommandType.Text;
            cal_command.Parameters.Add(NewParameter("@evntID", eventID));
            DataSet ds = new DataSet();
            SqlDataAdapter da = new SqlDataAdapter(cal_command);
            da.Fill(ds);
            cal_connection.Close();

            return ds;
        }

        public void addCalEvent(string evDate, string evDesc, string evType, string evImage, string evTitle, string fntColor, string evSite, string evAddr, string evCity, string evState, string evZip, string stePhone, string sTime, string eTime, string evtContact, string cPhone)
        {
            string cal_sql = "INSERT INTO CAL_Calendar(EventTitle, EventDescription, CategoryTitle, EventDate, CategoryImage, CategoryColor, Location, Street, City, State, ZipCode, SitePhone, StartTime, EndTime, ContactName, ContactPhone) VALUES(@eTitle, @eDesc, @catTitle, @eDate, @catImage, @catColor, @eLoc, @st, @eCity, @eState, @eZip, @sPhone, @stTime, @enTime, @cName, @ctPhone)";
            SqlConnection cal_connection = new SqlConnection(_connectionString);
            cal_connection.Open();
            SqlCommand cal_command = new SqlCommand(cal_sql, cal_connection);
            cal_command.CommandType = CommandType.Text;

            SqlParameter titleParameter = new SqlParameter("@eTitle", SqlDbType.VarChar);
            titleParameter.Value = System.Convert.ToString(evTitle);
            cal_command.Parameters.Add(titleParameter);

            SqlParameter descParameter = new SqlParameter("@eDesc", SqlDbType.Text);
            descParameter.Value = System.Convert.ToString(evDesc);
            cal_command.Parameters.Add(descParameter);

            SqlParameter catParameter = new SqlParameter("@catTitle", SqlDbType.Text);
            catParameter.Value = System.Convert.ToString(evType);
            cal_command.Parameters.Add(catParameter);

            SqlParameter dateParameter = new SqlParameter("@eDate", SqlDbType.DateTime);
            dateParameter.Value = System.Convert.ToDateTime(evDate);
            cal_command.Parameters.Add(dateParameter);

            SqlParameter imageParameter = new SqlParameter("@catImage", SqlDbType.VarChar);
            imageParameter.Value = System.Convert.ToString(evImage);
            cal_command.Parameters.Add(imageParameter);

            SqlParameter fcolorParameter = new SqlParameter("@catColor", SqlDbType.VarChar);
            fcolorParameter.Value = System.Convert.ToString(fntColor);
            cal_command.Parameters.Add(fcolorParameter);

            SqlParameter locParameter = new SqlParameter("@eLoc", SqlDbType.Text);
            locParameter.Value = System.Convert.ToString(evSite);
            cal_command.Parameters.Add(locParameter);

            SqlParameter addrParameter = new SqlParameter("@st", SqlDbType.VarChar);
            addrParameter.Value = System.Convert.ToString(evAddr);
            cal_command.Parameters.Add(addrParameter);

            SqlParameter cityParameter = new SqlParameter("@eCity", SqlDbType.VarChar);
            cityParameter.Value = System.Convert.ToString(evCity);
            cal_command.Parameters.Add(cityParameter);

            SqlParameter stateParameter = new SqlParameter("@eState", SqlDbType.VarChar);
            stateParameter.Value = System.Convert.ToString(evState);
            cal_command.Parameters.Add(stateParameter);

            SqlParameter zipParameter = new SqlParameter("@eZip", SqlDbType.VarChar);
            zipParameter.Value = System.Convert.ToString(evZip);
            cal_command.Parameters.Add(zipParameter);

            SqlParameter sphoneParameter = new SqlParameter("@sPhone", SqlDbType.VarChar);
            sphoneParameter.Value = System.Convert.ToString(stePhone);
            cal_command.Parameters.Add(sphoneParameter);

            SqlParameter stTimeParameter = new SqlParameter("@stTime", SqlDbType.VarChar);
            stTimeParameter.Value = System.Convert.ToString(sTime);
            cal_command.Parameters.Add(stTimeParameter);

            SqlParameter entimeParameter = new SqlParameter("@enTime", SqlDbType.VarChar);
            entimeParameter.Value = System.Convert.ToString(eTime);
            cal_command.Parameters.Add(entimeParameter);

            SqlParameter evtParameter = new SqlParameter("@cName", SqlDbType.VarChar);
            evtParameter.Value = System.Convert.ToString(evtContact);
            cal_command.Parameters.Add(evtParameter);

            SqlParameter ctphnParameter = new SqlParameter("@ctPhone", SqlDbType.VarChar);
            ctphnParameter.Value = System.Convert.ToString(cPhone);
            cal_command.Parameters.Add(ctphnParameter);
            cal_command.ExecuteNonQuery();
            cal_connection.Close();
        }

        public void removeCalEvent(int eventID)
        {
            string delevent_sql = "DELETE FROM CAL_Calendar WHERE EventId = @eID";
            SqlConnection cal_connection = new SqlConnection(_connectionString);
            cal_connection.Open();
            SqlCommand delevent_command = new SqlCommand(delevent_sql, cal_connection);
            delevent_command.CommandType = CommandType.Text;

            SqlParameter idParameter = new SqlParameter("@eID", SqlDbType.Int);
            idParameter.Value = eventID;
            delevent_command.Parameters.Add(idParameter);

            delevent_command.ExecuteNonQuery();
            cal_connection.Close();
        }

        #endregion

        #region ParentContactInfo

        public DataSet getParentContactInfo(string parentReferral)
        {
            DataSet data = new DataSet();
            string sql = "Si2_GetReferralContactInfo";
            using (SqlCommand command = new SqlCommand(sql))
            {
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.Add(NewParameter("@referralCode", System.Data.SqlDbType.Char, parentReferral, System.Data.ParameterDirection.Input));
                data = Execute(command);
            }
            return (data);
        }

        #endregion

        #region WholesaleMemberships

        public DataSet GetWholesaleMembershipList(long userID)
        {
            string sql = "Si2_GetWholesaleMembershipList";
            DataSet returnData = new DataSet();

            using (SqlCommand command = new SqlCommand(sql))
            {
                command.CommandType = CommandType.StoredProcedure;

                command.Parameters.Add(NewParameter("@memberid", userID));

                returnData = Execute(command);
            }

            return returnData;
        }

        public DataSet GetOpenWholesaleMemberships()
        {
            string sql = "Si2_GetOpenWholesaleMemberships";
            DataSet returnData = new DataSet();

            using (SqlCommand command = new SqlCommand(sql))
            {
                command.CommandType = CommandType.StoredProcedure;

                returnData = Execute(command);
            }

            return returnData;
        }
        public DataSet GetWholesaleMembership(long userID, string transactionID)
        {
            string sql = "Si2_GetWholesaleMembership";
            DataSet returnData = new DataSet();

            using (SqlCommand command = new SqlCommand(sql))
            {
                command.CommandType = CommandType.StoredProcedure;

                command.Parameters.Add(NewParameter("@memberid", userID));
                command.Parameters.Add(NewParameter("@transactionID", transactionID));

                returnData = Execute(command);
            }

            return returnData;
        }
        public void SetWholesaleMembership(string transactionID, long userID, DateTime PurchaseDate, string Status, string RedemptionInfo, string FirstName, string LastName, string EmailAddress, String StreetAddress, string City, string State, string Zip, string Country, DateTime GrantedDate, string FromName)
        {
            string sql = "Si2_SetWholesaleMembership";

            using (SqlCommand command = new SqlCommand(sql))
            {
                command.CommandType = CommandType.StoredProcedure;

                command.Parameters.Add(NewParameter("@memberid", userID));
                command.Parameters.Add(NewParameter("@transactionID", transactionID));
                command.Parameters.Add(NewParameter("@PurchaseDate", PurchaseDate));
                command.Parameters.Add(NewParameter("@Status", Status));
                command.Parameters.Add(NewParameter("@RedemptionInfo", RedemptionInfo));
                command.Parameters.Add(NewParameter("@FirstName", FirstName));
                command.Parameters.Add(NewParameter("@LastName", LastName));
                command.Parameters.Add(NewParameter("@EmailAddress", EmailAddress));
                command.Parameters.Add(NewParameter("@StreetAddress", StreetAddress));
                command.Parameters.Add(NewParameter("@City", City));
                command.Parameters.Add(NewParameter("@State", State));
                command.Parameters.Add(NewParameter("@Zip", Zip));
                command.Parameters.Add(NewParameter("@Country", Country));
                command.Parameters.Add(NewParameter("@GrantedDate", GrantedDate));
                command.Parameters.Add(NewParameter("@FromName", FromName));
                ExecuteNonQuery(command);
            }
        }

        public void SetWholesaleMembershipRedemptionInfo(string transactionID, string RedemptionInfo)
        {
            string sql = "Si2_SetWholesaleMembershipRedemptionInfo";

            using (SqlCommand command = new SqlCommand(sql))
            {
                command.CommandType = CommandType.StoredProcedure;

                command.Parameters.Add(NewParameter("@transactionID", transactionID));
                command.Parameters.Add(NewParameter("@RedemptionInfo", RedemptionInfo));

                ExecuteNonQuery(command);
            }
        }
        #endregion

        #region MarketingAssistance

        public int GetExpiredMACount()
        {
            DateTime beginDate = new DateTime(2005, 1, 1);
            DateTime endDate = DateTime.Today;
            endDate = endDate.Subtract(TimeSpan.FromDays(30));
            return GetOpenMACount(beginDate, endDate);
        }
        public int Get5DayExpiredMACount()
        {
            DateTime beginDate = DateTime.Today;
            beginDate = beginDate.Subtract(TimeSpan.FromDays(29));
            DateTime endDate = DateTime.Today;
            endDate = endDate.Subtract(TimeSpan.FromDays(25));
            return GetOpenMACount(beginDate, endDate);
        }
        public int Get10DayExpiredMACount()
        {
            DateTime beginDate = DateTime.Today;
            beginDate = beginDate.Subtract(TimeSpan.FromDays(29));
            DateTime endDate = DateTime.Today;
            endDate = endDate.Subtract(TimeSpan.FromDays(20));
            return GetOpenMACount(beginDate, endDate);
        }


        public int GetOpenMACount(DateTime beginDate, DateTime endDate)
        {
            DataSet data = new DataSet();
            string sql = "Si2_GetOpenMACount";
            using (SqlCommand command = new SqlCommand(sql))
            {
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.Add("@BeginDate", beginDate);
                command.Parameters.Add("@EndDate", endDate);
                data = Execute(command);
            }
            return DataBase.GetInt(data.Tables[0].Rows[0], "MACount");
        }

        public void AddPoolDater(int RXEMailBox, string parentReferralCode, string firstName, string lastName, int productID, string transactionID, double amount, bool active)
        {
            string sql = "Si2_CreatePoolDater";
            DataSet returnData = new DataSet();
            using (SqlCommand command = new SqlCommand(sql))
            {
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.Add(NewParameter("@RXEMailBox", RXEMailBox));
                command.Parameters.Add(NewParameter("@parentReferralCode", parentReferralCode));
                command.Parameters.Add(NewParameter("@firstName", firstName));
                command.Parameters.Add(NewParameter("@lastName", lastName));
                command.Parameters.Add(NewParameter("@productID", productID));
                command.Parameters.Add(NewParameter("@transactionID", transactionID));
                command.Parameters.Add(NewParameter("@amount", SqlDbType.Money, amount, ParameterDirection.Input));
                command.Parameters.Add(NewParameter("@active", SqlDbType.Int, active, ParameterDirection.Input));
                ExecuteNonQuery(command);
            }
        }

        public void AllocatePoolDaters()
        {
            DataSet unFullFilledMarketingAssistants = new DataSet();
            SqlCommand command = new SqlCommand("Si2_GetOpenMarketingAssistants");
            unFullFilledMarketingAssistants = Execute(command);
            foreach (DataRow row in unFullFilledMarketingAssistants.Tables[0].Rows)
            {
                int nextPoolDaterID = GetNextPoolDater();
                if (nextPoolDaterID != -1)
                    AssignPoolDater(nextPoolDaterID, GetInt(row, "MemberID"));
                else
                    return;
            }
        }
        private int GetNextPoolDater()
        {
            int retval = -1;
            string sql = "Si2_GetNextPoolDater";
            DataSet returnData = new DataSet();
            using (SqlCommand command = new SqlCommand(sql))
            {
                command.CommandType = CommandType.StoredProcedure;
                returnData = Execute(command);
            }
            if (returnData.Tables[0].Rows.Count > 0)
            {
                retval = GetInt(returnData.Tables[0].Rows[0], "ID");
            }
            return retval;
        }
        public void AssignPoolDater(int MailboxID, int MemberID)
        {
            string sql = "Si2_AssignPoolDater";
            DataSet returnData = new DataSet();
            using (SqlCommand command = new SqlCommand(sql))
            {
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.Add(NewParameter("@RXEMailBox", MailboxID));
                command.Parameters.Add(NewParameter("@MemberID", MemberID));
                ExecuteNonQuery(command);
            }
        }

        #endregion

        #region Store Email Purchase
        public bool PurchaseEmailAlreadyExists(string emailAddress)
        {
            string sql = "Si2_GetStoreEmailCount";

            SqlCommand command = new SqlCommand(sql);
            DataSet data = new DataSet();


            command.CommandType = CommandType.StoredProcedure;
            command.Parameters.Add(NewParameter("@emailAddress", 50, emailAddress));
            data = Execute(command);

            if ((int)data.Tables[0].Rows[0]["email_count"] > 0)
                return true;
            else
                return false;
        }

        public void PurchaseEmailAddress(int memberID, string emailAddress)
        {
            string sql = "Si2_SetStoreEmail";
            using (SqlCommand command = new SqlCommand(sql))
            {
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.Add(NewParameter("@memberid", memberID));
                command.Parameters.Add(NewParameter("@emailaddress", emailAddress));
                ExecuteNonQuery(command);
            }
        }

        public void AddEmailSubscriptionMonths(int UserID, string TransactionID, string Source, int months, string comments, string emailAddress)
        {
            string sql = "Si2_AddEmailMonthlyBillingSubscription";
            DateTime SubscriptionEnd = GetSubscriptionEnd(UserID);

            if (SubscriptionEnd == DateTime.MinValue || SubscriptionEnd < DateTime.Today)
                SubscriptionEnd = DateTime.Today;

            using (SqlCommand command = new SqlCommand(sql))
            {
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.Add(NewParameter("@memberid", UserID));
                command.Parameters.Add(NewParameter("@TransactionID", TransactionID));
                command.Parameters.Add(NewParameter("@Source", Source));
                command.Parameters.Add(NewParameter("@StartDate", SubscriptionEnd));
                command.Parameters.Add(NewParameter("@months", months));
                command.Parameters.Add(NewParameter("@Comments", comments));
                command.Parameters.Add(NewParameter("@emailAddress", emailAddress));
                ExecuteNonQuery(command);
            }

        }


        #endregion

        public DataSet GetTravelInvoiceDataForUser(long userid, DateTime startDate, DateTime endDate)
        {
            DataSet dataSet = null;
            using (SqlCommand command = new SqlCommand("RVLX_GetTravelSummaryReport"))
            {
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.Add(NewParameter("@userid", userid));
                command.Parameters.Add(NewParameter("@startDate", startDate));
                command.Parameters.Add(NewParameter("@endDate", endDate));

                dataSet = Execute(command);

            }

            return dataSet;
        }

        public SqlParameter GetNullParameterIfEmpty(string parameterName, int size, string value)
        {
            SqlParameter parameter = null;
            if (Utility.IsEmpty(value))
            {
                parameter = new SqlParameter(parameterName, DBNull.Value);
            }
            else
            {
                parameter = NewParameter(parameterName, size, value);
            }

            return parameter;

        }


        #region static retrieval methods

        public static Decimal GetDecimal(DataRowView row, string name)
        {
            Decimal returnValue = 0M;
            try
            {
                if (row[name] != DBNull.Value)
                {
                    returnValue = (Decimal)row[name];
                }
            }
            catch (Exception)
            {
                //noop
            }

            return returnValue;

        }

        public static long GetLong(DataRowView row, string name)
        {
            TraceWrite("getlong...", "format");

            long returnValue = 0;
            try
            {
                if (row[name] != DBNull.Value)
                {
                    returnValue = (long)row[name];
                }
            }
            catch (Exception exception)
            {
                Utility.Trace.Write("format", string.Format("{0} casting {1} to {2}", exception.Message, row[name].GetType().ToString(), "long"));

            }

            return returnValue;
        }



        public static long GetLong(DataRow row, string name)
        {
            TraceWrite("getlong...", "format");

            long returnValue = 0;
            try
            {
                if (row[name] != DBNull.Value)
                {
                    returnValue = (long)row[name];
                }
            }
            catch (Exception exception)
            {
                TraceWrite(exception.Message, "format");
            }

            return returnValue;
        }

        public static long GetLong(SqlParameter parameter)
        {
            TraceWrite("getlong...", "format");

            long returnValue = 0;
            try
            {
                if (parameter.Value != DBNull.Value)
                {
                    returnValue = (long)parameter.Value;
                }
            }
            catch (Exception exception)
            {
                Utility.Trace.Write("format", string.Format("{0} casting {1} to {2}", exception.Message, parameter.Value.GetType().ToString(), "long"));
            }

            return returnValue;
        }


        public static int GetInt(DataRow row, string name)
        {
            int returnValue = 0;
            try
            {
                if (row[name] != DBNull.Value)
                {
                    returnValue = (int)row[name];
                }
            }
            catch (Exception exception)
            {
                TraceWrite(string.Format("{0} parsing {1}: {2} :{3}", exception.Message, name, row[name].GetType().ToString(), row[name].ToString()), "GetInt");
            }

            return returnValue;
        }

        public static int GetInt(DataRowView row, string name)
        {
            int returnValue = 0;
            try
            {
                if (row[name] != DBNull.Value)
                {
                    returnValue = (int)row[name];
                }
            }
            catch (Exception exception)
            {
                TraceWrite(string.Format("{0} parsing {1}: {2} :{3}", exception.Message, name, row[name].GetType().ToString(), row[name].ToString()), "GetInt");

            }

            return returnValue;
        }

        public static int GetInt(SqlParameter parameter)
        {
            TraceWrite("getlong...", "format");

            int returnValue = 0;
            try
            {
                if (parameter.Value != DBNull.Value)
                {
                    returnValue = (int)parameter.Value;
                }
            }
            catch (Exception exception)
            {
                Utility.Trace.Write("format", string.Format("{0} casting {1} to {2}", exception.Message, parameter.Value.GetType().ToString(), "int"));
            }

            return returnValue;
        }


        public static bool GetBool(DataRow row, string name)
        {
            Utility.Trace.Write("GetBool", "------");

            bool returnValue = false;
            try
            {
                if (row[name] != DBNull.Value)
                {
                    Utility.Trace.Write("GetBool", string.Format("row[{0}] type: {1}", name, row[name].GetType().ToString()));
                    if (row[name].GetType() == typeof(short))
                    {
                        Utility.Trace.Write("GetBool", "Type matched typeof(short)");
                        returnValue = (short)row[name] > 0;
                    }
                    if (row[name].GetType() == typeof(int))
                    {
                        Utility.Trace.Write("GetBool", "Type matched typeof(int)");
                        returnValue = (int)row[name] > 0;
                    }
                    else if (row[name].GetType() == typeof(bool))
                    {
                        Utility.Trace.Write("GetBool", "Type matched typeof(bool)");
                        returnValue = (bool)row[name];
                    }
                }
            }
            catch (Exception exception)
            {
                Utility.Trace.Write("GetBool", string.Format("{0} parsing {1}: {2} :{3}", exception.Message, name, row[name].GetType().ToString(), row[name].ToString()));
            }

            Trace.Write("GetBool", string.Format("returning {0}", returnValue));
            return returnValue;
        }


        public static bool GetBool(SqlParameter parameter)
        {
            Utility.Trace.Write("GetBool(SqlParameter)", "----------");

            bool returnValue = false;
            try
            {
                if (parameter.Value != DBNull.Value)
                {
                    returnValue = (bool)parameter.Value;
                }
            }
            catch (Exception exception)
            {
                Utility.Trace.Write("GetBool", string.Format("{0} parsing {1}: {2}", exception.Message, parameter.Value.GetType().ToString()));
            }

            return returnValue;

        }


        public static bool GetBool(DataRowView row, string name)
        {
            Utility.Trace.Write("GetBool", "------");

            bool returnValue = false;
            try
            {
                if (row[name] != DBNull.Value)
                {
                    Utility.Trace.Write("GetBool", string.Format("row[{0}] type: {1}", name, row[name].GetType().ToString()));
                    if (row[name].GetType() == typeof(int))
                    {
                        Utility.Trace.Write("GetBool", "Type matched typeof(int)");
                        returnValue = (int)row[name] > 0;
                    }
                    else if (row[name].GetType() == typeof(bool))
                    {
                        Utility.Trace.Write("GetBool", "Type matched typeof(bool)");
                        returnValue = (bool)row[name];
                    }
                }
            }
            catch (Exception exception)
            {
                TraceWrite("GetBool", string.Format("{0} parsing {1}: {2} :{3}", exception.Message, name, row[name].GetType().ToString(), row[name].ToString()));
            }

            /*
			return returnValue;
			bool returnValue = false;
			try
			{
				if (row[name] != DBNull.Value)
				{
					returnValue = (bool)row[name];
				}
			}
			catch(Exception exception)
			{
				TraceWrite(string.Format("{0} parsing {1}: {2} :{3}", exception.Message, name, row[name].GetType().ToString(), row[name].ToString()), "GetInt");
			}
			*/
            return returnValue;
        }


        public static short GetShort(DataRowView row, string name)
        {
            short returnValue = 0;
            try
            {
                if (row[name] != DBNull.Value)
                {
                    returnValue = (short)row[name];
                }
            }
            catch (Exception exception)
            {
                TraceWrite(string.Format("{0} parsing {1}: {2} :{3}", exception.Message, name, row[name].GetType().ToString(), row[name].ToString()), "GetInt");
            }

            return returnValue;
        }

        public static short GetShort(DataRow row, string name)
        {
            short returnValue = 0;
            try
            {
                if (row[name] != DBNull.Value)
                {
                    returnValue = (short)row[name];
                }
            }
            catch (Exception exception)
            {
                TraceWrite(string.Format("{0} parsing {1}: {2} :{3}", exception.Message, name, row[name].GetType().ToString(), row[name].ToString()), "GetInt");
            }

            return returnValue;
        }


        public static Decimal GetDecimal(DataRow row, string name)
        {
            Decimal returnValue = 0M;
            try
            {
                if (row[name] != DBNull.Value)
                {
                    returnValue = (Decimal)row[name];
                }
            }
            catch (Exception)
            {
                //noop
            }

            return returnValue;

        }

        public static DateTime GetDateTime(DataRow row, string name)
        {
            DateTime returnValue = DateTime.MinValue;
            try
            {
                if (row[name] != DBNull.Value)
                {
                    returnValue = (DateTime)row[name];
                }
            }
            catch (Exception)
            {
                //noop
            }

            return returnValue;

        }


        public static string GetString(DataRow row, string name)
        {
            string returnValue = "";

            // TraceWrite(string.Format("name: {0}", name), "GetString");

            try
            {
                if (row[name] != DBNull.Value)
                {
                    returnValue = row[name].ToString();
                }
            }
            catch (Exception exception)
            {
                //TraceWrite(exception.Message, "GetString");
            }

            return returnValue;

        }

        public static string GetString(DataRowView row, string name)
        {
            string returnValue = "";

            // TraceWrite(string.Format("name: {0}", name), "GetString");

            try
            {
                if (row[name] != DBNull.Value)
                {
                    returnValue = row[name].ToString();
                }
            }
            catch (Exception exception)
            {
                //TraceWrite(exception.Message, "GetString");
            }

            return returnValue;

        }


        public static string FormatIntString(string value, string format)
        {
            string returnValue = "";
            try
            {
                int valueToFormat = int.Parse(value);
                returnValue = valueToFormat.ToString(format);
                TraceWrite(string.Format("formatted {0} with {1}. Returns {2}", valueToFormat, format, returnValue), "FormatIntString");
            }
            catch (FormatException fException)
            {
                TraceWrite(fException.Message, "FormatIntString");
            }
            catch (NullReferenceException nException)
            {
                TraceWrite(nException.Message, "FormatIntString");
            }

            return returnValue;
        }



        public static object GetNullIfEmpty(string value)
        {
            object returnValue;

            if (Utility.IsEmpty(value))
            {
                returnValue = DBNull.Value;
            }
            else
            {
                returnValue = value;
            }

            return returnValue;
        }

        public static string FormatLongString(string value, string format)
        {
            string returnValue = "";
            try
            {
                long valueToFormat = long.Parse(value);
                returnValue = valueToFormat.ToString(format);
                TraceWrite(string.Format("formatted {0} with {1}. Returns {2}", valueToFormat, format, returnValue), "FormatIntString");
            }
            catch (FormatException fException)
            {
                TraceWrite(fException.Message, "FormatIntString");
            }
            catch (NullReferenceException nException)
            {
                TraceWrite(nException.Message, "FormatIntString");
            }

            return returnValue;
        }

        private static void TraceWrite(string message, string category)
        {
            System.Web.HttpContext.Current.Trace.Write(category, message);
        }



        #endregion static retrieval methods

        #region static helper methods
        public static bool IsEmpty(DataTable table)
        {
            return (table == null || table.Rows.Count == 0);
        }

        public static bool IsEmpty(DataSet dataSet, int tableToCheck)
        {
            return (dataSet == null || dataSet.Tables.Count <= tableToCheck || IsEmpty(dataSet.Tables[tableToCheck]));
        }

        public static bool IsEmpty(DataSet dataSet, string tableName)
        {
            return (dataSet == null || IsEmpty(dataSet.Tables[tableName]));
        }

        public static bool IsEmpty(DataSet dataSet)
        {
            return IsEmpty(dataSet, 0);
        }
        #endregion static helper methods

        #region TRATest

        public int AddTRATest(int Memberid, Decimal TestScore)
        {
            string sql = "AddTRATest";
            int returnValue = 0;

            using (SqlCommand command = new SqlCommand(sql))
            {

                command.CommandType = CommandType.StoredProcedure;



                command.Parameters.Add(NewParameter("@MemberID", Memberid));
                command.Parameters.Add(NewParameter("@TestScore", SqlDbType.Decimal, TestScore, ParameterDirection.Input));
                command.Parameters.Add(NewParameter("@TestId", SqlDbType.Int, DBNull.Value, ParameterDirection.Output));


                ExecuteNonQuery(command);
                returnValue = GetInt(command.Parameters["@TestId"]);

                if (returnValue == 0)
                {
                    throw new Exception(string.Format("Invalid test id returned from {0}", sql));
                }
            }

            return returnValue;
        }

        public void AddTRATestAnswers(int Memberid, int TestID, int QuestionNumber, string TesterAnswer, string CorrectAnswer)
        {
            string sql = "AddTRATestAnswers";

            using (SqlCommand command = new SqlCommand(sql))
            {

                command.CommandType = CommandType.StoredProcedure;

                command.Parameters.Add(NewParameter("@MemberID", Memberid));
                command.Parameters.Add(NewParameter("@TestID", TestID));
                command.Parameters.Add(NewParameter("@QuestionNumber", QuestionNumber));
                command.Parameters.Add(NewParameter("@TesterAnswer", TesterAnswer));
                command.Parameters.Add(NewParameter("@CorrectAnswer", CorrectAnswer));

                ExecuteNonQuery(command);

            }


        }

        public DataSet GetMemberTestScore(long memberid)
        {
            string sql = "GetMemberTestScore";
            DataSet returnData = new DataSet();

            using (SqlCommand command = new SqlCommand(sql))
            {
                command.CommandType = CommandType.StoredProcedure;

                command.Parameters.Add(NewParameter("@memberid", memberid));

                returnData = Execute(command);
            }

            return returnData;
        }

        public decimal GetMemberTestScoreValue(long memberID)
        {
            DataSet ds = GetMemberTestScore(memberID);
            if (ds.Tables[0].Rows.Count == 0)
                return 0;
            else
                return (decimal)ds.Tables[0].Rows[0]["TestScore"];
        }

        #endregion TRATest

        #region Misc Functions
        public void WaitForQueueToProcess(long memberID)
        {
            string sql = "si2_IsQueueClearForUser";
            int queCount = 0;
            int tryCount = 0;

            do
            {
                using (SqlCommand command = new SqlCommand(sql))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.Add(NewParameter("@memberid", memberID));
                    ExecuteNonQuery(command);
                }
                tryCount++;
                if (tryCount > 5000)
                    return;

            }
            while (queCount > 0);
        }
        #endregion



        private void TraceCommand(SqlCommand command)
        {
            Utility.Trace.Write("TraceCommand", string.Format("Type:      {0}", command.CommandType));
            Utility.Trace.Write("TraceCommand", string.Format("Statement: {0}", command.CommandText));
            Utility.Trace.Write("TraceCommand", string.Format("Parameters:"));

            foreach (SqlParameter parameter in command.Parameters)
            {
                Utility.Trace.Write("TraceCommand", string.Format("     Name:  {0}  Value: {1}", parameter.ParameterName, parameter.Value.ToString()));
            }
        }

    }





}
