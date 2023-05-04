using LIS.MWI.DTO;
using Microsoft.Practices.EnterpriseLibrary.Data;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.IO;
using System.Text;
using System.Timers;
using System.Web;
using System.Web.Services;
using System.Web.UI.HtmlControls;
using System.Web.UI.WebControls;
using System.Xml.Linq;

[assembly: log4net.Config.XmlConfigurator(Watch = true)]
namespace LIS.MWI
{

    public partial class SampleContext : System.Web.UI.Page
    {
        dynamic arrayJsonData;
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        protected void Page_Load(object sender, EventArgs e)
        {
            Response.Cache.SetCacheability(HttpCacheability.NoCache);
            if (!IsPostBack)
            {
                GetDbSessionData();
                string strLoginName = Convert.ToString(Session["LoginID"]);
                if (string.IsNullOrEmpty(strLoginName))
                {
                    Response.Redirect("Login");
                    return;
                }
                hdnVersion.Value = System.Configuration.ConfigurationManager.AppSettings["Version"].ToString();
                int ServiceRefreshInterval = Convert.ToInt32(System.Configuration.ConfigurationManager.AppSettings["ServiceRefreshInterval"].ToString());
                ServiceRefreshInterval = (ServiceRefreshInterval * 60000);
                hdnServiceRefreshInterval.Value = Convert.ToString(ServiceRefreshInterval);
                LoadControls();
                //LoadConfigData();
                hdnLoggedInUser.Value = strLoginName;
                lblUserNameWebView.Text = hdnLoggedInUser.Value;
                lblUserNameMobileView.Text = hdnLoggedInUser.Value;
            }
        }
        private void GetDbSessionData()
        {
            string paramValue = "SessionTimeout,SessionAlertTimeWindow,ShowDiscardReasonPopup";
            int sessionTimeOut = 0;
            int sessionAlertWindow = 0;
            bool ShowDiscardReasonPopup = false;
            try
            {

                DatabaseProviderFactory factory = new DatabaseProviderFactory();
                Database db = factory.Create("ApplicationDB");
                using (DbCommand _dbCommand = db.GetStoredProcCommand("PGETMULTIAPPCONFIG"))
                {
                    _dbCommand.Parameters.Add(new SqlParameter("@CONFIGKEY", paramValue));
                    using (IDataReader dr = db.ExecuteReader(_dbCommand))
                    {
                        if (dr.Read())
                        {
                            do
                            {

                                string configKey = dr["ConfigKey"].ToString();
                                int configValue = dr.GetOrdinal("ConfigValue");
                                if (configKey == "SessionTimeout")
                                {
                                    sessionTimeOut = Convert.ToInt32(dr.GetString(configValue));
                                }
                                else if (configKey == "SessionAlertTimeWindow")
                                {
                                    sessionAlertWindow = Convert.ToInt32(dr.GetString(configValue));
                                }
                                else if (configKey == "ShowDiscardReasonPopup")
                                {
                                    ShowDiscardReasonPopup = Convert.ToBoolean(dr.GetString(configValue));
                                }
                            }
                            while (dr.Read());
                            hdnSessionTimeout.Value = Convert.ToString(sessionTimeOut);
                            hdnSessionAlertWindow.Value = Convert.ToString(sessionAlertWindow);
                            hdnShowDiscardReasonPopup.Value = Convert.ToString(ShowDiscardReasonPopup);
                        }
                        else
                        {
                            //If no recordset returned 
                            StringBuilder _strErrorMsg = new StringBuilder();
                            _strErrorMsg.Append("Error:  No Recordset returned for Parameter : " + paramValue.ToString() + "\n");
                            _strErrorMsg.Append("strLoggedInUser : " + hdnLoggedInUser.Value + "\n");
                            log.Error(_strErrorMsg);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                StringBuilder _strErrorMsg = new StringBuilder();
                _strErrorMsg.Append("strLoggedInUser : " + hdnLoggedInUser.Value + "\n");
                _strErrorMsg.Append("DB Exception: " + ex + "\n");
                log.Error(_strErrorMsg);
            }
        }



        public void LoadConfigData()
        {
            string strResult = string.Empty;
            try
            {
                DatabaseProviderFactory factory = new DatabaseProviderFactory();
                Database db = factory.Create("ApplicationDB");
                string _jsonData = string.Empty;

                using (DbCommand _dbCommand = db.GetStoredProcCommand("PGETMULTIAPPCONFIG"))
                {
                    _dbCommand.Parameters.Add(new SqlParameter("@CONFIGKEY", "ControlsConfig"));
                    using (IDataReader dr = db.ExecuteReader(_dbCommand))
                    {
                        if (dr.Read())
                        {
                            do
                            {
                                strResult = dr["CONFIGVALUE"].ToString();
                                try
                                {
                                    arrayJsonData = JsonConvert.DeserializeObject(strResult);
                                    var _webControlsArray = arrayJsonData[0].Config.Control;

                                    for (int i = 0; i < _webControlsArray.Count; i++)
                                    {
                                        bool isMandatory = true;
                                        if (_webControlsArray[i].Mandatory != null && _webControlsArray[i].Mandatory.Value.ToLower() == "no")
                                            isMandatory = false;

                                        bool isAdditionalInfo = false;
                                        if (_webControlsArray[i].AdditionalInfo != null && _webControlsArray[i].AdditionalInfo.Value.ToLower() == "yes")
                                            isAdditionalInfo = true;

                                        Label _labelElement = CreateLabel(_webControlsArray[i].DisplayLabelValue.Value, isMandatory);
                                        System.Web.UI.HtmlControls.HtmlGenericControl _divChildLabel = CreateDiv(_webControlsArray[i].ControlID.ToString());
                                        _divChildLabel.Controls.Add(_labelElement);

                                        string _sElementType = _webControlsArray[i].ElementType;
                                        if (_sElementType == "DropDownList")
                                        {
                                            DropDownList _dropDownList = new DropDownList();
                                            _dropDownList.ID = _webControlsArray[i].ControlName;
                                            _dropDownList.Width = Unit.Percentage((int)_webControlsArray[i].Width);
                                            _dropDownList.Attributes.Add("onChange", "DropDownOnChangeEvent(this);");
                                            ListItem lst = new ListItem();
                                            lst.Text = _webControlsArray[i].DefaultValue;
                                            lst.Value = _webControlsArray[i].DefaultValue;
                                            _dropDownList.Items.Add(lst);
                                            _divChildLabel.Controls.Add(_dropDownList);
                                            if (isAdditionalInfo)
                                            {
                                                string _controlName = _webControlsArray[i].ControlName;
                                                Label _labelAdditionalInfo = CreateAdditionalInfoLabel(_controlName);
                                                _divChildLabel.Controls.Add(_labelAdditionalInfo);
                                            }
                                        }
                                        else if (_sElementType == "TextBox")
                                        {
                                            TextBox _textBox = new TextBox();
                                            _textBox.ID = _webControlsArray[i].ControlName;
                                            _textBox.Width = Unit.Percentage((int)_webControlsArray[i].Width);
                                            _textBox.Text = string.Empty;
                                            if (!string.IsNullOrEmpty(_webControlsArray[i].Maxlength))
                                                _textBox.MaxLength = (int)_webControlsArray[i].Maxlength;
                                            else
                                                _textBox.MaxLength = 10;
                                            _divChildLabel.Controls.Add(_textBox);
                                        }
                                        divDynamicControl.Controls.Add(_divChildLabel);
                                    }
                                }
                                catch (Exception ex)
                                {
                                    log.Error("LoadControls: ", ex);
                                }
                            }
                            while (dr.Read());
                        }
                        else
                        {
                            //If no recordset returned 
                            strResult = "Cannot create file, Sample context not found";
                        }
                    }
                }

            }
            catch (Exception ex)
            {

                strResult = "Failed to fetch JSON file. Please try again later.";
                log.Error(strResult);
            }
        }




        [WebMethod]
        public static string GetJsonConfigGData()
        {
            string strResult = string.Empty;
            List<FileMetaDataDTO> lstFileMetaDataDTO = new List<FileMetaDataDTO>();
            try
            {
                DatabaseProviderFactory factory = new DatabaseProviderFactory();
                Database db = factory.Create("ApplicationDB");
                using (DbCommand _dbCommand = db.GetStoredProcCommand("PGETMULTIAPPCONFIG"))
                {
                    _dbCommand.Parameters.Add(new SqlParameter("@CONFIGKEY", "ControlsConfig"));
                    using (IDataReader dr = db.ExecuteReader(_dbCommand))
                    {
                        if (dr.Read())
                        {

                            do
                            {
                                try
                                {

                                    //arrayJsonData = JsonConvert.DeserializeObject(strResult);


                                    //var _webControlsArray = arrayJsonData1.Config.Control;

                                    //for (int i = 0; i < _webControlsArray.Count; i++)
                                    //{
                                    //    bool isMandatory = true;
                                    //    if (_webControlsArray[i].Mandatory != null && _webControlsArray[i].Mandatory.Value.ToLower() == "no")
                                    //        isMandatory = false;

                                    //    bool isAdditionalInfo = false;
                                    //    if (_webControlsArray[i].AdditionalInfo != null && _webControlsArray[i].AdditionalInfo.Value.ToLower() == "yes")
                                    //        isAdditionalInfo = true;

                                    //    Label _labelElement = CreateLabel(_webControlsArray[i].DisplayLabelValue.Value, isMandatory);
                                    //    System.Web.UI.HtmlControls.HtmlGenericControl _divChildLabel = CreateDiv(_webControlsArray[i].ControlID.ToString());
                                    //    _divChildLabel.Controls.Add(_labelElement);

                                    //    string _sElementType = _webControlsArray[i].ElementType;
                                    //    if (_sElementType == "DropDownList")
                                    //    {
                                    //        DropDownList _dropDownList = new DropDownList();
                                    //        _dropDownList.ID = _webControlsArray[i].ControlName;
                                    //        _dropDownList.Width = Unit.Percentage((int)_webControlsArray[i].Width);
                                    //        _dropDownList.Attributes.Add("onChange", "DropDownOnChangeEvent(this);");
                                    //        ListItem lst = new ListItem();
                                    //        lst.Text = _webControlsArray[i].DefaultValue;
                                    //        lst.Value = _webControlsArray[i].DefaultValue;
                                    //        _dropDownList.Items.Add(lst);
                                    //        _divChildLabel.Controls.Add(_dropDownList);
                                    //        if (isAdditionalInfo)
                                    //        {
                                    //            string _controlName = _webControlsArray[i].ControlName;
                                    //            Label _labelAdditionalInfo = CreateAdditionalInfoLabel(_controlName);
                                    //            _divChildLabel.Controls.Add(_labelAdditionalInfo);
                                    //        }
                                    //    }
                                    //    else if (_sElementType == "TextBox")
                                    //    {
                                    //        TextBox _textBox = new TextBox();
                                    //        _textBox.ID = _webControlsArray[i].ControlName;
                                    //        _textBox.Width = Unit.Percentage((int)_webControlsArray[i].Width);
                                    //        _textBox.Text = string.Empty;
                                    //        if (!string.IsNullOrEmpty(_webControlsArray[i].Maxlength))
                                    //            _textBox.MaxLength = (int)_webControlsArray[i].Maxlength;
                                    //        else
                                    //            _textBox.MaxLength = 10;
                                    //        _divChildLabel.Controls.Add(_textBox);
                                    //    }
                                    //    divDynamicControl.Controls.Add(_divChildLabel);
                                    //}


                                    strResult = dr["CONFIGVALUE"].ToString();

                                }
                                catch (Exception ex)
                                {
                                    log.Error("LoadControls: ", ex);
                                }
                            }
                            while (dr.Read());
                        }
                        else
                        {
                            //If no recordset returned 
                            strResult = "Cannot create file, Sample context not found";
                        }
                    }
                }
                return strResult;

            }
            catch (Exception ex)
            {

                strResult = "Failed to fetch JSON file. Please try again later.";
                log.Error(strResult);
            }
            return strResult;
        }

        private void LoadControls()
        {
            using (StreamReader readJson = new StreamReader(Server.MapPath("Json/ControlsConfig.json")))
            {
                try
                {
                    string jsonData = readJson.ReadToEnd();
                    arrayJsonData = JsonConvert.DeserializeObject(jsonData);
                    var _webControlsArray = arrayJsonData.Config.Control;
                    for (int i = 0; i < _webControlsArray.Count; i++)
                    {
                        bool isMandatory = true;
                        if (_webControlsArray[i].Mandatory != null && _webControlsArray[i].Mandatory.Value.ToLower() == "no")
                            isMandatory = false;

                        bool isAdditionalInfo = false;
                        if (_webControlsArray[i].AdditionalInfo != null && _webControlsArray[i].AdditionalInfo.Value.ToLower() == "yes")
                            isAdditionalInfo = true;

                        Label _labelElement = CreateLabel(_webControlsArray[i].DisplayLabelValue.Value, isMandatory);
                        System.Web.UI.HtmlControls.HtmlGenericControl _divChildLabel = CreateDiv(_webControlsArray[i].ControlID.ToString());
                        _divChildLabel.Controls.Add(_labelElement);

                        string _sElementType = _webControlsArray[i].ElementType;
                        if (_sElementType == "DropDownList")
                        {
                            DropDownList _dropDownList = new DropDownList();
                            _dropDownList.ID = _webControlsArray[i].ControlName;
                            _dropDownList.Width = Unit.Percentage((int)_webControlsArray[i].Width);
                            _dropDownList.Attributes.Add("onChange", "DropDownOnChangeEvent(this);");
                            ListItem lst = new ListItem();
                            lst.Text = _webControlsArray[i].DefaultValue;
                            lst.Value = _webControlsArray[i].DefaultValue;
                            _dropDownList.Items.Add(lst);
                            _divChildLabel.Controls.Add(_dropDownList);
                            if (isAdditionalInfo)
                            {
                                string _controlName = _webControlsArray[i].ControlName;
                                Label _labelAdditionalInfo = CreateAdditionalInfoLabel(_controlName);
                                _divChildLabel.Controls.Add(_labelAdditionalInfo);
                            }
                        }
                        else if (_sElementType == "TextBox")
                        {
                            TextBox _textBox = new TextBox();
                            _textBox.ID = _webControlsArray[i].ControlName;
                            _textBox.Width = Unit.Percentage((int)_webControlsArray[i].Width);
                            _textBox.Text = string.Empty;
                            if (!string.IsNullOrEmpty(_webControlsArray[i].Maxlength))
                                _textBox.MaxLength = (int)_webControlsArray[i].Maxlength;
                            else
                                _textBox.MaxLength = 10;
                            _divChildLabel.Controls.Add(_textBox);
                        }
                        divDynamicControl.Controls.Add(_divChildLabel);
                    }
                }
                catch (Exception ex)
                {
                    StringBuilder _strErrorMsg = new StringBuilder();
                    _strErrorMsg.Append("strLoggedInUser : " + hdnLoggedInUser.Value + "\n");
                    _strErrorMsg.Append("LoadControls: " + ex + "\n");
                }
            }
        }
        private HtmlGenericControl CreateDiv(string elementDisplayName)
        {
            System.Web.UI.HtmlControls.HtmlGenericControl childElementLabel =
             new System.Web.UI.HtmlControls.HtmlGenericControl("DIV");
            childElementLabel.ID = "Controldiv" + elementDisplayName;
            childElementLabel.Attributes["class"] = "row";
            return childElementLabel;
        }
        private Label CreateLabel(string elementDisplayValue, bool isMandatory)
        {
            Label labelElement = new Label();
            string _elementDisplayValue = elementDisplayValue;
            if (isMandatory)
                _elementDisplayValue = "<span class='cssMandatory'>* </span>" + elementDisplayValue;
            labelElement.Text = _elementDisplayValue;
            return labelElement;
        }

        private Label CreateAdditionalInfoLabel(string ControlName)
        {
            Label labelElement = new Label();
            labelElement.ID = "lblAI_" + ControlName;
            labelElement.CssClass = "csslblAdditionalInfo";
            return labelElement;
        }
        [WebMethod]
        public static string GetServiceStatus(string _equipmentID, string _strLoggedInUser)
        {
            string strResult = "OFFLINE";
            DateTime _dtNextHeartBeart = DateTime.UtcNow;
            try
            {
                CheckSessionAlive(_strLoggedInUser);
                DatabaseProviderFactory factory = new DatabaseProviderFactory();
                Database db = factory.Create("ApplicationDB");
                using (DbCommand _dbCommand = db.GetStoredProcCommand("pgetservicestatus"))
                {
                    _dbCommand.Parameters.Add(new SqlParameter("@SERVERDATETIMEUTC", _dtNextHeartBeart));
                    _dbCommand.Parameters.Add(new SqlParameter("@EQUIPMENTID", SqlDbType.NVarChar, 50));
                    _dbCommand.Parameters["@EQUIPMENTID"].Value = string.IsNullOrEmpty(_equipmentID) ? (object)DBNull.Value : _equipmentID;

                    SqlParameter _sqlParam = new SqlParameter();
                    _sqlParam.ParameterName = "@OVERALLSTATUS";
                    _sqlParam.DbType = DbType.String;
                    _sqlParam.Direction = ParameterDirection.Output;
                    _sqlParam.Size = 100;
                    _dbCommand.Parameters.Add(_sqlParam);
                    db.ExecuteNonQuery(_dbCommand);
                    strResult = _dbCommand.Parameters["@OVERALLSTATUS"].Value.ToString();
                }
            }
            catch (Exception ex)
            {
                StringBuilder _strErrorMsg = new StringBuilder();
                _strErrorMsg.Append("strLoggedInUser : " + _strLoggedInUser + "\n");
                _strErrorMsg.Append("GetServiceStatus : " + ex + "\n");
                log.Error(_strErrorMsg);
            }
            return strResult;
        }

        [WebMethod]
        public static List<SampleContextDTO> GetSampleContextDetail(string _strLoggedInUser, string _strEquipmentID, string _strSampleContextJSON)
        {
            List<SampleContextDTO> lstSampleContextDTO = new List<SampleContextDTO>();
            try
            {
                CheckSessionAlive(_strLoggedInUser);
                DatabaseProviderFactory factory = new DatabaseProviderFactory();
                Database db = factory.Create("ApplicationDB");
                using (DbCommand _dbCommand = db.GetStoredProcCommand("PGETSAMPLECONTEXTDETAILS"))
                {
                    _dbCommand.Parameters.Add(new SqlParameter("@LOGGEDINUSER", _strLoggedInUser));
                    _dbCommand.Parameters.Add(new SqlParameter("@EQUIPMENTID", _strEquipmentID));
                    _dbCommand.Parameters.Add(new SqlParameter("@SAMPLECONTEXTJSON", _strSampleContextJSON));
                    using (IDataReader dr = db.ExecuteReader(_dbCommand))
                    {
                        if (dr.Read())
                        {
                            int iDeviceRawDataOID = dr.GetOrdinal(name: "DEVICERAWDATAOID");
                            int iSampleStatus = dr.GetOrdinal("SAMPLESTATUS");
                            int iDeviceStatus = dr.GetOrdinal("DEVICESTATUS");
                            int iRawDataPacket = dr.GetOrdinal("RAWDATAPACKET");
                            int iDeviceNextTimeout = dr.GetOrdinal("DEVICENEXTTIMEOUT");
                            int iFileName = dr.GetOrdinal("FILENAME");
                            int iServerID = dr.GetOrdinal("SERVERID");

                            long OID = 0;
                            do
                            {
                                SampleContextDTO oSampleContextDTO = new SampleContextDTO();
                                long.TryParse(dr[iDeviceRawDataOID] + "", out OID);
                                oSampleContextDTO.DEVICERAWDATAOID = OID;
                                oSampleContextDTO.SAMPLESTATUS = !dr.IsDBNull(iSampleStatus) ? dr.GetString(iSampleStatus) : null;
                                oSampleContextDTO.DEVICESTATUS = !dr.IsDBNull(iDeviceStatus) ? dr.GetString(iDeviceStatus) : null;
                                oSampleContextDTO.RAWDATAPACKET = !dr.IsDBNull(iRawDataPacket) ? dr.GetString(iRawDataPacket) : null;
                                oSampleContextDTO.DEVICENEXTTIMEOUT = !dr.IsDBNull(iDeviceNextTimeout) ? dr.GetDateTime(iDeviceNextTimeout) : (DateTime?)null;
                                oSampleContextDTO.FILENAME = !dr.IsDBNull(iFileName) ? dr.GetString(iFileName) : null;
                                oSampleContextDTO.SERVERID = !dr.IsDBNull(iServerID) ? dr.GetString(iServerID) : null;
                                lstSampleContextDTO.Add(oSampleContextDTO);
                            }
                            while (dr.Read());
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                StringBuilder _strErrorMsg = new StringBuilder();
                _strErrorMsg.Append("\nError Message : " + ex.Message.ToString() + "\n");
                _strErrorMsg.Append("Parameters: \n");
                _strErrorMsg.Append("strLoggedInUser : " + _strLoggedInUser + "\n");
                _strErrorMsg.Append("strEquipmentID : " + _strEquipmentID + "\n");
                _strErrorMsg.Append("strSampleContextJSON : " + _strSampleContextJSON + "\n");
                log.Error(_strErrorMsg);
            }
            return lstSampleContextDTO;
        }

        [WebMethod]
        public static List<SampleContextDTO> RefreshSampleContextDetails(string _strLoggedInUser, long _oid)
        {
            List<SampleContextDTO> lstSampleContextDTO = new List<SampleContextDTO>();
            try
            {
                CheckSessionAlive(_strLoggedInUser);

                DatabaseProviderFactory factory = new DatabaseProviderFactory();
                Database db = factory.Create("ApplicationDB");
                using (DbCommand _dbCommand = db.GetStoredProcCommand("PREFRESHSAMPLECONTEXTDETAILS"))
                {
                    _dbCommand.Parameters.Add(new SqlParameter("@DEVICERAWDATAOID", _oid));
                    using (IDataReader dr = db.ExecuteReader(_dbCommand))
                    {
                        if (dr.Read())
                        {
                            int iSampleStatus = dr.GetOrdinal("SAMPLESTATUS");
                            int iDeviceStatus = dr.GetOrdinal("DEVICESTATUS");
                            int iRawDataPacket = dr.GetOrdinal("RAWDATAPACKET");
                            int iDeviceNextTimeout = dr.GetOrdinal("DEVICENEXTTIMEOUT");
                            int iFileName = dr.GetOrdinal("FILENAME");

                            do
                            {
                                SampleContextDTO oSampleContextDTO = new SampleContextDTO();
                                oSampleContextDTO.SAMPLESTATUS = !dr.IsDBNull(iSampleStatus) ? dr.GetString(iSampleStatus) : null;
                                oSampleContextDTO.DEVICESTATUS = !dr.IsDBNull(iDeviceStatus) ? dr.GetString(iDeviceStatus) : null;
                                oSampleContextDTO.RAWDATAPACKET = !dr.IsDBNull(iRawDataPacket) ? dr.GetString(iRawDataPacket) : null;
                                oSampleContextDTO.DEVICENEXTTIMEOUT = !dr.IsDBNull(iDeviceNextTimeout) ? dr.GetDateTime(iDeviceNextTimeout) : (DateTime?)null;
                                oSampleContextDTO.FILENAME = !dr.IsDBNull(iFileName) ? dr.GetString(iFileName) : null;

                                lstSampleContextDTO.Add(oSampleContextDTO);
                            }
                            while (dr.Read());
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                StringBuilder _strErrorMsg = new StringBuilder();
                _strErrorMsg.Append("\nError Message : " + ex.Message.ToString() + "\n");
                _strErrorMsg.Append("Parameters: \n");
                _strErrorMsg.Append("strLoggedInUser : " + _strLoggedInUser + "\n");
                _strErrorMsg.Append("OID : " + _oid.ToString() + "\n");
                log.Error(_strErrorMsg);
            }
            return lstSampleContextDTO;
        }

        [WebMethod]
        public static string SetSampleContextDetail(string _strLoggedInUser, string _strEquipmentID, string _strSampleContextJSON)
        {
            string strResult = string.Empty;
            int iResultCode = 0;
            long OID = 0;

            try
            {
                CheckSessionAlive(_strLoggedInUser);
                DatabaseProviderFactory factory = new DatabaseProviderFactory();
                Database db = factory.Create("ApplicationDB");
                using (DbCommand _dbCommand = db.GetStoredProcCommand("PSETSAMPLECONTEXT"))
                {
                    _dbCommand.Parameters.Add(new SqlParameter("@LOGGEDINUSER", _strLoggedInUser));
                    _dbCommand.Parameters.Add(new SqlParameter("@EQUIPMENTID", _strEquipmentID));
                    _dbCommand.Parameters.Add(new SqlParameter("@SAMPLECONTEXTJSON", _strSampleContextJSON));
                    SqlParameter _sqlParam = new SqlParameter();
                    _sqlParam.ParameterName = "@RESULTCODE";
                    _sqlParam.DbType = DbType.Int16;
                    _sqlParam.Direction = ParameterDirection.Output;
                    _dbCommand.Parameters.Add(_sqlParam);

                    strResult = (string)db.ExecuteScalar(_dbCommand);

                    iResultCode = Convert.ToInt16(_dbCommand.Parameters["@RESULTCODE"].Value);

                    //On Success
                    if (iResultCode == 0)
                        return strResult;
                    else
                    {
                        StringBuilder _strErrorMsg = new StringBuilder();
                        _strErrorMsg.Append("\nParameters: \n");
                        _strErrorMsg.Append("strLoggedInUser : " + _strLoggedInUser + "\n");
                        _strErrorMsg.Append("strEquipmentID : " + _strEquipmentID + "\n");
                        _strErrorMsg.Append("strSampleContextJSON : " + _strSampleContextJSON + "\n");
                        //If ResultCode is not equal to 0, log the RESPONSEMSG value returned by the SP, alongwith the ResultCode and the parameters
                        log.Error("Error: ResultCode=" + iResultCode.ToString() + ": " + strResult + _strErrorMsg);
                    }
                }
            }
            catch (Exception ex)
            {
                StringBuilder _strErrorMsg = new StringBuilder();
                _strErrorMsg.Append("\nError Message : " + ex.Message.ToString() + "\n");
                _strErrorMsg.Append("Parameters: \n");
                _strErrorMsg.Append("strLoggedInUser : " + _strLoggedInUser + "\n");
                _strErrorMsg.Append("strEquipmentID : " + _strEquipmentID + "\n");
                _strErrorMsg.Append("strSampleContextJSON : " + _strSampleContextJSON + "\n");
                log.Error(_strErrorMsg);
            }
            return strResult;
        }



        [WebMethod]
        public static int ReconnectDevice(string _strLoggedInUser, long deviceRawDataOID)
        {
            int resultCode = 0;
            try
            {
                CheckSessionAlive(_strLoggedInUser);

                DatabaseProviderFactory factory = new DatabaseProviderFactory();
                Database db = factory.Create("ApplicationDB");
                using (DbCommand _dbCommand = db.GetStoredProcCommand("PRECONNECTDEVICE"))
                {
                    _dbCommand.Parameters.Add(new SqlParameter("@LOGGEDINUSER", _strLoggedInUser));
                    _dbCommand.Parameters.Add(new SqlParameter("@DEVICERAWDATAOID", deviceRawDataOID));

                    SqlParameter _sqlParam = new SqlParameter();
                    _sqlParam.ParameterName = "@RESULTCODE";
                    _sqlParam.DbType = DbType.Int16;
                    _sqlParam.Direction = ParameterDirection.Output;
                    _dbCommand.Parameters.Add(_sqlParam);
                    db.ExecuteNonQuery(_dbCommand);
                    resultCode = Convert.ToInt16(_dbCommand.Parameters["@RESULTCODE"].Value);
                    if (resultCode < 0)
                    {
                        StringBuilder _strErrorMsg = new StringBuilder();
                        _strErrorMsg.Append("\nResultCode : " + resultCode + "\n");
                        _strErrorMsg.Append("Parameters: \n");
                        _strErrorMsg.Append("strLoggedInUser : " + _strLoggedInUser + "\n");
                        _strErrorMsg.Append("deviceRawDataOID : " + deviceRawDataOID.ToString() + "\n");
                        log.Error(_strErrorMsg);
                    }
                }
                return resultCode;
            }
            catch (Exception ex)
            {
                resultCode = -2;
                StringBuilder _strErrorMsg = new StringBuilder();
                _strErrorMsg.Append("\nError Message : " + ex.Message.ToString() + "\n");
                _strErrorMsg.Append("Parameters: \n");
                _strErrorMsg.Append("strLoggedInUser : " + _strLoggedInUser + "\n");
                _strErrorMsg.Append("deviceRawDataOID : " + deviceRawDataOID.ToString() + "\n");

                log.Error(_strErrorMsg);
            }
            return resultCode;
        }

        [WebMethod]
        public static string GetFileMetaData(string _strLoggedInUser, string _strEquipmentID, long deviceRawDataOID, string strFileHeaderDisplayText)
        {
            string strResult = string.Empty;
            FileMetaDataDTO oFileMetaDataDTO = new FileMetaDataDTO();

            try
            {
                CheckSessionAlive(_strLoggedInUser);
                DatabaseProviderFactory factory = new DatabaseProviderFactory();
                Database db = factory.Create("ApplicationDB");
                using (DbCommand _dbCommand = db.GetStoredProcCommand("PGETFILEMETADATA"))
                {
                    _dbCommand.Parameters.Add(new SqlParameter("@DEVICERAWDATAOID", deviceRawDataOID));
                    using (IDataReader dr = db.ExecuteReader(_dbCommand))
                    {
                        if (dr.Read())
                        {
                            int iRawDataFileTargetPath = dr.GetOrdinal("RAWDATAFILETARGETPATH");
                            int iHeaderText = dr.GetOrdinal("HEADERTEXT");
                            int iRawDataPacket = dr.GetOrdinal("RAWDATAPACKET");
                            int iFooterText = dr.GetOrdinal("FOOTERTEXT");
                            int iMetaTag = dr.GetOrdinal("METATAG");
                            int TargetFileEncoding = dr.GetOrdinal("TARGETFILEENCODING");

                            oFileMetaDataDTO.RAWDATAFILETARGETPATH = !dr.IsDBNull(iRawDataFileTargetPath) ? dr.GetString(iRawDataFileTargetPath) : string.Empty;
                            oFileMetaDataDTO.HEADERTEXT = !dr.IsDBNull(iHeaderText) ? dr.GetString(iHeaderText) : string.Empty;
                            oFileMetaDataDTO.RAWDATAPACKET = !dr.IsDBNull(iRawDataPacket) ? dr.GetString(iRawDataPacket) : string.Empty;
                            oFileMetaDataDTO.FOOTERTEXT = !dr.IsDBNull(iFooterText) ? dr.GetString(iFooterText) : string.Empty;
                            oFileMetaDataDTO.METATAG = !dr.IsDBNull(iMetaTag) ? dr.GetString(iMetaTag) : string.Empty;
                            oFileMetaDataDTO.TARGETFILEENCODING = !dr.IsDBNull(TargetFileEncoding) ? dr.GetString(TargetFileEncoding) : string.Empty;

                            strResult = CreateFile(_strEquipmentID, oFileMetaDataDTO, deviceRawDataOID, strFileHeaderDisplayText, _strLoggedInUser);

                        }
                        else
                        {
                            //If no recordset returned 
                            strResult = "Cannot create file, Sample context not found";

                            log.Error(strResult);
                        }
                    }
                }
            }
            catch (Exception ex)
            {

                strResult = "Failed to create file. Please try again later.";
                StringBuilder _strErrorMsg = new StringBuilder();
                _strErrorMsg.Append("\nError Message : " + ex.Message.ToString() + "\n");
                _strErrorMsg.Append("Parameters: \n");
                _strErrorMsg.Append("strLoggedInUser : " + _strLoggedInUser + "\n");
                _strErrorMsg.Append("strEquipmentID : " + _strEquipmentID + "\n");
                log.Error(_strErrorMsg);
            }
            return strResult;
        }

        public static string CreateFile(string _strEquipmentID, FileMetaDataDTO _oFileMetaDataDTO,
            long deviceRawDataOID, string _strFileHeaderDisplayText, string _strLoggedInUser)
        {
            string strResult = string.Empty;
            int iUpdateSampleContextResult = 0;
            try
            {
                CheckSessionAlive(_strLoggedInUser);

                string _strFileContent = string.Empty;
                string _strFileName = string.Empty;

                StringBuilder _strErrorMsg = new StringBuilder();
                _strErrorMsg.Append("\nParameters: \n");
                _strErrorMsg.Append("strEquipmentID : " + _strEquipmentID + "\n");
                _strErrorMsg.Append("lstFileMetaDataDTO : " + _oFileMetaDataDTO.ToString() + "\n");
                _strErrorMsg.Append("deviceRawDataOID : " + deviceRawDataOID.ToString() + "\n");
                _strErrorMsg.Append("strFileHeaderDisplayText : " + _strFileHeaderDisplayText + "\n");
                _strErrorMsg.Append("strLoggedInUser : " + _strLoggedInUser + "\n");

                if (string.IsNullOrEmpty(_oFileMetaDataDTO.RAWDATAPACKET))
                {
                    strResult = "Please collect data first before proceeding to create file.";
                    log.Error("Error: " + strResult + _strErrorMsg);
                    return strResult;
                }

                if (string.IsNullOrEmpty(_oFileMetaDataDTO.RAWDATAFILETARGETPATH))
                {
                    strResult =
                        "File path not set for the current Equipment. Please contact System administrator to resolve this issue.";
                    log.Error("Error: " + strResult + _strErrorMsg);
                    return strResult;
                }

                _oFileMetaDataDTO.HEADERTEXT = string.IsNullOrEmpty(_oFileMetaDataDTO.HEADERTEXT) ? _oFileMetaDataDTO.HEADERTEXT : _oFileMetaDataDTO.HEADERTEXT + "\n \n";
                _oFileMetaDataDTO.FOOTERTEXT = string.IsNullOrEmpty(_oFileMetaDataDTO.FOOTERTEXT) ? _oFileMetaDataDTO.FOOTERTEXT : _oFileMetaDataDTO.FOOTERTEXT + "\n \n";
                _strFileContent = _oFileMetaDataDTO.HEADERTEXT + _strFileHeaderDisplayText + "\n \n" + _oFileMetaDataDTO.RAWDATAPACKET + "\n \n" +
                                  _oFileMetaDataDTO.FOOTERTEXT + _oFileMetaDataDTO.METATAG;

                char[] separators = new char[] { ' ', ':', ';', ',', '\r', '\t', '\n', '*', '<', '>', '|', '?', '/', '\\', '"' };
                string strTmpFileName = _strEquipmentID;
                string[] strSplit = strTmpFileName.Split(separators, StringSplitOptions.RemoveEmptyEntries);
                strTmpFileName = string.Join("", strSplit);
                _strFileName = strTmpFileName + "_" + DateTime.Now.ToString("yyyyddMMHHmmssfff") + ".txt";

                try
                {
                    if (!Directory.Exists(_oFileMetaDataDTO.RAWDATAFILETARGETPATH))
                    {
                        strResult = "Failed to create file as the path configuration is invalid. Please contact System Administrator.";
                        log.Error("Error: " + strResult + _strErrorMsg);
                        return strResult;
                    }

                    string _strTemppath = Path.Combine(_oFileMetaDataDTO.RAWDATAFILETARGETPATH, "temp");
                    if (!Directory.Exists(_strTemppath))
                    {
                        Directory.CreateDirectory(_strTemppath);
                    }

                    //To apply encoding of the file content while writing
                    switch (_oFileMetaDataDTO.TARGETFILEENCODING)
                    {
                        case "NONE":
                            File.WriteAllText(Path.Combine(_strTemppath, _strFileName),
                                _strFileContent); // Write file in temp location.
                            break;
                        case "DEFAULT":
                            File.WriteAllText(Path.Combine(_strTemppath, _strFileName), _strFileContent,
                                Encoding.Default);
                            break;
                        default:
                            int.TryParse(_oFileMetaDataDTO.TARGETFILEENCODING, out int codePage);
                            if (codePage <= 0)
                            {
                                strResult =
                                    "Failed to create file as the encoding configuration is invalid. Please contact System Administrator.";
                                log.Error("Error: " + strResult + _strErrorMsg);
                                return strResult;
                            }

                            File.WriteAllText(Path.Combine(_strTemppath, _strFileName), _strFileContent,
                                Encoding.GetEncoding(codePage));
                            break;
                    }

                    iUpdateSampleContextResult = UpdateSampleContextMetaData(_strLoggedInUser, deviceRawDataOID,
                        _strFileHeaderDisplayText, _strFileName);

                    //After success we need move the file to original target path from temp
                    if (iUpdateSampleContextResult == 0)
                    {
                        string strSourceFile = Path.Combine(_strTemppath, _strFileName);
                        string strDestinationFile = Path.Combine(_oFileMetaDataDTO.RAWDATAFILETARGETPATH, _strFileName);
                        File.Move(strSourceFile, strDestinationFile);
                        strResult = "File created successfully.";
                        log.Info(strResult);
                    }
                    else
                    {
                        strResult = "Failed to create file. Please try again later.";
                        log.Error("Error: " + strResult + _strErrorMsg);
                    }

                }
                catch (Exception ex)
                {
                    strResult = "Failed to create file. Please try again later.";
                    StringBuilder _strException = new StringBuilder();
                    _strException.Append("\nError Message : " + ex.Message.ToString() + "\n");
                    _strException.Append("Parameters: \n");
                    _strException.Append("deviceRawDataOID : " + deviceRawDataOID.ToString() + "\n");
                    _strException.Append("lstFileMetaDataDTO : " + _oFileMetaDataDTO.ToString() + "\n");
                    _strException.Append("strFileHeaderDisplayText : " + _strFileHeaderDisplayText + "\n");
                    _strException.Append("strLoggedInUser : " + _strLoggedInUser + "\n");
                    log.Error(_strException);
                }

            }
            catch (Exception ex)
            {
                strResult = "Failed to create file. Please try again later.";

                StringBuilder _strErrorMsg = new StringBuilder();
                _strErrorMsg.Append("\nError Message : " + ex.Message.ToString() + "\n");
                _strErrorMsg.Append("Parameters: \n");
                _strErrorMsg.Append("strEquipmentID : " + _strEquipmentID + "\n");
                _strErrorMsg.Append("lstFileMetaDataDTO : " + _oFileMetaDataDTO.ToString() + "\n");
                _strErrorMsg.Append("strFileHeaderDisplayText : " + _strFileHeaderDisplayText + "\n");
                _strErrorMsg.Append("strLoggedInUser : " + _strLoggedInUser + "\n");
                log.Error(_strErrorMsg);
            }

            return strResult;
        }

        public static int UpdateSampleContextMetaData(string _strLoggedInUser, long _deviceRawDataOID, string _strFileHeaderDisplayText, string _strFileName)
        {
            int resultCode = 0;
            try
            {
                DatabaseProviderFactory factory = new DatabaseProviderFactory();
                Database db = factory.Create("ApplicationDB");
                using (DbCommand _dbCommand = db.GetStoredProcCommand("PUPDATESAMPLECONTEXTMETADATA"))
                {
                    _dbCommand.Parameters.Add(new SqlParameter("@LOGGEDINUSER", _strLoggedInUser));
                    _dbCommand.Parameters.Add(new SqlParameter("@DEVICERAWDATAOID", _deviceRawDataOID));
                    _dbCommand.Parameters.Add(new SqlParameter("@SAMPLECONTEXTHEADER", _strFileHeaderDisplayText));
                    _dbCommand.Parameters.Add(new SqlParameter("@FILENAME", _strFileName));
                    SqlParameter _sqlParam = new SqlParameter();
                    _sqlParam.ParameterName = "@RESULTCODE";
                    _sqlParam.DbType = DbType.Int16;
                    _sqlParam.Direction = ParameterDirection.Output;
                    _dbCommand.Parameters.Add(_sqlParam);
                    db.ExecuteNonQuery(_dbCommand);
                    resultCode = Convert.ToInt16(_dbCommand.Parameters["@RESULTCODE"].Value);
                }
            }
            catch (Exception ex)
            {
                resultCode = -2;
                StringBuilder _strErrorMsg = new StringBuilder();
                _strErrorMsg.Append("\nError Message : " + ex.Message.ToString() + "\n");
                _strErrorMsg.Append("Parameters: \n");
                _strErrorMsg.Append("strLoggedInUser : " + _strLoggedInUser + "\n");
                _strErrorMsg.Append("deviceRawDataOid : " + _deviceRawDataOID.ToString() + "\n");
                _strErrorMsg.Append("strFileHeaderDisplayText : " + _strFileHeaderDisplayText + "\n");
                _strErrorMsg.Append("strFileName : " + _strFileName + "\n");
                log.Error(_strErrorMsg);
            }
            return resultCode;
        }

        public static bool ColumnExists(IDataReader reader, string columnName)
        {
            for (int i = 0; i < reader.FieldCount; i++)
            {
                if (reader.GetName(i).Equals(columnName, StringComparison.InvariantCultureIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        [WebMethod]
        public static List<FieldDataDTO> GetChildControlData(string _strLoggedInUser, string targetControl, string visibleControlsData, string mandatory, string mandatorytext)
        {
            List<FieldDataDTO> lstFieldata = new List<FieldDataDTO>();
            string[] data = null;
            string[] keyValue;

            DataTable dt = new DataTable();
            dt.Columns.Add("PARAMKEY", typeof(string));
            dt.Columns.Add("PARAMVALUE", typeof(string));

            XElement identity = new XElement("params");
            if (visibleControlsData != "")
            {
                if (visibleControlsData.Contains("&,#"))
                {
                    data = visibleControlsData.Split(new string[] { "&,#" }, StringSplitOptions.RemoveEmptyEntries);
                }
                else
                {
                    keyValue = visibleControlsData.Split(new string[] { "&:#" }, StringSplitOptions.RemoveEmptyEntries);
                    dt.Rows.Add(keyValue[0], keyValue[1]);
                }

                if (data != null)
                {
                    for (int i = 0; i < data.Length; i++)
                    {
                        keyValue = data[i].Split(new string[] { "&:#" }, StringSplitOptions.RemoveEmptyEntries);
                        dt.Rows.Add(keyValue[0], keyValue[1]);
                    }
                }
            }

            try
            {
                CheckSessionAlive(_strLoggedInUser);
                DatabaseProviderFactory factory = new DatabaseProviderFactory();
                Database db = factory.Create("LIMSDB");

                if (mandatory != "undefined" && mandatory.ToLower() == "no")
                {
                    lstFieldata.Add(new FieldDataDTO
                    {
                        FieldKey = "NA",
                        FieldValue = mandatorytext == "undefined" ? " " : mandatorytext
                    });
                }

                using (DbCommand _dbCommand = db.GetStoredProcCommand("PGETDATAFORUI"))
                {
                    _dbCommand.Parameters.Add(new SqlParameter("@LOGGEDINUSER", _strLoggedInUser));
                    _dbCommand.Parameters.Add(new SqlParameter("@COMBOID", targetControl));
                    _dbCommand.Parameters.Add(new SqlParameter("PARAMS", dt));

                    using (IDataReader dr = db.ExecuteReader(_dbCommand))
                    {

                        if (dr.Read())
                        {
                            bool checkAIColumnExists = ColumnExists(dr, "ADDITIONALINFO");
                            if (dr["PARAMKEY"] + "" == "ERROR")
                            {
                                log.Error(dr["PARAMVALUE"] + "");
                            }
                            else
                            {
                                do
                                {
                                    lstFieldata.Add(new FieldDataDTO
                                    {
                                        FieldKey = dr["PARAMKEY"].ToString(),
                                        FieldValue = dr["PARAMVALUE"].ToString(),
                                        //AdditionalInfo = checkAIColumnExists ? dr["ADDITIONALINFO"].ToString() : string.Empty
                                        AdditionalInfo = checkAIColumnExists ? dr["ADDITIONALINFO"].ToString() : dr["PARAMKEY"].ToString() + " - " + dr["PARAMVALUE"].ToString()  // to remove
                                    }); ;
                                }
                                while (dr.Read());
                            }
                        }
                        else
                        {
                            StringBuilder _strErrorMsg = new StringBuilder();
                            _strErrorMsg.Append("\n No values were returned from SP for -  : " + visibleControlsData + "\n");
                            _strErrorMsg.Append("strLoggedInUser : " + _strLoggedInUser + "\n");
                            log.Error(_strErrorMsg);
                        }

                    }
                }
            }
            catch (Exception ex)
            {
                StringBuilder _strErrorMsg = new StringBuilder();
                _strErrorMsg.Append("\nError Message : " + ex.Message.ToString() + "\n");
                _strErrorMsg.Append("Parameters: \n");
                _strErrorMsg.Append("targetControl : " + targetControl + "\n");
                _strErrorMsg.Append("visibleControlsData : " + visibleControlsData + "\n");
                _strErrorMsg.Append("strLoggedInUser : " + _strLoggedInUser + "\n");
                log.Error(_strErrorMsg);
            }
            return lstFieldata;
        }

        [System.Web.Services.WebMethod(EnableSession = true)]
        public static bool CheckSessionAlive(string LoggedInUser)
        {
            bool bResult = false;
            int sessionTimeout = 0;
            try
            {
                string sLoginID = Convert.ToString(HttpContext.Current.Session["LoginID"]);
                sessionTimeout = System.Web.HttpContext.Current.Session.Timeout;
                if (!string.IsNullOrEmpty(sLoginID))
                    bResult = true;
                else
                {
                    InsertAuditLog("LOGIN", "SESSION-EXPIRED", LoggedInUser, null, null);
                    log.Info("Login session expired for the user : " + LoggedInUser + " and session timeout = " + Convert.ToString(sessionTimeout));
                    HttpContext.Current.Session["LoginID"] = LoggedInUser;
                    InsertAuditLog("LOGIN", "SESSION-RE-ASSIGNED", LoggedInUser, null, null);
                    log.Info("Login session re-assigned for the user : " + LoggedInUser + " and session timeout = " + Convert.ToString(sessionTimeout));

                }
            }
            catch (Exception ex)
            {
                bResult = false;
                InsertAuditLog("LOGIN", "SESSION-EXPIRED-EXCEPTION", LoggedInUser, null, null);
                log.Info("Login session expired for the user : " + LoggedInUser + " due to exception and session timeout = " + Convert.ToString(sessionTimeout));
                log.Error("CheckSessionAlive: ", ex);
                HttpContext.Current.Session["LoginID"] = LoggedInUser;
                InsertAuditLog("LOGIN", "SESSION-EXCEPTION-RE-ASSIGNED", LoggedInUser, null, null);
                log.Info("Login session exception re-assigned for the user : " + LoggedInUser + " and session timeout = " + Convert.ToString(sessionTimeout));
            }
            return bResult;
        }

        [System.Web.Services.WebMethod(EnableSession = true)]
        public static bool SessionExpired(string LoggedInUser, string sessionTimeout)
        {
            bool bResult = false;
            try
            {
                HttpContext.Current.Session.Clear();
                InsertAuditLog("LOGIN", "SESSION-EXPIRED", LoggedInUser, null, null);
                log.Info("Login session expired for the user : " + LoggedInUser + " and session timeout = " + Convert.ToString(sessionTimeout));
            }
            catch (Exception ex)
            {
                bResult = false;
                InsertAuditLog("LOGIN", "SESSION-EXPIRED-EXCEPTION", LoggedInUser, null, null);
                log.Info("Login session expired for the user : " + LoggedInUser + " due to exception and session timeout = " + Convert.ToString(sessionTimeout));
                log.Error("SessionAudit: ", ex);
            }
            return bResult;
        }

        public static int InsertAuditLog(string _tableName, string _action, string _userID, string _oldData, string _newData)
        {
            int resultCode = 0;
            try
            {
                DatabaseProviderFactory factory = new DatabaseProviderFactory();
                Database db = factory.Create("ApplicationDB");
                using (DbCommand _dbCommand = db.GetStoredProcCommand("PINSERTAUDITLOG"))
                {
                    _dbCommand.Parameters.Add(new SqlParameter("@TABLENAME", _tableName));
                    _dbCommand.Parameters.Add(new SqlParameter("@ACTION", _action));
                    _dbCommand.Parameters.Add(new SqlParameter("@USERID", _userID));
                    _dbCommand.Parameters.Add(new SqlParameter("@OLDDATA", _oldData));
                    _dbCommand.Parameters.Add(new SqlParameter("@NEWDATA", _newData));
                    _dbCommand.Parameters.Add(new SqlParameter("@EVENTDATETIMESTAMPUTC", DateTime.UtcNow));

                    resultCode = db.ExecuteNonQuery(_dbCommand);
                }
            }
            catch (Exception ex)
            {
                resultCode = -1;
                StringBuilder _strErrorMsg = new StringBuilder();
                _strErrorMsg.Append("\nError Message : " + ex.Message.ToString() + "\n");
                _strErrorMsg.Append("Parameters: \n");
                _strErrorMsg.Append("tableName : " + _tableName + "\n");
                _strErrorMsg.Append("action : " + _action + "\n");
                _strErrorMsg.Append("userID : " + _userID + "\n");
                _strErrorMsg.Append("oldData : " + _oldData + "\n");
                _strErrorMsg.Append("newData : " + _newData + "\n");
                _strErrorMsg.Append("eventDateTimeStampUTC : " + DateTime.UtcNow.ToString() + "\n");
                log.Error(_strErrorMsg);
            }
            return resultCode;
        }
        public static string GetScriptUrl(string fileName)
        {
            // generate a unique version number  
            string version = System.Configuration.ConfigurationManager.AppSettings["Version"].ToString(); //DateTime.UtcNow.Ticks.ToString();
            string url = VirtualPathUtility.ToAbsolute(fileName);

            return url + "?v=" + version;

        }

        [System.Web.Services.WebMethod(EnableSession = true)]
        public static bool Logout(string LoggedInUser)
        {
            try
            {
                HttpContext.Current.Session.Clear();
                InsertAuditLog("LOGOUT", "LOGOUT", LoggedInUser, null, null);
                log.Info("Logout by the user : " + LoggedInUser);
            }
            catch (Exception ex)
            {
                InsertAuditLog("LOGOUT", "LOGOUT-EXCEPTION", LoggedInUser, null, null);
                log.Error("Logout exception by the user : " + LoggedInUser, ex);
            }
            return true;
        }
    }
}
