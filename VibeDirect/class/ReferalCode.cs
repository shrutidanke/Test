using System;
using Si2.Cryptography;


namespace VibeDirect
{
    /// <summary>
    /// Summary description for ReferalCode.
    /// </summary>
    public class ReferalCode
    {
        private Random randObj;
        private DataBase db;


        public ReferalCode()
        {
            randObj = new Random((int)DateTime.Now.Ticks);
        }
        public string GenReferalCode()
        {
            string referalCode = "";

            for (int tryCount = 0; tryCount < 20 && referalCode.Length == 0; tryCount++)
            {
                //Generate Referal code in the form of AAAA####
                String[] letters = new String[] { "A", "B", "C", "D", "E", "F", "G", "H", "J", "K", "M", "N", "P", "Q", "R", "S", "T", "U", "V", "W", "X", "Y" };
                String[] digits = new String[] { "3", "4", "5", "6", "7", "8", "9" };
                for (int i = 0; i < 4; ++i)
                {
                    referalCode += letters[randObj.Next(letters.Length)];
                }
                for (int i = 0; i < 4; ++i)
                {
                    referalCode += digits[randObj.Next(digits.Length)];
                }
                if (IsReferalCodeInValid(referalCode)) referalCode = "";
            }
            return referalCode;
        }

        private bool IsReferalCodeInValid(string referalCode)
        {
            bool returnValue = false;
            bool dbValue;
            bool badWord;

            if (db == null)
            {
                db = new DataBase();
            }

            if (db.ReferralCodeExists(referalCode) || ContainsBadWords(referalCode))
            {
                returnValue = true;
            }

            return returnValue;
        }

        private bool ContainsBadWords(string referalCode)
        {
            Crypt crypt = new Crypt("e307af79-d2fa-46c7-be34-c6ade24671ed");
            string rawString = System.Configuration.ConfigurationSettings.AppSettings["bw"];
            string[] badWords = rawString.Split(',');

            for (int i = 0; i < badWords.Length; i++)
            {
                string dirtyWord = crypt.DecryptString(badWords[i]);
                if (referalCode.IndexOf(dirtyWord) > 0)
                {
                    Utility.Trace.Write(string.Format("OOPS! {0} >> {1}", referalCode, dirtyWord));

                    return true;
                }
            }
            return false;
        }
    }
}
