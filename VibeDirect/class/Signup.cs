using System;
using System.Web;
using System.Xml;
using System.Xml.Serialization;
using System.IO;
using System.Net;
using System.Text;

namespace VibeDirect
{
    public enum SignupPackage : int
    {
        UltimateMarketing = 0,
        MarketingAssistant = 1,
        IndependendRepresentative = 2
    }

    public enum PaymentType : int
    {
        Undefined = -1,
        CreditCard = 0,
        AchCheck = 1
    }

    public enum ResponseType : int
    {
        Approved = 0,
        Declined = 1,
        Error = 2,
        Fraud = 3
    }

    public class ResponseData
    {
        private ResponseType responseType = ResponseType.Error;
        private string message = "";
        private string transactionId = "";
        private string statusText = "";

        public ResponseData()
        {
        }


        public ResponseType Response
        {
            get
            {
                return responseType;
            }
            set
            {
                responseType = value;
            }
        }

        public string Message
        {
            get
            {
                return message;
            }
            set
            {
                message = value;
            }
        }

        public string TransactionId
        {
            get
            {
                return transactionId;
            }
            set
            {
                transactionId = value;
            }
        }

        public string StatusText
        {
            get
            {
                return statusText;
            }
            set
            {
                statusText = value;
            }
        }
    }

    public class ValidationResponse
    {
        private string message = "";
        private string transactionId = "";

        public ValidationResponse()
        {
        }


        public ResponseData GetResponse(XmlDocument responseDocument)
        {
            ResponseData responseData = new ResponseData();

            if (responseDocument != null)
            {
                Utility.Trace.Write("GetResponse", responseDocument.OuterXml);
                try
                {

                    XmlNode statusNode = responseDocument.SelectSingleNode("//TransactionStatus");
                    XmlNode idNode = responseDocument.SelectSingleNode("//TransactionId");
                    XmlNode messageNode = responseDocument.SelectSingleNode("//Message");

                    responseData.StatusText = GetNodeValue(statusNode);
                    responseData.TransactionId = GetNodeValue(idNode);
                    responseData.Message = GetNodeValue(messageNode);

                    if (Utility.IsEmpty(responseData.StatusText))
                    {
                        string errorMessage = "An error occurred reading the response document. No status code was returned.";

                        SetMessage(responseData, errorMessage);
                    }
                    else
                    {
                        switch (responseData.StatusText)
                        {
                            case "A":
                                {
                                    responseData.Response = ResponseType.Approved;
                                    break;
                                }
                            case "D":
                                {
                                    responseData.Response = ResponseType.Declined;
                                    break;
                                }
                            case "F":
                                {
                                    responseData.Response = ResponseType.Fraud;
                                    break;
                                }
                            case "E":
                                {
                                    responseData.Response = ResponseType.Error;
                                    break;
                                }
                            default:
                                {
                                    string errorMessage = "The response returned an unrecognized code.";
                                    responseData.Response = ResponseType.Error;

                                    SetMessage(responseData, errorMessage);
                                    break;
                                }

                        }
                    }
                }
                catch (Exception exception)
                {
                    string errorMessage = string.Format("While reading response document, encountered {0} -- {1}.", exception.GetType().ToString(), exception.Message);

                    responseData.Response = ResponseType.Error;
                    SetMessage(responseData, errorMessage);
                }
            }
            else
            {
                responseData.Response = ResponseType.Error;
                SetMessage(responseData, "An error occurred reading the response. The response was empty or was invalid XML");
            }

            return responseData;
        }

        public ResponseData ForceErrorResponse(string messageText)
        {
            ResponseData responseData = new ResponseData();

            Utility.Trace.Write("ValidationResponse.ForceErrorResponse", messageText);

            responseData.TransactionId = "";
            responseData.Message = messageText;
            responseData.Response = ResponseType.Error;

            return responseData;
        }
        public ResponseData FakeSuccessResponse()
        {
            ResponseData responseData = new ResponseData();
            responseData.TransactionId = "00000000000";
            responseData.StatusText = "0";
            responseData.Response = ResponseType.Approved;

            return responseData;
        }

        private void SetMessage(ResponseData responseData, string errorMessage)
        {
            if (Utility.IsEmpty(responseData.Message))
            {
                responseData.Message = errorMessage;
            }
            else
            {
                responseData.Message = string.Format("{0} The original response text was: {1}", errorMessage, message);
            }
        }

        private string GetNodeValue(XmlNode node)
        {
            string returnValue = "";

            if (node != null)
            {
                XmlNode child = node.ChildNodes[0];
                if (child != null && child.NodeType == XmlNodeType.Text)
                {
                    returnValue = child.Value;
                }
            }

            return returnValue;
        }

    }


    public interface IPaymentData
    {
        PaymentType TypeOfPayment
        {
            get;
        }

        Address BillingAddress
        {
            get;
            set;
        }

        string AccountHolderName
        {
            get;
            set;
        }


    }


    public interface IPayment
    {
        IPaymentData PaymentData
        {
            get;
        }

        ResponseData Charge(decimal amount);
    }


    public abstract class PaymentBase : IPayment
    {

        protected abstract XmlDocument BuildRequest(decimal amount);

        protected decimal AmountToCharge = 0M;
        protected IPaymentData paymentData;

        protected void CreateTextNode(XmlNode parent, string childName, string text)
        {
            XmlDocument owner = parent.OwnerDocument;
            XmlNode childNode = owner.CreateNode(XmlNodeType.Element, childName, "");
            XmlText textNode = owner.CreateTextNode(text);

            childNode.AppendChild(textNode);
            parent.AppendChild(childNode);
        }


        #region IPayment Members

        public IPaymentData PaymentData
        {
            get
            {
                return paymentData;
            }
        }

        public ResponseData Charge(decimal amount)
        {
            return Charge(amount, "Default");
        }

        public ResponseData Charge(decimal amount, string chargeType)
        {
            ResponseData responseData = null;

            if (System.Configuration.ConfigurationSettings.AppSettings["charge_validation_fake_success"] == "y")
            {
                responseData = new ValidationResponse().FakeSuccessResponse();
            }
            else
            {

                ValidationResponse response = null;
                //  XmlDocument requestDocument = BuildRequest(amount);
                CreditCard cc = new CreditCard();
                //XmlDocument requestDocument = cc.BuildPaymentRequest(amount);
                // ServicePointManager.SecurityProtocol = SecurityProtocolType.Ssl3;
                string requestDocument = cc.BuildPaymentRequest(amount);

                if (requestDocument != null)
                {
                    string ResponseData = Utility.postXMLData(Utility.GetConfigSetting("charge_validation_url"), requestDocument);
                    //var request = (HttpWebRequest)WebRequest.Create("https://apitest.authorize.net/xml/v1/request.api");
                    //var data = Encoding.ASCII.GetBytes(requestDocument);

                    //request.Method = "POST";
                    //request.ContentType = "application/xml";
                    //request.ContentLength = data.Length;
                    //try
                    //{ 

                    //using (var stream = request.GetRequestStream())
                    //{
                    //    stream.Write(data, 0, data.Length);
                    //}
                    //var responses = (HttpWebResponse)request.GetResponse();
                    //var responseString = new StreamReader(responses.GetResponseStream()).ReadToEnd();

                    //var responseStringXML = responseString;
                    //}
                    //catch(Exception ex)
                    //{
                    //    throw ex.InnerException;
                    //}
                    //FiveAndDimeSoftware.WebUtilities.HttpXmlPostSession postSession = new FiveAndDimeSoftware.WebUtilities.HttpXmlPostSession(90000);


                    //CreditCardData card = (CreditCardData)PaymentData;
                    //Uri uri;

                    ////					if (card.CardType == "Visa" || card.CardType == "MasterCard")
                    ////						uri = new Uri(Utility.GetConfigSetting("VisaMC_charge_validation_url"));
                    ////					else
                    //uri = new Uri(Utility.GetConfigSetting("charge_validation_url"));

                    //if (chargeType == "Store")
                    //    uri = new Uri(Utility.GetConfigSetting("store_charge_validation_url"));

                    //XmlDocument responseDocument = postSession.PostXml(requestDocument, uri);

                    //response = new ValidationResponse();
                    //responseData = response.GetResponse(responseDocument);

                    //Commenting old code and adding new code for authorize.net

                }
                else
                {
                    response = new ValidationResponse();
                    responseData = response.ForceErrorResponse("The request document was null");
                }
            }

            return responseData;
        }
        #endregion


    }


    public class Address
    {
        private string streetAddress;
        private string streetAddressLineTwo;
        private string streetAddressLineThree;
        private string city;
        private string state;
        private string zip;

        public Address()
        {
        }


        public string StreetAddress
        {
            get
            {
                return streetAddress;
            }
            set
            {
                streetAddress = value;
            }
        }

        public string StreetAddressLineTwo
        {
            get
            {
                return streetAddressLineTwo;
            }
            set
            {
                streetAddressLineTwo = value;
            }
        }
        public string StreetAddressLineThree
        {
            get
            {
                return streetAddressLineThree;
            }
            set
            {
                streetAddressLineThree = value;
            }
        }

        public string City
        {
            get
            {
                return city;
            }
            set
            {
                city = value;
            }
        }
        public string State
        {
            get
            {
                return state;
            }
            set
            {
                state = value;
            }
        }
        public string Zip
        {
            get
            {
                return zip;
            }
            set
            {
                zip = value;
            }
        }


        public Address Clone()
        {
            Address address = new Address();

            address.StreetAddress = this.StreetAddress;
            address.StreetAddressLineTwo = this.StreetAddressLineTwo;
            address.streetAddressLineThree = this.streetAddressLineThree;
            address.City = this.City;
            address.State = this.State;
            address.Zip = this.Zip;

            return address;
        }


    }


    public class CreditCardData : IPaymentData
    {
        private string cardType;
        private string cardNumber;
        private DateTime expirationDate;
        private string accountHolderName;
        private string cvv2;
        private Address billingAddress;
        private string phone;
        public string SessionID = "";
        public string bankName = "";
        public string bankAccountType = "";
        public string bankRoutingNumber = "";
        public string bankAccountNumber = "";
        public string bankCheckNumber = "";

        public CreditCardData()
        {
            billingAddress = new Address();
        }

        public string Phone
        {
            get
            {
                return phone;
            }
            set
            {
                phone = value;
            }
        }


        public string CardType
        {
            get
            {
                return cardType;
            }
            set
            {
                cardType = value;
            }
        }

        public string CardNumber
        {
            get
            {
                return cardNumber;
            }
            set
            {
                cardNumber = value;
            }
        }

        public DateTime ExpirationDate
        {
            get
            {
                return expirationDate;
            }
            set
            {
                expirationDate = value;
            }
        }

        public string Cvv2
        {
            get
            {
                return cvv2;
            }
            set
            {
                cvv2 = value;
            }
        }


        #region IPaymentData Members
        public Si2.Soulmatebiz.PaymentType TypeOfPayment
        {
            get
            {
                return PaymentType.CreditCard;
            }
        }
        public string AccountHolderName
        {
            get
            {
                return accountHolderName;
            }
            set
            {
                accountHolderName = value;
            }
        }

        public Address BillingAddress
        {
            get
            {
                return billingAddress;
            }
            set
            {
                billingAddress = value;
            }
        }

        #endregion
    }


    public class CreditCardCommerce : PaymentBase //changed class CreditCard to CreditCardCommerce to avoid conflict with authorize.net
    {
        public CreditCardCommerce(CreditCardData card)
        {
            paymentData = card;
        }

        protected override XmlDocument BuildRequest(decimal amount)
        {
            CreditCardData card = null;
            XmlDocument request = null;

            try
            {
                card = (CreditCardData)PaymentData;
            }
            catch (Exception exception)
            {
                card = null;
            }

            if (card != null)
            {
                string cardType = "";
                switch (card.CardType)
                {
                    case "Visa":
                    case "VI":
                        cardType = "VI";
                        break;
                    case "MasterCard":
                    case "MC":
                        cardType = "MC";
                        break;
                    case "Discover":
                    case "DI":
                        cardType = "DI";
                        break;
                    case "AMEX":
                        cardType = "AM";
                        break;
                    case "Check":
                        cardType = "CK";
                        break;
                }
                request = new XmlDocument();
                XmlDeclaration declaration = request.CreateXmlDeclaration("1.0", "utf-8", null);

                string rootNodeName = "ChargeRequest";

                if (cardType == "CK")
                    rootNodeName = "CheckRequest";

                XmlNode chargeRequestNode = request.CreateNode(XmlNodeType.Element, rootNodeName, "");

                request.AppendChild(declaration);
                request.AppendChild(chargeRequestNode);

                CreateTextNode(chargeRequestNode, "Name", card.AccountHolderName);
                CreateTextNode(chargeRequestNode, "SessionID", card.SessionID);

                if (cardType != "CK")
                {
                    CreateTextNode(chargeRequestNode, "Number", card.CardNumber);
                    CreateTextNode(chargeRequestNode, "CardType", cardType);
                    CreateTextNode(chargeRequestNode, "Expires", card.ExpirationDate.ToString("MM/yy"));
                    CreateTextNode(chargeRequestNode, "Cvv2", card.Cvv2);
                }
                else
                {
                    CreateTextNode(chargeRequestNode, "AccountNumber", card.bankAccountNumber);
                    CreateTextNode(chargeRequestNode, "BankName", card.bankName);
                    CreateTextNode(chargeRequestNode, "RoutingNumber", card.bankRoutingNumber);
                    CreateTextNode(chargeRequestNode, "CheckNumber", card.bankCheckNumber);
                    CreateTextNode(chargeRequestNode, "AccountType", card.bankAccountType);
                }

                CreateTextNode(chargeRequestNode, "Amount", (amount * 100).ToString("##########"));
                CreateTextNode(chargeRequestNode, "Phone", card.Phone);
                CreateTextNode(chargeRequestNode, "Street1", card.BillingAddress.StreetAddress);
                CreateTextNode(chargeRequestNode, "Street2", card.BillingAddress.StreetAddressLineTwo);
                CreateTextNode(chargeRequestNode, "Street3", "");
                CreateTextNode(chargeRequestNode, "City", card.BillingAddress.City);
                CreateTextNode(chargeRequestNode, "State", card.BillingAddress.State);
                CreateTextNode(chargeRequestNode, "Zip", card.BillingAddress.Zip);

                Utility.Trace.Write("BuildRequest", request.OuterXml);
            }

            return request;
        }


    }


    public class AchCheckData : IPaymentData
    {
        private string bankName;
        private int accountType;
        private string routingNumber;
        private string routingNumberConfirmation;
        private string accountNumber;
        private string accountNumberConfirmation;
        private string accountHolderName;
        private string checkType;
        private string phoneNumber;
        private Address billingAddress;
        private string checkNumber;

        public AchCheckData()
        {
            billingAddress = new Address();
        }


        public string BankName
        {
            get
            {
                return bankName;
            }
            set
            {
                bankName = value;
            }
        }
        public int AccountType
        {
            get
            {
                return accountType;
            }
            set
            {
                accountType = value;
            }
        }

        public string RoutingNumber
        {
            get
            {
                return routingNumber;
            }
            set
            {
                routingNumber = value;
            }
        }

        public string RoutingNumberConfirmation
        {
            get
            {
                return routingNumberConfirmation;
            }
            set
            {
                routingNumberConfirmation = value;
            }
        }

        public string AccountNumber
        {
            get
            {
                return accountNumber;
            }
            set
            {
                accountNumber = value;
            }
        }
        public string AccountNumberConfirmation
        {
            get
            {
                return accountNumberConfirmation;
            }
            set
            {
                accountNumberConfirmation = value;
            }
        }

        public string CheckType
        {
            get
            {
                return checkType;
            }
            set
            {
                checkType = value;
            }
        }

        public string PhoneNumber
        {
            get
            {
                return phoneNumber;
            }
            set
            {
                phoneNumber = value;
            }
        }
        public string CheckNumber
        {
            get
            {
                return checkNumber;
            }
            set
            {
                checkNumber = value;
            }
        }


        #region IPaymentData Members
        public Si2.Soulmatebiz.PaymentType TypeOfPayment
        {
            get
            {
                return PaymentType.AchCheck;
            }
        }
        public string AccountHolderName
        {
            get
            {
                return accountHolderName;
            }
            set
            {
                accountHolderName = value;
            }
        }

        public Address BillingAddress
        {
            get
            {
                return billingAddress;
            }
            set
            {
                billingAddress = value;
            }
        }

        #endregion

    }


    public class AchCheck : PaymentBase
    {
        public AchCheck(AchCheckData check)
        {
            paymentData = check;
        }


        protected override XmlDocument BuildRequest(decimal amount)
        {

            XmlDocument request = null;
            AchCheckData check = null;

            try
            {
                check = (AchCheckData)paymentData;
            }
            catch (Exception exception)
            {
                check = null;
            }

            if (check != null)
            {

                request = new XmlDocument();
                XmlDeclaration declaration = request.CreateXmlDeclaration("1.0", "utf-8", null);
                XmlNode chargeRequestNode = request.CreateNode(XmlNodeType.Element, "CheckRequest", "");

                request.AppendChild(chargeRequestNode);

                CreateTextNode(chargeRequestNode, "Name", check.AccountHolderName);
                CreateTextNode(chargeRequestNode, "AccountNumber", check.AccountNumber);
                CreateTextNode(chargeRequestNode, "RoutingNumber", check.RoutingNumber);
                CreateTextNode(chargeRequestNode, "CheckNumber", check.CheckNumber);
                CreateTextNode(chargeRequestNode, "AccountType", check.AccountType.ToString());
                CreateTextNode(chargeRequestNode, "CheckType", "1");
                CreateTextNode(chargeRequestNode, "Amount", (amount * 100).ToString("##########"));
                CreateTextNode(chargeRequestNode, "Phone", check.PhoneNumber);
                CreateTextNode(chargeRequestNode, "Street1", check.BillingAddress.StreetAddress);
                CreateTextNode(chargeRequestNode, "Street2", check.BillingAddress.StreetAddressLineTwo);
                CreateTextNode(chargeRequestNode, "Street3", check.BillingAddress.StreetAddressLineThree);
                CreateTextNode(chargeRequestNode, "City", check.BillingAddress.City);
                CreateTextNode(chargeRequestNode, "State", check.BillingAddress.State);
                CreateTextNode(chargeRequestNode, "Zip", check.BillingAddress.Zip);

                Utility.Trace.Write("BuildRequest", request.OuterXml);
            }

            return request;
        }
    }


    public class PackageData
    {
        private bool ultimatePackage;
        private bool marketingPackage;
        private bool independentPackage;

        public PackageData()
        {
        }

        public bool UltimatePackage
        {
            get
            {
                return ultimatePackage;
            }
            set
            {
                ultimatePackage = value;
            }
        }

        public bool MarketingPackage
        {
            get
            {
                return marketingPackage;
            }
            set
            {
                marketingPackage = value;
            }
        }

        public bool IndependentPackage
        {
            get
            {
                return independentPackage;
            }
            set
            {
                independentPackage = value;
            }
        }


        public decimal Amount
        {
            get
            {
                decimal amount = 0M;
                decimal ultimateCost = 485M;
                decimal marketingCost = 14.95M;
                decimal independentCost = 50M;

                if (UltimatePackage)
                {
                    amount += ultimateCost;
                }

                if (MarketingPackage)
                {
                    amount += marketingCost;
                }

                if (IndependentPackage)
                {
                    amount += independentCost;
                }

                return amount;

            }

        }


    }


    public class SignupData
    {
        private string parentReferralCode;
        private string referralCode;
        private string firstName;
        private string middleName;
        private string lastName;
        private string suffix;
        private Address address;
        private short businessType = short.MinValue;
        private string businessTypeOther;
        private string businessName;
        private string displayName;
        private string ssn;
        private string ein;
        private string phoneNumber;
        private string username;
        private string password;
        private string emailAddress;
        private string emailAddressConfirm;
        private int userId = int.MinValue;
        private bool checkedW9;
        private bool checkedAgreement;
        private bool checkedPledge;

        private byte[] _ProfileImage;
        private string _ProfileImageName;
        private PackageData packageData;
        public bool TravelCert;
        public bool MonthlyAgentCert;
        public bool MonthlyAccounting;
        public bool AnnualAgentCert;
        public bool AnnualAccouning;
        public bool IndependentRep;
        public bool FreeIndependentRep;

        public bool UltAgent;
        public bool MonthlyUltAgentFee;
        public bool AnnualUltAgentFee;
        public bool Agent;
        public bool MonthlyAgentFee;

        public bool CruiseCert;
        public string CertFirstName = "";
        public string CertMI = "";
        public string CertLastName = "";
        public string CertEmailAddress = "";
        public string CertShipFullName = "";
        public string CertShipStreet = "";
        public string CertShipCity = "";
        public string CertShipState = "";
        public string CertShipZip = "";



        public decimal Amount;

        /*		
                private bool ultimatePackage;
                private bool marketingPackage;
                private bool independentPackage;
        */
        private IPaymentData paymentData;
        private ResponseData validationResponseData;

        public SignupData()
        {
            address = new Address();
            packageData = new PackageData();
        }


        public string ParentReferallCode
        {
            get
            {
                return parentReferralCode;
            }
            set
            {
                parentReferralCode = value;
            }
        }
        public string ReferralCode
        {
            get
            {
                return referralCode;
            }
            set
            {
                referralCode = value;
            }
        }

        public string FirstName
        {
            get
            {
                return firstName;
            }
            set
            {
                firstName = value;
            }
        }
        public string MiddleName
        {
            get
            {
                return middleName;
            }
            set
            {
                middleName = value;
            }
        }

        public string LastName
        {
            get
            {
                return lastName;
            }
            set
            {
                lastName = value;
            }
        }
        public string Suffix
        {
            get
            {
                return suffix;
            }
            set
            {
                suffix = value;
            }
        }

        public Address Address
        {
            get
            {
                return address;
            }
            set
            {
                address = value;
            }
        }
        public short BusinessType
        {
            get
            {
                return businessType;
            }
            set
            {
                businessType = value;
            }
        }

        public string BusinessTypeOther
        {
            get
            {
                return businessTypeOther;
            }
            set
            {
                businessTypeOther = value;
            }
        }
        public string BusinessName
        {
            get
            {
                return businessName;
            }
            set
            {
                businessName = value;
            }
        }

        public string DisplayName
        {
            get
            {
                return displayName;
            }
            set
            {
                displayName = value;
            }
        }

        public string SSN
        {
            get
            {
                return ssn;
            }
            set
            {
                ssn = value;
            }
        }
        public string EIN
        {
            get
            {
                return ein;
            }
            set
            {
                ein = value;
            }
        }
        public string PhoneNumber
        {
            get
            {
                return phoneNumber;
            }
            set
            {
                phoneNumber = value;
            }
        }

        public string Username
        {
            get
            {
                return username;
            }
            set
            {
                username = value;
            }
        }

        public string Password
        {
            get
            {
                return password;
            }
            set
            {
                password = value;
            }
        }

        public string EmailAddress
        {
            get
            {
                return emailAddress;
            }
            set
            {
                emailAddress = value;
            }
        }

        public string EmailAddressConfirm
        {
            get
            {
                return emailAddressConfirm;
            }
            set
            {
                emailAddressConfirm = value;
            }
        }

        public byte[] ProfileImage
        {
            get
            {
                return _ProfileImage;
            }
            set
            {
                _ProfileImage = value;
            }
        }
        public string ProfileImageName
        {
            get
            {
                return _ProfileImageName;
            }
            set
            {
                _ProfileImageName = value;
            }
        }
        public int UserID
        {
            get
            {
                return userId;
            }
            set
            {
                userId = value;
            }
        }
        public bool CheckedW9
        {
            get
            {
                return checkedW9;
            }
            set
            {
                checkedW9 = value;
            }
        }

        public bool CheckedAgreement
        {
            get
            {
                return checkedAgreement;
            }
            set
            {
                checkedAgreement = value;
            }
        }

        public bool CheckedPledge
        {
            get
            {
                return checkedPledge;
            }
            set
            {
                checkedPledge = value;
            }
        }
        ////Added for profile Image
        //public string ProfileImage
        //{
        //    get
        //    {
        //        return ProfileImage;
        //    }
        //    set
        //    {
        //        ProfileImage = value;
        //    }
        //}
        //End of code added for profile Image

        public bool UltimatePackage
        {
            get
            {
                return packageData.UltimatePackage;
            }
            set
            {
                packageData.UltimatePackage = value;
            }
        }

        public bool MarketingPackage
        {
            get
            {
                return packageData.MarketingPackage;
            }
            set
            {
                packageData.MarketingPackage = value;
            }
        }

        public bool IndependentPackage
        {
            get
            {
                return packageData.IndependentPackage;
            }
            set
            {
                packageData.IndependentPackage = value;
            }
        }

        public IPaymentData PaymentData
        {
            get
            {
                return paymentData;
            }
            set
            {
                paymentData = value;
            }
        }
        public PaymentType TypeOfPayment
        {
            get
            {
                PaymentType returnValue = PaymentType.Undefined;

                if (paymentData != null)
                {
                    returnValue = paymentData.TypeOfPayment;
                }

                return returnValue;
            }
            set
            {
                if (paymentData == null || paymentData.TypeOfPayment != value)
                {

                    switch (value)
                    {
                        case PaymentType.AchCheck:
                            {
                                paymentData = new AchCheckData();
                                break;
                            }
                        case PaymentType.CreditCard:
                            {
                                paymentData = new CreditCardData();
                                break;
                            }
                        default:
                            {
                                paymentData = null;
                                break;
                            }
                    }
                }
                paymentData.AccountHolderName = string.Format("{0} {1}", this.FirstName, this.LastName);
                paymentData.BillingAddress = this.Address.Clone();
            }
        }

        public ResponseData ValidationResponseData
        {
            get
            {
                return validationResponseData;
            }
            set
            {
                validationResponseData = value;
            }
        }

        /*
		public decimal Amount
		{
			get
			{
				//return packageData.Amount;
			}
		}
*/

        public override string ToString()
        {
            string format = "PRC: {0},FN: {1}, LN: {2}";
            return string.Format(format, ParentReferallCode, FirstName, LastName);
        }
    }


    /// <summary>
    /// Summary description for Signup.
    /// </summary>
    public class Signup
    {
        private SignupData data = null;
        private bool loaded = false;

        public Signup()
        {
            data = new SignupData();

        }


        public SignupData Data
        {
            get
            {
                return data;
            }
        }

        public bool IsLoaded
        {
            get
            {
                return loaded;
            }
        }


        /*
				public IPaymentData PaymentData
				{
					get
					{
						return paymentData;
					}
					set
					{
						paymentData = value;
					}
				}
		*/


        public decimal Amount
        {
            get
            {
                return data.Amount;
            }

        }


        public bool PersistInformation()
        {
            Utility.Trace.Write("Signup.PersistInformation", data.ToString());
            Utility.Session["SignupData"] = data;
            return true;
        }

        public ResponseData ValidatePaymentType()
        {
            ResponseData responseData = null;
            IPayment payment;
            switch (data.TypeOfPayment)
            {
                case PaymentType.AchCheck:
                    {
                        payment = new AchCheck((AchCheckData)data.PaymentData);
                        break;
                    }
                case PaymentType.CreditCard:
                    {
                        // payment = new CreditCard((CreditCardData)data.PaymentData);
                        payment = new CreditCardCommerce((CreditCardData)data.PaymentData);
                        break;
                    }
                default:
                    {
                        payment = null;
                        break;
                    }
            }

            if (payment != null)
            {   // KenV no charges yet...
                responseData = payment.Charge(Amount);
            }
            else
            {
                ValidationResponse response = new ValidationResponse();
                responseData = response.ForceErrorResponse("Unknown or un-initialized payment type.");
            }

            //fake an approval...
            //responseData = new ResponseData();
            //responseData.Response = ResponseType.Approved;
            //responseData.TransactionId = System.Guid.NewGuid().ToString();
            //responseData.Message = "Delayed Billing..";


            data.ValidationResponseData = responseData;

            if (responseData.Response == ResponseType.Approved)
            {
                DataBase db = new DataBase();
                ReferalCode rc = new ReferalCode();

                data.ReferralCode = rc.GenReferalCode();

                data.UserID = db.RegisterUser(data);

                if (data.UserID < 1)
                {
                    throw new Exception("invalid user id");
                }

                db.AuthenticateUser(data.Username, data.Password);

                Utility.Trace.Write("ValidatePayemntType", string.Format("user id: {0}", data.UserID));
            }

            return responseData;
        }

        /*
		public ResponseData RegisterUser()
		{
			return true;
		}
		*/

        public bool Open()
        {
            try
            {
                data = (SignupData)Utility.GetSessionValue("SignupData");
            }
            catch (Exception)
            {
                // noop. Stays null
            }

            if (data == null)
            {
                data = new SignupData();
                loaded = false;
            }
            else
            {
                loaded = true;
                Utility.Trace.Write("Signup.Open", data.ToString());
            }

            Utility.Trace.Write("Signup.Open()", string.Format("member id: {0}", data.UserID));

            return loaded;
        }

    }

    //Code added for Authorize.net
    [XmlRoot(ElementName = "merchantAuthentication")]
    public class MerchantAuthentication
    {
        [XmlElement(ElementName = "name")]
        public string Name { get; set; }
        [XmlElement(ElementName = "transactionKey")]
        public string TransactionKey { get; set; }

        public MerchantAuthentication()
        {
            this.Name = "6h6tRrF7pqL";
            this.TransactionKey = "58fEhjK339nH2XKk";
        }
    }

    [XmlRoot(ElementName = "creditCard")]
    public class CreditCard
    {
        [XmlElement(ElementName = "cardNumber")]
        public string CardNumber { get; set; }
        [XmlElement(ElementName = "expirationDate")]
        public string ExpirationDate { get; set; }
        [XmlElement(ElementName = "cardCode")]
        public string CardCode { get; set; }

        public CreditCard()
        {
            this.CardNumber = "4111111111111111";
            this.ExpirationDate = "0725";
            this.CardCode = "123";
        }

        public string BuildPaymentRequest(decimal amount)
        {
            //XmlDocument request = null;
            MerchantAuthentication merchantAuthentication = new MerchantAuthentication();
            TransactionRequest transactionRequest = new TransactionRequest();
            Payment payment = new Payment();
            CreditCard creditCard = new CreditCard();
            payment.CreditCard = creditCard;
            Customer customer = new Customer();
            BillTo billTo = new BillTo();
            transactionRequest.BillTo = billTo;
            transactionRequest.Customer = customer;
            transactionRequest.Payment = payment;

            CreateTransactionRequest obj = new CreateTransactionRequest();
            obj.MerchantAuthentication = merchantAuthentication;
            obj.TransactionRequest = transactionRequest;

            XmlSerializer xsSubmit = new XmlSerializer(typeof(CreateTransactionRequest));
            //var subReq = new CreateTransactionRequest();
            var xml = "";

            using (var sww = new StringWriter())
            {
                using (XmlWriter writer = XmlWriter.Create(sww))
                {
                    xsSubmit.Serialize(writer, obj);
                    xml = sww.ToString(); // Your XML
                }
            }

            //XmlDocument xmlDoc = new XmlDocument();
            //xmlDoc.LoadXml(xml);
            // request = xmlDoc;
            return xml;
        }
    }

    [XmlRoot(ElementName = "payment")]
    public class Payment
    {
        [XmlElement(ElementName = "creditCard")]
        public CreditCard CreditCard { get; set; }
    }

    [XmlRoot(ElementName = "customer")]
    public class Customer
    {
        [XmlElement(ElementName = "id")]
        public string Id { get; set; }

        public Customer()
        {
            this.Id = "456";
        }
    }

    [XmlRoot(ElementName = "billTo")]
    public class BillTo
    {
        [XmlElement(ElementName = "firstName")]
        public string FirstName { get; set; }
        [XmlElement(ElementName = "lastName")]
        public string LastName { get; set; }
        [XmlElement(ElementName = "company")]
        public string Company { get; set; }
        [XmlElement(ElementName = "address")]
        public string Address { get; set; }
        [XmlElement(ElementName = "city")]
        public string City { get; set; }
        [XmlElement(ElementName = "state")]
        public string State { get; set; }
        [XmlElement(ElementName = "zip")]
        public string Zip { get; set; }
        [XmlElement(ElementName = "country")]
        public string Country { get; set; }

        public BillTo()
        {
            this.FirstName = "Ellen";
            this.LastName = "abc";
            this.Company = "Souveniropolis";
            this.Address = "pune";
            this.City = "Pecan Spring";
            this.State = "MAH";
            this.Zip = "454545";
            this.Country = "INdian";
        }
    }

    [XmlRoot(ElementName = "transactionRequest")]
    public class TransactionRequest
    {
        [XmlElement(ElementName = "transactionType")]
        public string TransactionType { get; set; }
        [XmlElement(ElementName = "amount")]
        public double Amount { get; set; }
        [XmlElement(ElementName = "payment")]
        public Payment Payment { get; set; }
        [XmlElement(ElementName = "customer")]
        public Customer Customer { get; set; }
        [XmlElement(ElementName = "billTo")]
        public BillTo BillTo { get; set; }

        public TransactionRequest()
        {
            this.TransactionType = "authCaptureTransaction";
            this.Amount = 5;
        }
    }

    [XmlRoot(ElementName = "createTransactionRequest", Namespace = "AnetApi/xml/v1/schema/AnetApiSchema.xsd")]
    public class CreateTransactionRequest
    {
        [XmlElement(ElementName = "merchantAuthentication")]
        public MerchantAuthentication MerchantAuthentication { get; set; }
        [XmlElement(ElementName = "refId")]
        public string RefId { get; set; }
        [XmlElement(ElementName = "transactionRequest")]
        public TransactionRequest TransactionRequest { get; set; }


        public CreateTransactionRequest()
        {
            this.RefId = "123456";

        }

    }
    //End of code added for Authorize.net
}
