// ------------------------------------------------------------------------------------------------------------------
// About  : This Console App retrives Inspection Deatils & insert Data into D365 for Daily Processing
// Author : 
// Date   : 2/18/2022
//-------------------------------------------------------------------------------------------------------------------


#region Namespace
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using System.Linq;
    using SODA;
    using System.Net;
    using System.IO;
    using Newtonsoft.Json.Linq;
    using Newtonsoft.Json;
    using Microsoft.Crm.Sdk.Messages;
    using Microsoft.IdentityModel.Clients.ActiveDirectory;
    using Microsoft.Xrm.Sdk;
    using Microsoft.Xrm.Sdk.Query;
    using Microsoft.Xrm.Sdk.WebServiceClient;
    using Microsoft.Xrm.Tooling.Connector;
#endregion

namespace ConsoleApp_Inspection
{
    class RetrieveInspectionDetails
    {
        public static async Task Main(string[] args)
        {
            try
            {
                IOrganizationService organizationService = null;

                // Create Dynamics CRM Connection ....................................
                string clientID = "ac8512b1-977a-4665-be7d-a1f78051ef89";
                string clientSecret = "qS2kh5__.NdV4TRh_UY0-z9S-n9RIklYI0";
                string resource = "https://gfwpoc.crm.dynamics.com";
                string authority = "https://login.microsoftonline.com/d52d0834-97c6-45cc-a07e-07ded8eeec67/oauth2/authorize";

                AuthenticationResult _authResult;

                AuthenticationContext authContext = new AuthenticationContext(authority);

                ClientCredential credentials = new ClientCredential(clientID, clientSecret);
                _authResult = await authContext.AcquireTokenAsync(resource, credentials);

                string authToken = _authResult.AccessToken;
                string crmServiceURL = "/xrmservices/2011/organization.svc/web?SdkClientVersion=9.1";
                Uri serviceUrl = new Uri(resource + crmServiceURL.ToString());

                using (OrganizationWebProxyClient sdkService = new OrganizationWebProxyClient(serviceUrl, false))
                {
                    sdkService.HeaderToken = authToken;
                    organizationService = (IOrganizationService)sdkService;
                    Guid userid = ((WhoAmIResponse)organizationService.Execute(new WhoAmIRequest())).UserId;
                }

                // Retrieve subset of Inspection Details for Daily Processing ...........................
                string requestURL = "https://data.smgov.net/resource/xird-2kxi.json?$$app_token=zqEdnXx9ZDWNAbisD5B4AmlFT&status='C of O'&inspection_scheduled_date=2022-02-01";

                WebRequest request = HttpWebRequest.Create(requestURL);
                WebResponse response = request.GetResponse();
                StreamReader reader = new StreamReader(response.GetResponseStream());
                string inspectionsDetails = reader.ReadToEnd();

                dynamic inspectionsDetailsObj = JsonConvert.DeserializeObject(inspectionsDetails);

                // Process through each Inspection ...........................................
                foreach (var inspection in inspectionsDetailsObj)
                {
                    var eachInspectionRequest = inspection;
                    ProcessInspectionDetails(eachInspectionRequest, organizationService);
                }

                // Testing ....
                //string startupPath = System.IO.Directory.GetCurrentDirectory();
                //string folderPath = startupPath.Substring(0, startupPath.IndexOf("\\bin\\Debug")) + "\\SampleRequestDetails.txt";
                //string sampleRequest = System.IO.File.ReadAllText(folderPath);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message.ToString());
            }
        }

        // Process Each Inspection ..............................................................
        public static void ProcessInspectionDetails(dynamic inspectionRequest, IOrganizationService organizationService)
        {
            try
            {
                string requestDetail = Convert.ToString(inspectionRequest);
                var inspectionDetails = JsonConvert.DeserializeObject<Dictionary<string, object>>(requestDetail);

                string requestId = string.Empty;
                string permitNO = string.Empty;
                string inspectionScheduledDate = string.Empty;
                string estimatedInspectionArrivalTime = string.Empty;
                string address = string.Empty;
                string inspectionType = string.Empty;
                string description = string.Empty;
                string inspectorAssigned = string.Empty;
                string status = string.Empty;
                string valuation = string.Empty;
                string classCode = string.Empty;
                string classCodeDescription = string.Empty;
                string inspectionRequestDate = string.Empty;
                string permitIssuanceDate = string.Empty;
                string permitExpirationDate = string.Empty;
                string firstInspector = string.Empty;
                string lastInspector = string.Empty;
                string permitType = string.Empty;
                string permitSubType = string.Empty;

                foreach (var item in inspectionDetails)
                {
                    var attributeName = item.Key;
                    var attributeValue = item.Value;
 
                    switch (attributeName)
                    {
                        case "request_id":
                            requestId = Convert.ToString(attributeValue);
                            break;
                        case "permit_number":
                            permitNO = Convert.ToString(attributeValue);
                            break;
                        case "inspection_scheduled_date":
                            inspectionScheduledDate = Convert.ToString(attributeValue);
                            break;
                        case "estimated_inspection_arrival_time":
                            estimatedInspectionArrivalTime = Convert.ToString(attributeValue);
                            break;
                        case "address":
                            address = Convert.ToString(attributeValue);
                            break;
                        case "inspection_type":
                            inspectionType = Convert.ToString(attributeValue);
                            break;
                        case "description":
                            description = Convert.ToString(attributeValue);
                            break;
                        case "inspector_assigned":
                            inspectorAssigned = Convert.ToString(attributeValue);
                            break;
                        case "status":
                            status = Convert.ToString(attributeValue);
                            break;
                        case "valuation":
                            valuation = Convert.ToString(attributeValue);
                            break;
                        case "class_code":
                            classCode = Convert.ToString(attributeValue);
                            break;
                        case "class_code_description":
                            classCodeDescription = Convert.ToString(attributeValue);
                            break;
                        case "inspection_request_date":
                            inspectionRequestDate = Convert.ToString(attributeValue);
                            break;
                        case "permit_issuance_date":
                            permitIssuanceDate = Convert.ToString(attributeValue);
                            break;
                        case "permit_expiration_date":
                            permitExpirationDate = Convert.ToString(attributeValue);
                            break;
                        case "first_inspector":
                            firstInspector = Convert.ToString(attributeValue);
                            break;
                        case "last_inspector":
                            lastInspector = Convert.ToString(attributeValue);
                            break;
                        case "permit_type":
                            permitType = Convert.ToString(attributeValue);
                            break;
                        case "permit_sub_type":
                            permitSubType = Convert.ToString(attributeValue);
                            break;
                    }
                }


                // Process Data into D365 ..........................................
                Entity objEntity = new Entity("new_inspectiondetails");
                objEntity["new_name"] = requestId;
                objEntity["new_requestid"] = requestId;
                objEntity["new_permitno"] = permitNO;
                objEntity["new_inspection_scheduled_date"] = Convert.ToDateTime(inspectionScheduledDate);
                objEntity["new_arrivaltime"] = estimatedInspectionArrivalTime;
                objEntity["new_address"] = address;
                objEntity["new_inspection_type"] = inspectionType;
                objEntity["new_description"] = description;
                objEntity["new_inspector_assigned"] = inspectorAssigned;
                objEntity["new_status"] = status;
                objEntity["new_valuation"] = Convert.ToDecimal(valuation);
                objEntity["new_class_code"] = classCode;
                objEntity["new_class_code_description"] = classCodeDescription;
                objEntity["new_inspection_request_date"] = Convert.ToDateTime(inspectionRequestDate);
                objEntity["new_permit_issuance_date"] = Convert.ToDateTime(permitIssuanceDate);
                objEntity["new_permit_expiration_date"] = Convert.ToDateTime(permitExpirationDate);
                objEntity["new_first_inspector"] = firstInspector;
                objEntity["new_last_inspector"] = lastInspector;
                objEntity["new_permit_type"] = permitType;
                objEntity["new_permit_sub_type"] = permitSubType;

                Guid entityguid = organizationService.Create(objEntity);

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message.ToString());
            }
        }
    }
}

