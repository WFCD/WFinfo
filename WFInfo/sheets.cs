using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using System.Collections;
using System.Collections.Generic;

namespace WFInfo
{
    internal class Sheets
    {
        private readonly GoogleCredential Cred;
        private readonly string SheetID = "1uAbqfwBYrcqlWCJad4juankmu9-_UqzBmd7XtrrrwkM";
        private readonly SheetsService service;

        public Sheets()
        {
            Cred = GoogleCredential.FromJson(Properties.Resources.google_creds).CreateScoped(SheetsService.Scope.SpreadsheetsReadonly);
            service = new SheetsService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = Cred,
                ApplicationName = "WFInfo"
            });
        }


        public IList<IList<object>> GetSheet(string range)
        {
            try {
                ValueRange response = service.Spreadsheets.Values.Get(SheetID, range).Execute();
                return response.Values;
            }
            catch (System.Exception) {
                Main.AddLog("Unable to get response from google sheets");
                Main.StatusUpdate("Unable to fetch database", 1);
                throw;
            }

        }
    }
}