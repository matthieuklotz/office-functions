using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Newtonsoft.Json;
using System.IO;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Cellenza.Functions.Office
{
    public static class WordFunctions
    {
        [FunctionName("GenerateDocxFromTemplate")]
        public async static Task<IActionResult> GenerateDocxFromTemplate(
            [HttpTrigger(AuthorizationLevel.Function, "POST", Route = "office/word/generate/{template}")] HttpRequest request,
            [Blob("office/templates/{template}.dotx", FileAccess.Read, Connection = "Office.StorageAccount.ConnectionString")] Stream wordTemplate,
            TraceWriter log)
        {
            log.Info($"Function CreateDocx called, with uri {request.Path}");
            string requestBody = await new StreamReader(request.Body).ReadToEndAsync();
            XNode root = JsonConvert.DeserializeXNode(requestBody, "root");
            string xmlPart = root.ToString(SaveOptions.DisableFormatting);


            MemoryStream result = new MemoryStream();
            await wordTemplate.CopyToAsync(result);
            using (WordprocessingDocument doc = WordprocessingDocument.Open(result, true))
            {
                doc.ChangeDocumentType(WordprocessingDocumentType.Document);
                MainDocumentPart main = doc.MainDocumentPart;
                main.DeleteParts<CustomXmlPart>(main.CustomXmlParts);
                CustomXmlPart customXml = main.AddCustomXmlPart(CustomXmlPartType.CustomXml);
                using (StreamWriter ts = new StreamWriter(customXml.GetStream()))
                {
                    ts.Write(xmlPart);
                }
            }

            result.Seek(0, SeekOrigin.Begin);
            return new FileStreamResult(result, "application/ms-word");
        }
    }
}
